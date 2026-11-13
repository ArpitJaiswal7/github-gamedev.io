using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Studio.NativeImagePipeline
{
    public enum LevelSyncState
    {
        Idle,
        Syncing,
        SyncedOnline,
        SyncedOfflineCache,
        SyncedEmbeddedFallback,
        Failed
    }

    /// <summary>
    /// Level Sync Manager for Calm Jigsaw & Native Image Bridge.
    /// Maps remote published puzzle catalogs from the backend studio (https://arpitjaiswal7.github.io/github-gamedev.io/labs/puzzle-image-studio.html)
    /// to sequential game levels (Level 1, Level 2, ... N).
    /// Features:
    /// - Non-blocking manifest-diffing with silent failover (Local Server :4200 -> GitHub Cloud CDN -> Local Cache -> Embedded 71 Levels).
    /// - Level progression and prefetching (Level N is High priority, Level N+1 is Medium priority).
    /// - Category filtering (Nature, Architecture, Animals, All).
    /// - Offline persistence in PlayerPrefs.
    /// </summary>
    public sealed class LevelSyncManager : MonoBehaviour
    {
        private const string PrefKeyCurrentLevel = "Studio_NIP_CurrentLevel";
        private const string PrefKeyManifestCache = "Studio_NIP_CachedManifestJson";

        private static LevelSyncManager instance;

        public static LevelSyncManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<LevelSyncManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("__LevelSyncManager");
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<LevelSyncManager>();
                    }
                }
                return instance;
            }
        }

        public LevelSyncState State { get; private set; } = LevelSyncState.Idle;
        public string LastSyncSource { get; private set; } = "None";
        public string LastError { get; private set; } = string.Empty;

        public event Action<bool, int> OnSyncCompleted;
        public event Action<int, PuzzleImageEntry> OnLevelChanged;

        private readonly List<PuzzleImageEntry> syncedLevels = new List<PuzzleImageEntry>(128);
        private readonly Dictionary<string, PuzzleImageEntry> puzzleById = new Dictionary<string, PuzzleImageEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> cachedCategories = new List<string>();

        public IReadOnlyList<PuzzleImageEntry> SyncedLevels => syncedLevels;
        public int TotalLevels => syncedLevels.Count;
        public IReadOnlyList<string> Categories => cachedCategories;

        public int CurrentLevel
        {
            get => PlayerPrefs.GetInt(PrefKeyCurrentLevel, 1);
            private set
            {
                int clamped = Mathf.Clamp(value, 1, Mathf.Max(1, TotalLevels));
                PlayerPrefs.SetInt(PrefKeyCurrentLevel, clamped);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Begins syncing puzzle levels from the backend server or static cloud CDN.
        /// </summary>
        public void SyncFromBackend(string customManifestUrl = null, Action<bool, int> onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(SyncFromBackendCoroutine(customManifestUrl, onComplete));
        }

        private IEnumerator SyncFromBackendCoroutine(string customManifestUrl, Action<bool, int> onComplete)
        {
            State = LevelSyncState.Syncing;
            LastError = string.Empty;

            var candidateUrls = new List<string>();
            if (!string.IsNullOrWhiteSpace(customManifestUrl))
            {
                candidateUrls.Add(customManifestUrl.Trim());
            }

            // 1. Try local studio server (http://localhost:4200/api/puzzles/published)
            candidateUrls.Add(ImagePipelineConfig.DefaultLocalServerEndpoint);

            // 2. Try live GitHub Pages cloud CDN (https://arpitjaiswal7.github.io/github-gamedev.io/assets/puzzles/published.json)
            candidateUrls.Add(ImagePipelineConfig.DefaultLiveBackendEndpoint);

            string loadedJson = null;
            string sourceTag = "None";

            for (int i = 0; i < candidateUrls.Count; i++)
            {
                string url = candidateUrls[i];
                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    req.timeout = url.Contains("localhost") ? 2 : 12;
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(req.downloadHandler.text))
                    {
                        string text = req.downloadHandler.text.Trim();
                        if (text.Contains("\"puzzles\"") || text.Contains("\"urls\""))
                        {
                            loadedJson = text;
                            sourceTag = url.Contains("localhost") ? "Local Server (:4200)" : "GitHub Cloud CDN";
                            break;
                        }
                    }
                }
            }

            // 3. Fallback to cached manifest from previous successful session
            if (string.IsNullOrEmpty(loadedJson))
            {
                string cachedJson = PlayerPrefs.GetString(PrefKeyManifestCache, string.Empty);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    loadedJson = cachedJson;
                    sourceTag = "Local Cache (Offline)";
                }
            }

            // 4. Fallback to embedded default published catalog
            if (string.IsNullOrEmpty(loadedJson))
            {
                loadedJson = EmbeddedPublishedCatalog.GetFallbackCatalogJson();
                sourceTag = "Embedded 71 Puzzles Fallback";
            }

            bool success = ApplyManifestJson(loadedJson, sourceTag);

            if (success && sourceTag.Contains("Server") || sourceTag.Contains("CDN"))
            {
                PlayerPrefs.SetString(PrefKeyManifestCache, loadedJson);
                PlayerPrefs.Save();
            }

            onComplete?.Invoke(success, TotalLevels);
            OnSyncCompleted?.Invoke(success, TotalLevels);

            if (success && TotalLevels > 0)
            {
                SetCurrentLevel(CurrentLevel);
            }
        }

        public bool ApplyManifestJson(string manifestJson, string sourceDescription)
        {
            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                State = LevelSyncState.Failed;
                LastError = "Manifest JSON is empty.";
                return false;
            }

            try
            {
                string normalizedJson = manifestJson.Trim();
                if (normalizedJson.StartsWith("["))
                {
                    normalizedJson = "{\"urls\":" + normalizedJson + "}";
                }

                var manifest = JsonUtility.FromJson<PuzzleManifestResponse>(normalizedJson);
                if (manifest == null)
                {
                    State = LevelSyncState.Failed;
                    LastError = "Failed to deserialize PuzzleManifestResponse.";
                    return false;
                }

                var entries = manifest.GetNormalizedEntries();
                if (entries == null || entries.Count == 0)
                {
                    State = LevelSyncState.Failed;
                    LastError = "No puzzle entries found in manifest.";
                    return false;
                }

                syncedLevels.Clear();
                puzzleById.Clear();
                cachedCategories.Clear();
                var catSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < entries.Count; i++)
                {
                    var item = entries[i];
                    item.level = i + 1; // 1-based sequential game level
                    syncedLevels.Add(item);
                    puzzleById[item.id] = item;

                    if (!string.IsNullOrWhiteSpace(item.category) && catSet.Add(item.category.Trim()))
                    {
                        cachedCategories.Add(item.category.Trim());
                    }
                }

                LastSyncSource = sourceDescription;
                State = sourceDescription.Contains("Offline") ? LevelSyncState.SyncedOfflineCache :
                        sourceDescription.Contains("Embedded") ? LevelSyncState.SyncedEmbeddedFallback :
                        LevelSyncState.SyncedOnline;

                // Enqueue records into NativeImagePipeline
                var pipeline = NativeImagePipeline.Instance;
                for (int i = 0; i < syncedLevels.Count; i++)
                {
                    var p = syncedLevels[i];
                    var tier = p.saveToRomOnDownload ? StorageTier.PermanentROM : StorageTier.VolatileCache;
                    pipeline.EnqueueUrl(p.url, RequestPriority.Low, tier, p.id);
                }

                return true;
            }
            catch (Exception ex)
            {
                State = LevelSyncState.Failed;
                LastError = ex.Message;
                Debug.LogError($"[LevelSyncManager] Manifest error: {ex.Message}");
                return false;
            }
        }

        public PuzzleImageEntry GetPuzzleForLevel(int levelNumber)
        {
            if (syncedLevels.Count == 0) return null;
            int idx = Mathf.Clamp(levelNumber - 1, 0, syncedLevels.Count - 1);
            return syncedLevels[idx];
        }

        public PuzzleImageEntry GetPuzzleById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            puzzleById.TryGetValue(id.Trim(), out var puzzle);
            return puzzle;
        }

        public List<PuzzleImageEntry> GetPuzzlesByCategory(string category)
        {
            var list = new List<PuzzleImageEntry>();
            if (string.IsNullOrWhiteSpace(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                list.AddRange(syncedLevels);
                return list;
            }

            for (int i = 0; i < syncedLevels.Count; i++)
            {
                if (string.Equals(syncedLevels[i].category, category, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(syncedLevels[i]);
                }
            }
            return list;
        }

        public void SetCurrentLevel(int newLevel)
        {
            if (TotalLevels == 0) return;
            CurrentLevel = newLevel;
            var currentPuzzle = GetPuzzleForLevel(CurrentLevel);

            if (currentPuzzle != null)
            {
                var pipeline = NativeImagePipeline.Instance;
                // Boost current level to High priority
                pipeline.PrioritizeViewportItems(new[] { currentPuzzle.id });

                // Prefetch next level (Level N + 1) with Low/Medium priority
                int nextLvl = CurrentLevel + 1;
                if (nextLvl <= TotalLevels)
                {
                    var nextPuzzle = GetPuzzleForLevel(nextLvl);
                    if (nextPuzzle != null)
                    {
                        pipeline.EnqueueUrl(nextPuzzle.url, RequestPriority.Low, StorageTier.VolatileCache, nextPuzzle.id);
                    }
                }

                OnLevelChanged?.Invoke(CurrentLevel, currentPuzzle);
            }
        }

        public void NextLevel()
        {
            if (CurrentLevel < TotalLevels)
            {
                SetCurrentLevel(CurrentLevel + 1);
            }
            else
            {
                SetCurrentLevel(1); // loop back
            }
        }

        public void PrevLevel()
        {
            if (CurrentLevel > 1)
            {
                SetCurrentLevel(CurrentLevel - 1);
            }
            else
            {
                SetCurrentLevel(TotalLevels);
            }
        }
    }
}

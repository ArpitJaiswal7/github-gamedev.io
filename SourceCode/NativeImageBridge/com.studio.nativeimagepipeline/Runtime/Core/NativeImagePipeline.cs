using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Primary developer-facing API for the Native Image Pipeline across Android (.java), iOS (.mm),
    /// WebGL (.jslib), and Unity Editor.
    ///
    /// Features:
    /// - Zero-touch connectivity detection: automatically starts/pauses/resumes background downloads.
    /// - Single download -> Dual-tier JPEG conversion: saves both 256px (Icon) and 1080px (FullSize) JPEGs.
    /// - Custom-size helper: generates arbitrary sizes (e.g., 512px) on demand from local 1080px cache or network.
    /// - String-keyed request registry: tracks status, byte sizes, duration, and errors for 100+ puzzle URLs.
    /// - Viewport priority queue: promotes visible ScrollView puzzle URLs and purges faulty strings.
    /// - Dual storage tiers: Volatile Cache vs. Permanent ROM hard-drive persistence.
    /// - Direct Sprite conversion with reference-counted Texture2D memory management.
    /// </summary>
    public sealed class NativeImagePipeline : MonoBehaviour
    {
        private static NativeImagePipeline instance;

        [SerializeField]
        [Tooltip("Optional configuration asset. If unassigned, default settings are used.")]
        private ImagePipelineConfig config;

        private INativeImageBridge nativeBridge;
        private SpriteMemoryCache spriteCache;

        // Master registry of all puzzle image requests keyed by their deterministic RequestKey string
        private readonly Dictionary<string, ImageRequestRecord> requestRegistry
            = new Dictionary<string, ImageRequestRecord>(StringComparer.Ordinal);

        // Reverse lookup: normalized URL -> RequestKey string
        private readonly Dictionary<string, string> urlToKeyMap
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Callbacks waiting for an in-flight download (key -> list of waiting Sprite/byte requests)
        private readonly Dictionary<string, List<Action<ImageRequestRecord>>> pendingDownloadCallbacks
            = new Dictionary<string, List<Action<ImageRequestRecord>>>(StringComparer.Ordinal);

        // Callbacks waiting for a custom size generation (key_sizePx -> list of callbacks)
        private readonly Dictionary<string, List<Action<bool, string>>> pendingCustomSizeCallbacks
            = new Dictionary<string, List<Action<bool, string>>>(StringComparer.Ordinal);

        // Tracks currently visible viewport keys so scrolling out of view automatically demotes priority
        private readonly HashSet<string> currentViewportKeys = new HashSet<string>(StringComparer.Ordinal);

        private string volatileCachePath;
        private string permanentRomPath;
        private bool isInitialized;

        // =====================================================================
        // Public Events & Properties
        // =====================================================================

        /// <summary>
        /// Singleton accessor. Automatically creates and initializes the pipeline if not yet present in the scene.
        /// </summary>
        public static NativeImagePipeline Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    instance = FindFirstObjectByType<NativeImagePipeline>();
#else
                    instance = FindObjectOfType<NativeImagePipeline>();
#endif
                    if (instance == null)
                    {
                        var go = new GameObject("[NativeImagePipeline]");
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<NativeImagePipeline>();
                    }
                    instance.EnsureInitialized();
                }
                return instance;
            }
        }

        /// <summary>
        /// Fired when internet connectivity changes (True = Online & downloading, False = Offline & paused).
        /// </summary>
        public event Action<bool> OnConnectivityChanged;

        /// <summary>
        /// Fired whenever any puzzle image request string updates its status (Completed, Failed, PromotedToROM, etc.).
        /// </summary>
        public event Action<ImageRequestRecord> OnRequestUpdated;

        /// <summary>
        /// Fired when a remote JSON manifest endpoint finishes parsing and queueing all puzzle URLs.
        /// </summary>
        public event Action<IReadOnlyList<ImageRequestRecord>> OnManifestLoaded;

        /// <summary>
        /// Current internet connectivity state reported by the native bridge.
        /// </summary>
        public bool IsOnline => nativeBridge != null && nativeBridge.IsOnline;

        /// <summary>
        /// Read-only dictionary of all tracked image request strings and their live status/sizes/duration.
        /// </summary>
        public IReadOnlyDictionary<string, ImageRequestRecord> AllRequests => requestRegistry;

        /// <summary>
        /// OS directory path used for Volatile Cache storage.
        /// </summary>
        public string VolatileCachePath => volatileCachePath;
        public string CacheDirectoryPath => volatileCachePath;

        /// <summary>
        /// OS directory path used for Permanent ROM storage.
        /// </summary>
        public string PermanentRomPath => permanentRomPath;
        public string RomDirectoryPath => permanentRomPath;

        private float smoothedSpeedMbps;
        private float peakSpeedMbps;
        private float lastBandwidthSampleTime;

        /// <summary>
        /// Real-time download/transcode throughput in Mbps (decays smoothly to 0 when idle).
        /// </summary>
        public float CurrentSpeedMbps
        {
            get
            {
                if (!IsOnline) return 0f;
                float age = Time.realtimeSinceStartup - lastBandwidthSampleTime;
                if (age > 4.5f) return 0f;
                return smoothedSpeedMbps * Mathf.Clamp01(1f - (age / 5.5f));
            }
        }

        /// <summary>
        /// Peak download/transcode throughput observed during this session in Mbps.
        /// </summary>
        public float PeakSpeedMbps => peakSpeedMbps;

        // =====================================================================
        // Unity Lifecycle & Initialization
        // =====================================================================

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialized();
        }

        /// <summary>
        /// Initializes the pipeline with an optional custom config or remote JSON manifest API endpoint.
        /// If manifestApiEndpoint is provided, automatically fetches the JSON list and queues all 100+ puzzle images.
        /// </summary>
        public void Initialize(string manifestApiEndpoint = null, ImagePipelineConfig customConfig = null)
        {
            if (customConfig != null)
            {
                config = customConfig;
            }

            EnsureInitialized();

            string endpointToFetch = !string.IsNullOrWhiteSpace(manifestApiEndpoint)
                ? manifestApiEndpoint
                : config.manifestApiEndpoint;

            if (!string.IsNullOrWhiteSpace(endpointToFetch))
            {
                StartCoroutine(FetchAndEnqueueManifestCoroutine(endpointToFetch));
            }
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            if (config == null)
            {
                config = ImagePipelineConfig.CreateDefault();
            }

            volatileCachePath = Path.Combine(Application.temporaryCachePath, "NativeImagePipeline_Cache");
            permanentRomPath = Path.Combine(Application.persistentDataPath, "NativeImagePipeline_ROM");

            spriteCache = new SpriteMemoryCache(config.maxRamSpritePoolCount);
            nativeBridge = CreatePlatformBridge();

            nativeBridge.OnConnectivityChanged += HandleBridgeConnectivityChanged;
            nativeBridge.OnDownloadCompleted += HandleBridgeDownloadCompleted;
            nativeBridge.OnCustomSizeCompleted += HandleBridgeCustomSizeCompleted;

            nativeBridge.Initialize(volatileCachePath, permanentRomPath, config);

            if (config.autoStartOnConnectivity && !string.IsNullOrWhiteSpace(config.manifestApiEndpoint))
            {
                StartCoroutine(FetchAndEnqueueManifestCoroutine(config.manifestApiEndpoint));
            }
        }

        private static INativeImageBridge CreatePlatformBridge()
        {
#if UNITY_EDITOR
            return new EditorImageBridge();
#elif UNITY_ANDROID
            return new AndroidImageBridge();
#elif UNITY_IOS
            return new IOSImageBridge();
#elif UNITY_WEBGL
            return new WebGLImageBridge();
#else
            return new EditorImageBridge();
#endif
        }

        // =====================================================================
        // 1. Enqueue & Manifest Loading (Single & Batch 100+ URLs)
        // =====================================================================

        /// <summary>
        /// Converts a puzzle image URL into a tracked RequestKey string and enqueues it for background download.
        /// Upon download, the native bridge automatically generates both 256px (Icon) and 1080px (FullSize) JPEGs.
        /// Returns the deterministic RequestKey string used to track or fetch the Sprite.
        /// </summary>
        public string EnqueueUrl(
            string imageUrl,
            RequestPriority priority = RequestPriority.Low,
            StorageTier storageTier = StorageTier.VolatileCache,
            string customRequestKey = null)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(imageUrl))
                return string.Empty;

            string cleanUrl = imageUrl.Trim();
            string requestKey = !string.IsNullOrWhiteSpace(customRequestKey)
                ? customRequestKey.Trim()
                : GetOrCreateKeyForUrl(cleanUrl);

            urlToKeyMap[cleanUrl] = requestKey;

            if (!requestRegistry.TryGetValue(requestKey, out var record))
            {
                record = new ImageRequestRecord(requestKey, cleanUrl, priority, storageTier);
                requestRegistry[requestKey] = record;

                // Check if both 256px and 1080px already exist on disk (ROM or Cache) from a previous session
                if (nativeBridge.HasCachedFile(requestKey, config.defaultIconSizePx, out StorageTier foundTier, out long iconBytes) &&
                    nativeBridge.HasCachedFile(requestKey, config.defaultFullSizePx, out _, out long fullBytes))
                {
                    record.MarkCompletedDefaultTiers(iconBytes, fullBytes, 0L, foundTier);
                    OnRequestUpdated?.Invoke(record);
                    return requestKey;
                }
            }
            else if (record.IsReady)
            {
                return requestKey;
            }
            else if (priority > record.Priority)
            {
                record.SetPriority(priority);
                nativeBridge.UpdatePriority(requestKey, priority);
                return requestKey;
            }

            record.MarkDownloading();
            nativeBridge.EnqueueRequest(requestKey, cleanUrl, priority, storageTier);
            OnRequestUpdated?.Invoke(record);
            return requestKey;
        }

        /// <summary>
        /// Enqueues a batch of 100+ puzzle image URLs for background downloading and returns their RequestKey strings.
        /// </summary>
        public List<string> EnqueueBatch(
            IEnumerable<string> imageUrls,
            RequestPriority priority = RequestPriority.Low,
            StorageTier storageTier = StorageTier.VolatileCache)
        {
            EnsureInitialized();
            var keys = new List<string>();
            if (imageUrls == null)
                return keys;

            foreach (string url in imageUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    string key = EnqueueUrl(url, priority, storageTier);
                    if (!string.IsNullOrEmpty(key))
                    {
                        keys.Add(key);
                    }
                }
            }
            return keys;
        }

        private IEnumerator FetchAndEnqueueManifestCoroutine(string endpointUrl)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(endpointUrl))
            {
                req.timeout = 15;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[NativeImagePipeline] Failed fetching manifest from '{endpointUrl}': {req.error}");
                    yield break;
                }

                string json = req.downloadHandler.text;
                EnqueueFromManifestJson(json);
            }
        }

        /// <summary>
        /// Parses a JSON manifest string (containing 100+ puzzle image URLs) and enqueues all entries automatically.
        /// </summary>
        public List<ImageRequestRecord> EnqueueFromManifestJson(string manifestJson)
        {
            EnsureInitialized();
            var addedRecords = new List<ImageRequestRecord>();
            if (string.IsNullOrWhiteSpace(manifestJson))
                return addedRecords;

            try
            {
                // Support raw JSON array `[ "https://...", ... ]` by wrapping if needed
                string normalizedJson = manifestJson.Trim();
                if (normalizedJson.StartsWith("["))
                {
                    normalizedJson = "{\"urls\":" + normalizedJson + "}";
                }

                var manifest = JsonUtility.FromJson<PuzzleManifestResponse>(normalizedJson);
                if (manifest == null)
                    return addedRecords;

                var entries = manifest.GetNormalizedEntries();
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    StorageTier targetTier = entry.saveToRomOnDownload
                        ? StorageTier.PermanentROM
                        : StorageTier.VolatileCache;

                    string key = EnqueueUrl(entry.url, RequestPriority.Low, targetTier, entry.id);
                    if (requestRegistry.TryGetValue(key, out var rec))
                    {
                        addedRecords.Add(rec);
                    }
                }

                OnManifestLoaded?.Invoke(addedRecords);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NativeImagePipeline] Manifest JSON parse error: {ex.Message}");
            }

            return addedRecords;
        }

        // =====================================================================
        // 2. Viewport Priority Queue & Faulty Request Management
        // =====================================================================

        /// <summary>
        /// Prioritizes the puzzle images currently visible in the user's ScrollView viewport (`High` priority)
        /// and automatically demotes previously visible off-screen items back to `Low` priority.
        /// Pass either RequestKey strings or raw URLs.
        /// </summary>
        public void PrioritizeViewportItems(IEnumerable<string> visibleKeysOrUrls)
        {
            EnsureInitialized();
            var nextVisibleSet = new HashSet<string>(StringComparer.Ordinal);

            if (visibleKeysOrUrls != null)
            {
                foreach (string keyOrUrl in visibleKeysOrUrls)
                {
                    string key = ResolveRequestKey(keyOrUrl);
                    if (!string.IsNullOrEmpty(key))
                    {
                        nextVisibleSet.Add(key);
                    }
                }
            }

            // Demote items that scrolled out of the viewport
            foreach (string oldKey in currentViewportKeys)
            {
                if (!nextVisibleSet.Contains(oldKey) && requestRegistry.TryGetValue(oldKey, out var oldRec))
                {
                    if (oldRec.IsInFlight && oldRec.Priority == RequestPriority.High)
                    {
                        oldRec.SetPriority(RequestPriority.Low);
                        nativeBridge.UpdatePriority(oldKey, RequestPriority.Low);
                    }
                }
            }

            // Promote newly visible items to High priority
            foreach (string visibleKey in nextVisibleSet)
            {
                if (requestRegistry.TryGetValue(visibleKey, out var rec) && rec.IsInFlight)
                {
                    if (rec.Priority < RequestPriority.High)
                    {
                        rec.SetPriority(RequestPriority.High);
                        nativeBridge.UpdatePriority(visibleKey, RequestPriority.High);
                    }
                }
            }

            currentViewportKeys.Clear();
            foreach (string k in nextVisibleSet)
            {
                currentViewportKeys.Add(k);
            }
        }

        /// <summary>
        /// Sets the priority of a single request string (e.g., setting Critical priority when a player clicks a puzzle).
        /// </summary>
        public void SetRequestPriority(string keyOrUrl, RequestPriority priority)
        {
            EnsureInitialized();
            string key = ResolveRequestKey(keyOrUrl);
            if (requestRegistry.TryGetValue(key, out var rec) && rec.IsInFlight)
            {
                rec.SetPriority(priority);
                nativeBridge.UpdatePriority(key, priority);
            }
        }

        /// <summary>
        /// Cancels and removes a specific request string from the queue and deletes its cached files.
        /// </summary>
        public void RemoveRequest(string keyOrUrl)
        {
            EnsureInitialized();
            string key = ResolveRequestKey(keyOrUrl);
            if (string.IsNullOrEmpty(key))
                return;

            nativeBridge.CancelAndRemoveRequest(key);
            spriteCache.EvictAllResolutionsForKey(key);

            if (requestRegistry.TryGetValue(key, out var rec))
            {
                rec.MarkCancelled();
                requestRegistry.Remove(key);
            }
        }

        /// <summary>
        /// Purges all failed/faulty request strings from the tracker and priority queue.
        /// Returns the number of faulty request strings removed.
        /// </summary>
        public int PurgeFaultyRequests()
        {
            EnsureInitialized();
            var faultyKeys = new List<string>();
            foreach (var kvp in requestRegistry)
            {
                if (kvp.Value.State == RequestState.Failed)
                {
                    faultyKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < faultyKeys.Count; i++)
            {
                RemoveRequest(faultyKeys[i]);
            }

            return faultyKeys.Count;
        }

        // =====================================================================
        // 3. Helper Functions: Fetch Sprite, Custom Sizes, & Cache <-> ROM
        // =====================================================================

        /// <summary>
        /// Fetches a ready-to-render Unity Sprite by its RequestKey string (or URL) and desired resolution tier
        /// (Icon256 = 256px, Full1080 = 1080px).
        /// - Checks Unity RAM Sprite pool first.
        /// - Checks Permanent ROM second, then Volatile Cache third.
        /// - If still downloading in the queue, automatically boosts priority and invokes onLoaded when ready.
        /// </summary>
        public void GetSprite(
            string keyOrUrl,
            ImageResolutionTier tier,
            Action<Sprite> onLoaded,
            Action<string> onError = null)
        {
            int sizePx;
            switch (tier)
            {
                case ImageResolutionTier.Icon256:
                    sizePx = config.defaultIconSizePx;
                    break;
                case ImageResolutionTier.Tile512:
                    sizePx = 512;
                    break;
                case ImageResolutionTier.Ultra2048:
                    sizePx = 2048;
                    break;
                case ImageResolutionTier.Full1080:
                default:
                    sizePx = config.defaultFullSizePx;
                    break;
            }

            GetSpriteByCustomSize(keyOrUrl, sizePx, onLoaded, onError);
        }

        /// <summary>
        /// Helper method: Downloads/resizes and returns a Unity Sprite at ANY requested pixel size
        /// (e.g., 256 for small icon, 512 for square tile, 1080 for full-screen puzzle).
        /// If the 1080px file is already on disk, downscales locally without re-downloading!
        /// </summary>
        public void GetSpriteByCustomSize(
            string keyOrUrl,
            int sizePx,
            Action<Sprite> onLoaded,
            Action<string> onError = null,
            RequestPriority priority = RequestPriority.High)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(keyOrUrl) || sizePx <= 0)
            {
                onError?.Invoke("Invalid keyOrUrl or sizePx.");
                return;
            }

            string requestKey = ResolveOrEnqueueKey(keyOrUrl, priority);

            // 1. Check Unity RAM Sprite Pool first
            if (spriteCache.TryAcquireSprite(requestKey, sizePx, out Sprite cachedSprite))
            {
                onLoaded?.Invoke(cachedSprite);
                return;
            }

            // 2. Check if this exact resolution (256, 1080, or custom e.g. 512) is already on disk (ROM or Cache)
            if (nativeBridge.HasCachedFile(requestKey, sizePx, out _, out _))
            {
                LoadSpriteFromDiskBytes(requestKey, sizePx, onLoaded, onError);
                return;
            }

            // 3. If it's a standard tier (256 or 1080), wait for the main download to finish
            bool isStandardTier = (sizePx == config.defaultIconSizePx || sizePx == config.defaultFullSizePx);
            if (isStandardTier)
            {
                SetRequestPriority(requestKey, priority);
                RegisterPendingDownloadCallback(requestKey, record =>
                {
                    if (record.IsReady)
                    {
                        LoadSpriteFromDiskBytes(requestKey, sizePx, onLoaded, onError);
                    }
                    else
                    {
                        onError?.Invoke(record.ErrorMessage ?? "Download failed");
                    }
                });
                return;
            }

            // 4. Custom size (e.g. 512px): if 1080px is already ready on disk, ask native bridge to downscale immediately;
            // otherwise wait for 1080px download to finish first, then generate the custom size.
            if (requestRegistry.TryGetValue(requestKey, out var rec) && rec.IsReady)
            {
                TriggerNativeCustomSizeGeneration(requestKey, rec.SourceUrl, sizePx, priority, onLoaded, onError);
            }
            else
            {
                SetRequestPriority(requestKey, priority);
                RegisterPendingDownloadCallback(requestKey, completedRec =>
                {
                    if (completedRec.IsReady)
                    {
                        TriggerNativeCustomSizeGeneration(requestKey, completedRec.SourceUrl, sizePx, priority, onLoaded, onError);
                    }
                    else
                    {
                        onError?.Invoke(completedRec.ErrorMessage ?? "Base image download failed");
                    }
                });
            }
        }

        /// <summary>
        /// Helper method to explicitly download a file by passing its endpoint URL, target size (e.g. 256, 512, 1080),
        /// priority, and target storage tier (VolatileCache or PermanentROM).
        /// </summary>
        public string DownloadFileWithSize(
            string fileEndpointUrl,
            int requestedSizePx,
            RequestPriority priority = RequestPriority.Normal,
            StorageTier storageTier = StorageTier.VolatileCache,
            Action<Sprite> onSpriteReady = null,
            Action<string> onError = null)
        {
            string key = EnqueueUrl(fileEndpointUrl, priority, storageTier);
            if (onSpriteReady != null || (requestedSizePx != config.defaultIconSizePx && requestedSizePx != config.defaultFullSizePx))
            {
                GetSpriteByCustomSize(key, requestedSizePx, onSpriteReady, onError, priority);
            }
            return key;
        }

        /// <summary>
        /// Releases a Sprite reference when a UI element is recycled or hidden, keeping Unity Texture memory bounded.
        /// The JPEG remains safely on disk (Cache or ROM) for instant reload later.
        /// </summary>
        public void ReleaseSprite(string keyOrUrl, int sizePx, bool destroyImmediatelyIfZero = false)
        {
            if (!isInitialized || string.IsNullOrWhiteSpace(keyOrUrl))
                return;

            string key = ResolveRequestKey(keyOrUrl);
            spriteCache.ReleaseSprite(key, sizePx, destroyImmediatelyIfZero);
        }

        /// <summary>
        /// Moves all generated JPEG files (256px, 1080px, custom sizes) for this request string
        /// permanently into hard-drive ROM storage and deletes them from Volatile Cache.
        /// Subsequent fetches automatically read from ROM.
        /// </summary>
        public bool SaveToPermanentROM(string keyOrUrl)
        {
            EnsureInitialized();
            string key = ResolveRequestKey(keyOrUrl);
            if (string.IsNullOrEmpty(key))
                return false;

            bool moved = nativeBridge.PromoteToROM(key);
            if (moved && requestRegistry.TryGetValue(key, out var rec))
            {
                rec.MarkPromotedToROM();
                OnRequestUpdated?.Invoke(rec);
            }
            return moved;
        }

        /// <summary>
        /// Moves files from Permanent ROM back to Volatile Cache (e.g., after the player finishes a puzzle).
        /// </summary>
        public bool MoveFromRomToCache(string keyOrUrl)
        {
            EnsureInitialized();
            string key = ResolveRequestKey(keyOrUrl);
            if (string.IsNullOrEmpty(key))
                return false;

            bool moved = nativeBridge.DemoteToCache(key);
            if (moved && requestRegistry.TryGetValue(key, out var rec))
            {
                rec.MarkDemotedToCache();
                OnRequestUpdated?.Invoke(rec);
            }
            return moved;
        }

        /// <summary>
        /// Returns the live tracking record for a request string or URL (State, IconSizeBytes, FullSizeBytes, DurationMs, Error).
        /// </summary>
        public ImageRequestRecord GetRequestStatus(string keyOrUrl)
        {
            EnsureInitialized();
            string key = ResolveRequestKey(keyOrUrl);
            return !string.IsNullOrEmpty(key) && requestRegistry.TryGetValue(key, out var rec) ? rec : null;
        }

        /// <summary>
        /// Clears Volatile Cache on disk while preserving all Permanent ROM files.
        /// </summary>
        public void ClearVolatileCache()
        {
            EnsureInitialized();
            nativeBridge.ClearVolatileCache();
            spriteCache.TrimUnreferenced();
        }

        /// <summary>
        /// Opens the physical OS folder (Windows Explorer / macOS Finder) highlighting the cached JPEG file,
        /// or in WebGL builds opens the interactive IndexedDB Folder Explorer modal directly in the browser.
        /// </summary>
        public void RevealInFileExplorer(string keyOrUrl, int sizePx = 1080)
        {
            EnsureInitialized();
            string key = ResolveRequestKey(keyOrUrl);
            if (string.IsNullOrEmpty(key)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLImageBridge.OpenBrowserFolderModal(key, sizePx);
#else
            var rec = GetRequestStatus(key);
            bool isRom = rec != null && rec.StorageTier == StorageTier.PermanentROM;
            string dirPath = isRom ? permanentRomPath : volatileCachePath;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string filePath = DiskStorageHelper.GetFilePath(dirPath, key, sizePx);
            if (!File.Exists(filePath))
            {
                // Fallback to 1080px or 256px if the selected tier hasn't been generated yet
                string fullPath = DiskStorageHelper.GetFilePath(dirPath, key, 1080);
                string iconPath = DiskStorageHelper.GetFilePath(dirPath, key, 256);
                if (File.Exists(fullPath)) filePath = fullPath;
                else if (File.Exists(iconPath)) filePath = iconPath;
            }

            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    if (File.Exists(filePath))
                    {
                        string winFile = filePath.Replace('/', '\\');
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{winFile}\"");
                    }
                    else
                    {
                        string winDir = dirPath.Replace('/', '\\');
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{winDir}\"");
                    }
                }
                else
                {
                    Application.OpenURL("file://" + dirPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NativeImagePipeline] Could not open folder '{dirPath}': {ex.Message}");
            }
#endif
        }

        private void RecordBandwidthSample(long transcodedBytes, long durationMs)
        {
            if (transcodedBytes <= 0) return;
            // Source 2080x2080 image stream + transcoded JPEG output payload
            float estimatedWireBytes = transcodedBytes * 2.2f;
            float seconds = Mathf.Max(0.06f, durationMs > 0 ? (durationMs / 1000f) : 0.12f);
            float sampleMbps = ((estimatedWireBytes * 8f) / (1024f * 1024f)) / seconds;
            sampleMbps = Mathf.Clamp(sampleMbps, 0.4f, 250f);

            smoothedSpeedMbps = smoothedSpeedMbps <= 0.01f
                ? sampleMbps
                : Mathf.Lerp(smoothedSpeedMbps, sampleMbps, 0.35f);

            if (smoothedSpeedMbps > peakSpeedMbps)
            {
                peakSpeedMbps = smoothedSpeedMbps;
            }
            lastBandwidthSampleTime = Time.realtimeSinceStartup;
        }

        // =====================================================================
        // Internal Bridge Event Handlers & Helpers
        // =====================================================================

        private void LoadSpriteFromDiskBytes(
            string requestKey,
            int sizePx,
            Action<Sprite> onLoaded,
            Action<string> onError)
        {
            nativeBridge.ReadImageBytesAsync(
                requestKey,
                sizePx,
                bytes =>
                {
                    Sprite sprite = spriteCache.CreateAndCacheSprite(requestKey, sizePx, bytes);
                    if (sprite != null)
                    {
                        onLoaded?.Invoke(sprite);
                    }
                    else
                    {
                        onError?.Invoke($"Failed decoding JPEG bytes into Texture2D for '{requestKey}' ({sizePx}px)");
                    }
                },
                err => onError?.Invoke(err));
        }

        private void TriggerNativeCustomSizeGeneration(
            string requestKey,
            string fallbackUrl,
            int sizePx,
            RequestPriority priority,
            Action<Sprite> onLoaded,
            Action<string> onError)
        {
            string customWaitKey = $"{requestKey}_{sizePx}";
            if (!pendingCustomSizeCallbacks.TryGetValue(customWaitKey, out var waitList))
            {
                waitList = new List<Action<bool, string>>();
                pendingCustomSizeCallbacks[customWaitKey] = waitList;
                nativeBridge.RequestCustomSize(requestKey, fallbackUrl, sizePx, priority);
            }

            waitList.Add((success, errorMsg) =>
            {
                if (success)
                {
                    LoadSpriteFromDiskBytes(requestKey, sizePx, onLoaded, onError);
                }
                else
                {
                    onError?.Invoke(errorMsg);
                }
            });
        }

        private void RegisterPendingDownloadCallback(string requestKey, Action<ImageRequestRecord> callback)
        {
            if (!pendingDownloadCallbacks.TryGetValue(requestKey, out var list))
            {
                list = new List<Action<ImageRequestRecord>>();
                pendingDownloadCallbacks[requestKey] = list;
            }
            list.Add(callback);
        }

        private void HandleBridgeConnectivityChanged(bool online)
        {
            OnConnectivityChanged?.Invoke(online);
        }

        private void HandleBridgeDownloadCompleted(NativeDownloadResult result)
        {
            if (string.IsNullOrWhiteSpace(result.requestKey))
                return;

            if (!requestRegistry.TryGetValue(result.requestKey, out var record))
                return;

            if (result.success)
            {
                StorageTier tier = result.storageTier == 1 ? StorageTier.PermanentROM : StorageTier.VolatileCache;
                record.MarkCompletedDefaultTiers(result.iconSizeBytes, result.fullSizeBytes, result.durationMs, tier);
                RecordBandwidthSample(result.iconSizeBytes + result.fullSizeBytes, result.durationMs);
            }
            else
            {
                record.MarkFailed(result.errorMessage);
                if (config.autoRemoveFaultyRequests && record.RetryCount >= config.maxRetryCount)
                {
                    nativeBridge.CancelAndRemoveRequest(result.requestKey);
                }
            }

            OnRequestUpdated?.Invoke(record);

            if (pendingDownloadCallbacks.TryGetValue(result.requestKey, out var callbacks))
            {
                pendingDownloadCallbacks.Remove(result.requestKey);
                for (int i = 0; i < callbacks.Count; i++)
                {
                    try { callbacks[i]?.Invoke(record); }
                    catch (Exception ex) { Debug.LogException(ex); }
                }
            }
        }

        private void HandleBridgeCustomSizeCompleted(NativeCustomSizeResult result)
        {
            if (string.IsNullOrWhiteSpace(result.requestKey))
                return;

            if (result.success && requestRegistry.TryGetValue(result.requestKey, out var record))
            {
                record.RecordCustomResolution(result.sizePx, result.sizeBytes, result.durationMs);
                RecordBandwidthSample(result.sizeBytes, result.durationMs);
                OnRequestUpdated?.Invoke(record);
            }

            string customWaitKey = $"{result.requestKey}_{result.sizePx}";
            if (pendingCustomSizeCallbacks.TryGetValue(customWaitKey, out var callbacks))
            {
                pendingCustomSizeCallbacks.Remove(customWaitKey);
                for (int i = 0; i < callbacks.Count; i++)
                {
                    try { callbacks[i]?.Invoke(result.success, result.errorMessage); }
                    catch (Exception ex) { Debug.LogException(ex); }
                }
            }
        }

        private string GetOrCreateKeyForUrl(string cleanUrl)
        {
            if (urlToKeyMap.TryGetValue(cleanUrl, out string existingKey))
                return existingKey;

            string generated = RequestKeyGenerator.FromUrl(cleanUrl);
            urlToKeyMap[cleanUrl] = generated;
            return generated;
        }

        private string ResolveRequestKey(string keyOrUrl)
        {
            if (string.IsNullOrWhiteSpace(keyOrUrl))
                return string.Empty;

            string trimmed = keyOrUrl.Trim();
            if (requestRegistry.ContainsKey(trimmed))
                return trimmed;

            if (urlToKeyMap.TryGetValue(trimmed, out string mappedKey))
                return mappedKey;

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return RequestKeyGenerator.FromUrl(trimmed);
            }

            return trimmed;
        }

        private string ResolveOrEnqueueKey(string keyOrUrl, RequestPriority priority)
        {
            string trimmed = keyOrUrl.Trim();
            if (requestRegistry.ContainsKey(trimmed))
                return trimmed;

            if (urlToKeyMap.TryGetValue(trimmed, out string mappedKey) && requestRegistry.ContainsKey(mappedKey))
                return mappedKey;

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return EnqueueUrl(trimmed, priority);
            }

            return trimmed;
        }

        private void OnDestroy()
        {
            if (nativeBridge != null)
            {
                nativeBridge.OnConnectivityChanged -= HandleBridgeConnectivityChanged;
                nativeBridge.OnDownloadCompleted -= HandleBridgeDownloadCompleted;
                nativeBridge.OnCustomSizeCompleted -= HandleBridgeCustomSizeCompleted;
                nativeBridge.Dispose();
                nativeBridge = null;
            }

            spriteCache?.Dispose();
        }
    }
}

using System;
using System.Collections;
using System.IO;
using ThisIsBlast.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace ThisIsBlast.Core
{
    /// <summary>
    /// OmniPace LiveOps Runtime Manager:
    /// Bridges Unity gameplay with the OmniPace backend server.
    /// Manages A/B test Progression Archetypes (Linear vs Sawtooth),
    /// dynamic phase routing, and real-time DDA cohorts (Silent Pity & Win-Streak Challenge).
    /// Fully resilient with zero-latency local fallback if server is offline.
    /// </summary>
    public class LiveOpsManager : MonoBehaviour
    {
        public static LiveOpsManager Instance { get; private set; }

        public const string PREF_JOURNEY_KEY = "OmniPace_ActiveJourney";
        public const string PREF_COHORT_KEY = "OmniPace_ActiveCohort";
        public const string PREF_FAILS_KEY = "OmniPace_ConsecutiveFails";
        public const string PREF_WINS_KEY = "OmniPace_ConsecutiveWins";

        public const string PREF_SERVER_URL_KEY = "OmniPace_ServerUrl";

        [Header("Backend Connection")]
        [SerializeField] private string _serverUrl = "http://localhost:8080";
        [SerializeField] private float _requestTimeoutSec = 2.0f;

        [Header("Active LiveOps State")]
        public string ActiveJourney { get; private set; } = "journey_linear";
        public string ActiveCohort { get; private set; } = "cohort_core";
        public string ActiveArchetype { get; private set; } = "arc_linear";
        public string ActivePhaseName { get; private set; } = "Gentle Linear Ramp";
        public string ActiveBatch { get; private set; } = "b01";
        public string ActiveSeed { get; private set; } = "clb";
        public string ActiveSeedReason { get; private set; } = "Cohort Baseline";
        public bool IsPityActive { get; private set; } = false;
        public bool IsChallengeActive { get; private set; } = false;
        public int ConsecutiveFails { get; private set; } = 0;
        public int ConsecutiveWins { get; private set; } = 0;
        public bool IsConnectedToBackend { get; private set; } = false;

        public event Action OnLiveOpsStateChanged;

        [Serializable]
        public class ProgressionResponse
        {
            public bool success;
            public string game;
            public string vault;
            public int stage;
            public string journeyId;
            public string cohortId;
            public string phaseName;
            public string archetype;
            public string batch;
            public int levelId;
            public string resolvedSeed;
            public string seedReason;
            public bool isPityActive;
            public bool isChallengeActive;
            public LevelData levelData;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSavedState();
        }

        private void LoadSavedState()
        {
            string customServer = PlayerPrefs.GetString(PREF_SERVER_URL_KEY, "");
            if (!string.IsNullOrEmpty(customServer))
            {
                _serverUrl = customServer;
            }

            ActiveJourney = PlayerPrefs.GetString(PREF_JOURNEY_KEY, "journey_linear");
            ActiveCohort = PlayerPrefs.GetString(PREF_COHORT_KEY, "cohort_core");
            ConsecutiveFails = PlayerPrefs.GetInt(PREF_FAILS_KEY, 0);
            ConsecutiveWins = PlayerPrefs.GetInt(PREF_WINS_KEY, 0);

            UpdateArchetypeFromJourney();
        }

        private void UpdateArchetypeFromJourney()
        {
            if (ActiveJourney == "journey_sawtooth")
            {
                ActiveArchetype = "arc_sawtooth";
                ActivePhaseName = "Sawtooth Chapter Climaxes";
            }
            else if (ActiveJourney == "journey_royal_rush")
            {
                ActiveArchetype = "arc_royalRush";
                ActivePhaseName = "Royal Rush Cascade Pace";
            }
            else if (ActiveJourney == "journey_hybrid")
            {
                ActiveArchetype = "arc_sawtooth";
                ActivePhaseName = "Hybrid Progression";
            }
            else
            {
                ActiveArchetype = "arc_linear";
                ActivePhaseName = "Gentle Linear Ramp";
            }
        }

        public void RecordVictory()
        {
            ConsecutiveWins++;
            ConsecutiveFails = 0;
            PlayerPrefs.SetInt(PREF_WINS_KEY, ConsecutiveWins);
            PlayerPrefs.SetInt(PREF_FAILS_KEY, ConsecutiveFails);
            PlayerPrefs.Save();

            Debug.Log($"<color=green>[LiveOps] Victory recorded! Win Streak: {ConsecutiveWins}</color>");
            OnLiveOpsStateChanged?.Invoke();
        }

        public void RecordDefeat()
        {
            ConsecutiveFails++;
            ConsecutiveWins = 0;
            PlayerPrefs.SetInt(PREF_FAILS_KEY, ConsecutiveFails);
            PlayerPrefs.SetInt(PREF_WINS_KEY, ConsecutiveWins);
            PlayerPrefs.Save();

            Debug.Log($"<color=orange>[LiveOps] Defeat recorded! Consecutive Fails: {ConsecutiveFails}</color>");
            OnLiveOpsStateChanged?.Invoke();
        }

        /// <summary>
        /// Instantly switches the player's progression journey between Linear and Sawtooth archetypes.
        /// </summary>
        public void SwitchJourney(string journeyId)
        {
            ActiveJourney = journeyId;
            PlayerPrefs.SetString(PREF_JOURNEY_KEY, ActiveJourney);
            PlayerPrefs.Save();

            UpdateArchetypeFromJourney();
            Debug.Log($"<color=cyan>[LiveOps] Switched Journey to: {ActiveJourney} (Archetype: {ActiveArchetype})</color>");

            // Update RemoteConfig progression track for sync
            if (RemoteConfigService.Instance != null)
            {
                string trackName = (ActiveArchetype == "arc_sawtooth") ? "track_sawtooth_b" : "track_linear_a";
                RemoteConfigService.Instance.SetConfigOverride("progression_track", trackName);
            }

            OnLiveOpsStateChanged?.Invoke();

            // Reload current level with the newly active archetype!
            GameManager.Instance?.RestartLevel();
        }

        public void SetServerUrl(string url)
        {
            _serverUrl = url;
            PlayerPrefs.SetString(PREF_SERVER_URL_KEY, url);
            PlayerPrefs.Save();
            Debug.Log($"<color=cyan>[LiveOps] Server URL updated to: {_serverUrl}</color>");
        }

        public void ToggleArchetype()
        {
            if (ActiveArchetype == "arc_linear")
            {
                SwitchJourney("journey_sawtooth");
            }
            else if (ActiveArchetype == "arc_sawtooth")
            {
                SwitchJourney("journey_royal_rush");
            }
            else
            {
                SwitchJourney("journey_linear");
            }
        }

        /// <summary>
        /// Fetches the resolved level data for the given stage from the OmniPace backend server.
        /// Seamlessly falls back to local cached files or bundled resources if offline.
        /// </summary>
        public IEnumerator FetchProgressionLevelRoutine(int stage, Action<LevelData> onComplete)
        {
            string url = $"{_serverUrl}/api/game/progression?stage={stage}&fails={ConsecutiveFails}&wins={ConsecutiveWins}&journey={ActiveJourney}&cohort={ActiveCohort}";
            Debug.Log($"<color=cyan>[LiveOps] Querying OmniPace: {url}</color>");

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = Mathf.RoundToInt(_requestTimeoutSec);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string json = req.downloadHandler.text;
                    ProgressionResponse resp = null;
                    try
                    {
                        resp = JsonUtility.FromJson<ProgressionResponse>(json);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LiveOps] Failed to parse backend progression json: {ex.Message}");
                    }

                    if (resp != null && resp.success && resp.levelData != null)
                    {
                        IsConnectedToBackend = true;
                        ActivePhaseName = resp.phaseName;
                        ActiveArchetype = resp.archetype;
                        ActiveBatch = resp.batch;
                        ActiveSeed = resp.resolvedSeed;
                        ActiveSeedReason = resp.seedReason;
                        IsPityActive = resp.isPityActive;
                        IsChallengeActive = resp.isChallengeActive;

                        LevelDecoder.SanitizeLevelArsenal(resp.levelData);

                        Debug.Log($"<color=green>[LiveOps] OmniPace Progression Success! Phase: '{ActivePhaseName}', Arch: '{ActiveArchetype}', Seed: '{ActiveSeed}' ({ActiveSeedReason})</color>");
                        OnLiveOpsStateChanged?.Invoke();

                        onComplete?.Invoke(resp.levelData);
                        yield break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LiveOps] OmniPace backend unreachable ({req.error}). Falling back to local offline vault.");
                }
            }

            // --- OFFLINE FALLBACK ---
            IsConnectedToBackend = false;
            UpdateArchetypeFromJourney();

            // Local DDA rule fallback
            if (ConsecutiveFails >= 2)
            {
                ActiveSeed = "frg";
                ActiveSeedReason = $"Silent Pity (Offline, {ConsecutiveFails} losses)";
                IsPityActive = true;
                IsChallengeActive = false;
            }
            else if (ConsecutiveWins >= 5)
            {
                ActiveSeed = "tgt";
                ActiveSeedReason = $"Win-Streak Challenge (Offline, {ConsecutiveWins} wins)";
                IsPityActive = false;
                IsChallengeActive = true;
            }
            else
            {
                ActiveSeed = "clb";
                ActiveSeedReason = "Cohort Baseline (Offline)";
                IsPityActive = false;
                IsChallengeActive = false;
            }

            OnLiveOpsStateChanged?.Invoke();

            // Return null so GameManager loads via LevelDecoder (which maps using tracks_manifest.json)
            onComplete?.Invoke(null);
        }
    }
}

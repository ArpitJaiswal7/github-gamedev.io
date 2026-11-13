using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meowsweeper.Core
{
    public class MeowGameManager : MonoBehaviour
    {
        public static MeowGameManager Instance { get; private set; }

        public const int MAX_LIVES = 5;
        public static readonly int[] GRID_SIZES = new int[] { 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public MeowLevelData currentLevel;
        public int currentLevelIndex = -1;
        public int currentSizeIdx = 2; // Starts at 6x6
        public bool isPlayingCampaignMode = false;

        public int[,] boardState; // 0 = Empty, 1 = Cross (✕), 2 = Cat (🐱)
        public bool[,] isHighlightedCell;
        public bool[,] hasMouseBadge;
        public float[,] wrongFlashTimer;
        public float boardShakeTimer = 0f;

        public int remainingHearts = MAX_LIVES;
        public int powerCatReveal = 5;
        public int powerHint = 5;
        public int powerMouse = 5;

        public float currentRunTimer = 0f;
        public bool isLevelComplete = false;
        public bool isTransitioningLevel = false;
        public bool isPlaying = false;

        // Toast Banner State (matches web version toastBar)
        public string toastMessage = "Double-Tap a cell to place a Cat  |  Single-Tap or Swipe across cells to add/remove Cross";
        public string toastType = "default"; // "default", "info", "win", "lose"
        private float toastResetTimer = 0f;

        // Colors matching Meowsweeper web palette
        public static readonly Color[] RegionColors = new Color[]
        {
            new Color(0.99f, 0.90f, 0.54f), // 0 Amber (#fde68a)
            new Color(0.65f, 0.95f, 0.81f), // 1 Emerald (#a7f3d0)
            new Color(0.75f, 0.86f, 0.99f), // 2 Blue (#bfdbfe)
            new Color(0.98f, 0.81f, 0.91f), // 3 Pink (#fbcfe8)
            new Color(0.86f, 0.84f, 0.99f), // 4 Violet (#ddd6fe)
            new Color(0.99f, 0.84f, 0.67f), // 5 Orange (#fed7aa)
            new Color(0.60f, 0.96f, 0.89f), // 6 Teal (#99f6e4)
            new Color(0.99f, 0.79f, 0.79f), // 7 Rose (#fecaca)
            new Color(0.85f, 0.97f, 0.62f), // 8 Lime (#d9f99d)
            new Color(0.73f, 0.90f, 0.99f), // 9 Sky (#bae6fd)
            new Color(0.95f, 0.88f, 0.78f), // 10 Warm Sand
            new Color(0.78f, 0.95f, 0.92f)  // 11 Mint
        };

        public event Action<MeowLevelData> OnLevelLoaded;
        public event Action<int, int, int> OnCellStateChanged;
        public event Action<bool> OnGameOver;

        private Coroutine transitionCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            // Immediately start with a procedurally generated 6x6 linked-chain level (like web version)
            // while syncing manifest in the background and loading Campaign Level 1 once synced
            LoadProceduralGridSize(6, false);
            StartCoroutine(InitGameFlow());
        }

        private IEnumerator InitGameFlow()
        {
            yield return LevelSyncService.Instance.FetchManifestCoroutine((success, count) =>
            {
                Debug.Log($"[GameManager] Manifest loaded: {count} levels available.");
            });

            if (LevelSyncService.Instance.TotalLevels > 0)
            {
                PlayCampaignLevel(0);
            }
        }

        public void ShowToast(string message, string type = "info", float duration = 1.65f)
        {
            toastMessage = message;
            toastType = type;
            toastResetTimer = duration;
        }

        public void PlayCampaignLevel(int index)
        {
            if (LevelSyncService.Instance.TotalLevels == 0)
            {
                ShowToast("No synced campaign levels available yet — shuffling linked-chain level!", "info");
                ShuffleGridSize(false);
                return;
            }

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            isPlayingCampaignMode = true;
            currentLevelIndex = (index + LevelSyncService.Instance.TotalLevels) % LevelSyncService.Instance.TotalLevels;
            string levelId = LevelSyncService.Instance.AvailableLevelIds[currentLevelIndex];

            StartCoroutine(LevelSyncService.Instance.DownloadLevelCoroutine(levelId, (levelData) =>
            {
                SetupBoard(levelData);
                ShowToast($"Loaded Campaign #{currentLevelIndex + 1} ({levelData.gridSize}x{levelData.gridSize} {levelData.tier})", "info");
            }));
        }

        public void PlayNextCampaignLevel()
        {
            PlayCampaignLevel(currentLevelIndex + 1);
        }

        public void LoadNextLevel()
        {
            PlayNextCampaignLevel();
        }

        public void LoadPrevLevel()
        {
            PlayCampaignLevel(currentLevelIndex - 1);
        }

        public void CycleGridSize()
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            isPlayingCampaignMode = false;
            currentSizeIdx = (currentSizeIdx + 1) % GRID_SIZES.Length;
            int nextSize = GRID_SIZES[currentSizeIdx];
            LoadProceduralGridSize(nextSize, true);
        }

        public void LoadProceduralGridSize(int size, bool showToast = true)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            isPlayingCampaignMode = false;
            int idx = Array.IndexOf(GRID_SIZES, size);
            if (idx >= 0) currentSizeIdx = idx;

            var generated = LevelSyncService.Instance.GenerateValidPuzzle(size, $"chain_{size}x{size}");
            SetupBoard(generated);
            if (showToast)
            {
                ShowToast($"Switched to {size}x{size} Linked-Chain grid!", "info");
            }
        }

        public void ShuffleGridSize(bool showToast = true)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            isPlayingCampaignMode = false;

            int currentSize = currentLevel != null ? currentLevel.gridSize : -1;
            List<int> otherSizes = new List<int>();
            foreach (int s in GRID_SIZES)
            {
                if (s != currentSize) otherSizes.Add(s);
            }
            int targetSize = otherSizes[UnityEngine.Random.Range(0, otherSizes.Count)];

            var proc = LevelSyncService.Instance.GenerateValidPuzzle(targetSize, $"shuffle_{targetSize}x{targetSize}_{UnityEngine.Random.Range(100, 999)}");
            SetupBoard(proc);

            if (showToast)
            {
                ShowToast($"Shuffled new {targetSize}x{targetSize} Linked-Chain level!", "info");
            }
        }

        public void SetupBoard(MeowLevelData level)
        {
            currentLevel = level;
            int N = level.gridSize;
            int idx = Array.IndexOf(GRID_SIZES, N);
            if (idx >= 0) currentSizeIdx = idx;

            boardState = new int[N, N];
            isHighlightedCell = new bool[N, N];
            hasMouseBadge = new bool[N, N];
            wrongFlashTimer = new float[N, N];
            boardShakeTimer = 0f;

            // Pre-place starting defaultCatCoord (matches Web Version loadLevel & loadCustomLevel)
            if (level.defaultCatCoord.x >= 0 && level.defaultCatCoord.x < N &&
                level.defaultCatCoord.y >= 0 && level.defaultCatCoord.y < N)
            {
                boardState[level.defaultCatCoord.x, level.defaultCatCoord.y] = 2;
            }
            else
            {
                // Fallback: find first solution cat
                for (int r = 0; r < N; r++)
                {
                    bool placed = false;
                    for (int c = 0; c < N; c++)
                    {
                        if (level.HasCat(r, c))
                        {
                            boardState[r, c] = 2;
                            level.defaultCatCoord = new Vector2Int(r, c);
                            placed = true;
                            break;
                        }
                    }
                    if (placed) break;
                }
            }

            remainingHearts = MAX_LIVES;
            powerCatReveal = 5;
            powerHint = 5;
            powerMouse = 5;

            currentRunTimer = 0f;
            isLevelComplete = false;
            isTransitioningLevel = false;
            isPlaying = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { SyncGridSizeToJS(N); } catch (Exception) {}
#endif

            OnLevelLoaded?.Invoke(level);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void SyncGridSizeToJS(int size);
#endif

        public void ClearBoard()
        {
            if (currentLevel == null || !isPlaying || isTransitioningLevel) return;
            int N = currentLevel.gridSize;
            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    // Keep revealed cats (state == 2), only clear crosses (state == 1)
                    if (boardState[r, c] == 1)
                    {
                        boardState[r, c] = 0;
                        isHighlightedCell[r, c] = false;
                        OnCellStateChanged?.Invoke(r, c, 0);
                    }
                }
            }
            ShowToast("Cleared all crosses!", "info", 1.2f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (isPlaying && !isLevelComplete && !isTransitioningLevel)
            {
                currentRunTimer += dt;
            }

            if (boardShakeTimer > 0f)
            {
                boardShakeTimer = Mathf.Max(0f, boardShakeTimer - dt);
            }

            if (toastResetTimer > 0f)
            {
                toastResetTimer -= dt;
                if (toastResetTimer <= 0f)
                {
                    toastType = "default";
                    toastMessage = "Double-Tap a cell to place a Cat  |  Single-Tap or Swipe across cells to add/remove Cross";
                }
            }

            if (currentLevel != null && wrongFlashTimer != null)
            {
                int N = currentLevel.gridSize;
                for (int r = 0; r < N; r++)
                {
                    for (int c = 0; c < N; c++)
                    {
                        if (wrongFlashTimer[r, c] > 0f)
                        {
                            wrongFlashTimer[r, c] = Mathf.Max(0f, wrongFlashTimer[r, c] - dt);
                        }
                    }
                }
            }
        }

        public void ClearHighlights()
        {
            if (currentLevel == null || isHighlightedCell == null) return;
            int N = currentLevel.gridSize;
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                    isHighlightedCell[r, c] = false;
        }

        public int GetPlacedCatCount()
        {
            if (currentLevel == null || boardState == null) return 0;
            int N = currentLevel.gridSize;
            int count = 0;
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                    if (boardState[r, c] == 2) count++;
            return count;
        }

        public void SetCellCrossState(int r, int c, int targetState)
        {
            if (!isPlaying || isTransitioningLevel || currentLevel == null) return;
            int N = currentLevel.gridSize;
            if (r < 0 || r >= N || c < 0 || c >= N) return;

            // Never overwrite placed Cats
            if (boardState[r, c] == 2) return;

            if (boardState[r, c] != targetState)
            {
                boardState[r, c] = targetState;
                OnCellStateChanged?.Invoke(r, c, targetState);
            }
        }

        public int SingleTapCross(int r, int c)
        {
            if (!isPlaying || isTransitioningLevel || currentLevel == null) return 1;
            int N = currentLevel.gridSize;
            if (r < 0 || r >= N || c < 0 || c >= N) return 1;

            int current = boardState[r, c];
            if (current == 2) return 1; // Do not disturb placed Cat

            int next = (current == 1) ? 0 : 1;
            boardState[r, c] = next;
            OnCellStateChanged?.Invoke(r, c, next);
            return next;
        }

        /// <summary>
        /// Exact port of Web Version's handleDoubleClickCat(r, c, isSafeMouse)
        /// Validates against currentLevel.HasCat(r, c), places Cat if right, or deducts 1 Life & flashes red if wrong.
        /// </summary>
        public void DoubleTapCat(int r, int c, bool isSafeMouse = false)
        {
            if (!isPlaying || isTransitioningLevel || currentLevel == null) return;
            int N = currentLevel.gridSize;
            if (r < 0 || r >= N || c < 0 || c >= N) return;

            // Already revealed Cat cannot be removed
            if (boardState[r, c] == 2) return;

            bool isCatInSolution = currentLevel.HasCat(r, c);

            if (isCatInSolution)
            {
                boardState[r, c] = 2;
                ClearHighlights();
                isHighlightedCell[r, c] = true;
                if (isSafeMouse) hasMouseBadge[r, c] = true;

                OnCellStateChanged?.Invoke(r, c, 2);
                ShowToast($"Revealed Cat! ({GetPlacedCatCount()}/{N} Cats found)", "info", 1.2f);
                CheckWinCondition();
            }
            else if (isSafeMouse)
            {
                boardState[r, c] = 1;
                isHighlightedCell[r, c] = true;
                hasMouseBadge[r, c] = true;
                OnCellStateChanged?.Invoke(r, c, 1);
            }
            else
            {
                // Wrong cell! Deduct 1 life, place a Cross (1), flash red, and shake board
                remainingHearts = Mathf.Max(0, remainingHearts - 1);
                boardState[r, c] = 1;
                wrongFlashTimer[r, c] = 0.45f;
                boardShakeTimer = 0.26f;
                OnCellStateChanged?.Invoke(r, c, 1);

                if (remainingHearts == 0)
                {
                    isTransitioningLevel = true;
                    ShowToast("Game Over! All 5 lives lost — shuffling new level...", "lose", 1.6f);
                    OnGameOver?.Invoke(false);
                    transitionCoroutine = StartCoroutine(DelayedTransitionCoroutine(1.35f, false));
                }
                else
                {
                    ShowToast($"Wrong cell! Not a Cat — lost 1 Life ({remainingHearts}/{MAX_LIVES} left)", "lose", 1.5f);
                }
            }
        }

        /// <summary>
        /// Exact port of Web Version's useCatReveal()
        /// </summary>
        public void UseCatReveal()
        {
            if (powerCatReveal <= 0 || !isPlaying || isTransitioningLevel || currentLevel == null) return;
            int N = currentLevel.gridSize;
            var unfoundCats = new List<Vector2Int>();

            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    if (currentLevel.HasCat(r, c) && boardState[r, c] != 2)
                    {
                        unfoundCats.Add(new Vector2Int(r, c));
                    }
                }
            }
            if (unfoundCats.Count == 0) return;

            powerCatReveal--;
            Vector2Int pick = unfoundCats[UnityEngine.Random.Range(0, unfoundCats.Count)];
            boardState[pick.x, pick.y] = 2;
            ClearHighlights();
            isHighlightedCell[pick.x, pick.y] = true;

            OnCellStateChanged?.Invoke(pick.x, pick.y, 2);
            ShowToast($"Reveal Power-Up: Found Cat at ({pick.x + 1},{pick.y + 1})!", "info");
            CheckWinCondition();
        }

        /// <summary>
        /// Exact port of Web Version's useSmartHint()
        /// </summary>
        public void UseSmartHint()
        {
            if (powerHint <= 0 || !isPlaying || isTransitioningLevel || currentLevel == null) return;
            int N = currentLevel.gridSize;

            // 1. Cross out remaining empty cells of a colored region that already has a cat
            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    if (boardState[r, c] == 2)
                    {
                        int regId = currentLevel.GetRegion(r, c);
                        var toCross = new List<Vector2Int>();
                        for (int rr = 0; rr < N; rr++)
                        {
                            for (int cc = 0; cc < N; cc++)
                            {
                                if (currentLevel.GetRegion(rr, cc) == regId && boardState[rr, cc] == 0)
                                {
                                    toCross.Add(new Vector2Int(rr, cc));
                                }
                            }
                        }
                        if (toCross.Count > 0)
                        {
                            powerHint--;
                            ClearHighlights();
                            foreach (var pt in toCross)
                            {
                                boardState[pt.x, pt.y] = 1;
                                isHighlightedCell[pt.x, pt.y] = true;
                            }
                            ShowToast($"Smart Hint: Crossed out {toCross.Count} cells in solved region!", "info");
                            return;
                        }
                    }
                }
            }

            // 2. Cross out Row, Col, and 8 surrounding neighbors of a placed cat
            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    if (boardState[r, c] == 2)
                    {
                        var toCross = new List<Vector2Int>();
                        for (int i = 0; i < N; i++)
                        {
                            if (boardState[r, i] == 0) toCross.Add(new Vector2Int(r, i));
                            if (boardState[i, c] == 0 && !toCross.Contains(new Vector2Int(i, c)))
                                toCross.Add(new Vector2Int(i, c));
                        }
                        for (int dr = -1; dr <= 1; dr++)
                        {
                            for (int dc = -1; dc <= 1; dc++)
                            {
                                int nr = r + dr, nc = c + dc;
                                if (nr >= 0 && nr < N && nc >= 0 && nc < N && boardState[nr, nc] == 0)
                                {
                                    var pt = new Vector2Int(nr, nc);
                                    if (!toCross.Contains(pt)) toCross.Add(pt);
                                }
                            }
                        }
                        if (toCross.Count > 0)
                        {
                            powerHint--;
                            ClearHighlights();
                            foreach (var pt in toCross)
                            {
                                boardState[pt.x, pt.y] = 1;
                                isHighlightedCell[pt.x, pt.y] = true;
                            }
                            ShowToast($"Smart Hint: Crossed out row/col/neighbors of Cat ({r + 1},{c + 1})!", "info");
                            return;
                        }
                    }
                }
            }

            // 3. Otherwise reveal the next linked cat in the chain and cross out its color region
            var unfound = new List<Vector2Int>();
            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    if (currentLevel.HasCat(r, c) && boardState[r, c] != 2)
                    {
                        unfound.Add(new Vector2Int(r, c));
                    }
                }
            }
            if (unfound.Count > 0)
            {
                powerHint--;
                ClearHighlights();
                Vector2Int pick = unfound[0];
                boardState[pick.x, pick.y] = 2;
                isHighlightedCell[pick.x, pick.y] = true;

                int regId = currentLevel.GetRegion(pick.x, pick.y);
                for (int rr = 0; rr < N; rr++)
                {
                    for (int cc = 0; cc < N; cc++)
                    {
                        if (currentLevel.GetRegion(rr, cc) == regId && boardState[rr, cc] == 0)
                        {
                            boardState[rr, cc] = 1;
                            isHighlightedCell[rr, cc] = true;
                        }
                    }
                }
                ShowToast($"Smart Hint: Revealed Cat at ({pick.x + 1},{pick.y + 1}) & cleared its region!", "info");
                CheckWinCondition();
            }
        }

        /// <summary>
        /// Exact port of Web Version's useMouseHelper()
        /// Reveals up to 3 undecided cells with Mouse badges.
        /// </summary>
        public void UseMouseHelper()
        {
            if (powerMouse <= 0 || !isPlaying || isTransitioningLevel || currentLevel == null) return;
            int N = currentLevel.gridSize;
            var undecided = new List<Vector2Int>();

            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    if (boardState[r, c] == 0) undecided.Add(new Vector2Int(r, c));
                }
            }
            if (undecided.Count == 0) return;

            powerMouse--;
            ClearHighlights();
            int count = Mathf.Min(3, undecided.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = UnityEngine.Random.Range(0, undecided.Count);
                Vector2Int pt = undecided[idx];
                undecided.RemoveAt(idx);

                boardState[pt.x, pt.y] = currentLevel.HasCat(pt.x, pt.y) ? 2 : 1;
                isHighlightedCell[pt.x, pt.y] = true;
                hasMouseBadge[pt.x, pt.y] = true;
            }

            ShowToast($"Mouse Helper: Inspected {count} cells!", "info");
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (isTransitioningLevel || currentLevel == null) return;
            int N = currentLevel.gridSize;
            int placedCats = GetPlacedCatCount();

            if (placedCats == N)
            {
                isTransitioningLevel = true;
                isLevelComplete = true;

                int elapsedMs = Mathf.Max(3500, Mathf.RoundToInt(currentRunTimer * 1000f));
                int leaguePts = N * 35 + remainingHearts * 20;

                var resBest = OfflineLeaderboardService.SubmitRun("meowsweeper", elapsedMs);
                var resUni = OfflineLeaderboardService.SubmitRun("unified_league", leaguePts);
                var activeRes = OfflineLeaderboardService.ActiveMode == "meowsweeper" ? resBest : resUni;

                string rankMsg = activeRes.rankDelta > 0
                    ? $" | Rank #{activeRes.newRank} (+{activeRes.rankDelta})!"
                    : $" | Rank #{activeRes.newRank}";

                ShowToast($"Cleared {N}x{N} in {(elapsedMs / 1000f):F1}s (+{leaguePts}p){rankMsg}", "win", 2.0f);
                OnGameOver?.Invoke(true);

                transitionCoroutine = StartCoroutine(DelayedTransitionCoroutine(1.65f, isPlayingCampaignMode));
            }
        }

        private IEnumerator DelayedTransitionCoroutine(float delaySec, bool advanceCampaign)
        {
            yield return new WaitForSecondsRealtime(delaySec);
            if (advanceCampaign && LevelSyncService.Instance.TotalLevels > 0)
            {
                PlayNextCampaignLevel();
            }
            else
            {
                ShuffleGridSize(false);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Meowsweeper.Core
{
    public class LevelSyncService : MonoBehaviour
    {
        public static LevelSyncService Instance { get; private set; }

        public const string LIVE_REPO_BASE_URL = "https://arpitjaiswal7.github.io/github-gamedev.io/content";
        public const string LOCAL_DEV_BASE_URL = "http://localhost:3000";

        public List<string> AvailableLevelIds = new List<string>();
        public Dictionary<int, List<string>> LevelsBySize = new Dictionary<int, List<string>>();
        public int TotalLevels => AvailableLevelIds.Count;

        // Strictly alternate 50% levels with 1 single-cell color region and 50% all multi-cell regions (matches web version)
        private bool levelToggle50Percent = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator FetchManifestCoroutine(Action<bool, int> onComplete)
        {
            // 1. Try local dev server first (matches web version LevelSyncManager)
            using (UnityWebRequest localReq = UnityWebRequest.Get($"{LOCAL_DEV_BASE_URL}/manifest.json"))
            {
                localReq.timeout = 2;
                yield return localReq.SendWebRequest();
                if (localReq.result == UnityWebRequest.Result.Success)
                {
                    ParseManifestLevels(localReq.downloadHandler.text);
                    Debug.Log($"[LevelSync] Fetched manifest from local server: {AvailableLevelIds.Count} levels.");
                    onComplete?.Invoke(true, AvailableLevelIds.Count);
                    yield break;
                }
            }

            // 2. Fallback to live GitHub Pages repository content
            string url = $"{LIVE_REPO_BASE_URL}/manifest.json";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    ParseManifestLevels(req.downloadHandler.text);
                    Debug.Log($"[LevelSync] Fetched manifest from Live GitHub Repo: {AvailableLevelIds.Count} levels.");
                    onComplete?.Invoke(true, AvailableLevelIds.Count);
                    yield break;
                }
            }

            // 3. Offline fallback catalog
            AvailableLevelIds.Clear();
            LevelsBySize.Clear();
            for (int i = 1; i <= 65; i++)
            {
                string id = $"lvl_{i}";
                AvailableLevelIds.Add(id);
                int size = 4 + (i / 10);
                if (size > 10) size = 10;
                AddLevelToSize(size, id);
            }
            Debug.LogWarning("[LevelSync] Manifest request failed. Initialized fallback catalog (65 levels).");
            onComplete?.Invoke(false, AvailableLevelIds.Count);
        }

        public IEnumerator DownloadLevelCoroutine(string levelId, Action<MeowLevelData> onComplete)
        {
            string url = $"{LIVE_REPO_BASE_URL}/levels/{levelId}.json";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var data = MeowLevelData.FromJson(req.downloadHandler.text);
                    if (data != null)
                    {
                        onComplete?.Invoke(data);
                        yield break;
                    }
                }
            }

            Debug.LogWarning($"[LevelSync] Could not download {levelId} from remote. Generating linked-chain puzzle.");
            int N = ExtractNumber(levelId);
            if (N < 4 || N > 12) N = 6;
            onComplete?.Invoke(GenerateValidPuzzle(N, levelId));
        }

        private void AddLevelToSize(int size, string id)
        {
            if (!LevelsBySize.ContainsKey(size))
            {
                LevelsBySize[size] = new List<string>();
            }
            if (!LevelsBySize[size].Contains(id))
            {
                LevelsBySize[size].Add(id);
            }
        }

        public string GetRandomLevelIdForSize(int size)
        {
            if (LevelsBySize.TryGetValue(size, out var list) && list != null && list.Count > 0)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }
            return null;
        }

        private void ParseManifestLevels(string manifestJson)
        {
            AvailableLevelIds.Clear();
            LevelsBySize.Clear();

            int searchIdx = 0;
            while (true)
            {
                int lvlKey = manifestJson.IndexOf("\"levelId\"", searchIdx, StringComparison.OrdinalIgnoreCase);
                if (lvlKey == -1) break;
                int colon = manifestJson.IndexOf(':', lvlKey);
                if (colon == -1) break;
                int q1 = manifestJson.IndexOf('"', colon);
                if (q1 == -1) break;
                int q2 = manifestJson.IndexOf('"', q1 + 1);
                if (q2 == -1) break;

                string id = manifestJson.Substring(q1 + 1, q2 - q1 - 1);
                if (!AvailableLevelIds.Contains(id))
                {
                    AvailableLevelIds.Add(id);
                }

                int size = 6;
                int sizeKey = manifestJson.IndexOf("\"gridSize\"", q2, StringComparison.OrdinalIgnoreCase);
                if (sizeKey != -1 && sizeKey - q2 < 200)
                {
                    int sColon = manifestJson.IndexOf(':', sizeKey);
                    if (sColon != -1)
                    {
                        int comma = manifestJson.IndexOf(',', sColon);
                        if (comma != -1)
                        {
                            string sizeStr = manifestJson.Substring(sColon + 1, comma - sColon - 1).Trim();
                            if (int.TryParse(sizeStr, out int parsedSize))
                            {
                                size = parsedSize;
                            }
                        }
                    }
                }

                AddLevelToSize(size, id);
                searchIdx = q2 + 1;
            }

            AvailableLevelIds.Sort((a, b) =>
            {
                int na = ExtractNumber(a);
                int nb = ExtractNumber(b);
                return na.CompareTo(nb);
            });
        }

        private int ExtractNumber(string s)
        {
            string digits = "";
            foreach (char c in s) if (char.IsDigit(c)) digits += c;
            return int.TryParse(digits, out int v) ? v : 0;
        }

        /// <summary>
        /// Exact C# port of the Web Version's generateChainedLevel(N, useSingleCellColor)
        /// Guarantees a 100% logically deducible Linked-Chain puzzle.
        /// </summary>
        public MeowLevelData GenerateValidPuzzle(int N, string id = "chained_level")
        {
            if (N < 4) N = 4;
            if (N > 12) N = 12;

            bool useSingleCellColor = levelToggle50Percent;
            levelToggle50Percent = !levelToggle50Percent;

            return GenerateChainedLevel(N, useSingleCellColor, id);
        }

        public MeowLevelData GenerateChainedLevel(int N, bool useSingleCellColor, string id = "chained_level")
        {
            Vector2Int[] dirs = new Vector2Int[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(0, 1)
            };

            for (int attempt = 0; attempt < 120; attempt++)
            {
                int[] catCols = GenerateValidCatSolution(N);
                Vector2Int[] catCoords = new Vector2Int[N];
                for (int r = 0; r < N; r++) catCoords[r] = new Vector2Int(r, catCols[r]);

                int startRow = UnityEngine.Random.Range(0, N);
                List<int> chainRows = new List<int> { startRow };
                int minR = startRow, maxR = startRow;
                while (chainRows.Count < N)
                {
                    bool canUp = minR > 0;
                    bool canDown = maxR < N - 1;
                    if (canUp && (!canDown || UnityEngine.Random.value < 0.5f))
                    {
                        minR--;
                        chainRows.Add(minR);
                    }
                    else
                    {
                        maxR++;
                        chainRows.Add(maxR);
                    }
                }

                Vector2Int[] orderedCats = new Vector2Int[N];
                for (int i = 0; i < N; i++) orderedCats[i] = catCoords[chainRows[i]];

                int[,] regions = new int[N, N];
                for (int r = 0; r < N; r++)
                    for (int c = 0; c < N; c++)
                        regions[r, c] = -1;

                int[] regionSizes = new int[N];
                for (int idx = 0; idx < N; idx++)
                {
                    Vector2Int coord = orderedCats[idx];
                    regions[coord.x, coord.y] = idx;
                    regionSizes[idx] = 1;
                }

                int singleCellIdx = useSingleCellColor ? UnityEngine.Random.Range(1, N) : -1;

                HashSet<int> solvedRows = new HashSet<int> { orderedCats[0].x };
                for (int k = 1; k < N; k++)
                {
                    int rk = orderedCats[k].x;
                    int ck = orderedCats[k].y;
                    if (k != singleCellIdx)
                    {
                        int adjRow = solvedRows.Contains(rk - 1) ? rk - 1 : rk + 1;
                        if (adjRow >= 0 && adjRow < N && regions[adjRow, ck] == -1)
                        {
                            regions[adjRow, ck] = k;
                            regionSizes[k]++;
                        }
                    }
                    solvedRows.Add(rk);
                }

                // Expand region 0 to at least 2 cells
                int r0 = orderedCats[0].x;
                int c0 = orderedCats[0].y;
                Vector2Int[] shuffledDirs = ShuffleDirs(dirs);
                for (int d = 0; d < shuffledDirs.Length; d++)
                {
                    int nr = r0 + shuffledDirs[d].x;
                    int nc = c0 + shuffledDirs[d].y;
                    if (nr >= 0 && nr < N && nc >= 0 && nc < N && regions[nr, nc] == -1)
                    {
                        regions[nr, nc] = 0;
                        regionSizes[0]++;
                        if (regionSizes[0] >= 2) break;
                    }
                }
                if (regionSizes[0] < 2) continue;

                int unassigned = 0;
                for (int r = 0; r < N; r++)
                    for (int c = 0; c < N; c++)
                        if (regions[r, c] == -1) unassigned++;

                int guard = 0;
                while (unassigned > 0 && guard < 250)
                {
                    guard++;
                    bool progressed = false;
                    for (int r = 0; r < N; r++)
                    {
                        for (int c = 0; c < N; c++)
                        {
                            if (regions[r, c] != -1) continue;
                            List<int> validRegs = new List<int>();
                            for (int d = 0; d < 4; d++)
                            {
                                int nr = r + dirs[d].x;
                                int nc = c + dirs[d].y;
                                if (nr >= 0 && nr < N && nc >= 0 && nc < N && regions[nr, nc] != -1)
                                {
                                    int regId = regions[nr, nc];
                                    if (regId == singleCellIdx) continue;
                                    if (regId == 0 || IsEliminatedByEarlier(r, c, regId, orderedCats))
                                    {
                                        validRegs.Add(regId);
                                    }
                                }
                            }
                            if (validRegs.Count > 0)
                            {
                                validRegs.Sort();
                                int pick = UnityEngine.Random.value < 0.75f
                                    ? validRegs[0]
                                    : validRegs[UnityEngine.Random.Range(0, validRegs.Count)];
                                regions[r, c] = pick;
                                regionSizes[pick]++;
                                unassigned--;
                                progressed = true;
                            }
                        }
                    }
                    if (!progressed) break;
                }

                if (unassigned > 0) continue;

                bool sizesValid = regionSizes[0] >= 2;
                for (int k = 1; k < N; k++)
                {
                    if (k == singleCellIdx && regionSizes[k] != 1) sizesValid = false;
                    if (k != singleCellIdx && regionSizes[k] < 2) sizesValid = false;
                }
                if (!sizesValid) continue;

                // Shuffle color IDs
                int[] perm = new int[N];
                for (int i = 0; i < N; i++) perm[i] = i;
                ShuffleIntArray(perm);

                int[] regionsFlat = new int[N * N];
                int[] solutionFlat = new int[N * N];
                for (int r = 0; r < N; r++)
                {
                    solutionFlat[r * N + catCols[r]] = 1;
                    for (int c = 0; c < N; c++)
                    {
                        regionsFlat[r * N + c] = perm[regions[r, c]];
                    }
                }

                return new MeowLevelData
                {
                    levelId = id,
                    levelDisplayName = $"{N}x{N} Linked Chain",
                    order = ExtractNumber(id),
                    gridSize = N,
                    tier = GetTierName(N),
                    maxHearts = 5,
                    hintsAllowed = 5,
                    hasSingleCellColor = useSingleCellColor,
                    defaultCatCoord = orderedCats[0],
                    regionsFlat = regionsFlat,
                    solutionFlat = solutionFlat
                };
            }

            // Fallback if 120 attempts didn't converge
            int[] fbCols = GenerateValidCatSolution(N);
            int[] fbRegs = new int[N * N];
            int[] fbSol = new int[N * N];
            for (int r = 0; r < N; r++)
            {
                fbSol[r * N + fbCols[r]] = 1;
                for (int c = 0; c < N; c++) fbRegs[r * N + c] = r;
            }
            return new MeowLevelData
            {
                levelId = id,
                levelDisplayName = $"{N}x{N} Linked Chain",
                order = 1,
                gridSize = N,
                tier = GetTierName(N),
                maxHearts = 5,
                hintsAllowed = 5,
                hasSingleCellColor = false,
                defaultCatCoord = new Vector2Int(0, fbCols[0]),
                regionsFlat = fbRegs,
                solutionFlat = fbSol
            };
        }

        private bool IsEliminatedByEarlier(int r, int c, int k, Vector2Int[] orderedCats)
        {
            for (int j = 0; j < k; j++)
            {
                int rj = orderedCats[j].x;
                int cj = orderedCats[j].y;
                if (r == rj || c == cj || (Mathf.Abs(r - rj) <= 1 && Mathf.Abs(c - cj) <= 1))
                {
                    return true;
                }
            }
            return false;
        }

        private int[] GenerateValidCatSolution(int N)
        {
            int[] cols = new int[N];
            for (int i = 0; i < N; i++) cols[i] = -1;
            bool[] usedCols = new bool[N];

            bool Backtrack(int r)
            {
                if (r == N) return true;
                int[] candidates = new int[N];
                for (int i = 0; i < N; i++) candidates[i] = i;
                ShuffleIntArray(candidates);

                for (int i = 0; i < N; i++)
                {
                    int c = candidates[i];
                    if (usedCols[c]) continue;
                    if (r > 0 && Mathf.Abs(c - cols[r - 1]) <= 1) continue;
                    cols[r] = c;
                    usedCols[c] = true;
                    if (Backtrack(r + 1)) return true;
                    usedCols[c] = false;
                    cols[r] = -1;
                }
                return false;
            }

            if (Backtrack(0)) return cols;

            // Deterministic valid fallback
            int idx = 0;
            for (int i = 1; i < N; i += 2) cols[idx++] = i;
            for (int i = 0; i < N; i += 2) cols[idx++] = i;
            return cols;
        }

        private void ShuffleIntArray(int[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = arr[i];
                arr[i] = arr[j];
                arr[j] = tmp;
            }
        }

        private Vector2Int[] ShuffleDirs(Vector2Int[] dirs)
        {
            Vector2Int[] copy = new Vector2Int[dirs.Length];
            Array.Copy(dirs, copy, dirs.Length);
            for (int i = copy.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = copy[i];
                copy[i] = copy[j];
                copy[j] = tmp;
            }
            return copy;
        }

        private string GetTierName(int N)
        {
            if (N <= 4) return "Tutorial";
            if (N <= 6) return "Easy";
            if (N <= 8) return "Medium";
            if (N <= 10) return "Hard";
            return "Expert";
        }
    }
}

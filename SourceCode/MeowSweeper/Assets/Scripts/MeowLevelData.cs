using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowsweeper.Core
{
    [Serializable]
    public class LevelManifestMeta
    {
        public string levelId;
        public string levelDisplayName;
        public int order;
        public int gridSize;
        public string tier;
        public string hash;
        public int fileSize;
        public string url;
    }

    [Serializable]
    public class MeowLevelData
    {
        public string levelId;
        public string levelDisplayName;
        public int order;
        public int gridSize;
        public string tier;
        public int maxHearts = 5;
        public int hintsAllowed = 5;
        public int solveTimeSec = 120;
        public int stepScore = 100;
        public bool requireGuided;
        public bool requireLineLock;

        // Web version parity fields
        public bool hasSingleCellColor;
        public Vector2Int defaultCatCoord = new Vector2Int(-1, -1);

        // Flattened 2D matrix representations (size = gridSize * gridSize)
        public int[] regionsFlat;
        public int[] solutionFlat;

        public int GetRegion(int r, int c)
        {
            if (regionsFlat == null || r < 0 || r >= gridSize || c < 0 || c >= gridSize) return 0;
            return regionsFlat[r * gridSize + c];
        }

        public bool HasCat(int r, int c)
        {
            if (solutionFlat == null || r < 0 || r >= gridSize || c < 0 || c >= gridSize) return false;
            return solutionFlat[r * gridSize + c] == 1;
        }

        public static MeowLevelData FromJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText)) return null;

            var data = new MeowLevelData();
            try
            {
                data.levelId = ExtractString(jsonText, "levelId", "lvl_1");
                data.levelDisplayName = ExtractString(jsonText, "levelDisplayName", "Level 1");
                data.order = ExtractInt(jsonText, "order", 1);
                data.gridSize = ExtractInt(jsonText, "gridSize", 6);
                data.tier = ExtractString(jsonText, "tier", "Easy");
                data.maxHearts = ExtractInt(jsonText, "maxHearts", 5);
                data.hintsAllowed = ExtractInt(jsonText, "hintsAllowed", 5);
                data.solveTimeSec = ExtractInt(jsonText, "solveTimeSec", 120);
                data.stepScore = ExtractInt(jsonText, "stepScore", 100);

                int N = data.gridSize;
                data.regionsFlat = new int[N * N];
                data.solutionFlat = new int[N * N];

                Parse2DArray(jsonText, "regions", N, data.regionsFlat);
                Parse2DArray(jsonText, "solution", N, data.solutionFlat);

                // Compute region cell counts to detect 1-cell region anchor
                var regCounts = new Dictionary<int, int>();
                for (int i = 0; i < N * N; i++)
                {
                    int reg = data.regionsFlat[i];
                    if (!regCounts.ContainsKey(reg)) regCounts[reg] = 0;
                    regCounts[reg]++;
                }

                data.hasSingleCellColor = false;
                foreach (var kv in regCounts)
                {
                    if (kv.Value == 1)
                    {
                        data.hasSingleCellColor = true;
                        break;
                    }
                }

                // Match web version loadCustomLevel defaultCatCoord selection
                data.defaultCatCoord = new Vector2Int(0, 0);
                for (int r = 0; r < N; r++)
                {
                    for (int c = 0; c < N; c++)
                    {
                        if (data.solutionFlat[r * N + c] == 1)
                        {
                            data.defaultCatCoord = new Vector2Int(r, c);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MeowLevelData] Error parsing level JSON: {ex.Message}");
            }
            return data;
        }

        private static string ExtractString(string json, string key, string fallback)
        {
            int idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return fallback;
            int colon = json.IndexOf(':', idx);
            if (colon == -1) return fallback;
            int q1 = json.IndexOf('"', colon);
            if (q1 == -1) return fallback;
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 == -1) return fallback;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static int ExtractInt(string json, string key, int fallback)
        {
            int idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return fallback;
            int colon = json.IndexOf(':', idx);
            if (colon == -1) return fallback;
            int end = json.IndexOfAny(new char[] { ',', '}', '\n', '\r' }, colon + 1);
            if (end == -1) end = json.Length;
            string valStr = json.Substring(colon + 1, end - colon - 1).Trim();
            return int.TryParse(valStr, out int val) ? val : fallback;
        }

        private static void Parse2DArray(string json, string key, int N, int[] targetFlat)
        {
            int keyIdx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
            if (keyIdx == -1) return;
            int openBracket = json.IndexOf('[', keyIdx);
            if (openBracket == -1) return;

            int r = 0, c = 0;
            bool insideRow = false;
            string numToken = "";

            for (int i = openBracket + 1; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '[')
                {
                    insideRow = true;
                    c = 0;
                    numToken = "";
                }
                else if (ch == ']')
                {
                    if (insideRow)
                    {
                        if (!string.IsNullOrEmpty(numToken.Trim()))
                        {
                            if (r < N && c < N && int.TryParse(numToken.Trim(), out int v))
                                targetFlat[r * N + c] = v;
                            c++;
                        }
                        insideRow = false;
                        r++;
                        if (r >= N) break;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (insideRow)
                {
                    if (ch == ',')
                    {
                        if (r < N && c < N && int.TryParse(numToken.Trim(), out int v))
                            targetFlat[r * N + c] = v;
                        c++;
                        numToken = "";
                    }
                    else if (char.IsDigit(ch) || ch == '-')
                    {
                        numToken += ch;
                    }
                }
            }
        }
    }
}

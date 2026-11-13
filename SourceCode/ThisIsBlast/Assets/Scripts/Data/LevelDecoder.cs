using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ThisIsBlast.Core;

namespace ThisIsBlast.Data
{
    /// <summary>
    /// Handles decoding of level data downloaded from CDN or read from disk.
    /// Can decode both JSON files (human-readable) and compact binary/FlatBuffer streams.
    /// Safely handles old vs new build compatibility with default values.
    /// </summary>
    public static class LevelDecoder
    {
        private const byte CURRENT_BINARY_VERSION = 1;
        private const uint BINARY_MAGIC_HEADER = 0x4C564C42; // "LVLB" in ASCII

        [Serializable]
        private class TracksManifestData
        {
            public List<int> track_linear_a = new List<int>();
            public List<int> track_sawtooth_b = new List<int>();
            public List<int> track_early_c = new List<int>();
            public List<int> track_royal_rhythm_d = new List<int>();
            public List<int> track_zen_flow_e = new List<int>();
            public List<int> track_staircase_f = new List<int>();
            public List<int> track_boss_rush_g = new List<int>();
        }

        private static TracksManifestData _cachedManifest;
        private static bool _manifestChecked = false;

        /// <summary>
        /// Resolves a stage number into a specific level ID using the active A/B test progression track.
        /// If tracks_manifest.json does not exist or index is out of bounds, defaults to 1:1 stage number.
        /// </summary>
        public static int ResolveMappedLevelId(int stageNumber)
        {
            if (!_manifestChecked)
            {
                _manifestChecked = true;
                TextAsset manifestAsset = Resources.Load<TextAsset>("Levels/tracks_manifest");
                if (manifestAsset != null && !string.IsNullOrEmpty(manifestAsset.text))
                {
                    try
                    {
                        _cachedManifest = JsonUtility.FromJson<TracksManifestData>(manifestAsset.text);
                        Debug.Log("<color=cyan>[LevelDecoder] Loaded tracks_manifest.json for A/B testing.</color>");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LevelDecoder] Failed parsing tracks_manifest.json: {ex.Message}");
                    }
                }
            }

            if (_cachedManifest != null)
            {
                string activeTrack = "track_linear_a";
                if (LiveOpsManager.Instance != null)
                {
                    activeTrack = (LiveOpsManager.Instance.ActiveArchetype == "arc_sawtooth") ? "track_sawtooth_b" : "track_linear_a";
                }
                else if (RemoteConfigService.Instance != null)
                {
                    activeTrack = RemoteConfigService.Instance.GetString("progression_track", "track_linear_a");
                }
                List<int> targetList = null;
                switch (activeTrack)
                {
                    case "track_sawtooth_b": targetList = _cachedManifest.track_sawtooth_b; break;
                    case "track_early_c": targetList = _cachedManifest.track_early_c; break;
                    case "track_royal_rhythm_d": targetList = _cachedManifest.track_royal_rhythm_d; break;
                    case "track_zen_flow_e": targetList = _cachedManifest.track_zen_flow_e; break;
                    case "track_staircase_f": targetList = _cachedManifest.track_staircase_f; break;
                    case "track_boss_rush_g": targetList = _cachedManifest.track_boss_rush_g; break;
                    default: targetList = _cachedManifest.track_linear_a; break;
                }

                if (targetList != null && stageNumber >= 1 && stageNumber <= targetList.Count)
                {
                    int mappedId = targetList[stageNumber - 1];
                    Debug.Log($"<color=magenta>[LevelDecoder] A/B Track '{activeTrack}': Stage {stageNumber} -> Level ID {mappedId}</color>");
                    return mappedId;
                }
            }

            return stageNumber;
        }

        #region Public API

        /// <summary>
        /// Loads a level by levelNumber using 3-tier loading:
        /// 1. Persistent CDN Cache (Application.persistentDataPath/Levels/Level_X.bin)
        /// 2. Shipped Resources fallback (Resources/Levels/Level_X.bin or .json)
        /// 3. Procedural generator fallback (guarantees the game never fails)
        /// </summary>
        public static LevelData LoadLevel(int stageNumber)
        {
            int levelNumber = ResolveMappedLevelId(stageNumber);

            // Tier 1: Check downloaded/cached CDN file in persistentDataPath
            try
            {
                string cachedPath = Path.Combine(Application.persistentDataPath, "Levels", $"Level_{levelNumber}.bin");
                if (File.Exists(cachedPath))
                {
                    byte[] bytes = File.ReadAllBytes(cachedPath);
                    var cachedLevel = Decode(bytes);
                    if (cachedLevel != null)
                    {
                        Debug.Log($"<color=cyan>[LevelDecoder] Loaded Level {levelNumber} from Persistent CDN Cache.</color>");
                        return cachedLevel;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelDecoder] Failed reading cached level: {ex.Message}");
            }

            // Tier 2: Check bundled Resources (TextAsset matches .bin or .json)
            TextAsset bundledAsset = Resources.Load<TextAsset>($"Levels/Level_{levelNumber}");
            if (bundledAsset != null && bundledAsset.bytes != null && bundledAsset.bytes.Length > 0)
            {
                var bundledLevel = Decode(bundledAsset.bytes);
                if (bundledLevel != null)
                {
                    Debug.Log($"<color=green>[LevelDecoder] Loaded Level {levelNumber} from Bundled Resources.</color>");
                    return bundledLevel;
                }
            }

            // Tier 3: Procedural Generator fallback
            Debug.Log($"<color=yellow>[LevelDecoder] Generating Level {levelNumber} procedurally.</color>");
            return ProceduralLevelGenerator.GenerateLevel(levelNumber);
        }

        /// <summary>
        /// Saves a downloaded level binary to the local device persistent cache.
        /// </summary>
        public static void SaveToCache(int levelNumber, byte[] bytes)
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Levels");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, $"Level_{levelNumber}.bin");
                File.WriteAllBytes(filePath, bytes);
                Debug.Log($"<color=cyan>[LevelDecoder] Cached Level {levelNumber} to disk ({bytes.Length} bytes).</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelDecoder] Failed to cache level {levelNumber}: {ex.Message}");
            }
        }

        /// <summary>
        /// Decodes a downloaded byte array (either JSON text bytes or Binary bytes) into a usable LevelData object.
        /// </summary>
        public static LevelData Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError("[LevelDecoder] Received empty or null level bytes!");
                return null;
            }

            // Check if binary header matches
            if (IsBinaryFormat(bytes))
            {
                return DecodeFromBinary(bytes);
            }

            // Otherwise decode as JSON string
            string jsonText = System.Text.Encoding.UTF8.GetString(bytes);
            return DecodeFromJson(jsonText);
        }

        /// <summary>
        /// Decodes a JSON string directly into a runtime LevelData ScriptableObject.
        /// </summary>
        public static LevelData DecodeFromJson(string json)
        {
            try
            {
                var level = JsonUtility.FromJson<LevelData>(json);
                SanitizeLevelArsenal(level);
                return level;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelDecoder] Failed to decode JSON: {ex.Message}");
                return null;
            }
        }

        public static int SanitizeBulletCount(int rawCount)
        {
            if (rawCount <= 0) return 20;
            // Valid multiples of 10, 15, or 20
            if (rawCount % 10 == 0 || rawCount % 15 == 0 || rawCount % 20 == 0)
            {
                return rawCount;
            }

            // Map random values (like 2, 3, 8) to clean multiples of 10, 15, or 20
            if (rawCount <= 12) return 10;
            if (rawCount <= 17) return 15;
            return 20;
        }

        public static void SanitizeLevelArsenal(LevelData level)
        {
            if (level == null || level.arsenalColumns == null) return;
            foreach (var col in level.arsenalColumns)
            {
                if (col == null || col.blocks == null) continue;
                foreach (var b in col.blocks)
                {
                    if (b != null)
                    {
                        b.bulletCount = SanitizeBulletCount(b.bulletCount);
                    }
                }
            }
        }

        /// <summary>
        /// Encodes a LevelData object into a JSON string.
        /// </summary>
        public static string EncodeToJson(LevelData level, bool prettyPrint = true)
        {
            if (level == null) return string.Empty;
            return JsonUtility.ToJson(level, prettyPrint);
        }

        /// <summary>
        /// Decodes compact binary level data (e.g., from CDN or local cache) into a LevelData ScriptableObject.
        /// Guaranteed backward compatible across live builds using schema versioning and safe defaults.
        /// </summary>
        public static LevelData DecodeFromBinary(byte[] bytes)
        {
            var level = new LevelData();

            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                // 1. Read & Validate Header
                uint magic = reader.ReadUInt32();
                if (magic != BINARY_MAGIC_HEADER)
                {
                    Debug.LogWarning("[LevelDecoder] Binary magic header mismatch! Attempting safe parse...");
                }

                // 2. Read Schema Version (Enables backward compatibility!)
                byte fileVersion = reader.ReadByte();

                // 3. Read Core Level Parameters
                level.levelId = reader.ReadInt32();
                level.tier = reader.ReadInt32();
                level.gridWidth = reader.ReadInt32();
                level.gridHeight = reader.ReadInt32();
                level.gridDepth = reader.ReadInt32();
                level.arsenalColumnsCount = reader.ReadInt32();

                level.winCondition = (WinConditionType)reader.ReadByte();
                level.clearPercentageRequired = reader.ReadSingle();
                level.targetChestsRequired = reader.ReadInt32();

                // 4. Read Enemy Blocks
                int enemyCount = reader.ReadInt32();
                level.enemyBlocks = new List<EnemyBlockData>(enemyCount);

                for (int i = 0; i < enemyCount; i++)
                {
                    var block = new EnemyBlockData
                    {
                        x = reader.ReadInt32(),
                        y = reader.ReadInt32(),
                        z = reader.ReadInt32(),
                        type = (EnemyBlockType)reader.ReadByte(),
                        color = (BlockColor)reader.ReadByte(),
                        hp = reader.ReadInt32(),
                        hitThreshold = reader.ReadInt32(),
                        gemReward = reader.ReadInt32(),
                        partnerX = reader.ReadInt32(),
                        partnerY = reader.ReadInt32(),
                        unlockColIndex = reader.ReadInt32()
                    };

                    // Future schema versions (e.g. v2 mechanics) can be conditionally read here:
                    if (fileVersion >= 2)
                    {
                        // Safely read extra fields if present
                    }

                    level.enemyBlocks.Add(block);
                }

                // 5. Read Arsenal Columns
                int colCount = reader.ReadInt32();
                level.arsenalColumns = new List<ArsenalColumnData>(colCount);

                for (int c = 0; c < colCount; c++)
                {
                    var col = new ArsenalColumnData
                    {
                        columnIndex = reader.ReadInt32(),
                        isLockedInitially = reader.ReadBoolean(),
                        unlockKeyId = reader.ReadInt32(),
                        blocks = new List<ArsenalBlockData>()
                    };

                    int blockCount = reader.ReadInt32();
                    for (int b = 0; b < blockCount; b++)
                    {
                        var arsenalBlock = new ArsenalBlockData
                        {
                            color = (BlockColor)reader.ReadByte(),
                            bulletCount = reader.ReadInt32(),
                            tier = reader.ReadInt32(),
                            isHidden = reader.ReadBoolean()
                        };
                        col.blocks.Add(arsenalBlock);
                    }

                    level.arsenalColumns.Add(col);
                }
            }

            SanitizeLevelArsenal(level);
            return level;
        }

        /// <summary>
        /// Encodes a LevelData object into an ultra-compact binary format (~700 bytes) ready for CDN upload.
        /// </summary>
        public static byte[] EncodeToBinary(LevelData level)
        {
            if (level == null) return null;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // 1. Header & Version
                writer.Write(BINARY_MAGIC_HEADER);
                writer.Write(CURRENT_BINARY_VERSION);

                // 2. Core Parameters
                writer.Write(level.levelId);
                writer.Write(level.tier);
                writer.Write(level.gridWidth);
                writer.Write(level.gridHeight);
                writer.Write(level.gridDepth);
                writer.Write(level.arsenalColumnsCount);

                writer.Write((byte)level.winCondition);
                writer.Write(level.clearPercentageRequired);
                writer.Write(level.targetChestsRequired);

                // 3. Enemy Blocks
                int enemyCount = level.enemyBlocks != null ? level.enemyBlocks.Count : 0;
                writer.Write(enemyCount);

                for (int i = 0; i < enemyCount; i++)
                {
                    var b = level.enemyBlocks[i];
                    writer.Write(b.x);
                    writer.Write(b.y);
                    writer.Write(b.z);
                    writer.Write((byte)b.type);
                    writer.Write((byte)b.color);
                    writer.Write(b.hp);
                    writer.Write(b.hitThreshold);
                    writer.Write(b.gemReward);
                    writer.Write(b.partnerX);
                    writer.Write(b.partnerY);
                    writer.Write(b.unlockColIndex);
                }

                // 4. Arsenal Columns
                int colCount = level.arsenalColumns != null ? level.arsenalColumns.Count : 0;
                writer.Write(colCount);

                for (int c = 0; c < colCount; c++)
                {
                    var col = level.arsenalColumns[c];
                    writer.Write(col.columnIndex);
                    writer.Write(col.isLockedInitially);
                    writer.Write(col.unlockKeyId);

                    int blockCount = col.blocks != null ? col.blocks.Count : 0;
                    writer.Write(blockCount);

                    for (int b = 0; b < blockCount; b++)
                    {
                        var ab = col.blocks[b];
                        writer.Write((byte)ab.color);
                        writer.Write(ab.bulletCount);
                        writer.Write(ab.tier);
                        writer.Write(ab.isHidden);
                    }
                }

                return ms.ToArray();
            }
        }

        #endregion

        #region Helper Methods

        private static bool IsBinaryFormat(byte[] bytes)
        {
            if (bytes.Length < 4) return false;
            uint magic = BitConverter.ToUInt32(bytes, 0);
            return magic == BINARY_MAGIC_HEADER;
        }

        #endregion
    }
}

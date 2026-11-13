using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetOrganizer.Editor
{
    [Serializable]
    public class PinnedAssetGroup
    {
        public string id;
        public string name;
        public Color color = new Color(1f, 0.84f, 0.1f, 1f);
        public bool isExpanded = true;
        public bool isDefaultUngrouped;

        public PinnedAssetGroup() { }

        public PinnedAssetGroup(string id, string name, Color color, bool isDefaultUngrouped = false)
        {
            this.id = id;
            this.name = name;
            this.color = color;
            this.isExpanded = true;
            this.isDefaultUngrouped = isDefaultUngrouped;
        }
    }

    [Serializable]
    public class PinnedAssetEntry
    {
        public string guid;
        public string assetPath;
        public string fileName;
        public string folderPath;
        public string assetType;
        public long fileSizeBytes;
        public string groupId;
        public long pinnedTicks;

        public PinnedAssetEntry() { }

        public PinnedAssetEntry(string guid, string assetPath, string groupId)
        {
            this.guid = guid;
            this.assetPath = assetPath;
            this.groupId = string.IsNullOrEmpty(groupId) ? PinnedAssetsStore.UngroupedGroupId : groupId;
            this.pinnedTicks = DateTime.UtcNow.Ticks;
            RefreshMetadata();
        }

        public void RefreshMetadata()
        {
            if (string.IsNullOrEmpty(guid))
                return;

            string resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                assetPath = resolvedPath;
            }

            if (string.IsNullOrEmpty(assetPath))
                return;

            fileName = Path.GetFileName(assetPath);
            int lastSlash = assetPath.LastIndexOf('/');
            folderPath = lastSlash >= 0 ? assetPath.Substring(0, lastSlash + 1) : string.Empty;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                assetType = "Folder";
                fileSizeBytes = CalculateFolderSize(assetPath);
            }
            else
            {
                Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                assetType = GetFriendlyAssetTypeName(mainType, assetPath);
                fileSizeBytes = GetFileSizeOnDisk(assetPath);
            }
        }

        private static string GetFriendlyAssetTypeName(Type type, string path)
        {
            if (type == null)
                return "Asset";

            if (type == typeof(Texture2D))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    return "Sprite";
                return "Texture2D";
            }

            if (type == typeof(GameObject))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".prefab")
                    return "Prefab";
                if (ext == ".fbx" || ext == ".obj" || ext == ".blend" || ext == ".dae")
                    return "Model";
                return "GameObject";
            }

            if (type == typeof(MonoScript))
                return "Script";

            if (type == typeof(SceneAsset))
                return "Scene";

            if (type == typeof(AudioClip))
                return "AudioClip";

            if (type == typeof(Material))
                return "Material";

            if (type == typeof(Shader))
                return "Shader";

            if (typeof(ScriptableObject).IsAssignableFrom(type))
                return type.Name;

            return type.Name;
        }

        private static long GetFileSizeOnDisk(string assetRelativePath)
        {
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrEmpty(projectRoot))
                    return 0L;

                string fullPath = Path.Combine(projectRoot, assetRelativePath);
                if (File.Exists(fullPath))
                {
                    return new FileInfo(fullPath).Length;
                }
            }
            catch
            {
                // Ignore IO exceptions on locked or virtual files
            }
            return 0L;
        }

        private static long CalculateFolderSize(string folderRelativePath)
        {
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrEmpty(projectRoot))
                    return 0L;

                string fullPath = Path.Combine(projectRoot, folderRelativePath);
                if (!Directory.Exists(fullPath))
                    return 0L;

                long total = 0L;
                string[] files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    if (!files[i].EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        total += new FileInfo(files[i]).Length;
                    }
                }
                return total;
            }
            catch
            {
                return 0L;
            }
        }
    }

    /// <summary>
    /// Persistent per-project store for pinned assets and groups.
    /// Stored in ProjectSettings/AssetOrganizerPinnedStore.json (Editor-only, zero runtime cost).
    /// </summary>
    public static class PinnedAssetsStore
    {
        public const string UngroupedGroupId = "ungrouped";
        private const string StoreFilePath = "ProjectSettings/AssetOrganizerPinnedStore.json";

        public static readonly Color DefaultYellowTagColor = new Color(1f, 0.84f, 0.08f, 1f);

        private static readonly Color[] PresetGroupPalette = new Color[]
        {
            new Color(0.24f, 0.72f, 1.00f, 1f), // Sky Blue
            new Color(0.30f, 0.88f, 0.48f, 1f), // Emerald Green
            new Color(1.00f, 0.45f, 0.35f, 1f), // Coral Red
            new Color(0.75f, 0.45f, 1.00f, 1f), // Violet
            new Color(1.00f, 0.62f, 0.18f, 1f), // Orange
            new Color(0.20f, 0.90f, 0.85f, 1f), // Teal
            new Color(1.00f, 0.42f, 0.72f, 1f), // Pink
        };

        [Serializable]
        private class StoreData
        {
            public List<PinnedAssetGroup> groups = new List<PinnedAssetGroup>();
            public List<PinnedAssetEntry> entries = new List<PinnedAssetEntry>();
        }

        private static StoreData _data;
        private static readonly Dictionary<string, PinnedAssetEntry> _entryByGuid = new Dictionary<string, PinnedAssetEntry>(StringComparer.Ordinal);
        private static readonly Dictionary<string, PinnedAssetGroup> _groupById = new Dictionary<string, PinnedAssetGroup>(StringComparer.Ordinal);
        private static bool _isLoaded;

        public static event Action OnStoreChanged;

        public static IReadOnlyList<PinnedAssetGroup> Groups
        {
            get
            {
                EnsureLoaded();
                return _data.groups;
            }
        }

        public static IReadOnlyList<PinnedAssetEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return _data.entries;
            }
        }

        public static Color GetSuggestedNewGroupColor()
        {
            EnsureLoaded();
            int customCount = Mathf.Max(0, _data.groups.Count - 1);
            return PresetGroupPalette[customCount % PresetGroupPalette.Length];
        }

        public static void EnsureLoaded()
        {
            if (_isLoaded && _data != null)
                return;

            _data = new StoreData();
            if (File.Exists(StoreFilePath))
            {
                try
                {
                    string json = File.ReadAllText(StoreFilePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        StoreData loaded = JsonUtility.FromJson<StoreData>(json);
                        if (loaded != null)
                        {
                            _data = loaded;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Asset Organizer] Failed to read {StoreFilePath}: {ex.Message}");
                }
            }

            if (_data.groups == null)
                _data.groups = new List<PinnedAssetGroup>();
            if (_data.entries == null)
                _data.entries = new List<PinnedAssetEntry>();

            EnsureUngroupedExists();
            RebuildLookupTables(pruneMissing: false);
            _isLoaded = true;
        }

        private static void EnsureUngroupedExists()
        {
            PinnedAssetGroup ungrouped = null;
            for (int i = 0; i < _data.groups.Count; i++)
            {
                if (_data.groups[i] != null && _data.groups[i].id == UngroupedGroupId)
                {
                    ungrouped = _data.groups[i];
                    ungrouped.isDefaultUngrouped = true;
                    if (string.IsNullOrEmpty(ungrouped.name))
                        ungrouped.name = "Ungrouped";
                    break;
                }
            }

            if (ungrouped == null)
            {
                ungrouped = new PinnedAssetGroup(UngroupedGroupId, "Ungrouped", DefaultYellowTagColor, isDefaultUngrouped: true);
                _data.groups.Insert(0, ungrouped);
            }
        }

        private static void RebuildLookupTables(bool pruneMissing)
        {
            _groupById.Clear();
            for (int i = 0; i < _data.groups.Count; i++)
            {
                PinnedAssetGroup group = _data.groups[i];
                if (group != null && !string.IsNullOrEmpty(group.id))
                {
                    _groupById[group.id] = group;
                }
            }

            _entryByGuid.Clear();
            for (int i = _data.entries.Count - 1; i >= 0; i--)
            {
                PinnedAssetEntry entry = _data.entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.guid))
                {
                    _data.entries.RemoveAt(i);
                    continue;
                }

                // Duplicate-pin prevention
                if (_entryByGuid.ContainsKey(entry.guid))
                {
                    _data.entries.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(entry.groupId) || !_groupById.ContainsKey(entry.groupId))
                {
                    entry.groupId = UngroupedGroupId;
                }

                string currentPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (pruneMissing && (string.IsNullOrEmpty(currentPath) || (!File.Exists(currentPath) && !Directory.Exists(currentPath))))
                {
                    _data.entries.RemoveAt(i);
                    continue;
                }

                if (!string.IsNullOrEmpty(currentPath) && string.IsNullOrEmpty(entry.fileName))
                {
                    entry.RefreshMetadata();
                }

                _entryByGuid[entry.guid] = entry;
            }
        }

        public static void Save(bool notify = true)
        {
            EnsureLoaded();
            RebuildLookupTables(pruneMissing: false);

            try
            {
                string json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(StoreFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Asset Organizer] Failed to save {StoreFilePath}: {ex.Message}");
            }

            if (notify)
            {
                NotifyChanged();
            }
        }

        public static void NotifyChanged()
        {
            OnStoreChanged?.Invoke();
            EditorApplication.RepaintProjectWindow();
        }

        public static bool IsPinned(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return false;
            EnsureLoaded();
            return _entryByGuid.ContainsKey(guid);
        }

        public static bool TryGetEntry(string guid, out PinnedAssetEntry entry)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(guid))
            {
                entry = null;
                return false;
            }
            return _entryByGuid.TryGetValue(guid, out entry);
        }

        public static PinnedAssetGroup GetGroup(string groupId)
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(groupId) && _groupById.TryGetValue(groupId, out PinnedAssetGroup group))
            {
                return group;
            }
            _groupById.TryGetValue(UngroupedGroupId, out PinnedAssetGroup fallback);
            return fallback;
        }

        public static Color GetGroupColor(string groupId)
        {
            PinnedAssetGroup group = GetGroup(groupId);
            return group != null ? group.color : DefaultYellowTagColor;
        }

        public static List<PinnedAssetGroup> GetCustomGroups()
        {
            EnsureLoaded();
            List<PinnedAssetGroup> result = new List<PinnedAssetGroup>();
            for (int i = 0; i < _data.groups.Count; i++)
            {
                PinnedAssetGroup g = _data.groups[i];
                if (g != null && !g.isDefaultUngrouped && g.id != UngroupedGroupId)
                {
                    result.Add(g);
                }
            }
            return result;
        }

        public static List<PinnedAssetEntry> GetEntriesForGroup(string groupId)
        {
            EnsureLoaded();
            string targetId = string.IsNullOrEmpty(groupId) ? UngroupedGroupId : groupId;
            List<PinnedAssetEntry> list = new List<PinnedAssetEntry>();
            for (int i = 0; i < _data.entries.Count; i++)
            {
                PinnedAssetEntry e = _data.entries[i];
                if (e != null && e.groupId == targetId)
                {
                    list.Add(e);
                }
            }
            return list;
        }

        public static PinnedAssetEntry PinAsset(string guid, string groupId = UngroupedGroupId, bool saveImmediately = true)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;

            EnsureLoaded();

            string targetGroupId = string.IsNullOrEmpty(groupId) || !_groupById.ContainsKey(groupId)
                ? UngroupedGroupId
                : groupId;

            // Duplicate-pin prevention: update group if already pinned
            if (_entryByGuid.TryGetValue(guid, out PinnedAssetEntry existing))
            {
                existing.groupId = targetGroupId;
                existing.RefreshMetadata();
                if (saveImmediately)
                    Save();
                return existing;
            }

            PinnedAssetEntry entry = new PinnedAssetEntry(guid, path, targetGroupId);
            _data.entries.Add(entry);
            _entryByGuid[guid] = entry;

            // Expand target group when an asset is added to it
            PinnedAssetGroup group = GetGroup(targetGroupId);
            if (group != null)
            {
                group.isExpanded = true;
            }

            if (saveImmediately)
                Save();

            return entry;
        }

        public static int PinAssets(IEnumerable<string> guids, string groupId = UngroupedGroupId)
        {
            if (guids == null)
                return 0;

            EnsureLoaded();
            int count = 0;
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid))
                    continue;
                if (PinAsset(guid, groupId, saveImmediately: false) != null)
                {
                    count++;
                }
            }

            if (count > 0)
            {
                Save();
            }
            return count;
        }

        public static bool UnpinAsset(string guid, bool saveImmediately = true)
        {
            if (string.IsNullOrEmpty(guid))
                return false;

            EnsureLoaded();
            if (!_entryByGuid.ContainsKey(guid))
                return false;

            _entryByGuid.Remove(guid);
            for (int i = _data.entries.Count - 1; i >= 0; i--)
            {
                if (_data.entries[i].guid == guid)
                {
                    _data.entries.RemoveAt(i);
                }
            }

            if (saveImmediately)
                Save();

            return true;
        }

        public static int UnpinAssets(IEnumerable<string> guids)
        {
            if (guids == null)
                return 0;

            EnsureLoaded();
            int removed = 0;
            foreach (string guid in guids)
            {
                if (UnpinAsset(guid, saveImmediately: false))
                {
                    removed++;
                }
            }

            if (removed > 0)
            {
                Save();
            }
            return removed;
        }

        /// <summary>
        /// Toggles pin state for the given GUIDs:
        /// If all provided GUIDs are already pinned, unpins them.
        /// Otherwise pins any unpinned GUIDs into the specified default group (Ungrouped by default).
        /// Returns true if assets were pinned, false if assets were unpinned.
        /// </summary>
        public static bool TogglePinAssets(IReadOnlyList<string> guids, out List<PinnedAssetEntry> newlyPinned, string defaultGroupId = UngroupedGroupId)
        {
            newlyPinned = new List<PinnedAssetEntry>();
            if (guids == null || guids.Count == 0)
                return false;

            EnsureLoaded();

            bool allAlreadyPinned = true;
            for (int i = 0; i < guids.Count; i++)
            {
                if (!IsPinned(guids[i]))
                {
                    allAlreadyPinned = false;
                    break;
                }
            }

            if (allAlreadyPinned)
            {
                for (int i = 0; i < guids.Count; i++)
                {
                    UnpinAsset(guids[i], saveImmediately: false);
                }
                Save();
                return false;
            }
            else
            {
                for (int i = 0; i < guids.Count; i++)
                {
                    if (!IsPinned(guids[i]))
                    {
                        PinnedAssetEntry entry = PinAsset(guids[i], defaultGroupId, saveImmediately: false);
                        if (entry != null)
                        {
                            newlyPinned.Add(entry);
                        }
                    }
                }
                Save();
                return true;
            }
        }

        public static PinnedAssetGroup CreateGroup(string groupName, Color color)
        {
            EnsureLoaded();
            string trimmed = string.IsNullOrWhiteSpace(groupName) ? "New Group" : groupName.Trim();

            // If a group with the exact same name exists (case-insensitive), return it and update color
            for (int i = 0; i < _data.groups.Count; i++)
            {
                if (string.Equals(_data.groups[i].name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    _data.groups[i].color = color;
                    Save();
                    return _data.groups[i];
                }
            }

            string newId = Guid.NewGuid().ToString("N");
            PinnedAssetGroup newGroup = new PinnedAssetGroup(newId, trimmed, color, isDefaultUngrouped: false);
            _data.groups.Add(newGroup);
            _groupById[newId] = newGroup;
            Save();
            return newGroup;
        }

        public static void RenameGroup(string groupId, string newName)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrWhiteSpace(newName))
                return;

            EnsureLoaded();
            PinnedAssetGroup group = GetGroup(groupId);
            if (group != null && !group.isDefaultUngrouped)
            {
                group.name = newName.Trim();
                Save();
            }
        }

        public static void SetGroupColor(string groupId, Color newColor)
        {
            EnsureLoaded();
            PinnedAssetGroup group = GetGroup(groupId);
            if (group != null)
            {
                newColor.a = 1f;
                if (group.color != newColor)
                {
                    group.color = newColor;
                    Save();
                }
            }
        }

        public static void DeleteGroup(string groupId, bool unpinContainedAssets = false)
        {
            if (string.IsNullOrEmpty(groupId) || groupId == UngroupedGroupId)
                return;

            EnsureLoaded();
            for (int i = _data.entries.Count - 1; i >= 0; i--)
            {
                if (_data.entries[i].groupId == groupId)
                {
                    if (unpinContainedAssets)
                    {
                        _entryByGuid.Remove(_data.entries[i].guid);
                        _data.entries.RemoveAt(i);
                    }
                    else
                    {
                        _data.entries[i].groupId = UngroupedGroupId;
                    }
                }
            }

            _groupById.Remove(groupId);
            for (int i = _data.groups.Count - 1; i >= 0; i--)
            {
                if (_data.groups[i].id == groupId)
                {
                    _data.groups.RemoveAt(i);
                }
            }

            Save();
        }

        public static void ReorderGroup(int fromIndex, int toIndex)
        {
            EnsureLoaded();
            if (fromIndex < 0 || fromIndex >= _data.groups.Count)
                return;

            toIndex = Mathf.Clamp(toIndex, 0, _data.groups.Count - 1);
            if (fromIndex == toIndex)
                return;

            PinnedAssetGroup item = _data.groups[fromIndex];
            _data.groups.RemoveAt(fromIndex);
            _data.groups.Insert(toIndex, item);
            Save();
        }

        public static void MoveAssetToGroup(string guid, string targetGroupId, string insertBeforeGuid = null)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            EnsureLoaded();
            string resolvedGroupId = string.IsNullOrEmpty(targetGroupId) || !_groupById.ContainsKey(targetGroupId)
                ? UngroupedGroupId
                : targetGroupId;

            if (!_entryByGuid.TryGetValue(guid, out PinnedAssetEntry entry))
            {
                PinAsset(guid, resolvedGroupId, saveImmediately: true);
                return;
            }

            entry.groupId = resolvedGroupId;
            PinnedAssetGroup targetGroup = GetGroup(resolvedGroupId);
            if (targetGroup != null)
            {
                targetGroup.isExpanded = true;
            }

            if (!string.IsNullOrEmpty(insertBeforeGuid) && insertBeforeGuid != guid)
            {
                _data.entries.Remove(entry);
                int targetIdx = _data.entries.FindIndex(e => e.guid == insertBeforeGuid);
                if (targetIdx >= 0)
                    _data.entries.Insert(targetIdx, entry);
                else
                    _data.entries.Add(entry);
            }
            else
            {
                // Move to end of list so it appears at the bottom of the target group
                _data.entries.Remove(entry);
                _data.entries.Add(entry);
            }

            Save();
        }

        public static void RefreshAllMetadata()
        {
            EnsureLoaded();
            bool anyRemoved = false;
            for (int i = _data.entries.Count - 1; i >= 0; i--)
            {
                PinnedAssetEntry entry = _data.entries[i];
                string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (string.IsNullOrEmpty(path) || (!File.Exists(path) && !Directory.Exists(path)))
                {
                    _entryByGuid.Remove(entry.guid);
                    _data.entries.RemoveAt(i);
                    anyRemoved = true;
                    continue;
                }
                entry.RefreshMetadata();
            }

            if (anyRemoved)
                Save();
            else
                NotifyChanged();
        }

        public static void OnAssetsChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets)
        {
            EnsureLoaded();
            bool modified = false;

            if (deletedAssets != null && deletedAssets.Length > 0)
            {
                for (int i = _data.entries.Count - 1; i >= 0; i--)
                {
                    PinnedAssetEntry entry = _data.entries[i];
                    string currentPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    bool wasDeleted = false;
                    for (int d = 0; d < deletedAssets.Length; d++)
                    {
                        if (string.Equals(entry.assetPath, deletedAssets[d], StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(currentPath, deletedAssets[d], StringComparison.OrdinalIgnoreCase))
                        {
                            wasDeleted = true;
                            break;
                        }
                    }

                    if (wasDeleted || string.IsNullOrEmpty(currentPath))
                    {
                        _entryByGuid.Remove(entry.guid);
                        _data.entries.RemoveAt(i);
                        modified = true;
                    }
                }
            }

            if (movedAssets != null && movedAssets.Length > 0)
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    string guid = AssetDatabase.AssetPathToGUID(movedAssets[i]);
                    if (!string.IsNullOrEmpty(guid) && _entryByGuid.TryGetValue(guid, out PinnedAssetEntry entry))
                    {
                        entry.RefreshMetadata();
                        modified = true;
                    }
                }
            }

            if (importedAssets != null && importedAssets.Length > 0)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    string guid = AssetDatabase.AssetPathToGUID(importedAssets[i]);
                    if (!string.IsNullOrEmpty(guid) && _entryByGuid.TryGetValue(guid, out PinnedAssetEntry entry))
                    {
                        entry.RefreshMetadata();
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                Save();
            }
        }

        public static long GetTotalPinnedSizeBytes()
        {
            EnsureLoaded();
            long total = 0L;
            for (int i = 0; i < _data.entries.Count; i++)
            {
                total += _data.entries[i].fileSizeBytes;
            }
            return total;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "0 B";
            if (bytes < 1024L)
                return $"{bytes} B";
            if (bytes < 1024L * 1024L)
                return $"{(bytes / 1024.0):F1} KB";
            if (bytes < 1024L * 1024L * 1024L)
                return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
            return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
        }
    }
}

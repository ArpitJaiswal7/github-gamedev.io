using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Provides Project Window right-click context menus under "Assets/Asset Organizer"
    /// (supporting both single and multi-asset selection) and handles the global Ctrl+Win+P
    /// shortcut to toggle Pin/Unpin on all selected Project assets together.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowContextMenu
    {
#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_CONTROL = 0x11;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_P = 0x50;
#endif

        private static bool _wasComboDown;
        private static double _lastToggleTime;
        private static double _lastContextMenuActionTime;

        static ProjectWindowContextMenu()
        {
            EditorApplication.update -= PollCtrlWinPShortcut;
            EditorApplication.update += PollCtrlWinPShortcut;
        }

        private static void PollCtrlWinPShortcut()
        {
#if UNITY_EDITOR_WIN
            if (!InternalEditorUtility.isApplicationActive)
            {
                _wasComboDown = false;
                return;
            }

            bool ctrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
            bool winDown = ((GetAsyncKeyState(VK_LWIN) & 0x8000) != 0) || ((GetAsyncKeyState(VK_RWIN) & 0x8000) != 0);
            bool pDown = (GetAsyncKeyState(VK_P) & 0x8000) != 0;

            bool isComboDown = ctrlDown && winDown && pDown;
            if (isComboDown && !_wasComboDown)
            {
                _wasComboDown = true;
                TriggerTogglePinShortcut();
            }
            else if (!isComboDown)
            {
                _wasComboDown = false;
            }
#endif
        }

        private static void TriggerTogglePinShortcut()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastToggleTime < 0.25)
                return;

            List<string> guids = GetSelectedProjectAssetGuids();
            if (guids.Count == 0)
                return;

            _lastToggleTime = now;
            TogglePinForGuids(guids);
        }

        /// <summary>
        /// Returns all currently selected Project Window asset GUIDs (supporting multi-selection
        /// across both 1-column tree view and 2-column grid/list view).
        /// </summary>
        public static List<string> GetSelectedProjectAssetGuids()
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>();

            string[] selectedGuids = Selection.assetGUIDs;
            if (selectedGuids != null && selectedGuids.Length > 0)
            {
                for (int i = 0; i < selectedGuids.Length; i++)
                {
                    string guid = selectedGuids[i];
                    if (!string.IsNullOrEmpty(guid) && seen.Add(guid))
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets"))
                        {
                            result.Add(guid);
                        }
                    }
                }
            }

            Object[] selectedObjects = Selection.objects;
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    Object obj = selectedObjects[i];
                    if (obj == null)
                        continue;

                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets"))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (!string.IsNullOrEmpty(guid) && seen.Add(guid))
                        {
                            result.Add(guid);
                        }
                    }
                }
            }

            return result;
        }

        public static void TogglePinForGuids(IReadOnlyList<string> guids)
        {
            if (guids == null || guids.Count == 0)
                return;

            bool pinned = PinnedAssetsStore.TogglePinAssets(guids, out List<PinnedAssetEntry> newlyPinned, PinnedAssetsStore.UngroupedGroupId);
            if (pinned && newlyPinned.Count > 0)
            {
                AssetOrganizerWindow.OpenAndHighlight(newlyPinned[0].guid, pingInProject: true);
            }
            else
            {
                AssetOrganizerWindow.RepaintIfOpen();
            }
        }

        private static bool ShouldExecuteBatchContextMenuAction()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastContextMenuActionTime < 0.15)
                return false;

            _lastContextMenuActionTime = now;
            return true;
        }

        // =====================================================================
        // Project Window Context Menu Items ("Assets/Asset Organizer/...")
        // =====================================================================

        [MenuItem("Assets/Asset Organizer/Pin Asset (Ctrl+Win+P)", false, 1100)]
        private static void MenuPinOrToggleAsset()
        {
            if (!ShouldExecuteBatchContextMenuAction())
                return;

            List<string> guids = GetSelectedProjectAssetGuids();
            if (guids.Count == 0)
                return;

            TogglePinForGuids(guids);
        }

        [MenuItem("Assets/Asset Organizer/Pin Asset (Ctrl+Win+P)", true)]
        private static bool ValidateMenuPinOrToggleAsset()
        {
            return GetSelectedProjectAssetGuids().Count > 0;
        }

        [MenuItem("Assets/Asset Organizer/Pin and Add to Group...", false, 1101)]
        private static void MenuPinAndAddToGroup()
        {
            if (!ShouldExecuteBatchContextMenuAction())
                return;

            List<string> guids = GetSelectedProjectAssetGuids();
            if (guids.Count == 0)
                return;

            GroupPopupWindow.ShowForPinAndAddToGroup(guids);
        }

        [MenuItem("Assets/Asset Organizer/Pin and Add to Group...", true)]
        private static bool ValidateMenuPinAndAddToGroup()
        {
            return GetSelectedProjectAssetGuids().Count > 0;
        }

        [MenuItem("Assets/Asset Organizer/Unpin Asset", false, 1102)]
        private static void MenuUnpinSelectedAssets()
        {
            if (!ShouldExecuteBatchContextMenuAction())
                return;

            List<string> guids = GetSelectedProjectAssetGuids();
            if (guids.Count == 0)
                return;

            PinnedAssetsStore.UnpinAssets(guids);
            AssetOrganizerWindow.RepaintIfOpen();
        }

        [MenuItem("Assets/Asset Organizer/Unpin Asset", true)]
        private static bool ValidateMenuUnpinSelectedAssets()
        {
            List<string> guids = GetSelectedProjectAssetGuids();
            for (int i = 0; i < guids.Count; i++)
            {
                if (PinnedAssetsStore.IsPinned(guids[i]))
                    return true;
            }
            return false;
        }

        [MenuItem("Assets/Asset Organizer/Copy Full Disk Path (C)", false, 1110)]
        private static void MenuCopyFullDiskPath()
        {
            if (!ShouldExecuteBatchContextMenuAction())
                return;

            List<string> guids = GetSelectedProjectAssetGuids();
            if (guids.Count == 0)
                return;

            OSFileExplorerUtility.CopySingleAssetFullDiskPath(guids[0]);
        }

        [MenuItem("Assets/Asset Organizer/Copy Full Disk Path (C)", true)]
        private static bool ValidateMenuCopyFullDiskPath()
        {
            return GetSelectedProjectAssetGuids().Count > 0;
        }

        [MenuItem("Assets/Asset Organizer/Reveal Selected in Explorer or Finder (Ctrl+Click Tag)", false, 1111)]
        private static void MenuRevealSelectedInExplorer()
        {
            if (!ShouldExecuteBatchContextMenuAction())
                return;

            List<string> guids = GetSelectedProjectAssetGuids();
            if (guids.Count == 0)
                return;

            OSFileExplorerUtility.RevealAssetsInExplorerOrFinder(guids);
        }

        [MenuItem("Assets/Asset Organizer/Reveal Selected in Explorer or Finder (Ctrl+Click Tag)", true)]
        private static bool ValidateMenuRevealSelectedInExplorer()
        {
            return GetSelectedProjectAssetGuids().Count > 0;
        }

        [MenuItem("Assets/Asset Organizer/Open Pinned Assets Window", false, 1125)]
        private static void MenuOpenWindow()
        {
            AssetOrganizerWindow.ShowWindow();
        }
    }
}

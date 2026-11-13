using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Modal / utility window for:
    /// 1) Prompting the user to create a group (with name and tag color) when no custom groups exist,
    ///    or when "+ New Group" is clicked.
    /// 2) Selecting an existing group or creating a new group when "Pin and Add to Group" is invoked.
    /// </summary>
    public sealed class GroupPopupWindow : EditorWindow
    {
        public enum Mode
        {
            CreateNewGroupOnly,
            SelectOrCreateGroup
        }

        private Mode _mode = Mode.CreateNewGroupOnly;
        private List<string> _targetGuids = new List<string>();
        private string _newGroupName = "New Group";
        private Color _newGroupColor = PinnedAssetsStore.DefaultYellowTagColor;
        private Vector2 _scrollPos;
        private bool _focusedInput;
        private Action<PinnedAssetGroup> _onGroupSelectedOrCreated;

        private const string InputControlName = "AssetOrganizer_GroupNameField";

        /// <summary>
        /// Opens the "Pin and Add to Group" window for the given asset GUIDs.
        /// If no custom groups exist, opens directly in Create Group prompt mode.
        /// </summary>
        public static void ShowForPinAndAddToGroup(IReadOnlyList<string> guids)
        {
            List<PinnedAssetGroup> customGroups = PinnedAssetsStore.GetCustomGroups();
            Mode mode = customGroups.Count == 0 ? Mode.CreateNewGroupOnly : Mode.SelectOrCreateGroup;

            GroupPopupWindow window = GetWindow<GroupPopupWindow>( utility: true,
                title: mode == Mode.CreateNewGroupOnly ? "Create Group & Pin Asset" : "Pin and Add to Group",
                focus: true);

            window._mode = mode;
            window._targetGuids = guids != null ? new List<string>(guids) : new List<string>();
            window._newGroupName = $"Group {customGroups.Count + 1}";
            window._newGroupColor = PinnedAssetsStore.GetSuggestedNewGroupColor();
            window._focusedInput = false;
            window._onGroupSelectedOrCreated = null;

            Vector2 size = mode == Mode.CreateNewGroupOnly ? new Vector2(360f, 175f) : new Vector2(380f, 340f);
            window.minSize = size;
            window.maxSize = new Vector2(520f, 520f);
            window.ShowUtility();
            window.CenterOnMainWin();
        }

        /// <summary>
        /// Opens a prompt solely to create a new group (optionally invoking a callback when created).
        /// </summary>
        public static void ShowCreateGroupPrompt(Action<PinnedAssetGroup> onCreated = null)
        {
            List<PinnedAssetGroup> customGroups = PinnedAssetsStore.GetCustomGroups();
            GroupPopupWindow window = GetWindow<GroupPopupWindow>(utility: true, title: "Create Pinned Asset Group", focus: true);

            window._mode = Mode.CreateNewGroupOnly;
            window._targetGuids.Clear();
            window._newGroupName = $"Group {customGroups.Count + 1}";
            window._newGroupColor = PinnedAssetsStore.GetSuggestedNewGroupColor();
            window._focusedInput = false;
            window._onGroupSelectedOrCreated = onCreated;

            Vector2 size = new Vector2(360f, 165f);
            window.minSize = size;
            window.maxSize = new Vector2(480f, 240f);
            window.ShowUtility();
            window.CenterOnMainWin();
        }

        private void CenterOnMainWin()
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Rect pos = position;
            pos.x = main.x + (main.width - pos.width) * 0.5f;
            pos.y = main.y + (main.height - pos.height) * 0.5f;
            position = pos;
        }

        private void OnGUI()
        {
            Event evt = Event.current;
            if (evt != null && evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    ConfirmCreateGroup();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(8f);

            if (_mode == Mode.SelectOrCreateGroup)
            {
                DrawSelectExistingGroupSection();
                EditorGUILayout.Space(8f);
                DrawHorizontalDivider();
                EditorGUILayout.Space(6f);
            }

            DrawCreateNewGroupSection();
        }

        private void DrawSelectExistingGroupSection()
        {
            int assetCount = _targetGuids != null ? _targetGuids.Count : 0;
            EditorGUILayout.LabelField(
                assetCount > 0
                    ? $"Select a group to pin {assetCount} selected asset(s):"
                    : "Select a target group:",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(4f);

            IReadOnlyList<PinnedAssetGroup> allGroups = PinnedAssetsStore.Groups;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(155f));

            for (int i = 0; i < allGroups.Count; i++)
            {
                PinnedAssetGroup group = allGroups[i];
                if (group == null)
                    continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Circular tag preview
                Rect dotRect = GUILayoutUtility.GetRect(16f, 20f, GUILayout.Width(16f));
                Rect centeredDot = new Rect(dotRect.x + 2f, dotRect.y + (dotRect.height - 12f) * 0.5f, 12f, 12f);
                ProjectWindowBadge.DrawCircularTag(centeredDot, group.color);

                // Inline color picker before group name
                EditorGUI.BeginChangeCheck();
                Color newCol = EditorGUILayout.ColorField(GUIContent.none, group.color, false, false, false, GUILayout.Width(34f), GUILayout.Height(18f));
                if (EditorGUI.EndChangeCheck())
                {
                    PinnedAssetsStore.SetGroupColor(group.id, newCol);
                }

                int countInGroup = PinnedAssetsStore.GetEntriesForGroup(group.id).Count;
                string buttonLabel = $"{group.name}  ({countInGroup})";

                if (GUILayout.Button(buttonLabel, EditorStyles.miniButton, GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                {
                    AssignTargetAssetsToGroup(group);
                    Close();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCreateNewGroupSection()
        {
            List<PinnedAssetGroup> customGroups = PinnedAssetsStore.GetCustomGroups();
            if (_mode == Mode.CreateNewGroupOnly && customGroups.Count == 0 && _targetGuids.Count > 0)
            {
                EditorGUILayout.HelpBox("No custom groups exist yet. Create a group below to pin your selected asset(s).", MessageType.Info);
                EditorGUILayout.Space(4f);
            }
            else
            {
                EditorGUILayout.LabelField("Create New Group", EditorStyles.boldLabel);
                EditorGUILayout.Space(2f);
            }

            // Row with Color Picker right before Group Name input
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tag & Name:", GUILayout.Width(80f));

            Rect tagPreviewRect = GUILayoutUtility.GetRect(16f, 20f, GUILayout.Width(16f));
            Rect centeredPreview = new Rect(tagPreviewRect.x + 1f, tagPreviewRect.y + 4f, 12f, 12f);
            ProjectWindowBadge.DrawCircularTag(centeredPreview, _newGroupColor);

            _newGroupColor = EditorGUILayout.ColorField(GUIContent.none, _newGroupColor, true, false, false, GUILayout.Width(42f));

            GUI.SetNextControlName(InputControlName);
            _newGroupName = EditorGUILayout.TextField(_newGroupName);
            EditorGUILayout.EndHorizontal();

            if (!_focusedInput && Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl(InputControlName);
                _focusedInput = true;
            }

            EditorGUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(85f), GUILayout.Height(24f)))
            {
                Close();
                GUIUtility.ExitGUI();
            }

            string confirmText = _targetGuids != null && _targetGuids.Count > 0
                ? $"Create & Pin ({_targetGuids.Count})"
                : "Create Group";

            GUI.enabled = !string.IsNullOrWhiteSpace(_newGroupName);
            if (GUILayout.Button(confirmText, GUILayout.Width(135f), GUILayout.Height(24f)))
            {
                ConfirmCreateGroup();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void ConfirmCreateGroup()
        {
            if (string.IsNullOrWhiteSpace(_newGroupName))
                return;

            PinnedAssetGroup created = PinnedAssetsStore.CreateGroup(_newGroupName.Trim(), _newGroupColor);
            AssignTargetAssetsToGroup(created);
            _onGroupSelectedOrCreated?.Invoke(created);
            Close();
        }

        private void AssignTargetAssetsToGroup(PinnedAssetGroup group)
        {
            if (group == null)
                return;

            if (_targetGuids != null && _targetGuids.Count > 0)
            {
                PinnedAssetsStore.PinAssets(_targetGuids, group.id);
                AssetOrganizerWindow.OpenAndHighlight(_targetGuids[0]);
            }
        }

        private static void DrawHorizontalDivider()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.35f));
        }
    }
}

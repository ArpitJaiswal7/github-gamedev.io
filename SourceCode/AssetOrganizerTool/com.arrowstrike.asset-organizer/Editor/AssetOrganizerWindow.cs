using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Unity Editor Window ("Pinned Assets") for browsing, searching, grouping,
    /// reordering, and pinging pinned project assets.
    /// </summary>
    public sealed class AssetOrganizerWindow : EditorWindow
    {
        private const string DragGroupIndexKey = "AssetOrganizer_DragGroupIndex";
        private const string DragPinnedGuidKey = "AssetOrganizer_DragPinnedGuid";

        private enum SortColumn
        {
            None,
            Path,
            FileName,
            Type,
            Size
        }

        private string _searchQuery = string.Empty;
        private string _selectedTypeFilter = "All";
        private SortColumn _sortColumn = SortColumn.None;
        private bool _sortAscending = true;

        private Vector2 _scrollPosition;
        private string _selectedAssetGuid;

        // Inline group renaming state
        private string _renamingGroupId;
        private string _renamingGroupBuffer = string.Empty;

        // Drag state tracking
        private Vector2 _mouseDownPos;
        private string _potentialDragAssetGuid;
        private int _potentialDragGroupIndex = -1;
        private string _hoveredDropGroupId;
        private int _hoveredReorderGroupTargetIndex = -1;

        // Cached GUI styles
        private GUIStyle _headerToolbarButtonStyle;
        private GUIStyle _groupHeaderBoxStyle;
        private GUIStyle _rowEvenStyle;
        private GUIStyle _rowOddStyle;
        private GUIStyle _rowSelectedStyle;
        private GUIStyle _richPathStyle;
        private GUIStyle _columnHeaderStyle;
        private GUIStyle _bottomPillButtonStyle;
        private GUIStyle _dragHandleStyle;
        private bool _stylesInitialized;

        [MenuItem("Tools/Asset Organizer/Pinned Assets Window", false, 100)]
        [MenuItem("Window/Asset Organizer/Pinned Assets", false, 100)]
        public static AssetOrganizerWindow ShowWindow()
        {
            AssetOrganizerWindow window = GetWindow<AssetOrganizerWindow>("Pinned Assets");
            window.titleContent = new GUIContent("Pinned Assets", EditorGUIUtility.IconContent("d_Favorite").image);
            window.minSize = new Vector2(560f, 340f);
            window.Show();
            return window;
        }

        public static void OpenAndHighlight(string guid, bool pingInProject = true)
        {
            AssetOrganizerWindow window = GetWindow<AssetOrganizerWindow>("Pinned Assets", focus: false);
            window.titleContent = new GUIContent("Pinned Assets", EditorGUIUtility.IconContent("d_Favorite").image);
            window.minSize = new Vector2(560f, 340f);
            window._selectedAssetGuid = guid;

            if (pingInProject && !string.IsNullOrEmpty(guid))
            {
                PingAssetByGuid(guid, selectObject: false);
            }

            window.Repaint();
        }

        public static void RepaintIfOpen()
        {
            if (HasOpenInstances<AssetOrganizerWindow>())
            {
                AssetOrganizerWindow window = GetWindow<AssetOrganizerWindow>("Pinned Assets", focus: false);
                window.Repaint();
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Pinned Assets", EditorGUIUtility.IconContent("d_Favorite").image);
            PinnedAssetsStore.EnsureLoaded();
            PinnedAssetsStore.OnStoreChanged -= OnStoreChanged;
            PinnedAssetsStore.OnStoreChanged += OnStoreChanged;
        }

        private void OnDisable()
        {
            PinnedAssetsStore.OnStoreChanged -= OnStoreChanged;
        }

        private void OnStoreChanged()
        {
            Repaint();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized && _richPathStyle != null)
                return;

            _headerToolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };

            _groupHeaderBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(4, 4, 4, 0)
            };

            _rowEvenStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(6, 6, 2, 2)
            };

            _rowOddStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(6, 6, 2, 2)
            };

            _rowSelectedStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(6, 6, 2, 2)
            };

            _richPathStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                fontSize = 12
            };

            _columnHeaderStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                padding = new RectOffset(6, 6, 0, 0)
            };

            _bottomPillButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 8, 2, 2),
                fixedHeight = 22f
            };

            _dragHandleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            PinnedAssetsStore.EnsureLoaded();

            DrawTopToolbar();
            DrawFilterAndSearchBar();
            DrawColumnHeaders();

            _hoveredDropGroupId = null;
            _hoveredReorderGroupTargetIndex = -1;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawGroupsAndAssets();
            DrawWindowDropZoneFallback();
            EditorGUILayout.EndScrollView();

            DrawBottomStatusBar();

            HandleGlobalDragCleanup();
        }

        // =====================================================================
        // Top Toolbars & Search Bar
        // =====================================================================

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(new GUIContent(" + New Group", "Create a new custom group with color tag"), _headerToolbarButtonStyle, GUILayout.Width(95f)))
            {
                GroupPopupWindow.ShowCreateGroupPrompt();
            }

            List<string> selectedProjectGuids = ProjectWindowContextMenu.GetSelectedProjectAssetGuids();
            GUI.enabled = selectedProjectGuids.Count > 0;
            if (GUILayout.Button(new GUIContent(" Pin Selected", "Pin currently selected Project asset(s) (Ctrl+Win+P)"), _headerToolbarButtonStyle, GUILayout.Width(95f)))
            {
                ProjectWindowContextMenu.TogglePinForGuids(selectedProjectGuids);
            }

            if (GUILayout.Button(new GUIContent(" Pin to Group...", "Pin currently selected Project asset(s) into a specific group"), _headerToolbarButtonStyle, GUILayout.Width(100f)))
            {
                GroupPopupWindow.ShowForPinAndAddToGroup(selectedProjectGuids);
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Expand All", "Expand all groups"), _headerToolbarButtonStyle, GUILayout.Width(74f)))
            {
                SetAllGroupsExpanded(true);
            }

            if (GUILayout.Button(new GUIContent("Collapse All", "Collapse all groups"), _headerToolbarButtonStyle, GUILayout.Width(78f)))
            {
                SetAllGroupsExpanded(false);
            }

            if (GUILayout.Button(new GUIContent("Refresh", "Recalculate asset paths, types, and file sizes"), _headerToolbarButtonStyle, GUILayout.Width(62f)))
            {
                PinnedAssetsStore.RefreshAllMetadata();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterAndSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Type Filter Popup (Build Report Tool style: "Filter: All (N)")
            List<string> typeOptions = BuildTypeFilterOptions(out List<string> rawTypes, out int currentFilterIndex);
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(38f));
            int newFilterIdx = EditorGUILayout.Popup(currentFilterIndex, typeOptions.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(145f));
            if (newFilterIdx >= 0 && newFilterIdx < rawTypes.Count)
            {
                _selectedTypeFilter = rawTypes[newFilterIdx];
            }

            GUILayout.Space(6f);

            // Search Bar for searching assets by name or path
            GUIStyle searchFieldStyle = GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
            GUIStyle cancelBtnStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? EditorStyles.toolbarButton;

            _searchQuery = EditorGUILayout.TextField(_searchQuery, searchFieldStyle, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                if (GUILayout.Button(GUIContent.none, cancelBtnStyle))
                {
                    _searchQuery = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            else
            {
                // Draw subtle placeholder inside the search bar
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.Repaint)
                {
                    Color prev = GUI.color;
                    GUI.color = new Color(0.65f, 0.65f, 0.65f, 0.7f);
                    GUI.Label(new Rect(lastRect.x + 18f, lastRect.y, lastRect.width - 24f, lastRect.height), "Search pinned assets by name or path...", EditorStyles.miniLabel);
                    GUI.color = prev;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private List<string> BuildTypeFilterOptions(out List<string> rawTypes, out int selectedIndex)
        {
            IReadOnlyList<PinnedAssetEntry> allEntries = PinnedAssetsStore.Entries;
            Dictionary<string, int> countsByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < allEntries.Count; i++)
            {
                string t = string.IsNullOrEmpty(allEntries[i].assetType) ? "Asset" : allEntries[i].assetType;
                countsByType.TryGetValue(t, out int count);
                countsByType[t] = count + 1;
            }

            List<string> labels = new List<string> { $"All ({allEntries.Count})" };
            rawTypes = new List<string> { "All" };
            selectedIndex = 0;

            foreach (KeyValuePair<string, int> kvp in countsByType)
            {
                if (string.Equals(kvp.Key, _selectedTypeFilter, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = rawTypes.Count;
                }
                rawTypes.Add(kvp.Key);
                labels.Add($"{kvp.Key} ({kvp.Value})");
            }

            if (selectedIndex == 0 && !string.Equals(_selectedTypeFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                _selectedTypeFilter = "All";
            }

            return labels;
        }

        private void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Ping", _columnHeaderStyle, GUILayout.Width(42f));
            GUILayout.Label("Tag", _columnHeaderStyle, GUILayout.Width(28f));

            DrawSortableColumnHeader("Asset Path / Filename", SortColumn.Path, GUILayout.ExpandWidth(true));
            DrawSortableColumnHeader("Type", SortColumn.Type, GUILayout.Width(110f));
            DrawSortableColumnHeader("Size", SortColumn.Size, GUILayout.Width(76f));
            GUILayout.Label("Percent", _columnHeaderStyle, GUILayout.Width(58f));
            GUILayout.Label("", _columnHeaderStyle, GUILayout.Width(24f));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSortableColumnHeader(string title, SortColumn column, params GUILayoutOption[] options)
        {
            string arrow = string.Empty;
            if (_sortColumn == column)
            {
                arrow = _sortAscending ? " ▲" : " ▼";
            }

            if (GUILayout.Button(title + arrow, _columnHeaderStyle, options))
            {
                if (_sortColumn == column)
                {
                    if (!_sortAscending)
                    {
                        _sortColumn = SortColumn.None;
                    }
                    else
                    {
                        _sortAscending = false;
                    }
                }
                else
                {
                    _sortColumn = column;
                    _sortAscending = true;
                }
            }
        }

        // =====================================================================
        // Groups & Asset Rows Rendering
        // =====================================================================

        private void DrawGroupsAndAssets()
        {
            IReadOnlyList<PinnedAssetGroup> groups = PinnedAssetsStore.Groups;
            long totalPinnedBytes = Math.Max(1L, PinnedAssetsStore.GetTotalPinnedSizeBytes());

            for (int gIdx = 0; gIdx < groups.Count; gIdx++)
            {
                PinnedAssetGroup group = groups[gIdx];
                if (group == null)
                    continue;

                List<PinnedAssetEntry> groupEntries = GetFilteredAndSortedEntries(group.id);

                // If searching by name/filter, hide empty groups unless it's the only group or not searching
                bool isFiltering = !string.IsNullOrWhiteSpace(_searchQuery) || !string.Equals(_selectedTypeFilter, "All", StringComparison.OrdinalIgnoreCase);
                if (isFiltering && groupEntries.Count == 0 && groups.Count > 1)
                    continue;

                DrawSingleGroup(group, gIdx, groupEntries, totalPinnedBytes);
                EditorGUILayout.Space(4f);
            }
        }

        private void DrawSingleGroup(PinnedAssetGroup group, int groupIndex, List<PinnedAssetEntry> visibleEntries, long totalPinnedBytes)
        {
            long groupSizeBytes = 0L;
            for (int i = 0; i < visibleEntries.Count; i++)
            {
                groupSizeBytes += visibleEntries[i].fileSizeBytes;
            }

            // Outer container for group
            Rect groupStartRect = EditorGUILayout.BeginVertical(_groupHeaderBoxStyle);

            // Draw group header bar
            Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24f));

            // Left accent stripe in group color
            if (Event.current.type == EventType.Repaint)
            {
                Rect accentRect = new Rect(headerRect.x - 4f, headerRect.y - 2f, 4f, headerRect.height + 4f);
                EditorGUI.DrawRect(accentRect, group.color);
            }

            // 1. Group Drag Handle (for dragging groups to reorder)
            Rect dragHandleRect = GUILayoutUtility.GetRect(new GUIContent("≡", "Drag to reorder group"), _dragHandleStyle, GUILayout.Width(18f), GUILayout.Height(20f));
            EditorGUIUtility.AddCursorRect(dragHandleRect, MouseCursor.Pan);
            GUI.Label(dragHandleRect, new GUIContent("≡", "Drag to reorder group"), _dragHandleStyle);

            // 2. Foldout Toggle
            bool newExpanded = EditorGUILayout.Foldout(group.isExpanded, GUIContent.none, true);
            if (newExpanded != group.isExpanded)
            {
                group.isExpanded = newExpanded;
                PinnedAssetsStore.Save(notify: false);
            }

            // 3. Circular Tag Badge + Color Picker right before Group Name
            Rect tagDotRect = GUILayoutUtility.GetRect(16f, 20f, GUILayout.Width(16f));
            Rect centeredDot = new Rect(tagDotRect.x + 2f, tagDotRect.y + 4f, 12f, 12f);
            ProjectWindowBadge.DrawCircularTag(centeredDot, group.color);

            EditorGUI.BeginChangeCheck();
            Color updatedColor = EditorGUILayout.ColorField(GUIContent.none, group.color, true, false, false, GUILayout.Width(38f), GUILayout.Height(18f));
            if (EditorGUI.EndChangeCheck())
            {
                PinnedAssetsStore.SetGroupColor(group.id, updatedColor);
            }

            GUILayout.Space(4f);

            // 4. Group Name (or inline rename text field)
            if (_renamingGroupId == group.id && !group.isDefaultUngrouped)
            {
                _renamingGroupBuffer = EditorGUILayout.TextField(_renamingGroupBuffer, GUILayout.Width(160f));
                if (GUILayout.Button("Save", EditorStyles.miniButton, GUILayout.Width(44f)))
                {
                    PinnedAssetsStore.RenameGroup(group.id, _renamingGroupBuffer);
                    _renamingGroupId = null;
                }
                if (GUILayout.Button("Cancel", EditorStyles.miniButton, GUILayout.Width(52f)))
                {
                    _renamingGroupId = null;
                }
            }
            else
            {
                string groupTitle = $"{group.name}  ({visibleEntries.Count})";
                GUILayout.Label(new GUIContent(groupTitle, "Click to expand/collapse, or drag header to reorder groups. Drag assets here to pin into this group."), EditorStyles.boldLabel);
            }

            GUILayout.FlexibleSpace();

            // Group total size badge
            GUILayout.Label(PinnedAssetsStore.FormatBytes(groupSizeBytes), EditorStyles.miniLabel, GUILayout.Width(68f));

            // Rename button for custom groups
            if (!group.isDefaultUngrouped && _renamingGroupId != group.id)
            {
                if (GUILayout.Button(new GUIContent("Rename", "Rename this group"), EditorStyles.miniButton, GUILayout.Width(54f)))
                {
                    _renamingGroupId = group.id;
                    _renamingGroupBuffer = group.name;
                }

                if (GUILayout.Button(new GUIContent("×", "Delete group (moves pinned assets back to Ungrouped)"), EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    int choice = EditorUtility.DisplayDialogComplex(
                        $"Delete Group '{group.name}'",
                        $"What would you like to do with the {visibleEntries.Count} asset(s) inside '{group.name}'?",
                        "Move to Ungrouped",
                        "Cancel",
                        "Unpin Assets");

                    if (choice == 0)
                    {
                        PinnedAssetsStore.DeleteGroup(group.id, unpinContainedAssets: false);
                        GUIUtility.ExitGUI();
                    }
                    else if (choice == 2)
                    {
                        PinnedAssetsStore.DeleteGroup(group.id, unpinContainedAssets: true);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // Handle Group Header Drag-to-Reorder initiation
            HandleGroupReorderDragSource(dragHandleRect, headerRect, groupIndex, group);

            // Draw Asset Rows when expanded
            if (group.isExpanded)
            {
                if (visibleEntries.Count == 0)
                {
                    Rect emptyRect = EditorGUILayout.GetControlRect(false, 26f);
                    GUI.Label(emptyRect, "   Drag & drop assets here from Project Window or other groups, or press Ctrl+Win+P in Project Window.", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < visibleEntries.Count; i++)
                    {
                        DrawAssetRow(visibleEntries[i], i, group, totalPinnedBytes);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            // Handle Drop Target on the entire Group box (both for Project assets, internal asset moves, and group reordering)
            HandleGroupDropTarget(groupStartRect, headerRect, group, groupIndex);
        }

        private void DrawAssetRow(PinnedAssetEntry entry, int rowIndex, PinnedAssetGroup group, long totalPinnedBytes)
        {
            bool isSelected = !string.IsNullOrEmpty(_selectedAssetGuid) && _selectedAssetGuid == entry.guid;

            Rect rowRect = EditorGUILayout.BeginHorizontal(
                isSelected ? _rowSelectedStyle : (rowIndex % 2 == 0 ? _rowEvenStyle : _rowOddStyle),
                GUILayout.Height(22f));

            // Row background painting (Build Report Tool alternating rows + bright blue selection row)
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor;
                if (isSelected)
                {
                    bgColor = new Color(0.17f, 0.36f, 0.53f, 0.95f);
                }
                else if (rowIndex % 2 == 0)
                {
                    bgColor = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f, 0.55f)
                        : new Color(0.82f, 0.82f, 0.82f, 0.55f);
                }
                else
                {
                    bgColor = EditorGUIUtility.isProSkin
                        ? new Color(0.19f, 0.19f, 0.19f, 0.55f)
                        : new Color(0.76f, 0.76f, 0.76f, 0.55f);
                }
                EditorGUI.DrawRect(rowRect, bgColor);
            }

            // 1. "Ping" Button (Build Report Tool style)
            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(38f), GUILayout.Height(18f)))
            {
                _selectedAssetGuid = entry.guid;
                PingAssetByGuid(entry.guid, selectObject: true);
            }

            // 2. Group Circular Tag Dot
            Rect tagCell = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
            Rect dotRect = new Rect(tagCell.x + 4f, tagCell.y + 5f, 10f, 10f);
            ProjectWindowBadge.DrawCircularTag(dotRect, group.color);

            // 3. Asset Icon
            Texture iconTex = AssetDatabase.GetCachedIcon(entry.assetPath);
            Rect iconRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
            if (iconTex != null)
            {
                GUI.DrawTexture(new Rect(iconRect.x, iconRect.y + 1f, 16f, 16f), iconTex, ScaleMode.ScaleToFit);
            }

            // 4. Index + Folder Path (muted grey) + Filename (bold bright, just like Build Report Tool screenshot)
            string folderHex = isSelected ? "#D0E4FF" : (EditorGUIUtility.isProSkin ? "#8C8C8C" : "#555555");
            string fileHex = isSelected ? "#FFFFFF" : (EditorGUIUtility.isProSkin ? "#EBEBEB" : "#111111");
            string indexPrefix = $"{rowIndex + 1}. ";
            string formattedPath = $"<color={folderHex}>{indexPrefix}{entry.folderPath}</color><b><color={fileHex}>{entry.fileName}</color></b>";

            GUILayout.Label(new GUIContent(formattedPath, entry.assetPath), _richPathStyle, GUILayout.ExpandWidth(true), GUILayout.Height(18f));

            // 5. Asset Type Column
            GUILayout.Label(entry.assetType ?? "Asset", EditorStyles.miniLabel, GUILayout.Width(110f), GUILayout.Height(18f));

            // 6. File Size Column
            GUILayout.Label(PinnedAssetsStore.FormatBytes(entry.fileSizeBytes), EditorStyles.miniLabel, GUILayout.Width(76f), GUILayout.Height(18f));

            // 7. Percentage Column
            double pct = (entry.fileSizeBytes * 100.0) / totalPinnedBytes;
            GUILayout.Label($"{pct:F1}%", EditorStyles.miniLabel, GUILayout.Width(58f), GUILayout.Height(18f));

            // 8. Unpin Button
            if (GUILayout.Button(new GUIContent("×", "Unpin asset (Ctrl+Win+P)"), EditorStyles.miniButton, GUILayout.Width(20f), GUILayout.Height(18f)))
            {
                if (_selectedAssetGuid == entry.guid)
                    _selectedAssetGuid = null;
                PinnedAssetsStore.UnpinAsset(entry.guid);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            // Row interaction: click to Ping in Project window, right-click for menu, drag to move between groups
            HandleAssetRowInteraction(rowRect, entry);
        }

        // =====================================================================
        // Row Click (Ping), Right-Click Menu, and Drag-and-Drop Handlers
        // =====================================================================

        private void HandleAssetRowInteraction(Rect rowRect, PinnedAssetEntry entry)
        {
            Event evt = Event.current;
            if (evt == null)
                return;

            if (evt.type == EventType.MouseDown && rowRect.Contains(evt.mousePosition))
            {
                if (evt.button == 0)
                {
                    _selectedAssetGuid = entry.guid;
                    _mouseDownPos = evt.mousePosition;
                    _potentialDragAssetGuid = entry.guid;
                    _potentialDragGroupIndex = -1;

                    // Clicking any asset in the tool window pings that asset in Project Window
                    PingAssetByGuid(entry.guid, selectObject: true);

                    if (evt.clickCount == 2)
                    {
                        Object assetObj = AssetDatabase.LoadMainAssetAtPath(entry.assetPath);
                        if (assetObj != null)
                        {
                            AssetDatabase.OpenAsset(assetObj);
                        }
                    }

                    evt.Use();
                    Repaint();
                }
                else if (evt.button == 1)
                {
                    _selectedAssetGuid = entry.guid;
                    ShowAssetRowContextMenu(entry);
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && _potentialDragAssetGuid == entry.guid)
            {
                if (Vector2.Distance(evt.mousePosition, _mouseDownPos) > 5f)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData(DragPinnedGuidKey, entry.guid);

                    Object obj = AssetDatabase.LoadMainAssetAtPath(entry.assetPath);
                    if (obj != null)
                    {
                        DragAndDrop.objectReferences = new Object[] { obj };
                    }
                    DragAndDrop.paths = new string[] { entry.assetPath };
                    DragAndDrop.StartDrag($"Move Pinned: {entry.fileName}");

                    _potentialDragAssetGuid = null;
                    evt.Use();
                }
            }
        }

        private void HandleGroupReorderDragSource(Rect dragHandleRect, Rect headerRect, int groupIndex, PinnedAssetGroup group)
        {
            Event evt = Event.current;
            if (evt == null)
                return;

            // Allow starting a group reorder drag from the drag handle '≡' or anywhere on the group header bar
            if (evt.type == EventType.MouseDown && evt.button == 0 && (dragHandleRect.Contains(evt.mousePosition) || headerRect.Contains(evt.mousePosition)))
            {
                _mouseDownPos = evt.mousePosition;
                _potentialDragGroupIndex = groupIndex;
                _potentialDragAssetGuid = null;
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0 && _potentialDragGroupIndex == groupIndex && headerRect.Contains(evt.mousePosition))
            {
                _potentialDragGroupIndex = -1;
                if (!dragHandleRect.Contains(evt.mousePosition))
                {
                    group.isExpanded = !group.isExpanded;
                    PinnedAssetsStore.Save(notify: false);
                }
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseDrag && _potentialDragGroupIndex == groupIndex)
            {
                if (Vector2.Distance(evt.mousePosition, _mouseDownPos) > 4f)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData(DragGroupIndexKey, groupIndex);
                    DragAndDrop.objectReferences = Array.Empty<Object>();
                    DragAndDrop.StartDrag($"Reorder Group: {group.name}");
                    _potentialDragGroupIndex = -1;
                    evt.Use();
                }
            }
        }

        private void HandleGroupDropTarget(Rect groupBoxRect, Rect headerRect, PinnedAssetGroup group, int groupIndex)
        {
            Event evt = Event.current;
            if (evt == null)
                return;

            bool isMouseInside = groupBoxRect.Contains(evt.mousePosition);
            object draggedGroupIdxObj = DragAndDrop.GetGenericData(DragGroupIndexKey);

            // Case 1: Reordering Groups
            if (draggedGroupIdxObj is int draggedGroupIndex)
            {
                if (isMouseInside && (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    _hoveredReorderGroupTargetIndex = groupIndex;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.SetGenericData(DragGroupIndexKey, null);
                        PinnedAssetsStore.ReorderGroup(draggedGroupIndex, groupIndex);
                    }
                    evt.Use();
                    Repaint();
                }

                if (Event.current.type == EventType.Repaint && _hoveredReorderGroupTargetIndex == groupIndex && draggedGroupIndex != groupIndex)
                {
                    float lineY = draggedGroupIndex < groupIndex ? groupBoxRect.yMax : groupBoxRect.yMin;
                    Rect lineRect = new Rect(groupBoxRect.x, lineY - 1.5f, groupBoxRect.width, 3f);
                    EditorGUI.DrawRect(lineRect, new Color(0.25f, 0.68f, 1f, 1f));
                }
                return;
            }

            // Case 2: Dragging an Asset Row between Groups OR Dragging Asset(s) from Project Window into a Group
            if (isMouseInside && (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
            {
                string internalGuid = DragAndDrop.GetGenericData(DragPinnedGuidKey) as string;
                List<string> externalGuids = ExtractDraggedProjectAssetGuids();

                if (!string.IsNullOrEmpty(internalGuid) || externalGuids.Count > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    _hoveredDropGroupId = group.id;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.SetGenericData(DragPinnedGuidKey, null);

                        if (!string.IsNullOrEmpty(internalGuid))
                        {
                            PinnedAssetsStore.MoveAssetToGroup(internalGuid, group.id);
                            _selectedAssetGuid = internalGuid;
                        }
                        else if (externalGuids.Count > 0)
                        {
                            PinnedAssetsStore.PinAssets(externalGuids, group.id);
                            _selectedAssetGuid = externalGuids[0];
                        }
                    }

                    evt.Use();
                    Repaint();
                }
            }

            // Highlight target group border when hovering with an asset drag
            if (Event.current.type == EventType.Repaint && _hoveredDropGroupId == group.id)
            {
                Color highlight = group.color;
                highlight.a = 0.9f;
                DrawRectOutline(groupBoxRect, highlight, 2f);
            }
        }

        private void DrawWindowDropZoneFallback()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 36f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Event evt = Event.current;
            if (evt == null)
                return;

            if (dropArea.Contains(evt.mousePosition) && (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
            {
                object draggedGroupIdxObj = DragAndDrop.GetGenericData(DragGroupIndexKey);
                if (draggedGroupIdxObj is int draggedGroupIndex)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.SetGenericData(DragGroupIndexKey, null);
                        PinnedAssetsStore.ReorderGroup(draggedGroupIndex, PinnedAssetsStore.Groups.Count - 1);
                    }
                    evt.Use();
                    return;
                }

                List<string> draggedGuids = ExtractDraggedProjectAssetGuids();
                if (draggedGuids.Count > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        PinnedAssetsStore.PinAssets(draggedGuids, PinnedAssetsStore.UngroupedGroupId);
                        _selectedAssetGuid = draggedGuids[0];
                    }
                    evt.Use();
                    Repaint();
                }
            }
        }

        private static List<string> ExtractDraggedProjectAssetGuids()
        {
            List<string> guids = new List<string>();

            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                for (int i = 0; i < DragAndDrop.paths.Length; i++)
                {
                    string path = DragAndDrop.paths[i];
                    if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets"))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (!string.IsNullOrEmpty(guid) && !guids.Contains(guid))
                        {
                            guids.Add(guid);
                        }
                    }
                }
            }

            if (guids.Count == 0 && DragAndDrop.objectReferences != null)
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    Object obj = DragAndDrop.objectReferences[i];
                    if (obj == null)
                        continue;
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets"))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (!string.IsNullOrEmpty(guid) && !guids.Contains(guid))
                        {
                            guids.Add(guid);
                        }
                    }
                }
            }

            return guids;
        }

        private void HandleGlobalDragCleanup()
        {
            Event evt = Event.current;
            if (evt != null && (evt.type == EventType.MouseUp || evt.type == EventType.DragExited))
            {
                _potentialDragAssetGuid = null;
                _potentialDragGroupIndex = -1;
            }
        }

        // =====================================================================
        // Bottom Status Bar & Selected Asset Pill (Build Report Tool Style)
        // =====================================================================

        private void DrawBottomStatusBar()
        {
            IReadOnlyList<PinnedAssetEntry> allEntries = PinnedAssetsStore.Entries;
            long totalBytes = PinnedAssetsStore.GetTotalPinnedSizeBytes();

            PinnedAssetsStore.TryGetEntry(_selectedAssetGuid, out PinnedAssetEntry selectedEntry);

            // Top summary strip
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string selectedSummary = selectedEntry != null
                ? $"Selected: 1 ({PinnedAssetsStore.FormatBytes(selectedEntry.fileSizeBytes)})   |   "
                : "Selected: 0   |   ";

            GUILayout.Label(
                $"{selectedSummary}Total Pinned: {allEntries.Count} asset(s) ({PinnedAssetsStore.FormatBytes(totalBytes)})   •   Shortcut: Ctrl+Win+P to Pin/Unpin",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Bottom Selected Asset Pill Bar (matches the red arrow in Build Report Tool screenshot)
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(28f));

            if (selectedEntry != null)
            {
                Texture icon = AssetDatabase.GetCachedIcon(selectedEntry.assetPath);
                GUIContent pillContent = new GUIContent($" {selectedEntry.fileName}", icon, $"Click to ping {selectedEntry.assetPath}");

                if (GUILayout.Button(pillContent, _bottomPillButtonStyle, GUILayout.ExpandWidth(false)))
                {
                    PingAssetByGuid(selectedEntry.guid, selectObject: true);
                }

                GUILayout.Label("is in group", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));

                PinnedAssetGroup group = PinnedAssetsStore.GetGroup(selectedEntry.groupId);
                if (group != null)
                {
                    Rect dotCell = GUILayoutUtility.GetRect(14f, 18f, GUILayout.Width(14f));
                    ProjectWindowBadge.DrawCircularTag(new Rect(dotCell.x + 1f, dotCell.y + 4f, 11f, 11f), group.color);

                    if (GUILayout.Button(group.name, _bottomPillButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        group.isExpanded = true;
                    }
                }

                GUILayout.Space(6f);
                GUILayout.Label(selectedEntry.assetPath, EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.Label("Click any pinned asset row above to ping it in the Project Window, or drag assets between groups.", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();
        }

        // =====================================================================
        // Context Menu & Helpers
        // =====================================================================

        private void ShowAssetRowContextMenu(PinnedAssetEntry entry)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Ping Asset in Project Window"), false, () =>
            {
                PingAssetByGuid(entry.guid, selectObject: true);
            });

            menu.AddItem(new GUIContent("Open Asset"), false, () =>
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(entry.assetPath);
                if (obj != null)
                    AssetDatabase.OpenAsset(obj);
            });

            menu.AddItem(new GUIContent("Reveal in Explorer / Finder (R)"), false, () =>
            {
                OSFileExplorerUtility.RevealAssetsInExplorerOrFinder(new List<string> { entry.guid });
            });

            menu.AddItem(new GUIContent("Reveal All Group Assets in Single Explorer / Finder Window"), false, () =>
            {
                List<PinnedAssetEntry> groupEntries = PinnedAssetsStore.GetEntriesForGroup(entry.groupId);
                List<string> groupGuids = new List<string>(groupEntries.Count);
                for (int i = 0; i < groupEntries.Count; i++)
                {
                    groupGuids.Add(groupEntries[i].guid);
                }
                OSFileExplorerUtility.RevealAssetsInExplorerOrFinder(groupGuids);
            });

            menu.AddSeparator("");

            IReadOnlyList<PinnedAssetGroup> groups = PinnedAssetsStore.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                PinnedAssetGroup g = groups[i];
                bool isCurrent = entry.groupId == g.id;
                string targetGroupId = g.id;
                menu.AddItem(new GUIContent($"Move to Group/{g.name}"), isCurrent, () =>
                {
                    PinnedAssetsStore.MoveAssetToGroup(entry.guid, targetGroupId);
                });
            }

            menu.AddSeparator("Move to Group/");
            menu.AddItem(new GUIContent("Move to Group/+ Create New Group..."), false, () =>
            {
                GroupPopupWindow.ShowForPinAndAddToGroup(new List<string> { entry.guid });
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy Full Disk Path (C)"), false, () =>
            {
                OSFileExplorerUtility.CopySingleAssetFullDiskPath(entry.guid);
            });

            menu.AddItem(new GUIContent("Copy Relative Asset Path"), false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = entry.assetPath;
            });

            menu.AddItem(new GUIContent("Unpin Asset (Ctrl+Win+P)"), false, () =>
            {
                PinnedAssetsStore.UnpinAsset(entry.guid);
            });

            menu.ShowAsContext();
        }

        private List<PinnedAssetEntry> GetFilteredAndSortedEntries(string groupId)
        {
            List<PinnedAssetEntry> raw = PinnedAssetsStore.GetEntriesForGroup(groupId);
            List<PinnedAssetEntry> filtered = new List<PinnedAssetEntry>(raw.Count);

            bool hasSearch = !string.IsNullOrWhiteSpace(_searchQuery);
            string query = hasSearch ? _searchQuery.Trim() : string.Empty;
            bool filterByType = !string.IsNullOrEmpty(_selectedTypeFilter) && !string.Equals(_selectedTypeFilter, "All", StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < raw.Count; i++)
            {
                PinnedAssetEntry e = raw[i];
                if (e == null)
                    continue;

                if (filterByType && !string.Equals(e.assetType, _selectedTypeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasSearch)
                {
                    bool matchesName = !string.IsNullOrEmpty(e.fileName) && e.fileName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool matchesPath = !string.IsNullOrEmpty(e.assetPath) && e.assetPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!matchesName && !matchesPath)
                        continue;
                }

                filtered.Add(e);
            }

            if (_sortColumn != SortColumn.None)
            {
                filtered.Sort((a, b) =>
                {
                    int cmp = 0;
                    switch (_sortColumn)
                    {
                        case SortColumn.Path:
                            cmp = string.Compare(a.assetPath, b.assetPath, StringComparison.OrdinalIgnoreCase);
                            break;
                        case SortColumn.FileName:
                            cmp = string.Compare(a.fileName, b.fileName, StringComparison.OrdinalIgnoreCase);
                            break;
                        case SortColumn.Type:
                            cmp = string.Compare(a.assetType, b.assetType, StringComparison.OrdinalIgnoreCase);
                            break;
                        case SortColumn.Size:
                            cmp = a.fileSizeBytes.CompareTo(b.fileSizeBytes);
                            break;
                    }
                    return _sortAscending ? cmp : -cmp;
                });
            }

            return filtered;
        }

        private static void SetAllGroupsExpanded(bool expanded)
        {
            IReadOnlyList<PinnedAssetGroup> groups = PinnedAssetsStore.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].isExpanded = expanded;
            }
            PinnedAssetsStore.Save(notify: true);
        }

        public static void PingAssetByGuid(string guid, bool selectObject)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return;

            Object obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj != null)
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(obj);
                if (selectObject)
                {
                    Selection.activeObject = obj;
                }
            }
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }
    }
}

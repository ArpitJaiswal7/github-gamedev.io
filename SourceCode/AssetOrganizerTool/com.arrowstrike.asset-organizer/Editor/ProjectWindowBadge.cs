using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Collaboratively handles:
    /// 1) Rendering the color-coded circular Pinned Asset badge at the top-right corner of pinned assets
    ///    in both Tile (Grid) view and List view.
    /// 2) Enabling an interactive tag-cum-button ("C" for Copy full disk path, or "R" for Reveal in Explorer/Finder
    ///    when 'R' is pressed while hovering) when the user hovers over the small top-right corner rect of any asset,
    ///    and disabling it immediately when the cursor exits that small rect.
    ///    - Mouse 1 on "C": Copies that single hovered asset's complete full disk path (with parent drive) even if multiple assets are selected.
    ///    - Mouse 1 on "R": Reveals that single hovered asset in Explorer/Finder.
    ///    - Ctrl/Cmd + Mouse 1: Reveals all currently selected Project assets together inside a single Windows Explorer / macOS Finder window.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowBadge
    {
#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_R = 0x52;
        private const int VK_C = 0x43;
#endif

        private static Texture2D _circleFillTex;
        private static Texture2D _circleBorderTex;
        private static GUIStyle _tagLetterStyle;

        // Hover & mode state for the interactive top-right tag-cum-button
        private static string _hoveredTagGuid;
        private static bool _isHoveringAnyTagRectThisFrame;
        private static bool _revealModeActive;
        private static bool _wasRKeyDown;
        private static bool _wasCKeyDown;
        private static double _lastModeToggleTime;

        // Visual feedback flash when path is copied
        private static string _copiedFeedbackGuid;
        private static double _copiedFeedbackUntilTime;

        static ProjectWindowBadge()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            // Ensure Project Window repaints smoothly when hovering items or pressing R/C while hovering the tag button
            if (EditorWindow.mouseOverWindow != null)
            {
                string winType = EditorWindow.mouseOverWindow.GetType().Name;
                if (winType.Contains("ProjectBrowser") || winType.Contains("Project"))
                {
                    EditorWindow.mouseOverWindow.wantsMouseMove = true;
                    EditorWindow.mouseOverWindow.Repaint();
                }
            }

#if UNITY_EDITOR_WIN
            if (!string.IsNullOrEmpty(_hoveredTagGuid) && InternalEditorUtility.isApplicationActive)
            {
                bool rDown = (GetAsyncKeyState(VK_R) & 0x8000) != 0;
                if (rDown && !_wasRKeyDown)
                {
                    _wasRKeyDown = true;
                    ToggleOrSetRevealMode(toggle: true);
                }
                else if (!rDown)
                {
                    _wasRKeyDown = false;
                }

                bool cDown = (GetAsyncKeyState(VK_C) & 0x8000) != 0;
                if (cDown && !_wasCKeyDown)
                {
                    _wasCKeyDown = true;
                    SetRevealMode(false);
                }
                else if (!cDown)
                {
                    _wasCKeyDown = false;
                }
            }
            else
            {
                _wasRKeyDown = false;
                _wasCKeyDown = false;
            }
#endif
        }

        private static void ToggleOrSetRevealMode(bool toggle)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastModeToggleTime < 0.18)
                return;

            _lastModeToggleTime = now;
            _revealModeActive = toggle ? !_revealModeActive : true;
            EditorApplication.RepaintProjectWindow();
        }

        private static void SetRevealMode(bool reveal)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastModeToggleTime < 0.12)
                return;

            _lastModeToggleTime = now;
            if (_revealModeActive != reveal)
            {
                _revealModeActive = reveal;
                EditorApplication.RepaintProjectWindow();
            }
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            Event evt = Event.current;
            if (evt == null || string.IsNullOrEmpty(guid))
                return;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets"))
                return;

            if (EditorWindow.mouseOverWindow != null)
            {
                EditorWindow.mouseOverWindow.wantsMouseMove = true;
            }

            bool isPinned = PinnedAssetsStore.TryGetEntry(guid, out PinnedAssetEntry pinnedEntry);
            Color pinnedGroupColor = isPinned
                ? PinnedAssetsStore.GetGroupColor(pinnedEntry.groupId)
                : PinnedAssetsStore.DefaultYellowTagColor;

            bool isListView = selectionRect.height <= 20f;

            // Compute both the passive Pinned Badge rect and the interactive Top-Right Tag-Button rect(s)
            Rect passiveBadgeRect;
            Rect activeTagButtonRect;

            Vector2 mousePos = evt.mousePosition;
            bool isHoveringAsset = selectionRect.Contains(mousePos);
            bool isHoveringSmallTagRect = false;

            if (isListView)
            {
                // In List/Tree view, the 16x16 asset icon starts at selectionRect.x, selectionRect.y.
                const float passiveSize = 7.5f;
                float iconRight = selectionRect.x + 16f;
                passiveBadgeRect = new Rect(
                    iconRight - passiveSize + 1f,
                    selectionRect.y + 1f,
                    passiveSize,
                    passiveSize
                );

                // Support hovering the top-right corner of the asset icon OR the top-right corner of the list row
                Rect iconTopRightHitRect = new Rect(selectionRect.x + 6f, selectionRect.y, 14f, 14f);
                Rect rowTopRightHitRect = new Rect(selectionRect.xMax - 18f, selectionRect.y + 1f, 16f, 15f);

                if (isHoveringAsset && iconTopRightHitRect.Contains(mousePos))
                {
                    isHoveringSmallTagRect = true;
                    activeTagButtonRect = new Rect(selectionRect.x + 5f, selectionRect.y + 1f, 14f, 14f);
                }
                else if (isHoveringAsset && rowTopRightHitRect.Contains(mousePos))
                {
                    isHoveringSmallTagRect = true;
                    activeTagButtonRect = new Rect(selectionRect.xMax - 17f, selectionRect.y + 1f, 15f, 15f);
                }
                else
                {
                    activeTagButtonRect = new Rect(selectionRect.x + 6f, selectionRect.y + 1f, 12f, 12f);
                }
            }
            else
            {
                // In Tile/Grid view, the icon occupies the top square region of selectionRect.
                float iconSize = Mathf.Min(selectionRect.width, Mathf.Max(20f, selectionRect.height - 14f));
                float passiveSize = Mathf.Clamp(Mathf.Round(iconSize * 0.20f), 9f, 14f);
                float buttonSize = Mathf.Clamp(Mathf.Round(iconSize * 0.30f), 15f, 20f);

                float iconLeft = selectionRect.x + (selectionRect.width - iconSize) * 0.5f;
                float iconRight = iconLeft + iconSize;

                passiveBadgeRect = new Rect(
                    iconRight - passiveSize - 2f,
                    selectionRect.y + 2f,
                    passiveSize,
                    passiveSize
                );

                activeTagButtonRect = new Rect(
                    iconRight - buttonSize - 1f,
                    selectionRect.y + 1f,
                    buttonSize,
                    buttonSize
                );

                isHoveringSmallTagRect = isHoveringAsset && activeTagButtonRect.Contains(mousePos);
            }

            // Track hover entry/exit for the small tag-button rect
            if (isHoveringSmallTagRect)
            {
                _isHoveringAnyTagRectThisFrame = true;
                if (_hoveredTagGuid != guid)
                {
                    _hoveredTagGuid = guid;
                    // Default to 'C' (Copy Path) whenever entering a tag-button rect
                    _revealModeActive = false;
                }

                // Also check KeyDown directly inside OnGUI if Project Window has focus
                if (evt.type == EventType.KeyDown && !evt.control && !evt.command && !evt.alt)
                {
                    if (evt.keyCode == KeyCode.R)
                    {
                        ToggleOrSetRevealMode(toggle: true);
                        evt.Use();
                    }
                    else if (evt.keyCode == KeyCode.C)
                    {
                        SetRevealMode(false);
                        evt.Use();
                    }
                }

                // Handle Mouse 1 and Ctrl/Cmd + Mouse 1 on the active tag-cum-button
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    bool isCtrlOrCmd = evt.control || evt.command;
                    ExecuteTagButtonAction(guid, isCtrlOrCmd, _revealModeActive);
                    evt.Use();
                    EditorApplication.RepaintProjectWindow();
                    return;
                }
                else if (evt.type == EventType.MouseUp && evt.button == 0)
                {
                    evt.Use();
                    return;
                }
            }
            else if (_hoveredTagGuid == guid && (evt.type == EventType.MouseMove || evt.type == EventType.Repaint || evt.type == EventType.Layout))
            {
                // Cursor exited the small tag/button rect for this asset -> disable the button and reset mode to 'C'
                _hoveredTagGuid = null;
                _revealModeActive = false;
            }

            // Render visuals during Repaint
            if (evt.type == EventType.Repaint)
            {
                if (isHoveringSmallTagRect)
                {
                    // Draw the enabled interactive Tag-cum-Button with letter 'C' or 'R'
                    EditorGUIUtility.AddCursorRect(activeTagButtonRect, MouseCursor.Link);

                    bool isCopiedFlash = _copiedFeedbackGuid == guid && EditorApplication.timeSinceStartup < _copiedFeedbackUntilTime;
                    Color buttonColor;
                    string letter;

                    if (isCopiedFlash)
                    {
                        buttonColor = new Color(0.22f, 0.88f, 0.42f, 1f);
                        letter = "✓";
                    }
                    else if (_revealModeActive)
                    {
                        // 'R' (Reveal in Folder) mode
                        buttonColor = isPinned ? Color.Lerp(pinnedGroupColor, new Color(0.15f, 0.88f, 0.72f, 1f), 0.45f) : new Color(0.18f, 0.85f, 0.65f, 1f);
                        letter = "R";
                    }
                    else
                    {
                        // 'C' (Copy Full Disk Path) mode
                        buttonColor = isPinned ? pinnedGroupColor : PinnedAssetsStore.DefaultYellowTagColor;
                        letter = "C";
                    }

                    DrawInteractiveTagButton(activeTagButtonRect, buttonColor, letter);

                    string tooltip = _revealModeActive
                        ? "R (Reveal Mode)\n• Click (Mouse 1): Reveal this asset in Explorer/Finder\n• Ctrl+Click: Reveal all selected assets in a single Explorer/Finder window\n• Press C or R: Switch back to Copy Path (C)"
                        : "C (Copy Full Path Mode)\n• Click (Mouse 1): Copy complete disk path from parent drive\n• Press R: Switch button to Reveal (R)\n• Ctrl+Click: Reveal all selected assets in a single Explorer/Finder window";

                    GUI.Label(activeTagButtonRect, new GUIContent(string.Empty, tooltip));
                }
                else
                {
                    // Button is disabled outside the small top-right rect.
                    // If the asset is pinned, draw its normal circular group tag badge.
                    if (isPinned)
                    {
                        DrawCircularTag(passiveBadgeRect, pinnedGroupColor);
                    }
                    else if (isHoveringAsset)
                    {
                        // Subtle target indicator at the top-right corner while hovering the asset
                        // so the user can easily locate the small top-right hotzone rect.
                        DrawSubtleHoverTargetHint(passiveBadgeRect);
                    }
                }
            }
        }

        private static void ExecuteTagButtonAction(string hoveredGuid, bool isCtrlOrCmdHeld, bool isRevealMode)
        {
            if (isCtrlOrCmdHeld)
            {
                // Ctrl + Mouse 1 (or Cmd + Mouse 1 on Mac):
                // Reveal ALL selected Unity assets together inside a single Windows Explorer / macOS Finder window.
                List<string> selectedGuids = ProjectWindowContextMenu.GetSelectedProjectAssetGuids();
                if (!selectedGuids.Contains(hoveredGuid))
                {
                    selectedGuids.Add(hoveredGuid);
                }

                OSFileExplorerUtility.RevealAssetsInExplorerOrFinder(selectedGuids);
                return;
            }

            if (isRevealMode)
            {
                // 'R' mode + Mouse 1:
                // Reveal the hovered asset (or if multiple assets are selected and the hovered asset is part of the selection,
                // reveal the single hovered asset on normal click, while Ctrl+Click reveals all selected).
                OSFileExplorerUtility.RevealAssetsInExplorerOrFinder(new List<string> { hoveredGuid });
            }
            else
            {
                // 'C' mode + Mouse 1:
                // Copy ONLY the single hovered asset's complete full disk path (from parent drive, e.g. C:\Users\...)
                // even when multiple assets are currently selected in the Project Window.
                OSFileExplorerUtility.CopySingleAssetFullDiskPath(hoveredGuid);
                _copiedFeedbackGuid = hoveredGuid;
                _copiedFeedbackUntilTime = EditorApplication.timeSinceStartup + 0.65;
            }
        }

        private static void DrawInteractiveTagButton(Rect rect, Color fillColor, string letter)
        {
            EnsureTextures();
            EnsureLetterStyle();

            // Draw circular tag background
            DrawCircularTag(rect, fillColor);

            // Calculate contrasting text color (dark text on bright yellow/green, white text on dark colors)
            float luminance = 0.299f * fillColor.r + 0.587f * fillColor.g + 0.114f * fillColor.b;
            Color textColor = luminance > 0.52f ? new Color(0.08f, 0.08f, 0.08f, 1f) : Color.white;

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.62f), 8, 12);
            _tagLetterStyle.fontSize = fontSize;
            _tagLetterStyle.normal.textColor = textColor;
            _tagLetterStyle.hover.textColor = textColor;

            GUI.Label(rect, letter, _tagLetterStyle);
        }

        private static void DrawSubtleHoverTargetHint(Rect rect)
        {
            EnsureTextures();
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            GUI.DrawTexture(rect, _circleBorderTex, ScaleMode.ScaleToFit, true);
            GUI.color = prev;
        }

        /// <summary>
        /// Draws a crisp anti-aliased circular tag with a subtle dark border and inner fill.
        /// </summary>
        public static void DrawCircularTag(Rect rect, Color color)
        {
            EnsureTextures();

            Color prevColor = GUI.color;

            // Dark outer ring for contrast against both light and dark icons
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.94f);
            GUI.DrawTexture(rect, _circleBorderTex, ScaleMode.ScaleToFit, true);

            // Colored inner circle
            Color fill = color;
            fill.a = 1f;
            GUI.color = fill;
            GUI.DrawTexture(rect, _circleFillTex, ScaleMode.ScaleToFit, true);

            GUI.color = prevColor;
        }

        private static void EnsureLetterStyle()
        {
            if (_tagLetterStyle != null)
                return;

            _tagLetterStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                clipping = TextClipping.Overflow
            };
        }

        private static void EnsureTextures()
        {
            if (_circleFillTex != null && _circleBorderTex != null)
                return;

            const int size = 64;
            float center = (size - 1) * 0.5f;
            float outerRadius = size * 0.47f;
            float innerRadius = size * 0.38f;

            _circleBorderTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AssetOrganizer_BadgeBorder",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            _circleFillTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AssetOrganizer_BadgeFill",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] borderPixels = new Color[size * size];
            Color[] fillPixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Anti-aliased outer disc
                    float borderAlpha = Mathf.Clamp01(outerRadius - dist + 0.75f);
                    borderPixels[y * size + x] = new Color(1f, 1f, 1f, borderAlpha);

                    // Anti-aliased inner fill disc
                    float fillAlpha = Mathf.Clamp01(innerRadius - dist + 0.75f);
                    fillPixels[y * size + x] = new Color(1f, 1f, 1f, fillAlpha);
                }
            }

            _circleBorderTex.SetPixels(borderPixels);
            _circleBorderTex.Apply(false, true);

            _circleFillTex.SetPixels(fillPixels);
            _circleFillTex.Apply(false, true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Diagnostic Test Harness & Benchmark Dashboard for the Native Image Pipeline.
    /// Features:
    /// - Virtualized 60 FPS IMGUI ScrollView for WebGL & Editor performance.
    /// - Coupled multi-resolution rows (256px, 512px, 1080px, 2048px) separated by a 1px line per puzzle,
    ///   with double spacing between different puzzles.
    /// - Full-size responsive Sprite preview in the right panel + Fullscreen View mode.
    /// - Direct OS / IndexedDB folder opening via RevealInFileExplorer.
    /// - Real-time internet download speed indicator in the bottom-right corner.
    /// </summary>
    public sealed class NativeImagePipelineTestHarness : MonoBehaviour
    {
        [Header("Test Batch Settings")]
        [Tooltip("Number of 2080x2080 puzzle image URLs to generate and enqueue on startup.")]
        [Range(10, 500)]
        public int testPuzzleCount = 120;

        [Tooltip("Include a couple of intentionally broken URLs to verify faulty request tracking & removal.")]
        public bool includeFaultyTestUrls = true;

        private static readonly int[] SupportedResolutions = { 256, 512, 1080, 2048 };
        private readonly List<ImageRequestRecord> cachedRecordList = new List<ImageRequestRecord>(128);

        private Vector2 scrollPosition;
        private string selectedKey;
        private int activeResolutionPx = 1080;
        private bool isFullscreenPreview = false;
        private bool showPathDetailsBar = false;
        private Sprite previewSprite;
        private string previewInfo = "Click any puzzle tier row on the left to preview its Sprite (256px / 512px / 1080px / 2048px).";

        // Cached telemetry counters (updated on events or 4x/sec instead of every OnGUI layout pass)
        private int statQueued, statDownloading, statCached, statInRom, statFailed;
        private long statTotalIconBytes, statTotalFullBytes;
        private float nextStatsRefreshTime;

        // Cached GUI textures & styles for zero-allocation 60 FPS WebGL rendering
        private Texture2D separatorLineTex;
        private Texture2D puzzleCardBgTex;
        private Texture2D speedBadgeBgTex;
        private GUIStyle richLabelStyle;
        private GUIStyle puzzleGroupStyle;
        private GUIStyle tierRowButtonStyle;
        private GUIStyle speedBadgeStyle;
        private bool stylesInitialized;

        private string activeCategory = "All";

        private void Start()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.05f, 0.07f, 0.09f, 1f);
            }

            var pipeline = NativeImagePipeline.Instance;
            pipeline.OnRequestUpdated += HandlePipelineRequestUpdated;

            var sync = LevelSyncManager.Instance;
            sync.OnSyncCompleted += HandleSyncCompleted;
            sync.OnLevelChanged += HandleLevelChanged;

            // Start backend synchronization from live GitHub cloud CDN or local server (:4200)
            sync.SyncFromBackend(null, (success, totalCount) =>
            {
                RebuildCachedListAndStats();
                if (cachedRecordList.Count > 0)
                {
                    selectedKey = cachedRecordList[0].RequestKey;
                    LoadPreview(selectedKey, activeResolutionPx);
                }
            });
        }

        private void HandleSyncCompleted(bool success, int totalLevels)
        {
            RebuildCachedListAndStats();
            if (string.IsNullOrEmpty(selectedKey) && cachedRecordList.Count > 0)
            {
                selectedKey = cachedRecordList[0].RequestKey;
                LoadPreview(selectedKey, activeResolutionPx);
            }
        }

        private void HandleLevelChanged(int newLevel, PuzzleImageEntry puzzle)
        {
            if (puzzle != null)
            {
                selectedKey = puzzle.id;
                LoadPreview(selectedKey, activeResolutionPx);
            }
        }

        private void OnDestroy()
        {
            if (separatorLineTex != null) Destroy(separatorLineTex);
            if (puzzleCardBgTex != null) Destroy(puzzleCardBgTex);
            if (speedBadgeBgTex != null) Destroy(speedBadgeBgTex);
        }

        private void Update()
        {
            if (isFullscreenPreview && Input.GetKeyDown(KeyCode.Escape))
            {
                isFullscreenPreview = false;
            }

            if (Time.unscaledTime >= nextStatsRefreshTime)
            {
                nextStatsRefreshTime = Time.unscaledTime + 0.25f;
                RebuildCachedListAndStats();
            }
        }

        private void HandlePipelineRequestUpdated(ImageRequestRecord updatedRecord)
        {
            // Auto-load preview as soon as the initially selected key finishes downloading
            if (previewSprite == null &&
                updatedRecord != null &&
                updatedRecord.IsReady &&
                string.Equals(updatedRecord.RequestKey, selectedKey, StringComparison.Ordinal))
            {
                LoadPreview(selectedKey, activeResolutionPx);
            }
        }

        private void RebuildCachedListAndStats()
        {
            var allRequests = NativeImagePipeline.Instance.AllRequests;
            cachedRecordList.Clear();

            int q = 0, dl = 0, c = 0, rom = 0, f = 0;
            long iconB = 0, fullB = 0;

            foreach (var kvp in allRequests)
            {
                var r = kvp.Value;
                if (!string.IsNullOrEmpty(activeCategory) && !activeCategory.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var entry = LevelSyncManager.Instance.GetPuzzleById(r.RequestKey);
                    if (entry == null || !string.Equals(entry.category, activeCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                cachedRecordList.Add(r);
                switch (r.State)
                {
                    case RequestState.Queued: q++; break;
                    case RequestState.Downloading:
                    case RequestState.Processing: dl++; break;
                    case RequestState.Cached: c++; break;
                    case RequestState.SavedInROM: rom++; break;
                    case RequestState.Failed: f++; break;
                }
                iconB += r.IconSizeBytes;
                fullB += r.FullSizeBytes;
            }

            cachedRecordList.Sort((a, b) =>
            {
                var ea = LevelSyncManager.Instance.GetPuzzleById(a.RequestKey);
                var eb = LevelSyncManager.Instance.GetPuzzleById(b.RequestKey);
                int la = ea != null && ea.level > 0 ? ea.level : 9999;
                int lb = eb != null && eb.level > 0 ? eb.level : 9999;
                return la.CompareTo(lb);
            });

            statQueued = q;
            statDownloading = dl;
            statCached = c;
            statInRom = rom;
            statFailed = f;
            statTotalIconBytes = iconB;
            statTotalFullBytes = fullB;
        }

        public static string GenerateHighResTestManifestJson(int count, bool includeFaulty)
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":\"1.0.0\",\"puzzles\":[");

            for (int i = 1; i <= count; i++)
            {
                if (i > 1) sb.Append(",");
                int imageSeed = 10 + i;
                string id = $"puzzle_{i:D4}";
                string url = (includeFaulty && i % 45 == 0)
                    ? "https://picsum.photos/id/9999999/2080/2080"
                    : $"https://picsum.photos/seed/{imageSeed}/2080/2080";

                sb.Append($"{{\"id\":\"{id}\",\"url\":\"{url}\",\"category\":\"Landscape\",\"saveToRomOnDownload\":false}}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private void EnsureStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;

            separatorLineTex = CreateSolidTexture(new Color(0.32f, 0.38f, 0.46f, 0.85f));
            puzzleCardBgTex = CreateSolidTexture(new Color(0.11f, 0.14f, 0.18f, 0.96f));
            speedBadgeBgTex = CreateSolidTexture(new Color(0.06f, 0.09f, 0.13f, 0.95f));

            richLabelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true
            };

            puzzleGroupStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(2, 2, 0, 0)
            };
            puzzleGroupStyle.normal.background = puzzleCardBgTex;

            tierRowButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                fontSize = 12,
                padding = new RectOffset(10, 10, 4, 4)
            };

            speedBadgeStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true,
                fontSize = 12,
                padding = new RectOffset(12, 12, 6, 6)
            };
            speedBadgeStyle.normal.background = speedBadgeBgTex;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void CycleActiveResolution()
        {
            int idx = Array.IndexOf(SupportedResolutions, activeResolutionPx);
            activeResolutionPx = SupportedResolutions[(idx + 1) % SupportedResolutions.Length];
            if (!string.IsNullOrEmpty(selectedKey))
            {
                LoadPreview(selectedKey, activeResolutionPx);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            var pipeline = NativeImagePipeline.Instance;
            var selectedRec = !string.IsNullOrEmpty(selectedKey) ? pipeline.GetRequestStatus(selectedKey) : null;
            string storageBadge = (selectedRec != null && selectedRec.StorageTier == StorageTier.PermanentROM)
                ? "NIP:ROM"
                : "NIP:CACHE";

            // 1. Fullscreen Image View Mode
            if (isFullscreenPreview)
            {
                DrawFullscreenPreviewMode(pipeline, selectedRec, storageBadge);
                DrawRealtimeInternetSpeedBadge(pipeline);
                return;
            }

            // Reserve 36px at bottom for the bottom-right real-time internet speed bar
            float mainHeight = Screen.height - 54f;
            GUILayout.BeginArea(new Rect(12, 10, Screen.width - 24, mainHeight));
            GUILayout.BeginVertical("box");

            // Top Header & Controls
            GUILayout.BeginHorizontal();
            var sync = LevelSyncManager.Instance;
            string syncBadge = $"<color=#34d399>● {sync.TotalLevels} Synced</color> ({sync.LastSyncSource})";
            GUILayout.Label($"<b>Native Image Pipeline — WebGL Demo</b> [{syncBadge}]", richLabelStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("↻ Sync Backend", GUILayout.Width(130), GUILayout.Height(26)))
            {
                sync.SyncFromBackend(null, (ok, cnt) => RebuildCachedListAndStats());
            }

            if (GUILayout.Button($"Size: {activeResolutionPx}px (Cycle 256/512/1080/2048)", GUILayout.Width(240), GUILayout.Height(26)))
            {
                CycleActiveResolution();
            }

            if (GUILayout.Button($"Open Cache Folder [{storageBadge}]", GUILayout.Width(190), GUILayout.Height(26)))
            {
                if (!string.IsNullOrEmpty(selectedKey))
                {
                    pipeline.RevealInFileExplorer(selectedKey, activeResolutionPx);
                }
                showPathDetailsBar = !showPathDetailsBar;
            }

            if (GUILayout.Button("Fullscreen View", GUILayout.Width(120), GUILayout.Height(26)))
            {
                isFullscreenPreview = true;
            }
            GUILayout.EndHorizontal();

            // Level Progression & Category Toolbar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Prev Level", GUILayout.Width(100), GUILayout.Height(26)))
            {
                sync.PrevLevel();
            }

            var currPuzzle = sync.GetPuzzleForLevel(sync.CurrentLevel);
            string currTitle = currPuzzle != null ? currPuzzle.title : $"Level {sync.CurrentLevel}";
            string currCat = currPuzzle != null ? $" <color=#93c5fd>[{currPuzzle.category}]</color>" : "";
            GUILayout.Label($"<b>Level {sync.CurrentLevel:D2} / {Mathf.Max(1, sync.TotalLevels)}:</b> {currTitle}{currCat}", richLabelStyle, GUILayout.Width(320));

            if (GUILayout.Button("Next Level ▶", GUILayout.Width(100), GUILayout.Height(26)))
            {
                sync.NextLevel();
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Filter:</b>", richLabelStyle, GUILayout.Width(45));
            string[] cats = { "All", "Nature", "Architecture", "Animals" };
            for (int ci = 0; ci < cats.Length; ci++)
            {
                string catName = cats[ci];
                bool isCatActive = string.Equals(activeCategory, catName, StringComparison.OrdinalIgnoreCase);
                string catLabel = isCatActive ? $"<b><color=#fef08a>[{catName}]</color></b>" : catName;
                if (GUILayout.Button(catLabel, GUILayout.Height(26)))
                {
                    activeCategory = catName;
                    RebuildCachedListAndStats();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(
                $"Online: <b>{pipeline.IsOnline}</b> | " +
                $"Queued: {statQueued} | Downloading: <b>{statDownloading}</b> | Cached: <b>{statCached}</b> | ROM: <b>{statInRom}</b> | Failed: {statFailed} | " +
                $"256px Icons: {FormatBytes(statTotalIconBytes)} | 1080px Full: {FormatBytes(statTotalFullBytes)}",
                richLabelStyle);

            // Action Toolbar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorImageBridge.SimulateOfflineMode ? "Resume Online (Simulated)" : "Simulate Offline Drop", GUILayout.Height(26)))
            {
                EditorImageBridge.SimulateOfflineMode = !EditorImageBridge.SimulateOfflineMode;
            }
            if (GUILayout.Button("Boost Top 10 Unfinished (Viewport Priority)", GUILayout.Height(26)))
            {
                BoostNextUnfinishedBatch(10);
            }
            if (GUILayout.Button($"Purge Faulty Strings ({statFailed})", GUILayout.Height(26)))
            {
                pipeline.PurgeFaultyRequests();
                RebuildCachedListAndStats();
            }
            if (GUILayout.Button("Clear Volatile Cache (Keep ROM)", GUILayout.Height(26)))
            {
                pipeline.ClearVolatileCache();
                RebuildCachedListAndStats();
            }
            GUILayout.EndHorizontal();

            if (showPathDetailsBar && selectedRec != null)
            {
                GUILayout.Space(4);
                DrawCacheLocationInspector(pipeline, selectedRec);
            }

            GUILayout.Space(6);

            // Main Split Area: Left Virtualized Coupled Puzzle List + Right Full-Size Image Preview
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            float leftWidth = Mathf.Floor((Screen.width - 48f) * 0.54f);
            DrawVirtualizedCoupledPuzzleList(pipeline, leftWidth, mainHeight - 115f);

            GUILayout.Space(8);

            // Right Column: Full-Size Responsive Preview Panel
            DrawRightPreviewPanel(pipeline, selectedRec, storageBadge);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Bottom-Right Real-Time Internet Speed Indicator
            DrawRealtimeInternetSpeedBadge(pipeline);
        }

        /// <summary>
        /// Virtualized ScrollView that pairs/couples all resolutions of the same puzzle together
        /// with a simple 1px horizontal line in between them, and puts double vertical spacing (20px)
        /// between different puzzles. Only renders visible rows for 60 FPS WebGL performance.
        /// </summary>
        private void DrawVirtualizedCoupledPuzzleList(NativeImagePipeline pipeline, float width, float viewportHeight)
        {
            GUILayout.BeginVertical("box", GUILayout.Width(width), GUILayout.ExpandHeight(true));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            int totalPuzzles = cachedRecordList.Count;
            // Approximate height of one coupled puzzle block (header + 2-4 coupled tier rows + 20px double spacing)
            const float estimatedBlockHeight = 96f;
            const float doublePuzzleSpacing = 20f;

            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(scrollPosition.y / estimatedBlockHeight) - 2);
            int visibleCount = Mathf.CeilToInt(Mathf.Max(300f, viewportHeight) / estimatedBlockHeight) + 5;
            int lastVisible = Mathf.Min(totalPuzzles - 1, firstVisible + visibleCount);

            if (firstVisible > 0)
            {
                GUILayout.Space(firstVisible * estimatedBlockHeight);
            }

            for (int i = firstVisible; i <= lastVisible; i++)
            {
                var rec = cachedRecordList[i];
                bool isSelectedPuzzle = string.Equals(rec.RequestKey, selectedKey, StringComparison.Ordinal);
                string tierTag = rec.StorageTier == StorageTier.PermanentROM ? "ROM" : "CACHE";

                // Coupled Puzzle Container
                GUILayout.BeginVertical(puzzleGroupStyle);

                // Top Row of Puzzle Block: Level, Title, Category, State, Priority, Duration + Folder & ROM Actions
                GUILayout.BeginHorizontal();
                var entry = LevelSyncManager.Instance.GetPuzzleById(rec.RequestKey);
                string lvlPrefix = entry != null && entry.level > 0 ? $"[Lvl {entry.level:D2}] " : "";
                string displayTitle = entry != null && !string.IsNullOrEmpty(entry.title) ? entry.title : rec.RequestKey;
                string catTag = entry != null && !string.IsNullOrEmpty(entry.category) ? $"  <color=#93c5fd>({entry.category})</color>" : "";
                string headerPrefix = isSelectedPuzzle ? "▶ " : "";
                string errText = !string.IsNullOrEmpty(rec.ErrorMessage) ? $" | <color=#f87171>ERR: {rec.ErrorMessage}</color>" : "";
                GUILayout.Label(
                    $"<b>{headerPrefix}{lvlPrefix}{displayTitle}</b>{catTag}  [{rec.State} | {tierTag}]  (Prio: {rec.Priority} | {rec.DurationMs} ms){errText}",
                    richLabelStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Folder", GUILayout.Width(90), GUILayout.Height(22)))
                {
                    selectedKey = rec.RequestKey;
                    pipeline.RevealInFileExplorer(rec.RequestKey, activeResolutionPx);
                    LoadPreview(rec.RequestKey, activeResolutionPx);
                }

                if (rec.State == RequestState.Cached && GUILayout.Button("-> ROM", GUILayout.Width(64), GUILayout.Height(22)))
                {
                    pipeline.SaveToPermanentROM(rec.RequestKey);
                    RebuildCachedListAndStats();
                }
                else if (rec.State == RequestState.SavedInROM && GUILayout.Button("-> Cache", GUILayout.Width(68), GUILayout.Height(22)))
                {
                    pipeline.MoveFromRomToCache(rec.RequestKey);
                    RebuildCachedListAndStats();
                }
                GUILayout.EndHorizontal();

                // Coupled Resolution Rows for this same puzzle separated by a simple 1px horizontal line
                DrawSimpleConnectingLine();
                DrawCoupledSizeRow(rec, 256, "256px Icon", rec.IconSizeBytes, isSelectedPuzzle);

                if (rec.HasResolution(512))
                {
                    DrawSimpleConnectingLine();
                    DrawCoupledSizeRow(rec, 512, "512px Tile", rec.GetResolutionSizeBytes(512), isSelectedPuzzle);
                }

                DrawSimpleConnectingLine();
                DrawCoupledSizeRow(rec, 1080, "1080px Full", rec.FullSizeBytes, isSelectedPuzzle);

                if (rec.HasResolution(2048))
                {
                    DrawSimpleConnectingLine();
                    DrawCoupledSizeRow(rec, 2048, "2048px Ultra", rec.GetResolutionSizeBytes(2048), isSelectedPuzzle);
                }

                GUILayout.EndVertical();

                // Double vertical spacing between different puzzles
                GUILayout.Space(doublePuzzleSpacing);
            }

            int remainingAfter = totalPuzzles - 1 - lastVisible;
            if (remainingAfter > 0)
            {
                GUILayout.Space(remainingAfter * estimatedBlockHeight);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a simple 1px horizontal line coupling the different resolution tiers of the same puzzle.
        /// </summary>
        private void DrawSimpleConnectingLine()
        {
            GUILayout.Space(2);
            Rect lineRect = GUILayoutUtility.GetRect(10f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint && separatorLineTex != null)
            {
                GUI.DrawTexture(lineRect, separatorLineTex);
            }
            GUILayout.Space(2);
        }

        private void DrawCoupledSizeRow(ImageRequestRecord rec, int sizePx, string tierLabel, long sizeBytes, bool isSelectedPuzzle)
        {
            bool isActiveTier = isSelectedPuzzle && activeResolutionPx == sizePx;
            string fileName = RequestKeyGenerator.GetFileName(rec.RequestKey, sizePx);
            string activeMarker = isActiveTier ? "  <b>[ACTIVE PREVIEW]</b>" : "";
            string rowText = $"   ├─ <b>{tierLabel}</b> ({sizePx}×{sizePx}px)   |   File: {fileName}   |   Size: <b>{FormatBytes(sizeBytes)}</b>{activeMarker}";

            if (GUILayout.Button(rowText, tierRowButtonStyle, GUILayout.Height(22)))
            {
                selectedKey = rec.RequestKey;
                activeResolutionPx = sizePx;
                LoadPreview(rec.RequestKey, sizePx);
            }
        }

        private void DrawRightPreviewPanel(NativeImagePipeline pipeline, ImageRequestRecord selectedRec, string storageBadge)
        {
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Selected Key:</b> {selectedKey ?? "None"}   |   <b>Active Tier:</b> {activeResolutionPx}px ({storageBadge})", richLabelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("⛶ Fullscreen View", GUILayout.Width(135), GUILayout.Height(24)))
            {
                isFullscreenPreview = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(previewInfo, richLabelStyle);

            if (!string.IsNullOrEmpty(selectedKey))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Cycle Size ({activeResolutionPx}px -> Next)", GUILayout.Height(26)))
                {
                    CycleActiveResolution();
                }
                if (GUILayout.Button($"Open Cache Folder ({storageBadge})", GUILayout.Height(26)))
                {
                    pipeline.RevealInFileExplorer(selectedKey, activeResolutionPx);
                    showPathDetailsBar = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("256px Icon", GUILayout.Height(24))) { activeResolutionPx = 256; LoadPreview(selectedKey, 256); }
                if (GUILayout.Button("512px Tile", GUILayout.Height(24))) { activeResolutionPx = 512; LoadPreview(selectedKey, 512); }
                if (GUILayout.Button("1080px Full", GUILayout.Height(24))) { activeResolutionPx = 1080; LoadPreview(selectedKey, 1080); }
                if (GUILayout.Button("2048px Ultra", GUILayout.Height(24))) { activeResolutionPx = 2048; LoadPreview(selectedKey, 2048); }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Full-Size Responsive Preview Box filling all remaining space in the right panel
                Rect previewArea = GUILayoutUtility.GetRect(
                    200f,
                    200f,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true));

                if (previewSprite != null && previewSprite.texture != null)
                {
                    GUI.DrawTexture(previewArea, previewSprite.texture, ScaleMode.ScaleToFit, false);
                }
                else
                {
                    GUI.Box(previewArea, "Select any cached puzzle row on the left to display its full-size Sprite.");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawFullscreenPreviewMode(NativeImagePipeline pipeline, ImageRequestRecord selectedRec, string storageBadge)
        {
            GUILayout.BeginArea(new Rect(12, 10, Screen.width - 24, Screen.height - 54));
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"<b>FULLSCREEN PREVIEW</b> — <b>{selectedKey ?? "None"}</b> | <b>{activeResolutionPx}×{activeResolutionPx}px</b> ({storageBadge}) | {previewInfo}",
                richLabelStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button($"Cycle Size ({activeResolutionPx}px -> Next)", GUILayout.Width(190), GUILayout.Height(28)))
            {
                CycleActiveResolution();
            }
            if (GUILayout.Button("256px", GUILayout.Width(60), GUILayout.Height(28))) { activeResolutionPx = 256; LoadPreview(selectedKey, 256); }
            if (GUILayout.Button("512px", GUILayout.Width(60), GUILayout.Height(28))) { activeResolutionPx = 512; LoadPreview(selectedKey, 512); }
            if (GUILayout.Button("1080px", GUILayout.Width(65), GUILayout.Height(28))) { activeResolutionPx = 1080; LoadPreview(selectedKey, 1080); }
            if (GUILayout.Button("2048px", GUILayout.Width(65), GUILayout.Height(28))) { activeResolutionPx = 2048; LoadPreview(selectedKey, 2048); }

            if (GUILayout.Button("Open Cache Folder", GUILayout.Width(140), GUILayout.Height(28)))
            {
                if (!string.IsNullOrEmpty(selectedKey))
                {
                    pipeline.RevealInFileExplorer(selectedKey, activeResolutionPx);
                }
            }

            if (GUILayout.Button("✕ Exit Fullscreen [ESC]", GUILayout.Width(160), GUILayout.Height(28)))
            {
                isFullscreenPreview = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            Rect fullCanvasRect = GUILayoutUtility.GetRect(
                Screen.width - 48f,
                Screen.height - 120f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (previewSprite != null && previewSprite.texture != null)
            {
                GUI.DrawTexture(fullCanvasRect, previewSprite.texture, ScaleMode.ScaleToFit, false);
            }
            else
            {
                GUI.Box(fullCanvasRect, "No Sprite loaded yet.");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawRealtimeInternetSpeedBadge(NativeImagePipeline pipeline)
        {
            float currentMbps = pipeline.CurrentSpeedMbps;
            float peakMbps = pipeline.PeakSpeedMbps;
            float currentMBps = currentMbps / 8f;

            string statusText;
            if (!pipeline.IsOnline)
            {
                statusText = "<color=#f87171>OFFLINE (0.0 Mbps)</color>";
            }
            else if (statDownloading > 0 || currentMbps > 0.05f)
            {
                statusText = $"<color=#34d399><b>{currentMbps:F1} Mbps</b> ({currentMBps:F2} MB/s)</color>  |  Peak: {peakMbps:F1} Mbps  |  Active Downloads: {statDownloading}";
            }
            else
            {
                statusText = $"<color=#60a5fa>Online Idle (0.0 Mbps)</color>  |  Peak Session Speed: <b>{peakMbps:F1} Mbps</b> ({(peakMbps / 8f):F2} MB/s)";
            }

            float badgeWidth = Mathf.Min(Screen.width - 24f, 620f);
            Rect badgeRect = new Rect(Screen.width - badgeWidth - 12f, Screen.height - 38f, badgeWidth, 30f);
            GUI.Box(badgeRect, $"🌐 <b>Real-Time Internet Speed:</b> {statusText}", speedBadgeStyle);
        }

        private void DrawCacheLocationInspector(NativeImagePipeline pipeline, ImageRequestRecord rec)
        {
            bool isRom = rec.StorageTier == StorageTier.PermanentROM;
            string fileName = RequestKeyGenerator.GetFileName(rec.RequestKey, activeResolutionPx);
            long tierBytes = rec.GetResolutionSizeBytes(activeResolutionPx);
            string webglStore = isRom ? "permanent_rom" : "volatile_cache";
            string subFolder = isRom ? "ImageROM" : "ImageCache";

            string webglPath = $"indexeddb://Studio_NativeImagePipeline_DB/{webglStore}/{fileName}";
            string editorOrOsPath = DiskStorageHelper.GetFilePath(
                isRom ? pipeline.PermanentRomPath : pipeline.VolatileCachePath,
                rec.RequestKey,
                activeResolutionPx);

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Cache & ROM Folder Inspector ({fileName} — {FormatBytes(tierBytes)})</b>", richLabelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Folder Now", GUILayout.Width(130)))
            {
                pipeline.RevealInFileExplorer(rec.RequestKey, activeResolutionPx);
            }
            if (GUILayout.Button(isRom ? "Demote to Volatile Cache" : "Promote to Permanent ROM", GUILayout.Width(185)))
            {
                if (isRom) pipeline.MoveFromRomToCache(rec.RequestKey);
                else pipeline.SaveToPermanentROM(rec.RequestKey);
                RebuildCachedListAndStats();
            }
            if (GUILayout.Button("Copy Path", GUILayout.Width(95)))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                GUIUtility.systemCopyBuffer = webglPath;
#else
                GUIUtility.systemCopyBuffer = editorOrOsPath;
#endif
            }
            if (GUILayout.Button("Hide [X]", GUILayout.Width(70)))
            {
                showPathDetailsBar = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"<b>Local / OS File Path:</b> {editorOrOsPath}   |   <b>WebGL IndexedDB:</b> {webglPath}", richLabelStyle);
            GUILayout.EndVertical();
        }

        private void LoadPreview(string key, int sizePx)
        {
            if (string.IsNullOrEmpty(key)) return;
            previewInfo = $"Loading Sprite for '{key}' at {sizePx}px...";
            NativeImagePipeline.Instance.GetSpriteByCustomSize(
                key,
                sizePx,
                sprite =>
                {
                    previewSprite = sprite;
                    RebuildCachedListAndStats();
                    var rec = NativeImagePipeline.Instance.GetRequestStatus(key);
                    long diskBytes = rec != null ? rec.GetResolutionSizeBytes(sizePx) : 0;
                    string fileName = RequestKeyGenerator.GetFileName(key, sizePx);
                    previewInfo =
                        $"Loaded Sprite: <b>{sprite.texture.width}×{sprite.texture.height}</b> ({sprite.texture.format}) | File: <b>{fileName}</b> | " +
                        $"Disk JPEG Size: <b>{FormatBytes(diskBytes)}</b> | Storage: <b>{rec?.StorageTier}</b>";
                },
                err =>
                {
                    previewInfo = $"Error loading {sizePx}px Sprite: {err}";
                },
                RequestPriority.Critical);
        }

        private void BoostNextUnfinishedBatch(int count)
        {
            var toBoost = new List<string>();
            foreach (var kvp in NativeImagePipeline.Instance.AllRequests)
            {
                if (kvp.Value.IsInFlight)
                {
                    toBoost.Add(kvp.Key);
                    if (toBoost.Count >= count) break;
                }
            }
            NativeImagePipeline.Instance.PrioritizeViewportItems(toBoost);
            RebuildCachedListAndStats();
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024f):F1} KB";
            return $"{(bytes / (1024f * 1024f)):F2} MB";
        }
    }
}

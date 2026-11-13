using System;
using System.Collections.Generic;
using UnityEngine;

namespace CalmJigsaw.Core
{
    public class CalmJigsawGameUI : MonoBehaviour
    {
        private Texture2D darkBgTex;
        private Texture2D cardBgTex;
        private Texture2D buttonBgTex;
        private Texture2D buttonHoverTex;
        private Texture2D blueBtnTex;
        private Texture2D borderTex;
        private Texture2D ghostOverlayTex;
        private Texture2D shadowTex;
        private Texture2D emeraldBadgeTex;
        private Texture2D amberBadgeTex;
        private Texture2D purpleBadgeTex;
        private Texture2D slotBorderTex;

        private GUIStyle headerStyle;
        private GUIStyle btnStyle;
        private GUIStyle blueBtnStyle;
        private GUIStyle labelStyle;
        private GUIStyle badgeStyle;
        private GUIStyle toastStyle;
        private GUIStyle winTitleStyle;

        private Vector2 drawerScroll = Vector2.zero;
        private Vector2 lbScroll = Vector2.zero;
        private Rect boardScreenRect;

        private GUIStyle rightAlignLabelStyle;
        private GUIStyle amberLabelStyle;
        private GUIStyle emeraldLabelStyle;
        private GUIStyle roseLabelStyle;
        private GUIStyle rowNameStyle;

        private static float Hypot(float a, float b) => Mathf.Sqrt(a * a + b * b);

        private void Start()
        {
            GenerateProceduralTextures();
        }

        private Texture2D MakeSolidTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void GenerateProceduralTextures()
        {
            darkBgTex = MakeSolidTex(new Color(0.05f, 0.07f, 0.09f, 1f));     // #0d1117
            cardBgTex = MakeSolidTex(new Color(0.086f, 0.106f, 0.133f, 1f)); // #161b22
            buttonBgTex = MakeSolidTex(new Color(0.13f, 0.15f, 0.18f, 1f));  // #21262d
            buttonHoverTex = MakeSolidTex(new Color(0.19f, 0.21f, 0.24f, 1f)); // #30363d
            blueBtnTex = MakeSolidTex(new Color(0.15f, 0.39f, 0.92f, 1f));   // #2563eb
            borderTex = MakeSolidTex(new Color(0.19f, 0.21f, 0.24f, 1f));    // #30363d
            ghostOverlayTex = MakeSolidTex(new Color(1f, 1f, 1f, 0.32f));
            shadowTex = MakeSolidTex(new Color(0f, 0f, 0f, 0.48f));
            emeraldBadgeTex = MakeSolidTex(new Color(0.06f, 0.72f, 0.51f, 0.22f));
            amberBadgeTex = MakeSolidTex(new Color(0.96f, 0.62f, 0.04f, 0.22f));
            purpleBadgeTex = MakeSolidTex(new Color(0.65f, 0.55f, 0.98f, 0.22f));
            slotBorderTex = MakeSolidTex(new Color(0.55f, 0.62f, 0.72f, 0.25f));
        }

        private void InitStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.90f, 0.93f, 0.95f);

            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnStyle.normal.background = buttonBgTex;
            btnStyle.normal.textColor = new Color(0.90f, 0.93f, 0.95f);
            btnStyle.hover.background = buttonHoverTex;
            btnStyle.hover.textColor = Color.white;

            blueBtnStyle = new GUIStyle(btnStyle);
            blueBtnStyle.normal.background = blueBtnTex;
            blueBtnStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(0.55f, 0.58f, 0.62f);

            rightAlignLabelStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleRight
            };
            rightAlignLabelStyle.normal.textColor = new Color(0.90f, 0.93f, 0.95f);

            amberLabelStyle = new GUIStyle(labelStyle);
            amberLabelStyle.normal.textColor = new Color(0.98f, 0.75f, 0.14f);

            emeraldLabelStyle = new GUIStyle(labelStyle);
            emeraldLabelStyle.normal.textColor = new Color(0.20f, 0.83f, 0.60f);

            roseLabelStyle = new GUIStyle(labelStyle);
            roseLabelStyle.normal.textColor = new Color(0.98f, 0.44f, 0.52f);

            rowNameStyle = new GUIStyle(labelStyle);
            rowNameStyle.normal.textColor = new Color(0.90f, 0.93f, 0.95f);

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            toastStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            toastStyle.normal.background = cardBgTex;
            toastStyle.normal.textColor = new Color(0.90f, 0.93f, 0.95f);

            winTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            winTitleStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            InitStyles();
            var gm = CalmJigsawGameManager.Instance;
            if (gm == null) return;

            // Fullscreen dark background
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkBgTex);

            float totalW = Screen.width;
            float totalH = Screen.height;

            // App shell dimensions (matches web version 926px expanded / 760px collapsed)
            float drawerW = gm.drawerExpanded ? 290f : 148f;
            float maxShellW = Mathf.Min(totalW - 16f, 760f + (gm.drawerExpanded ? 150f : 0f));
            float shellX = (totalW - maxShellW) * 0.5f;
            float shellY = 8f;
            float shellH = totalH - 16f;

            Rect shellRect = new Rect(shellX, shellY, maxShellW, shellH);

            // Draw Card Shell
            GUI.DrawTexture(shellRect, cardBgTex);
            DrawBorder(shellRect, borderTex, 1);

            // --- 1. TOP HEADER (Row 1 & Row 2) ---
            float headerH = 74f;
            Rect headerRect = new Rect(shellX, shellY, maxShellW, headerH);
            GUI.DrawTexture(headerRect, cardBgTex);
            DrawBorder(headerRect, borderTex, 1);

            // Row 1: Left (Title + Placed Badge) & Right (Scene, Grid, Shuffle, Timer, Leaderboard Rank, Coins)
            float r1Y = shellY + 6f;
            float leftX = shellX + 12f;

            GUI.Label(new Rect(leftX, r1Y, 104f, 24f), "Calm Jigsaw", headerStyle);
            leftX += 106f;

            int lockedCount = gm.pieces.FindAll(p => p.status == PieceStatus.Locked).Count;
            int totalPieces = gm.gridSize * gm.gridSize;
            GUI.color = new Color(0.2f, 0.9f, 0.5f);
            GUI.Box(new Rect(leftX, r1Y + 2f, 82f, 22f), $"{lockedCount}/{totalPieces} Placed", badgeStyle);
            GUI.color = Color.white;

            // Right-aligned controls on Row 1
            float rightX = shellX + maxShellW - 10f;

            // Coins Badge
            rightX -= 68f;
            GUI.color = new Color(1f, 0.75f, 0.2f);
            GUI.Box(new Rect(rightX, r1Y + 2f, 68f, 22f), $"{gm.coins}c", badgeStyle);
            GUI.color = Color.white;

            // Top Leaderboard Rank Button (#30)
            rightX -= 58f;
            GUIStyle topLbBtnStyle = (gm.rightTab == RightPanelTab.Leaderboard) ? blueBtnStyle : btnStyle;
            if (GUI.Button(new Rect(rightX, r1Y + 2f, 54f, 22f), $"#{gm.topHeaderJigsawRank}", topLbBtnStyle))
            {
                if (gm.rightTab == RightPanelTab.Leaderboard)
                {
                    gm.rightTab = RightPanelTab.Drawer;
                }
                else
                {
                    gm.rightTab = RightPanelTab.Leaderboard;
                    gm.drawerExpanded = true;
                    gm.LbRefreshView();
                }
            }

            // Live Run Timer Display (00:00)
            rightX -= 58f;
            int totalSec = Mathf.Max(0, Mathf.FloorToInt(gm.runElapsedSeconds));
            string timerStr = $"{(totalSec / 60):D2}:{(totalSec % 60):D2}";
            GUI.Box(new Rect(rightX, r1Y + 2f, 54f, 22f), timerStr, badgeStyle);

            // Shuffle Button
            rightX -= 68f;
            if (GUI.Button(new Rect(rightX, r1Y + 2f, 64f, 22f), "Shuffle", blueBtnStyle))
            {
                gm.ShuffleTrayPieces();
                gm.ShowToast("Shuffled pieces in the drawer!");
            }

            // Grid Size Cycle
            rightX -= 84f;
            if (GUI.Button(new Rect(rightX, r1Y + 2f, 80f, 22f), $"{gm.gridSize}×{gm.gridSize} ({totalPieces})", btnStyle))
            {
                int[] sizes = new int[] { 4, 5, 6, 7, 8, 9, 10 };
                int idx = Array.IndexOf(sizes, gm.gridSize);
                gm.gridSize = sizes[(idx + 1) % sizes.Length];
                gm.hintsLeft = 3;
                gm.magnetsLeft = 3;
                gm.InitPuzzle(false);
            }

            // Native Image Bridge IndexedDB Explorer Button (DB)
            rightX -= 36f;
            if (GUI.Button(new Rect(rightX, r1Y + 2f, 32f, 22f), "DB", btnStyle))
            {
                gm.OpenBridgeCacheExplorer();
            }

            // Server Sync Button
            rightX -= 62f;
            if (GUI.Button(new Rect(rightX, r1Y + 2f, 58f, 22f), gm.serverStatusText, btnStyle))
            {
                gm.SyncServerPuzzles(false);
            }

            // Server Puzzle / Demo Scene Cycle Button
            rightX -= 114f;
            string sceneName = gm.GetActivePuzzleTitle();
            if (GUI.Button(new Rect(rightX, r1Y + 2f, 110f, 22f), sceneName, gm.activeServerPuzzleIndex >= 0 ? blueBtnStyle : btnStyle))
            {
                gm.CyclePuzzleArtwork();
            }

            // Row 2: 5 Powerups (Hint, Magnet, Preview, Edges, Reset)
            float r2Y = shellY + 38f;
            float btnW = (maxShellW - 20f - 16f) / 5f;
            float pX = shellX + 10f;

            if (GUI.Button(new Rect(pX, r2Y, btnW, 26f), $"Hint ({gm.hintsLeft})", btnStyle))
            {
                gm.TriggerHint();
            }
            pX += btnW + 4f;

            if (GUI.Button(new Rect(pX, r2Y, btnW, 26f), $"Magnet ({gm.magnetsLeft})", btnStyle))
            {
                gm.TriggerMagnet();
            }
            pX += btnW + 4f;

            string prevTxt = gm.showPreview ? "Preview: ON" : "Preview";
            if (GUI.Button(new Rect(pX, r2Y, btnW, 26f), prevTxt, gm.showPreview ? blueBtnStyle : btnStyle))
            {
                gm.showPreview = !gm.showPreview;
            }
            pX += btnW + 4f;

            string edgeTxt = gm.edgesOnly ? "Edges: ON" : "Edges";
            if (GUI.Button(new Rect(pX, r2Y, btnW, 26f), edgeTxt, gm.edgesOnly ? blueBtnStyle : btnStyle))
            {
                gm.edgesOnly = !gm.edgesOnly;
            }
            pX += btnW + 4f;

            if (GUI.Button(new Rect(pX, r2Y, btnW, 26f), "Reset", btnStyle))
            {
                gm.hintsLeft = 3;
                gm.magnetsLeft = 3;
                gm.InitPuzzle(false);
            }

            // --- 2. MAIN PLAY AREA (Board Left/Center + Right Panel) ---
            float mainY = shellY + headerH;
            float mainH = shellH - headerH;

            float boardAreaW = maxShellW - drawerW;
            Rect boardAreaRect = new Rect(shellX, mainY, boardAreaW, mainH);

            // Compute square puzzle board rect inside boardAreaRect
            float boardPadding = 24f;
            float maxBoardDim = Mathf.Min(boardAreaW - boardPadding * 2f, mainH - boardPadding * 2f - 24f);
            float boardDim = Mathf.Max(200f, Mathf.Min(500f, maxBoardDim));
            gm.boardPixelSize = boardDim;

            float boardX = shellX + (boardAreaW - boardDim) * 0.5f;
            float boardY = mainY + (mainH - boardDim) * 0.5f - 10f;
            boardScreenRect = new Rect(boardX, boardY, boardDim, boardDim);

            // Draw Board Container
            GUI.DrawTexture(boardScreenRect, cardBgTex);
            DrawBorder(boardScreenRect, borderTex, 2);

            // Ghost Preview image if active
            if (gm.showPreview && gm.fullBoardTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.32f);
                GUI.DrawTexture(boardScreenRect, gm.fullBoardTexture);
                GUI.color = Color.white;
            }

            // Jigsaw-Shaped Slot Outlines (matches boardSlotsLayer in web version — exact Bezier piece silhouettes, not squares)
            float tileSize = boardDim / gm.gridSize;
            float slotPad = tileSize * 0.28f;
            float slotSpr = tileSize + slotPad * 2f;
            GUI.color = new Color(1f, 1f, 1f, 0.42f);
            foreach (var p in gm.pieces)
            {
                if (p.outlineTexture != null)
                {
                    Rect slotR = new Rect(boardX + p.col * tileSize - slotPad, boardY + p.row * tileSize - slotPad, slotSpr, slotSpr);
                    GUI.DrawTexture(slotR, p.outlineTexture);
                }
            }
            GUI.color = Color.white;

            // Draw Locked Pieces
            foreach (var p in gm.pieces)
            {
                if (p.status == PieceStatus.Locked && p.pieceTexture != null)
                {
                    float pad = tileSize * 0.28f;
                    float spr = tileSize + pad * 2f;
                    Rect pr = new Rect(boardX + p.col * tileSize - pad, boardY + p.row * tileSize - pad, spr, spr);
                    if (p.pulseScale > 1f)
                    {
                        float scaledSpr = spr * p.pulseScale;
                        pr = new Rect(pr.center.x - scaledSpr * 0.5f, pr.center.y - scaledSpr * 0.5f, scaledSpr, scaledSpr);
                    }
                    GUI.DrawTexture(pr, p.pieceTexture);
                }
            }

            // Draw Floating Board Pieces (with drop shadow)
            foreach (var p in gm.pieces)
            {
                if (p.status == PieceStatus.Board && p != gm.draggingLead && !gm.draggingGroup.Contains(p) && p.pieceTexture != null)
                {
                    float pad = tileSize * 0.28f;
                    float spr = tileSize + pad * 2f;

                    // Contact Shadow
                    Rect shadowR = new Rect(boardX + p.x - pad + 3f, boardY + p.y - pad + 6f, spr, spr);
                    GUI.color = new Color(0f, 0f, 0f, 0.45f);
                    GUI.DrawTexture(shadowR, p.pieceTexture);
                    GUI.color = Color.white;

                    Rect pr = new Rect(boardX + p.x - pad, boardY + p.y - pad, spr, spr);
                    GUI.DrawTexture(pr, p.pieceTexture);
                }
            }

            // Draw Visual Effects (Shockwave Rings & Sparkles)
            foreach (var eff in gm.activeEffects)
            {
                if (eff.type == VisualEffectItem.EffectType.Ring)
                {
                    float dia = eff.radius * 2f;
                    Rect ringR = new Rect(boardX + eff.pos.x - eff.radius, boardY + eff.pos.y - eff.radius, dia, dia);
                    GUI.color = new Color(eff.color.r, eff.color.g, eff.color.b, eff.alpha);
                    DrawBorder(ringR, borderTex, 2);
                    GUI.color = Color.white;
                }
                else if (eff.type == VisualEffectItem.EffectType.Sparkle)
                {
                    Rect sparkR = new Rect(boardX + eff.pos.x - eff.size * 0.5f, boardY + eff.pos.y - eff.size * 0.5f, eff.size, eff.size);
                    GUI.color = new Color(eff.color.r, eff.color.g, eff.color.b, eff.alpha);
                    GUI.DrawTexture(sparkR, MakeSolidTex(eff.color));
                    GUI.color = Color.white;
                }
            }

            // Toast Message at bottom of jigsaw board
            if (!string.IsNullOrEmpty(gm.toastMessage))
            {
                float toastW = Mathf.Min(maxBoardDim, 360f);
                Rect toastR = new Rect(boardX + (boardDim - toastW) * 0.5f, boardY + boardDim + 10f, toastW, 28f);
                GUI.color = gm.isToastError ? new Color(0.9f, 0.2f, 0.2f) : Color.white;
                GUI.Box(toastR, gm.toastMessage, toastStyle);
                DrawBorder(toastR, borderTex, 1);
                GUI.color = Color.white;
            }

            // --- 3. RIGHT SIDE PANEL: TABS FOR PIECE DRAWER & REAL-TIME OFFLINE LEADERBOARD ---
            float drawerX = shellX + boardAreaW;
            Rect drawerRect = new Rect(drawerX, mainY, drawerW, mainH);
            GUI.DrawTexture(drawerRect, cardBgTex);
            DrawBorder(drawerRect, borderTex, 1);

            float dHeadH = 30f;
            var trayPieces = gm.pieces.FindAll(p =>
                p.status == PieceStatus.Tray &&
                p != gm.draggingLead &&
                !gm.draggingGroup.Contains(p) &&
                (!gm.edgesOnly || p.isEdge));

            int unplacedCount = gm.pieces.FindAll(p =>
                p.status == PieceStatus.Tray &&
                p != gm.draggingLead &&
                !gm.draggingGroup.Contains(p)).Count;

            string dTitle = gm.edgesOnly ? $"Edges ({unplacedCount})" : $"Drawer ({unplacedCount})";
            GUIStyle drawerTabStyle = (gm.rightTab == RightPanelTab.Drawer) ? blueBtnStyle : btnStyle;
            GUIStyle lbTabStyle = (gm.rightTab == RightPanelTab.Leaderboard) ? blueBtnStyle : btnStyle;

            float tab1W = gm.drawerExpanded ? 96f : 82f;
            if (GUI.Button(new Rect(drawerX + 6f, mainY + 6f, tab1W, 22f), dTitle, drawerTabStyle))
            {
                gm.rightTab = RightPanelTab.Drawer;
            }

            float tab2W = gm.drawerExpanded ? 94f : 28f;
            string lbTabLabel = gm.drawerExpanded ? "Leaderboard" : "LB";
            if (GUI.Button(new Rect(drawerX + 6f + tab1W + 4f, mainY + 6f, tab2W, 22f), lbTabLabel, lbTabStyle))
            {
                gm.rightTab = RightPanelTab.Leaderboard;
                gm.drawerExpanded = true;
                gm.LbRefreshView();
            }

            string arrow = gm.drawerExpanded ? "<<" : ">>";
            if (GUI.Button(new Rect(drawerX + drawerW - 32f, mainY + 6f, 26f, 22f), arrow, btnStyle))
            {
                gm.drawerExpanded = !gm.drawerExpanded;
                if (!gm.drawerExpanded && gm.rightTab == RightPanelTab.Leaderboard)
                {
                    gm.rightTab = RightPanelTab.Drawer;
                }
            }

            float panelContentY = mainY + dHeadH + 6f;
            float panelContentH = mainH - dHeadH - 14f;

            if (gm.rightTab == RightPanelTab.Drawer)
            {
                // TAB 1: JIGSAW PIECE TRAY
                Rect trayViewRect = new Rect(drawerX + 6f, panelContentY, drawerW - 12f, panelContentH);
                GUI.DrawTexture(trayViewRect, darkBgTex);
                DrawBorder(trayViewRect, borderTex, 1);

                int cols = gm.drawerExpanded ? (gm.gridSize <= 5 ? 2 : (gm.gridSize <= 7 ? 3 : 4)) : (gm.gridSize <= 6 ? 1 : 2);
                float thumbSize = Mathf.Clamp((trayViewRect.width - 24f) / cols - 6f, 44f, 96f);
                int rows = Mathf.CeilToInt((float)trayPieces.Count / cols);
                float contentH = Mathf.Max(panelContentH - 4f, rows * (thumbSize + 8f) + 12f);

                float gridTotalW = cols * thumbSize + (cols - 1) * 6f;
                float startX = Mathf.Max(6f, (trayViewRect.width - 18f - gridTotalW) * 0.5f);

                Rect trayContentRect = new Rect(0, 0, trayViewRect.width - 18f, contentH);
                drawerScroll = GUI.BeginScrollView(trayViewRect, drawerScroll, trayContentRect);

                for (int i = 0; i < trayPieces.Count; i++)
                {
                    var tp = trayPieces[i];
                    int c = i % cols;
                    int r = i / cols;
                    float tx = startX + c * (thumbSize + 6f);
                    float ty = r * (thumbSize + 8f) + 6f;

                    Rect thumbR = new Rect(tx, ty, thumbSize, thumbSize);

                    if (tp.thumbTexture != null)
                    {
                        Rect shadowR = new Rect(thumbR.x + 2f, thumbR.y + 4f, thumbR.width, thumbR.height);
                        GUI.color = new Color(0f, 0f, 0f, 0.45f);
                        GUI.DrawTexture(shadowR, tp.thumbTexture);
                        GUI.color = Color.white;

                        GUI.DrawTexture(thumbR, tp.thumbTexture);
                    }

                    // Handle pointer down on drawer piece: takes that exact piece out of drawer
                    Event e = Event.current;
                    if (e.type == EventType.MouseDown && thumbR.Contains(e.mousePosition) && !gm.isAnimatingPowerup && gm.draggingLead == null)
                    {
                        gm.draggingLead = tp;
                        gm.draggingGroup = new List<JigsawPiece> { tp };
                        gm.dragOffset = Vector2.zero;
                        gm.dragIsFromDrawer = true;
                        e.Use();
                    }
                }
                GUI.EndScrollView();
            }
            else
            {
                // TAB 2: REAL-TIME OFFLINE LEADERBOARD PANEL (Matches Meowsweeper & Web Version)
                DrawLeaderboardPanel(gm, drawerX + 6f, panelContentY, drawerW - 12f, panelContentH);
            }

            // --- 4. DRAG & DROP POINTER HANDLING ---
            HandleMouseInput();

            // --- 4B. FOREGROUND DRAG LAYER (Rendered on top of both Drawer and Board) ---
            if (gm.draggingLead != null)
            {
                Vector2 mousePos = Event.current.mousePosition;
                float pad = tileSize * 0.28f;
                float spr = (tileSize + pad * 2f) * 1.04f; // 3D lift scale

                foreach (var p in gm.draggingGroup)
                {
                    float relX = (p.col - gm.draggingLead.col) * tileSize;
                    float relY = (p.row - gm.draggingLead.row) * tileSize;
                    Vector2 pieceCurPos = mousePos + gm.dragOffset + new Vector2(relX, relY);

                    // Large dynamic contact shadow for lift
                    Rect sR = new Rect(pieceCurPos.x - spr * 0.5f + 5f, pieceCurPos.y - spr * 0.5f + 14f, spr, spr);
                    GUI.color = new Color(0f, 0f, 0f, 0.55f);
                    GUI.DrawTexture(sR, p.pieceTexture);
                    GUI.color = Color.white;

                    Rect pR = new Rect(pieceCurPos.x - spr * 0.5f, pieceCurPos.y - spr * 0.5f, spr, spr);
                    GUI.DrawTexture(pR, p.pieceTexture);
                }
            }

            // --- 5. PUZZLE COMPLETE OVERLAY ---
            if (gm.isPuzzleComplete)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.65f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkBgTex);
                GUI.color = Color.white;

                float winW = 340f;
                float winH = 230f;
                Rect winRect = new Rect((Screen.width - winW) * 0.5f, (Screen.height - winH) * 0.5f, winW, winH);
                GUI.DrawTexture(winRect, cardBgTex);
                DrawBorder(winRect, borderTex, 2);

                GUI.Label(new Rect(winRect.x, winRect.y + 18f, winW, 28f), "Puzzle Complete!", winTitleStyle);
                GUI.Box(new Rect(winRect.x + 16f, winRect.y + 56f, winW - 32f, 26f), gm.winSummaryText, badgeStyle);

                GUI.color = new Color(1f, 0.75f, 0.2f);
                GUI.Box(new Rect(winRect.x + 24f, winRect.y + 92f, winW - 48f, 26f), gm.winRankBadgeText, badgeStyle);
                GUI.color = Color.white;

                float halfBtnW = (winW - 44f) * 0.5f;
                if (GUI.Button(new Rect(winRect.x + 18f, winRect.y + 150f, halfBtnW, 38f), "Leaderboard", btnStyle))
                {
                    gm.isPuzzleComplete = false;
                    gm.rightTab = RightPanelTab.Leaderboard;
                    gm.drawerExpanded = true;
                    gm.LbRefreshView();
                }

                if (GUI.Button(new Rect(winRect.x + 26f + halfBtnW, winRect.y + 150f, halfBtnW, 38f), "Play Again", blueBtnStyle))
                {
                    gm.hintsLeft = 3;
                    gm.magnetsLeft = 3;
                    gm.InitPuzzle(false);
                }
            }
        }

        private void DrawLeaderboardPanel(CalmJigsawGameManager gm, float x, float y, float w, float h)
        {
            var v = gm.lbCurrentView;
            float curY = y;

            // 1. Mode Sub-Tabs (Best Time vs Unified + Countdown)
            Rect modeBox = new Rect(x, curY, w, 32f);
            GUI.DrawTexture(modeBox, darkBgTex);
            DrawBorder(modeBox, borderTex, 1);

            GUIStyle bestStyle = (gm.lbActiveMode == LeaderboardMode.CalmJigsaw) ? blueBtnStyle : btnStyle;
            GUIStyle uniStyle = (gm.lbActiveMode == LeaderboardMode.UnifiedLeague) ? blueBtnStyle : btnStyle;

            if (GUI.Button(new Rect(x + 4f, curY + 5f, 76f, 22f), "Best Time", bestStyle))
            {
                gm.lbActiveMode = LeaderboardMode.CalmJigsaw;
                gm.LbRefreshView();
            }
            if (GUI.Button(new Rect(x + 84f, curY + 5f, 68f, 22f), "Unified", uniStyle))
            {
                gm.lbActiveMode = LeaderboardMode.UnifiedLeague;
                gm.LbRefreshView();
            }

            GUI.Label(new Rect(x + w - 96f, curY + 5f, 90f, 22f), v.countdownText, amberLabelStyle);
            curY += 38f;

            // 2. Player Rank & Score KPI Strip
            Rect kpiBox = new Rect(x, curY, w, 28f);
            GUI.DrawTexture(kpiBox, darkBgTex);
            DrawBorder(kpiBox, borderTex, 1);

            GUI.Label(new Rect(x + 8f, curY + 4f, 38f, 20f), "Rank:", labelStyle);
            GUI.Label(new Rect(x + 44f, curY + 4f, 52f, 20f), $"#{v.playerRank}/{v.totalEntries}", amberLabelStyle);

            if (v.netDelta > 0)
            {
                GUI.Label(new Rect(x + 96f, curY + 4f, 44f, 20f), $"+{v.netDelta}", emeraldLabelStyle);
            }
            else if (v.netDelta < 0)
            {
                GUI.Label(new Rect(x + 96f, curY + 4f, 44f, 20f), $"{v.netDelta}", roseLabelStyle);
            }

            GUI.Label(new Rect(x + w - 118f, curY + 4f, 110f, 20f), $"{v.scoreLabel} {v.playerFormattedScore}", rightAlignLabelStyle);
            curY += 34f;

            // 3. Season Expired / Idempotent Reward Claim Banner
            if (v.isExpired)
            {
                Rect claimBox = new Rect(x, curY, w, 30f);
                GUI.DrawTexture(claimBox, amberBadgeTex);
                DrawBorder(claimBox, borderTex, 1);

                if (!v.rewardClaimed)
                {
                    GUI.Label(new Rect(x + 6f, curY + 5f, w - 72f, 20f), $"Rank #{v.playerRank}: +{v.rewardTierCoins} Coins", amberLabelStyle);
                    if (GUI.Button(new Rect(x + w - 62f, curY + 4f, 56f, 22f), "Claim", blueBtnStyle))
                    {
                        gm.LbClaimReward();
                    }
                }
                else
                {
                    GUI.Label(new Rect(x + 6f, curY + 5f, w - 72f, 20f), $"Claimed +{v.rewardTierCoins} Coins!", emeraldLabelStyle);
                    if (GUI.Button(new Rect(x + w - 62f, curY + 4f, 56f, 22f), "Next", blueBtnStyle))
                    {
                        gm.LbNextSeason();
                    }
                }
                curY += 36f;
            }

            // 4. Pooled User-Centered Window Rows
            float footerH = 30f;
            float listH = Mathf.Max(80f, (y + h) - curY - footerH - 6f);
            Rect rowsBox = new Rect(x, curY, w, listH);
            GUI.DrawTexture(rowsBox, darkBgTex);
            DrawBorder(rowsBox, borderTex, 1);

            float rowH = 26f;
            float totalRowsH = Mathf.Max(listH - 4f, v.windowedRows.Count * rowH + 6f);
            Rect rowsContent = new Rect(0, 0, w - 16f, totalRowsH);
            lbScroll = GUI.BeginScrollView(rowsBox, lbScroll, rowsContent);

            float ry = 2f;
            for (int i = 0; i < v.windowedRows.Count; i++)
            {
                var row = v.windowedRows[i];
                Rect rRect = new Rect(2f, ry, rowsContent.width - 4f, rowH - 2f);

                if (row.isDivider)
                {
                    GUI.DrawTexture(rRect, cardBgTex);
                    GUI.Label(new Rect(rRect.x + 8f, rRect.y + 2f, rRect.width - 16f, 20f), $"... {row.skippedCount} rivals ...", labelStyle);
                }
                else
                {
                    if (row.isPlayer)
                    {
                        GUI.DrawTexture(rRect, amberBadgeTex);
                        // Left accent bar
                        GUI.DrawTexture(new Rect(rRect.x, rRect.y, 3f, rRect.height), MakeSolidTex(new Color(0.98f, 0.75f, 0.14f)));
                    }
                    else if (row.rank <= 3)
                    {
                        GUI.DrawTexture(rRect, cardBgTex);
                    }

                    GUIStyle rStyle = row.isPlayer ? amberLabelStyle : labelStyle;
                    GUIStyle nStyle = row.isPlayer ? amberLabelStyle : rowNameStyle;

                    GUI.Label(new Rect(rRect.x + 6f, rRect.y + 2f, 34f, 20f), $"#{row.rank}", rStyle);
                    GUI.Label(new Rect(rRect.x + 40f, rRect.y + 2f, 108f, 20f), row.name, nStyle);
                    GUI.Label(new Rect(rRect.x + rRect.width - 104f, rRect.y + 2f, 36f, 20f), row.rewardCoinText, labelStyle);
                    GUI.Label(new Rect(rRect.x + rRect.width - 68f, rRect.y + 2f, 64f, 20f), row.formattedScore, rightAlignLabelStyle);
                }
                ry += rowH;
            }
            GUI.EndScrollView();

            // 5. Live-Ops Simulation Footer (+4h, -6h Rollback Clamp, End Season)
            float footY = y + h - footerH;
            if (GUI.Button(new Rect(x, footY + 4f, 44f, 22f), "+4h", btnStyle))
            {
                gm.LbPlus4h();
            }
            if (GUI.Button(new Rect(x + 48f, footY + 4f, 44f, 22f), "-6h", btnStyle))
            {
                gm.LbMinus6h();
            }
            if (GUI.Button(new Rect(x + 96f, footY + 4f, 44f, 22f), "End", btnStyle))
            {
                gm.LbExpireSeason();
            }

            GUIStyle phaseStyle = v.isClamped ? roseLabelStyle : rightAlignLabelStyle;
            GUI.Label(new Rect(x + w - 92f, footY + 4f, 90f, 22f), v.phaseText, phaseStyle);
        }

        private void HandleMouseInput()
        {
            var gm = CalmJigsawGameManager.Instance;
            if (gm == null) return;
            Event e = Event.current;

            // MouseDown on board pieces
            if (e.type == EventType.MouseDown && gm.draggingLead == null && !gm.isAnimatingPowerup)
            {
                Vector2 mPos = e.mousePosition;
                if (boardScreenRect.Contains(mPos))
                {
                    float clickBoardX = mPos.x - boardScreenRect.x;
                    float clickBoardY = mPos.y - boardScreenRect.y;

                    // Search board pieces top-most
                    var boardPieces = gm.pieces.FindAll(p => p.status == PieceStatus.Board);
                    for (int i = boardPieces.Count - 1; i >= 0; i--)
                    {
                        var p = boardPieces[i];
                        if (clickBoardX >= p.x && clickBoardX <= p.x + p.tileSize &&
                            clickBoardY >= p.y && clickBoardY <= p.y + p.tileSize)
                        {
                            gm.draggingLead = p;
                            gm.draggingGroup = gm.pieces.FindAll(o => o.status == PieceStatus.Board && o.groupId == p.groupId);
                            gm.dragOffset = new Vector2(boardScreenRect.x + p.x + p.tileSize * 0.5f - mPos.x, boardScreenRect.y + p.y + p.tileSize * 0.5f - mPos.y);
                            gm.dragIsFromDrawer = false;
                            e.Use();
                            break;
                        }
                    }
                }
            }

            // MouseDrag: Real-time auto-placement threshold check
            if (e.type == EventType.MouseDrag && gm.draggingLead != null)
            {
                Vector2 mPos = e.mousePosition;
                Vector2 effectiveCenter = mPos + gm.dragOffset;
                if (boardScreenRect.Contains(effectiveCenter))
                {
                    float leadBoardX = effectiveCenter.x - boardScreenRect.x - gm.draggingLead.tileSize * 0.5f;
                    float leadBoardY = effectiveCenter.y - boardScreenRect.y - gm.draggingLead.tileSize * 0.5f;

                    float targetX = gm.draggingLead.col * gm.draggingLead.tileSize;
                    float targetY = gm.draggingLead.row * gm.draggingLead.tileSize;
                    float dist = Hypot(leadBoardX - targetX, leadBoardY - targetY);

                    if (dist <= gm.snapThreshold)
                    {
                        var lockedList = new List<JigsawPiece>(gm.draggingGroup);
                        foreach (var p in gm.draggingGroup)
                        {
                            p.status = PieceStatus.Locked;
                            p.x = p.col * p.tileSize;
                            p.y = p.row * p.tileSize;
                        }
                        gm.draggingLead = null;
                        gm.draggingGroup.Clear();

                        gm.TriggerPlacementEffect(lockedList);
                        gm.CheckWinCondition();
                        e.Use();
                    }
                }
            }

            // MouseUp: Drop onto board or return to drawer
            if (e.type == EventType.MouseUp && gm.draggingLead != null)
            {
                Vector2 mPos = e.mousePosition;
                Vector2 effectiveCenter = mPos + gm.dragOffset;
                if (boardScreenRect.Contains(effectiveCenter))
                {
                    float leadBoardX = effectiveCenter.x - boardScreenRect.x - gm.draggingLead.tileSize * 0.5f;
                    float leadBoardY = effectiveCenter.y - boardScreenRect.y - gm.draggingLead.tileSize * 0.5f;

                    float targetX = gm.draggingLead.col * gm.draggingLead.tileSize;
                    float targetY = gm.draggingLead.row * gm.draggingLead.tileSize;
                    float dist = Hypot(leadBoardX - targetX, leadBoardY - targetY);

                    if (dist <= gm.snapThreshold)
                    {
                        var lockedList = new List<JigsawPiece>(gm.draggingGroup);
                        foreach (var p in gm.draggingGroup)
                        {
                            p.status = PieceStatus.Locked;
                            p.x = p.col * p.tileSize;
                            p.y = p.row * p.tileSize;
                        }
                        gm.TriggerPlacementEffect(lockedList);
                        gm.CheckWinCondition();
                    }
                    else
                    {
                        // Place group onto board
                        foreach (var p in gm.draggingGroup)
                        {
                            float relX = (p.col - gm.draggingLead.col) * p.tileSize;
                            float relY = (p.row - gm.draggingLead.row) * p.tileSize;
                            p.status = PieceStatus.Board;
                            p.x = leadBoardX + relX;
                            p.y = leadBoardY + relY;
                        }
                        gm.CheckAndMergeBoardNeighbors(gm.draggingLead.groupId);
                    }
                }
                else
                {
                    // Dropped outside board
                    if (gm.draggingGroup.Count == 1)
                    {
                        gm.draggingLead.status = PieceStatus.Tray;
                    }
                    else
                    {
                        // Clamp group onto board
                        float minCol = float.MaxValue, maxCol = float.MinValue;
                        float minRow = float.MaxValue, maxRow = float.MinValue;
                        foreach (var p in gm.draggingGroup)
                        {
                            if (p.col < minCol) minCol = p.col;
                            if (p.col > maxCol) maxCol = p.col;
                            if (p.row < minRow) minRow = p.row;
                            if (p.row > maxRow) maxRow = p.row;
                        }

                        float grpW = (maxCol - minCol + 1f) * gm.draggingLead.tileSize;
                        float grpH = (maxRow - minRow + 1f) * gm.draggingLead.tileSize;

                        float clampedX = Mathf.Clamp(mPos.x - boardScreenRect.x, 0f, Mathf.Max(0f, gm.boardPixelSize - grpW));
                        float clampedY = Mathf.Clamp(mPos.y - boardScreenRect.y, 0f, Mathf.Max(0f, gm.boardPixelSize - grpH));

                        foreach (var p in gm.draggingGroup)
                        {
                            p.status = PieceStatus.Board;
                            p.x = clampedX + (p.col - minCol) * p.tileSize;
                            p.y = clampedY + (p.row - minRow) * p.tileSize;
                        }
                    }
                }

                gm.draggingLead = null;
                gm.draggingGroup.Clear();
                e.Use();
            }
        }

        private void DrawBorder(Rect r, Texture2D tex, int thickness)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), tex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), tex);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), tex);
            GUI.DrawTexture(new Rect(r.x + r.width - thickness, r.y, thickness, r.height), tex);
        }
    }
}

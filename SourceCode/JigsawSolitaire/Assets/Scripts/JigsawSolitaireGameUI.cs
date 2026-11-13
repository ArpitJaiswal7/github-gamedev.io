using System;
using System.Collections.Generic;
using UnityEngine;

namespace JigsawSolitaire.Core
{
    public class JigsawSolitaireGameUI : MonoBehaviour
    {
        private Texture2D darkBgTex;
        private Texture2D cardBgTex;
        private Texture2D buttonBgTex;
        private Texture2D buttonHoverTex;
        private Texture2D blueBtnTex;
        private Texture2D purpleBtnTex;
        private Texture2D borderTex;
        private Texture2D shadowTex;
        private Texture2D emeraldBadgeTex;
        private Texture2D amberBadgeTex;
        private Texture2D purpleBadgeTex;
        private Texture2D cardBorderTex;
        private Texture2D dropHighlightTex;

        private GUIStyle headerStyle;
        private GUIStyle btnStyle;
        private GUIStyle blueBtnStyle;
        private GUIStyle purpleBtnStyle;
        private GUIStyle labelStyle;
        private GUIStyle rightAlignLabelStyle;
        private GUIStyle amberLabelStyle;
        private GUIStyle emeraldLabelStyle;
        private GUIStyle roseLabelStyle;
        private GUIStyle rowNameStyle;
        private GUIStyle badgeStyle;
        private GUIStyle toastStyle;
        private GUIStyle winTitleStyle;

        private Vector2 lbScroll = Vector2.zero;
        private Vector2 catalogScroll = Vector2.zero;
        private Rect boardScreenRect;

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
            purpleBtnTex = MakeSolidTex(new Color(0.55f, 0.25f, 0.95f, 1f)); // #8b5cf6
            borderTex = MakeSolidTex(new Color(0.19f, 0.21f, 0.24f, 1f));    // #30363d
            shadowTex = MakeSolidTex(new Color(0f, 0f, 0f, 0.55f));
            emeraldBadgeTex = MakeSolidTex(new Color(0.06f, 0.72f, 0.51f, 0.22f));
            amberBadgeTex = MakeSolidTex(new Color(0.96f, 0.62f, 0.04f, 0.22f));
            purpleBadgeTex = MakeSolidTex(new Color(0.65f, 0.55f, 0.98f, 0.22f));
            cardBorderTex = MakeSolidTex(new Color(1f, 1f, 1f, 0.38f));
            dropHighlightTex = MakeSolidTex(new Color(0.22f, 0.74f, 0.97f, 0.25f));
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

            purpleBtnStyle = new GUIStyle(btnStyle);
            purpleBtnStyle.normal.background = purpleBtnTex;
            purpleBtnStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(0.55f, 0.58f, 0.62f);

            rightAlignLabelStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleRight };
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
            var gm = JigsawSolitaireGameManager.Instance;
            if (gm == null) return;

            float screenW = Screen.width;
            float screenH = Screen.height;

            // Fullscreen dark background (#0d1117)
            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkBgTex);

            float headerH = 76f;
            float margin = 8f;
            float rightPanelW = gm.rightPanelExpanded ? Mathf.Min(340f, screenW * 0.36f) : 0f;
            float boardAreaW = screenW - (margin * 2f) - (rightPanelW > 0 ? (rightPanelW + margin) : 0f);
            float boardAreaH = screenH - headerH - (margin * 2f);

            // Draw Top Header
            DrawHeader(new Rect(margin, margin, screenW - margin * 2f, headerH - margin), gm);

            // Calculate Board Dimensions (Vertical rectangle, fixed 0.80 aspect)
            float maxBoardH = boardAreaH - 32f;
            float maxBoardW = boardAreaW - 32f;
            float calcH = maxBoardH;
            float calcW = calcH * JigsawSolitaireGameManager.BOARD_ASPECT;

            if (calcW > maxBoardW)
            {
                calcW = maxBoardW;
                calcH = calcW / JigsawSolitaireGameManager.BOARD_ASPECT;
            }

            // Snap to multiple of cols/rows
            calcW = Mathf.Floor(calcW / gm.cols) * gm.cols;
            calcH = Mathf.Floor(calcH / gm.rows) * gm.rows;
            gm.boardWidth = calcW;
            gm.boardHeight = calcH;

            float boardX = margin + (boardAreaW - calcW) * 0.5f;
            float boardY = headerH + (boardAreaH - calcH) * 0.5f;
            boardScreenRect = new Rect(boardX, boardY, calcW, calcH);

            // Draw Board Frame & Cards
            DrawBoard(boardScreenRect, gm);

            // Draw Right Panel (Leaderboard / Catalog)
            if (rightPanelW > 0)
            {
                float rpX = screenW - margin - rightPanelW;
                float rpH = screenH - headerH - (margin * 2f);
                DrawRightPanel(new Rect(rpX, headerH, rightPanelW, rpH), gm);
            }

            // Draw Active Flight Animations
            DrawFlightAnimations(boardScreenRect, gm);

            // Draw Visual Effects
            DrawVisualEffects(boardScreenRect, gm);

            // Draw Dragging Cluster
            DrawDraggingCluster(boardScreenRect, gm);

            // Draw Toast Banner
            if (!string.IsNullOrEmpty(gm.activeToastMessage))
            {
                float toastW = Mathf.Min(480f, screenW - 40f);
                float toastH = 34f;
                Rect toastRect = new Rect((screenW - toastW) * 0.5f, screenH - toastH - 12f, toastW, toastH);
                GUI.DrawTexture(toastRect, cardBgTex);
                GUI.DrawTexture(toastRect, borderTex, ScaleMode.StretchToFill, true, 0, gm.toastIsError ? Color.red : new Color(0.2f, 0.4f, 0.8f), 1, 0);
                GUI.Label(toastRect, gm.activeToastMessage, toastStyle);
            }

            // Draw Win Overlay
            if (gm.isPuzzleComplete)
            {
                DrawWinOverlay(new Rect(0, 0, screenW, screenH), gm);
            }
        }

        private void DrawHeader(Rect r, JigsawSolitaireGameManager gm)
        {
            GUI.DrawTexture(r, cardBgTex);
            GUI.DrawTexture(r, borderTex, ScaleMode.StretchToFill, true, 0, Color.clear, 1, 0);

            // Row 1: Title, Connected badge, Mode/Grid dropdowns, Shuffle, Timer, Leaderboard toggle, Coins
            float y1 = r.y + 6f;
            float x = r.x + 10f;

            GUI.Label(new Rect(x, y1, 190f, 24f), "🃏 Calm Jigsaw Solitaire", headerStyle);
            x += 195f;

            int conn = gm.CountConnectedCards();
            int total = gm.rows * gm.cols;
            badgeStyle.normal.background = emeraldBadgeTex;
            badgeStyle.normal.textColor = new Color(0.20f, 0.83f, 0.60f);
            GUI.Box(new Rect(x, y1 + 2f, 105f, 20f), $"{conn}/{total} Connected", badgeStyle);
            x += 112f;

            // Scene / Server button
            string sceneName = gm.activeServerPuzzleIndex >= 0 && gm.activeServerPuzzleIndex < gm.serverPuzzles.Count
                ? "🌐 " + gm.serverPuzzles[gm.activeServerPuzzleIndex].title
                : gm.currentScene.ToString();
            if (sceneName.Length > 12) sceneName = sceneName.Substring(0, 11) + "…";

            if (GUI.Button(new Rect(x, y1, 95f, 24f), sceneName, btnStyle))
            {
                gm.rightPanelExpanded = true;
                gm.rightTab = RightPanelTab.PuzzlesCatalog;
            }
            x += 100f;

            // Grid size button (cycles 3x3 -> 4x3 -> 4x4 -> 5x4 -> 5x5 -> 6x5 -> 6x6)
            string gridName = $"{gm.rows}×{gm.cols}";
            if (GUI.Button(new Rect(x, y1, 65f, 24f), gridName, btnStyle))
            {
                CycleGridSize(gm);
            }
            x += 70f;

            // Shuffle Button
            if (GUI.Button(new Rect(x, y1, 75f, 24f), "🔀 Shuffle", blueBtnStyle))
            {
                gm.TriggerShuffle();
            }
            x += 80f;

            // Timer badge
            int sec = (int)gm.runElapsedSeconds;
            badgeStyle.normal.background = buttonBgTex;
            badgeStyle.normal.textColor = Color.white;
            GUI.Box(new Rect(x, y1 + 2f, 65f, 20f), $"⏱ {sec / 60:D2}:{sec % 60:D2}", badgeStyle);
            x += 70f;

            // Right-aligned elements: Leaderboard Toggle, Coins
            float rightX = r.xMax - 10f;
            rightX -= 70f;
            badgeStyle.normal.background = amberBadgeTex;
            badgeStyle.normal.textColor = new Color(0.98f, 0.75f, 0.14f);
            GUI.Box(new Rect(rightX, y1 + 2f, 70f, 20f), $"🪙 {gm.coins}", badgeStyle);

            rightX -= 80f;
            string lbToggleText = gm.rightPanelExpanded && gm.rightTab == RightPanelTab.Leaderboard ? "🏆 Hide LB" : $"🏆 #{gm.topHeaderRank}";
            if (GUI.Button(new Rect(rightX, y1, 75f, 24f), lbToggleText, gm.rightPanelExpanded && gm.rightTab == RightPanelTab.Leaderboard ? purpleBtnStyle : btnStyle))
            {
                if (gm.rightPanelExpanded && gm.rightTab == RightPanelTab.Leaderboard)
                {
                    gm.rightPanelExpanded = false;
                }
                else
                {
                    gm.rightPanelExpanded = true;
                    gm.rightTab = RightPanelTab.Leaderboard;
                }
            }

            // Row 2: Powerups (Hint, Magnet, Preview, Mystery Cards, Reset)
            float y2 = r.y + 36f;
            float pwBtnW = (r.width - 20f - 24f) / 5f;
            float px = r.x + 10f;

            if (GUI.Button(new Rect(px, y2, pwBtnW, 24f), $"💡 Hint ({gm.hintsLeft})", btnStyle))
            {
                gm.TriggerHint();
            }
            px += pwBtnW + 6f;

            if (GUI.Button(new Rect(px, y2, pwBtnW, 24f), $"🧲 Magnet ({gm.magnetsLeft})", btnStyle))
            {
                gm.TriggerMagnet();
            }
            px += pwBtnW + 6f;

            if (GUI.Button(new Rect(px, y2, pwBtnW, 24f), gm.showPreview ? "👁 Hide View" : "👁 Preview", gm.showPreview ? blueBtnStyle : btnStyle))
            {
                gm.showPreview = !gm.showPreview;
            }
            px += pwBtnW + 6f;

            if (GUI.Button(new Rect(px, y2, pwBtnW, 24f), gm.mysteryMode ? "🂠 Mystery: ON" : "🂠 Mystery: OFF", gm.mysteryMode ? purpleBtnStyle : btnStyle))
            {
                gm.mysteryMode = !gm.mysteryMode;
                gm.ShowToast(gm.mysteryMode ? "🂠 Mystery Cards ON: Face-down cards flip when neighbors connect!" : "🂠 Mystery Cards OFF: All cards face-up.");
                gm.InitPuzzle(false);
            }
            px += pwBtnW + 6f;

            if (GUI.Button(new Rect(px, y2, pwBtnW, 24f), "🔄 Reset", btnStyle))
            {
                gm.hintsLeft = 3;
                gm.magnetsLeft = 3;
                gm.InitPuzzle(false);
            }
        }

        private void CycleGridSize(JigsawSolitaireGameManager gm)
        {
            if (gm.rows == 3 && gm.cols == 3) { gm.rows = 4; gm.cols = 3; }
            else if (gm.rows == 4 && gm.cols == 3) { gm.rows = 4; gm.cols = 4; }
            else if (gm.rows == 4 && gm.cols == 4) { gm.rows = 5; gm.cols = 4; }
            else if (gm.rows == 5 && gm.cols == 4) { gm.rows = 5; gm.cols = 5; }
            else if (gm.rows == 5 && gm.cols == 5) { gm.rows = 6; gm.cols = 5; }
            else if (gm.rows == 6 && gm.cols == 5) { gm.rows = 6; gm.cols = 6; }
            else { gm.rows = 3; gm.cols = 3; }

            gm.hintsLeft = 3;
            gm.magnetsLeft = 3;
            gm.InitPuzzle(false);
        }

        private void DrawBoard(Rect r, JigsawSolitaireGameManager gm)
        {
            // Dark board canvas
            GUI.DrawTexture(r, darkBgTex);
            GUI.DrawTexture(r, borderTex, ScaleMode.StretchToFill, true, 0, new Color(0.2f, 0.25f, 0.3f), 2, 0);

            // Preview Ghost Image
            if (gm.showPreview && gm.fullBoardTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(r, gm.fullBoardTexture, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }

            float tileW = r.width / gm.cols;
            float tileH = r.height / gm.rows;

            // Drop Footprint Highlight if dragging
            if (gm.draggingLead != null)
            {
                foreach (var c in gm.draggingCluster)
                {
                    int relR = c.boardRow - gm.draggingLead.boardRow;
                    int relC = c.boardCol - gm.draggingLead.boardCol;
                    int tr = gm.targetLeadRow + relR;
                    int tc = gm.targetLeadCol + relC;
                    if (tr >= 0 && tr < gm.rows && tc >= 0 && tc < gm.cols)
                    {
                        Rect hlRect = new Rect(r.x + tc * tileW + 1f, r.y + tr * tileH + 1f, tileW - 2f, tileH - 2f);
                        GUI.DrawTexture(hlRect, dropHighlightTex);
                    }
                }
            }

            Event evt = Event.current;
            var draggingIds = new HashSet<string>();
            if (gm.draggingLead != null)
            {
                foreach (var c in gm.draggingCluster) draggingIds.Add(c.id);
            }

            // Draw Cards on Board
            foreach (var card in gm.cards)
            {
                // Check if card is currently flying
                if (gm.activeFlights.Exists(f => f.card == card)) continue;

                // Check same-group neighbor connections
                bool connTop = IsConnected(card, -1, 0, gm);
                bool connRight = IsConnected(card, 0, 1, gm);
                bool connBottom = IsConnected(card, 1, 0, gm);
                bool connLeft = IsConnected(card, 0, -1, gm);

                float topInset = connTop ? 0f : JigsawSolitaireGameManager.CARD_GAP;
                float bottomInset = connBottom ? 0f : JigsawSolitaireGameManager.CARD_GAP;
                float leftInset = connLeft ? 0f : JigsawSolitaireGameManager.CARD_GAP;
                float rightInset = connRight ? 0f : JigsawSolitaireGameManager.CARD_GAP;

                float cx = r.x + card.boardCol * tileW + leftInset;
                float cy = r.y + card.boardRow * tileH + topInset;
                float cw = tileW - leftInset - rightInset;
                float ch = tileH - topInset - bottomInset;
                Rect cardRect = new Rect(cx, cy, cw, ch);

                bool isBeingDragged = draggingIds.Contains(card.id);

                if (isBeingDragged)
                {
                    // Draw faint placeholder slot
                    GUI.color = new Color(1f, 1f, 1f, 0.22f);
                    if (card.faceDown) GUI.DrawTexture(cardRect, gm.cardBackTexture);
                    else if (card.cardTexture != null) GUI.DrawTexture(cardRect, card.cardTexture);
                    GUI.color = Color.white;
                }
                else
                {
                    // Draw Card Face
                    if (card.faceDown)
                    {
                        GUI.DrawTexture(cardRect, gm.cardBackTexture);
                    }
                    else if (card.cardTexture != null)
                    {
                        GUI.DrawTexture(cardRect, card.cardTexture);
                    }

                    // Draw Thin Card Borders on Unconnected Edges
                    if (!card.faceDown)
                    {
                        Color bCol = new Color(1f, 1f, 1f, 0.35f);
                        if (!connTop) GUI.DrawTexture(new Rect(cx, cy, cw, 1f), borderTex, ScaleMode.StretchToFill, true, 0, bCol, 1, 0);
                        if (!connBottom) GUI.DrawTexture(new Rect(cx, cy + ch - 1f, cw, 1f), borderTex, ScaleMode.StretchToFill, true, 0, bCol, 1, 0);
                        if (!connLeft) GUI.DrawTexture(new Rect(cx, cy, 1f, ch), borderTex, ScaleMode.StretchToFill, true, 0, bCol, 1, 0);
                        if (!connRight) GUI.DrawTexture(new Rect(cx + cw - 1f, cy, 1f, ch), borderTex, ScaleMode.StretchToFill, true, 0, bCol, 1, 0);
                    }

                    // Mouse Interaction: Start Dragging
                    if (evt.type == EventType.MouseDown && evt.button == 0 && cardRect.Contains(evt.mousePosition))
                    {
                        StartCardDrag(card, evt.mousePosition, r, gm);
                        evt.Use();
                    }
                }
            }

            // Mouse Interaction: Drag update & Drop
            if (gm.draggingLead != null)
            {
                if (evt.type == EventType.MouseDrag)
                {
                    UpdateCardDrag(evt.mousePosition, r, gm);
                    evt.Use();
                }
                else if (evt.type == EventType.MouseUp && evt.button == 0)
                {
                    EndCardDrag(gm);
                    evt.Use();
                }
            }
        }

        private bool IsConnected(SolitaireCard card, int dr, int dc, JigsawSolitaireGameManager gm)
        {
            if (card.faceDown) return false;
            var nb = gm.GetCardAtSlot(card.boardRow + dr, card.boardCol + dc);
            return nb != null && !nb.faceDown && nb.groupId == card.groupId;
        }

        private void StartCardDrag(SolitaireCard lead, Vector2 mousePos, Rect boardRect, JigsawSolitaireGameManager gm)
        {
            if (gm.isAnimatingPowerup || gm.isPuzzleComplete) return;

            gm.draggingLead = lead;
            gm.draggingCluster = lead.faceDown ? new List<SolitaireCard> { lead } : gm.cards.FindAll(c => !c.faceDown && c.groupId == lead.groupId);

            float tileW = boardRect.width / gm.cols;
            float tileH = boardRect.height / gm.rows;
            float leadCenterX = boardRect.x + (lead.boardCol + 0.5f) * tileW;
            float leadCenterY = boardRect.y + (lead.boardRow + 0.5f) * tileH;

            gm.dragScreenOffset = mousePos - new Vector2(leadCenterX, leadCenterY);
            gm.targetLeadRow = lead.boardRow;
            gm.targetLeadCol = lead.boardCol;
        }

        private void UpdateCardDrag(Vector2 mousePos, Rect boardRect, JigsawSolitaireGameManager gm)
        {
            float tileW = boardRect.width / gm.cols;
            float tileH = boardRect.height / gm.rows;
            Vector2 leadPos = mousePos - gm.dragScreenOffset;

            int rawCol = Mathf.RoundToInt((leadPos.x - boardRect.x - tileW * 0.5f) / tileW);
            int rawRow = Mathf.RoundToInt((leadPos.y - boardRect.y - tileH * 0.5f) / tileH);

            int minRelR = 0, maxRelR = 0, minRelC = 0, maxRelC = 0;
            foreach (var c in gm.draggingCluster)
            {
                int rr = c.boardRow - gm.draggingLead.boardRow;
                int rc = c.boardCol - gm.draggingLead.boardCol;
                if (rr < minRelR) minRelR = rr;
                if (rr > maxRelR) maxRelR = rr;
                if (rc < minRelC) minRelC = rc;
                if (rc > maxRelC) maxRelC = rc;
            }

            gm.targetLeadRow = Mathf.Clamp(rawRow, -minRelR, gm.rows - 1 - maxRelR);
            gm.targetLeadCol = Mathf.Clamp(rawCol, -minRelC, gm.cols - 1 - maxRelC);
        }

        private void EndCardDrag(JigsawSolitaireGameManager gm)
        {
            var lead = gm.draggingLead;
            int tr = gm.targetLeadRow;
            int tc = gm.targetLeadCol;

            gm.draggingLead = null;
            gm.ExecuteClusterDrop(lead, tr, tc);
        }

        private void DrawDraggingCluster(Rect boardRect, JigsawSolitaireGameManager gm)
        {
            if (gm.draggingLead == null) return;

            float tileW = boardRect.width / gm.cols;
            float tileH = boardRect.height / gm.rows;
            Vector2 mousePos = Event.current.mousePosition;
            Vector2 leadCenter = mousePos - gm.dragScreenOffset;

            foreach (var c in gm.draggingCluster)
            {
                int relR = c.boardRow - gm.draggingLead.boardRow;
                int relC = c.boardCol - gm.draggingLead.boardCol;

                float cx = leadCenter.x - tileW * 0.5f + relC * tileW;
                float cy = leadCenter.y - tileH * 0.5f + relR * tileH;
                Rect r = new Rect(cx, cy, tileW, tileH);

                // Shadow
                GUI.DrawTexture(new Rect(cx + 4f, cy + 8f, tileW, tileH), shadowTex);

                if (c.faceDown)
                {
                    GUI.DrawTexture(r, gm.cardBackTexture);
                }
                else if (c.cardTexture != null)
                {
                    GUI.DrawTexture(r, c.cardTexture);
                    GUI.DrawTexture(r, borderTex, ScaleMode.StretchToFill, true, 0, new Color(0.35f, 0.85f, 0.98f, 0.85f), 1.5f, 0);
                }
            }
        }

        private void DrawFlightAnimations(Rect boardRect, JigsawSolitaireGameManager gm)
        {
            float tileW = boardRect.width / gm.cols;
            float tileH = boardRect.height / gm.rows;

            foreach (var fl in gm.activeFlights)
            {
                float t = Mathf.SmoothStep(0f, 1f, fl.progress);
                float curR = Mathf.Lerp(fl.fromRow, fl.toRow, t);
                float curC = Mathf.Lerp(fl.fromCol, fl.toCol, t);

                float cx = boardRect.x + curC * tileW + JigsawSolitaireGameManager.CARD_GAP;
                float cy = boardRect.y + curR * tileH + JigsawSolitaireGameManager.CARD_GAP;
                float cw = tileW - JigsawSolitaireGameManager.CARD_GAP * 2f;
                float ch = tileH - JigsawSolitaireGameManager.CARD_GAP * 2f;
                Rect r = new Rect(cx, cy, cw, ch);

                GUI.DrawTexture(new Rect(cx + 3f, cy + 5f, cw, ch), shadowTex);
                if (fl.card.faceDown) GUI.DrawTexture(r, gm.cardBackTexture);
                else if (fl.card.cardTexture != null) GUI.DrawTexture(r, fl.card.cardTexture);
            }
        }

        private void DrawVisualEffects(Rect boardRect, JigsawSolitaireGameManager gm)
        {
            foreach (var fx in gm.activeEffects)
            {
                if (fx.alpha <= 0f) continue;
                Vector2 screenPos = new Vector2(boardRect.x + fx.pos.x, boardRect.y + fx.pos.y);

                if (fx.type == VisualEffectItem.EffectType.Ring)
                {
                    GUI.color = new Color(fx.color.r, fx.color.g, fx.color.b, fx.alpha * fx.color.a);
                    Rect ringRect = new Rect(screenPos.x - fx.radius, screenPos.y - fx.radius, fx.radius * 2f, fx.radius * 2f);
                    GUI.DrawTexture(ringRect, borderTex, ScaleMode.StretchToFill, true, 0, fx.color, 2, 0);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(fx.color.r, fx.color.g, fx.color.b, fx.alpha * fx.color.a);
                    Rect spRect = new Rect(screenPos.x - fx.size * 0.5f, screenPos.y - fx.size * 0.5f, fx.size, fx.size);
                    GUI.DrawTexture(spRect, borderTex);
                    GUI.color = Color.white;
                }
            }
        }

        // --- RIGHT PANEL: OFFLINE LEADERBOARD & SERVER CATALOG ---
        private void DrawRightPanel(Rect r, JigsawSolitaireGameManager gm)
        {
            GUI.DrawTexture(r, cardBgTex);
            GUI.DrawTexture(r, borderTex, ScaleMode.StretchToFill, true, 0, Color.clear, 1, 0);

            // Tab bar
            float tabH = 30f;
            float tabW = (r.width - 16f) / 2f;
            if (GUI.Button(new Rect(r.x + 8f, r.y + 6f, tabW, tabH), "🏆 Leaderboard", gm.rightTab == RightPanelTab.Leaderboard ? blueBtnStyle : btnStyle))
            {
                gm.rightTab = RightPanelTab.Leaderboard;
            }
            if (GUI.Button(new Rect(r.x + 8f + tabW, r.y + 6f, tabW, tabH), "🌐 Server Images", gm.rightTab == RightPanelTab.PuzzlesCatalog ? blueBtnStyle : btnStyle))
            {
                gm.rightTab = RightPanelTab.PuzzlesCatalog;
            }

            Rect contentRect = new Rect(r.x + 8f, r.y + tabH + 12f, r.width - 16f, r.height - tabH - 18f);

            if (gm.rightTab == RightPanelTab.Leaderboard)
            {
                DrawLeaderboardContent(contentRect, gm);
            }
            else
            {
                DrawCatalogContent(contentRect, gm);
            }
        }

        private void DrawLeaderboardContent(Rect r, JigsawSolitaireGameManager gm)
        {
            var cv = gm.lbCurrentView;

            // Header summary: Season info & Mode selector
            float y = r.y;
            string modeName = gm.lbActiveMode == LeaderboardMode.SolitaireLeague ? "Solitaire Speed" : "Unified League";
            if (GUI.Button(new Rect(r.x, y, r.width, 22f), $"Mode: {modeName} (Click to switch)", btnStyle))
            {
                gm.lbActiveMode = gm.lbActiveMode == LeaderboardMode.SolitaireLeague ? LeaderboardMode.UnifiedLeague : LeaderboardMode.SolitaireLeague;
                gm.LbRefreshCurrentView();
            }
            y += 26f;

            // Rank & Score badge
            badgeStyle.normal.background = buttonBgTex;
            badgeStyle.normal.textColor = Color.white;
            GUI.Box(new Rect(r.x, y, r.width * 0.48f, 22f), $"Rank: #{cv.playerRank}", badgeStyle);
            GUI.Box(new Rect(r.x + r.width * 0.52f, y, r.width * 0.48f, 22f), $"{cv.scoreLabel} {cv.playerFormattedScore}", badgeStyle);
            y += 26f;

            // Season ends countdown + +4h advance button
            GUI.Label(new Rect(r.x, y, r.width * 0.6f, 20f), $"⏳ Ends: {cv.countdownText}", labelStyle);
            if (GUI.Button(new Rect(r.x + r.width * 0.65f, y, r.width * 0.35f, 20f), "⏩ +4h Sim", btnStyle))
            {
                gm.LbPlus4h();
            }
            y += 24f;

            // Scrollable Leaderboard List
            float scrollH = r.height - (y - r.y) - 34f;
            Rect viewRect = new Rect(r.x, y, r.width, scrollH);
            Rect totalContentRect = new Rect(0, 0, r.width - 16f, cv.windowedRows.Count * 28f);

            lbScroll = GUI.BeginScrollView(viewRect, lbScroll, totalContentRect);
            float rowY = 0f;
            foreach (var row in cv.windowedRows)
            {
                if (row.isDivider)
                {
                    GUI.Label(new Rect(0, rowY, totalContentRect.width, 24f), $"--- {row.skippedCount} players ---", labelStyle);
                }
                else
                {
                    Rect rowRect = new Rect(0, rowY, totalContentRect.width, 26f);
                    if (row.isPlayer)
                    {
                        GUI.DrawTexture(rowRect, blueBtnTex);
                    }
                    else if (row.rank <= 3)
                    {
                        GUI.DrawTexture(rowRect, amberBadgeTex);
                    }

                    string rStr = row.rank <= 3 ? (row.rank == 1 ? "🥇" : (row.rank == 2 ? "🥈" : "🥉")) : $"#{row.rank}";
                    GUI.Label(new Rect(4f, rowY, 32f, 26f), rStr, row.rank <= 3 ? amberLabelStyle : labelStyle);
                    GUI.Label(new Rect(36f, rowY, totalContentRect.width - 110f, 26f), row.name, row.isPlayer ? emeraldLabelStyle : rowNameStyle);
                    GUI.Label(new Rect(totalContentRect.width - 70f, rowY, 66f, 26f), row.formattedScore, rightAlignLabelStyle);
                }
                rowY += 28f;
            }
            GUI.EndScrollView();

            // Reward claim / Reset button
            float botY = r.yMax - 28f;
            if (cv.isExpired && !cv.rewardClaimed)
            {
                if (GUI.Button(new Rect(r.x, botY, r.width, 26f), $"🎉 Claim {cv.rewardTierCoins}c Reward", blueBtnStyle))
                {
                    gm.LbClaimReward();
                }
            }
            else
            {
                if (GUI.Button(new Rect(r.x, botY, r.width, 26f), "🔄 Reset / Next Season", btnStyle))
                {
                    gm.LbResetSeason();
                }
            }
        }

        private void DrawCatalogContent(Rect r, JigsawSolitaireGameManager gm)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 20f), $"🌐 Published Images ({gm.serverPuzzles.Count})", headerStyle);
            float y = r.y + 24f;

            if (GUI.Button(new Rect(r.x, y, r.width, 22f), "🔄 Refresh Catalog from Server", btnStyle))
            {
                gm.StartCoroutine(RefreshCatalogCoroutine(gm));
            }
            y += 26f;

            // Demo Procedural Scenes Buttons
            GUI.Label(new Rect(r.x, y, r.width, 18f), "Built-in Procedural Scenes:", labelStyle);
            y += 20f;
            float bw = (r.width - 6f) / 2f;
            if (GUI.Button(new Rect(r.x, y, bw, 22f), "Alpine Sunset", btnStyle))
            {
                gm.activeServerPuzzleIndex = -1;
                gm.currentScene = SolitaireScene.Sunset;
                gm.InitPuzzle(false);
            }
            if (GUI.Button(new Rect(r.x + bw + 6f, y, bw, 22f), "Flamingo Lagoon", btnStyle))
            {
                gm.activeServerPuzzleIndex = -1;
                gm.currentScene = SolitaireScene.Flamingo;
                gm.InitPuzzle(false);
            }
            y += 24f;
            if (GUI.Button(new Rect(r.x, y, bw, 22f), "Misty Pines", btnStyle))
            {
                gm.activeServerPuzzleIndex = -1;
                gm.currentScene = SolitaireScene.Forest;
                gm.InitPuzzle(false);
            }
            if (GUI.Button(new Rect(r.x + bw + 6f, y, bw, 22f), "Calm Cove", btnStyle))
            {
                gm.activeServerPuzzleIndex = -1;
                gm.currentScene = SolitaireScene.Coast;
                gm.InitPuzzle(false);
            }
            y += 28f;

            // Server Puzzles List
            GUI.Label(new Rect(r.x, y, r.width, 18f), "Server Published Images:", labelStyle);
            y += 20f;

            float scrollH = r.height - (y - r.y);
            Rect viewRect = new Rect(r.x, y, r.width, scrollH);
            Rect totalContentRect = new Rect(0, 0, r.width - 16f, Mathf.Max(1f, gm.serverPuzzles.Count * 32f));

            catalogScroll = GUI.BeginScrollView(viewRect, catalogScroll, totalContentRect);
            float rowY = 0f;
            for (int i = 0; i < gm.serverPuzzles.Count; i++)
            {
                var item = gm.serverPuzzles[i];
                bool isSel = (gm.activeServerPuzzleIndex == i);
                Rect rowRect = new Rect(0, rowY, totalContentRect.width, 28f);

                string title = string.IsNullOrEmpty(item.title) ? item.id : item.title;
                if (title.Length > 20) title = title.Substring(0, 19) + "…";

                if (GUI.Button(rowRect, $"🖼️ {title}", isSel ? blueBtnStyle : btnStyle))
                {
                    gm.LoadServerPuzzle(i);
                }
                rowY += 32f;
            }
            GUI.EndScrollView();
        }

        private System.Collections.IEnumerator RefreshCatalogCoroutine(JigsawSolitaireGameManager gm)
        {
            gm.ShowToast("Fetching server catalog...");
            var bridge = JigsawSolitaireServerBridge.EnsureInitialized();
            yield return bridge.GetPublishedManifestCoroutine(
                JigsawSolitaireServerBridge.DefaultServerUrl,
                onSuccess: list =>
                {
                    gm.serverPuzzles = list;
                    gm.ShowToast($"Loaded {list.Count} server puzzles!");
                },
                onError: err =>
                {
                    gm.ShowToast($"Server catalog unreachable: {err}", true);
                }
            );
        }

        private void DrawWinOverlay(Rect r, JigsawSolitaireGameManager gm)
        {
            GUI.DrawTexture(r, new Texture2D(1, 1) { wrapMode = TextureWrapMode.Repeat });
            GUI.DrawTexture(r, shadowTex);

            float cardW = 340f;
            float cardH = 220f;
            Rect box = new Rect((r.width - cardW) * 0.5f, (r.height - cardH) * 0.5f, cardW, cardH);
            GUI.DrawTexture(box, cardBgTex);
            GUI.DrawTexture(box, borderTex, ScaleMode.StretchToFill, true, 0, Color.cyan, 2, 0);

            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 30f), "🎉 Picture Complete!", winTitleStyle);
            GUI.Label(new Rect(box.x + 14f, box.y + 50f, box.width - 28f, 36f), gm.winSummaryText, labelStyle);

            badgeStyle.normal.background = amberBadgeTex;
            badgeStyle.normal.textColor = new Color(0.98f, 0.75f, 0.14f);
            GUI.Box(new Rect(box.x + 20f, box.y + 94f, box.width - 40f, 26f), $"🏆 {gm.winRankBadgeText}", badgeStyle);

            float bw = (box.width - 38f) / 2f;
            if (GUI.Button(new Rect(box.x + 14f, box.y + 140f, bw, 32f), "🏆 Leaderboard", btnStyle))
            {
                gm.rightPanelExpanded = true;
                gm.rightTab = RightPanelTab.Leaderboard;
            }

            if (GUI.Button(new Rect(box.x + 20f + bw, box.y + 140f, bw, 32f), "Play Again", blueBtnStyle))
            {
                gm.InitPuzzle(false);
            }
        }
    }
}

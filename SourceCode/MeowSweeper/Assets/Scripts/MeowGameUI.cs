using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowsweeper.Core
{
    public class MeowGameUI : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle shuffleButtonStyle;
        private GUIStyle campaignButtonStyle;
        private GUIStyle powerButtonStyle;
        private GUIStyle labelStyle;
        private GUIStyle centerLabelStyle;
        private GUIStyle toastStyle;
        private GUIStyle badgeStyle;

        private bool showLeaderboard = false;

        // Procedural GPU Textures (100% font-independent in WebGL)
        private Texture2D[] regionTextures;
        private Texture2D thinBorderTexture;
        private Texture2D thickBorderTexture;
        private Texture2D hoverTexture;
        private Texture2D wrongFlashFillTexture;
        private Texture2D wrongFlashBorderTexture;
        private Texture2D hintBorderTexture;
        private Texture2D toastDefaultBg;
        private Texture2D toastInfoBg;
        private Texture2D toastWinBg;
        private Texture2D toastLoseBg;
        private Texture2D darkPanelBg;
        private Texture2D playerRowBg;

        private Texture2D catTexture;
        private Texture2D crossTexture;
        private Texture2D heartFullTexture;
        private Texture2D heartEmptyTexture;
        private Texture2D mouseIconTexture;

        // Cached board geometry
        private Rect currentBoardRect;
        private float currentStartX;
        private float currentTopY;
        private float currentCellSize;
        private int currentGridSize;

        // Unified Pointer/Touch state in Update()
        private bool uPointerDown = false;
        private int uLastRow = -1;
        private int uLastCol = -1;
        private int uSwipeTarget = 1; // 1 = Add Cross, 0 = Remove Cross

        // Double-tap tracking
        private int lastTapRow = -1;
        private int lastTapCol = -1;
        private float lastTapTime = -10f;
        private const float DOUBLE_TAP_WINDOW = 0.34f;

        private void Start()
        {
            GenerateTextures();
        }

        private Texture2D MakeSolidTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void GenerateTextures()
        {
            regionTextures = new Texture2D[MeowGameManager.RegionColors.Length];
            for (int i = 0; i < MeowGameManager.RegionColors.Length; i++)
            {
                regionTextures[i] = MakeSolidTex(MeowGameManager.RegionColors[i]);
            }

            thinBorderTexture = MakeSolidTex(new Color(0.11f, 0.10f, 0.09f, 0.22f));
            thickBorderTexture = MakeSolidTex(new Color(0.08f, 0.10f, 0.14f, 0.92f));
            hoverTexture = MakeSolidTex(new Color(1f, 1f, 1f, 0.22f));

            wrongFlashFillTexture = MakeSolidTex(new Color(0.99f, 0.65f, 0.65f, 0.88f));
            wrongFlashBorderTexture = MakeSolidTex(new Color(0.94f, 0.27f, 0.27f, 1f));
            hintBorderTexture = MakeSolidTex(new Color(0.96f, 0.62f, 0.04f, 1f));

            toastDefaultBg = MakeSolidTex(new Color(0.09f, 0.11f, 0.14f, 0.98f));
            toastInfoBg = MakeSolidTex(new Color(0.25f, 0.20f, 0.08f, 0.98f));
            toastWinBg = MakeSolidTex(new Color(0.02f, 0.59f, 0.41f, 0.98f));
            toastLoseBg = MakeSolidTex(new Color(0.88f, 0.11f, 0.28f, 0.98f));

            darkPanelBg = MakeSolidTex(new Color(0.07f, 0.09f, 0.12f, 0.96f));
            playerRowBg = MakeSolidTex(new Color(0.96f, 0.62f, 0.04f, 0.22f));

            catTexture = CreateCatTexture(64);
            crossTexture = CreateCrossTexture(64);
            heartFullTexture = CreateHeartTexture(32, new Color(0.94f, 0.27f, 0.27f, 1f));
            heartEmptyTexture = CreateHeartTexture(32, new Color(0.22f, 0.25f, 0.30f, 0.85f));
            mouseIconTexture = CreateMouseTexture(32);
        }

        private Texture2D CreateCrossTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color crossColor = new Color(0.18f, 0.16f, 0.15f, 0.88f);

            float margin = size * 0.22f;
            float thickness = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < margin || x > size - margin || y < margin || y > size - margin)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float d1 = Mathf.Abs(x - y) / 1.4142f;
                    float d2 = Mathf.Abs(x - (size - 1 - y)) / 1.4142f;
                    float d = Mathf.Min(d1, d2);

                    if (d <= thickness)
                    {
                        float alpha = Mathf.Clamp01((thickness - d) * 1.5f);
                        tex.SetPixel(x, y, new Color(crossColor.r, crossColor.g, crossColor.b, crossColor.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private Texture2D CreateCatTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color outlineCol = new Color(0.10f, 0.11f, 0.15f, 1f);
            Color bodyCol = new Color(0.96f, 0.58f, 0.16f, 1f);
            Color innerEarCol = new Color(0.95f, 0.42f, 0.48f, 1f);
            Color eyeCol = new Color(0.10f, 0.11f, 0.15f, 1f);

            Vector2 center = new Vector2(size * 0.5f, size * 0.42f);
            float headRadius = size * 0.29f;
            float outlineRadius = headRadius + size * 0.045f;

            Vector2 lEarA = new Vector2(size * 0.22f, size * 0.48f);
            Vector2 lEarB = new Vector2(size * 0.18f, size * 0.85f);
            Vector2 lEarC = new Vector2(size * 0.43f, size * 0.64f);

            Vector2 rEarA = new Vector2(size * 0.78f, size * 0.48f);
            Vector2 rEarB = new Vector2(size * 0.82f, size * 0.85f);
            Vector2 rEarC = new Vector2(size * 0.57f, size * 0.64f);

            Vector2 lInnerA = new Vector2(size * 0.25f, size * 0.54f);
            Vector2 lInnerB = new Vector2(size * 0.22f, size * 0.77f);
            Vector2 lInnerC = new Vector2(size * 0.38f, size * 0.63f);

            Vector2 rInnerA = new Vector2(size * 0.75f, size * 0.54f);
            Vector2 rInnerB = new Vector2(size * 0.78f, size * 0.77f);
            Vector2 rInnerC = new Vector2(size * 0.62f, size * 0.63f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float distHead = Vector2.Distance(p, center);

                    bool inLeftEar = PointInTriangle(p, lEarA, lEarB, lEarC);
                    bool inRightEar = PointInTriangle(p, rEarA, rEarB, rEarC);
                    bool inHeadFill = distHead <= headRadius;
                    bool inHeadOutline = distHead <= outlineRadius;

                    bool inLeftEarOutline = PointInTriangle(p, lEarA + new Vector2(-3, -2), lEarB + new Vector2(-2, 3), lEarC + new Vector2(2, 2));
                    bool inRightEarOutline = PointInTriangle(p, rEarA + new Vector2(3, -2), rEarB + new Vector2(2, 3), rEarC + new Vector2(-2, 2));

                    Color pixel = Color.clear;
                    if (inHeadOutline || inLeftEarOutline || inRightEarOutline) pixel = outlineCol;
                    if (inHeadFill || inLeftEar || inRightEar) pixel = bodyCol;
                    if (PointInTriangle(p, lInnerA, lInnerB, lInnerC) || PointInTriangle(p, rInnerA, rInnerB, rInnerC)) pixel = innerEarCol;

                    Vector2 leftEye = new Vector2(size * 0.38f, size * 0.46f);
                    Vector2 rightEye = new Vector2(size * 0.62f, size * 0.46f);
                    float eyeR = size * 0.055f;

                    if (Vector2.Distance(p, leftEye) <= eyeR || Vector2.Distance(p, rightEye) <= eyeR) pixel = eyeCol;
                    if (Vector2.Distance(p, leftEye + new Vector2(-1.2f, 1.2f)) <= eyeR * 0.38f ||
                        Vector2.Distance(p, rightEye + new Vector2(-1.2f, 1.2f)) <= eyeR * 0.38f) pixel = Color.white;

                    Vector2 noseA = new Vector2(size * 0.45f, size * 0.37f);
                    Vector2 noseB = new Vector2(size * 0.55f, size * 0.37f);
                    Vector2 noseC = new Vector2(size * 0.50f, size * 0.31f);
                    if (PointInTriangle(p, noseA, noseB, noseC)) pixel = innerEarCol;

                    if (Mathf.Abs(y - size * 0.27f) <= 1.2f && Mathf.Abs(x - size * 0.50f) <= size * 0.08f) pixel = outlineCol;

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            return tex;
        }

        private Texture2D CreateHeartTexture(int size, Color fillCol)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.36f);
                    float ny = (y - size * 0.42f) / (size * 0.36f);
                    float a = nx * nx + ny * ny - 1f;
                    float val = a * a * a - nx * nx * ny * ny * ny;
                    tex.SetPixel(x, y, val <= 0f ? fillCol : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateMouseTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color body = new Color(0.62f, 0.66f, 0.72f, 1f);
            Color earPink = new Color(0.95f, 0.55f, 0.65f, 1f);
            Color dark = new Color(0.12f, 0.14f, 0.18f, 1f);

            Vector2 headC = new Vector2(size * 0.50f, size * 0.40f);
            Vector2 lEarC = new Vector2(size * 0.25f, size * 0.70f);
            Vector2 rEarC = new Vector2(size * 0.75f, size * 0.70f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    Color col = Color.clear;

                    if (Vector2.Distance(p, lEarC) <= size * 0.22f || Vector2.Distance(p, rEarC) <= size * 0.22f) col = body;
                    if (Vector2.Distance(p, lEarC) <= size * 0.13f || Vector2.Distance(p, rEarC) <= size * 0.13f) col = earPink;
                    if (Vector2.Distance(p, headC) <= size * 0.28f) col = body;
                    if (Vector2.Distance(p, new Vector2(size * 0.50f, size * 0.20f)) <= size * 0.06f) col = earPink;
                    if (Vector2.Distance(p, new Vector2(size * 0.38f, size * 0.44f)) <= size * 0.045f ||
                        Vector2.Distance(p, new Vector2(size * 0.62f, size * 0.44f)) <= size * 0.045f) col = dark;

                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private void InitStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            headerStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };

            shuffleButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            shuffleButtonStyle.normal.textColor = new Color(0.38f, 0.78f, 1f);

            campaignButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            campaignButtonStyle.normal.textColor = new Color(0.35f, 0.95f, 0.68f);

            powerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                richText = true
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                richText = true
            };
            labelStyle.normal.textColor = new Color(0.90f, 0.93f, 0.96f);

            centerLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                richText = true
            };
            centerLabelStyle.normal.textColor = Color.white;

            toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };
            toastStyle.normal.textColor = Color.white;

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                richText = true
            };
            badgeStyle.normal.textColor = new Color(0.45f, 0.92f, 0.68f);
        }

        private void Update()
        {
            var gm = MeowGameManager.Instance;
            if (gm == null || gm.currentLevel == null || !gm.isPlaying || gm.isTransitioningLevel || showLeaderboard) return;

            int N = gm.currentLevel.gridSize;
            if (currentCellSize <= 0) return;

            bool pointerDownThisFrame = false;
            bool pointerHeld = false;
            bool pointerUpThisFrame = false;
            bool rightClickThisFrame = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screenPos = t.position;
                if (t.phase == TouchPhase.Began) pointerDownThisFrame = true;
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) pointerHeld = true;
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) pointerUpThisFrame = true;
            }
            else
            {
                screenPos = Input.mousePosition;
                pointerDownThisFrame = Input.GetMouseButtonDown(0);
                pointerHeld = Input.GetMouseButton(0);
                pointerUpThisFrame = Input.GetMouseButtonUp(0);
                rightClickThisFrame = Input.GetMouseButtonDown(1);
            }

            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;
            Vector2 guiPos = new Vector2(guiX, guiY);

            bool isOver = currentBoardRect.Contains(guiPos);
            int c = Mathf.FloorToInt((guiX - currentStartX) / currentCellSize);
            int r = Mathf.FloorToInt((guiY - currentTopY) / currentCellSize);
            bool inBounds = isOver && (r >= 0 && r < N && c >= 0 && c < N);

            if (rightClickThisFrame && inBounds)
            {
                gm.SingleTapCross(r, c);
                return;
            }

            if (pointerDownThisFrame && inBounds)
            {
                float now = Time.unscaledTime;

                if (r == lastTapRow && c == lastTapCol && (now - lastTapTime) <= DOUBLE_TAP_WINDOW)
                {
                    lastTapRow = -1;
                    lastTapCol = -1;
                    lastTapTime = -10f;
                    uPointerDown = false;

                    gm.DoubleTapCat(r, c, false);
                    return;
                }

                lastTapRow = r;
                lastTapCol = c;
                lastTapTime = now;

                uPointerDown = true;
                uLastRow = r;
                uLastCol = c;

                int crossResult = gm.SingleTapCross(r, c);
                uSwipeTarget = crossResult;
            }
            else if (pointerHeld && uPointerDown)
            {
                if (inBounds && (r != uLastRow || c != uLastCol))
                {
                    lastTapRow = -1;
                    lastTapCol = -1;
                    uLastRow = r;
                    uLastCol = c;

                    if (gm.boardState[r, c] != 2)
                    {
                        gm.SetCellCrossState(r, c, uSwipeTarget);
                    }
                }
            }
            else if (pointerUpThisFrame && uPointerDown)
            {
                uPointerDown = false;
            }
        }

        private void OnGUI()
        {
            InitStyles();

            float screenW = Screen.width;
            float screenH = Screen.height;
            var gm = MeowGameManager.Instance;
            if (gm == null) return;

            // =========================================================================
            // SINGLE COMPACT TOP BAR (y = 4..38, height = 34)
            // All navigation & core buttons stay strictly in ONE non-wrapping row!
            // =========================================================================
            Rect topBarRect = new Rect(6f, 4f, screenW - 12f, 34f);
            if (darkPanelBg != null) GUI.DrawTexture(topBarRect, darkPanelBg);
            GUI.Box(topBarRect, "");

            var lvl = gm.currentLevel;
            int curN = lvl != null ? lvl.gridSize : 6;
            var lbSync = OfflineLeaderboardService.SyncAndCalculate("meowsweeper");

            float bx = 12f;
            float by = 8f;
            float bh = 26f;
            float gap = 5f;

            // Cat icon + Brand
            if (catTexture != null)
            {
                GUI.DrawTexture(new Rect(bx, by + 2f, 22f, 22f), catTexture, ScaleMode.ScaleToFit, true);
                bx += 25f;
            }
            GUI.Label(new Rect(bx, by, 108f, bh), "MEOWSWEEPER", headerStyle);
            bx += 110f;

            // Mode badge
            if (lvl != null && screenW >= 620f)
            {
                string modeText = gm.isPlayingCampaignMode
                    ? $"Campaign #{gm.currentLevelIndex + 1} ({lvl.tier})"
                    : (lvl.hasSingleCellColor ? "1-Cell + Chain" : "Linked Chain");
                float badgeW = Mathf.Min(145f, screenW * 0.19f);
                GUI.Label(new Rect(bx, by, badgeW, bh), modeText, badgeStyle);
                bx += badgeW + gap;
            }

            // Right-aligned single-row button strip
            float rightButtonsTotalW = 74f + gap + 92f + gap + 96f + gap + 50f + gap + 78f + gap + 74f;
            float rx = Mathf.Max(bx, screenW - rightButtonsTotalW - 12f);

            if (GUI.Button(new Rect(rx, by, 74f, bh), $"Grid: {curN}x{curN}", buttonStyle))
            {
                gm.CycleGridSize();
            }
            rx += 74f + gap;

            if (GUI.Button(new Rect(rx, by, 92f, bh), "Shuffle (4-12)", shuffleButtonStyle))
            {
                gm.ShuffleGridSize(true);
            }
            rx += 92f + gap;

            int campNum = Mathf.Max(1, gm.currentLevelIndex + 1);
            if (GUI.Button(new Rect(rx, by, 96f, bh), $"Campaign: #{campNum}", campaignButtonStyle))
            {
                gm.PlayNextCampaignLevel();
            }
            rx += 96f + gap;

            if (GUI.Button(new Rect(rx, by, 50f, bh), "Clear", buttonStyle))
            {
                gm.ClearBoard();
            }
            rx += 50f + gap;

            string lbBtnLabel = showLeaderboard ? "Game" : $"Rank #{lbSync.playerRank}";
            if (GUI.Button(new Rect(rx, by, 78f, bh), lbBtnLabel, buttonStyle))
            {
                showLeaderboard = !showLeaderboard;
            }
            rx += 78f + gap;

            GUI.Label(new Rect(rx, by, 74f, bh), $"Coins: {OfflineLeaderboardService.GetWalletCoins()}", badgeStyle);

            // =========================================================================
            // MAIN VIEW AREA (y = 42 .. screenH - 6)
            // Grid occupies the FULL vertical height, with Core Gameplay Controls & HUD
            // placed cleanly to the right side of the Square Grid container!
            // =========================================================================
            if (showLeaderboard)
            {
                RenderLeaderboard(screenW, screenH);
            }
            else
            {
                RenderMainSplitView(screenW, screenH);
            }
        }

        private void RenderMainSplitView(float screenW, float screenH)
        {
            var gm = MeowGameManager.Instance;
            var lvl = gm.currentLevel;
            if (lvl == null) return;

            int N = lvl.gridSize;
            float topY = 44f;
            float bottomPad = 8f;
            float availH = screenH - topY - bottomPad;

            // Reserve side panel width (min 250px, max 340px) beside the full-height square grid
            float sidePanelW = Mathf.Clamp(screenW * 0.34f, 240f, 340f);
            float maxGridW = screenW - sidePanelW - 26f;

            // Square Grid fills 100% of available vertical height!
            float boardSize = Mathf.Max(180f, Mathf.Min(availH, maxGridW));
            float totalContentW = boardSize + 12f + sidePanelW;
            float contentStartX = Mathf.Max(8f, (screenW - totalContentW) / 2f);

            float shakeOffsetX = 0f;
            if (gm.boardShakeTimer > 0f)
            {
                shakeOffsetX = Mathf.Sin(gm.boardShakeTimer * 55f) * 6f;
            }

            float boardStartX = contentStartX + shakeOffsetX;
            float boardTopY = topY + (availH - boardSize) / 2f;
            float cellSize = boardSize / N;

            Rect boardRect = new Rect(boardStartX, boardTopY, boardSize, boardSize);
            currentBoardRect = boardRect;
            currentStartX = boardStartX;
            currentTopY = boardTopY;
            currentCellSize = cellSize;
            currentGridSize = N;

            // --- 1. RENDER FULL-HEIGHT SQUARE PUZZLE GRID ---
            DrawSquareGridBoard(gm, lvl, N, boardStartX, boardTopY, boardSize, cellSize);

            // --- 2. RENDER SIDE GAMEPLAY CONTROLS & HUD PANEL (To the right of Grid) ---
            float sideX = contentStartX + boardSize + 12f;
            Rect sideRect = new Rect(sideX, boardTopY, sidePanelW, boardSize);
            RenderSideGameplayPanel(gm, lvl, N, sideRect);
        }

        private void DrawSquareGridBoard(MeowGameManager gm, MeowLevelData lvl, int N, float startX, float topY, float boardSize, float cellSize)
        {
            Vector2 mousePos = (Event.current != null) ? Event.current.mousePosition : Vector2.zero;
            int mouseCol = Mathf.FloorToInt((mousePos.x - startX) / cellSize);
            int mouseRow = Mathf.FloorToInt((mousePos.y - topY) / cellSize);
            bool isMouseOverBoard = currentBoardRect.Contains(mousePos) && mouseRow >= 0 && mouseRow < N && mouseCol >= 0 && mouseCol < N;

            GUI.Box(new Rect(startX - 3f, topY - 3f, boardSize + 6f, boardSize + 6f), "");

            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    int reg = lvl.GetRegion(r, c);
                    Rect cellRect = new Rect(startX + c * cellSize, topY + r * cellSize, cellSize, cellSize);

                    if (regionTextures != null && regionTextures.Length > 0)
                    {
                        int colorIdx = Mathf.Abs(reg) % regionTextures.Length;
                        GUI.DrawTexture(cellRect, regionTextures[colorIdx]);
                    }

                    if (gm.wrongFlashTimer != null && gm.wrongFlashTimer[r, c] > 0f && wrongFlashFillTexture != null)
                    {
                        GUI.DrawTexture(cellRect, wrongFlashFillTexture);
                    }

                    if (isMouseOverBoard && mouseRow == r && mouseCol == c && hoverTexture != null)
                    {
                        GUI.DrawTexture(cellRect, hoverTexture);
                    }

                    int state = gm.boardState[r, c];
                    if (state == 2 && catTexture != null)
                    {
                        float pad = cellSize * 0.10f;
                        Rect iconRect = new Rect(cellRect.x + pad, cellRect.y + pad, cellRect.width - pad * 2f, cellRect.height - pad * 2f);
                        GUI.DrawTexture(iconRect, catTexture, ScaleMode.ScaleToFit, true);
                    }
                    else if (state == 1 && crossTexture != null)
                    {
                        float pad = cellSize * 0.17f;
                        Rect iconRect = new Rect(cellRect.x + pad, cellRect.y + pad, cellRect.width - pad * 2f, cellRect.height - pad * 2f);
                        GUI.DrawTexture(iconRect, crossTexture, ScaleMode.ScaleToFit, true);
                    }

                    if (gm.hasMouseBadge != null && gm.hasMouseBadge[r, c] && mouseIconTexture != null)
                    {
                        float mSize = Mathf.Clamp(cellSize * 0.28f, 10f, 20f);
                        Rect mRect = new Rect(cellRect.xMax - mSize - 2f, cellRect.y + 2f, mSize, mSize);
                        GUI.DrawTexture(mRect, mouseIconTexture, ScaleMode.ScaleToFit, true);
                    }

                    if (gm.isHighlightedCell != null && gm.isHighlightedCell[r, c] && hintBorderTexture != null)
                    {
                        DrawInsetBorder(cellRect, 2.5f, hintBorderTexture);
                    }

                    if (gm.wrongFlashTimer != null && gm.wrongFlashTimer[r, c] > 0f && wrongFlashBorderTexture != null)
                    {
                        DrawInsetBorder(cellRect, 2.5f, wrongFlashBorderTexture);
                    }

                    bool diffTop = (r == 0) || (lvl.GetRegion(r - 1, c) != reg);
                    bool diffLeft = (c == 0) || (lvl.GetRegion(r, c - 1) != reg);

                    if (diffTop && thickBorderTexture != null)
                        GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, cellRect.width, 2.5f), thickBorderTexture);
                    else if (thinBorderTexture != null)
                        GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), thinBorderTexture);

                    if (diffLeft && thickBorderTexture != null)
                        GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, 2.5f, cellRect.height), thickBorderTexture);
                    else if (thinBorderTexture != null)
                        GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), thinBorderTexture);
                }
            }

            if (thickBorderTexture != null)
            {
                GUI.DrawTexture(new Rect(startX, topY + boardSize - 2.5f, boardSize, 2.5f), thickBorderTexture);
                GUI.DrawTexture(new Rect(startX + boardSize - 2.5f, topY, 2.5f, boardSize), thickBorderTexture);
            }
        }

        private void RenderSideGameplayPanel(MeowGameManager gm, MeowLevelData lvl, int N, Rect sideRect)
        {
            float x = sideRect.x;
            float y = sideRect.y;
            float w = sideRect.width;

            // 1. CORE GAMEPLAY POWER-UPS ROW (Stays in a single row outside the grid container!)
            Rect powersBox = new Rect(x, y, w, 40f);
            if (darkPanelBg != null) GUI.DrawTexture(powersBox, darkPanelBg);
            GUI.Box(powersBox, "");

            float btnGap = 5f;
            float pBtnW = (w - 12f - btnGap * 2f) / 3f;
            float pBtnY = y + 6f;

            GUI.enabled = gm.powerCatReveal > 0 && !gm.isTransitioningLevel;
            if (GUI.Button(new Rect(x + 6f, pBtnY, pBtnW, 28f), $"Reveal [{gm.powerCatReveal}]", powerButtonStyle))
            {
                gm.UseCatReveal();
            }

            GUI.enabled = gm.powerHint > 0 && !gm.isTransitioningLevel;
            if (GUI.Button(new Rect(x + 6f + pBtnW + btnGap, pBtnY, pBtnW, 28f), $"Hint [{gm.powerHint}]", powerButtonStyle))
            {
                gm.UseSmartHint();
            }

            GUI.enabled = gm.powerMouse > 0 && !gm.isTransitioningLevel;
            if (GUI.Button(new Rect(x + 6f + (pBtnW + btnGap) * 2f, pBtnY, pBtnW, 28f), $"Mouse [{gm.powerMouse}]", powerButtonStyle))
            {
                gm.UseMouseHelper();
            }
            GUI.enabled = true;

            y += 46f;

            // 2. CATS, TIMER & 5 LIVES STATUS CARD (Cleanly separated so Time & Lives never overlap)
            Rect statsBox = new Rect(x, y, w, 62f);
            if (darkPanelBg != null) GUI.DrawTexture(statsBox, darkPanelBg);
            GUI.Box(statsBox, "");

            int placedCats = gm.GetPlacedCatCount();
            int elapsedSec = Mathf.Max(0, Mathf.FloorToInt(gm.currentRunTimer));
            int mins = elapsedSec / 60;
            int secs = elapsedSec % 60;

            // Row 1 inside statsBox: Cats Counter (Left) + Run Timer (Right)
            if (catTexture != null)
            {
                GUI.DrawTexture(new Rect(x + 10f, y + 7f, 20f, 20f), catTexture, ScaleMode.ScaleToFit, true);
            }
            GUI.Label(new Rect(x + 34f, y + 6f, 110f, 22f), $"<b>Cats:  {placedCats} / {N}</b>", labelStyle);
            GUI.Label(new Rect(x + w - 105f, y + 6f, 95f, 22f), $"<b>Time: {mins:D2}:{secs:D2}</b>", labelStyle);

            // Row 2 inside statsBox: 5 Hearts Lives Indicator
            GUI.Label(new Rect(x + 10f, y + 34f, 48f, 22f), "<b>Lives:</b>", labelStyle);
            for (int h = 0; h < MeowGameManager.MAX_LIVES; h++)
            {
                Texture2D hTex = (h < gm.remainingHearts) ? heartFullTexture : heartEmptyTexture;
                if (hTex != null)
                {
                    GUI.DrawTexture(new Rect(x + 58f + h * 19f, y + 37f, 16f, 16f), hTex, ScaleMode.ScaleToFit, true);
                }
            }
            GUI.Label(new Rect(x + 158f, y + 34f, 55f, 22f), $"<b>({gm.remainingHearts}/{MeowGameManager.MAX_LIVES})</b>", labelStyle);

            y += 68f;

            // 3. DEDICATED TOAST / FEEDBACK BANNER
            Rect toastRect = new Rect(x, y, w, 42f);
            Texture2D toastBg = toastDefaultBg;
            if (gm.toastType == "win") toastBg = toastWinBg;
            else if (gm.toastType == "lose") toastBg = toastLoseBg;
            else if (gm.toastType == "info") toastBg = toastInfoBg;

            if (toastBg != null) GUI.DrawTexture(toastRect, toastBg);
            GUI.Box(toastRect, "");
            GUI.Label(new Rect(x + 6f, y + 3f, w - 12f, 36f), gm.toastMessage, toastStyle);

            y += 48f;

            // 4. LIVE MINI-LEADERBOARD CARD (Fills remaining height of side column, just like Web Version!)
            float remH = sideRect.yMax - y;
            if (remH >= 90f)
            {
                Rect lbCard = new Rect(x, y, w, remH);
                if (darkPanelBg != null) GUI.DrawTexture(lbCard, darkPanelBg);
                GUI.Box(lbCard, "");

                var sync = OfflineLeaderboardService.SyncAndCalculate(OfflineLeaderboardService.ActiveMode);

                // Mini Header
                GUI.Label(new Rect(x + 8f, y + 5f, w - 95f, 20f), $"<b>Leaderboard ({sync.countdownText})</b>", labelStyle);
                if (GUI.Button(new Rect(x + w - 82f, y + 4f, 76f, 22f), "Expand", buttonStyle))
                {
                    showLeaderboard = true;
                }

                float ry = y + 28f;
                int maxRows = Mathf.FloorToInt((remH - 34f) / 22f);
                int shown = 0;

                foreach (var row in sync.windowedRows)
                {
                    if (shown >= maxRows) break;
                    if (row.isDivider)
                    {
                        GUI.Label(new Rect(x + 8f, ry, w - 16f, 18f), $"   ... {row.skippedCount} rivals ...", labelStyle);
                        ry += 18f;
                        shown++;
                        continue;
                    }

                    Rect rRect = new Rect(x + 4f, ry, w - 8f, 20f);
                    if (row.isPlayer && playerRowBg != null)
                    {
                        GUI.DrawTexture(rRect, playerRowBg);
                    }

                    GUI.Label(new Rect(rRect.x + 4f, rRect.y, 34f, 20f), $"<b>#{row.rank}</b>", labelStyle);
                    GUI.Label(new Rect(rRect.x + 40f, rRect.y, w - 115f, 20f), row.isPlayer ? $"<b>{row.name}</b>" : row.name, labelStyle);
                    GUI.Label(new Rect(rRect.xMax - 68f, rRect.y, 64f, 20f), $"<b>{row.formattedScore}</b>", labelStyle);
                    ry += 21f;
                    shown++;
                }
            }
        }

        private void DrawInsetBorder(Rect r, float thickness, Texture2D tex)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), tex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), tex);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), tex);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), tex);
        }

        private void RenderLeaderboard(float screenW, float screenH)
        {
            var gm = MeowGameManager.Instance;
            float panelW = Mathf.Min(540f, screenW - 24f);
            Rect area = new Rect((screenW - panelW) / 2f, 44f, panelW, screenH - 52f);
            GUILayout.BeginArea(area, GUI.skin.box);

            var sync = OfflineLeaderboardService.SyncAndCalculate(OfflineLeaderboardService.ActiveMode);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(OfflineLeaderboardService.ActiveMode == "meowsweeper" ? "[ Best Time ]" : "Best Time", buttonStyle, GUILayout.Width(130), GUILayout.Height(26)))
            {
                OfflineLeaderboardService.ActiveMode = "meowsweeper";
                sync = OfflineLeaderboardService.SyncAndCalculate("meowsweeper");
            }
            if (GUILayout.Button(OfflineLeaderboardService.ActiveMode == "unified_league" ? "[ Unified League ]" : "Unified League", buttonStyle, GUILayout.Width(140), GUILayout.Height(26)))
            {
                OfflineLeaderboardService.ActiveMode = "unified_league";
                sync = OfflineLeaderboardService.SyncAndCalculate("unified_league");
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>Season: {sync.countdownText}</b>  |  {sync.phaseText}", labelStyle, GUILayout.Height(26));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal(GUI.skin.box);
            string deltaStr = sync.rankDelta > 0 ? $" (+{sync.rankDelta})" : (sync.rankDelta < 0 ? $" ({sync.rankDelta})" : "");
            GUILayout.Label($"<b>Your Rank: #{sync.playerRank}/{sync.totalEntries}{deltaStr}</b>", labelStyle, GUILayout.Width(220));
            GUILayout.FlexibleSpace();
            string scoreTitle = OfflineLeaderboardService.ActiveMode == "unified_league" ? "League Score:" : "Best Time:";
            GUILayout.Label($"<b>{scoreTitle} {sync.playerScoreFormatted}</b>", labelStyle);
            GUILayout.EndHorizontal();

            if (sync.countdownText == "ENDED")
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (!sync.rewardClaimed)
                {
                    GUILayout.Label($"<b>Season Complete! Rank #{sync.playerRank}: {sync.rewardTierName} (+{sync.rewardTierCoins} Coins)</b>", labelStyle);
                    if (GUILayout.Button("Claim Reward", campaignButtonStyle, GUILayout.Width(110), GUILayout.Height(24)))
                    {
                        if (OfflineLeaderboardService.ClaimSeasonReward(OfflineLeaderboardService.ActiveMode, out string msg))
                            gm.ShowToast(msg, "win");
                        else
                            gm.ShowToast(msg, "lose");
                    }
                }
                else
                {
                    GUILayout.Label($"<b>Claimed {sync.rewardTierName} (+{sync.rewardTierCoins} Coins)!</b>", labelStyle);
                    if (GUILayout.Button("Next Season", shuffleButtonStyle, GUILayout.Width(110), GUILayout.Height(24)))
                    {
                        OfflineLeaderboardService.StartNextSeason(OfflineLeaderboardService.ActiveMode);
                        gm.ShowToast("New Season Started!", "win");
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            foreach (var row in sync.windowedRows)
            {
                if (row.isDivider)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"... {row.skippedCount} rivals ...", labelStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    continue;
                }

                Rect rowRect = GUILayoutUtility.GetRect(panelW - 20f, 24f);
                if (row.isPlayer && playerRowBg != null)
                {
                    GUI.DrawTexture(rowRect, playerRowBg);
                }
                GUI.Box(rowRect, "");

                string rankTag = row.rank == 1 ? "#1 [1st]" : (row.rank == 2 ? "#2 [2nd]" : (row.rank == 3 ? "#3 [3rd]" : $"#{row.rank}"));
                GUI.Label(new Rect(rowRect.x + 8f, rowRect.y + 1f, 70f, 22f), $"<b>{rankTag}</b>", labelStyle);
                GUI.Label(new Rect(rowRect.x + 85f, rowRect.y + 1f, 180f, 22f), row.isPlayer ? $"<b>{row.name}</b>" : row.name, labelStyle);
                GUI.Label(new Rect(rowRect.x + 270f, rowRect.y + 1f, 80f, 22f), $"[{row.archetype}]", labelStyle);
                GUI.Label(new Rect(rowRect.xMax - 155f, rowRect.y + 1f, 55f, 22f), row.rewardLabel, labelStyle);
                GUI.Label(new Rect(rowRect.xMax - 95f, rowRect.y + 1f, 85f, 22f), $"<b>{row.formattedScore}</b>", labelStyle);
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+4h Sim", buttonStyle, GUILayout.Width(80), GUILayout.Height(26)))
            {
                OfflineLeaderboardService.AdvanceTimeHours(4f);
                gm.ShowToast("Advanced +4h — Bots simulated in real time!", "info");
            }
            if (GUILayout.Button("-6h Spoof", buttonStyle, GUILayout.Width(85), GUILayout.Height(26)))
            {
                OfflineLeaderboardService.AdvanceTimeHours(-6f);
                gm.ShowToast("Clock Rollback Detected (-6h) — Clamped to High-Water Timestamp!", "lose");
            }
            if (GUILayout.Button("End Season", buttonStyle, GUILayout.Width(95), GUILayout.Height(26)))
            {
                OfflineLeaderboardService.ExpireSeason(OfflineLeaderboardService.ActiveMode);
                gm.ShowToast("Season finished! Claim your rank reward above.", "info");
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close Leaderboard", shuffleButtonStyle, GUILayout.Width(140), GUILayout.Height(26)))
            {
                showLeaderboard = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}

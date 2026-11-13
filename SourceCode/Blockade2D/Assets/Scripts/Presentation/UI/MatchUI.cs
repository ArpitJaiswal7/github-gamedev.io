using System;
using UnityEngine;
using Blockade2D.Core.AI;
using Blockade2D.Core.Model;
using Blockade2D.Core.State;
using Blockade2D.Presentation.Utils;

namespace Blockade2D.Presentation.UI
{
    public enum ActionMode
    {
        MovePawn,
        PlaceWall
    }

    /// <summary>
    /// Renders game HUD, dedicated wall buttons, win screen modal, Main Menu selector, and post-game match replay controls.
    /// Supports both Classic 2-player and Bulls Eye 4-player game modes.
    /// </summary>
    public class MatchUI : MonoBehaviour
    {
        public ActionMode CurrentMode { get; private set; } = ActionMode.MovePawn;
        public WallOrientation CurrentOrientation { get; private set; } = WallOrientation.Horizontal;
        public bool IsAIMode { get; set; } = true;
        public AIDifficulty Difficulty { get; set; } = AIDifficulty.Medium;
        public bool IsAIThinking { get; set; } = false;
        public PlayerId ThinkingBotPlayer { get; set; } = PlayerId.Player2;
        public bool IsDraggingWall { get; set; } = false;
        public bool IsReplayMode { get; private set; } = false;
        public bool IsMenuVisible { get; set; } = true;
        public int BoardSize { get; set; } = 15;
        public int ReplayStep { get; set; } = 0;
        public int ReplayTotalSteps { get; set; } = 0;
        public string CurrentReplayNotation { get; set; } = "";

        public event Action<ActionMode> OnModeChanged;
        public event Action<WallOrientation> OnWallOrientationSelected;
        public event Action<WallOrientation> OnWallDragStarted;
        public event Action<bool> OnAIModeToggled;
        public event Action<AIDifficulty> OnDifficultyChanged;
        public event Action<int> OnBoardSizeChanged;
        public event Action<GameMode> OnModeSelected;
        public event Action OnOpenMenuClicked;
        public event Action OnUndoClicked;
        public event Action OnRestartClicked;
        public event Action OnEnterReplayClicked;
        public event Action OnReplayNextClicked;
        public event Action OnReplayPrevClicked;
        public event Action OnExitReplayClicked;

        private GameState _state;
        private bool _isWinModalVisible = false;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _activeButtonStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _wallButtonStyle;
        private GUIStyle _activeWallButtonStyle;
        private GUIStyle _disabledButtonStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _modalBoxStyle;
        private GUIStyle _modalTitleStyle;
        private GUIStyle _modalBodyStyle;
        private Texture2D _dimTexture;

        private Texture2D _horizWallIcon;
        private Texture2D _vertWallIcon;
        private Texture2D _horizWallIconActive;
        private Texture2D _vertWallIconActive;
        private GUIContent _horizContent;
        private GUIContent _vertContent;
        private GUIContent _horizContentActive;
        private GUIContent _vertContentActive;

        public void Bind(GameState state)
        {
            _state = state;
            _isWinModalVisible = _state != null && _state.IsGameOver;
        }

        public void ShowWinModal()
        {
            _isWinModalVisible = true;
        }

        public void CloseWinModal()
        {
            _isWinModalVisible = false;
        }

        public void SetMode(ActionMode mode)
        {
            if (CurrentMode != mode)
            {
                CurrentMode = mode;
                OnModeChanged?.Invoke(CurrentMode);
            }
        }

        public void SetOrientation(WallOrientation orientation)
        {
            CurrentOrientation = orientation;
        }

        public void SelectOrientation(WallOrientation orientation)
        {
            CurrentOrientation = orientation;
            OnWallOrientationSelected?.Invoke(CurrentOrientation);
        }

        public void SetReplayMode(bool active)
        {
            IsReplayMode = active;
        }

        private float GetUIScale(float screenHeight)
        {
            return Mathf.Clamp(screenHeight / 720f, 1.25f, 2.8f);
        }

        private void InitStyles(float uiScale)
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };

                _subHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.82f, 0.88f, 0.95f) }
                };

                _badgeStyle = new GUIStyle(GUI.skin.box)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.86f, 0.35f) }
                };

                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                _activeButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.cyan }
                };

                _primaryButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.88f, 0.35f) }
                };

                _wallButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 8, 8)
                };

                _activeWallButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 8, 8),
                    normal = { textColor = Color.cyan }
                };

                _disabledButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.55f, 0.55f, 0.55f, 0.5f) }
                };

                _boxStyle = new GUIStyle(GUI.skin.box);

                _modalBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(24, 24, 24, 24)
                };

                _modalTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.88f, 0.25f) }
                };

                _modalBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.92f, 0.96f) }
                };

                _dimTexture = new Texture2D(1, 1);
                _dimTexture.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.07f, 0.88f));
                _dimTexture.Apply();

                _horizWallIcon = SpriteUtils.CreateWallIconTexture(true, 96, new Color(0.95f, 0.78f, 0.28f, 1f));
                _vertWallIcon = SpriteUtils.CreateWallIconTexture(false, 96, new Color(0.95f, 0.78f, 0.28f, 1f));
                _horizWallIconActive = SpriteUtils.CreateWallIconTexture(true, 96, new Color(0.1f, 0.95f, 0.85f, 1f));
                _vertWallIconActive = SpriteUtils.CreateWallIconTexture(false, 96, new Color(0.1f, 0.95f, 0.85f, 1f));

                _horizContent = new GUIContent(_horizWallIcon);
                _vertContent = new GUIContent(_vertWallIcon);
                _horizContentActive = new GUIContent(_horizWallIconActive);
                _vertContentActive = new GUIContent(_vertWallIconActive);
            }

            // Dynamically scale fonts with resolution
            _headerStyle.fontSize = Mathf.RoundToInt(22 * uiScale);
            _subHeaderStyle.fontSize = Mathf.RoundToInt(14 * uiScale);
            _badgeStyle.fontSize = Mathf.RoundToInt(18 * uiScale);
            _buttonStyle.fontSize = Mathf.RoundToInt(16 * uiScale);
            _activeButtonStyle.fontSize = Mathf.RoundToInt(16 * uiScale);
            _primaryButtonStyle.fontSize = Mathf.RoundToInt(18 * uiScale);
            _disabledButtonStyle.fontSize = Mathf.RoundToInt(14 * uiScale);
            _modalTitleStyle.fontSize = Mathf.RoundToInt(28 * uiScale);
            _modalBodyStyle.fontSize = Mathf.RoundToInt(16 * uiScale);
        }

        private void OnGUI()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float uiScale = GetUIScale(screenHeight);

            InitStyles(uiScale);

            // Main Menu Screen (shown when loaded or opened from Menu button)
            if (IsMenuVisible)
            {
                RenderMainMenu(screenWidth, screenHeight, uiScale);
                return;
            }

            if (_state == null) return;

            // Replay Mode UI
            if (IsReplayMode)
            {
                RenderReplayControls(screenWidth, screenHeight, uiScale);
                return;
            }

            // Normal Gameplay HUD
            RenderTopHUD(screenWidth, uiScale);
            RenderBottomControls(screenWidth, screenHeight, uiScale);

            // Win Screen Modal Overlay (Shows on Game Over)
            if (_isWinModalVisible && !IsReplayMode)
            {
                RenderWinModal(screenWidth, screenHeight, uiScale);
            }
        }

        private void RenderMainMenu(float screenWidth, float screenHeight, float uiScale)
        {
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), _dimTexture);

            float modalWidth = Mathf.Min(screenWidth - (24f * uiScale), 520f * uiScale);
            float modalHeight = 490f * uiScale;
            float modalX = (screenWidth - modalWidth) * 0.5f;
            float modalY = (screenHeight - modalHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(modalX, modalY, modalWidth, modalHeight), _modalBoxStyle);
            {
                GUI.contentColor = new Color(1f, 0.88f, 0.25f);
                GUILayout.Label("BLOCKADE 2D", _modalTitleStyle);
                GUI.contentColor = Color.white;
                GUILayout.Label("Select Game Mode", _subHeaderStyle);

                GUILayout.Space(20f * uiScale);

                // Option 1: Classic Vs Computer
                float btnHeight = 64f * uiScale;
                string classicText = $"🤖  VS COMPUTER ({BoardSize}×{BoardSize})\n<size=12>Classic Quoridor across opposing baselines</size>";
                if (GUILayout.Button(classicText, _buttonStyle, GUILayout.Height(btnHeight)))
                {
                    IsMenuVisible = false;
                    OnModeSelected?.Invoke(GameMode.Classic);
                }

                GUILayout.Space(12f * uiScale);

                // Option 2: Bulls Eye 4v4 (4 Players)
                string bullsEyeText = $"🎯  BULLS EYE 4v4 ({BoardSize}×{BoardSize})\n<size=12>4 borders • Race to the center cell ({BoardSize / 2}, {BoardSize / 2})!</size>";
                if (GUILayout.Button(bullsEyeText, _primaryButtonStyle, GUILayout.Height(btnHeight)))
                {
                    IsMenuVisible = false;
                    OnModeSelected?.Invoke(GameMode.BullsEye);
                }

                GUILayout.Space(16f * uiScale);

                // Board Grid Size toggle
                GUILayout.BeginHorizontal();
                GUILayout.Label("Grid Size:", _subHeaderStyle, GUILayout.Width(130f * uiScale), GUILayout.Height(36f * uiScale));
                string sizeText = BoardSize == 15 ? "📐 15×15 (Grand)" : "📐 9×9 (Standard)";
                if (GUILayout.Button(sizeText, _buttonStyle, GUILayout.Height(36f * uiScale)))
                {
                    BoardSize = BoardSize == 15 ? 9 : 15;
                    OnBoardSizeChanged?.Invoke(BoardSize);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8f * uiScale);

                // Bot Difficulty toggle
                GUILayout.BeginHorizontal();
                GUILayout.Label("AI Difficulty:", _subHeaderStyle, GUILayout.Width(130f * uiScale), GUILayout.Height(36f * uiScale));
                string diffText = $"Level: {Difficulty}";
                if (GUILayout.Button(diffText, _buttonStyle, GUILayout.Height(36f * uiScale)))
                {
                    Difficulty = (AIDifficulty)(((int)Difficulty + 1) % 3);
                    OnDifficultyChanged?.Invoke(Difficulty);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void RenderTopHUD(float screenWidth, float uiScale)
        {
            if (_state != null && _state.Mode == GameMode.BullsEye)
            {
                RenderTopHUDBullsEye(screenWidth, uiScale);
                return;
            }

            float topY = 10f * uiScale;
            float topHeight = 78f * uiScale;
            float margin = 12f * uiScale;

            // 1. Top Left Panel: Opponent Remaining Blockers
            float oppWidth = 175f * uiScale;
            float oppX = margin;
            RenderTopLeftOpponent(oppX, topY, oppWidth, topHeight, uiScale);

            // 2. Top Right Panel: Game Settings & Difficulty
            float settingsWidth = 165f * uiScale;
            float settingsX = screenWidth - settingsWidth - margin;
            RenderTopRightSettings(settingsX, topY, settingsWidth, topHeight, uiScale);

            // 3. Top Center Panel: Turn Status
            float leftBoundary = oppX + oppWidth + (10f * uiScale);
            float rightBoundary = settingsX - (10f * uiScale);
            float availableWidth = rightBoundary - leftBoundary;

            if (availableWidth >= 220f * uiScale)
            {
                float bannerWidth = Mathf.Min(availableWidth, 380f * uiScale);
                float bannerX = (screenWidth - bannerWidth) * 0.5f;
                RenderTopCenterBanner(new Rect(bannerX, topY, bannerWidth, topHeight), uiScale);
            }
            else
            {
                float bannerWidth = Mathf.Min(screenWidth - (24f * uiScale), 420f * uiScale);
                float bannerX = (screenWidth - bannerWidth) * 0.5f;
                float bannerY = topY + topHeight + (8f * uiScale);
                float bannerHeight = 70f * uiScale;
                RenderTopCenterBanner(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), uiScale);
            }
        }

        private void RenderTopHUDBullsEye(float screenWidth, float uiScale)
        {
            float topY = 10f * uiScale;
            float topHeight = 78f * uiScale;
            float margin = 12f * uiScale;

            // Top Left: Bot 2 (North Orange)
            float leftWidth = 165f * uiScale;
            float leftX = margin;
            GUILayout.BeginArea(new Rect(leftX, topY, leftWidth, topHeight), _boxStyle);
            {
                GUI.contentColor = new Color(1f, 0.62f, 0.22f);
                GUILayout.Label("🤖 Bot 2 (Orange)", _subHeaderStyle);
                GUI.contentColor = Color.white;
                GUILayout.Box($"🧱 {_state.Player2WallsRemaining} Walls", _badgeStyle, GUILayout.Height(36f * uiScale));
            }
            GUILayout.EndArea();

            // Top Right: Bot 3 & Bot 4 Wall Badges + Menu Button
            float rightWidth = 195f * uiScale;
            float rightX = screenWidth - rightWidth - margin;
            GUILayout.BeginArea(new Rect(rightX, topY, rightWidth, topHeight), _boxStyle);
            {
                GUILayout.BeginHorizontal();
                GUI.contentColor = new Color(0.2f, 0.88f, 0.5f);
                GUILayout.Label($"B3: {_state.Player3WallsRemaining}🧱", _subHeaderStyle);
                GUI.contentColor = new Color(0.88f, 0.35f, 0.9f);
                GUILayout.Label($"B4: {_state.Player4WallsRemaining}🧱", _subHeaderStyle);
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();

                if (GUILayout.Button("🏠 Menu", _buttonStyle, GUILayout.Height(36f * uiScale)))
                {
                    IsMenuVisible = true;
                    OnOpenMenuClicked?.Invoke();
                }
            }
            GUILayout.EndArea();

            // Top Center: Turn Status Banner
            float leftBoundary = leftX + leftWidth + (10f * uiScale);
            float rightBoundary = rightX - (10f * uiScale);
            float availableWidth = rightBoundary - leftBoundary;

            if (availableWidth >= 220f * uiScale)
            {
                float bannerWidth = Mathf.Min(availableWidth, 380f * uiScale);
                float bannerX = (screenWidth - bannerWidth) * 0.5f;
                RenderTopCenterBanner(new Rect(bannerX, topY, bannerWidth, topHeight), uiScale);
            }
            else
            {
                float bannerWidth = Mathf.Min(screenWidth - (24f * uiScale), 420f * uiScale);
                float bannerX = (screenWidth - bannerWidth) * 0.5f;
                float bannerY = topY + topHeight + (8f * uiScale);
                float bannerHeight = 70f * uiScale;
                RenderTopCenterBanner(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), uiScale);
            }
        }

        private void RenderTopLeftOpponent(float x, float y, float width, float height, float uiScale)
        {
            GUILayout.BeginArea(new Rect(x, y, width, height), _boxStyle);
            {
                string oppName = IsAIMode ? "🤖 AI Bot (Orange)" : "Player 2 (Orange)";
                Color oppColor = new Color(1f, 0.62f, 0.22f);

                GUI.contentColor = oppColor;
                GUILayout.Label(oppName, _subHeaderStyle);
                GUI.contentColor = Color.white;

                int oppWalls = _state.Player2WallsRemaining;
                string blockerLabel = $"🧱 {oppWalls} Blockers";
                GUILayout.Box(blockerLabel, _badgeStyle, GUILayout.Height(36f * uiScale));
            }
            GUILayout.EndArea();
        }

        private void RenderTopRightSettings(float x, float y, float width, float height, float uiScale)
        {
            GUILayout.BeginArea(new Rect(x, y, width, height), _boxStyle);
            {
                float btnH = 32f * uiScale;
                string modeText = IsAIMode ? "🤖 vs AI" : "👥 2-Player";
                if (GUILayout.Button(modeText, _buttonStyle, GUILayout.Height(btnH)))
                {
                    IsAIMode = !IsAIMode;
                    OnAIModeToggled?.Invoke(IsAIMode);
                }

                if (IsAIMode)
                {
                    string diffText = $"Level: {Difficulty}";
                    if (GUILayout.Button(diffText, _buttonStyle, GUILayout.Height(btnH)))
                    {
                        Difficulty = (AIDifficulty)(((int)Difficulty + 1) % 3);
                        OnDifficultyChanged?.Invoke(Difficulty);
                    }
                }
            }
            GUILayout.EndArea();
        }

        private void RenderTopCenterBanner(Rect rect, float uiScale)
        {
            GUILayout.BeginArea(rect, _boxStyle);
            {
                if (_state.IsGameOver)
                {
                    GUI.contentColor = Color.yellow;
                    string winTitle = _state.Mode == GameMode.BullsEye 
                        ? $"🎯 {_state.Winner.Value.DisplayName()} WINS! 🎯"
                        : $"🏆 {_state.Winner} WINS! 🏆";
                    GUILayout.Label(winTitle, _headerStyle);
                    GUI.contentColor = Color.white;
                    GUILayout.Label("Click 'Play Again' to restart", _subHeaderStyle);
                }
                else if (IsAIThinking)
                {
                    GUI.contentColor = new Color(1f, 0.6f, 0.2f);
                    string thinkingText = _state.Mode == GameMode.BullsEye
                        ? $"🤖 {ThinkingBotPlayer.DisplayName()} is thinking..."
                        : "🤖 AI is thinking...";
                    GUILayout.Label(thinkingText, _headerStyle);
                    GUI.contentColor = Color.white;
                    GUILayout.Label("Calculating counter-move", _subHeaderStyle);
                }
                else
                {
                    if (_state.Mode == GameMode.BullsEye)
                    {
                        if (_state.ActivePlayer == PlayerId.Player1)
                        {
                            GUI.contentColor = new Color(0.35f, 0.82f, 1f);
                            GUILayout.Label("🎯 Your Turn (Player 1 Blue)", _headerStyle);
                            GUI.contentColor = Color.white;
                            GUILayout.Label($"Race to center Bulls Eye ({_state.CenterCell})!", _subHeaderStyle);
                        }
                        else
                        {
                            GUI.contentColor = _state.ActivePlayer switch
                            {
                                PlayerId.Player2 => new Color(1f, 0.6f, 0.2f),
                                PlayerId.Player3 => new Color(0.2f, 0.88f, 0.5f),
                                PlayerId.Player4 => new Color(0.88f, 0.35f, 0.9f),
                                _ => Color.white
                            };
                            GUILayout.Label($"{_state.ActivePlayer.DisplayName()}'s Turn", _headerStyle);
                            GUI.contentColor = Color.white;
                            GUILayout.Label("Heading for the Bulls Eye!", _subHeaderStyle);
                        }
                    }
                    else
                    {
                        string p1Name = "Player 1 (Blue)";
                        string p2Name = IsAIMode ? "AI Bot (Orange)" : "Player 2 (Orange)";
                        string activePlayerName = _state.ActivePlayer == PlayerId.Player1 ? p1Name : p2Name;
                        Color playerColor = _state.ActivePlayer == PlayerId.Player1 
                            ? new Color(0.35f, 0.82f, 1f) 
                            : new Color(1f, 0.6f, 0.2f);

                        GUI.contentColor = playerColor;
                        GUILayout.Label($"{activePlayerName}'s Turn", _headerStyle);
                        GUI.contentColor = Color.white;
                        GUILayout.Label("Move pawn or place blocker", _subHeaderStyle);
                    }
                }
            }
            GUILayout.EndArea();
        }

        private void RenderBottomControls(float screenWidth, float screenHeight, float uiScale)
        {
            float panelWidth = Mathf.Min(screenWidth - (20f * uiScale), 580f * uiScale);
            float panelX = (screenWidth - panelWidth) * 0.5f;

            int activePlayerWalls = _state.GetWallsRemaining(_state.ActivePlayer);
            bool hasWalls = activePlayerWalls > 0;

            // Wall Tools Panel
            float wallPanelHeight = 84f * uiScale;
            float wallButtonHeight = 68f * uiScale;
            float mainControlHeight = 72f * uiScale;
            float mainButtonHeight = 56f * uiScale;

            float mainControlY = screenHeight - mainControlHeight - (12f * uiScale);
            float wallPanelY = mainControlY - wallPanelHeight - (8f * uiScale);

            GUILayout.BeginArea(new Rect(panelX, wallPanelY, panelWidth, wallPanelHeight), _boxStyle);
            {
                GUILayout.BeginHorizontal();

                string wallCountLabel = $"🧱 Walls: {activePlayerWalls}";
                GUILayout.Box(wallCountLabel, _badgeStyle, GUILayout.Width(panelWidth * 0.38f), GUILayout.Height(wallButtonHeight));

                // Horizontal Wall Button (Hold & Drop)
                bool isHorizActive = IsDraggingWall && CurrentOrientation == WallOrientation.Horizontal;
                var horizStyle = isHorizActive ? _activeWallButtonStyle : _wallButtonStyle;
                var horizIcon = isHorizActive ? _horizContentActive : _horizContent;

                GUI.enabled = hasWalls;
                Rect horizRect = GUILayoutUtility.GetRect(horizIcon, horizStyle, GUILayout.Height(wallButtonHeight));
                if (Event.current.type == EventType.MouseDown && horizRect.Contains(Event.current.mousePosition) && hasWalls)
                {
                    CurrentOrientation = WallOrientation.Horizontal;
                    OnWallDragStarted?.Invoke(WallOrientation.Horizontal);
                    Event.current.Use();
                }
                GUI.Box(horizRect, horizIcon, horizStyle);

                // Vertical Wall Button (Hold & Drop)
                bool isVertActive = IsDraggingWall && CurrentOrientation == WallOrientation.Vertical;
                var vertStyle = isVertActive ? _activeWallButtonStyle : _wallButtonStyle;
                var vertIcon = isVertActive ? _vertContentActive : _vertContent;

                Rect vertRect = GUILayoutUtility.GetRect(vertIcon, vertStyle, GUILayout.Height(wallButtonHeight));
                if (Event.current.type == EventType.MouseDown && vertRect.Contains(Event.current.mousePosition) && hasWalls)
                {
                    CurrentOrientation = WallOrientation.Vertical;
                    OnWallDragStarted?.Invoke(WallOrientation.Vertical);
                    Event.current.Use();
                }
                GUI.Box(vertRect, vertIcon, vertStyle);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            // Main Controls Panel
            GUILayout.BeginArea(new Rect(panelX, mainControlY, panelWidth, mainControlHeight), _boxStyle);
            {
                GUILayout.BeginHorizontal();

                var moveStyle = CurrentMode == ActionMode.MovePawn ? _activeButtonStyle : _buttonStyle;
                if (GUILayout.Button("🏃 Move", moveStyle, GUILayout.Height(mainButtonHeight)))
                {
                    SetMode(ActionMode.MovePawn);
                }

                if (GUILayout.Button("↩ Undo", _buttonStyle, GUILayout.Height(mainButtonHeight)))
                {
                    OnUndoClicked?.Invoke();
                }

                if (GUILayout.Button("🔄 Reset", _buttonStyle, GUILayout.Height(mainButtonHeight)))
                {
                    OnRestartClicked?.Invoke();
                }

                if (GUILayout.Button("🏠 Menu", _buttonStyle, GUILayout.Height(mainButtonHeight)))
                {
                    IsMenuVisible = true;
                    OnOpenMenuClicked?.Invoke();
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void RenderWinModal(float screenWidth, float screenHeight, float uiScale)
        {
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), _dimTexture);

            float modalWidth = Mathf.Min(screenWidth - (30f * uiScale), 520f * uiScale);
            float modalHeight = 310f * uiScale;
            float modalX = (screenWidth - modalWidth) * 0.5f;
            float modalY = (screenHeight - modalHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(modalX, modalY, modalWidth, modalHeight), _modalBoxStyle);
            {
                bool isBullsEye = _state != null && _state.Mode == GameMode.BullsEye;
                string winnerText;
                Color winnerColor;

                if (isBullsEye)
                {
                    winnerText = _state.Winner == PlayerId.Player1 
                        ? "YOU CONQUERED THE BULLS EYE!" 
                        : $"{_state.Winner.Value.DisplayName().ToUpper()} WINS!";
                    winnerColor = _state.Winner == PlayerId.Player1 
                        ? new Color(0.35f, 0.85f, 1f) 
                        : new Color(1f, 0.8f, 0.2f);
                }
                else
                {
                    winnerText = _state.Winner == PlayerId.Player1 ? "PLAYER 1 WINS!" : "PLAYER 2 WINS!";
                    winnerColor = _state.Winner == PlayerId.Player1 ? new Color(0.35f, 0.85f, 1f) : new Color(1f, 0.65f, 0.25f);
                }

                GUI.contentColor = winnerColor;
                GUILayout.Label($"🏆 {winnerText} 🏆", _modalTitleStyle);
                GUI.contentColor = Color.white;

                GUILayout.Space(10f * uiScale);
                string winSubtitle;
                if (isBullsEye)
                {
                    winSubtitle = _state.Winner == PlayerId.Player1
                        ? $"Blue pawn was first to seize the center cell ({_state.CenterCell})!"
                        : $"{_state.Winner.Value.DisplayName()} outmaneuvered all competitors to the center cell!";
                }
                else
                {
                    winSubtitle = _state.Winner == PlayerId.Player1
                        ? "Blue reached the opposing baseline!"
                        : (IsAIMode ? "AI Bot outmaneuvered you to victory!" : "Orange reached the baseline!");
                }
                GUILayout.Label(winSubtitle, _modalBodyStyle);

                GUILayout.Space(22f * uiScale);

                // Action Buttons
                GUILayout.BeginHorizontal();

                float modalBtnHeight = 56f * uiScale;
                if (GUILayout.Button("🔄 Play Again", _primaryButtonStyle, GUILayout.Height(modalBtnHeight)))
                {
                    CloseWinModal();
                    OnRestartClicked?.Invoke();
                }

                if (GUILayout.Button("🏠 Main Menu", _buttonStyle, GUILayout.Height(modalBtnHeight)))
                {
                    CloseWinModal();
                    IsMenuVisible = true;
                    OnOpenMenuClicked?.Invoke();
                }

                if (GUILayout.Button("🎬 Replay", _buttonStyle, GUILayout.Height(modalBtnHeight)))
                {
                    CloseWinModal();
                    OnEnterReplayClicked?.Invoke();
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void RenderReplayControls(float screenWidth, float screenHeight, float uiScale)
        {
            // Top Replay Banner
            float topW = Mathf.Min(screenWidth - (20f * uiScale), 500f * uiScale);
            float topX = (screenWidth - topW) * 0.5f;
            float topH = 82f * uiScale;
            GUILayout.BeginArea(new Rect(topX, 15f * uiScale, topW, topH), _boxStyle);
            {
                GUILayout.Label("🎬 MATCH REPLAY", _headerStyle);
                string stepInfo = ReplayTotalSteps > 0 
                    ? $"Step {ReplayStep} of {ReplayTotalSteps}  •  {CurrentReplayNotation}" 
                    : "No moves recorded.";
                GUILayout.Label(stepInfo, _subHeaderStyle);
            }
            GUILayout.EndArea();

            // Bottom Navigation Panel
            float bottomW = Mathf.Min(screenWidth - (20f * uiScale), 560f * uiScale);
            float bottomX = (screenWidth - bottomW) * 0.5f;
            float bottomH = 74f * uiScale;
            float btnH = 56f * uiScale;
            GUILayout.BeginArea(new Rect(bottomX, screenHeight - bottomH - (15f * uiScale), bottomW, bottomH), _boxStyle);
            {
                GUILayout.BeginHorizontal();

                GUI.enabled = ReplayStep > 0;
                if (GUILayout.Button("◀ Prev", _buttonStyle, GUILayout.Height(btnH)))
                {
                    OnReplayPrevClicked?.Invoke();
                }
                GUI.enabled = true;

                GUI.enabled = ReplayStep < ReplayTotalSteps;
                if (GUILayout.Button("Next ▶", _buttonStyle, GUILayout.Height(btnH)))
                {
                    OnReplayNextClicked?.Invoke();
                }
                GUI.enabled = true;

                if (GUILayout.Button("🔄 Play Again", _primaryButtonStyle, GUILayout.Height(btnH)))
                {
                    CloseWinModal();
                    OnExitReplayClicked?.Invoke();
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
    }
}

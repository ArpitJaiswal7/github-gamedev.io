using System.Collections.Generic;
using UnityEngine;
using Blockade2D.Core.Engine;
using Blockade2D.Core.Model;
using Blockade2D.Core.State;
using Blockade2D.Presentation.UI;
using Blockade2D.Presentation.Views;

namespace Blockade2D.Presentation.Controllers
{
    /// <summary>
    /// Master coordinator connecting Core GameEngine with Presentation Views, UI, and AI Opponents.
    /// Supports both Classic 2-player and Bulls Eye 4-player game modes.
    /// Dynamically fits the board within device screen bounds across all aspect ratios.
    /// </summary>
    public class MatchCoordinator : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private WallPlacementController wallController;
        [SerializeField] private AIController aiController;
        [SerializeField] private MatchUI matchUI;

        [Header("Configuration")]
        [SerializeField] private int boardSize = 15;
        [SerializeField] private int wallsPerPlayer = 10;

        private GameEngine _engine;
        private Camera _mainCamera;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private GameMode _currentMode = GameMode.Classic;

        private void Start()
        {
            EnsureDependencies();
            InitializeGame();
            FitCameraToScreen();
        }

        private void Update()
        {
            // Dynamically adapt camera framing if window or device orientation changes
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                FitCameraToScreen();
            }

            if (matchUI != null && matchUI.IsMenuVisible)
                return;

            // Continuous cursor tracking strictly while holding and dragging
            wallController.UpdateCursorTracking(_engine, boardView, IsHumanInputAllowed());

            // Direct click handling for instant tile tapping
            bool isPointerDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (isPointerDown && IsHumanInputAllowed() && !wallController.IsDragging)
            {
                if (_mainCamera != null && boardView != null)
                {
                    Vector3 pointerScreen = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
                    Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(pointerScreen);
                    if (boardView.TryGetGridPositionFromWorld(mouseWorld, out BoardPosition clickedGrid))
                    {
                        HandleTileClicked(clickedGrid);
                    }
                }
            }

            // Handle Hold-and-Drop Release
            bool isRelease = Input.GetMouseButtonUp(0)
                || (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled))
                || (!Input.GetMouseButton(0) && Input.touchCount == 0 && wallController.IsDragging);

            if (isRelease && wallController.IsDragging)
            {
                if (wallController.HoveredIntersection.HasValue)
                {
                    bool placed = wallController.TryCommitWall(wallController.HoveredIntersection.Value, _engine, boardView);
                    if (!placed)
                    {
                        wallController.CancelPlacement(boardView);
                    }
                }
                else
                {
                    wallController.CancelPlacement(boardView);
                }

                wallController.StopDragging();
                matchUI.IsDraggingWall = false;
                TileView.InteractivityEnabled = true;
                RefreshLegalMoveHighlights();
            }
        }

        private void EnsureDependencies()
        {
            if (boardView == null)
            {
                boardView = FindFirstObjectByType<BoardView>();
                if (boardView == null)
                {
                    var bvObj = new GameObject("BoardView");
                    bvObj.transform.SetParent(transform);
                    boardView = bvObj.AddComponent<BoardView>();
                }
            }

            if (wallController == null)
            {
                wallController = GetComponent<WallPlacementController>();
                if (wallController == null)
                {
                    wallController = FindFirstObjectByType<WallPlacementController>();
                }
                if (wallController == null)
                {
                    wallController = gameObject.AddComponent<WallPlacementController>();
                }
            }

            if (aiController == null)
            {
                aiController = GetComponent<AIController>();
                if (aiController == null)
                {
                    aiController = FindFirstObjectByType<AIController>();
                }
                if (aiController == null)
                {
                    aiController = gameObject.AddComponent<AIController>();
                }
            }

            if (matchUI == null)
            {
                matchUI = GetComponent<MatchUI>();
                if (matchUI == null)
                {
                    matchUI = FindFirstObjectByType<MatchUI>();
                }
                if (matchUI == null)
                {
                    matchUI = gameObject.AddComponent<MatchUI>();
                }
            }

            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var camObj = new GameObject("Main Camera");
                _mainCamera = camObj.AddComponent<Camera>();
                _mainCamera.tag = "MainCamera";
            }

            _mainCamera.orthographic = true;
            _mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f); // Sleek dark backdrop
        }

        private void FitCameraToScreen()
        {
            if (_mainCamera == null || boardView == null)
                return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            float screenW = Mathf.Max(100, Screen.width);
            float screenH = Mathf.Max(100, Screen.height);
            float aspect = screenW / screenH;

            float boardExtent = boardView.BoardExtent;

            // UI Pixel Reservations:
            float uiScale = Mathf.Clamp(screenH / 720f, 1.15f, 2.5f);
            float oppWidth = 175f * uiScale;
            float settingsWidth = 165f * uiScale;
            float availableCenterWidth = screenW - oppWidth - settingsWidth - (44f * uiScale);
            bool isStackedTop = availableCenterWidth < 220f * uiScale;

            float topMarginPx = (isStackedTop ? 165f : 100f) * uiScale;
            float bottomMarginPx = 195f * uiScale;
            float sideMarginPx = 24f * uiScale;

            float safeHeightFraction = Mathf.Clamp01((screenH - topMarginPx - bottomMarginPx) / screenH);
            float safeWidthFraction = Mathf.Clamp01((screenW - (2f * sideMarginPx)) / screenW);

            safeHeightFraction = Mathf.Max(0.4f, safeHeightFraction);
            safeWidthFraction = Mathf.Max(0.4f, safeWidthFraction);

            float worldPadding = 0.5f;
            float requiredWorldSize = boardExtent + worldPadding;

            float orthoFromWidth = requiredWorldSize / (2f * aspect * safeWidthFraction);
            float orthoFromHeight = requiredWorldSize / (2f * safeHeightFraction);

            float targetOrtho = Mathf.Max(orthoFromWidth, orthoFromHeight);
            _mainCamera.orthographicSize = targetOrtho;

            float centerOffsetPx = (bottomMarginPx - topMarginPx) * 0.5f;
            float worldShiftY = (centerOffsetPx / screenH) * (2f * targetOrtho);

            _mainCamera.transform.position = new Vector3(0f, -worldShiftY, -10f);
        }

        private void InitializeGame()
        {
            _engine = new GameEngine(boardSize, wallsPerPlayer, _currentMode);

            // Bind UI
            matchUI.BoardSize = boardSize;
            matchUI.Bind(_engine.State);
            matchUI.IsAIMode = aiController.IsAIEnabled;
            matchUI.Difficulty = aiController.Difficulty;
            matchUI.OnModeChanged += HandleModeChanged;
            matchUI.OnBoardSizeChanged += size =>
            {
                boardSize = size;
                StartMatch(_currentMode);
            };
            matchUI.OnWallOrientationSelected += HandleWallOrientationSelected;
            matchUI.OnWallDragStarted += orientation =>
            {
                TileView.InteractivityEnabled = false;
                wallController.StartDragging(orientation);
                matchUI.IsDraggingWall = true;
                matchUI.SetOrientation(orientation);
                boardView.ClearTileHighlights();
            };
            matchUI.OnUndoClicked += HandleUndo;
            matchUI.OnRestartClicked += HandleRestart;
            matchUI.OnEnterReplayClicked += HandleEnterReplay;
            matchUI.OnReplayNextClicked += HandleReplayNext;
            matchUI.OnReplayPrevClicked += HandleReplayPrev;
            matchUI.OnExitReplayClicked += HandleRestart;
            matchUI.OnModeSelected += HandleModeSelected;
            matchUI.OnOpenMenuClicked += HandleOpenMenuClicked;
            matchUI.OnAIModeToggled += enabled =>
            {
                aiController.IsAIEnabled = enabled;
                if (enabled && IsAIPlayer(_engine.State.ActivePlayer))
                {
                    boardView.ClearTileHighlights();
                    matchUI.ThinkingBotPlayer = _engine.State.ActivePlayer;
                    aiController.QueueTurn(_engine, _engine.State.ActivePlayer);
                }
                else if (!enabled)
                {
                    aiController.CancelPendingTurn();
                    matchUI.IsAIThinking = false;
                    TileView.InteractivityEnabled = true;
                    RefreshLegalMoveHighlights();
                }
            };
            matchUI.OnDifficultyChanged += diff => aiController.Difficulty = diff;

            // Bind AI Controller
            aiController.OnAIBeganThinking += () =>
            {
                TileView.InteractivityEnabled = false;
                matchUI.IsAIThinking = true;
                matchUI.ThinkingBotPlayer = aiController.CurrentBotPlayer;
                boardView.ClearTileHighlights();
            };
            aiController.OnAIFinishedTurn += () =>
            {
                if (!IsAIPlayer(_engine.State.ActivePlayer))
                {
                    TileView.InteractivityEnabled = true;
                    matchUI.IsAIThinking = false;
                    RefreshLegalMoveHighlights();
                }
            };

            // Bind Wall Controller
            wallController.OnOrientationChanged += HandleOrientationChanged;
            wallController.OnWallSuccessfullyPlaced += wall =>
            {
                matchUI.SetMode(ActionMode.MovePawn);
                RefreshLegalMoveHighlights();
            };

            // Bind Board View
            boardView.InitializeBoard(boardSize, _currentMode);
            boardView.OnTileClicked += HandleTileClicked;
            boardView.OnIntersectionHovered += HandleIntersectionHovered;
            boardView.OnIntersectionUnhovered += HandleIntersectionUnhovered;
            boardView.OnIntersectionClicked += HandleIntersectionClicked;
            boardView.OnPawnClicked += HandlePawnClicked;

            // Bind GameEngine Events
            _engine.OnPawnMoved += (player, from, to) => boardView.MovePawn(player, to);
            _engine.OnWallPlaced += (player, wall) => boardView.SpawnWall(wall);
            _engine.OnTurnChanged += HandleTurnChanged;
            _engine.OnGameOver += HandleGameOver;

            // Show Menu by default on startup
            matchUI.IsMenuVisible = true;
            boardView.ClearTileHighlights();
        }

        private void StartMatch(GameMode mode)
        {
            StopAllCoroutines();
            if (aiController != null)
            {
                aiController.CancelPendingTurn();
            }
            wallController.CancelPlacement(boardView);
            _currentMode = mode;

            int walls = GameState.GetDefaultWalls(mode, boardSize);
            _engine.ResetGame(boardSize, walls, mode);

            boardView.InitializeBoard(boardSize, mode);
            boardView.ClearAllWalls();
            SyncBoardToState();

            matchUI.Bind(_engine.State);
            matchUI.CloseWinModal();
            matchUI.SetReplayMode(false);
            matchUI.IsAIThinking = false;
            matchUI.SetMode(ActionMode.MovePawn);

            if (!matchUI.IsMenuVisible)
            {
                RefreshLegalMoveHighlights();
            }
            else
            {
                boardView.ClearTileHighlights();
            }

            FitCameraToScreen();
        }

        private void HandleModeSelected(GameMode mode)
        {
            matchUI.IsMenuVisible = false;
            StartMatch(mode);
            RefreshLegalMoveHighlights();
        }

        private void HandleOpenMenuClicked()
        {
            StopAllCoroutines();
            if (aiController != null)
            {
                aiController.CancelPendingTurn();
            }
            wallController.CancelPlacement(boardView);
            matchUI.IsAIThinking = false;
            matchUI.IsMenuVisible = true;
            boardView.ClearTileHighlights();
        }

        public bool IsAIPlayer(PlayerId player)
        {
            if (aiController == null || !aiController.IsAIEnabled)
                return false;

            if (_currentMode == GameMode.BullsEye)
                return player != PlayerId.Player1;
            else
                return player == PlayerId.Player2;
        }

        private bool IsHumanInputAllowed()
        {
            if (matchUI != null && matchUI.IsMenuVisible)
                return false;

            if (_engine == null || _engine.State.IsGameOver)
                return false;

            if (aiController != null && aiController.IsThinking)
                return false;

            if (IsAIPlayer(_engine.State.ActivePlayer))
                return false;

            return true;
        }

        private void HandleModeChanged(ActionMode newMode)
        {
            if (newMode == ActionMode.MovePawn)
            {
                TileView.InteractivityEnabled = true;
                boardView.GhostWall.Hide();
                RefreshLegalMoveHighlights();
            }
            else
            {
                TileView.InteractivityEnabled = false;
                boardView.ClearTileHighlights();
                if (wallController.HoveredIntersection.HasValue)
                {
                    wallController.OnIntersectionHovered(
                        wallController.HoveredIntersection.Value,
                        _engine,
                        boardView,
                        true);
                }
            }
        }

        private void HandleWallOrientationSelected(WallOrientation orientation)
        {
            wallController.SetOrientation(orientation);
            HandleModeChanged(ActionMode.PlaceWall);
        }

        private void HandleOrientationChanged(WallOrientation newOrientation)
        {
            if (matchUI.CurrentOrientation != newOrientation)
            {
                matchUI.SetOrientation(newOrientation);
            }

            if (matchUI.CurrentMode == ActionMode.PlaceWall && wallController.HoveredIntersection.HasValue)
            {
                wallController.OnIntersectionHovered(
                    wallController.HoveredIntersection.Value,
                    _engine,
                    boardView,
                    true);
            }
        }

        private int _lastTileClickFrame = -1;
        private void HandleTileClicked(BoardPosition position)
        {
            if (!IsHumanInputAllowed() || wallController.IsDragging)
                return;

            if (Time.frameCount == _lastTileClickFrame)
                return;

            var result = _engine.TryMovePawn(_engine.State.ActivePlayer, position);
            if (result.IsSuccess)
            {
                _lastTileClickFrame = Time.frameCount;
                RefreshLegalMoveHighlights();
            }
        }

        private void HandleIntersectionHovered(BoardPosition intersection)
        {
        }

        private void HandleIntersectionUnhovered(BoardPosition intersection)
        {
        }

        private void HandleIntersectionClicked(BoardPosition intersection)
        {
        }

        private void HandlePawnClicked(PlayerId player)
        {
            if (player == _engine.State.ActivePlayer && IsHumanInputAllowed())
            {
                matchUI.SetMode(ActionMode.MovePawn);
            }
        }

        private void HandleTurnChanged(PlayerId newPlayer)
        {
            if (IsAIPlayer(newPlayer))
            {
                boardView.ClearTileHighlights();
                matchUI.ThinkingBotPlayer = newPlayer;
                aiController.QueueTurn(_engine, newPlayer);
            }
            else
            {
                if (aiController != null)
                {
                    aiController.CancelPendingTurn();
                }
                matchUI.IsAIThinking = false;
                TileView.InteractivityEnabled = true;
                RefreshLegalMoveHighlights();
            }
        }

        private void HandleGameOver(PlayerId winner)
        {
            boardView.ClearTileHighlights();
            boardView.GhostWall.Hide();
            matchUI.ShowWinModal();
        }

        private void RefreshLegalMoveHighlights()
        {
            if (!IsHumanInputAllowed() || wallController.IsDragging)
            {
                boardView.ClearTileHighlights();
                return;
            }

            var legalMoves = _engine.GetLegalPawnMoves(_engine.State.ActivePlayer);
            boardView.HighlightTiles(legalMoves);
        }

        private void HandleUndo()
        {
            if (aiController.IsThinking)
                return;

            wallController.CancelPlacement(boardView);

            if (_currentMode == GameMode.BullsEye && aiController.IsAIEnabled)
            {
                // In Bulls Eye mode, unwind all bot turns back to Player 1's previous turn
                int steps = 0;
                while (_engine.History.CurrentIndex >= 0 && (steps == 0 || _engine.State.ActivePlayer != PlayerId.Player1))
                {
                    if (!_engine.Undo())
                        break;
                    steps++;
                    if (_engine.State.ActivePlayer == PlayerId.Player1)
                        break;
                }
            }
            else
            {
                int stepsToUndo = (aiController.IsAIEnabled && _engine.State.ActivePlayer == PlayerId.Player1 && _engine.History.CurrentIndex >= 1) ? 2 : 1;
                for (int i = 0; i < stepsToUndo; i++)
                {
                    _engine.Undo();
                }
            }

            if (!_engine.State.IsGameOver)
            {
                matchUI.CloseWinModal();
            }
            matchUI.Bind(_engine.State);
            SyncBoardToState();
            RefreshLegalMoveHighlights();
        }

        private void HandleRestart()
        {
            StartMatch(_currentMode);
        }

        private void HandleEnterReplay()
        {
            StopAllCoroutines();
            wallController.CancelPlacement(boardView);
            matchUI.CloseWinModal();
            matchUI.SetReplayMode(true);
            matchUI.ReplayTotalSteps = _engine.History.AllRecordedMoves.Count;

            while (_engine.History.CurrentIndex >= 0)
            {
                _engine.Undo();
            }

            SyncBoardToState();
            matchUI.ReplayStep = 0;
            matchUI.CurrentReplayNotation = "Game Start (Step 0)";
        }

        private void HandleReplayNext()
        {
            if (_engine.Redo())
            {
                SyncBoardToState();
                matchUI.ReplayStep = _engine.History.CurrentIndex + 1;
                var currentCommand = _engine.History.AllRecordedMoves[_engine.History.CurrentIndex];
                matchUI.CurrentReplayNotation = currentCommand.Notation;
            }
        }

        private void HandleReplayPrev()
        {
            if (_engine.Undo())
            {
                SyncBoardToState();
                matchUI.ReplayStep = _engine.History.CurrentIndex + 1;
                if (_engine.History.CurrentIndex >= 0)
                {
                    var currentCommand = _engine.History.AllRecordedMoves[_engine.History.CurrentIndex];
                    matchUI.CurrentReplayNotation = currentCommand.Notation;
                }
                else
                {
                    matchUI.CurrentReplayNotation = "Game Start (Step 0)";
                }
            }
        }

        private void SyncBoardToState()
        {
            boardView.ClearTileHighlights();
            boardView.ClearAllWalls();
            foreach (var wall in _engine.State.PlacedWalls)
            {
                boardView.SpawnWall(wall);
            }

            for (int i = 0; i < _engine.State.PlayerCount; i++)
            {
                var p = (PlayerId)i;
                boardView.MovePawn(p, _engine.State.GetPlayerPosition(p));
            }
        }
    }
}

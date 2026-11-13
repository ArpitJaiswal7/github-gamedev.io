using System;
using UnityEngine;
using Blockade2D.Core.Engine;
using Blockade2D.Core.Model;
using Blockade2D.Core.Rules;
using Blockade2D.Presentation.Views;

namespace Blockade2D.Presentation.Controllers
{
    /// <summary>
    /// Manages wall rotation, cursor snapping, ghost wall previews (faded real-wall color),
    /// and both Click-and-Drop and Direct-Drag-and-Drop wall placement.
    /// </summary>
    public class WallPlacementController : MonoBehaviour
    {
        public WallOrientation CurrentOrientation { get; private set; } = WallOrientation.Horizontal;
        public BoardPosition? HoveredIntersection { get; private set; }
        public bool IsDragging { get; private set; }

        [Header("Drag Offset Settings")]
        [Tooltip("Vertical world units offset so blocker preview floats clearly above the finger while dragging")]
        [SerializeField] private float dragYOffset = 1.6f;

        public float DragYOffset
        {
            get => dragYOffset;
            set => dragYOffset = value;
        }

        public event Action<WallOrientation> OnOrientationChanged;
        public event Action<Wall> OnWallSuccessfullyPlaced;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            // Keyboard shortcut 'R' or Space to quickly flip wall orientation
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
            {
                ToggleOrientation();
            }
        }

        public void ToggleOrientation()
        {
            CurrentOrientation = CurrentOrientation == WallOrientation.Horizontal
                ? WallOrientation.Vertical
                : WallOrientation.Horizontal;

            OnOrientationChanged?.Invoke(CurrentOrientation);
        }

        public void SetOrientation(WallOrientation orientation)
        {
            if (CurrentOrientation != orientation)
            {
                CurrentOrientation = orientation;
                OnOrientationChanged?.Invoke(CurrentOrientation);
            }
        }

        public void StartDragging(WallOrientation orientation)
        {
            SetOrientation(orientation);
            IsDragging = true;
        }

        public void StopDragging()
        {
            IsDragging = false;
        }

        /// <summary>
        /// Continuously tracks cursor in world coordinates and snaps ghost wall to nearest intersection.
        /// Strictly active only while holding and dragging a wall button.
        /// </summary>
        public void UpdateCursorTracking(
            GameEngine engine,
            BoardView boardView,
            bool isHumanInputAllowed)
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null || boardView == null || boardView.GhostWall == null)
                return;

            // Wall preview is strictly active only while holding and dragging
            if (!isHumanInputAllowed || !IsDragging || engine.State.IsGameOver)
            {
                if (HoveredIntersection.HasValue)
                {
                    HoveredIntersection = null;
                    boardView.GhostWall.Hide();
                }
                return;
            }

            Vector3 pointerScreen = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            // Ignore if cursor is outside viewport
            if (pointerScreen.x < 0 || pointerScreen.x > Screen.width || pointerScreen.y < 0 || pointerScreen.y > Screen.height)
            {
                boardView.GhostWall.Hide();
                HoveredIntersection = null;
                return;
            }

            Vector3 pointerWorld = _mainCamera.ScreenToWorldPoint(pointerScreen);
            pointerWorld.z = 0f;

            // Apply +Y vertical offset so the preview appears clearly above the finger, and placement snaps to that preview position
            float effectiveYOffset = dragYOffset > 0f ? dragYOffset : (boardView != null ? boardView.Step * 1.35f : 1.6f);
            Vector3 previewTargetWorld = pointerWorld + new Vector3(0f, effectiveYOffset, 0f);

            if (boardView.TryGetNearestIntersection(previewTargetWorld, out BoardPosition snappedIntersection))
            {
                OnIntersectionHovered(snappedIntersection, engine, boardView);
            }
            else
            {
                OnIntersectionUnhovered(boardView);
            }
        }

        public void OnIntersectionHovered(
            BoardPosition intersection,
            GameEngine engine,
            BoardView boardView,
            bool isWallPlacementActive = true)
        {
            HoveredIntersection = intersection;

            if (!IsDragging || engine.State.IsGameOver)
            {
                boardView.GhostWall.Hide();
                return;
            }

            var proposedWall = new Wall(intersection, CurrentOrientation, engine.State.ActivePlayer);
            var error = engine.CheckWallLegality(engine.State.ActivePlayer, proposedWall);
            bool isValid = error == WallPlacementError.None;

            Color playerColor = boardView.GetPlayerWallColor(engine.State.ActivePlayer);
            Vector3 worldPos = boardView.IntersectionToWorldPosition(intersection);
            boardView.GhostWall.UpdatePreview(worldPos, CurrentOrientation, isValid, playerColor);
        }

        public void OnIntersectionUnhovered(BoardView boardView)
        {
            HoveredIntersection = null;
            if (boardView != null && boardView.GhostWall != null)
            {
                boardView.GhostWall.Hide();
            }
        }

        public bool TryCommitWall(
            BoardPosition intersection,
            GameEngine engine,
            BoardView boardView)
        {
            var wall = new Wall(intersection, CurrentOrientation, engine.State.ActivePlayer);
            var result = engine.TryPlaceWall(engine.State.ActivePlayer, wall);

            if (result.IsSuccess)
            {
                boardView.GhostWall.Hide();
                HoveredIntersection = null;
                IsDragging = false;
                OnWallSuccessfullyPlaced?.Invoke(wall);
                return true;
            }
            else
            {
                boardView.GhostWall.TriggerErrorShake();
                return false;
            }
        }

        public void CancelPlacement(BoardView boardView)
        {
            HoveredIntersection = null;
            IsDragging = false;
            if (boardView != null && boardView.GhostWall != null)
            {
                boardView.GhostWall.Hide();
            }
        }
    }
}

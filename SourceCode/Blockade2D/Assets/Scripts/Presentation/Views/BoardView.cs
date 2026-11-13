using System;
using System.Collections.Generic;
using UnityEngine;
using Blockade2D.Core.Model;
using Blockade2D.Presentation.Utils;

namespace Blockade2D.Presentation.Views
{
    /// <summary>
    /// Generates and renders the visual board, tiles, pawns, intersection slots, and placed walls.
    /// Supports both Classic 2-player and Bulls Eye 4-player game modes.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject pawnPrefab;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject ghostWallPrefab;

        [Header("Board Sizing")]
        [SerializeField] private int boardSize = 9;
        [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private float gutterSize = 0.08f; // Reduced gap for a tighter, sleeker grid
        [SerializeField] private float wallThickness = 0.24f; // Width offset for vertical wall, height offset for horizontal wall

        [Header("Player Colors")]
        [SerializeField] private Color player1Color = new Color(0.22f, 0.74f, 0.97f, 1f); // Electric Blue
        [SerializeField] private Color player2Color = new Color(0.98f, 0.57f, 0.24f, 1f); // Vibrant Orange
        [SerializeField] private Color player3Color = new Color(0.18f, 0.88f, 0.55f, 1f); // Emerald Green
        [SerializeField] private Color player4Color = new Color(0.88f, 0.32f, 0.88f, 1f); // Vibrant Purple

        [Header("Blocker / Wall Colors")]
        [SerializeField] private Color player1WallColor = new Color(0.22f, 0.78f, 1f, 1f); // Cyan/Blue Barricade
        [SerializeField] private Color player2WallColor = new Color(0.98f, 0.55f, 0.18f, 1f); // Amber/Orange Barricade
        [SerializeField] private Color player3WallColor = new Color(0.18f, 0.88f, 0.55f, 1f); // Emerald Barricade
        [SerializeField] private Color player4WallColor = new Color(0.88f, 0.32f, 0.88f, 1f); // Purple Barricade

        private readonly Dictionary<BoardPosition, TileView> _tiles = new Dictionary<BoardPosition, TileView>();
        private readonly Dictionary<BoardPosition, IntersectionSlotView> _intersectionSlots = new Dictionary<BoardPosition, IntersectionSlotView>();
        private readonly Dictionary<Wall, WallView> _placedWallViews = new Dictionary<Wall, WallView>();
        private readonly Dictionary<PlayerId, PawnView> _pawns = new Dictionary<PlayerId, PawnView>();

        private Transform _tilesContainer;
        private Transform _slotsContainer;
        private Transform _pawnsContainer;
        private Transform _placedWallsContainer;

        private GhostWallView _ghostWall;
        private GameMode _gameMode = GameMode.Classic;

        private Sprite _tileSprite;
        private Sprite _bullsEyeSprite;
        private Sprite _pawnSprite;
        private Sprite _wallSprite;

        public float Step => cellSize + gutterSize;
        public float BoardExtent => (boardSize * cellSize) + ((boardSize - 1) * gutterSize);
        public float WallLength => (2 * cellSize) + gutterSize;
        public float WallThickness => wallThickness;
        public GhostWallView GhostWall => _ghostWall;
        public GameMode Mode => _gameMode;

        public event Action<BoardPosition> OnTileClicked;
        public event Action<BoardPosition> OnIntersectionHovered;
        public event Action<BoardPosition> OnIntersectionUnhovered;
        public event Action<BoardPosition> OnIntersectionClicked;
        public event Action<PlayerId> OnPawnClicked;

        public void InitializeBoard(int size = 9, GameMode mode = GameMode.Classic)
        {
            boardSize = size;
            _gameMode = mode;
            LoadPrefabsAndSprites();
            CreateContainersIfMissing();
            InstantiateTiles();
            InstantiateIntersectionSlots();
            InstantiatePawns();
            InstantiateGhostWall();
        }

        private void LoadPrefabsAndSprites()
        {
            if (tilePrefab == null) tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");
            if (pawnPrefab == null) pawnPrefab = Resources.Load<GameObject>("Prefabs/Pawn");
            if (slotPrefab == null) slotPrefab = Resources.Load<GameObject>("Prefabs/IntersectionSlot");
            if (wallPrefab == null) wallPrefab = Resources.Load<GameObject>("Prefabs/Wall");
            if (ghostWallPrefab == null) ghostWallPrefab = Resources.Load<GameObject>("Prefabs/GhostWall");

            GenerateSprites();
        }

        private void CreateContainersIfMissing()
        {
            _tilesContainer = transform.Find("Tiles");
            if (_tilesContainer == null)
            {
                var go = new GameObject("Tiles");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                _tilesContainer = go.transform;
            }

            _slotsContainer = transform.Find("IntersectionSlots");
            if (_slotsContainer == null)
            {
                var go = new GameObject("IntersectionSlots");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                _slotsContainer = go.transform;
            }

            _pawnsContainer = transform.Find("Pawns");
            if (_pawnsContainer == null)
            {
                var go = new GameObject("Pawns");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                _pawnsContainer = go.transform;
            }

            _placedWallsContainer = transform.Find("PlacedWalls");
            if (_placedWallsContainer == null)
            {
                var go = new GameObject("PlacedWalls");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                _placedWallsContainer = go.transform;
            }
        }

        private void InstantiateTiles()
        {
            _tiles.Clear();
            var existingChildren = new List<GameObject>();
            foreach (Transform child in _tilesContainer)
            {
                existingChildren.Add(child.gameObject);
            }
            foreach (var child in existingChildren)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var defaultTileColor = new Color(0.12f, 0.15f, 0.19f, 1f);
            var centerPos = new BoardPosition(boardSize / 2, boardSize / 2);

            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    var pos = new BoardPosition(x, y);
                    GameObject tileObj;
                    if (tilePrefab != null)
                    {
                        tileObj = Instantiate(tilePrefab, _tilesContainer);
                    }
                    else
                    {
                        tileObj = new GameObject($"Tile_{x}_{y}");
                        tileObj.transform.SetParent(_tilesContainer);
                        tileObj.AddComponent<SpriteRenderer>();
                        tileObj.AddComponent<BoxCollider2D>();
                        tileObj.AddComponent<TileView>();
                    }
                    tileObj.name = $"Tile_{x}_{y}";
                    tileObj.transform.position = GridToWorldPosition(pos);

                    var tileView = tileObj.GetComponent<TileView>();

                    if (_gameMode == GameMode.BullsEye && pos == centerPos)
                    {
                        // Distinct Bulls Eye target cell visual in the center
                        tileView.Initialize(pos, _bullsEyeSprite ?? _tileSprite, cellSize, Color.white);
                    }
                    else
                    {
                        Color tileBaseColor = GetTileBaseColor(pos, defaultTileColor);
                        tileView.Initialize(pos, _tileSprite, cellSize, tileBaseColor);
                    }

                    tileView.OnTileClicked += HandleTileClickedInternal;
                    _tiles[pos] = tileView;
                }
            }
        }

        private void InstantiateIntersectionSlots()
        {
            _intersectionSlots.Clear();
            var existingChildren = new List<GameObject>();
            foreach (Transform child in _slotsContainer)
            {
                existingChildren.Add(child.gameObject);
            }
            foreach (var child in existingChildren)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            int intersectionsCount = boardSize - 1;
            for (int y = 0; y < intersectionsCount; y++)
            {
                for (int x = 0; x < intersectionsCount; x++)
                {
                    var intersection = new BoardPosition(x, y);
                    GameObject slotObj;
                    if (slotPrefab != null)
                    {
                        slotObj = Instantiate(slotPrefab, _slotsContainer);
                    }
                    else
                    {
                        slotObj = new GameObject($"Slot_{x}_{y}");
                        slotObj.transform.SetParent(_slotsContainer);
                        slotObj.AddComponent<BoxCollider2D>();
                        slotObj.AddComponent<IntersectionSlotView>();
                    }
                    slotObj.name = $"Slot_{x}_{y}";
                    slotObj.transform.position = IntersectionToWorldPosition(intersection);

                    var slotView = slotObj.GetComponent<IntersectionSlotView>();
                    slotView.Initialize(intersection, Step * 0.45f);
                    slotView.OnHovered += HandleIntersectionHoveredInternal;
                    slotView.OnUnhovered += HandleIntersectionUnhoveredInternal;
                    slotView.OnClicked += HandleIntersectionClickedInternal;

                    _intersectionSlots[intersection] = slotView;
                }
            }
        }

        private void InstantiatePawns()
        {
            _pawns.Clear();
            var existingChildren = new List<GameObject>();
            foreach (Transform child in _pawnsContainer)
            {
                existingChildren.Add(child.gameObject);
            }
            foreach (var child in existingChildren)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            int playerCount = _gameMode == GameMode.BullsEye ? 4 : 2;

            for (int i = 0; i < playerCount; i++)
            {
                var player = (PlayerId)i;
                var pPos = player.StartingPosition(boardSize);
                string pName = $"Pawn_P{i + 1}";

                GameObject pObj = pawnPrefab != null ? Instantiate(pawnPrefab, _pawnsContainer) : new GameObject(pName);
                pObj.name = pName;
                pObj.transform.position = GridToWorldPosition(pPos);

                var pawnView = pObj.GetComponent<PawnView>() ?? pObj.AddComponent<PawnView>();
                pawnView.Initialize(player, pPos, GridToWorldPosition(pPos), _pawnSprite, GetPlayerColor(player), cellSize * 0.72f);
                pawnView.OnPawnClicked += HandlePawnClickedInternal;

                _pawns[player] = pawnView;
            }
        }

        private void InstantiateGhostWall()
        {
            Transform existingGhost = transform.Find("GhostWall");
            if (existingGhost != null)
            {
                if (Application.isPlaying) Destroy(existingGhost.gameObject);
                else DestroyImmediate(existingGhost.gameObject);
            }

            GameObject ghostObj = ghostWallPrefab != null ? Instantiate(ghostWallPrefab, transform) : new GameObject("GhostWall");
            ghostObj.name = "GhostWall";
            _ghostWall = ghostObj.GetComponent<GhostWallView>() ?? ghostObj.AddComponent<GhostWallView>();
            _ghostWall.Setup(_wallSprite, WallLength, WallThickness);
            _ghostWall.Hide();
        }

        private void GenerateSprites()
        {
            if (_tileSprite == null)
            {
                _tileSprite = Resources.Load<Sprite>("Sprites/TileRoundedSquare");
                if (_tileSprite == null)
                    _tileSprite = SpriteUtils.CreateRoundedSquareSprite(128, 20);
            }

            if (_bullsEyeSprite == null)
            {
                _bullsEyeSprite = Resources.Load<Sprite>("Sprites/TileBullsEye");
                if (_bullsEyeSprite == null)
                    _bullsEyeSprite = SpriteUtils.CreateBullsEyeSprite(128);
            }

            if (_pawnSprite == null)
            {
                _pawnSprite = Resources.Load<Sprite>("Sprites/PawnCircle");
                if (_pawnSprite == null)
                    _pawnSprite = SpriteUtils.CreateCircleSprite(128);
            }

            if (_wallSprite == null)
            {
                _wallSprite = Resources.Load<Sprite>("Sprites/WallRoundedSquare");
                if (_wallSprite == null)
                    _wallSprite = SpriteUtils.CreateRoundedSquareSprite(64, 12);
            }
        }

        public Color GetTileBaseColor(BoardPosition pos, Color defaultTileColor)
        {
            if (_gameMode == GameMode.BullsEye)
            {
                int mid = boardSize / 2;
                int max = boardSize - 1;

                // Border midpoints faint player tints
                if (pos.X == mid && pos.Y == 0) return Color.Lerp(defaultTileColor, player1Color, 0.28f);
                if (pos.X == mid && pos.Y == max) return Color.Lerp(defaultTileColor, player2Color, 0.28f);
                if (pos.X == 0 && pos.Y == mid) return Color.Lerp(defaultTileColor, player3Color, 0.28f);
                if (pos.X == max && pos.Y == mid) return Color.Lerp(defaultTileColor, player4Color, 0.28f);

                return defaultTileColor;
            }
            else
            {
                if (pos.Y == 0)
                {
                    // Bottom edge row: Opponent's faded color (Player 2 Orange)
                    return Color.Lerp(defaultTileColor, player2Color, 0.28f);
                }
                else if (pos.Y == boardSize - 1)
                {
                    // Top edge row: Opponent's faded color (Player 1 Blue)
                    return Color.Lerp(defaultTileColor, player1Color, 0.28f);
                }
                return defaultTileColor;
            }
        }

        public Color GetRowBaseColor(int y, Color defaultTileColor)
        {
            return GetTileBaseColor(new BoardPosition(0, y), defaultTileColor);
        }

        private void HandleTileClickedInternal(BoardPosition pos) => OnTileClicked?.Invoke(pos);
        private void HandleIntersectionHoveredInternal(BoardPosition pos) => OnIntersectionHovered?.Invoke(pos);
        private void HandleIntersectionUnhoveredInternal(BoardPosition pos) => OnIntersectionUnhovered?.Invoke(pos);
        private void HandleIntersectionClickedInternal(BoardPosition pos) => OnIntersectionClicked?.Invoke(pos);
        private void HandlePawnClickedInternal(PlayerId player) => OnPawnClicked?.Invoke(player);

#if UNITY_EDITOR
        [ContextMenu("Clear Board Hierarchy")]
        public void ClearHierarchyInEditor()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (var child in children)
            {
                DestroyImmediate(child);
            }
            _tiles.Clear();
            _intersectionSlots.Clear();
            _placedWallViews.Clear();
            _pawns.Clear();
            _ghostWall = null;
            _placedWallsContainer = null;
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif

        public Vector3 GridToWorldPosition(BoardPosition pos)
        {
            float halfExtent = (boardSize - 1) / 2.0f;
            float worldX = (pos.X - halfExtent) * Step;
            float worldY = (pos.Y - halfExtent) * Step;
            return new Vector3(worldX, worldY, 0f);
        }

        public bool TryGetGridPositionFromWorld(Vector3 worldPos, out BoardPosition pos)
        {
            pos = default;
            float halfExtent = (boardSize - 1) / 2.0f;
            float continuousX = (worldPos.x / Step) + halfExtent;
            float continuousY = (worldPos.y / Step) + halfExtent;

            int gx = Mathf.RoundToInt(continuousX);
            int gy = Mathf.RoundToInt(continuousY);

            if (gx >= 0 && gx < boardSize && gy >= 0 && gy < boardSize)
            {
                float dx = Mathf.Abs(continuousX - gx);
                float dy = Mathf.Abs(continuousY - gy);
                float halfCellInSteps = (cellSize * 0.5f) / Step;

                if (dx <= halfCellInSteps && dy <= halfCellInSteps)
                {
                    pos = new BoardPosition(gx, gy);
                    return true;
                }
            }
            return false;
        }

        public Vector3 IntersectionToWorldPosition(BoardPosition intersection)
        {
            float halfExtent = (boardSize - 1) / 2.0f;
            float worldX = (intersection.X - halfExtent + 0.5f) * Step;
            float worldY = (intersection.Y - halfExtent + 0.5f) * Step;
            return new Vector3(worldX, worldY, 0f);
        }

        public bool TryGetNearestIntersection(Vector3 worldPos, out BoardPosition intersection, float maxDistanceMultiplier = 1.0f)
        {
            intersection = default;
            int maxIntersection = boardSize - 2; // For 9x9 board: 0 to 7
            float halfExtent = (boardSize - 1) / 2.0f;

            float continuousX = (worldPos.x / Step) + halfExtent - 0.5f;
            float continuousY = (worldPos.y / Step) + halfExtent - 0.5f;

            int ix = Mathf.Clamp(Mathf.RoundToInt(continuousX), 0, maxIntersection);
            int iy = Mathf.Clamp(Mathf.RoundToInt(continuousY), 0, maxIntersection);

            Vector3 snappedPos = IntersectionToWorldPosition(new BoardPosition(ix, iy));
            float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(snappedPos.x, snappedPos.y));

            if (dist <= Step * maxDistanceMultiplier)
            {
                intersection = new BoardPosition(ix, iy);
                return true;
            }

            return false;
        }

        public void HighlightTiles(IEnumerable<BoardPosition> validPositions)
        {
            ClearTileHighlights();
            foreach (var pos in validPositions)
            {
                if (_tiles.TryGetValue(pos, out var tileView))
                {
                    tileView.SetHighlight(true);
                }
            }
        }

        public void ClearTileHighlights()
        {
            foreach (var tile in _tiles.Values)
            {
                tile.SetHighlight(false);
            }
        }

        public void MovePawn(PlayerId player, BoardPosition newPos, Action onComplete = null)
        {
            if (_pawns.TryGetValue(player, out var pawn))
            {
                pawn.MoveTo(newPos, GridToWorldPosition(newPos), onComplete);
            }
        }

        public Color GetPlayerColor(PlayerId player) => player switch
        {
            PlayerId.Player1 => player1Color,
            PlayerId.Player2 => player2Color,
            PlayerId.Player3 => player3Color,
            PlayerId.Player4 => player4Color,
            _ => player1Color
        };

        public Color GetPlayerWallColor(PlayerId player) => player switch
        {
            PlayerId.Player1 => player1WallColor,
            PlayerId.Player2 => player2WallColor,
            PlayerId.Player3 => player3WallColor,
            PlayerId.Player4 => player4WallColor,
            _ => player1WallColor
        };

        public void SpawnWall(Wall wall)
        {
            if (_placedWallViews.ContainsKey(wall))
                return;

            GameObject wallObj;
            if (wallPrefab != null)
            {
                wallObj = Instantiate(wallPrefab, _placedWallsContainer != null ? _placedWallsContainer : transform);
            }
            else
            {
                wallObj = new GameObject($"Wall_{wall.Orientation}_{wall.Intersection}_{wall.PlacedBy}");
                wallObj.transform.SetParent(_placedWallsContainer != null ? _placedWallsContainer : transform);
                wallObj.AddComponent<SpriteRenderer>();
                wallObj.AddComponent<WallView>();
            }
            wallObj.name = $"Wall_{wall.Orientation}_{wall.Intersection}_{wall.PlacedBy}";

            Color color = GetPlayerWallColor(wall.PlacedBy);

            var wallView = wallObj.GetComponent<WallView>() ?? wallObj.AddComponent<WallView>();
            wallView.Initialize(
                wall,
                IntersectionToWorldPosition(wall.Intersection),
                _wallSprite,
                WallLength,
                WallThickness,
                color);

            _placedWallViews[wall] = wallView;
        }

        public void RemoveWall(Wall wall)
        {
            if (_placedWallViews.TryGetValue(wall, out var wallView))
            {
                Destroy(wallView.gameObject);
                _placedWallViews.Remove(wall);
            }
        }

        public void ClearAllWalls()
        {
            foreach (var view in _placedWallViews.Values)
            {
                Destroy(view.gameObject);
            }
            _placedWallViews.Clear();
        }
    }
}

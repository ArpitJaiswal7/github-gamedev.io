using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowStrike.ShapeSystem
{
    public class ShapeGridSystem : MonoBehaviour
    {
        [Header("Grid Prefab & Spacing")]
        public GameObject cellPrefab;
        public int gridWidth = 36;
        public int gridHeight = 36;
        public float cellSizeX = 0.22f;
        public float cellSizeY = 0.22f;
        public float cellOffset = 0.02f;
        public Color cellBoardTint = new Color(0.85f, 0.9f, 0.96f, 0.18f);

        [Header("Grid Origin")]
        public Vector2 platformOrigin = Vector2.zero;

        [Header("Camera Control")]
        public Camera mainCamera;
        public float cameraPadding = 0.35f;

        [HideInInspector] public ShapeGridCell[,] gridArray;

        private GameObject[,] occupantMap;
        private bool[,] activeMask;
        private Color[,] colorMap;
        private readonly List<Vector2Int> activeCoordinates = new List<Vector2Int>();
        private int lastScreenW;
        private int lastScreenH;

        public IReadOnlyList<Vector2Int> ActiveCoordinates => activeCoordinates;

        private void Update()
        {
            // Keep camera framed if WebGL browser window or mobile orientation resizes
            if (gridArray != null && (Screen.width != lastScreenW || Screen.height != lastScreenH))
            {
                AdjustCameraToGrid();
            }
        }

        public IEnumerator InitializeShapedGrid(ShapeLevelConfig config)
        {
            ClearGridObjects();

            if (config != null)
            {
                config.ResolveGridDimensions(out gridWidth, out gridHeight);
                config.BuildShapeData(gridWidth, gridHeight, out activeMask, out colorMap);
            }
            else
            {
                activeMask = new bool[gridWidth, gridHeight];
                colorMap = new Color[gridWidth, gridHeight];
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        activeMask[x, y] = true;
                        colorMap[x, y] = Color.white;
                    }
                }
            }

            gridArray = new ShapeGridCell[gridWidth, gridHeight];
            occupantMap = new GameObject[gridWidth, gridHeight];
            activeCoordinates.Clear();

            float stepX = cellSizeX + cellOffset;
            float stepY = cellSizeY + cellOffset;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (!activeMask[x, y])
                        continue;

                    Vector3 worldPos = new Vector3(
                        platformOrigin.x + x * stepX,
                        platformOrigin.y + y * stepY,
                        0f
                    );

                    GameObject cellObj = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
                    cellObj.name = $"Cell_{x}_{y}";
                    cellObj.transform.localScale = new Vector3(cellSizeX, cellSizeY, 1f);

                    if (!cellObj.TryGetComponent(out ShapeGridCell cell))
                    {
                        cell = cellObj.AddComponent<ShapeGridCell>();
                    }

                    Vector2Int coord = new Vector2Int(x, y);
                    cell.Configure(coord, colorMap[x, y], cellBoardTint);
                    gridArray[x, y] = cell;
                    activeCoordinates.Add(coord);
                }
            }

            AdjustCameraToGrid();
            yield return null;
        }

        public void ClearGridObjects()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            activeCoordinates.Clear();
        }

        public void AdjustCameraToGrid()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            lastScreenW = Screen.width;
            lastScreenH = Screen.height;

            float stepX = cellSizeX + cellOffset;
            float stepY = cellSizeY + cellOffset;

            // Compute tight bounds around actual active shape cells so the shape fills the screen prominently
            int minX = 0, maxX = gridWidth - 1;
            int minY = 0, maxY = gridHeight - 1;

            if (activeCoordinates.Count > 0)
            {
                minX = int.MaxValue; maxX = int.MinValue;
                minY = int.MaxValue; maxY = int.MinValue;
                for (int i = 0; i < activeCoordinates.Count; i++)
                {
                    Vector2Int c = activeCoordinates[i];
                    if (c.x < minX) minX = c.x;
                    if (c.x > maxX) maxX = c.x;
                    if (c.y < minY) minY = c.y;
                    if (c.y > maxY) maxY = c.y;
                }
            }

            float activeWidth = (maxX - minX + 1) * stepX;
            float activeHeight = (maxY - minY + 1) * stepY;

            // Slight vertical offset (+stepY * 0.6f) so top UI header doesn't overlap the top of the shape
            Vector3 shapeCenter = new Vector3(
                platformOrigin.x + (minX + maxX) * 0.5f * stepX,
                platformOrigin.y + (minY + maxY) * 0.5f * stepY + stepY * 0.5f,
                -10f
            );

            mainCamera.transform.position = shapeCenter;

            if (mainCamera.orthographic)
            {
                float aspectRatio = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.77f;
                float halfHeight = activeHeight * 0.5f + cameraPadding;
                float halfWidthAsHeight = (activeWidth / Mathf.Max(0.1f, aspectRatio)) * 0.5f + cameraPadding;
                mainCamera.orthographicSize = Mathf.Max(halfHeight, halfWidthAsHeight);
            }
        }

        public Vector3 GetWorldPosition(Vector2Int coord)
        {
            float stepX = cellSizeX + cellOffset;
            float stepY = cellSizeY + cellOffset;
            return new Vector3(
                platformOrigin.x + coord.x * stepX,
                platformOrigin.y + coord.y * stepY,
                0f
            );
        }

        /// <summary>
        /// True if the coordinate is within the outer [0..width-1, 0..height-1] bounding rectangle
        /// (even if the cell itself is an empty gap inside a concave animal/object shape).
        /// </summary>
        public bool IsInsideBoundingBox(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < gridWidth &&
                   coord.y >= 0 && coord.y < gridHeight;
        }

        /// <summary>
        /// True only if the coordinate is inside the bounding box AND part of the active shape mask.
        /// </summary>
        public bool IsValidActiveCell(Vector2Int coord)
        {
            return IsInsideBoundingBox(coord) &&
                   gridArray != null &&
                   gridArray[coord.x, coord.y] != null;
        }

        public bool IsCellFree(Vector2Int coord)
        {
            return IsValidActiveCell(coord) && !gridArray[coord.x, coord.y].isOccupied;
        }

        public ShapeGridCell GetCell(Vector2Int coord)
        {
            if (!IsInsideBoundingBox(coord) || gridArray == null)
                return null;
            return gridArray[coord.x, coord.y];
        }

        public Color GetCellColor(Vector2Int coord)
        {
            if (IsInsideBoundingBox(coord) && colorMap != null)
                return colorMap[coord.x, coord.y];
            return Color.white;
        }

        public bool IsCoordinateOccupiedByOther(Vector2Int coord, GameObject self)
        {
            if (!IsInsideBoundingBox(coord))
                return false;

            if (occupantMap != null)
            {
                GameObject occ = occupantMap[coord.x, coord.y];
                if (occ != null && occ != self)
                    return true;
            }

            ShapeGridCell cell = GetCell(coord);
            return cell != null && cell.isOccupied && cell.occupant != null && cell.occupant != self;
        }

        public void MarkCellOccupied(Vector2Int coord, GameObject occupant)
        {
            if (!IsInsideBoundingBox(coord))
                return;

            if (occupantMap != null)
                occupantMap[coord.x, coord.y] = occupant;

            if (IsValidActiveCell(coord))
                gridArray[coord.x, coord.y].OccupyCell(occupant);
        }

        public void MarkCellFree(Vector2Int coord)
        {
            if (!IsInsideBoundingBox(coord))
                return;

            if (occupantMap != null)
                occupantMap[coord.x, coord.y] = null;

            if (IsValidActiveCell(coord))
                gridArray[coord.x, coord.y].FreeCell();
        }

        public void ClearAllOccupancy()
        {
            if (occupantMap != null)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        occupantMap[x, y] = null;
                    }
                }
            }

            if (gridArray == null) return;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (gridArray[x, y] != null)
                        gridArray[x, y].FreeCell();
                }
            }
        }
    }
}

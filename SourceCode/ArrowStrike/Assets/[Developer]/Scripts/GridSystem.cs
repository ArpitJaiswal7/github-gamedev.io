using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject gridPrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSizeX = 0.01f;
    public float cellSizeY = 0.01f;
    public float cellOffset = 0.02f;

    [Header("Grid Origin")]
    public Vector2 platformOrigin; 

    [Header("Camera Control")]
    public Camera mainCamera;
    public float cameraPadding = 1.5f; 

    [HideInInspector] public Grid[,] gridArray;

    private void Start()
    {
        StartCoroutine(InitializeGrid());
    }


    public IEnumerator InitializeGrid()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        gridArray = new Grid[gridWidth, gridHeight];
        AdjustCameraToGrid();
        // Build grid cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPosition = new Vector3(
                    platformOrigin.x + x * (cellSizeX + cellOffset),
                    platformOrigin.y + y * (cellSizeY + cellOffset),
                    0f
                );

                GameObject gridObject = Instantiate(gridPrefab, worldPosition, Quaternion.identity, transform);
                gridObject.name = $"Grid {x}-{y}";
                gridObject.transform.localScale = Vector3.one * 0.01f;

                Grid gridComponent = gridObject.GetComponent<Grid>();
                if (gridComponent != null)
                {
                    gridComponent.gridPosition = new Vector2Int(x, y);
                    gridArray[x, y] = gridComponent;
                }
            }
        }
        ColorizeQuadrants();
        yield return null;
    }

    // --- CAMERA LOGIC ---
    private void AdjustCameraToGrid()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("No camera found for GridSystem to adjust!");
            return;
        }

        float totalWidth = gridWidth * (cellSizeX + cellOffset);
        float totalHeight = gridHeight * (cellSizeY + cellOffset);

        Vector3 gridCenter = new Vector3(
            platformOrigin.x + totalWidth / 2f - (cellSizeX + cellOffset) / 2f,
            platformOrigin.y + totalHeight / 2f - (cellSizeY + cellOffset) / 2f,
            -10f // camera z position (for 2D view)
        );

        mainCamera.transform.position = gridCenter;

        if (mainCamera.orthographic)
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            float gridAspect = totalWidth / totalHeight;

            if (gridAspect > aspectRatio)
                mainCamera.orthographicSize = (totalWidth / aspectRatio) / 2f + cameraPadding;
            else
                mainCamera.orthographicSize = totalHeight / 2f + cameraPadding;
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        float x = platformOrigin.x + gridPosition.x * (cellSizeX + cellOffset);
        float y = platformOrigin.y + gridPosition.y * (cellSizeY + cellOffset);
        return new Vector3(x, y, 0f);
    }

    public Grid GetGridByPosition(Vector2Int targetGridPosition)
    {
        return gridArray.Cast<Grid>().FirstOrDefault(grid => grid.gridPosition == targetGridPosition);
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        float x = worldPosition.x - platformOrigin.x;
        float y = worldPosition.y - platformOrigin.y;

        int gridX = Mathf.FloorToInt(x / (cellSizeX + cellOffset));
        int gridY = Mathf.FloorToInt(y / (cellSizeY + cellOffset));

        return new Vector2Int(
            Mathf.Clamp(gridX, 0, gridWidth - 1),
            Mathf.Clamp(gridY, 0, gridHeight - 1)
        );
    }

    public bool AreCellsFree(Vector2Int startGridPosition)
    {
        Debug.Log($"StartGridPos: {startGridPosition} \t gridArray[{startGridPosition.x}][{startGridPosition.y}]");
        if (!IsValidGridPosition(startGridPosition) ||
            !gridArray[startGridPosition.x, startGridPosition.y].IsFree())
            return false;

        return true;
    }

    public void MarkCellOccupied(Vector2Int gridPosition)
    {
        gridArray[gridPosition.x, gridPosition.y].OccupyCell(null);
    }

    public bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }
    
    public bool IsCellFree(Vector2Int cell)
    {
        return IsValidCell(cell) && !gridArray[cell.x,cell.y].isOccupied;
    }

    public void MarkCellOccupied(Vector2Int gridPosition, GameObject occupant)
    {
        if (IsValidGridPosition(gridPosition))
            gridArray[gridPosition.x, gridPosition.y].OccupyCell(occupant);
    }

    public void MarkCellFree(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
            gridArray[gridPosition.x, gridPosition.y].FreeCell();
    }

    private bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }

    public void ClearAllCellsOccupied()  
    {
        foreach (var grid in gridArray)
        {
            if (grid != null)
                grid.isOccupied = false;
        }
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        foreach (var item in gridArray)
    //        {
    //            item.gameObject.SetActive(!item.isOccupied);
    //        }
    //    }
    //    else if(Input.GetKeyDown(KeyCode.R))
    //        foreach (var item in gridArray)
    //            item.gameObject.SetActive(true);
    //}


    private void ColorizeQuadrants()
    {
        return;
        int midX = gridWidth / 2;
        int midY = gridHeight / 2;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var grid = gridArray[x, y];
                if (grid == null) continue;

                var renderer = grid.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;

                if (x < midX && y >= midY)
                    renderer.color = Color.red; // Top Left
                else if (x >= midX && y >= midY)
                    renderer.color = Color.blue; // Top Right
                else if (x < midX && y < midY)
                    renderer.color = Color.green; // Bottom Left
                else
                    renderer.color = Color.yellow; // Bottom Right
            }
        }
    }


    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    int cellNum = 0;
    //    for (int x = 0; x < gridWidth; x++)
    //    {
    //        for (int y = 0; y < gridHeight; y++)
    //        {
    //            Vector3 cellCenter = GetWorldPosition(new Vector2Int(x, y));
    //            cellNum++;
    //            Gizmos.DrawWireCube(cellCenter, new Vector3(cellSizeX, cellSizeY, 0.01f));
    //            Handles.color = Color.red;
    //            Handles.Label(cellCenter, $"{x},{y}\n{cellNum}",new GUIStyle { fontSize = 15, alignment = TextAnchor.MiddleCenter}); //({x},{y}) = 
    //        }
    //    }
    //}
}

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridCell
{
    public Vector2Int position;
    public bool occupied;
    public GameObject occupant;
}

public class GridManager : MonoBehaviour
{
    public int gridWidth = 24;
    public int gridHeight = 12;
    public float cellSize = 1f;
    private Dictionary<Vector2Int, GridCell> cells = new();


    private void Awake()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        cells.Clear();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new(x, y);
                cells[pos] = new GridCell { position = pos, occupied = false };
                GameObject _gridCell = Instantiate(cells[pos].occupant, new Vector3(pos.x,pos.y), Quaternion.identity);
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

    public bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }

    public bool IsCellFree(Vector2Int cell)
    {
        return IsValidCell(cell) && !cells[cell].occupied;
    }

    public void MarkCellOccupied(Vector2Int cell, GameObject obj)
    {
        if (IsValidCell(cell))
        {
            cells[cell].occupied = true;
            cells[cell].occupant = obj;
        }
    }

    public void MarkCellFree(Vector2Int cell)
    {
        if (IsValidCell(cell))
        {
            cells[cell].occupied = false;
            cells[cell].occupant = null;
        }
    }
}

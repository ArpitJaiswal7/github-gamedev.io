using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum ArrowDirection
{
    Left,
    Right,
    Up,
    Down,
    None
}

public class Arrow : MonoBehaviour
{
    public ArrowDirection direction;
    public GridSystem gridManager;
    private List<Vector2Int> path; 
    private List<ArrowUnit> units = new List<ArrowUnit>();

    public IEnumerator Initialize(GridSystem grid, List<Vector2Int> pathCells, ArrowDirection dir)
    {
        
        gridManager = grid;
        path = new List<Vector2Int>(pathCells);
        direction = dir;

        units.Clear();
        yield return new WaitForSeconds(0.3f);
        foreach (Transform t in transform)
        {
            if (t.TryGetComponent(out ArrowUnit au))
                units.Add(au);
        }

        
        Color arrowColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);
        for (int i = 0; i < units.Count; i++)
        {
            Grid gridCell = gridManager.GetGridByPosition(path[i]);
            units[i].Initialize(gridCell,arrowColor);
        }

        
        for (int i = 0; i < units.Count; i++)
        {
            
            ArrowUnit unit = units[i];

            List<Vector2Int> occupiedCells = path;

            Grid startGrid = gridManager.GetGridByPosition(occupiedCells[i]);
            unit.Initialize(startGrid, arrowColor);

            List<Vector2Int> exitCells = new List<Vector2Int>();

            Vector2Int _dir = path.Count >= 2 ? path[^1] - path[^2] : DirectionToVector2(direction);
            Vector2Int nextCell = occupiedCells[^1] + _dir;
            
            for (int step = 0; step < 50; step++)
            {
                if (occupiedCells.Contains(nextCell))
                {
                    Debug.Log("This one is occupied cell");
                    continue;
                }

                if (!gridManager.IsValidCell(nextCell))
                    break;

                exitCells.Add(nextCell);
                nextCell += _dir;
            }

            unit.SetExitPath(gridManager, exitCells);

        }
    }

    public List<ArrowUnit> GetArrowUnits()
    {
        return units;
    }

    public List<Grid> GetOccupiedGrids()
    {
        List<Grid> pathGrids = new();
        foreach (Vector2Int vInt in path)
            pathGrids.Add(gridManager.GetGridByPosition(vInt));

        return pathGrids;
    }

    public Vector2Int DirectionToVector2(ArrowDirection dir)
    {
        return dir switch
        {
            ArrowDirection.Up => Vector2Int.up,
            ArrowDirection.Down => Vector2Int.down,
            ArrowDirection.Left => Vector2Int.left,
            ArrowDirection.Right => Vector2Int.right,
            _ => Vector2Int.up
        };
    }
}

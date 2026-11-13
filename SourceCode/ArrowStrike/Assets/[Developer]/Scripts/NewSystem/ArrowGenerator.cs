using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowGenerator : MonoBehaviour
{
    public GridSystem gridManager;
    public GameObject arrowPrefab;

    [Range(2, 8)]
    public int arrowLength = 5;

    private void GenerateArrowFromTopLeft()
    {
        Vector2Int startCell = new Vector2Int(0, gridManager.gridHeight - 1); // top-left cell
        Vector2Int direction = ChooseValidDirection(startCell);
        if (direction == Vector2Int.zero)
        {
            Debug.LogWarning("No valid direction from start cell!");
            return;
        }

        List<Vector2Int> path = new();
        Vector2Int current = startCell;

        Debug.Log("Generating arrow");
        for (int i = 0; i < arrowLength; i++)
        {
            if (!gridManager.IsValidCell(current) || !gridManager.IsCellFree(current))
                break;

            path.Add(current);
            current += direction;
        }

        if (path.Count > 0)
        {
            Debug.Log("Instantiating arrow");
            GameObject arrowObj = Instantiate(arrowPrefab);
            arrowObj.name = $"Arrow_{path[0].x}_{path[0].y}";                 
        }
    }

    private Vector2Int ChooseValidDirection(Vector2Int start)
    {
        // Prefer right first, then down, then left, then up
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };

        foreach (var dir in directions)
        {
            bool valid = true;
            Vector2Int check = start;
            for (int i = 0; i < arrowLength; i++)
            {
                if (!gridManager.IsValidCell(check) || !gridManager.IsCellFree(check))
                {
                    valid = false;
                    break;
                }
                check += dir;
            }
            if (valid) return dir;
        }

        return Vector2Int.zero;
    }
}

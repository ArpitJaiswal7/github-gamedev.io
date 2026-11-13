using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewSystem
{
[RequireComponent(typeof(SpriteRenderer))]
public class ArrowUnit : MonoBehaviour
{
    public Color arrowColor = Color.white;

    // Current grid the unit is on
    public Grid currentGrid;

    // Optional: the next grid to move to
    [SerializeField] Grid targetGrid;

    // Speed of movement
    public float moveSpeed = 5f;

    // Full path of the arrow (all occupied grids)
    [SerializeField] List<Grid> occupiedPath = new List<Grid>();

    // Exit path for moving the arrow (different from occupiedPath)
    [SerializeField] List<Grid> exitPath = new List<Grid>();

    // Reference to the grid system
    private GridSystem gridManager;

    // Initialize the unit with starting grid and color
    public void Initialize(Grid startGrid, Color _color)
    {
        currentGrid = startGrid;
        targetGrid = startGrid;
        arrowColor = _color;

        if (TryGetComponent(out SpriteRenderer sp))
            sp.color = _color;

        transform.position = currentGrid.transform.position;

        // Mark as occupied
        occupiedPath.Clear();
        occupiedPath.Add(startGrid);
    }

    // Assign exit path
    public void SetExitPath(GridSystem gridSys, List<Vector2Int> gridPath)
    {
        gridManager = gridSys;

        exitPath.Clear();
        foreach (var cell in gridPath)
        {
            Grid g = gridManager.GetGridByPosition(cell);
            if (g != null)
                exitPath.Add(g);
        }
    }

    public void OnMouseDown()
    {
        MoveArrowStep();
    }

    // Move the arrow one step along its exit path
    public void MoveArrowStep()
    {
        if (exitPath == null || exitPath.Count == 0) return;

        // Compute next head grid from exitPath
        Grid nextGrid = exitPath[0];

        if (nextGrid == null || !gridManager.IsCellFree(nextGrid.gridPosition))
        {
            Debug.Log("Arrow blocked!");
            return;
        }

        // Move tail grids forward like a snake
        for (int i = occupiedPath.Count - 1; i > 0; i--)
        {
            occupiedPath[i] = occupiedPath[i - 1];
        }
        occupiedPath[0] = nextGrid;

        // Update world positions smoothly
        StartCoroutine(MoveUnitsSmooth());
    }

    // Coroutine to smoothly move unit to currentGrid
    private IEnumerator MoveUnitsSmooth()
    {
        for (int i = 0; i < occupiedPath.Count; i++)
        {
            currentGrid = occupiedPath[i];
            Vector3 targetPos = currentGrid.transform.position;

            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < 1f)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed);
                elapsed += Time.deltaTime * moveSpeed;
                yield return null;
            }

            transform.position = targetPos;
        }
    }
}
}

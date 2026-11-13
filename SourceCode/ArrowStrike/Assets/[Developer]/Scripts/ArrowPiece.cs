using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
public class ArrowPiece : MonoBehaviour, IPointerDownHandler
{
    [Header("Arrow Settings")]
    public Color arrowColor = Color.white;
    public float moveSpeed = 5f; // speed of arrow movement

    [Header("References")]
    public GridSystem gridSystem;
    public LevelManager levelManager;

    public List<Vector2Int> occupiedCells = new();
    private SpriteRenderer spriteRenderer;
    private bool isMoving = false;
    private Vector2Int moveDirection;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(GridSystem grid,List<Vector2Int> path, LevelManager manager)
    {
        if (path == null || path.Count == 0) return;

        gridSystem = grid;
        levelManager = manager;
        occupiedCells.Clear();
        occupiedCells.AddRange(path);

        foreach (var cell in occupiedCells)
            gridSystem.MarkCellOccupied(cell, gameObject);

        transform.position = GetCenterOfOccupiedCells();

        // Compute direction safely
        if (occupiedCells.Count > 1)
        {
            moveDirection = occupiedCells[^1] - occupiedCells[0];
            moveDirection = new Vector2Int(
                Mathf.Clamp(moveDirection.x, -1, 1),
                Mathf.Clamp(moveDirection.y, -1, 1)
            );

            if (moveDirection == Vector2Int.zero)
                moveDirection = Vector2Int.up; // fallback

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else moveDirection = Vector2Int.up;

        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        spriteRenderer.color = arrowColor;
    }

    private Vector3 GetCenterOfOccupiedCells()
    {
        float avgX = 0, avgY = 0;
        foreach (var cell in occupiedCells)
        {
            Vector3 worldPos = gridSystem.GetWorldPosition(cell);
            avgX += worldPos.x;
            avgY += worldPos.y;
        }
        avgX /= occupiedCells.Count;
        avgY /= occupiedCells.Count;
        return new Vector3(avgX, avgY, 0);
    }

    public List<Vector2Int> GetOccupiedCells() => occupiedCells;

    // ----------------------------- Mouse/Touch -----------------------------

    private void OnMouseDown()
    {
        if (!isMoving)
            StartCoroutine(MoveArrowSequence());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isMoving)
            StartCoroutine(MoveArrowSequence());
    }

    // --------------------------- Movement Logic ----------------------------

    private IEnumerator MoveArrowSequence()
    {
        Debug.Log("clicked on arrow");
        if (occupiedCells == null || occupiedCells.Count == 0 || gridSystem == null)
            yield break;

        // Determine direction (first → last cell)
        Vector2Int dir = occupiedCells[^1];
        if (dir == Vector2Int.zero)
            yield break;

        Debug.Log("getting arrow position");

        float moveSpeed = 4f; // adjustable speed (units per second)

        while (true)
        {
            // Compute next target position (move one step in grid direction)
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + new Vector3(dir.x * gridSystem.cellSizeX, dir.y * gridSystem.cellSizeX, 0f);

            // Safety checks to avoid NaN
            if (float.IsNaN(targetPos.x) || float.IsNaN(targetPos.y))
            {
                Debug.LogError("❌ Invalid target position (NaN). Stopping movement.");
                yield break;
            }

            Debug.Log("checking if forward cells are occupied or not");


            // Check if next cell(s) ahead are occupied
            bool blocked = false;
            foreach (var cell in occupiedCells)
            {
                Vector2Int nextCell = cell + dir;
                if (!gridSystem.GetGridByPosition(nextCell))
                {
                    blocked = false; // It's leaving the grid, not blocked
                    break;
                }
                if (gridSystem.GetGridByPosition(nextCell).isOccupied)
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
            {
                Debug.Log("Arrow blocked, stopping movement.");
                yield break;
            }

            // Move smoothly to target position
            float elapsed = 0f;
            Vector3 from = startPos;
            Vector3 to = targetPos;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Lerp(from, to, elapsed);
                yield return null;
            }

            transform.position = to;

            // Update grid occupancy (unmark old, mark new)
            List<Vector2Int> newCells = new();
            foreach (var cell in occupiedCells)
            {
                gridSystem.MarkCellFree(cell);
                Vector2Int next = cell + dir;
                if (gridSystem.GetGridByPosition(next))
                    newCells.Add(next);
            }

            occupiedCells = newCells;

            // Check if the arrow has fully left the grid
            //if (occupiedCells.Count == 0)
            //{
            //    gridSystem.UnregisterArrow(this);
            //    Destroy(gameObject);
            //    yield break;
            //}

            // Mark new occupied cells
            foreach (var cell in occupiedCells)
                gridSystem.MarkCellOccupied(cell, gameObject);

            yield return null; // small delay before next step
        }
    }


    private bool IsBlockedAhead(out GameObject blocker)
    {
        blocker = null;
        foreach (var cell in occupiedCells)
        {
            Vector2Int next = cell + moveDirection;
            //if (!gridSystem.IsInsideGrid(next))
            //    continue;

            Grid target = gridSystem.GetGridByPosition(next);
            if (target != null && target.isOccupied && target.GetOccupant() != gameObject)
            {
                blocker = target.GetOccupant();
                return true;
            }
        }
        return false;
    }

    private bool IsOutOfBounds(List<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            //if (!gridSystem.IsInsideGrid(c))
            //    return true;
        }
        return false;
    }

    private void ClearFromGrid()
    {
        foreach (var c in occupiedCells)
            gridSystem.MarkCellFree(c);
    }

    private IEnumerator MoveOutAndDestroy()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(moveDirection.x, moveDirection.y, 0) * gridSystem.cellSizeX * 2f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}

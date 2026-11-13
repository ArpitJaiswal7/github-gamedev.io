using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArrowUnit : MonoBehaviour
{
    public Color arrowColor = Color.white;

    public Grid currentGrid;

    [SerializeField] Grid targetGrid;

    public float moveSpeed = 5f;

    [SerializeField] List<Grid> occupiedPath = new List<Grid>();

    [SerializeField] List<Grid> exitPath = new List<Grid>();

    private GridSystem gridManager;

    List<ArrowUnit> unitsList = new List<ArrowUnit>();

    public bool canExitGrids = false;

    public void Initialize(Grid startGrid, Color _color)
    {
        currentGrid = startGrid;
        targetGrid = startGrid;
        arrowColor = _color;

        if (TryGetComponent(out SpriteRenderer sp))
            sp.color = _color;

        transform.position = currentGrid.transform.position;

        occupiedPath.Clear();
        occupiedPath.Add(startGrid);
    }

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
        Debug.Log("Clicked on the arrow");
        StartCoroutine(MoveArrowUntilEnd());
    }

    private IEnumerator MoveArrowUntilEnd()
    {
        canExitGrids = false;
        Arrow parentArrow = GetComponentInParent<Arrow>();
        Debug.Log("Init moveArrowUntilEnd Coroutine");
        if (parentArrow == null)
        {
            Debug.LogWarning("MoveArrowUntilEnd: parent Arrow not found.");
            yield break;
        }

        Debug.Log("parentArrow != null");
        if (exitPath.Count > 0 && exitPath.Any(x => x.isOccupied))
            yield break;

        Debug.Log("exitpath is 0 or exitpath is occupied");
        var occupied = parentArrow.GetOccupiedGrids();
        unitsList = parentArrow.GetArrowUnits();
        var gm = parentArrow.gridManager;
        var dir = parentArrow.direction;

        if (occupied == null || unitsList == null || gm == null)
        {
            Debug.LogWarning("MoveArrowUntilEnd: missing data on parent arrow.");
            yield break;
        }

        float moveDuration = 0.01f; // time to move one grid

        while (true)
        {
            if (occupied == null || occupied.Count == 0)
                yield break;

            Grid head = occupied[^1];
            Vector2Int nextPos = head.gridPosition + parentArrow.DirectionToVector2(dir);
            Grid nextHeadGrid = gm.GetGridByPosition(nextPos);

            // Free the tail *before* checking the next cell, to prevent false blocking
            gm.MarkCellFree(occupied[0].gridPosition);

            // Check occupancy properly (ignore own cells)
            if (nextHeadGrid == null ||
                (nextHeadGrid.isOccupied && nextHeadGrid.occupant != parentArrow.gameObject))
            {
                Debug.Log("Arrow stopped (end or blocked).");
                // Re-occupy the freed cell if move is invalid
                gm.MarkCellOccupied(occupied[0].gridPosition, parentArrow.gameObject);
                break;
            }

            List<Grid> newOccupied = new List<Grid>(occupied.Count);
            for (int i = 0; i < occupied.Count - 1; i++)
                newOccupied.Add(occupied[i + 1]);
            newOccupied.Add(nextHeadGrid);

            gm.MarkCellOccupied(nextHeadGrid.gridPosition, parentArrow.gameObject);

            float elapsed = 0f;
            List<Vector3> startPos = new();
            List<Vector3> endPos = new();

            for (int i = 0; i < unitsList.Count; i++)
            {
                startPos.Add(unitsList[i].transform.position);
                endPos.Add(newOccupied[i].transform.position);
            }

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                for (int i = 0; i < unitsList.Count; i++)
                    if (unitsList[i] != null)
                        unitsList[i].transform.position = Vector3.Lerp(startPos[i], endPos[i], t);

                yield return null;
            }

            for (int i = 0; i < unitsList.Count; i++)
            {
                unitsList[i].transform.position = newOccupied[i].transform.position;
                unitsList[i].currentGrid = newOccupied[i];
            }

            occupied = newOccupied;

            yield return new WaitForSeconds(0.02f);
        }

        Debug.Log("one arrowunit reached the end");

        foreach(ArrowUnit au in unitsList)
        {
            au.canExitGrids=true;
            foreach (Grid g in au.occupiedPath)
                g.FreeCell();
        }

        StartCoroutine(ClearArrowUnit());
        Debug.Log("Arrow movement complete.");
    }

    public IEnumerator ClearArrowUnit()
    {

        foreach (Grid g in occupiedPath)
            if (g != null)
                g.FreeCell();

        if (gridManager != null)
            gridManager = null;

        foreach(Grid exit_g in exitPath)
            if(exit_g != null)
                exit_g.FreeCell();

        occupiedPath.Clear();
        exitPath.Clear();

        foreach (ArrowUnit au in unitsList)
        {
            if (au != this)
                yield return StartCoroutine(au.ClearArrowUnit());
        }

        yield return new WaitForSeconds(0.1f);
        if (canExitGrids)
                Destroy(transform.parent.gameObject);

        Destroy(gameObject);
    }
}

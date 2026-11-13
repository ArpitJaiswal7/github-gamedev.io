using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LevelDifficulty
{
    Easy,
    Medium,
    Hard,
    Nightmare
}

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public GridSystem gridManager;
    public GameManager gameManager;

    [Header("Arrow Prefabs")]
    public GameObject arrowBodyPrefab;

    [Header("Level Configurations")]
    public LevelConfig easyConfig;
    public LevelConfig mediumConfig;
    public LevelConfig hardConfig;
    public LevelConfig nightmareConfig;

    [Header("Current Level Selection")]
    public LevelDifficulty currentDifficulty = LevelDifficulty.Easy;
    private LevelConfig activeConfig;

    private Transform arrowParent;
    private List<Arrow> allArrows = new();

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI currentLevelTxt;
    [SerializeField] private Button shuffleLevelBtn;

    private void Start()
    {
        shuffleLevelBtn.onClick.AddListener(() => StartCoroutine(InitRandomLevel()));
        StartCoroutine(InitRandomLevel());
    }

    private void OnDisable()
    {
        shuffleLevelBtn.onClick.RemoveAllListeners();
    }

    public IEnumerator InitRandomLevel()
    {
        shuffleLevelBtn.enabled = false;
        ClearPreviousArrows();
        arrowParent = transform.GetChild(transform.childCount - 1);
      

        // Pick random difficulty
        LevelDifficulty randomDiff = (LevelDifficulty)Random.Range(0, System.Enum.GetValues(typeof(LevelDifficulty)).Length);
        currentDifficulty = randomDiff;

        activeConfig = randomDiff switch
        {
            LevelDifficulty.Easy => easyConfig,
            LevelDifficulty.Medium => mediumConfig,
            LevelDifficulty.Hard => hardConfig,
            LevelDifficulty.Nightmare => nightmareConfig,
            _ => easyConfig
        };

        if (activeConfig == null)
        {
            Debug.LogError($"❌ No LevelConfig assigned for difficulty: {randomDiff}");
            yield break;
        }

        // Configure grid
        gridManager.gridWidth = Random.Range(activeConfig.gridWidth.min, activeConfig.gridWidth.max + 1);
        gridManager.gridHeight = Random.Range(activeConfig.gridHeight.min, activeConfig.gridHeight.max + 1);
        yield return StartCoroutine(gridManager.InitializeGrid());

        currentLevelTxt.text = $"Current Level: {randomDiff}";
        Debug.Log($"🧩 Level Configured: {randomDiff} | Grid: {gridManager.gridWidth}x{gridManager.gridHeight}");

        allArrows.Clear();

        // Generate other arrows
        yield return StartCoroutine(GenerateAllArrows(activeConfig.minNumberofArrows));
        yield return new WaitForSeconds(0.2f);
        shuffleLevelBtn.enabled = true;
    }

    private IEnumerator GenerateAllArrows(int maxCount)
    {
        int arrowsCreated = 0;

        Vector2Int center = new(gridManager.gridWidth / 2, gridManager.gridHeight / 2);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        int maxAttempts = maxCount * 20;
        int attempts = 0;

        while (arrowsCreated < maxCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int dir = directions[Random.Range(0, directions.Length)];

            int radiusX = gridManager.gridWidth / 2;
            int radiusY = gridManager.gridHeight / 2;
            Vector2Int offset = new(Random.Range(-radiusX, radiusX + 1), Random.Range(-radiusY, radiusY + 1));
            Vector2Int startCell = center + offset;

            if (!gridManager.IsValidCell(startCell) || !gridManager.IsCellFree(startCell))
                continue;

            yield return StartCoroutine(SpawnArrow(startCell, dir));
            arrowsCreated++;
            yield return new WaitForSeconds(0.01f);
        }

        if (arrowsCreated < maxCount)
            Debug.LogWarning($"Only generated {arrowsCreated} arrows out of {maxCount} due to space limitations.");        
    }

    private IEnumerator SpawnArrow(Vector2Int startCell, Vector2Int directionVec)
    {
        List<Vector2Int> path = new();
        Vector2Int current = startCell;
        int targetLength = activeConfig.GetRandomArrowLength();

        for (int i = 0; i < targetLength; i++)
        {
            if (!gridManager.IsValidCell(current) || !gridManager.IsCellFree(current))
                break;

            gridManager.MarkCellOccupied(current, gridManager.GetGridByPosition(current).gameObject);
            path.Add(current);

            if (i < targetLength - 1 && Random.value < 0.3f)
            {
                Vector2Int newDir = TryGetRandomTurn(current, directionVec);
                if (newDir != Vector2Int.zero)
                    directionVec = newDir;
            }

            current += directionVec;
            Debug.Log($"Direction : {current}");
        }

        if (path.Count == 0)
            yield break;

        Vector2Int finalDirectionVec = path.Count >= 2
            ? path[^1] - path[^2]
            : directionVec;

        ArrowDirection dirEnum = ArrowDirection.Up;
        if (finalDirectionVec == Vector2Int.down) dirEnum = ArrowDirection.Down;
        else if (finalDirectionVec == Vector2Int.left) dirEnum = ArrowDirection.Left;
        else if (finalDirectionVec == Vector2Int.right) dirEnum = ArrowDirection.Right;        

        GameObject arrowGO = new($"Arrow_{path[0].x}_{path[0].y}");
        Arrow arrow = arrowGO.AddComponent<Arrow>();
        StartCoroutine(arrow.Initialize(gridManager, path, dirEnum));
        
        allArrows.Add(arrow);
        arrowGO.transform.SetParent(arrowParent);

        Color randomArrowColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);

        foreach (Vector2Int cell in path)
        {
            if (!gridManager.IsValidCell(cell))
                continue;

            Grid grid = gridManager.GetGridByPosition(cell);
            if (grid == null)
                continue;

            Vector3 worldPos = grid.transform.position;
            GameObject body = Instantiate(arrowBodyPrefab, worldPos, Quaternion.identity, arrowGO.transform);
            body.name = $"ArrowPart_{cell.x}_{cell.y}";

            if (path[^1] == cell)
                body.transform.localScale = Vector3.one * 0.1f;

            if (body.TryGetComponent(out ArrowUnit arrowUnit))
            {
                //gridManager.MarkCellOccupied(current, body.gameObject);
                arrowUnit.Initialize(grid, randomArrowColor);
            }
        }

        yield return null;
    }



    private Vector2Int ChooseValidDirection(Vector2Int start)
    {
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        int arrowLen = activeConfig.GetRandomArrowLength();

        foreach (var dir in dirs)
        {
            bool valid = true;
            Vector2Int check = start;

            for (int i = 0; i < arrowLen; i++)
            {
                if (!gridManager.IsValidCell(check) || !gridManager.IsCellFree(check))
                {
                    valid = false;
                    break;
                }
                check += dir;
            }

            if (valid)
                return dir;
        }
        return Vector2Int.zero;
    }

    private Vector2Int TryGetRandomTurn(Vector2Int current, Vector2Int currentDir)
    {
        List<Vector2Int> dirs = new();

        if (currentDir.x != 0)
        {
            if (gridManager.IsValidCell(current + Vector2Int.up) && gridManager.IsCellFree(current + Vector2Int.up))
                dirs.Add(Vector2Int.up);
            if (gridManager.IsValidCell(current + Vector2Int.down) && gridManager.IsCellFree(current + Vector2Int.down))
                dirs.Add(Vector2Int.down);
        }
        else if (currentDir.y != 0)
        {
            if (gridManager.IsValidCell(current + Vector2Int.left) && gridManager.IsCellFree(current + Vector2Int.left))
                dirs.Add(Vector2Int.left);
            if (gridManager.IsValidCell(current + Vector2Int.right) && gridManager.IsCellFree(current + Vector2Int.right))
                dirs.Add(Vector2Int.right);
        }

        if (dirs.Count == 0)
            return Vector2Int.zero;

        return dirs[Random.Range(0, dirs.Count)];
    }

    private void ClearPreviousArrows()
    {
        if (arrowParent != null)
        {
            foreach (Transform child in arrowParent)
                if (child != null && child.TryGetComponent(out Arrow ar))
                {
                    ar.StopAllCoroutines();
                    Destroy(child.gameObject);
                }
        }

        allArrows.Clear();
        gridManager?.ClearAllCellsOccupied();
    }

    private Dictionary<int, Vector2Int> rowDirections = new();
    private Dictionary<int, Vector2Int> colDirections = new();

    private bool IsOppositeDirectionConflict(Vector2Int start, Vector2Int dir, List<Vector2Int> currentArrowPath)
    {
        // If there are at least 2 cells, decide direction based on last two
        if (currentArrowPath.Count >= 2)
        {
            Vector2Int last = currentArrowPath[^1];
            Vector2Int secondLast = currentArrowPath[^2];

            Vector2Int trend = last - secondLast; // Direction of previous movement

            // Prevent reversing direction
            if (dir == -trend)
                return true;

            // Optional: force arrow to continue along same row or column if you want
            if (trend.x != 0 && dir.y != 0) return true; // horizontal trend, can't go vertical
            if (trend.y != 0 && dir.x != 0) return true; // vertical trend, can't go horizontal

            return false;
        }

        // Fallback to previous logic if less than 2 cells
        if (dir == Vector2Int.left || dir == Vector2Int.right)
        {
            if (rowDirections.TryGetValue(start.y, out Vector2Int existingDir))
                return existingDir == -dir;
        }
        else if (dir == Vector2Int.up || dir == Vector2Int.down)
        {
            if (colDirections.TryGetValue(start.x, out Vector2Int existingDir))
                return existingDir == -dir;
        }

        // Register new direction
        if (dir.x != 0)
            rowDirections[start.y] = dir;
        else
            colDirections[start.x] = dir;

        return false;
    }
}
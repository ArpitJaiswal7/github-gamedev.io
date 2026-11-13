using System.Collections;
using System.Collections.Generic;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.EnemyGrid;
using UnityEngine;

namespace ThisIsBlast.Arsenal
{
    public class ArsenalManager : MonoBehaviour
    {
        public static ArsenalManager Instance { get; private set; }

        [Header("Layout Settings")]
        [SerializeField] private float _columnSpacing = 1.15f;
        [SerializeField] private float _visiblePanelGap = 1.35f; // Visible gap between Enemy Grid & Bench, and Bench & Player Panel
        [SerializeField] private Vector3 _benchOrigin = new Vector3(0f, -2.40f, 0f); // Shooting bench row
        [SerializeField] private Vector3 _playerPanelOrigin = new Vector3(0f, -5.05f, 0f); // Player block panel
        [SerializeField] private float _autoShootInterval = 0.20f;

        [Header("Prefabs (Optional - created procedurally if null)")]
        [SerializeField] private BenchRowTrack _benchRowTrackPrefab;
        [SerializeField] private BenchCellSlot _benchCellSlotPrefab;

        private readonly List<ArsenalColumn> _columns = new List<ArsenalColumn>();
        private readonly List<BenchCellSlot> _slots = new List<BenchCellSlot>();
        private readonly List<GameObject> _arsenalDecorObjects = new List<GameObject>();
        private BenchRowTrack _benchRowTrack;
        private float _autoShootTimer = 0f;
        private float _currentDashedLineY = -3.75f;

        public int TotalColumns => _columns.Count;
        public IReadOnlyList<BenchCellSlot> BenchSlots => _slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_benchRowTrackPrefab == null)
            {
                _benchRowTrackPrefab = Resources.Load<BenchRowTrack>("Prefabs/Arsenal/BenchRowTrack");
            }
            if (_benchCellSlotPrefab == null)
            {
                _benchCellSlotPrefab = Resources.Load<BenchCellSlot>("Prefabs/Arsenal/BenchCellSlot");
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // 1. Process player click/tap input
            if (Input.GetMouseButtonDown(0))
            {
                HandlePlayerClick(Input.mousePosition);
            }

            // 2. Continuous shooting for blocks currently docked on the shooting bench
            _autoShootTimer += Time.deltaTime;
            if (_autoShootTimer >= _autoShootInterval)
            {
                _autoShootTimer = 0f;
                for (int i = 0; i < _slots.Count; i++)
                {
                    var slot = _slots[i];
                    if (slot != null && slot.HasBlock && !slot.CurrentBlock.IsMoving)
                    {
                        slot.CurrentBlock.TryShoot();
                    }
                }
            }
        }

        private void HandlePlayerClick(Vector3 screenPos)
        {
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(screenPos) : default;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var block = hit.collider.GetComponentInParent<ArsenalBlock>();
                if (block != null)
                {
                    if (block.IsOnBench)
                    {
                        // Tapped a block on the shooting bench -> manual shot!
                        block.TryShoot();
                        return;
                    }

                    // Tapped a block in the Player Block Panel -> Try pick and move to bench!
                    var col = block.GetComponentInParent<ArsenalColumn>();
                    if (col != null)
                    {
                        col.TryPickBlock(block);
                        return;
                    }
                }

                var column = hit.collider.GetComponentInParent<ArsenalColumn>();
                if (column != null)
                {
                    column.TryPickBlock(column.FrontBlock);
                    return;
                }
            }

            // Fallback: If clicked in the lower player block panel area, map to closest column
            if (Camera.main != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                if (worldPos.y <= _currentDashedLineY + 0.2f) // In Player Block Panel
                {
                    ArsenalColumn closestCol = null;
                    float minXDist = float.MaxValue;

                    foreach (var c in _columns)
                    {
                        float dist = Mathf.Abs(c.transform.position.x - worldPos.x);
                        if (dist < minXDist)
                        {
                            minXDist = dist;
                            closestCol = c;
                        }
                    }

                    if (closestCol != null && minXDist < _columnSpacing * 0.85f)
                    {
                        closestCol.TryPickBlock(closestCol.FrontBlock);
                    }
                }
            }
        }

        public void BuildArsenal(LevelData levelData)
        {
            ClearArsenal();

            int colCount = levelData.arsenalColumns.Count;
            float totalWidth = (colCount - 1) * _columnSpacing;

            // 0. Compute Dynamic Non-Overlapping Layout with Equal Visible Gaps
            // Visible gap between Enemy Grid & Shooting Bench == Visible gap between Shooting Bench & Player Block Panel
            float enemyGridBottom = EnemyGridManager.Instance != null ? EnemyGridManager.Instance.GridTrayBottomY : -0.35f;

            float benchW = totalWidth + _columnSpacing * 1.35f;
            float benchH = 1.30f;
            float benchTotalH = benchH + 0.18f; // including Bench_TrayRim border frame
            float benchHalfH = benchTotalH * 0.5f;

            // Bench center: positioned exactly _visiblePanelGap below the bottom edge of Enemy Grid
            float benchCenterY = enemyGridBottom - _visiblePanelGap - benchHalfH;
            _benchOrigin = new Vector3(_benchOrigin.x, benchCenterY, 0f);

            // Bottom edge of Shooting Bench
            float benchBottom = benchCenterY - benchHalfH;

            // Player Block Panel top edge: positioned exactly _visiblePanelGap below the bottom edge of Shooting Bench
            float playerPanelTop = benchBottom - _visiblePanelGap;

            // Visual Dashed Line Separator sits right at the midpoint of this gap
            _currentDashedLineY = benchBottom - (_visiblePanelGap * 0.5f);

            // The front block in each column sits neatly inside the top of the player panel
            float frontBlockY = playerPanelTop - 0.62f;
            _playerPanelOrigin = new Vector3(_benchOrigin.x, frontBlockY, 0f);

            // 1. Shooting Bench Row Parent Container (BenchRowTrack)
            var benchRowGo = new GameObject("Bench_RowTrack");
            benchRowGo.transform.SetParent(transform);
            benchRowGo.transform.position = _benchOrigin;

            _benchRowTrack = benchRowGo.AddComponent<BenchRowTrack>();
            _benchRowTrack.Initialize(benchW, benchH);

            // 2. Spawn 5 BenchCellSlots on the Shooting Bench (INITIALIZED EMPTY - NO BLOCKS AT START!)
            Vector3 startBenchPos = _benchOrigin - new Vector3(totalWidth * 0.5f, 0, 0);
            for (int i = 0; i < colCount; i++)
            {
                Vector3 slotPos = startBenchPos + new Vector3(i * _columnSpacing, 0, 0);

                var slotGo = new GameObject($"BenchCellSlot_{i}");
                slotGo.transform.position = slotPos;

                var slotComp = slotGo.AddComponent<BenchCellSlot>();
                slotComp.Initialize(i, 1.08f);

                _benchRowTrack.AddSlot(slotComp);
                _slots.Add(slotComp);
            }

            // 3. Visual Dashed Line Separator between Shooting Bench and Player Block Panel (Equidistant in Gap)
            float dashSpan = totalWidth + _columnSpacing * 1.15f;
            int dashCount = 15;
            float dashStep = dashSpan / (dashCount - 1);
            float dashStart = _benchOrigin.x - dashSpan * 0.5f;

            var dashedParent = new GameObject("Bench_Queue_DashedLine");
            dashedParent.transform.SetParent(transform);
            _arsenalDecorObjects.Add(dashedParent);

            var dashMat = MaterialHelper.CreateStandardMaterial(new Color(0.70f, 0.85f, 1.0f, 0.85f), 0.5f);

            for (int d = 0; d < dashCount; d++)
            {
                float dx = dashStart + d * dashStep;
                var dashGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dashGo.name = $"Dash_{d}";
                dashGo.transform.SetParent(dashedParent.transform);
                dashGo.transform.position = new Vector3(dx, _currentDashedLineY, 0.16f);
                dashGo.transform.localScale = new Vector3(dashStep * 0.55f, 0.045f, 0.02f);
                dashGo.GetComponent<MeshRenderer>().material = dashMat;
                Destroy(dashGo.GetComponent<Collider>());
            }

            // 4. Player Blocks Queue Panel (Positioned STRICTLY below shooting bench with equal gap, NEVER overlapping)
            float queuePanelW = benchW;
            float queuePanelH = 5.4f;
            // The top of the queue tray begins exactly at playerPanelTop, extending downwards
            Vector3 queuePanelCenter = new Vector3(_benchOrigin.x, playerPanelTop - (queuePanelH * 0.5f), 0.24f);

            var queueTrayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            queueTrayGo.name = "Queue_BackgroundTray";
            queueTrayGo.transform.SetParent(transform);
            queueTrayGo.transform.position = queuePanelCenter;
            queueTrayGo.transform.localScale = new Vector3(queuePanelW, queuePanelH, 0.10f);

            var queueTrayMat = MaterialHelper.CreateStandardMaterial(new Color(0.045f, 0.06f, 0.085f), 0.35f);
            queueTrayGo.GetComponent<MeshRenderer>().material = queueTrayMat;
            Destroy(queueTrayGo.GetComponent<Collider>());
            _arsenalDecorObjects.Add(queueTrayGo);

            var queueRimGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            queueRimGo.name = "Queue_TrayRim";
            queueRimGo.transform.SetParent(transform);
            queueRimGo.transform.position = queuePanelCenter + new Vector3(0, 0, 0.04f);
            queueRimGo.transform.localScale = new Vector3(queuePanelW + 0.16f, queuePanelH, 0.08f);

            var queueRimMat = new Material(queueTrayMat);
            queueRimMat.color = new Color(0.14f, 0.18f, 0.25f);
            queueRimGo.GetComponent<MeshRenderer>().material = queueRimMat;
            Destroy(queueRimGo.GetComponent<Collider>());
            _arsenalDecorObjects.Add(queueRimGo);

            // 5. Spawn the Arsenal Columns in the Player Block Panel (Queue Stacks)
            Vector3 startPlayerPos = _playerPanelOrigin - new Vector3(totalWidth * 0.5f, 0, 0);
            for (int i = 0; i < colCount; i++)
            {
                var colData = levelData.arsenalColumns[i];
                Vector3 colPos = startPlayerPos + new Vector3(i * _columnSpacing, 0, 0);

                // Column queue lane track inside the player panel (stays strictly below playerPanelTop)
                float queueTrackH = queuePanelH - 0.2f;
                var queueTrack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                queueTrack.name = $"ColumnQueueTrack_{i}";
                queueTrack.transform.SetParent(transform);
                queueTrack.transform.position = new Vector3(colPos.x, playerPanelTop - (queueTrackH * 0.5f) - 0.08f, 0.18f);
                queueTrack.transform.localScale = new Vector3(1.05f, queueTrackH, 0.04f);

                var queueMat = new Material(queueTrayMat);
                queueMat.color = new Color(0.035f, 0.045f, 0.065f);
                queueTrack.GetComponent<MeshRenderer>().material = queueMat;
                Destroy(queueTrack.GetComponent<Collider>());
                _arsenalDecorObjects.Add(queueTrack);

                // Arsenal column stack
                var colGo = new GameObject($"ArsenalColumn_{i}");
                colGo.transform.SetParent(transform);
                colGo.transform.position = colPos;

                var col = colGo.AddComponent<ArsenalColumn>();
                col.Initialize(colData);
                _columns.Add(col);
            }

            Debug.Log($"[ArsenalManager] Built {_columns.Count} queue columns strictly below bench (benchY={_benchOrigin.y:F2}, playerTop={playerPanelTop:F2}, gap={_visiblePanelGap:F2}).");
            // NOTE: Bench starts completely empty! NO merge check is called on startup.
        }

        public void ClearArsenal()
        {
            foreach (var col in _columns)
            {
                if (col != null) Destroy(col.gameObject);
            }
            _columns.Clear();

            foreach (var slot in _slots)
            {
                if (slot != null && slot.CurrentBlock != null)
                {
                    Destroy(slot.CurrentBlock.gameObject);
                }
            }
            _slots.Clear();

            if (_benchRowTrack != null)
            {
                Destroy(_benchRowTrack.gameObject);
                _benchRowTrack = null;
            }

            foreach (var go in _arsenalDecorObjects)
            {
                if (go != null) Destroy(go);
            }
            _arsenalDecorObjects.Clear();
        }

        public BenchCellSlot GetFirstEmptyBenchSlot()
        {
            foreach (var slot in _slots)
            {
                if (slot != null && slot.IsAvailable)
                {
                    return slot;
                }
            }
            return null;
        }

        public List<ArsenalBlock> GetActiveBenchBlocks()
        {
            var list = new List<ArsenalBlock>();
            foreach (var slot in _slots)
            {
                if (slot != null && slot.CurrentBlock != null && !slot.CurrentBlock.IsMoving)
                {
                    list.Add(slot.CurrentBlock);
                }
            }
            return list;
        }

        public void TriggerMergeCheck()
        {
            // Merging ONLY happens on the shooting bench row
            ArsenalMergeController.Instance?.CheckMerges();
        }

        public void UnlockColumn(int colIndex)
        {
            if (colIndex >= 0 && colIndex < _columns.Count)
            {
                _columns[colIndex].Unlock();
            }
        }

        public Vector3 GetColumnTopPosition(int colIndex)
        {
            if (colIndex >= 0 && colIndex < _columns.Count && _columns[colIndex] != null)
            {
                var col = _columns[colIndex];
                if (col.FrontBlock != null)
                {
                    return col.FrontBlock.transform.position;
                }
                return col.transform.position;
            }
            return new Vector3(0, -3.5f, 0);
        }

        public List<ArsenalBlock> GetAllArsenalBlocks()
        {
            var list = new List<ArsenalBlock>();
            // Add bench blocks
            list.AddRange(GetActiveBenchBlocks());
            // Add player queue blocks
            foreach (var col in _columns)
            {
                if (col != null)
                {
                    list.AddRange(col.GetAllBlocks());
                }
            }
            return list;
        }

        private Coroutine _failCheckCoroutine;

        public void CheckFailCondition()
        {
            if (_failCheckCoroutine != null) StopCoroutine(_failCheckCoroutine);
            _failCheckCoroutine = StartCoroutine(CheckFailRoutine());
        }

        private IEnumerator CheckFailRoutine()
        {
            // 1. Wait while projectiles are still flying in the air!
            float timeout = 2.5f;
            float elapsed = 0f;
            while (Combat.ProjectilePool.Instance != null && Combat.ProjectilePool.Instance.ActiveProjectilesCount > 0 && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 2. Short grace period for death animations & cascades to register
            yield return new WaitForSeconds(0.35f);

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                _failCheckCoroutine = null;
                yield break;
            }

            // 3. Check if all enemies were cleared!
            int remainingEnemies = EnemyGridManager.Instance ? EnemyGridManager.Instance.RemainingBlocksCount : 0;
            if (remainingEnemies <= 0)
            {
                Debug.Log("<color=green>[ArsenalManager] In-flight bullets finished off enemies! TRIGGERING VICTORY!</color>");
                GameManager.Instance?.CheckWinCondition();
                _failCheckCoroutine = null;
                yield break;
            }

            // 4. Count remaining ammo on bench & player columns
            int totalRemainingAmmo = 0;
            foreach (var slot in _slots)
            {
                if (slot != null && slot.CurrentBlock != null && !slot.CurrentBlock.IsMoving)
                {
                    totalRemainingAmmo += slot.CurrentBlock.BulletCount;
                }
            }

            foreach (var col in _columns)
            {
                if (col == null || col.IsLocked) continue;
                foreach (var b in col.GetAllBlocks())
                {
                    if (b != null) totalRemainingAmmo += b.BulletCount;
                }
            }

            // 5. Only fail if truly 0 ammo AND enemies still alive
            if (totalRemainingAmmo <= 0)
            {
                // Re-verify remaining enemies count one final time
                remainingEnemies = EnemyGridManager.Instance ? EnemyGridManager.Instance.RemainingBlocksCount : 0;
                if (remainingEnemies > 0)
                {
                    Debug.Log("<color=red>[ArsenalManager] Out of ammo and enemies still remain! FAIL.</color>");
                    GameManager.Instance?.TriggerDefeat("Out of Ammo!");
                }
                else
                {
                    GameManager.Instance?.CheckWinCondition();
                }
            }

            _failCheckCoroutine = null;
        }

        public void HighlightBenchRow(Color color, float duration = 0.3f)
        {
            _benchRowTrack?.HighlightRow(color, duration);
        }

        public void HighlightBenchSlot(int columnIndex, Color color, float duration = 0.25f)
        {
            if (columnIndex >= 0 && columnIndex < _slots.Count)
            {
                _slots[columnIndex]?.Highlight(color, duration);
            }
        }
    }
}

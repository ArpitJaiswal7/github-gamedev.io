using System.Collections;
using System.Collections.Generic;
using ThisIsBlast.Combat;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using UnityEngine;

namespace ThisIsBlast.EnemyGrid
{
    public class EnemyGridManager : MonoBehaviour
    {
        public static EnemyGridManager Instance { get; private set; }

        [Header("Grid Layout Settings")]
        [SerializeField] private float _cellSize = 0.68f;
        [SerializeField] private float _cellSpacing = 0.14f;
        [SerializeField] private float _blockScaleRatio = 0.82f; // Makes blocks smaller than grid to reveal background grid
        [SerializeField] private Vector3 _gridCenterOrigin = new Vector3(0f, 2.75f, 0f); // Shifted upward considerably

        private readonly Dictionary<Vector3Int, EnemyBlock> _activeBlocks = new Dictionary<Vector3Int, EnemyBlock>();
        private readonly HashSet<EnemyBlock> _livingBlocks = new HashSet<EnemyBlock>();
        private readonly Dictionary<int, GridColumnTrack> _columnTracks = new Dictionary<int, GridColumnTrack>();
        private readonly Dictionary<int, GridRowTrack> _rowTracks = new Dictionary<int, GridRowTrack>();
        private readonly Dictionary<Vector2Int, GridCellSlot> _cellSlots = new Dictionary<Vector2Int, GridCellSlot>();
        private readonly List<GameObject> _backgroundDecorObjects = new List<GameObject>();
        private LevelData _currentLevelData;

        public int RemainingBlocksCount
        {
            get
            {
                _livingBlocks.RemoveWhere(b => b == null || b.IsDestroyed || b.CurrentHp <= 0);
                return _livingBlocks.Count;
            }
        }
        public float CellStep => _cellSize + _cellSpacing;

        public float GridTrayBottomY
        {
            get
            {
                int h = _currentLevelData != null ? _currentLevelData.gridHeight : 7;
                float totalHeight = (h - 1) * CellStep;
                float trayH = totalHeight + CellStep * 1.35f + 0.22f;
                return _gridCenterOrigin.y - (trayH * 0.5f);
            }
        }

        public GridColumnTrack GetColumnTrack(int colIndex) => _columnTracks.TryGetValue(colIndex, out var t) ? t : null;
        public GridRowTrack GetRowTrack(int rowIndex) => _rowTracks.TryGetValue(rowIndex, out var r) ? r : null;
        public GridCellSlot GetCellSlot(int x, int y) => _cellSlots.TryGetValue(new Vector2Int(x, y), out var s) ? s : null;

        public void HighlightRow(int rowIndex, Color color, float duration = 0.25f)
        {
            if (_rowTracks.TryGetValue(rowIndex, out var rowTrack))
            {
                rowTrack.HighlightRow(color, duration);
            }
        }

        public void HighlightColumn(int colIndex, Color color, float duration = 0.2f)
        {
            if (_columnTracks.TryGetValue(colIndex, out var colTrack))
            {
                colTrack.HighlightColumn(color, duration);
            }
        }

        public void HighlightCell(int x, int y, Color color, float duration = 0.25f)
        {
            if (_cellSlots.TryGetValue(new Vector2Int(x, y), out var slot))
            {
                slot.Highlight(color, duration);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnBlockDestroyed += HandleBlockDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnBlockDestroyed -= HandleBlockDestroyed;
        }

        public void ClearGrid()
        {
            foreach (var kvp in _activeBlocks)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _activeBlocks.Clear();

            foreach (var kvp in _cellSlots)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _cellSlots.Clear();

            foreach (var kvp in _columnTracks)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _columnTracks.Clear();

            foreach (var kvp in _rowTracks)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _rowTracks.Clear();

            foreach (var go in _backgroundDecorObjects)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _backgroundDecorObjects.Clear();
        }

        public void BuildGrid(LevelData levelData)
        {
            ClearGrid();
            _currentLevelData = levelData;

            int width = levelData.gridWidth;
            int height = levelData.gridHeight;

            // Dynamically scale cell size to fit cleanly within mobile screen width
            float targetGridWidth = 5.8f;
            _cellSize = Mathf.Min(0.72f, (targetGridWidth / Mathf.Max(1, width)) * 0.88f);
            _cellSpacing = _cellSize * 0.18f; // Increased spacing between grids

            // Compute center offset so enemy grid is nicely centered horizontally
            float totalWidth = (width - 1) * CellStep;
            float totalHeight = (height - 1) * CellStep;

            Vector3 baseOffset = _gridCenterOrigin - new Vector3(totalWidth * 0.5f, -totalHeight * 0.5f, 0f);

            // 1. Full Enemy Grid Container Tray (Dark background frame)
            var trayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trayGo.name = "Grid_BackgroundTray";
            trayGo.transform.SetParent(transform);
            float trayW = totalWidth + CellStep * 1.35f;
            float trayH = totalHeight + CellStep * 1.35f;
            trayGo.transform.position = _gridCenterOrigin + new Vector3(0, 0, 0.28f);
            trayGo.transform.localScale = new Vector3(trayW, trayH, 0.15f);

            var trayMat = MaterialHelper.CreateStandardMaterial(new Color(0.06f, 0.08f, 0.12f), 0.4f);
            trayGo.GetComponent<MeshRenderer>().material = trayMat;
            Destroy(trayGo.GetComponent<Collider>());
            _backgroundDecorObjects.Add(trayGo);

            // Tray Rim Border (Outer beveled frame)
            var rimGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rimGo.name = "Grid_TrayRim";
            rimGo.transform.SetParent(transform);
            rimGo.transform.position = _gridCenterOrigin + new Vector3(0, 0, 0.32f);
            rimGo.transform.localScale = new Vector3(trayW + 0.22f, trayH + 0.22f, 0.12f);

            var rimMat = new Material(trayMat);
            rimMat.color = new Color(0.18f, 0.22f, 0.30f);
            rimGo.GetComponent<MeshRenderer>().material = rimMat;
            Destroy(rimGo.GetComponent<Collider>());
            _backgroundDecorObjects.Add(rimGo);

            // 2. Spawn Grid Row Tracks (Parent containers for each horizontal row of cells)
            for (int row = 0; row < height; row++)
            {
                float rowY = baseOffset.y - row * CellStep;
                var rowTrackGo = new GameObject($"RowTrack_{row}");
                rowTrackGo.transform.SetParent(transform);
                rowTrackGo.transform.position = new Vector3(_gridCenterOrigin.x, rowY, 0f);

                var rowComp = rowTrackGo.AddComponent<GridRowTrack>();
                rowComp.Initialize(row, totalWidth + CellStep * 1.05f, _cellSize, _gridCenterOrigin.x);
                _rowTracks[row] = rowComp;
            }

            // 3. Spawn Grid Column Tracks (Lane overlays for each vertical column)
            for (int col = 0; col < width; col++)
            {
                float colX = baseOffset.x + col * CellStep;
                var colTrackGo = new GameObject($"ColumnTrack_{col}");
                colTrackGo.transform.SetParent(transform);
                colTrackGo.transform.position = new Vector3(colX, 0, 0);

                var trackComp = colTrackGo.AddComponent<GridColumnTrack>();
                trackComp.Initialize(col, CellStep * 0.96f, totalHeight + CellStep * 1.15f, _gridCenterOrigin.y);
                _columnTracks[col] = trackComp;
            }

            // 4. Spawn Grid Cell Slots (placed under their respective RowTrack parents)
            for (int col = 0; col < width; col++)
            {
                for (int row = 0; row < height; row++)
                {
                    Vector3 slotPos = GetWorldPos(col, row, 0, baseOffset);

                    var slotGo = new GameObject($"CellSlot_{col}_{row}");
                    slotGo.transform.position = slotPos;

                    var slotComp = slotGo.AddComponent<GridCellSlot>();
                    slotComp.Initialize(col, row, _cellSize);

                    _rowTracks[row].AddSlot(slotComp);
                    _columnTracks[col].AddSlot(slotComp);
                    _cellSlots[new Vector2Int(col, row)] = slotComp;
                }
            }

            // 5. Spawn Active Enemy Blocks inside their parent Cell Slots
            foreach (var blockData in levelData.enemyBlocks)
            {
                var slotKey = new Vector2Int(blockData.x, blockData.y);
                if (_cellSlots.TryGetValue(slotKey, out var slot))
                {
                    var blockGo = new GameObject($"EnemyBlock_{blockData.x}_{blockData.y}");
                    blockGo.transform.SetParent(slot.transform);
                    blockGo.transform.position = slot.transform.position;
                    blockGo.transform.localScale = Vector3.one * (_cellSize * _blockScaleRatio);

                    var blockComp = blockGo.AddComponent<EnemyBlock>();
                    blockComp.Initialize(blockData);

                    slot.SetBlock(blockComp);
                    _activeBlocks[new Vector3Int(blockData.x, blockData.y, blockData.z)] = blockComp;
                    _livingBlocks.Add(blockComp);
                }
            }

            Debug.Log($"[EnemyGrid] Built grid with {_columnTracks.Count} column tracks, {_cellSlots.Count} cell slots, and {_activeBlocks.Count} blocks.");
        }

        public Vector3 GetWorldPos(int x, int y, int z, Vector3 baseOffset)
        {
            return baseOffset + new Vector3(x * CellStep, -y * CellStep, z * CellStep);
        }

        public Vector3 GetWorldPosForCoords(int x, int y, int z = 0)
        {
            if (_currentLevelData == null) return Vector3.zero;
            float totalWidth = (_currentLevelData.gridWidth - 1) * CellStep;
            float totalHeight = (_currentLevelData.gridHeight - 1) * CellStep;
            Vector3 baseOffset = _gridCenterOrigin - new Vector3(totalWidth * 0.5f, -totalHeight * 0.5f, 0f);
            return GetWorldPos(x, y, z, baseOffset);
        }

        /// <summary>
        /// Returns the frontmost living block in the specified column (the "peek" block of the column stack
        /// pointing toward the player panel, nearest to us).
        /// Returns null if all blocks in this column are destroyed.
        /// </summary>
        public EnemyBlock GetFrontBlockOfColumn(int colIndex)
        {
            if (_activeBlocks.Count == 0) return null;

            EnemyBlock frontBlock = null;
            int maxY = -1;

            foreach (var kvp in _activeBlocks)
            {
                var block = kvp.Value;
                if (block == null || block.IsDestroyed) continue;
                if (block.Data.x == colIndex)
                {
                    // In grid coords, larger Y = closer to the player panel (front of the stack)
                    if (block.Data.y > maxY)
                    {
                        maxY = block.Data.y;
                        frontBlock = block;
                    }
                }
            }

            return frontBlock;
        }

        /// <summary>
        /// Finds the closest exposed front grid block that matches the requested color.
        /// STACK POP RULE:
        /// - Each enemy column functions strictly as a stack pointing towards the player panel.
        /// - Only the front grid block (the peek of the column stack, nearest to us) can be shot.
        /// - Blocks behind the front block CANNOT be shot or targeted under any circumstance.
        /// - If a column's front block has a different color, that column is blocked for this shooter.
        /// - Only if the front block of a column matches 'color' and can take damage is it a valid target.
        /// </summary>
        public EnemyBlock GetLowestBlockForColor(BlockColor color, float firingWorldX)
        {
            if (_activeBlocks.Count == 0 || color == BlockColor.None) return null;

            int gridWidth = _currentLevelData != null ? _currentLevelData.gridWidth : 10;

            EnemyBlock bestTarget = null;
            float minXDist = float.MaxValue;
            bool foundHealthyTarget = false;

            for (int col = 0; col < gridWidth; col++)
            {
                // 1. Get ONLY the frontmost grid block of this column (the peek of this column stack)
                EnemyBlock frontBlock = GetFrontBlockOfColumn(col);
                if (frontBlock == null || frontBlock.IsDestroyed) continue;

                // 2. Can it take damage? (e.g. not locked)
                if (!frontBlock.CanTakeDamage()) continue;

                // 3. STRICT SAME COLOR CHECK:
                // If this front block is of a different color, this entire column is blocked for this shooter!
                // Blocks behind it in this column CANNOT be shot!
                if (frontBlock.Data.color != color) continue;

                // 4. Check effective HP (to avoid overkilling dying targets when multiple targets exist)
                bool hasEffectiveHp = frontBlock.EffectiveHp > 0;

                if (!hasEffectiveHp && foundHealthyTarget) continue;

                float dist = Mathf.Abs(frontBlock.transform.position.x - firingWorldX);

                if (hasEffectiveHp && !foundHealthyTarget)
                {
                    foundHealthyTarget = true;
                    bestTarget = frontBlock;
                    minXDist = dist;
                }
                else
                {
                    if (dist < minXDist)
                    {
                        minXDist = dist;
                        bestTarget = frontBlock;
                    }
                }
            }

            return bestTarget;
        }

        public EnemyBlock GetLowestBlockForWorldX(float firingWorldX)
        {
            if (_activeBlocks.Count == 0) return null;

            // Find block with minimum distance to firingWorldX
            float minXDist = float.MaxValue;
            foreach (var kvp in _activeBlocks)
            {
                var block = kvp.Value;
                if (block == null || block.IsDestroyed) continue;

                float dist = Mathf.Abs(block.transform.position.x - firingWorldX);
                if (dist < minXDist)
                {
                    minXDist = dist;
                }
            }

            // Among blocks close to that X (within half cellStep), find the one with lowest Y (closest to player)
            EnemyBlock lowestBlock = null;
            float lowestY = float.MaxValue;

            foreach (var kvp in _activeBlocks)
            {
                var block = kvp.Value;
                if (block == null || block.IsDestroyed) continue;

                float dist = Mathf.Abs(block.transform.position.x - firingWorldX);
                if (dist <= minXDist + 0.15f)
                {
                    if (block.transform.position.y < lowestY)
                    {
                        lowestY = block.transform.position.y;
                        lowestBlock = block;
                    }
                }
            }

            return lowestBlock;
        }

        public EnemyBlock GetAnyLowestBlock()
        {
            EnemyBlock lowest = null;
            float lowestY = float.MaxValue;
            foreach (var kvp in _activeBlocks)
            {
                var b = kvp.Value;
                if (b == null || b.IsDestroyed) continue;
                if (b.transform.position.y < lowestY)
                {
                    lowestY = b.transform.position.y;
                    lowest = b;
                }
            }
            return lowest;
        }

        public void TriggerColorBlastCascade(EnemyBlock originBlock, BlockColor color)
        {
            StartCoroutine(DoColorBlastCascade(originBlock, color));
        }

        private IEnumerator DoColorBlastCascade(EnemyBlock originBlock, BlockColor color)
        {
            if (originBlock == null) yield break;

            int ox = originBlock.Data.x;
            int oy = originBlock.Data.y;
            int oz = originBlock.Data.z;

            Vector3Int[] neighborOffsets = new Vector3Int[]
            {
                new Vector3Int(-1, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(0, 1, 0)
            };

            List<EnemyBlock> matchedNeighbors = new List<EnemyBlock>();
            foreach (var offset in neighborOffsets)
            {
                var key = new Vector3Int(ox + offset.x, oy + offset.y, oz + offset.z);
                if (_activeBlocks.TryGetValue(key, out var neighbor))
                {
                    if (neighbor != null && !neighbor.IsDestroyed && neighbor.Data.color == color && neighbor.Data.type == EnemyBlockType.Standard)
                    {
                        matchedNeighbors.Add(neighbor);
                    }
                }
            }

            if (matchedNeighbors.Count > 0)
            {
                Color blastFxColor = BlockColorPalette.GetColor(color);
                FloatingText.Create(originBlock.transform.position, "COLOR BLAST! 💥", blastFxColor, 0.4f);
                CameraShaker.Instance?.Shake(0.12f, 0.16f);
                HighlightRow(oy, blastFxColor, 0.35f);
                HighlightColumn(ox, blastFxColor, 0.35f);

                foreach (var nb in matchedNeighbors)
                {
                    yield return new WaitForSeconds(0.04f);
                    if (nb != null && !nb.IsDestroyed)
                    {
                        HighlightCell(nb.Data.x, nb.Data.y, blastFxColor, 0.3f);
                        HighlightRow(nb.Data.y, blastFxColor, 0.25f);
                        HighlightColumn(nb.Data.x, blastFxColor, 0.25f);
                        nb.TakeHit(1, color);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the lowest (frontmost) living enemy block in column x.
        /// If mapped from 5 arsenal columns to N enemy columns, maps proportion or direct lane.
        /// </summary>
        public EnemyBlock GetLowestBlockInColumn(int arsenalColIndex, int totalArsenalCols)
        {
            if (_currentLevelData == null || _activeBlocks.Count == 0) return null;

            // Map arsenal column to enemy grid column range
            float normalizedCol = (float)arsenalColIndex / Mathf.Max(1, totalArsenalCols - 1);
            int targetEnemyCol = Mathf.RoundToInt(normalizedCol * (_currentLevelData.gridWidth - 1));

            // Look for blocks in that column with maximum y (lowest on screen)
            EnemyBlock lowestBlock = null;
            int maxY = -1;

            foreach (var kvp in _activeBlocks)
            {
                var block = kvp.Value;
                if (block == null || block.IsDestroyed) continue;

                if (block.Data.x == targetEnemyCol)
                {
                    if (block.Data.y > maxY)
                    {
                        maxY = block.Data.y;
                        lowestBlock = block;
                    }
                }
            }

            // Fallback: If exact column is cleared, find closest column with remaining blocks
            if (lowestBlock == null)
            {
                int minColDist = int.MaxValue;
                foreach (var kvp in _activeBlocks)
                {
                    var block = kvp.Value;
                    if (block == null || block.IsDestroyed) continue;

                    int dist = Mathf.Abs(block.Data.x - targetEnemyCol);
                    if (dist < minColDist)
                    {
                        minColDist = dist;
                        lowestBlock = block;
                        maxY = block.Data.y;
                    }
                    else if (dist == minColDist && block.Data.y > maxY)
                    {
                        lowestBlock = block;
                        maxY = block.Data.y;
                    }
                }
            }

            return lowestBlock;
        }

        public void CheckInterlockedDestruction(EnemyBlock block)
        {
            int px = block.Data.partnerX;
            int py = block.Data.partnerY;
            var partnerKey = new Vector3Int(px, py, block.Data.z);

            if (_activeBlocks.TryGetValue(partnerKey, out var partnerBlock))
            {
                if (partnerBlock != null && partnerBlock.CurrentHp <= 0)
                {
                    // Both partners are at 0 HP -> Destroy both together!
                    FloatingText.Create(block.transform.position, "UNLINKED!", Color.cyan);
                    block.DestroyInterlockedFinal();
                    partnerBlock.DestroyInterlockedFinal();
                }
                else
                {
                    FloatingText.Create(block.transform.position, "WAITING FOR PARTNER!", Color.yellow, 0.28f);
                    block.UpdateVisuals();
                }
            }
            else
            {
                // Partner already gone
                block.DestroyInterlockedFinal();
            }
        }

        public void SpawnKeyDropProjectile(Vector3 startPos, int targetColIndex)
        {
            StartCoroutine(AnimateKeyDrop(startPos, targetColIndex));
        }

        private IEnumerator AnimateKeyDrop(Vector3 startPos, int targetColIndex)
        {
            var keyGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            keyGo.name = "KeyDrop_Projectile";
            keyGo.transform.position = startPos;
            keyGo.transform.localScale = Vector3.one * 0.4f;

            var r = keyGo.GetComponent<MeshRenderer>();
            r.material = MaterialHelper.CreateStandardMaterial(BlockColorPalette.ChestGold, 0.6f);

            Vector3 targetPos = Arsenal.ArsenalManager.Instance ? 
                Arsenal.ArsenalManager.Instance.GetColumnTopPosition(targetColIndex) : 
                new Vector3(0, -3.5f, 0);

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Parabolic arc downwards
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * 1.5f;

                keyGo.transform.position = currentPos;
                keyGo.transform.Rotate(Vector3.up * 360f * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(keyGo);

            // Unlock column!
            SoundManager.Instance?.PlayUnlock();
            FloatingText.Create(targetPos, "COLUMN UNLOCKED! 🔓", Color.green, 0.4f);
            GameEvents.TriggerKeyCollected(0, 0, targetColIndex);
            Arsenal.ArsenalManager.Instance?.UnlockColumn(targetColIndex);
        }

        public void UnregisterBlock(EnemyBlock block)
        {
            if (block != null)
            {
                _livingBlocks.Remove(block);
            }
        }

        public void CheckRemainingBlocksAfterFlight()
        {
            if (RemainingBlocksCount <= 0)
            {
                Debug.Log("<color=green>[EnemyGrid] In-flight projectiles landed and all blocks dead! Checking win condition.</color>");
                GameManager.Instance?.CheckWinCondition();
            }
        }

        private void HandleBlockDestroyed(int x, int y, BlockColor color, EnemyBlockType type)
        {
            // Remove matching block from dictionary and purge any dead references
            List<Vector3Int> keysToRemove = new List<Vector3Int>();
            foreach (var kvp in _activeBlocks)
            {
                if (kvp.Value == null || kvp.Value.IsDestroyed || kvp.Value.CurrentHp <= 0 || (kvp.Value.Data.x == x && kvp.Value.Data.y == y))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var k in keysToRemove)
            {
                _activeBlocks.Remove(k);
            }

            var slotKey = new Vector2Int(x, y);
            if (_cellSlots.TryGetValue(slotKey, out var slot))
            {
                slot.ClearBlock();
            }

            // STACK POP MECHANIC:
            // When the frontmost block (peek) in column x at y is destroyed,
            // all blocks behind it (Data.y < y) shift +1 row down (closer to user).
            ShiftColumnDownAfterDestroy(x, y);

            if (RemainingBlocksCount <= 0)
            {
                Debug.Log("<color=green>[EnemyGrid] ALL BLOCKS CLEARED! WIN!</color>");
                GameManager.Instance?.CheckWinCondition();
            }
        }

        /// <summary>
        /// STACK POP MECHANIC:
        /// When the frontmost block (peek) in column colIndex at destroyedY is destroyed,
        /// all blocks behind it (Data.y < destroyedY) shift +1 row down (closer to user).
        /// Iterates descending by Data.y so each block slides into the newly vacated slot below.
        /// </summary>
        public void ShiftColumnDownAfterDestroy(int colIndex, int destroyedY)
        {
            // 1. Find all living blocks in this column behind the destroyed row
            List<EnemyBlock> blocksToShift = new List<EnemyBlock>();
            foreach (var kvp in _activeBlocks)
            {
                var b = kvp.Value;
                if (b != null && !b.IsDestroyed && b.Data.x == colIndex && b.Data.y < destroyedY)
                {
                    blocksToShift.Add(b);
                }
            }

            if (blocksToShift.Count == 0) return;

            // 2. Sort DESCENDING by Data.y so we shift lowest (nearest to destroyed) first
            blocksToShift.Sort((a, b) => b.Data.y.CompareTo(a.Data.y));

            int maxGridHeight = _currentLevelData != null ? _currentLevelData.gridHeight : 8;

            // 3. Move each block +1 row down towards the player panel
            foreach (var block in blocksToShift)
            {
                int oldY = block.Data.y;
                int newY = oldY + 1;
                if (newY >= maxGridHeight) continue;

                int z = block.Data.z;

                // Remove from old dict key
                _activeBlocks.Remove(new Vector3Int(colIndex, oldY, z));

                // Clear old slot
                var oldSlotKey = new Vector2Int(colIndex, oldY);
                if (_cellSlots.TryGetValue(oldSlotKey, out var oldSlot))
                {
                    oldSlot.ClearBlock();
                }

                // Update block coordinate data
                block.Data.y = newY;
                block.name = $"EnemyBlock_{colIndex}_{newY}";

                // Insert into new dict key
                _activeBlocks[new Vector3Int(colIndex, newY, z)] = block;

                // Place in new slot and trigger smooth slide animation
                var newSlotKey = new Vector2Int(colIndex, newY);
                if (_cellSlots.TryGetValue(newSlotKey, out var newSlot))
                {
                    newSlot.SetBlock(block);
                    block.AnimateShiftDown(newSlot.transform.position);
                }

                // If block is Interlocked, update its partner's target Y
                if (block.Data.type == EnemyBlockType.Interlocked && block.Data.partnerX >= 0)
                {
                    var partnerKey = new Vector3Int(block.Data.partnerX, block.Data.partnerY, z);
                    if (_activeBlocks.TryGetValue(partnerKey, out var partner) && partner != null)
                    {
                        partner.Data.partnerY = newY;
                    }
                }
            }
        }
    }
}

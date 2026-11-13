using System.Collections.Generic;
using ThisIsBlast.Combat;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using ThisIsBlast.EnemyGrid;
using UnityEngine;

namespace ThisIsBlast.Arsenal
{
    /// <summary>
    /// Represents a column queue stack of waiting blocks inside the Player Block Panel.
    /// Only the front block (index 0) can be picked to move up to a free slot on the shooting bench.
    /// </summary>
    public class ArsenalColumn : MonoBehaviour
    {
        public int ColumnIndex { get; private set; }
        public bool IsLocked { get; private set; }
        public int RemainingBlocksCount => _blocks.Count;
        public ArsenalBlock FrontBlock => _blocks.Count > 0 ? _blocks[0] : null;

        [Header("Column Stack Spacing")]
        [SerializeField] private float _blockSpacing = 1.10f; // Spacing between blocks in the player queue

        private readonly List<ArsenalBlock> _blocks = new List<ArsenalBlock>();
        private GameObject _lockObject;

        public void Initialize(ArsenalColumnData data)
        {
            ColumnIndex = data.columnIndex;
            IsLocked = data.isLockedInitially;

            Clear();

            // Spawn blocks in the player queue stack
            for (int i = 0; i < data.blocks.Count; i++)
            {
                var blockData = data.blocks[i];
                Vector3 localPos = new Vector3(0, -i * _blockSpacing, 0);

                var blockGo = new GameObject($"ArsenalBlock_{i}");
                blockGo.transform.SetParent(transform, false);
                blockGo.transform.localPosition = localPos;
                blockGo.transform.localScale = Vector3.one * 0.95f;

                var block = blockGo.AddComponent<ArsenalBlock>();
                block.Initialize(blockData, i == 0 && !IsLocked);
                _blocks.Add(block);
            }

            if (IsLocked)
            {
                SetupLockVisual();
            }
        }

        public void Clear()
        {
            foreach (var b in _blocks)
            {
                if (b != null) Destroy(b.gameObject);
            }
            _blocks.Clear();

            if (_lockObject != null)
            {
                Destroy(_lockObject);
                _lockObject = null;
            }
        }

        private void SetupLockVisual()
        {
            _lockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _lockObject.name = "ColumnLock";
            _lockObject.transform.SetParent(transform, false);
            _lockObject.transform.localPosition = new Vector3(0, 0, -0.1f);
            _lockObject.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            var r = _lockObject.GetComponent<MeshRenderer>();
            r.material = MaterialHelper.CreateStandardMaterial(BlockColorPalette.LockColor, 0.4f);

            var labelGo = new GameObject("LockLabel");
            labelGo.transform.SetParent(_lockObject.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 0, -0.52f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "🔒";
            tm.fontSize = 50;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
        }

        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;

            if (_lockObject != null)
            {
                ParticleEffectsManager.Instance?.SpawnBlockDestroyParticles(_lockObject.transform.position, BlockColorPalette.LockColor);
                Destroy(_lockObject);
                _lockObject = null;
            }

            if (FrontBlock != null)
            {
                FrontBlock.SetActivePeek(true);
            }

            GameEvents.TriggerColumnUnlocked(ColumnIndex);
        }

        /// <summary>
        /// Attempts to pick a block from this column and move it to a free valid slot on the shooting bench.
        /// Fails if:
        /// - Column is locked
        /// - Target block has a block in front/above it (not FrontBlock)
        /// - Target block is interlocked
        /// - There is no space on the shooting bench (all 5 slots full)
        /// </summary>
        public bool TryPickBlock(ArsenalBlock targetBlock = null)
        {
            if (IsLocked)
            {
                FloatingText.Create(transform.position, "LOCKED! 🔒", Color.red, 0.35f);
                SoundManager.Instance?.PlayHit();
                return false;
            }

            if (targetBlock == null)
            {
                targetBlock = FrontBlock;
            }

            if (targetBlock == null) return false;

            // 1. Cannot move if there is a block above/in front of it
            if (targetBlock != FrontBlock)
            {
                FloatingText.Create(targetBlock.transform.position, "BLOCKED!", Color.yellow, 0.25f);
                targetBlock.PunchBlockedWiggle();
                SoundManager.Instance?.PlayHit();
                return false;
            }

            // 2. Cannot move if interlocked with another block
            if (targetBlock.IsInterlocked)
            {
                FloatingText.Create(targetBlock.transform.position, "INTERLOCKED! ⛓", Color.yellow, 0.35f);
                targetBlock.PunchBlockedWiggle();
                SoundManager.Instance?.PlayHit();
                return false;
            }

            // 3. Cannot move if there's no space on the shooting bench
            var freeSlot = ArsenalManager.Instance != null ? ArsenalManager.Instance.GetFirstEmptyBenchSlot() : null;
            if (freeSlot == null)
            {
                FloatingText.Create(targetBlock.transform.position, "BENCH FULL!", Color.yellow, 0.35f);
                targetBlock.PunchBlockedWiggle();
                SoundManager.Instance?.PlayHit();
                return false;
            }

            // Valid move: reserve target bench slot
            freeSlot.IsReserved = true;

            // Remove from column queue
            _blocks.Remove(targetBlock);

            // Animate remaining blocks in this column sliding up
            for (int i = 0; i < _blocks.Count; i++)
            {
                Vector3 targetLocalPos = new Vector3(0, -i * _blockSpacing, 0);
                _blocks[i].AnimatePopUp(targetLocalPos);
                _blocks[i].SetActivePeek(i == 0 && !IsLocked);
            }

            // Animate picked block flying to the bench slot
            targetBlock.AnimateMoveToBenchSlot(freeSlot, () =>
            {
                ArsenalManager.Instance?.TriggerMergeCheck();
            });

            return true;
        }

        public void RemoveBlockFromStack(ArsenalBlock block)
        {
            int index = _blocks.IndexOf(block);
            if (index >= 0)
            {
                _blocks.RemoveAt(index);
                for (int i = 0; i < _blocks.Count; i++)
                {
                    Vector3 targetLocalPos = new Vector3(0, -i * _blockSpacing, 0);
                    _blocks[i].AnimatePopUp(targetLocalPos);
                    _blocks[i].SetActivePeek(i == 0 && !IsLocked);
                }
            }
        }

        public List<ArsenalBlock> GetAllBlocks()
        {
            return new List<ArsenalBlock>(_blocks);
        }
    }
}

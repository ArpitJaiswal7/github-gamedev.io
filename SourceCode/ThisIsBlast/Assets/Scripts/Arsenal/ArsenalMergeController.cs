using System.Collections;
using System.Collections.Generic;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using UnityEngine;

namespace ThisIsBlast.Arsenal
{
    public class ArsenalMergeController : MonoBehaviour
    {
        public static ArsenalMergeController Instance { get; private set; }

        private bool _isMerging = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void CheckMerges()
        {
            if (!_isMerging)
            {
                StartCoroutine(EvaluateAndRunMerges());
            }
        }

        private IEnumerator EvaluateAndRunMerges()
        {
            _isMerging = true;
            bool mergedAny = false;
            int cascadeCount = 0;

            while (true)
            {
                // Gather blocks ONLY currently docked on the shooting bench row
                var benchBlocks = ArsenalManager.Instance != null ? ArsenalManager.Instance.GetActiveBenchBlocks() : new List<ArsenalBlock>();
                
                // Group by color
                var colorBuckets = new Dictionary<BlockColor, List<ArsenalBlock>>();
                foreach (var b in benchBlocks)
                {
                    if (b == null || b.BlockColor == BlockColor.None || b.IsMoving) continue;
                    if (!colorBuckets.ContainsKey(b.BlockColor))
                    {
                        colorBuckets[b.BlockColor] = new List<ArsenalBlock>();
                    }
                    colorBuckets[b.BlockColor].Add(b);
                }

                // Check if any color has 3 or more blocks ON THE SHOOTING BENCH
                BlockColor mergeCandidateColor = BlockColor.None;
                List<ArsenalBlock> candidates = null;

                foreach (var kvp in colorBuckets)
                {
                    if (kvp.Value.Count >= 3)
                    {
                        mergeCandidateColor = kvp.Key;
                        candidates = kvp.Value;
                        break;
                    }
                }

                if (mergeCandidateColor == BlockColor.None || candidates == null)
                {
                    // No 3-matches on bench
                    break;
                }

                // Sort: survivor block with highest bullet count
                candidates.Sort((a, b) => b.BulletCount.CompareTo(a.BulletCount));

                ArsenalBlock survivor = candidates[0];
                ArsenalBlock consumedA = candidates[1];
                ArsenalBlock consumedB = candidates[2];

                int bulletsToAdd = consumedA.BulletCount + consumedB.BulletCount;

                // Animate consumed blocks flying into survivor and freeing their bench slots
                Vector3 targetPos = survivor.transform.position;
                consumedA.AnimateMergeInto(targetPos, () =>
                {
                    consumedA.DockedSlot?.ClearBlock();
                });
                consumedB.AnimateMergeInto(targetPos, () =>
                {
                    consumedB.DockedSlot?.ClearBlock();
                });

                yield return new WaitForSeconds(0.24f);

                // Add bullets to survivor
                if (survivor != null)
                {
                    survivor.AddBullets(bulletsToAdd);

                    string mode = RemoteConfigService.Instance ? RemoteConfigService.Instance.GetString("bench_merge_mode", "simple_accumulate") : "simple_accumulate";
                    if (mode == "tier_evolve" && survivor.Data != null)
                    {
                        survivor.Data.tier++;
                    }

                    SoundManager.Instance?.PlayMerge();
                    CameraShaker.Instance?.Shake(0.12f, 0.15f);
                    ParticleEffectsManager.Instance?.SpawnMergeParticles(survivor.transform.position, BlockColorPalette.GetColor(survivor.BlockColor));
                    
                    ArsenalManager.Instance?.HighlightBenchRow(BlockColorPalette.GetColor(survivor.BlockColor));
                    survivor.DockedSlot?.Highlight(BlockColorPalette.GetColor(survivor.BlockColor), 0.35f);

                    cascadeCount++;
                    string cascadeText = cascadeCount > 1 ? $"\nCASCADE x{cascadeCount}!" : "";
                    FloatingText.Create(survivor.transform.position, $"MERGE! +{bulletsToAdd} 💥{cascadeText}", BlockColorPalette.GetColor(survivor.BlockColor), 0.45f);

                    GameEvents.TriggerArsenalMergeTriggered(survivor.BlockColor, survivor.BulletCount, 3);
                    if (cascadeCount > 1)
                    {
                        GameEvents.TriggerComboChain();
                    }
                }

                mergedAny = true;
                yield return new WaitForSeconds(0.1f);
            }

            _isMerging = false;

            if (mergedAny)
            {
                ArsenalManager.Instance?.CheckFailCondition();
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ArrowStrike.ShapeSystem
{
    public struct GeneratedArrowPlan
    {
        public List<Vector2Int> path; // path[0] = tail, path[^1] = head
        public ShapeArrowDirection direction;
        public int solveOrderIndex;
    }

    /// <summary>
    /// Generates 100% guaranteed-solvable arrow layouts inside any convex, concave, or hollow pixel-art shape
    /// using an Outside-In (Peel-in-Solve-Order) + Warnsdorff degree-aware backward walk algorithm.
    /// </summary>
    public static class SolvableShapeGenerator
    {
        private struct HeadCandidate
        {
            public Vector2Int head;
            public Vector2Int neck;
            public Vector2Int dir;
            public int priorityScore;
        }

        private static readonly Vector2Int[] CardinalDirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public static List<GeneratedArrowPlan> GenerateSolvableLayout(
            ShapeGridSystem grid,
            int minArrowLen,
            int maxArrowLen,
            int maxArrowCount = 0)
        {
            List<GeneratedArrowPlan> plans = new List<GeneratedArrowPlan>();
            IReadOnlyList<Vector2Int> activeCoords = grid.ActiveCoordinates;
            if (activeCoords == null || activeCoords.Count < 2)
                return plans;

            HashSet<Vector2Int> uncarved = new HashSet<Vector2Int>(activeCoords);
            int targetMaxArrows = maxArrowCount > 0 ? maxArrowCount : activeCoords.Count;

            minArrowLen = Mathf.Max(2, minArrowLen);
            maxArrowLen = Mathf.Max(minArrowLen, maxArrowLen);

            List<HeadCandidate> candidates = new List<HeadCandidate>(128);
            int solveIndex = 0;

            while (uncarved.Count >= 2 && plans.Count < targetMaxArrows)
            {
                FindValidHeadCandidates(grid, uncarved, candidates);
                if (candidates.Count == 0)
                    break;

                // Pick among the highest-priority candidates (those that avoid stranding 1-cell islands)
                int bestScore = int.MinValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].priorityScore > bestScore)
                        bestScore = candidates[i].priorityScore;
                }

                List<HeadCandidate> topCandidates = new List<HeadCandidate>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].priorityScore >= bestScore - 1)
                        topCandidates.Add(candidates[i]);
                }

                HeadCandidate chosen = topCandidates[Random.Range(0, topCandidates.Count)];

                // Build arrow backwards: head -> neck -> ... -> tail
                int targetLen = Random.Range(minArrowLen, maxArrowLen + 1);
                List<Vector2Int> reversedPath = new List<Vector2Int>(targetLen)
                {
                    chosen.head,
                    chosen.neck
                };

                uncarved.Remove(chosen.head);
                uncarved.Remove(chosen.neck);

                GrowTailBackwards(uncarved, reversedPath, targetLen);

                // If any adjacent cell to the current tail just became a 0-degree orphan in uncarved, rescue it onto the tail
                TryRescueAdjacentOrphan(uncarved, reversedPath);

                // Reverse so path[0] is tail and path[^1] is head
                reversedPath.Reverse();

                plans.Add(new GeneratedArrowPlan
                {
                    path = reversedPath,
                    direction = ShapeArrow.VectorToDirection(chosen.dir),
                    solveOrderIndex = solveIndex++
                });
            }

            return plans;
        }

        private static void FindValidHeadCandidates(
            ShapeGridSystem grid,
            HashSet<Vector2Int> uncarved,
            List<HeadCandidate> results)
        {
            results.Clear();

            foreach (Vector2Int cell in uncarved)
            {
                for (int d = 0; d < CardinalDirs.Length; d++)
                {
                    Vector2Int dir = CardinalDirs[d];
                    Vector2Int neck = cell - dir;

                    // Neck must be an uncarved active cell so the arrow has length >= 2 and points along 'dir'
                    if (!uncarved.Contains(neck))
                        continue;

                    // Check that the forward exit ray from 'cell' in direction 'dir' contains ZERO uncarved cells
                    if (!IsExitRayClear(grid, uncarved, cell, dir))
                        continue;

                    // Score candidate: penalize if removing 'cell' strands another neighbor of 'cell' with 0 neighbors
                    int score = 10;
                    int headDegree = CountUncarvedNeighbors(uncarved, cell);
                    if (headDegree == 1)
                    {
                        // 'cell' is a leaf tip whose only neighbor is 'neck' — ideal to peel!
                        score += 6;
                    }
                    else
                    {
                        // Check if any other neighbor of 'cell' (besides 'neck') would become isolated (degree 1 -> 0)
                        for (int k = 0; k < CardinalDirs.Length; k++)
                        {
                            Vector2Int otherNb = cell + CardinalDirs[k];
                            if (otherNb != neck && uncarved.Contains(otherNb))
                            {
                                int nbDeg = CountUncarvedNeighbors(uncarved, otherNb);
                                if (nbDeg <= 1)
                                    score -= 8;
                            }
                        }
                    }

                    results.Add(new HeadCandidate
                    {
                        head = cell,
                        neck = neck,
                        dir = dir,
                        priorityScore = score
                    });
                }
            }
        }

        private static bool IsExitRayClear(
            ShapeGridSystem grid,
            HashSet<Vector2Int> uncarved,
            Vector2Int head,
            Vector2Int dir)
        {
            Vector2Int probe = head + dir;
            while (grid.IsInsideBoundingBox(probe))
            {
                if (uncarved.Contains(probe))
                    return false;
                probe += dir;
            }
            return true;
        }

        private static void GrowTailBackwards(
            HashSet<Vector2Int> uncarved,
            List<Vector2Int> reversedPath,
            int targetLen)
        {
            List<Vector2Int> validNext = new List<Vector2Int>(4);

            while (reversedPath.Count < targetLen)
            {
                Vector2Int current = reversedPath[^1];
                Vector2Int prevStepDir = current - reversedPath[^2]; // direction of backward walk

                validNext.Clear();
                Vector2Int orphanRescue = new Vector2Int(int.MinValue, int.MinValue);

                for (int i = 0; i < CardinalDirs.Length; i++)
                {
                    Vector2Int nb = current + CardinalDirs[i];
                    if (!uncarved.Contains(nb))
                        continue;

                    int degInRemaining = CountUncarvedNeighbors(uncarved, nb);
                    if (degInRemaining == 0)
                    {
                        // Stepping into this neighbor rescues it from becoming a 1-cell orphan!
                        orphanRescue = nb;
                    }

                    validNext.Add(nb);
                }

                if (validNext.Count == 0)
                    break;

                Vector2Int chosenNext;
                if (orphanRescue.x != int.MinValue)
                {
                    chosenNext = orphanRescue;
                }
                else
                {
                    // Prefer continuing straight ~65% of the time if available, else pick a turn (favoring low-degree cells)
                    Vector2Int straightCandidate = current + prevStepDir;
                    if (validNext.Contains(straightCandidate) && Random.value < 0.65f)
                    {
                        chosenNext = straightCandidate;
                    }
                    else
                    {
                        // Warnsdorff heuristic: pick neighbor with fewest remaining uncarved neighbors
                        int minDeg = int.MaxValue;
                        chosenNext = validNext[Random.Range(0, validNext.Count)];
                        for (int i = 0; i < validNext.Count; i++)
                        {
                            Vector2Int cand = validNext[i];
                            int deg = CountUncarvedNeighbors(uncarved, cand);
                            if (deg < minDeg || (deg == minDeg && Random.value < 0.5f))
                            {
                                minDeg = deg;
                                chosenNext = cand;
                            }
                        }
                    }
                }

                reversedPath.Add(chosenNext);
                uncarved.Remove(chosenNext);
            }
        }

        private static void TryRescueAdjacentOrphan(
            HashSet<Vector2Int> uncarved,
            List<Vector2Int> reversedPath)
        {
            Vector2Int tail = reversedPath[^1];
            for (int i = 0; i < CardinalDirs.Length; i++)
            {
                Vector2Int nb = tail + CardinalDirs[i];
                if (uncarved.Contains(nb) && CountUncarvedNeighbors(uncarved, nb) == 0)
                {
                    reversedPath.Add(nb);
                    uncarved.Remove(nb);
                    break;
                }
            }
        }

        private static int CountUncarvedNeighbors(HashSet<Vector2Int> uncarved, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < CardinalDirs.Length; i++)
            {
                if (uncarved.Contains(cell + CardinalDirs[i]))
                    count++;
            }
            return count;
        }
    }
}

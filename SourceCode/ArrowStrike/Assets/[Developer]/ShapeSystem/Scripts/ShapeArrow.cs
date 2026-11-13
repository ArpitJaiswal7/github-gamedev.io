using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowStrike.ShapeSystem
{
    public enum ShapeArrowDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [RequireComponent(typeof(LineRenderer))]
    public class ShapeArrow : MonoBehaviour
    {
        public ShapeArrowDirection direction;
        public ShapeGridSystem gridManager;

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private readonly List<ShapeArrowUnit> units = new List<ShapeArrowUnit>();
        private LineRenderer lineRenderer;
        private Vector3[] linePositions;
        private bool isMoving;
        private System.Action<ShapeArrow> onArrowCleared;
        private System.Action<ShapeArrow> onArrowBlocked;
        private System.Func<bool> canInteractCheck;

        private static Material sharedLineMaterial;

        public void Initialize(
            ShapeGridSystem grid,
            List<Vector2Int> pathCells,
            ShapeArrowDirection dir,
            List<ShapeArrowUnit> spawnedUnits,
            System.Action<ShapeArrow> onClearedCallback = null,
            System.Action<ShapeArrow> onBlockedCallback = null,
            System.Func<bool> canInteract = null)
        {
            gridManager = grid;
            direction = dir;
            onArrowCleared = onClearedCallback;
            onArrowBlocked = onBlockedCallback;
            canInteractCheck = canInteract;

            path.Clear();
            path.AddRange(pathCells);

            units.Clear();
            units.AddRange(spawnedUnits);

            for (int i = 0; i < path.Count; i++)
            {
                gridManager.MarkCellOccupied(path[i], gameObject);
            }

            SetupLineRenderer();
            RefreshLineAndHeadVisuals();
        }

        private void SetupLineRenderer()
        {
            if (!TryGetComponent(out lineRenderer))
                lineRenderer = gameObject.AddComponent<LineRenderer>();

            if (sharedLineMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader != null)
                    sharedLineMaterial = new Material(spriteShader);
            }

            if (sharedLineMaterial != null)
                lineRenderer.sharedMaterial = sharedLineMaterial;

            lineRenderer.useWorldSpace = true;
            lineRenderer.numCornerVertices = 6;
            lineRenderer.numCapVertices = 6;
            lineRenderer.sortingOrder = 3;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.alignment = LineAlignment.View;

            float lineWidth = gridManager != null ? gridManager.cellSizeX * 0.65f : 0.12f;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            linePositions = new Vector3[units.Count];
            lineRenderer.positionCount = units.Count;

            // Build color gradient from the sampled unit colors (up to 8 gradient keys supported by Unity)
            if (units.Count > 0)
            {
                Gradient gradient = new Gradient();
                int keyCount = Mathf.Clamp(units.Count, 2, 8);
                GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
                GradientAlphaKey[] alphaKeys =
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };

                for (int k = 0; k < keyCount; k++)
                {
                    float t = keyCount > 1 ? (float)k / (keyCount - 1) : 0f;
                    int unitIdx = Mathf.Clamp(Mathf.RoundToInt(t * (units.Count - 1)), 0, units.Count - 1);
                    colorKeys[k] = new GradientColorKey(units[unitIdx].unitColor, t);
                }

                gradient.SetKeys(colorKeys, alphaKeys);
                lineRenderer.colorGradient = gradient;
            }
        }

        private void RefreshLineAndHeadVisuals()
        {
            if (lineRenderer == null || units.Count < 2)
                return;

            if (linePositions == null || linePositions.Length != units.Count)
            {
                linePositions = new Vector3[units.Count];
                lineRenderer.positionCount = units.Count;
            }

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null)
                    linePositions[i] = units[i].transform.position;
            }

            // Compute head facing direction from neck -> head (or fallback to enum direction)
            Vector3 neckToHead = linePositions[^1] - linePositions[^2];
            Vector2 headFacing = neckToHead.sqrMagnitude > 0.00001f
                ? new Vector2(neckToHead.x, neckToHead.y).normalized
                : (Vector2)DirectionToVector(direction);

            // Pull the tip of the LineRenderer slightly back so the triangle arrowhead sits cleanly at the front
            float pullback = gridManager != null ? gridManager.cellSizeX * 0.18f : 0.03f;
            linePositions[^1] -= new Vector3(headFacing.x * pullback, headFacing.y * pullback, 0f);

            lineRenderer.SetPositions(linePositions);

            ShapeArrowUnit headUnit = units[^1];
            if (headUnit != null)
            {
                headUnit.SetHeadFacingDirection(headFacing);
            }
        }

        public void OnArrowClicked()
        {
            if (isMoving || gridManager == null || path.Count == 0)
                return;

            if (canInteractCheck != null && !canInteractCheck.Invoke())
                return;

            StartCoroutine(MoveOrBonkRoutine());
        }

        private IEnumerator MoveOrBonkRoutine()
        {
            isMoving = true;

            Vector2Int stepDir = DirectionToVector(direction);
            float stepDuration = 0.018f;

            List<Vector3> startPositions = new List<Vector3>(units.Count);
            List<Vector3> targetPositions = new List<Vector3>(units.Count);

            // Keep snapshots of each grid step so if the arrow gets blocked after moving forward,
            // it can smoothly retrace its exact path back to its original grid set.
            List<Vector2Int[]> pathHistory = new List<Vector2Int[]>
            {
                path.ToArray()
            };

            // Phase 1: Advance grid-by-grid inside the board until either:
            //   (a) the next cell in stepDir is occupied by another arrow (obstacle -> bonk, lose life, return to original grid set), or
            //   (b) the head steps outside the bounding box (clear path -> slide all the way out).
            while (true)
            {
                Vector2Int head = path[^1];
                Vector2Int nextHeadCoord = head + stepDir;

                // If the next cell is inside the board and occupied by another arrow, we hit an obstacle!
                if (gridManager.IsInsideBoundingBox(nextHeadCoord) &&
                    gridManager.IsCoordinateOccupiedByOther(nextHeadCoord, gameObject))
                {
                    onArrowBlocked?.Invoke(this);
                    yield return StartCoroutine(PlayBlockedBonkRoutine(stepDir));

                    // Return step-by-step back to the original grid set
                    for (int s = pathHistory.Count - 2; s >= 0; s--)
                    {
                        // Free the current front cell before stepping back
                        gridManager.MarkCellFree(path[^1]);

                        Vector2Int[] prevSnapshot = pathHistory[s];
                        for (int i = 0; i < path.Count; i++)
                        {
                            path[i] = prevSnapshot[i];
                            gridManager.MarkCellOccupied(path[i], gameObject);
                        }

                        yield return StartCoroutine(AnimateOneStepRoutine(startPositions, targetPositions, stepDuration));
                    }

                    isMoving = false;
                    yield break;
                }

                // If the head is about to exit the bounding box, no obstacle blocked it!
                if (!gridManager.IsInsideBoundingBox(nextHeadCoord))
                {
                    break;
                }

                // Advance 1 grid cell toward the obstacle / exit
                Vector2Int oldTail = path[0];
                gridManager.MarkCellFree(oldTail);

                for (int i = 0; i < path.Count - 1; i++)
                {
                    path[i] = path[i + 1];
                }
                path[^1] = nextHeadCoord;

                // Ensure all current path coordinates are marked occupied by this arrow
                for (int i = 0; i < path.Count; i++)
                {
                    gridManager.MarkCellOccupied(path[i], gameObject);
                }

                pathHistory.Add(path.ToArray());

                yield return StartCoroutine(AnimateOneStepRoutine(startPositions, targetPositions, stepDuration));
            }

            // Phase 2: Head has reached the outer boundary unblocked!
            // Free all cells occupied by this arrow so other arrows can be clicked immediately while it finishes exiting.
            for (int i = 0; i < path.Count; i++)
            {
                gridManager.MarkCellFree(path[i]);
            }

            int exitStepsRemaining = path.Count + 2;
            for (int step = 0; step < exitStepsRemaining; step++)
            {
                Vector2Int newHeadCoord = path[^1] + stepDir;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    path[i] = path[i + 1];
                }
                path[^1] = newHeadCoord;

                yield return StartCoroutine(AnimateOneStepRoutine(startPositions, targetPositions, stepDuration));

                if (!gridManager.IsInsideBoundingBox(path[0]))
                    break;
            }

            onArrowCleared?.Invoke(this);
            Destroy(gameObject);
        }

        private IEnumerator AnimateOneStepRoutine(List<Vector3> startPositions, List<Vector3> targetPositions, float stepDuration)
        {
            startPositions.Clear();
            targetPositions.Clear();

            for (int i = 0; i < units.Count; i++)
            {
                startPositions.Add(units[i].transform.position);
                targetPositions.Add(gridManager.GetWorldPosition(path[i]));
            }

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepDuration);
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i] != null)
                        units[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                }
                RefreshLineAndHeadVisuals();
                yield return null;
            }

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null)
                {
                    units[i].transform.position = targetPositions[i];
                    units[i].currentCoord = path[i];
                }
            }
            RefreshLineAndHeadVisuals();
        }

        private IEnumerator PlayBlockedBonkRoutine(Vector2Int stepDir)
        {
            Vector3 nudgeOffset = new Vector3(
                stepDir.x * (gridManager.cellSizeX + gridManager.cellOffset) * 0.45f,
                stepDir.y * (gridManager.cellSizeY + gridManager.cellOffset) * 0.45f,
                0f
            );

            List<Vector3> originalPos = new List<Vector3>(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                originalPos.Add(units[i] != null ? units[i].transform.position : Vector3.zero);
            }

            float halfDuration = 0.06f;
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i] != null)
                        units[i].transform.position = Vector3.Lerp(originalPos[i], originalPos[i] + nudgeOffset, t);
                }
                RefreshLineAndHeadVisuals();
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i] != null)
                        units[i].transform.position = Vector3.Lerp(originalPos[i] + nudgeOffset, originalPos[i], t);
                }
                RefreshLineAndHeadVisuals();
                yield return null;
            }

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null)
                    units[i].transform.position = originalPos[i];
            }
            RefreshLineAndHeadVisuals();
        }

        public static Vector2Int DirectionToVector(ShapeArrowDirection dir)
        {
            return dir switch
            {
                ShapeArrowDirection.Up => Vector2Int.up,
                ShapeArrowDirection.Down => Vector2Int.down,
                ShapeArrowDirection.Left => Vector2Int.left,
                ShapeArrowDirection.Right => Vector2Int.right,
                _ => Vector2Int.up
            };
        }

        public static ShapeArrowDirection VectorToDirection(Vector2Int vec)
        {
            if (vec == Vector2Int.down) return ShapeArrowDirection.Down;
            if (vec == Vector2Int.left) return ShapeArrowDirection.Left;
            if (vec == Vector2Int.right) return ShapeArrowDirection.Right;
            return ShapeArrowDirection.Up;
        }
    }
}

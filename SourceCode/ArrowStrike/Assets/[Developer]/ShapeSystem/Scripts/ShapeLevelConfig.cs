using System;
using UnityEngine;

namespace ArrowStrike.ShapeSystem
{
    public enum BoardShapeType
    {
        Rectangle = 0,
        Triangle = 1,
        Circle = 2,
        Diamond = 3,
        Heart = 4,
        Star = 5,
        Cross = 6,
        CustomTexture = 7
    }

    [CreateAssetMenu(fileName = "ShapeLevelConfig", menuName = "ArrowStrike/Shape Level Config", order = 0)]
    public class ShapeLevelConfig : ScriptableObject
    {
        [Serializable]
        public struct IntRange
        {
            [Range(8, 60)] public int min;
            [Range(8, 60)] public int max;
        }

        [Serializable]
        public struct ArrowLengthRange
        {
            [Range(2, 15)] public int min;
            [Range(2, 30)] public int max;
        }

        [Header("Level Identity")]
        public string levelTitle = "Shape Level";
        public BoardShapeType shapeType = BoardShapeType.Rectangle;

        [Header("Pixel-Art Texture (Used when ShapeType = CustomTexture)")]
        [Tooltip("Assign a pixel-art PNG (Read/Write Enabled, Point filter, Uncompressed).")]
        public Texture2D shapeTexture;
        [Range(0.01f, 0.99f)] public float alphaThreshold = 0.1f;
        public bool useTextureColors = true;
        public bool flipTextureY = false;

        [Header("Grid Size (Applies to all shapes including CustomTexture)")]
        public IntRange gridWidth = new IntRange { min = 36, max = 36 };
        public IntRange gridHeight = new IntRange { min = 36, max = 36 };

        [Header("Arrow Settings")]
        public ArrowLengthRange arrowLength = new ArrowLengthRange { min = 4, max = 8 };
        [Tooltip("Target number of arrows (or 0 to pack the entire shape).")]
        public int maxArrows = 0;

        public int GetRandomArrowLength()
        {
            int min = Mathf.Max(2, arrowLength.min);
            int max = Mathf.Max(min, arrowLength.max);
            return UnityEngine.Random.Range(min, max + 1);
        }

        public void ResolveGridDimensions(out int width, out int height)
        {
            int minW = Mathf.Max(8, gridWidth.min);
            int maxW = Mathf.Max(minW, gridWidth.max);
            int minH = Mathf.Max(8, gridHeight.min);
            int maxH = Mathf.Max(minH, gridHeight.max);

            width = UnityEngine.Random.Range(minW, maxW + 1);
            height = UnityEngine.Random.Range(minH, maxH + 1);
        }

        /// <summary>
        /// Builds the 2D active-cell mask and per-cell color map.
        /// Samples texture using normalized coordinates so any pixel-art PNG scales cleanly to larger gridWidth x gridHeight.
        /// </summary>
        public void BuildShapeData(int width, int height, out bool[,] activeMask, out Color[,] colorMap)
        {
            activeMask = new bool[width, height];
            colorMap = new Color[width, height];

            if (shapeType == BoardShapeType.CustomTexture && shapeTexture != null)
            {
                try
                {
                    Color32[] pixels = shapeTexture.GetPixels32();
                    int texW = shapeTexture.width;
                    int texH = shapeTexture.height;
                    byte minAlpha = (byte)Mathf.Clamp(Mathf.RoundToInt(alphaThreshold * 255f), 1, 254);

                    for (int x = 0; x < width; x++)
                    {
                        int tx = Mathf.Clamp(Mathf.FloorToInt(((x + 0.5f) / width) * texW), 0, texW - 1);
                        for (int y = 0; y < height; y++)
                        {
                            int rawTy = Mathf.Clamp(Mathf.FloorToInt(((y + 0.5f) / height) * texH), 0, texH - 1);
                            int ty = flipTextureY ? (texH - 1 - rawTy) : rawTy;
                            Color32 c = pixels[ty * texW + tx];

                            if (c.a >= minAlpha)
                            {
                                activeMask[x, y] = true;
                                colorMap[x, y] = new Color32(c.r, c.g, c.b, 255);
                            }
                        }
                    }
                    return;
                }
                catch (UnityException ex)
                {
                    Debug.LogWarning($"[ShapeLevelConfig] Texture '{shapeTexture.name}' is not readable ({ex.Message}). Falling back to procedural Diamond.");
                }
            }

            // Procedural geometric shapes
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool inside = EvaluateProceduralShape(shapeType, x, y, width, height);
                    activeMask[x, y] = inside;
                    if (inside)
                    {
                        colorMap[x, y] = EvaluateProceduralColor(shapeType, x, y, width, height);
                    }
                }
            }
        }

        public static bool EvaluateProceduralShape(BoardShapeType type, int x, int y, int width, int height)
        {
            // Normalized coordinates in [-1, 1]
            float nx = width > 1 ? (x / (float)(width - 1)) * 2f - 1f : 0f;
            float ny = height > 1 ? (y / (float)(height - 1)) * 2f - 1f : 0f;

            switch (type)
            {
                case BoardShapeType.Rectangle:
                    return true;

                case BoardShapeType.Triangle:
                {
                    // Upright isosceles triangle: wide at bottom (y=0), apex at top (y=height-1)
                    float rowProgress = height > 1 ? (float)y / (height - 1) : 0f; // 0 at bottom, 1 at top
                    float halfSpan = (1f - rowProgress * 0.9f);
                    return Mathf.Abs(nx) <= halfSpan;
                }

                case BoardShapeType.Circle:
                    return (nx * nx + ny * ny) <= 1.02f;

                case BoardShapeType.Diamond:
                    return (Mathf.Abs(nx) + Mathf.Abs(ny)) <= 1.05f;

                case BoardShapeType.Heart:
                {
                    // Classic implicit heart curve: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float hx = nx * 1.15f;
                    float hy = ny * 1.15f + 0.15f;
                    float a = hx * hx + hy * hy - 1f;
                    return (a * a * a - hx * hx * hy * hy * hy) <= 0f;
                }

                case BoardShapeType.Star:
                {
                    // 5-pointed star in polar coordinates
                    float angle = Mathf.Atan2(ny, nx) - Mathf.PI * 0.5f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float starRadius = 0.55f + 0.42f * Mathf.Cos(5f * angle);
                    return r <= starRadius;
                }

                case BoardShapeType.Cross:
                    return Mathf.Abs(nx) <= 0.36f || Mathf.Abs(ny) <= 0.36f;

                default:
                    return true;
            }
        }

        private static Color EvaluateProceduralColor(BoardShapeType type, int x, int y, int width, int height)
        {
            float u = width > 1 ? (float)x / (width - 1) : 0.5f;
            float v = height > 1 ? (float)y / (height - 1) : 0.5f;

            return type switch
            {
                BoardShapeType.Triangle => Color.Lerp(new Color(1f, 0.45f, 0.2f), new Color(1f, 0.85f, 0.2f), v),
                BoardShapeType.Circle => Color.Lerp(new Color(0.1f, 0.75f, 0.95f), new Color(0.4f, 0.35f, 0.95f), (u + v) * 0.5f),
                BoardShapeType.Diamond => Color.Lerp(new Color(0.18f, 0.85f, 0.55f), new Color(0.1f, 0.55f, 0.85f), Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f)),
                BoardShapeType.Heart => Color.Lerp(new Color(0.95f, 0.2f, 0.35f), new Color(1f, 0.5f, 0.65f), v),
                BoardShapeType.Star => Color.Lerp(new Color(1f, 0.85f, 0.15f), new Color(1f, 0.5f, 0.1f), Mathf.Abs(u - 0.5f) * 2f),
                BoardShapeType.Cross => new Color(0.3f, 0.85f, 0.75f),
                _ => Color.HSVToRGB((u * 0.5f + v * 0.5f) % 1f, 0.8f, 0.95f)
            };
        }
    }
}

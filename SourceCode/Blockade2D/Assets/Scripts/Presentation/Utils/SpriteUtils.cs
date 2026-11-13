using UnityEngine;

namespace Blockade2D.Presentation.Utils
{
    /// <summary>
    /// Utility to generate crisp procedural 2D sprites at runtime, ensuring the project
    /// renders beautiful visuals immediately without requiring imported art assets.
    /// </summary>
    public static class SpriteUtils
    {
        public static Sprite CreateRoundedSquareSprite(int size = 128, int cornerRadius = 24)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color[size * size];
            float r2 = cornerRadius * cornerRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    int dx = 0;
                    int dy = 0;

                    if (x < cornerRadius) dx = cornerRadius - x;
                    else if (x >= size - cornerRadius) dx = x - (size - cornerRadius - 1);

                    if (y < cornerRadius) dy = cornerRadius - y;
                    else if (y >= size - cornerRadius) dy = y - (size - cornerRadius - 1);

                    float distSq = dx * dx + dy * dy;

                    if (distSq <= r2)
                    {
                        // Inside corner
                        float alpha = Mathf.Clamp01(1.0f - (Mathf.Sqrt(distSq) - (cornerRadius - 1.0f)));
                        colors[index] = new Color(1f, 1f, 1f, alpha);
                    }
                    else if (dx > 0 && dy > 0)
                    {
                        // Outside corner
                        colors[index] = Color.clear;
                    }
                    else
                    {
                        // Straight edge or center
                        colors[index] = Color.white;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        public static Sprite CreateCircleSprite(int size = 128)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color[size * size];
            float radius = size / 2.0f;
            float rSq = radius * radius;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist);
                    colors[index] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        public static Sprite CreateSolidBoxSprite(int width = 32, int height = 32)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                32);
        }

        public static Sprite CreateBullsEyeSprite(int size = 128)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color[size * size];
            float radius = size / 2.0f;
            Vector2 center = new Vector2(radius, radius);

            Color targetColor = new Color(1f, 0.84f, 0.25f, 1f); // Vibrant Gold
            Color tileBgColor = new Color(0.22f, 0.16f, 0.08f, 1f); // Deep Warm Amber-Slate

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float norm = dist / radius;

                    if (norm > 0.98f)
                    {
                        colors[index] = Color.clear;
                    }
                    else
                    {
                        Color col = tileBgColor;

                        // Center Bullseye Dot
                        if (norm <= 0.25f)
                        {
                            col = targetColor;
                        }
                        // Middle Target Ring
                        else if (norm >= 0.50f && norm <= 0.70f)
                        {
                            col = targetColor;
                        }
                        // Outer Target Ring
                        else if (norm >= 0.88f && norm <= 0.97f)
                        {
                            col = targetColor;
                        }

                        colors[index] = col;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        public static Texture2D CreateWallIconTexture(bool isHorizontal, int size = 64, Color? color = null)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color barColor = color ?? new Color(0.95f, 0.77f, 0.25f, 1f); // Vibrant Gold
            var pixels = new Color[size * size];

            int barW = isHorizontal ? (int)(size * 0.78f) : (int)(size * 0.24f);
            int barH = isHorizontal ? (int)(size * 0.24f) : (int)(size * 0.78f);
            int startX = (size - barW) / 2;
            int startY = (size - barH) / 2;
            int cornerR = 3;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    if (x >= startX && x < startX + barW && y >= startY && y < startY + barH)
                    {
                        int dx = 0;
                        int dy = 0;

                        if (x < startX + cornerR) dx = startX + cornerR - x;
                        else if (x >= startX + barW - cornerR) dx = x - (startX + barW - cornerR - 1);

                        if (y < startY + cornerR) dy = startY + cornerR - y;
                        else if (y >= startY + barH - cornerR) dy = y - (startY + barH - cornerR - 1);

                        if (dx * dx + dy * dy <= cornerR * cornerR)
                            pixels[idx] = barColor;
                        else if (dx > 0 && dy > 0)
                            pixels[idx] = Color.clear;
                        else
                            pixels[idx] = barColor;
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}

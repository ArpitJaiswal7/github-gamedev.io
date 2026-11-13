using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Manages Unity Texture2D and Sprite memory for the Native Image Pipeline.
    /// - Converts compressed JPEG byte arrays (256px, 1080px, or custom sizes) into opaque RGB24 Texture2Ds and Sprites.
    /// - Tracks reference counts per (requestKey, sizePx) so active UI elements are never destroyed prematurely.
    /// - Automatically evicts and destroys unreferenced Texture2D/Sprite objects via LRU when the RAM pool limit is reached,
    ///   preventing VRAM/RAM exhaustion across 500+ puzzle images.
    /// </summary>
    public sealed class SpriteMemoryCache : IDisposable
    {
        private sealed class CachedSpriteEntry
        {
            public string CacheKey;
            public string RequestKey;
            public int SizePx;
            public Texture2D Texture;
            public Sprite Sprite;
            public int RefCount;
            public long LastAccessTick;
        }

        private readonly Dictionary<string, CachedSpriteEntry> entries = new Dictionary<string, CachedSpriteEntry>(StringComparer.Ordinal);
        private int maxPoolCapacity;
        private long accessCounter;

        public int ActiveSpriteCount => entries.Count;

        public SpriteMemoryCache(int maxPoolCapacity = 64)
        {
            this.maxPoolCapacity = Mathf.Max(8, maxPoolCapacity);
        }

        public void SetMaxCapacity(int maxPoolCapacity)
        {
            this.maxPoolCapacity = Mathf.Max(8, maxPoolCapacity);
            EvictIfNeeded();
        }

        /// <summary>
        /// Attempts to retrieve an already-created Sprite in Unity RAM and increments its reference count.
        /// </summary>
        public bool TryAcquireSprite(string requestKey, int sizePx, out Sprite sprite)
        {
            string key = BuildPoolKey(requestKey, sizePx);
            if (entries.TryGetValue(key, out var entry))
            {
                if (entry.Sprite != null && entry.Texture != null)
                {
                    entry.RefCount++;
                    entry.LastAccessTick = ++accessCounter;
                    sprite = entry.Sprite;
                    return true;
                }
                entries.Remove(key);
            }

            sprite = null;
            return false;
        }

        /// <summary>
        /// Decodes compressed JPEG bytes into an opaque RGB24 Texture2D, creates a Sprite,
        /// stores it in the managed RAM pool with RefCount = 1, and triggers LRU eviction if needed.
        /// </summary>
        public Sprite CreateAndCacheSprite(string requestKey, int sizePx, byte[] jpegBytes, float pixelsPerUnit = 100f)
        {
            if (jpegBytes == null || jpegBytes.Length == 0)
                return null;

            string key = BuildPoolKey(requestKey, sizePx);
            if (TryAcquireSprite(requestKey, sizePx, out Sprite existingSprite))
                return existingSprite;

            // Use RGB24 (no alpha channel) because our native pipeline converts all images to opaque JPEGs,
            // saving 25% VRAM compared to RGBA32!
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, mipChain: false);
            texture.name = $"NIP_Tex_{requestKey}_{sizePx}";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            if (!texture.LoadImage(jpegBytes, markNonReadable: true))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            var pivot = new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = $"NIP_Sprite_{requestKey}_{sizePx}";

            var newEntry = new CachedSpriteEntry
            {
                CacheKey = key,
                RequestKey = requestKey,
                SizePx = sizePx,
                Texture = texture,
                Sprite = sprite,
                RefCount = 1,
                LastAccessTick = ++accessCounter
            };

            entries[key] = newEntry;
            EvictIfNeeded();
            return sprite;
        }

        /// <summary>
        /// Decrements the reference count for a Sprite when a UI cell scrolls out of view or a puzzle closes.
        /// If destroyImmediatelyIfZero is true (useful for heavy 1080px full-screen textures),
        /// immediately frees the Texture2D from VRAM when RefCount hits 0.
        /// </summary>
        public void ReleaseSprite(string requestKey, int sizePx, bool destroyImmediatelyIfZero = false)
        {
            string key = BuildPoolKey(requestKey, sizePx);
            if (!entries.TryGetValue(key, out var entry))
                return;

            entry.RefCount = Mathf.Max(0, entry.RefCount - 1);
            entry.LastAccessTick = ++accessCounter;

            if (entry.RefCount == 0 && (destroyImmediatelyIfZero || sizePx >= (int)ImageResolutionTier.Full1080))
            {
                DestroyEntry(entry);
                entries.Remove(key);
            }
            else
            {
                EvictIfNeeded();
            }
        }

        /// <summary>
        /// Immediately destroys all loaded Sprites/Textures for a given requestKey across all resolution tiers.
        /// </summary>
        public void EvictAllResolutionsForKey(string requestKey)
        {
            if (string.IsNullOrWhiteSpace(requestKey))
                return;

            var keysToRemove = new List<string>();
            foreach (var kvp in entries)
            {
                if (string.Equals(kvp.Value.RequestKey, requestKey, StringComparison.Ordinal))
                {
                    DestroyEntry(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                entries.Remove(keysToRemove[i]);
            }
        }

        /// <summary>
        /// Destroys all unreferenced Sprites/Textures (RefCount <= 0) to free Unity RAM immediately.
        /// </summary>
        public int TrimUnreferenced()
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in entries)
            {
                if (kvp.Value.RefCount <= 0)
                {
                    DestroyEntry(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                entries.Remove(keysToRemove[i]);
            }

            return keysToRemove.Count;
        }

        private void EvictIfNeeded()
        {
            while (entries.Count > maxPoolCapacity)
            {
                CachedSpriteEntry oldestUnreferenced = null;
                foreach (var kvp in entries)
                {
                    var candidate = kvp.Value;
                    if (candidate.RefCount <= 0)
                    {
                        if (oldestUnreferenced == null || candidate.LastAccessTick < oldestUnreferenced.LastAccessTick)
                        {
                            oldestUnreferenced = candidate;
                        }
                    }
                }

                if (oldestUnreferenced == null)
                {
                    // All entries in pool are actively referenced by UI; do not destroy active sprites
                    break;
                }

                DestroyEntry(oldestUnreferenced);
                entries.Remove(oldestUnreferenced.CacheKey);
            }
        }

        private static void DestroyEntry(CachedSpriteEntry entry)
        {
            if (entry == null) return;
            if (entry.Sprite != null)
            {
                UnityEngine.Object.Destroy(entry.Sprite);
                entry.Sprite = null;
            }
            if (entry.Texture != null)
            {
                UnityEngine.Object.Destroy(entry.Texture);
                entry.Texture = null;
            }
        }

        private static string BuildPoolKey(string requestKey, int sizePx) => $"{requestKey}_{sizePx}";

        public void Dispose()
        {
            foreach (var kvp in entries)
            {
                DestroyEntry(kvp.Value);
            }
            entries.Clear();
        }
    }
}

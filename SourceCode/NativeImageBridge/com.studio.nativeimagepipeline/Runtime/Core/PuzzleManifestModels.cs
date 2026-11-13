using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Serializable models for parsing the startup JSON manifest returned by the backend API endpoint
    /// (supports both structured puzzle objects and flat URL arrays).
    /// </summary>
    [Serializable]
    public sealed class PuzzleManifestResponse
    {
        public string version = "1.0";
        public int count;
        public int totalPublished;
        public string updatedAt;
        public List<PuzzleImageEntry> puzzles = new List<PuzzleImageEntry>();
        public List<string> urls = new List<string>();

        /// <summary>
        /// Extracts all valid image URLs from either the structured 'puzzles' list or flat 'urls' array,
        /// assigning 1-based sequential levels and safe fallback titles.
        /// </summary>
        public List<PuzzleImageEntry> GetNormalizedEntries()
        {
            var result = new List<PuzzleImageEntry>();
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (puzzles != null)
            {
                for (int i = 0; i < puzzles.Count; i++)
                {
                    var item = puzzles[i];
                    if (item != null && !string.IsNullOrWhiteSpace(item.url) && seenUrls.Add(item.url.Trim()))
                    {
                        if (string.IsNullOrWhiteSpace(item.id))
                        {
                            item.id = RequestKeyGenerator.FromUrl(item.url);
                        }
                        if (string.IsNullOrWhiteSpace(item.title))
                        {
                            item.title = !string.IsNullOrWhiteSpace(item.filename)
                                ? Path.GetFileNameWithoutExtension(item.filename).Replace('_', ' ')
                                : item.id;
                        }
                        if (string.IsNullOrWhiteSpace(item.category))
                        {
                            item.category = "General";
                        }
                        if (item.level <= 0)
                        {
                            item.level = result.Count + 1;
                        }
                        result.Add(item);
                    }
                }
            }

            if (urls != null)
            {
                for (int i = 0; i < urls.Count; i++)
                {
                    string rawUrl = urls[i];
                    if (!string.IsNullOrWhiteSpace(rawUrl) && seenUrls.Add(rawUrl.Trim()))
                    {
                        string derivedId = RequestKeyGenerator.FromUrl(rawUrl);
                        string derivedTitle = Path.GetFileNameWithoutExtension(rawUrl).Replace('_', ' ');
                        result.Add(new PuzzleImageEntry
                        {
                            id = derivedId,
                            url = rawUrl.Trim(),
                            title = derivedTitle,
                            category = "Cloud Published",
                            level = result.Count + 1,
                            saveToRomOnDownload = false
                        });
                    }
                }
            }

            return result;
        }
    }

    [Serializable]
    public sealed class PuzzleImageEntry
    {
        /// <summary>
        /// Unique puzzle identifier (e.g. 'puz_4e0c22be'); if omitted, RequestKeyGenerator.FromUrl(url) is used.
        /// </summary>
        public string id;

        /// <summary>
        /// Human-readable title from the backend studio (e.g. 'mixed scenic sq 2048x2048 23').
        /// </summary>
        public string title;

        /// <summary>
        /// Category tag from backend studio (e.g., 'Nature', 'Architecture', 'Animals').
        /// </summary>
        public string category;

        /// <summary>
        /// Original source filename stored on the backend server.
        /// </summary>
        public string filename;

        /// <summary>
        /// Remote URL pointing to the source puzzle image (e.g., 2048x2048 JPG on GitHub Pages / port 4200).
        /// </summary>
        public string url;

        /// <summary>
        /// Optional direct REST image endpoint (e.g. 'http://localhost:4200/api/puzzles/puz_xyz/image').
        /// </summary>
        public string imageEndpoint;

        /// <summary>
        /// Indicates if this puzzle is published and active in the live catalog.
        /// </summary>
        public bool published = true;

        /// <summary>
        /// If true, stores this puzzle directly into Permanent ROM upon download instead of Volatile Cache.
        /// </summary>
        public bool saveToRomOnDownload;

        /// <summary>
        /// Width in pixels of original source image (e.g. 1080 or 2048).
        /// </summary>
        public int width = 1080;

        /// <summary>
        /// Height in pixels of original source image (e.g. 1080 or 2048).
        /// </summary>
        public int height = 1080;

        /// <summary>
        /// File size on disk/server in bytes.
        /// </summary>
        public long sizeBytes;

        /// <summary>
        /// MIME content type (e.g. 'image/jpeg' or 'image/png').
        /// </summary>
        public string mimeType = "image/jpeg";

        /// <summary>
        /// 1-based sequential campaign / game level mapped by LevelSyncManager.
        /// </summary>
        public int level = 0;

        /// <summary>
        /// Server creation ISO-8601 timestamp string.
        /// </summary>
        public string createdAt;
    }
}

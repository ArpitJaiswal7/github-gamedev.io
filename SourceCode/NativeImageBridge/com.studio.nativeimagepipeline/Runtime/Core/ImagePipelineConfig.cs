using System;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Configuration profile for the Native Image Pipeline.
    /// Can be configured per game (e.g., Calm Jigsaw vs. Jigsaw Solitaire) via ScriptableObject or code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ImagePipelineConfig",
        menuName = "Studio/Native Image Pipeline/Pipeline Config",
        order = 1)]
    public sealed class ImagePipelineConfig : ScriptableObject
    {
        public const string DefaultLiveBackendEndpoint = "https://arpitjaiswal7.github.io/github-gamedev.io/assets/puzzles/published.json";
        public const string DefaultLocalServerEndpoint = "http://localhost:4200/api/puzzles/published";

        [Header("Remote Manifest & Connectivity")]
        [Tooltip("Live remote API endpoint returning the published JSON puzzle catalog (e.g. GitHub Pages or port 4200).")]
        public string manifestApiEndpoint = DefaultLiveBackendEndpoint;

        [Tooltip("Optional local backend development server endpoint (port 4200).")]
        public string localBackendEndpoint = DefaultLocalServerEndpoint;

        [Tooltip("Interval in seconds for the native background monitor to actively verify internet connectivity.")]
        public int connectivityCheckIntervalSeconds = 5;

        [Tooltip("Automatically begin downloading queued manifest images as soon as internet connectivity is detected.")]
        public bool autoStartOnConnectivity = true;

        [Header("Concurrency & Retry Policy")]
        [Tooltip("Number of concurrent background download + JPEG transcode workers in the native priority queue.")]
        [Range(1, 8)]
        public int maxConcurrentDownloads = 3;

        [Tooltip("Maximum automatic retries for transient network errors before marking a request string as faulty.")]
        [Range(0, 5)]
        public int maxRetryCount = 2;

        [Tooltip("Automatically purge faulty/corrupt/404 request strings from the priority queue.")]
        public bool autoRemoveFaultyRequests = true;

        [Header("Default Resolution Tiers & JPEG Compression")]
        [Tooltip("Small icon resolution generated automatically upon initial download (default: 256px, ~30-100KB).")]
        public int defaultIconSizePx = (int)ImageResolutionTier.Icon256;

        [Tooltip("Full-screen puzzle resolution generated automatically upon initial download (default: 1080px, ~1-3MB).")]
        public int defaultFullSizePx = (int)ImageResolutionTier.Full1080;

        [Tooltip("JPEG compression quality (1-100). 85 strips alpha and shrinks 5MB PNGs to ~500KB JPGs with negligible visual loss.")]
        [Range(50, 100)]
        public int jpegQuality = 85;

        [Tooltip("Solid background color used when flattening transparent PNGs into opaque JPEGs.")]
        public Color pngTransparencyFillColor = Color.white;

        [Header("Cache & ROM Storage Budgets")]
        [Tooltip("Maximum disk budget (in MB) for Volatile Cache before LRU eviction cleans up old unpromoted images.")]
        [Range(32, 2048)]
        public int maxVolatileCacheSizeMB = 200;

        [Tooltip("Maximum number of active Sprites/Texture2Ds kept in Unity RAM pool before releasing unused textures.")]
        [Range(10, 500)]
        public int maxRamSpritePoolCount = 64;

        /// <summary>
        /// Creates a runtime default configuration instance when no ScriptableObject asset is assigned.
        /// </summary>
        public static ImagePipelineConfig CreateDefault(string manifestEndpoint = null)
        {
            var config = CreateInstance<ImagePipelineConfig>();
            config.manifestApiEndpoint = !string.IsNullOrWhiteSpace(manifestEndpoint)
                ? manifestEndpoint
                : DefaultLiveBackendEndpoint;
            return config;
        }
    }
}

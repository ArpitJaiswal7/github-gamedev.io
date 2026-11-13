using System;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Payload returned from the native layer (Android Java, iOS .mm, WebGL .jslib, or Editor)
    /// when an image download + dual-tier JPEG conversion (256px & 1080px) completes.
    /// </summary>
    [Serializable]
    public struct NativeDownloadResult
    {
        public string requestKey;
        public bool success;
        public long iconSizeBytes;
        public long fullSizeBytes;
        public long durationMs;
        public int storageTier; // 0 = VolatileCache, 1 = PermanentROM
        public string errorMessage;
    }

    /// <summary>
    /// Payload returned from the native layer when a custom-size helper request (e.g., 512px) finishes.
    /// </summary>
    [Serializable]
    public struct NativeCustomSizeResult
    {
        public string requestKey;
        public int sizePx;
        public bool success;
        public long sizeBytes;
        public long durationMs;
        public string errorMessage;
    }

    /// <summary>
    /// Platform-agnostic contract implemented by:
    /// - AndroidImageBridge (JNI -> NativeImageBridge.java)
    /// - IOSImageBridge (P/Invoke -> NativeImageBridge.mm)
    /// - WebGLImageBridge (P/Invoke -> NativeImageBridge.jslib / OffscreenCanvas + IndexedDB)
    /// - EditorImageBridge (C# Task + HttpClient simulator for Unity Editor)
    /// </summary>
    public interface INativeImageBridge : IDisposable
    {
        /// <summary>
        /// Fired when the native connectivity monitor detects a change in internet reachability.
        /// True = Online (queue auto-resumes), False = Offline (queue auto-pauses).
        /// </summary>
        event Action<bool> OnConnectivityChanged;

        /// <summary>
        /// Fired on the Unity main thread when a queued image finishes downloading and converting
        /// into both default tiers (256px Icon and 1080px FullSize JPEGs).
        /// </summary>
        event Action<NativeDownloadResult> OnDownloadCompleted;

        /// <summary>
        /// Fired on the Unity main thread when a custom resolution helper call (e.g., 512px) completes.
        /// </summary>
        event Action<NativeCustomSizeResult> OnCustomSizeCompleted;

        /// <summary>
        /// Current online/offline status reported by the native connectivity monitor.
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// Initializes the native background worker pool, priority queue, and active connectivity monitor.
        /// </summary>
        /// <param name="cacheDirPath">OS path for Volatile Cache (ignored on WebGL which uses IndexedDB volatile store).</param>
        /// <param name="romDirPath">OS path for Permanent ROM (ignored on WebGL which uses IndexedDB persistent store).</param>
        /// <param name="config">Pipeline configuration parameters (intervals, quality, worker count).</param>
        void Initialize(string cacheDirPath, string romDirPath, ImagePipelineConfig config);

        /// <summary>
        /// Enqueues an image URL identified by requestKey into the native priority queue.
        /// Upon download, the native layer automatically strips transparency, converts PNG -> JPG,
        /// and saves both 256px (icon) and 1080px (full size) tiers.
        /// </summary>
        void EnqueueRequest(string requestKey, string url, RequestPriority priority, StorageTier targetStorage);

        /// <summary>
        /// Dynamically updates the priority of an in-flight/queued request (e.g. boosting visible ScrollView items).
        /// </summary>
        void UpdatePriority(string requestKey, RequestPriority newPriority);

        /// <summary>
        /// Cancels a queued/downloading request and removes faulty/corrupt request files if present.
        /// </summary>
        void CancelAndRemoveRequest(string requestKey);

        /// <summary>
        /// Helper method: Generates a custom JPEG resolution (e.g., 512px) for the specified requestKey.
        /// If the 1080px JPEG is already in ROM or Cache, the native plugin downscales directly from disk
        /// without re-downloading from the network.
        /// </summary>
        void RequestCustomSize(string requestKey, string fallbackUrl, int customSizePx, RequestPriority priority);

        /// <summary>
        /// Moves all generated JPEG tiers (256px, 1080px, custom) for requestKey from Volatile Cache
        /// into Permanent ROM storage and deletes the Volatile Cache copy.
        /// </summary>
        bool PromoteToROM(string requestKey);

        /// <summary>
        /// Moves all generated JPEG tiers for requestKey from Permanent ROM back into Volatile Cache
        /// (freeing permanent storage after a puzzle is completed).
        /// </summary>
        bool DemoteToCache(string requestKey);

        /// <summary>
        /// Deletes the cached files for a specific requestKey from the specified storage tier.
        /// </summary>
        bool DeleteFiles(string requestKey, StorageTier tier);

        /// <summary>
        /// Checks if a specific resolution JPEG (e.g., 256, 1080, 512) already exists in ROM or Cache,
        /// and returns its byte size on disk (or >0 if present).
        /// </summary>
        bool HasCachedFile(string requestKey, int sizePx, out StorageTier foundInTier, out long fileSizeBytes);

        /// <summary>
        /// Asynchronously loads the compressed JPEG bytes for (requestKey, sizePx) from ROM first, then Cache.
        /// Uses an async callback so Android/iOS file reads and WebGL IndexedDB reads share the exact same API.
        /// </summary>
        void ReadImageBytesAsync(string requestKey, int sizePx, Action<byte[]> onBytesLoaded, Action<string> onError);

        /// <summary>
        /// Clears all files in the Volatile Cache directory/store while leaving Permanent ROM untouched.
        /// </summary>
        void ClearVolatileCache();
    }
}

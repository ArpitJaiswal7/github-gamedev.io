using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Tracks the complete lifecycle, storage location, file sizes (256px, 1080px, and custom tiers),
    /// download/transcode duration, and error state for a single puzzle image request string.
    /// </summary>
    [Serializable]
    public sealed class ImageRequestRecord
    {
        public const int DefaultIconSize = (int)ImageResolutionTier.Icon256;
        public const int DefaultFullSize = (int)ImageResolutionTier.Full1080;

        [SerializeField] private string requestKey;
        [SerializeField] private string sourceUrl;
        [SerializeField] private RequestState state;
        [SerializeField] private RequestPriority priority;
        [SerializeField] private StorageTier storageTier;
        [SerializeField] private long iconSizeBytes;
        [SerializeField] private long fullSizeBytes;
        [SerializeField] private long durationMs;
        [SerializeField] private int retryCount;
        [SerializeField] private string errorMessage;
        [SerializeField] private long lastUpdatedUtcTicks;

        // Tracks any additional custom resolutions generated via helper functions (e.g., 512px -> byte size)
        private readonly Dictionary<int, long> customResolutionByteSizes = new Dictionary<int, long>();

        /// <summary>
        /// Deterministic string identifier for this puzzle image URL.
        /// </summary>
        public string RequestKey => requestKey;

        /// <summary>
        /// Original remote URL for the source image.
        /// </summary>
        public string SourceUrl => sourceUrl;

        /// <summary>
        /// Current lifecycle state (Queued, Downloading, Processing, Cached, SavedInROM, Failed, Cancelled).
        /// </summary>
        public RequestState State => state;

        /// <summary>
        /// Current queue priority (Low, Normal, High for viewport, Critical for active puzzle).
        /// </summary>
        public RequestPriority Priority => priority;

        /// <summary>
        /// Current physical storage location (VolatileCache vs PermanentROM).
        /// </summary>
        public StorageTier StorageTier => storageTier;

        /// <summary>
        /// File size in bytes of the converted 256px JPEG icon (~30KB - 100KB).
        /// </summary>
        public long IconSizeBytes => iconSizeBytes;

        /// <summary>
        /// File size in bytes of the converted 1080px full-size JPEG (~500KB - 2.5MB).
        /// </summary>
        public long FullSizeBytes => fullSizeBytes;

        /// <summary>
        /// Total native download + PNG-to-JPG transcode & resize duration in milliseconds.
        /// </summary>
        public long DurationMs => durationMs;

        /// <summary>
        /// Number of retry attempts if transient network failures occurred.
        /// </summary>
        public int RetryCount => retryCount;

        /// <summary>
        /// Error description if the request failed (e.g., HTTP 404, corrupt image stream).
        /// </summary>
        public string ErrorMessage => errorMessage;

        /// <summary>
        /// True if both default tiers (256px and 1080px) are downloaded, converted, and ready on disk/IndexedDB.
        /// </summary>
        public bool IsReady => state == RequestState.Cached || state == RequestState.SavedInROM;

        /// <summary>
        /// True if the request is currently queued or actively downloading/processing in native code.
        /// </summary>
        public bool IsInFlight => state == RequestState.Queued || state == RequestState.Downloading || state == RequestState.Processing;

        /// <summary>
        /// Read-only view of any custom resolution sizes (e.g., 512px) generated for this image and their byte sizes.
        /// </summary>
        public IReadOnlyDictionary<int, long> CustomResolutionByteSizes => customResolutionByteSizes;

        public ImageRequestRecord(
            string requestKey,
            string sourceUrl,
            RequestPriority initialPriority = RequestPriority.Low,
            StorageTier initialStorage = StorageTier.VolatileCache)
        {
            if (string.IsNullOrWhiteSpace(requestKey))
                throw new ArgumentException("RequestKey cannot be null or empty.", nameof(requestKey));
            if (string.IsNullOrWhiteSpace(sourceUrl))
                throw new ArgumentException("SourceUrl cannot be null or empty.", nameof(sourceUrl));

            this.requestKey = requestKey;
            this.sourceUrl = sourceUrl;
            this.priority = initialPriority;
            this.storageTier = initialStorage;
            this.state = RequestState.Queued;
            this.lastUpdatedUtcTicks = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Checks whether a specific pixel resolution (256, 1080, or custom e.g. 512) has been generated and stored.
        /// </summary>
        public bool HasResolution(int sizePx)
        {
            if (!IsReady)
                return false;

            if (sizePx == DefaultIconSize)
                return iconSizeBytes > 0;
            if (sizePx == DefaultFullSize)
                return fullSizeBytes > 0;

            return customResolutionByteSizes.TryGetValue(sizePx, out long bytes) && bytes > 0;
        }

        /// <summary>
        /// Returns the recorded byte size on disk for the requested resolution tier.
        /// </summary>
        public long GetResolutionSizeBytes(int sizePx)
        {
            if (sizePx == DefaultIconSize)
                return iconSizeBytes;
            if (sizePx == DefaultFullSize)
                return fullSizeBytes;

            return customResolutionByteSizes.TryGetValue(sizePx, out long bytes) ? bytes : 0L;
        }

        internal void SetPriority(RequestPriority newPriority)
        {
            priority = newPriority;
            Touch();
        }

        internal void MarkDownloading()
        {
            state = RequestState.Downloading;
            errorMessage = null;
            Touch();
        }

        internal void MarkProcessing()
        {
            state = RequestState.Processing;
            Touch();
        }

        internal void MarkCompletedDefaultTiers(long iconBytes, long fullBytes, long elapsedMs, StorageTier targetStorage)
        {
            iconSizeBytes = Math.Max(0L, iconBytes);
            fullSizeBytes = Math.Max(0L, fullBytes);
            durationMs = Math.Max(0L, elapsedMs);
            storageTier = targetStorage;
            state = targetStorage == StorageTier.PermanentROM ? RequestState.SavedInROM : RequestState.Cached;
            errorMessage = null;
            Touch();
        }

        internal void RecordCustomResolution(int sizePx, long sizeBytes, long elapsedMs)
        {
            if (sizePx > 0 && sizeBytes > 0)
            {
                customResolutionByteSizes[sizePx] = sizeBytes;
            }
            if (elapsedMs > 0)
            {
                durationMs += elapsedMs;
            }
            Touch();
        }

        internal void MarkPromotedToROM()
        {
            storageTier = StorageTier.PermanentROM;
            if (state == RequestState.Cached)
            {
                state = RequestState.SavedInROM;
            }
            Touch();
        }

        internal void MarkDemotedToCache()
        {
            storageTier = StorageTier.VolatileCache;
            if (state == RequestState.SavedInROM)
            {
                state = RequestState.Cached;
            }
            Touch();
        }

        internal void MarkFailed(string error, bool incrementRetry = true)
        {
            state = RequestState.Failed;
            errorMessage = string.IsNullOrWhiteSpace(error) ? "Unknown native pipeline error" : error;
            if (incrementRetry)
            {
                retryCount++;
            }
            Touch();
        }

        internal void MarkCancelled()
        {
            state = RequestState.Cancelled;
            Touch();
        }

        private void Touch()
        {
            lastUpdatedUtcTicks = DateTime.UtcNow.Ticks;
        }
    }
}

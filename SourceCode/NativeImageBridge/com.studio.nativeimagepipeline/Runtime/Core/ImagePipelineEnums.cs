using System;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Standard resolution tiers generated automatically upon image download,
    /// plus support for custom sizes via helper functions.
    /// </summary>
    public enum ImageResolutionTier
    {
        /// <summary>
        /// Small icon tier (256px max dimension, ~30KB - 100KB JPEG).
        /// Generated automatically on initial download.
        /// </summary>
        Icon256 = 256,

        /// <summary>
        /// Medium square tile tier (512px max dimension, ~120KB - 250KB JPEG).
        /// </summary>
        Tile512 = 512,

        /// <summary>
        /// Full-screen puzzle resolution tier (1080px max dimension, ~500KB - 2.0MB JPEG).
        /// Generated automatically on initial download.
        /// </summary>
        Full1080 = 1080,

        /// <summary>
        /// Ultra HD 2K puzzle resolution tier (2048px max dimension, ~1.2MB - 3.5MB JPEG)
        /// for high-DPI tablets, iPads, zooming in on 100+ piece boards, or crisp WebGL/Desktop displays.
        /// </summary>
        Ultra2048 = 2048,

        /// <summary>
        /// Custom arbitrary resolution requested via helper methods.
        /// </summary>
        Custom = 0
    }

    /// <summary>
    /// Priority levels for the native background download & transcode queue.
    /// Higher values are dequeued first.
    /// </summary>
    public enum RequestPriority
    {
        /// <summary>
        /// Background manifest prefetch when the game starts or off-screen gallery items.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Standard on-demand request.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Currently visible in the ScrollView viewport (promoted dynamically as the user scrolls).
        /// </summary>
        High = 2,

        /// <summary>
        /// Active puzzle image selected by the player for immediate gameplay.
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Lifecycle state of an individual image request identified by its RequestKey string.
    /// </summary>
    public enum RequestState
    {
        None = 0,
        Queued = 1,
        Downloading = 2,
        Processing = 3,
        Cached = 4,
        SavedInROM = 5,
        Failed = 6,
        Cancelled = 7
    }

    /// <summary>
    /// Physical storage tier across Android, iOS, WebGL (IndexedDB), and Unity Editor.
    /// </summary>
    public enum StorageTier
    {
        /// <summary>
        /// Volatile device cache (OS Cache Dir / IndexedDB volatile store).
        /// Used for initial background downloads; can be evicted when cache budget is exceeded.
        /// </summary>
        VolatileCache = 0,

        /// <summary>
        /// Permanent ROM / Hard-drive storage (OS Files Dir / AppSupport without iCloud backup / IndexedDB persistent store).
        /// Files promoted to ROM are deleted from VolatileCache and retrieved directly from ROM.
        /// </summary>
        PermanentROM = 1
    }
}

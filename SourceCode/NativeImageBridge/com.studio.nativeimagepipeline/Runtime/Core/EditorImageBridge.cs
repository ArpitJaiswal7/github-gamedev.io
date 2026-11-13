using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Unity Editor & Standalone Simulator implementation of INativeImageBridge.
    /// Allows testing the exact same priority queue, active connectivity monitoring (including
    /// simulated offline/online toggles), PNG->JPG alpha stripping, 256px/1080px/custom resizing,
    /// and Cache vs. Permanent ROM promotion directly inside the Unity Editor on Windows/Mac.
    /// </summary>
    public sealed class EditorImageBridge : INativeImageBridge
    {
        private sealed class QueuedTaskItem
        {
            public string RequestKey;
            public string Url;
            public int Priority;
            public StorageTier TargetStorage;
            public long Sequence;
            public bool IsCustomSize;
            public int CustomSizePx;
            public bool Cancelled;
        }

        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private string cacheDirPath;
        private string romDirPath;
        private ImagePipelineConfig config;
        private NativeBridgeCallbackReceiver callbackReceiver;

        private readonly object queueLock = new object();
        private readonly List<QueuedTaskItem> priorityList = new List<QueuedTaskItem>();
        private readonly Dictionary<string, QueuedTaskItem> activeByKey = new Dictionary<string, QueuedTaskItem>();

        private int activeWorkerCount;
        private long sequenceCounter;
        private bool isDisposed;
        private bool lastOnlineState = true;
        private float nextConnectivityCheckTime;

        /// <summary>
        /// Editor-only testing toggle: set this to true in the Inspector or test harness
        /// to simulate losing internet connection and verify the queue pauses/resumes automatically.
        /// </summary>
        public static bool SimulateOfflineMode { get; set; } = false;

        public event Action<bool> OnConnectivityChanged;
        public event Action<NativeDownloadResult> OnDownloadCompleted;
        public event Action<NativeCustomSizeResult> OnCustomSizeCompleted;

        public bool IsOnline => !SimulateOfflineMode &&
                                Application.internetReachability != NetworkReachability.NotReachable;

        public void Initialize(string cacheDirPath, string romDirPath, ImagePipelineConfig config)
        {
            this.cacheDirPath = cacheDirPath;
            this.romDirPath = romDirPath;
            this.config = config;
            DiskStorageHelper.EnsureDirectories(cacheDirPath, romDirPath);

            callbackReceiver = NativeBridgeCallbackReceiver.EnsureInstance();
            lastOnlineState = IsOnline;

            // Start periodic connectivity & queue pump loop on main thread via receiver
            StartConnectivityMonitorLoop();
        }

        public void EnqueueRequest(string requestKey, string url, RequestPriority priority, StorageTier targetStorage)
        {
            if (string.IsNullOrWhiteSpace(requestKey) || string.IsNullOrWhiteSpace(url))
                return;

            // Fast-path: check if both 256px and 1080px already exist in ROM or Cache
            if (DiskStorageHelper.TryFindExistingFile(cacheDirPath, romDirPath, requestKey, config.defaultIconSizePx, out _, out StorageTier foundTier, out long iconBytes) &&
                DiskStorageHelper.TryFindExistingFile(cacheDirPath, romDirPath, requestKey, config.defaultFullSizePx, out _, out _, out long fullBytes))
            {
                callbackReceiver.EnqueueMainThread(() =>
                {
                    OnDownloadCompleted?.Invoke(new NativeDownloadResult
                    {
                        requestKey = requestKey,
                        success = true,
                        iconSizeBytes = iconBytes,
                        fullSizeBytes = fullBytes,
                        durationMs = 0L,
                        storageTier = (int)foundTier,
                        errorMessage = string.Empty
                    });
                });
                return;
            }

            lock (queueLock)
            {
                if (activeByKey.TryGetValue(requestKey, out var existing))
                {
                    if ((int)priority > existing.Priority)
                    {
                        existing.Priority = (int)priority;
                        SortPriorityQueueUnsafe();
                    }
                    return;
                }

                var item = new QueuedTaskItem
                {
                    RequestKey = requestKey,
                    Url = url,
                    Priority = (int)priority,
                    TargetStorage = targetStorage,
                    Sequence = Interlocked.Increment(ref sequenceCounter)
                };

                activeByKey[requestKey] = item;
                priorityList.Add(item);
                SortPriorityQueueUnsafe();
            }

            PumpQueue();
        }

        public void UpdatePriority(string requestKey, RequestPriority newPriority)
        {
            lock (queueLock)
            {
                if (activeByKey.TryGetValue(requestKey, out var existing))
                {
                    existing.Priority = (int)newPriority;
                    SortPriorityQueueUnsafe();
                }
            }
        }

        public void CancelAndRemoveRequest(string requestKey)
        {
            lock (queueLock)
            {
                if (activeByKey.TryGetValue(requestKey, out var existing))
                {
                    existing.Cancelled = true;
                    activeByKey.Remove(requestKey);
                    priorityList.Remove(existing);
                }
            }
            DiskStorageHelper.DeleteAllTiers(cacheDirPath, requestKey);
        }

        public void RequestCustomSize(string requestKey, string fallbackUrl, int customSizePx, RequestPriority priority)
        {
            if (string.IsNullOrWhiteSpace(requestKey) || customSizePx <= 0)
                return;

            if (DiskStorageHelper.TryFindExistingFile(cacheDirPath, romDirPath, requestKey, customSizePx, out _, out _, out long existingBytes))
            {
                callbackReceiver.EnqueueMainThread(() =>
                {
                    OnCustomSizeCompleted?.Invoke(new NativeCustomSizeResult
                    {
                        requestKey = requestKey,
                        sizePx = customSizePx,
                        success = true,
                        sizeBytes = existingBytes,
                        durationMs = 0L,
                        errorMessage = string.Empty
                    });
                });
                return;
            }

            lock (queueLock)
            {
                var item = new QueuedTaskItem
                {
                    RequestKey = requestKey,
                    Url = fallbackUrl,
                    Priority = (int)priority,
                    IsCustomSize = true,
                    CustomSizePx = customSizePx,
                    Sequence = Interlocked.Increment(ref sequenceCounter)
                };
                priorityList.Add(item);
                SortPriorityQueueUnsafe();
            }

            PumpQueue();
        }

        public bool PromoteToROM(string requestKey) =>
            DiskStorageHelper.MoveAllTiers(cacheDirPath, romDirPath, requestKey);

        public bool DemoteToCache(string requestKey) =>
            DiskStorageHelper.MoveAllTiers(romDirPath, cacheDirPath, requestKey);

        public bool DeleteFiles(string requestKey, StorageTier tier)
        {
            string dir = tier == StorageTier.PermanentROM ? romDirPath : cacheDirPath;
            return DiskStorageHelper.DeleteAllTiers(dir, requestKey);
        }

        public bool HasCachedFile(string requestKey, int sizePx, out StorageTier foundInTier, out long fileSizeBytes) =>
            DiskStorageHelper.TryFindExistingFile(cacheDirPath, romDirPath, requestKey, sizePx, out _, out foundInTier, out fileSizeBytes);

        public void ReadImageBytesAsync(string requestKey, int sizePx, Action<byte[]> onBytesLoaded, Action<string> onError)
        {
            if (DiskStorageHelper.TryFindExistingFile(cacheDirPath, romDirPath, requestKey, sizePx, out string fullPath, out _, out _))
            {
                DiskStorageHelper.ReadFileBytesAsync(fullPath, callbackReceiver, onBytesLoaded, onError);
            }
            else
            {
                callbackReceiver.EnqueueMainThread(() =>
                    onError?.Invoke($"Editor file not found in ROM or Cache for key='{requestKey}', size={sizePx}px"));
            }
        }

        public void ClearVolatileCache()
        {
            if (Directory.Exists(cacheDirPath))
            {
                foreach (string file in Directory.GetFiles(cacheDirPath, "*.jpg"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        private async void StartConnectivityMonitorLoop()
        {
            int intervalMs = Mathf.Max(1, config.connectivityCheckIntervalSeconds) * 1000;
            while (!isDisposed)
            {
                await Task.Delay(intervalMs);
                if (isDisposed) break;

                bool currentOnline = IsOnline;
                if (currentOnline != lastOnlineState)
                {
                    lastOnlineState = currentOnline;
                    OnConnectivityChanged?.Invoke(currentOnline);
                }

                if (currentOnline)
                {
                    PumpQueue();
                }
            }
        }

        private void PumpQueue()
        {
            if (isDisposed || !IsOnline)
                return;

            lock (queueLock)
            {
                int maxWorkers = Mathf.Max(1, config.maxConcurrentDownloads);
                while (activeWorkerCount < maxWorkers && priorityList.Count > 0)
                {
                    var next = priorityList[0];
                    priorityList.RemoveAt(0);
                    if (next.Cancelled)
                        continue;

                    activeWorkerCount++;
                    _ = ExecuteQueueItemAsync(next);
                }
            }
        }

        private async Task ExecuteQueueItemAsync(QueuedTaskItem item)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (item.IsCustomSize)
                {
                    await ProcessCustomSizeItemAsync(item, sw);
                }
                else
                {
                    await ProcessStandardDownloadItemAsync(item, sw);
                }
            }
            finally
            {
                lock (queueLock)
                {
                    if (!item.IsCustomSize)
                    {
                        activeByKey.Remove(item.RequestKey);
                    }
                    activeWorkerCount = Math.Max(0, activeWorkerCount - 1);
                }
                callbackReceiver.EnqueueMainThread(PumpQueue);
            }
        }

        private async Task ProcessStandardDownloadItemAsync(QueuedTaskItem item, Stopwatch sw)
        {
            byte[] rawBytes = null;
            string lastError = string.Empty;

            for (int attempt = 0; attempt <= config.maxRetryCount; attempt++)
            {
                if (item.Cancelled || !IsOnline)
                    return;

                try
                {
                    rawBytes = await SharedHttpClient.GetByteArrayAsync(item.Url);
                    if (rawBytes != null && rawBytes.Length > 0)
                        break;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    if (lastError.Contains("404"))
                        break;
                    await Task.Delay(400 * (attempt + 1));
                }
            }

            if (item.Cancelled)
                return;

            if (rawBytes == null || rawBytes.Length == 0)
            {
                sw.Stop();
                OnDownloadCompleted?.Invoke(new NativeDownloadResult
                {
                    requestKey = item.RequestKey,
                    success = false,
                    durationMs = sw.ElapsedMilliseconds,
                    errorMessage = string.IsNullOrEmpty(lastError) ? "Failed downloading bytes" : lastError
                });
                return;
            }

            // Decode, strip alpha, and resize to 1080px and 256px JPEGs
            string targetDir = item.TargetStorage == StorageTier.PermanentROM ? romDirPath : cacheDirPath;
            string fullPath = DiskStorageHelper.GetFilePath(targetDir, item.RequestKey, config.defaultFullSizePx);
            string iconPath = DiskStorageHelper.GetFilePath(targetDir, item.RequestKey, config.defaultIconSizePx);

            bool ok = TranscodeAndWriteDualTiersOnMainThread(
                rawBytes,
                config.defaultFullSizePx,
                config.defaultIconSizePx,
                config.jpegQuality,
                config.pngTransparencyFillColor,
                fullPath,
                iconPath,
                out long fullBytesLen,
                out long iconBytesLen,
                out string encodeError);

            sw.Stop();

            if (!ok)
            {
                OnDownloadCompleted?.Invoke(new NativeDownloadResult
                {
                    requestKey = item.RequestKey,
                    success = false,
                    durationMs = sw.ElapsedMilliseconds,
                    errorMessage = encodeError
                });
                return;
            }

            OnDownloadCompleted?.Invoke(new NativeDownloadResult
            {
                requestKey = item.RequestKey,
                success = true,
                iconSizeBytes = iconBytesLen,
                fullSizeBytes = fullBytesLen,
                durationMs = sw.ElapsedMilliseconds,
                storageTier = (int)item.TargetStorage,
                errorMessage = string.Empty
            });
        }

        private async Task ProcessCustomSizeItemAsync(QueuedTaskItem item, Stopwatch sw)
        {
            byte[] sourceBytes = null;
            string targetDir = cacheDirPath;

            bool hasLocalFull = DiskStorageHelper.TryFindExistingFile(
                cacheDirPath,
                romDirPath,
                item.RequestKey,
                config.defaultFullSizePx,
                out string existingFullPath,
                out StorageTier foundTier,
                out _);

            if (hasLocalFull)
            {
                targetDir = foundTier == StorageTier.PermanentROM ? romDirPath : cacheDirPath;
            }

            if (item.CustomSizePx <= config.defaultFullSizePx && hasLocalFull)
            {
                sourceBytes = await Task.Run(() => File.ReadAllBytes(existingFullPath));
            }
            else if (!string.IsNullOrWhiteSpace(item.Url))
            {
                try
                {
                    sourceBytes = await SharedHttpClient.GetByteArrayAsync(item.Url);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    OnCustomSizeCompleted?.Invoke(new NativeCustomSizeResult
                    {
                        requestKey = item.RequestKey,
                        sizePx = item.CustomSizePx,
                        success = false,
                        durationMs = sw.ElapsedMilliseconds,
                        errorMessage = ex.Message
                    });
                    return;
                }
            }
            else if (hasLocalFull)
            {
                sourceBytes = await Task.Run(() => File.ReadAllBytes(existingFullPath));
            }

            string customPath = DiskStorageHelper.GetFilePath(targetDir, item.RequestKey, item.CustomSizePx);
            bool ok = TranscodeSingleTierOnMainThread(
                sourceBytes,
                item.CustomSizePx,
                config.jpegQuality,
                config.pngTransparencyFillColor,
                customPath,
                out long writtenBytes,
                out string err);

            sw.Stop();
            OnCustomSizeCompleted?.Invoke(new NativeCustomSizeResult
            {
                requestKey = item.RequestKey,
                sizePx = item.CustomSizePx,
                success = ok,
                sizeBytes = writtenBytes,
                durationMs = sw.ElapsedMilliseconds,
                errorMessage = err ?? string.Empty
            });
        }

        private static bool TranscodeAndWriteDualTiersOnMainThread(
            byte[] rawSourceBytes,
            int fullMaxPx,
            int iconMaxPx,
            int jpegQuality,
            Color fillColor,
            string fullOutputPath,
            string iconOutputPath,
            out long fullSizeBytes,
            out long iconSizeBytes,
            out string error)
        {
            fullSizeBytes = 0L;
            iconSizeBytes = 0L;
            error = string.Empty;

            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!srcTex.LoadImage(rawSourceBytes, false))
                {
                    error = "Corrupt or unsupported image format";
                    return false;
                }

                byte[] fullJpeg = ResizeAndEncodeToJpeg(srcTex, fullMaxPx, jpegQuality, fillColor);
                byte[] iconJpeg = ResizeAndEncodeToJpeg(srcTex, iconMaxPx, jpegQuality, fillColor);

                File.WriteAllBytes(fullOutputPath, fullJpeg);
                File.WriteAllBytes(iconOutputPath, iconJpeg);

                fullSizeBytes = fullJpeg.Length;
                iconSizeBytes = iconJpeg.Length;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                UnityEngine.Object.Destroy(srcTex);
            }
        }

        private static bool TranscodeSingleTierOnMainThread(
            byte[] rawSourceBytes,
            int targetMaxPx,
            int jpegQuality,
            Color fillColor,
            string outputPath,
            out long writtenBytes,
            out string error)
        {
            writtenBytes = 0L;
            error = string.Empty;
            if (rawSourceBytes == null || rawSourceBytes.Length == 0)
            {
                error = "Empty source bytes for custom size";
                return false;
            }

            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!srcTex.LoadImage(rawSourceBytes, false))
                {
                    error = "Failed decoding source image for custom size";
                    return false;
                }

                byte[] jpeg = ResizeAndEncodeToJpeg(srcTex, targetMaxPx, jpegQuality, fillColor);
                File.WriteAllBytes(outputPath, jpeg);
                writtenBytes = jpeg.Length;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                UnityEngine.Object.Destroy(srcTex);
            }
        }

        private static byte[] ResizeAndEncodeToJpeg(Texture2D src, int maxDimensionPx, int quality, Color bgFill)
        {
            int w = src.width;
            int h = src.height;
            int maxSide = Mathf.Max(w, h);
            int targetW = w;
            int targetH = h;

            if (maxSide > maxDimensionPx && maxDimensionPx > 0)
            {
                float ratio = (float)maxDimensionPx / maxSide;
                targetW = Mathf.Max(1, Mathf.RoundToInt(w * ratio));
                targetH = Mathf.Max(1, Mathf.RoundToInt(h * ratio));
            }

            RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            Texture2D dst = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);

            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, bgFill);
                Graphics.Blit(src, rt);
                dst.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
                dst.Apply(false, false);
                return dst.EncodeToJPG(Mathf.Clamp(quality, 50, 100));
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(dst);
            }
        }

        private void SortPriorityQueueUnsafe()
        {
            priorityList.Sort((a, b) =>
            {
                int cmp = b.Priority.CompareTo(a.Priority);
                return cmp != 0 ? cmp : a.Sequence.CompareTo(b.Sequence);
            });
        }

        public void Dispose()
        {
            isDisposed = true;
            lock (queueLock)
            {
                priorityList.Clear();
                activeByKey.Clear();
            }
        }
    }
}

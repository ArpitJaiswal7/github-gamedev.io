using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Unity C# P/Invoke wrapper around NativeImageBridge.mm (iOS Objective-C++ plugin).
    /// Manages NWPathMonitor connectivity auto-pause/resume, NSOperationQueue priority downloads,
    /// hardware-accelerated CGImageSource PNG->JPG resizing (256px/1080px/custom), and ROM storage.
    /// </summary>
    public sealed class IOSImageBridge : INativeImageBridge
    {
        private string cacheDirPath;
        private string romDirPath;
        private NativeBridgeCallbackReceiver callbackReceiver;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NIP_Initialize(
            string cacheDir,
            string romDir,
            int checkIntervalSec,
            int maxConcurrentWorkers,
            int iconSizePx,
            int fullSizePx,
            int jpegQuality,
            int maxRetries);

        [DllImport("__Internal")]
        private static extern bool NIP_IsConnected();

        [DllImport("__Internal")]
        private static extern void NIP_EnqueueDownload(
            string requestKey,
            string url,
            int priority,
            int targetStorageTier);

        [DllImport("__Internal")]
        private static extern void NIP_UpdatePriority(string requestKey, int newPriority);

        [DllImport("__Internal")]
        private static extern void NIP_CancelOrRemoveRequest(string requestKey);

        [DllImport("__Internal")]
        private static extern void NIP_RequestCustomSize(
            string requestKey,
            string fallbackUrl,
            int customSizePx,
            int priority);
#endif

        public event Action<bool> OnConnectivityChanged;
        public event Action<NativeDownloadResult> OnDownloadCompleted;
        public event Action<NativeCustomSizeResult> OnCustomSizeCompleted;

        public bool IsOnline
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return NIP_IsConnected();
#else
                return Application.internetReachability != NetworkReachability.NotReachable;
#endif
            }
        }

        public void Initialize(string cacheDirPath, string romDirPath, ImagePipelineConfig config)
        {
            this.cacheDirPath = cacheDirPath;
            this.romDirPath = romDirPath;
            DiskStorageHelper.EnsureDirectories(cacheDirPath, romDirPath);

            callbackReceiver = NativeBridgeCallbackReceiver.EnsureInstance();
            callbackReceiver.OnNativeConnectivityChanged += HandleConnectivityChanged;
            callbackReceiver.OnNativeDownloadCompleted += HandleDownloadCompleted;
            callbackReceiver.OnNativeCustomSizeCompleted += HandleCustomSizeCompleted;

#if UNITY_IOS && !UNITY_EDITOR
            NIP_Initialize(
                cacheDirPath,
                romDirPath,
                config.connectivityCheckIntervalSeconds,
                config.maxConcurrentDownloads,
                config.defaultIconSizePx,
                config.defaultFullSizePx,
                config.jpegQuality,
                config.maxRetryCount);
#endif
        }

        public void EnqueueRequest(string requestKey, string url, RequestPriority priority, StorageTier targetStorage)
        {
#if UNITY_IOS && !UNITY_EDITOR
            NIP_EnqueueDownload(requestKey, url, (int)priority, (int)targetStorage);
#endif
        }

        public void UpdatePriority(string requestKey, RequestPriority newPriority)
        {
#if UNITY_IOS && !UNITY_EDITOR
            NIP_UpdatePriority(requestKey, (int)newPriority);
#endif
        }

        public void CancelAndRemoveRequest(string requestKey)
        {
#if UNITY_IOS && !UNITY_EDITOR
            NIP_CancelOrRemoveRequest(requestKey);
#endif
            DiskStorageHelper.DeleteAllTiers(cacheDirPath, requestKey);
        }

        public void RequestCustomSize(string requestKey, string fallbackUrl, int customSizePx, RequestPriority priority)
        {
#if UNITY_IOS && !UNITY_EDITOR
            NIP_RequestCustomSize(requestKey, fallbackUrl ?? string.Empty, customSizePx, (int)priority);
#endif
        }

        public bool PromoteToROM(string requestKey)
        {
            return DiskStorageHelper.MoveAllTiers(cacheDirPath, romDirPath, requestKey);
        }

        public bool DemoteToCache(string requestKey)
        {
            return DiskStorageHelper.MoveAllTiers(romDirPath, cacheDirPath, requestKey);
        }

        public bool DeleteFiles(string requestKey, StorageTier tier)
        {
            string targetDir = tier == StorageTier.PermanentROM ? romDirPath : cacheDirPath;
            return DiskStorageHelper.DeleteAllTiers(targetDir, requestKey);
        }

        public bool HasCachedFile(string requestKey, int sizePx, out StorageTier foundInTier, out long fileSizeBytes)
        {
            return DiskStorageHelper.TryFindExistingFile(
                cacheDirPath,
                romDirPath,
                requestKey,
                sizePx,
                out _,
                out foundInTier,
                out fileSizeBytes);
        }

        public void ReadImageBytesAsync(string requestKey, int sizePx, Action<byte[]> onBytesLoaded, Action<string> onError)
        {
            if (DiskStorageHelper.TryFindExistingFile(
                cacheDirPath,
                romDirPath,
                requestKey,
                sizePx,
                out string fullPath,
                out _,
                out _))
            {
                DiskStorageHelper.ReadFileBytesAsync(fullPath, callbackReceiver, onBytesLoaded, onError);
            }
            else
            {
                callbackReceiver.EnqueueMainThread(() =>
                    onError?.Invoke($"iOS file not found in ROM or Cache for key='{requestKey}', size={sizePx}px"));
            }
        }

        public void ClearVolatileCache()
        {
            if (System.IO.Directory.Exists(cacheDirPath))
            {
                foreach (string f in System.IO.Directory.GetFiles(cacheDirPath, "*.jpg"))
                {
                    try { System.IO.File.Delete(f); } catch { }
                }
            }
        }

        private void HandleConnectivityChanged(bool online) => OnConnectivityChanged?.Invoke(online);
        private void HandleDownloadCompleted(NativeDownloadResult result) => OnDownloadCompleted?.Invoke(result);
        private void HandleCustomSizeCompleted(NativeCustomSizeResult result) => OnCustomSizeCompleted?.Invoke(result);

        public void Dispose()
        {
            if (callbackReceiver != null)
            {
                callbackReceiver.OnNativeConnectivityChanged -= HandleConnectivityChanged;
                callbackReceiver.OnNativeDownloadCompleted -= HandleDownloadCompleted;
                callbackReceiver.OnNativeCustomSizeCompleted -= HandleCustomSizeCompleted;
            }
        }
    }
}

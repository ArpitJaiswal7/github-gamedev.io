using System;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Unity C# JNI wrapper around com.studio.nativeimagepipeline.NativeImageBridge (Android .java plugin).
    /// Handles 100+ background image downloads, active connectivity polling, PNG->JPG conversion,
    /// 256px/1080px/custom resizing, and Cache/ROM file management on Android devices.
    /// </summary>
    public sealed class AndroidImageBridge : INativeImageBridge
    {
        private const string JavaClassName = "com.studio.nativeimagepipeline.NativeImageBridge";

        private string cacheDirPath;
        private string romDirPath;
        private NativeBridgeCallbackReceiver callbackReceiver;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass bridgeJavaClass;
#endif

        public event Action<bool> OnConnectivityChanged;
        public event Action<NativeDownloadResult> OnDownloadCompleted;
        public event Action<NativeCustomSizeResult> OnCustomSizeCompleted;

        public bool IsOnline
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (bridgeJavaClass != null)
                {
                    return bridgeJavaClass.CallStatic<bool>("isConnected");
                }
#endif
                return Application.internetReachability != NetworkReachability.NotReachable;
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

#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass = new AndroidJavaClass(JavaClassName);
            bridgeJavaClass.CallStatic(
                "initialize",
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
#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass?.CallStatic(
                "enqueueDownload",
                requestKey,
                url,
                (int)priority,
                (int)targetStorage);
#endif
        }

        public void UpdatePriority(string requestKey, RequestPriority newPriority)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass?.CallStatic("updatePriority", requestKey, (int)newPriority);
#endif
        }

        public void CancelAndRemoveRequest(string requestKey)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass?.CallStatic("cancelOrRemoveRequest", requestKey);
#endif
        }

        public void RequestCustomSize(string requestKey, string fallbackUrl, int customSizePx, RequestPriority priority)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass?.CallStatic(
                "requestCustomSize",
                requestKey,
                fallbackUrl ?? string.Empty,
                customSizePx,
                (int)priority);
#endif
        }

        public bool PromoteToROM(string requestKey)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeJavaClass != null)
            {
                return bridgeJavaClass.CallStatic<bool>("promoteToROM", requestKey);
            }
#endif
            return DiskStorageHelper.MoveAllTiers(cacheDirPath, romDirPath, requestKey);
        }

        public bool DemoteToCache(string requestKey)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeJavaClass != null)
            {
                return bridgeJavaClass.CallStatic<bool>("demoteToCache", requestKey);
            }
#endif
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
                    onError?.Invoke($"Image file not found in ROM or Cache for key='{requestKey}', size={sizePx}px"));
            }
        }

        public void ClearVolatileCache()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass?.CallStatic("clearVolatileCache");
#endif
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
#if UNITY_ANDROID && !UNITY_EDITOR
            bridgeJavaClass?.Dispose();
            bridgeJavaClass = null;
#endif
        }
    }
}

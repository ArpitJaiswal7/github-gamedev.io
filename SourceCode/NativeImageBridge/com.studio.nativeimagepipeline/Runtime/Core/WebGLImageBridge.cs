using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Unity C# wrapper around NativeImageBridge.jslib for WebGL builds.
    /// Offloads downloading, PNG->JPG transcoding, and 256px/1080px/custom resizing to the browser's
    /// native OffscreenCanvas & createImageBitmap outside Unity's WASM heap, and persists files
    /// in IndexedDB (volatile_cache vs permanent_rom stores).
    /// </summary>
    public sealed class WebGLImageBridge : INativeImageBridge
    {
        private NativeBridgeCallbackReceiver callbackReceiver;
#if UNITY_WEBGL && !UNITY_EDITOR
        private int nextCallbackId = 1;
#endif

        private readonly Dictionary<int, (Action<byte[]> onLoaded, Action<string> onError)> pendingByteReads
            = new Dictionary<int, (Action<byte[]> onLoaded, Action<string> onError)>();

        // In-memory metadata mirror of completed IndexedDB files for instant synchronous existence checks
        private readonly Dictionary<string, (StorageTier tier, long bytes)> indexedDbCatalog
            = new Dictionary<string, (StorageTier tier, long bytes)>(StringComparer.Ordinal);

        private int defaultIconSizePx = 256;
        private int defaultFullSizePx = 1080;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NIP_WebGL_Initialize(
            int checkIntervalSec,
            int maxConcurrentWorkers,
            int iconSizePx,
            int fullSizePx,
            int jpegQuality,
            int maxRetries);

        [DllImport("__Internal")]
        private static extern int NIP_WebGL_IsConnected();

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_EnqueueDownload(
            string requestKey,
            string url,
            int priority,
            int targetStorageTier);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_UpdatePriority(string requestKey, int newPriority);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_CancelOrRemoveRequest(string requestKey);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_RequestCustomSize(
            string requestKey,
            string fallbackUrl,
            int customSizePx,
            int priority);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_ReadImageBytesAsync(
            string requestKey,
            int sizePx,
            int callbackId);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_FreeBuffer(IntPtr bufferPtr);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_MoveBetweenStores(string requestKey, int toRom);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_ClearVolatileCache();

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_OpenCacheFolderModal(string requestKey, int sizePx);
#endif

        public static void OpenBrowserFolderModal(string requestKey, int sizePx)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_OpenCacheFolderModal(requestKey, sizePx);
#endif
        }

        public event Action<bool> OnConnectivityChanged;
        public event Action<NativeDownloadResult> OnDownloadCompleted;
        public event Action<NativeCustomSizeResult> OnCustomSizeCompleted;

        public bool IsOnline
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return NIP_WebGL_IsConnected() != 0;
#else
                return Application.internetReachability != NetworkReachability.NotReachable;
#endif
            }
        }

        public void Initialize(string cacheDirPath, string romDirPath, ImagePipelineConfig config)
        {
            defaultIconSizePx = config.defaultIconSizePx > 0 ? config.defaultIconSizePx : 256;
            defaultFullSizePx = config.defaultFullSizePx > 0 ? config.defaultFullSizePx : 1080;

            callbackReceiver = NativeBridgeCallbackReceiver.EnsureInstance();
            callbackReceiver.OnNativeConnectivityChanged += HandleConnectivityChanged;
            callbackReceiver.OnNativeDownloadCompleted += HandleDownloadCompleted;
            callbackReceiver.OnNativeCustomSizeCompleted += HandleCustomSizeCompleted;
            callbackReceiver.OnNativeWebGLBytesLoaded += HandleWebGLBytesLoaded;

#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_Initialize(
                config.connectivityCheckIntervalSeconds,
                config.maxConcurrentDownloads,
                defaultIconSizePx,
                defaultFullSizePx,
                config.jpegQuality,
                config.maxRetryCount);
#endif
        }

        public void EnqueueRequest(string requestKey, string url, RequestPriority priority, StorageTier targetStorage)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_EnqueueDownload(requestKey, url, (int)priority, (int)targetStorage);
#endif
        }

        public void UpdatePriority(string requestKey, RequestPriority newPriority)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_UpdatePriority(requestKey, (int)newPriority);
#endif
        }

        public void CancelAndRemoveRequest(string requestKey)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_CancelOrRemoveRequest(requestKey);
#endif
        }

        public void RequestCustomSize(string requestKey, string fallbackUrl, int customSizePx, RequestPriority priority)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_RequestCustomSize(requestKey, fallbackUrl ?? string.Empty, customSizePx, (int)priority);
#endif
        }

        public bool PromoteToROM(string requestKey)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_MoveBetweenStores(requestKey, 1);
#endif
            UpdateCatalogTier(requestKey, StorageTier.PermanentROM);
            return true;
        }

        public bool DemoteToCache(string requestKey)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_MoveBetweenStores(requestKey, 0);
#endif
            UpdateCatalogTier(requestKey, StorageTier.VolatileCache);
            return true;
        }

        public bool DeleteFiles(string requestKey, StorageTier tier)
        {
            indexedDbCatalog.Remove(RequestKeyGenerator.GetFileName(requestKey, defaultIconSizePx));
            indexedDbCatalog.Remove(RequestKeyGenerator.GetFileName(requestKey, defaultFullSizePx));
            return true;
        }

        public bool HasCachedFile(string requestKey, int sizePx, out StorageTier foundInTier, out long fileSizeBytes)
        {
            string fileKey = RequestKeyGenerator.GetFileName(requestKey, sizePx);
            if (indexedDbCatalog.TryGetValue(fileKey, out var entry) && entry.bytes > 0)
            {
                foundInTier = entry.tier;
                fileSizeBytes = entry.bytes;
                return true;
            }

            foundInTier = StorageTier.VolatileCache;
            fileSizeBytes = 0L;
            return false;
        }

        public void ReadImageBytesAsync(string requestKey, int sizePx, Action<byte[]> onBytesLoaded, Action<string> onError)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int cbId = nextCallbackId++;
            pendingByteReads[cbId] = (onBytesLoaded, onError);
            NIP_WebGL_ReadImageBytesAsync(requestKey, sizePx, cbId);
#else
            onError?.Invoke("WebGLImageBridge is only active in WebGL builds.");
#endif
        }

        public void ClearVolatileCache()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NIP_WebGL_ClearVolatileCache();
#endif
            var keysToRemove = new List<string>();
            foreach (var kvp in indexedDbCatalog)
            {
                if (kvp.Value.tier == StorageTier.VolatileCache)
                    keysToRemove.Add(kvp.Key);
            }
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                indexedDbCatalog.Remove(keysToRemove[i]);
            }
        }

        private void HandleConnectivityChanged(bool online) => OnConnectivityChanged?.Invoke(online);

        private void HandleDownloadCompleted(NativeDownloadResult result)
        {
            if (result.success)
            {
                StorageTier tier = result.storageTier == 1 ? StorageTier.PermanentROM : StorageTier.VolatileCache;
                indexedDbCatalog[RequestKeyGenerator.GetFileName(result.requestKey, defaultIconSizePx)] = (tier, result.iconSizeBytes);
                indexedDbCatalog[RequestKeyGenerator.GetFileName(result.requestKey, defaultFullSizePx)] = (tier, result.fullSizeBytes);
            }
            OnDownloadCompleted?.Invoke(result);
        }

        private void HandleCustomSizeCompleted(NativeCustomSizeResult result)
        {
            if (result.success)
            {
                indexedDbCatalog[RequestKeyGenerator.GetFileName(result.requestKey, result.sizePx)] = (StorageTier.VolatileCache, result.sizeBytes);
            }
            OnCustomSizeCompleted?.Invoke(result);
        }

        private void HandleWebGLBytesLoaded(int callbackId, IntPtr bufferPtr, int byteLength)
        {
            if (!pendingByteReads.TryGetValue(callbackId, out var callbacks))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (bufferPtr != IntPtr.Zero) NIP_WebGL_FreeBuffer(bufferPtr);
#endif
                return;
            }

            pendingByteReads.Remove(callbackId);

            if (bufferPtr == IntPtr.Zero || byteLength <= 0)
            {
                callbacks.onError?.Invoke("Image bytes not found in WebGL IndexedDB.");
                return;
            }

            try
            {
                byte[] managedBytes = new byte[byteLength];
                Marshal.Copy(bufferPtr, managedBytes, 0, byteLength);
                callbacks.onLoaded?.Invoke(managedBytes);
            }
            catch (Exception ex)
            {
                callbacks.onError?.Invoke(ex.Message);
            }
            finally
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                NIP_WebGL_FreeBuffer(bufferPtr);
#endif
            }
        }

        private void UpdateCatalogTier(string requestKey, StorageTier newTier)
        {
            string iconKey = RequestKeyGenerator.GetFileName(requestKey, defaultIconSizePx);
            if (indexedDbCatalog.TryGetValue(iconKey, out var iconEntry))
                indexedDbCatalog[iconKey] = (newTier, iconEntry.bytes);

            string fullKey = RequestKeyGenerator.GetFileName(requestKey, defaultFullSizePx);
            if (indexedDbCatalog.TryGetValue(fullKey, out var fullEntry))
                indexedDbCatalog[fullKey] = (newTier, fullEntry.bytes);
        }

        public void Dispose()
        {
            if (callbackReceiver != null)
            {
                callbackReceiver.OnNativeConnectivityChanged -= HandleConnectivityChanged;
                callbackReceiver.OnNativeDownloadCompleted -= HandleDownloadCompleted;
                callbackReceiver.OnNativeCustomSizeCompleted -= HandleCustomSizeCompleted;
                callbackReceiver.OnNativeWebGLBytesLoaded -= HandleWebGLBytesLoaded;
            }
            pendingByteReads.Clear();
        }
    }
}

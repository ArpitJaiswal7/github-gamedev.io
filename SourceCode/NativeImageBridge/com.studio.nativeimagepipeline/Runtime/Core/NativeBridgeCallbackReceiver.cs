using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Receives asynchronous events from native background threads (Android JNI, iOS Obj-C++,
    /// WebGL .jslib UnitySendMessage, and Editor worker tasks) and marshals them safely onto
    /// the Unity main thread.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class NativeBridgeCallbackReceiver : MonoBehaviour
    {
        public const string ReceiverObjectName = "__NativeImagePipelineReceiver";

        private static NativeBridgeCallbackReceiver instance;
        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();

        public event Action<bool> OnNativeConnectivityChanged;
        public event Action<NativeDownloadResult> OnNativeDownloadCompleted;
        public event Action<NativeCustomSizeResult> OnNativeCustomSizeCompleted;
        public event Action<int, IntPtr, int> OnNativeWebGLBytesLoaded;

        public static NativeBridgeCallbackReceiver EnsureInstance()
        {
            if (instance != null)
                return instance;

#if UNITY_2023_1_OR_NEWER
            var existing = FindFirstObjectByType<NativeBridgeCallbackReceiver>();
#else
            var existing = FindObjectOfType<NativeBridgeCallbackReceiver>();
#endif
            if (existing != null)
            {
                instance = existing;
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }

            var go = new GameObject(ReceiverObjectName);
            DontDestroyOnLoad(go);
            instance = go.AddComponent<NativeBridgeCallbackReceiver>();
            return instance;
        }

        /// <summary>
        /// Enqueues any action from a background thread to execute on the next Unity Update tick.
        /// </summary>
        public void EnqueueMainThread(Action action)
        {
            if (action != null)
            {
                mainThreadQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            while (mainThreadQueue.TryDequeue(out Action action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        // =====================================================================
        // Native UnitySendMessage Entry Points (Used by Android, iOS, & WebGL)
        // =====================================================================

        /// <summary>
        /// Called via UnitySendMessage("__NativeImagePipelineReceiver", "OnConnectivityEvent", "1" or "0")
        /// </summary>
        public void OnConnectivityEvent(string statusPayload)
        {
            bool isOnline = statusPayload == "1" ||
                            string.Equals(statusPayload, "true", StringComparison.OrdinalIgnoreCase);
            OnNativeConnectivityChanged?.Invoke(isOnline);
        }

        /// <summary>
        /// Called via UnitySendMessage("__NativeImagePipelineReceiver", "OnDownloadResultEvent", jsonPayload)
        /// </summary>
        public void OnDownloadResultEvent(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
                return;

            try
            {
                var result = JsonUtility.FromJson<NativeDownloadResult>(jsonPayload);
                OnNativeDownloadCompleted?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NativeImagePipeline] Failed to parse NativeDownloadResult JSON: {jsonPayload}\n{ex}");
            }
        }

        /// <summary>
        /// Called via UnitySendMessage("__NativeImagePipelineReceiver", "OnCustomSizeResultEvent", jsonPayload)
        /// </summary>
        public void OnCustomSizeResultEvent(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
                return;

            try
            {
                var result = JsonUtility.FromJson<NativeCustomSizeResult>(jsonPayload);
                OnNativeCustomSizeCompleted?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NativeImagePipeline] Failed to parse NativeCustomSizeResult JSON: {jsonPayload}\n{ex}");
            }
        }

        /// <summary>
        /// Called by WebGL .jslib: "callbackId|bufferPtr|byteLen"
        /// </summary>
        public void OnWebGLBytesLoadedEvent(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return;

            string[] parts = payload.Split('|');
            if (parts.Length < 3)
                return;

            if (int.TryParse(parts[0], out int callbackId) &&
                long.TryParse(parts[1], out long ptrVal) &&
                int.TryParse(parts[2], out int byteLen))
            {
                OnNativeWebGLBytesLoaded?.Invoke(callbackId, new IntPtr(ptrVal), byteLen);
            }
        }
    }
}

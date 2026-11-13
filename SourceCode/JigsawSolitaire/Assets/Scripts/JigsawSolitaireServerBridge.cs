using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace JigsawSolitaire.Core
{
    [Serializable]
    public class ServerPuzzleItem
    {
        public string id;
        public string title;
        public string filename;
        public string url;
        public string category;
        public bool saveToRomOnDownload;
        public int width;
        public int height;
    }

    [Serializable]
    public class ServerUrlsResponse
    {
        public string version;
        public int count;
        public List<string> urls = new List<string>();
        public List<ServerPuzzleItem> puzzles = new List<ServerPuzzleItem>();
    }

    [Serializable]
    public class ServerManifestResponse
    {
        public string version;
        public int count;
        public List<string> urls = new List<string>();
        public List<ServerPuzzleItem> puzzles = new List<ServerPuzzleItem>();
    }

    [Serializable]
    public class WebGLDownloadResultPayload
    {
        public string requestKey;
        public bool success;
        public int iconSizeBytes;
        public int fullSizeBytes;
        public int durationMs;
        public int storageTier;
        public string errorMessage;
    }

    public class JigsawSolitaireServerBridge : MonoBehaviour
    {
        public static JigsawSolitaireServerBridge Instance { get; private set; }
        public const string DefaultServerUrl = "http://localhost:4200";
        public const string GitHubPagesFallbackBaseUrl = "https://arpitjaiswal7.github.io/github-gamedev.io/assets/puzzles";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NIP_WebGL_Initialize(int checkIntervalSec, int maxConcurrentWorkers, int iconSizePx, int fullSizePx, int jpegQuality, int maxRetries);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_EnqueueDownload(string requestKey, string url, int priority, int targetStorageTier);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_ReadRgbaPixelsBottomUpAsync(string requestKey, int targetDimPx, int callbackId);

        [DllImport("__Internal")]
        private static extern void NIP_WebGL_FreeBuffer(IntPtr bufferPtr);
#endif

        private class PendingDownloadRequest
        {
            public ServerPuzzleItem item;
            public int targetDimPx;
            public Action<Texture2D, string> onLoaded;
            public Action<string> onError;
        }

        private class PendingRgbaReadRequest
        {
            public ServerPuzzleItem item;
            public string sourceInfo;
            public Action<Texture2D, string> onLoaded;
            public Action<string> onError;
        }

        private readonly Dictionary<string, PendingDownloadRequest> pendingDownloads = new Dictionary<string, PendingDownloadRequest>();
        private readonly Dictionary<int, PendingRgbaReadRequest> pendingRgbaReads = new Dictionary<int, PendingRgbaReadRequest>();
        private int nextCallbackId = 1;

        public static JigsawSolitaireServerBridge EnsureInitialized()
        {
            if (Instance != null) return Instance;
            var go = GameObject.Find("__NativeImagePipelineReceiver");
            if (go == null)
            {
                go = new GameObject("__NativeImagePipelineReceiver");
                DontDestroyOnLoad(go);
            }
            Instance = go.GetComponent<JigsawSolitaireServerBridge>();
            if (Instance == null)
            {
                Instance = go.AddComponent<JigsawSolitaireServerBridge>();
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                gameObject.name = "__NativeImagePipelineReceiver";
                DontDestroyOnLoad(gameObject);
#if UNITY_WEBGL && !UNITY_EDITOR
                NIP_WebGL_Initialize(5, 2, 256, 1080, 88, 2);
#endif
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private static bool IsRunningOnHttps()
        {
            string abs = Application.absoluteURL ?? "";
            return abs.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerator GetPublishedImageUrlsCoroutine(
            string serverBaseUrl,
            Action<List<string>> onSuccess,
            Action<string> onError = null)
        {
            string cleanBase = string.IsNullOrEmpty(serverBaseUrl) ? DefaultServerUrl : serverBaseUrl.TrimEnd('/');
            var endpoints = new List<string>();
            if (IsRunningOnHttps())
            {
                endpoints.Add("../../assets/puzzles/urls.json");
                endpoints.Add(GitHubPagesFallbackBaseUrl + "/urls.json");
                endpoints.Add(cleanBase + "/api/puzzles/urls");
            }
            else
            {
                endpoints.Add(cleanBase + "/api/puzzles/urls");
                endpoints.Add("../../assets/puzzles/urls.json");
                endpoints.Add(GitHubPagesFallbackBaseUrl + "/urls.json");
            }

            string lastError = "Failed to fetch published URLs";
            foreach (string endpoint in endpoints)
            {
                using (UnityWebRequest req = UnityWebRequest.Get(endpoint))
                {
                    req.timeout = 5;
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string json = req.downloadHandler.text;
                        try
                        {
                            var resp = JsonUtility.FromJson<ServerUrlsResponse>(json);
                            if (resp != null && resp.urls != null && resp.urls.Count > 0)
                            {
                                onSuccess?.Invoke(resp.urls);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            lastError = ex.Message;
                        }
                    }
                    else
                    {
                        lastError = req.error;
                    }
                }
            }

            onError?.Invoke(lastError);
        }

        public IEnumerator GetPublishedManifestCoroutine(
            string serverBaseUrl,
            Action<List<ServerPuzzleItem>> onSuccess,
            Action<string> onError = null)
        {
            string cleanBase = string.IsNullOrEmpty(serverBaseUrl) ? DefaultServerUrl : serverBaseUrl.TrimEnd('/');
            var endpoints = new List<string>();
            if (IsRunningOnHttps())
            {
                endpoints.Add("../../assets/puzzles/published.json");
                endpoints.Add(GitHubPagesFallbackBaseUrl + "/published.json");
                endpoints.Add(cleanBase + "/api/puzzles/published");
            }
            else
            {
                endpoints.Add(cleanBase + "/api/puzzles/published");
                endpoints.Add("../../assets/puzzles/published.json");
                endpoints.Add(GitHubPagesFallbackBaseUrl + "/published.json");
            }

            string lastError = "Failed to fetch published manifest";
            foreach (string endpoint in endpoints)
            {
                using (UnityWebRequest req = UnityWebRequest.Get(endpoint))
                {
                    req.timeout = 5;
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string json = req.downloadHandler.text;
                        try
                        {
                            var resp = JsonUtility.FromJson<ServerManifestResponse>(json);
                            if (resp != null && resp.puzzles != null && resp.puzzles.Count > 0)
                            {
                                onSuccess?.Invoke(resp.puzzles);
                                yield break;
                            }
                        }
                        catch (Exception ex)
                        {
                            lastError = ex.Message;
                        }
                    }
                    else
                    {
                        lastError = req.error;
                    }
                }
            }

            onError?.Invoke(lastError);
        }

        public void LoadPuzzleImageViaBridge(
            ServerPuzzleItem item,
            int targetDimPx,
            Action<Texture2D, string> onLoaded,
            Action<string> onError)
        {
            if (item == null || string.IsNullOrEmpty(item.url))
            {
                onError?.Invoke("Invalid puzzle item or empty URL");
                return;
            }

            string finalUrl = item.url;
            if (IsRunningOnHttps())
            {
                if (finalUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    string filename = !string.IsNullOrEmpty(item.filename) ? item.filename : System.IO.Path.GetFileName(new Uri(finalUrl).AbsolutePath);
                    finalUrl = $"{GitHubPagesFallbackBaseUrl}/uploads/{Uri.EscapeDataString(filename)}";
                }
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            string requestKey = !string.IsNullOrEmpty(item.id) ? item.id : ("puzzle_" + Mathf.Abs(finalUrl.GetHashCode()));
            int storageTier = item.saveToRomOnDownload ? 1 : 0;

            pendingDownloads[requestKey] = new PendingDownloadRequest
            {
                item = item,
                targetDimPx = targetDimPx,
                onLoaded = onLoaded,
                onError = onError
            };

            NIP_WebGL_EnqueueDownload(requestKey, finalUrl, 100, storageTier);
#else
            // In Editor: fallback to image conversion or procedural deterministic texture
            StartCoroutine(LoadTextureViaWebBytesCoroutine(finalUrl, onLoaded, onError, targetDimPx, item.id));
#endif
        }

        private IEnumerator LoadTextureViaWebBytesCoroutine(string url, Action<Texture2D, string> onLoaded, Action<string> onError, int targetDimPx, string itemId)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = req.downloadHandler.data;
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(data))
                    {
                        onLoaded?.Invoke(tex, "Direct Download Handler");
                        yield break;
                    }
                }
            }

            // Fallback deterministic texture
            var fb = new Texture2D(targetDimPx, targetDimPx, TextureFormat.RGBA32, false);
            Color[] cols = new Color[targetDimPx * targetDimPx];
            int hash = (itemId ?? url).GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            Color c1 = Color.HSVToRGB(hue, 0.65f, 0.35f);
            Color c2 = Color.HSVToRGB((hue + 0.18f) % 1f, 0.70f, 0.75f);
            for (int y = 0; y < targetDimPx; y++)
            {
                float ny = (float)y / targetDimPx;
                for (int x = 0; x < targetDimPx; x++)
                {
                    cols[y * targetDimPx + x] = Color.Lerp(c1, c2, ny);
                }
            }
            fb.SetPixels(cols);
            fb.Apply();
            onLoaded?.Invoke(fb, "Generated Fallback");
        }

        // Invoked by NativeImageBridge.jslib SendMessage("__NativeImagePipelineReceiver", "OnDownloadResultEvent", payload)
        public void OnDownloadResultEvent(string jsonPayload)
        {
            if (string.IsNullOrEmpty(jsonPayload)) return;
            var res = JsonUtility.FromJson<WebGLDownloadResultPayload>(jsonPayload);
            if (res == null || string.IsNullOrEmpty(res.requestKey)) return;

            if (!pendingDownloads.TryGetValue(res.requestKey, out var pending))
                return;
            pendingDownloads.Remove(res.requestKey);

            if (!res.success)
            {
                pending.onError?.Invoke(string.IsNullOrEmpty(res.errorMessage) ? "Bridge download failed" : res.errorMessage);
                return;
            }

            string tierName = res.storageTier == 1 ? "ROM" : "Cache";
            string srcInfo = res.durationMs == 0
                ? $"IndexedDB {tierName} (0ms)"
                : $"Native Bridge ({res.durationMs}ms, {(res.fullSizeBytes / 1024f):F1}KB)";

#if UNITY_WEBGL && !UNITY_EDITOR
            int cbId = nextCallbackId++;
            pendingRgbaReads[cbId] = new PendingRgbaReadRequest
            {
                item = pending.item,
                sourceInfo = srcInfo,
                onLoaded = pending.onLoaded,
                onError = pending.onError
            };
            NIP_WebGL_ReadRgbaPixelsBottomUpAsync(res.requestKey, pending.targetDimPx, cbId);
#endif
        }

        // Invoked by NativeImageBridge.jslib SendMessage("__NativeImagePipelineReceiver", "OnWebGLRgbaLoadedEvent", "cbId|ptr|len|dim")
        public void OnWebGLRgbaLoadedEvent(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;
            string[] parts = payload.Split('|');
            if (parts.Length < 4) return;

            if (!int.TryParse(parts[0], out int cbId)) return;
            long ptrVal = long.TryParse(parts[1], out long p) ? p : 0;
            int byteLen = int.TryParse(parts[2], out int l) ? l : 0;
            int dim = int.TryParse(parts[3], out int d) ? d : 512;

            if (!pendingRgbaReads.TryGetValue(cbId, out var req))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (ptrVal != 0) NIP_WebGL_FreeBuffer(new IntPtr(ptrVal));
#endif
                return;
            }
            pendingRgbaReads.Remove(cbId);

            if (ptrVal == 0 || byteLen <= 0)
            {
                req.onError?.Invoke("Failed to decode RGBA pixels from IndexedDB");
                return;
            }

            try
            {
                IntPtr ptr = new IntPtr(ptrVal);
                byte[] rgbaBytes = new byte[byteLen];
                Marshal.Copy(ptr, rgbaBytes, 0, byteLen);
#if UNITY_WEBGL && !UNITY_EDITOR
                NIP_WebGL_FreeBuffer(ptr);
#endif
                var tex = new Texture2D(dim, dim, TextureFormat.RGBA32, false);
                tex.LoadRawTextureData(rgbaBytes);
                tex.Apply();

                req.onLoaded?.Invoke(tex, req.sourceInfo);
            }
            catch (Exception ex)
            {
                req.onError?.Invoke(ex.Message);
            }
        }
    }
}

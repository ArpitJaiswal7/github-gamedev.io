using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Shared disk storage helper for platforms with standard POSIX/Windows filesystems
    /// (Android, iOS, and Unity Editor). Handles atomic file moves between Volatile Cache
    /// and Permanent ROM, resolution existence checks, and async byte reads.
    /// (Note: WebGL overrides these methods to use browser IndexedDB object stores.)
    /// </summary>
    public static class DiskStorageHelper
    {
        public static void EnsureDirectories(string cacheDirPath, string romDirPath)
        {
            if (!string.IsNullOrWhiteSpace(cacheDirPath) && !Directory.Exists(cacheDirPath))
            {
                Directory.CreateDirectory(cacheDirPath);
            }
            if (!string.IsNullOrWhiteSpace(romDirPath) && !Directory.Exists(romDirPath))
            {
                Directory.CreateDirectory(romDirPath);
            }
        }

        public static string GetFilePath(string baseDir, string requestKey, int sizePx)
        {
            return Path.Combine(baseDir, RequestKeyGenerator.GetFileName(requestKey, sizePx));
        }

        /// <summary>
        /// Checks Permanent ROM first, then Volatile Cache for the requested (requestKey, sizePx) JPEG file.
        /// </summary>
        public static bool TryFindExistingFile(
            string cacheDirPath,
            string romDirPath,
            string requestKey,
            int sizePx,
            out string fullPath,
            out StorageTier foundTier,
            out long fileSizeBytes)
        {
            // 1. Check Permanent ROM first
            if (!string.IsNullOrWhiteSpace(romDirPath))
            {
                string romFile = GetFilePath(romDirPath, requestKey, sizePx);
                if (File.Exists(romFile))
                {
                    var info = new FileInfo(romFile);
                    if (info.Length > 0)
                    {
                        fullPath = romFile;
                        foundTier = StorageTier.PermanentROM;
                        fileSizeBytes = info.Length;
                        return true;
                    }
                }
            }

            // 2. Check Volatile Cache second
            if (!string.IsNullOrWhiteSpace(cacheDirPath))
            {
                string cacheFile = GetFilePath(cacheDirPath, requestKey, sizePx);
                if (File.Exists(cacheFile))
                {
                    var info = new FileInfo(cacheFile);
                    if (info.Length > 0)
                    {
                        fullPath = cacheFile;
                        foundTier = StorageTier.VolatileCache;
                        fileSizeBytes = info.Length;
                        return true;
                    }
                }
            }

            fullPath = null;
            foundTier = StorageTier.VolatileCache;
            fileSizeBytes = 0L;
            return false;
        }

        /// <summary>
        /// Moves all generated JPEG resolutions (`{requestKey}_*.jpg`) from sourceDir to destDir
        /// and deletes the source copies. Used for PromoteToROM and DemoteToCache.
        /// </summary>
        public static bool MoveAllTiers(string sourceDir, string destDir, string requestKey)
        {
            if (string.IsNullOrWhiteSpace(sourceDir) || string.IsNullOrWhiteSpace(destDir) || string.IsNullOrWhiteSpace(requestKey))
                return false;

            if (!Directory.Exists(sourceDir))
                return false;

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string searchPattern = $"{requestKey}_*.jpg";
            string[] matchingFiles = Directory.GetFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly);
            if (matchingFiles.Length == 0)
            {
                // Check if files are already in destDir
                string[] existingDest = Directory.GetFiles(destDir, searchPattern, SearchOption.TopDirectoryOnly);
                return existingDest.Length > 0;
            }

            bool movedAny = false;
            for (int i = 0; i < matchingFiles.Length; i++)
            {
                string srcPath = matchingFiles[i];
                string fileName = Path.GetFileName(srcPath);
                string dstPath = Path.Combine(destDir, fileName);

                try
                {
                    if (File.Exists(dstPath))
                    {
                        File.Delete(dstPath);
                    }
                    File.Move(srcPath, dstPath);
                    movedAny = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NativeImagePipeline] Failed moving '{srcPath}' to '{dstPath}': {ex.Message}");
                }
            }

            return movedAny;
        }

        /// <summary>
        /// Deletes all generated JPEG tiers (`{requestKey}_*.jpg`) from the specified directory.
        /// </summary>
        public static bool DeleteAllTiers(string targetDir, string requestKey)
        {
            if (string.IsNullOrWhiteSpace(targetDir) || string.IsNullOrWhiteSpace(requestKey) || !Directory.Exists(targetDir))
                return false;

            string searchPattern = $"{requestKey}_*.jpg";
            string[] matchingFiles = Directory.GetFiles(targetDir, searchPattern, SearchOption.TopDirectoryOnly);
            bool deletedAny = false;

            for (int i = 0; i < matchingFiles.Length; i++)
            {
                try
                {
                    File.Delete(matchingFiles[i]);
                    deletedAny = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NativeImagePipeline] Failed deleting '{matchingFiles[i]}': {ex.Message}");
                }
            }

            return deletedAny;
        }

        /// <summary>
        /// Asynchronously reads file bytes on a background thread and marshals the result back to the main thread.
        /// </summary>
        public static void ReadFileBytesAsync(
            string filePath,
            NativeBridgeCallbackReceiver callbackReceiver,
            Action<byte[]> onLoaded,
            Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                callbackReceiver.EnqueueMainThread(() => onError?.Invoke($"File not found on disk: {filePath}"));
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    callbackReceiver.EnqueueMainThread(() => onLoaded?.Invoke(bytes));
                }
                catch (Exception ex)
                {
                    callbackReceiver.EnqueueMainThread(() => onError?.Invoke(ex.Message));
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Cross-platform utility (Windows & macOS) for resolving full disk paths from the parent drive
    /// and revealing single or multiple selected Unity assets simultaneously in a single
    /// Windows Explorer or macOS Finder window.
    /// </summary>
    public static class OSFileExplorerUtility
    {
#if UNITY_EDITOR_WIN
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int SHParseDisplayName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            IntPtr pbc,
            out IntPtr ppidl,
            uint sfgaoIn,
            out uint psfgaoOut);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
            uint dwFlags);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern IntPtr ILFindLastID(IntPtr pidl);

        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern void CoTaskMemFree(IntPtr pv);
#endif

        /// <summary>
        /// Converts a Unity asset GUID to its complete OS file system path including the parent drive
        /// (e.g., "C:\Users\arpit\...\Assets\MyFile.png" on Windows or "/Users/.../Assets/MyFile.png" on macOS).
        /// </summary>
        public static string GetFullDiskPathFromGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return string.Empty;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            return GetFullDiskPathFromAssetPath(assetPath);
        }

        /// <summary>
        /// Converts a project-relative asset path (e.g., "Assets/...") to its complete OS file system path
        /// starting from the parent drive.
        /// </summary>
        public static string GetFullDiskPathFromAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrEmpty(projectRoot))
                    return Path.GetFullPath(assetPath);

                string combined = Path.Combine(projectRoot, assetPath);
                return Path.GetFullPath(combined);
            }
            catch
            {
                return assetPath;
            }
        }

        /// <summary>
        /// Copies the complete full OS disk path of a single asset GUID to the system clipboard.
        /// </summary>
        public static string CopySingleAssetFullDiskPath(string guid)
        {
            string fullPath = GetFullDiskPathFromGuid(guid);
            if (!string.IsNullOrEmpty(fullPath))
            {
                EditorGUIUtility.systemCopyBuffer = fullPath;
                Debug.Log($"[Asset Organizer] Copied full path to clipboard: {fullPath}");
            }
            return fullPath;
        }

        /// <summary>
        /// Reveals one or more Unity assets (by GUID) in a single Windows Explorer or macOS Finder window
        /// with all specified items selected simultaneously.
        /// </summary>
        public static void RevealAssetsInExplorerOrFinder(IReadOnlyList<string> guids)
        {
            if (guids == null || guids.Count == 0)
                return;

            List<string> fullPaths = new List<string>(guids.Count);
            for (int i = 0; i < guids.Count; i++)
            {
                string fullPath = GetFullDiskPathFromGuid(guids[i]);
                if (!string.IsNullOrEmpty(fullPath) && (File.Exists(fullPath) || Directory.Exists(fullPath)))
                {
                    if (!fullPaths.Contains(fullPath))
                    {
                        fullPaths.Add(fullPath);
                    }
                }
            }

            if (fullPaths.Count == 0)
                return;

            RevealFullDiskPathsInSingleWindow(fullPaths);
        }

        /// <summary>
        /// Opens the OS file manager (Windows Explorer or macOS Finder) and selects all provided full paths together.
        /// </summary>
        public static void RevealFullDiskPathsInSingleWindow(IReadOnlyList<string> fullPaths)
        {
            if (fullPaths == null || fullPaths.Count == 0)
                return;

#if UNITY_EDITOR_WIN
            RevealMultipleInWindowsExplorer(fullPaths);
#elif UNITY_EDITOR_OSX
            RevealMultipleInMacOSFinder(fullPaths);
#else
            string firstAssetPath = fullPaths[0];
            EditorUtility.RevealInFinder(firstAssetPath);
#endif
        }

#if UNITY_EDITOR_WIN
        private static void RevealMultipleInWindowsExplorer(IReadOnlyList<string> fullPaths)
        {
            // Group files/folders by their parent directory so all items in the same directory
            // are selected together inside a single Windows Explorer window.
            Dictionary<string, List<string>> itemsByParentFolder = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fullPaths.Count; i++)
            {
                string normalizedPath = Path.GetFullPath(fullPaths[i].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string parentDir = Path.GetDirectoryName(normalizedPath);
                if (string.IsNullOrEmpty(parentDir))
                    continue;

                if (!itemsByParentFolder.TryGetValue(parentDir, out List<string> list))
                {
                    list = new List<string>();
                    itemsByParentFolder[parentDir] = list;
                }

                if (!list.Contains(normalizedPath))
                {
                    list.Add(normalizedPath);
                }
            }

            foreach (KeyValuePair<string, List<string>> kvp in itemsByParentFolder)
            {
                string folderPath = kvp.Key;
                List<string> childPaths = kvp.Value;

                IntPtr folderPidl = IntPtr.Zero;
                List<IntPtr> allocatedItemPidls = new List<IntPtr>(childPaths.Count);

                try
                {
                    int hrFolder = SHParseDisplayName(folderPath, IntPtr.Zero, out folderPidl, 0, out _);
                    if (hrFolder != 0 || folderPidl == IntPtr.Zero)
                    {
                        // Fallback if PIDL parsing fails
                        EditorUtility.RevealInFinder(childPaths[0]);
                        continue;
                    }

                    List<IntPtr> childRelativePidls = new List<IntPtr>(childPaths.Count);
                    for (int i = 0; i < childPaths.Count; i++)
                    {
                        int hrItem = SHParseDisplayName(childPaths[i], IntPtr.Zero, out IntPtr itemPidl, 0, out _);
                        if (hrItem == 0 && itemPidl != IntPtr.Zero)
                        {
                            allocatedItemPidls.Add(itemPidl);
                            IntPtr relativePidl = ILFindLastID(itemPidl);
                            if (relativePidl != IntPtr.Zero)
                            {
                                childRelativePidls.Add(relativePidl);
                            }
                        }
                    }

                    if (childRelativePidls.Count > 0)
                    {
                        int hrOpen = SHOpenFolderAndSelectItems(
                            folderPidl,
                            (uint)childRelativePidls.Count,
                            childRelativePidls.ToArray(),
                            0);

                        if (hrOpen != 0)
                        {
                            EditorUtility.RevealInFinder(childPaths[0]);
                        }
                    }
                    else
                    {
                        EditorUtility.RevealInFinder(childPaths[0]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Asset Organizer] Explorer multi-select fallback: {ex.Message}");
                    EditorUtility.RevealInFinder(childPaths[0]);
                }
                finally
                {
                    for (int i = 0; i < allocatedItemPidls.Count; i++)
                    {
                        if (allocatedItemPidls[i] != IntPtr.Zero)
                        {
                            CoTaskMemFree(allocatedItemPidls[i]);
                        }
                    }

                    if (folderPidl != IntPtr.Zero)
                    {
                        CoTaskMemFree(folderPidl);
                    }
                }
            }

            Debug.Log($"[Asset Organizer] Revealed {fullPaths.Count} asset(s) in Windows Explorer.");
        }
#endif

#if UNITY_EDITOR_OSX
        private static void RevealMultipleInMacOSFinder(IReadOnlyList<string> fullPaths)
        {
            try
            {
                StringBuilder posixList = new StringBuilder();
                for (int i = 0; i < fullPaths.Count; i++)
                {
                    string escaped = fullPaths[i].Replace("\\", "\\\\").Replace("\"", "\\\"");
                    if (i > 0)
                        posixList.Append(", ");
                    posixList.Append($"POSIX file \"{escaped}\"");
                }

                string script = $"tell application \"Finder\"\nactivate\nreveal {{{posixList}}}\nend tell";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = $"-e '{script.Replace("'", "'\\''")}'",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                Debug.Log($"[Asset Organizer] Revealed {fullPaths.Count} asset(s) in macOS Finder.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Asset Organizer] Finder multi-select fallback: {ex.Message}");
                EditorUtility.RevealInFinder(fullPaths[0]);
            }
        }
#endif
    }
}

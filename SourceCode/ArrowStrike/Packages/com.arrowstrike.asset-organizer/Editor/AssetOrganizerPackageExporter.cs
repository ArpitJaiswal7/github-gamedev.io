using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Packaging and export menu hooks for exporting the Asset Organizer Tool
    /// as either a .unitypackage or a standalone UPM package folder.
    /// </summary>
    public static class AssetOrganizerPackageExporter
    {
        private const string PackageRootPath = "Packages/com.arrowstrike.asset-organizer";

        [MenuItem("Tools/Asset Organizer/Export as .unitypackage...", false, 200)]
        public static void ExportAsUnityPackage()
        {
            string defaultFileName = "AssetOrganizerTool_v1.0.0.unitypackage";
            string targetFile = EditorUtility.SaveFilePanel(
                "Export Asset Organizer .unitypackage",
                 Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                defaultFileName,
                "unitypackage");

            if (string.IsNullOrEmpty(targetFile))
                return;

            // Temporarily mirror Editor scripts under Assets/AssetOrganizerExportTemp if exporting via AssetDatabase.ExportPackage,
            // since Unity's ExportPackage requires an Assets/... path in some Unity versions.
            const string tempAssetsFolder = "Assets/AssetOrganizer";
            bool createdTemp = false;

            try
            {
                string sourceDir = Path.GetFullPath(PackageRootPath);
                if (!Directory.Exists(sourceDir))
                {
                    EditorUtility.DisplayDialog("Export Failed", $"Could not locate package directory at:\n{sourceDir}", "OK");
                    return;
                }

                if (!Directory.Exists(tempAssetsFolder))
                {
                    createdTemp = true;
                    CopyDirectoryRecursive(sourceDir, tempAssetsFolder);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                AssetDatabase.ExportPackage(
                    tempAssetsFolder,
                    targetFile,
                    ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);

                Debug.Log($"[Asset Organizer] Successfully exported .unitypackage to: {targetFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Asset Organizer] Failed to export .unitypackage: {ex.Message}");
            }
            finally
            {
                if (createdTemp && Directory.Exists(tempAssetsFolder))
                {
                    AssetDatabase.DeleteAsset(tempAssetsFolder);
                    AssetDatabase.Refresh();
                }
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }
    }
}

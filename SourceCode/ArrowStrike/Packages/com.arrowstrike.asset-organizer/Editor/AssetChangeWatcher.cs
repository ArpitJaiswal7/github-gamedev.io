using UnityEditor;

namespace AssetOrganizer.Editor
{
    /// <summary>
    /// Tracks asset moves, renames, imports, and deletions in the Unity project
    /// so that pinned entries in <see cref="PinnedAssetsStore"/> always stay valid.
    /// </summary>
    public sealed class AssetChangeWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            PinnedAssetsStore.OnAssetsChanged(importedAssets, deletedAssets, movedAssets);
        }
    }
}

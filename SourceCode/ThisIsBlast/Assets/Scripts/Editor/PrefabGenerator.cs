#if UNITY_EDITOR
using System.IO;
using ThisIsBlast.Arsenal;
using ThisIsBlast.Combat;
using ThisIsBlast.Data;
using ThisIsBlast.EnemyGrid;
using UnityEditor;
using UnityEngine;

namespace ThisIsBlast.Editor
{
    [InitializeOnLoad]
    public static class PrefabGenerator
    {
        static PrefabGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                // Auto-generate prefabs if they don't exist yet
                if (!File.Exists("Assets/Prefabs/Grid/GridCellSlot.prefab") ||
                    !File.Exists("Assets/Prefabs/Grid/GridRowTrack.prefab"))
                {
                    GenerateAllPrefabs();
                }
            };
        }

        [MenuItem("This is Blast/Generate Row and Cell Prefabs")]
        public static void GenerateAllPrefabs()
        {
            EnsureFolderExists("Assets/Prefabs");
            EnsureFolderExists("Assets/Prefabs/Grid");
            EnsureFolderExists("Assets/Prefabs/Arsenal");
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists("Assets/Resources/Prefabs");
            EnsureFolderExists("Assets/Resources/Prefabs/Grid");
            EnsureFolderExists("Assets/Resources/Prefabs/Arsenal");

            CreateGridRowTrackPrefab();
            CreateGridColumnTrackPrefab();
            CreateGridCellSlotPrefab();
            CreateEnemyBlockPrefab();

            CreateBenchRowTrackPrefab();
            CreateBenchCellSlotPrefab();
            CreateArsenalColumnPrefab();
            CreateArsenalBlockPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[PrefabGenerator] All Row, Column, Cell Slot, and Block prefabs successfully created in Assets/Prefabs/ and Assets/Resources/Prefabs/ !</color>");
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void CreateGridRowTrackPrefab()
        {
            var go = new GameObject("GridRowTrack");
            var comp = go.AddComponent<GridRowTrack>();
            comp.Initialize(0, 6.5f, 0.85f, 0f);

            string path1 = "Assets/Prefabs/Grid/GridRowTrack.prefab";
            string path2 = "Assets/Resources/Prefabs/Grid/GridRowTrack.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateGridColumnTrackPrefab()
        {
            var go = new GameObject("GridColumnTrack");
            var comp = go.AddComponent<GridColumnTrack>();
            comp.Initialize(0, 0.9f, 8.0f, 2.0f);

            string path1 = "Assets/Prefabs/Grid/GridColumnTrack.prefab";
            string path2 = "Assets/Resources/Prefabs/Grid/GridColumnTrack.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateGridCellSlotPrefab()
        {
            var go = new GameObject("GridCellSlot");
            var comp = go.AddComponent<GridCellSlot>();
            comp.Initialize(0, 0, 0.68f);

            string path1 = "Assets/Prefabs/Grid/GridCellSlot.prefab";
            string path2 = "Assets/Resources/Prefabs/Grid/GridCellSlot.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateEnemyBlockPrefab()
        {
            var go = new GameObject("EnemyBlock");
            go.AddComponent<EnemyBlock>();
            go.transform.localScale = Vector3.one * (0.68f * 0.82f); // Sized smaller than slot so background grid is visible

            string path1 = "Assets/Prefabs/Grid/EnemyBlock.prefab";
            string path2 = "Assets/Resources/Prefabs/Grid/EnemyBlock.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateBenchRowTrackPrefab()
        {
            var go = new GameObject("BenchRowTrack");
            var comp = go.AddComponent<BenchRowTrack>();
            comp.Initialize(6.5f, 1.35f);

            string path1 = "Assets/Prefabs/Arsenal/BenchRowTrack.prefab";
            string path2 = "Assets/Resources/Prefabs/Arsenal/BenchRowTrack.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateBenchCellSlotPrefab()
        {
            var go = new GameObject("BenchCellSlot");
            var comp = go.AddComponent<BenchCellSlot>();
            comp.Initialize(0, 1.08f);

            string path1 = "Assets/Prefabs/Arsenal/BenchCellSlot.prefab";
            string path2 = "Assets/Resources/Prefabs/Arsenal/BenchCellSlot.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateArsenalColumnPrefab()
        {
            var go = new GameObject("ArsenalColumn");
            go.AddComponent<ArsenalColumn>();

            string path1 = "Assets/Prefabs/Arsenal/ArsenalColumn.prefab";
            string path2 = "Assets/Resources/Prefabs/Arsenal/ArsenalColumn.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }

        private static void CreateArsenalBlockPrefab()
        {
            var go = new GameObject("ArsenalBlock");
            go.AddComponent<ArsenalBlock>();

            string path1 = "Assets/Prefabs/Arsenal/ArsenalBlock.prefab";
            string path2 = "Assets/Resources/Prefabs/Arsenal/ArsenalBlock.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path1);
            PrefabUtility.SaveAsPrefabAsset(go, path2);
            Object.DestroyImmediate(go);
        }
    }
}
#endif

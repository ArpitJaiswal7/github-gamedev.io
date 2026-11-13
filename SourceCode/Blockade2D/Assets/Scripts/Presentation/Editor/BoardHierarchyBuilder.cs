using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blockade2D.Presentation.Controllers;
using Blockade2D.Presentation.UI;
using Blockade2D.Presentation.Utils;
using Blockade2D.Presentation.Views;

namespace Blockade2D.Presentation.Editor
{
    [InitializeOnLoad]
    public static class BoardHierarchyBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainGame.unity";

        static BoardHierarchyBuilder()
        {
            EditorApplication.delayCall += AutoSetupOnProjectLoad;
        }

        private static void AutoSetupOnProjectLoad()
        {
            if (!File.Exists(ScenePath) || !File.Exists("Assets/Resources/Prefabs/Tile.prefab"))
            {
                SetupSceneHierarchy();
            }
        }

        [MenuItem("Blockade2D/Setup Scene Hierarchy")]
        public static void SetupSceneHierarchy()
        {
            EnsureSpriteAssetsExist();
            PrefabGenerator.GeneratePrefabsIfMissing();

            // 1. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.0f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            mainCam.transform.position = new Vector3(0f, 0f, -10f);

            // 2. Setup Board
            var boardView = Object.FindFirstObjectByType<BoardView>();
            if (boardView == null)
            {
                var boardObj = new GameObject("Board");
                boardView = boardObj.AddComponent<BoardView>();
            }

            // Clean any baked children so scene is clean and prefab-driven
            var childrenToRemove = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in boardView.transform)
            {
                childrenToRemove.Add(child.gameObject);
            }
            foreach (var child in childrenToRemove)
            {
                Object.DestroyImmediate(child);
            }

            // Create clean hierarchy containers
            var tilesContainer = new GameObject("Tiles");
            tilesContainer.transform.SetParent(boardView.transform);
            tilesContainer.transform.localPosition = Vector3.zero;

            var slotsContainer = new GameObject("IntersectionSlots");
            slotsContainer.transform.SetParent(boardView.transform);
            slotsContainer.transform.localPosition = Vector3.zero;

            var pawnsContainer = new GameObject("Pawns");
            pawnsContainer.transform.SetParent(boardView.transform);
            pawnsContainer.transform.localPosition = Vector3.zero;

            var placedWalls = new GameObject("PlacedWalls");
            placedWalls.transform.SetParent(boardView.transform);
            placedWalls.transform.localPosition = Vector3.zero;

            // Link Prefabs to BoardView
            var serializedBoard = new SerializedObject(boardView);
            serializedBoard.FindProperty("tilePrefab").objectReferenceValue = Resources.Load<GameObject>("Prefabs/Tile");
            serializedBoard.FindProperty("pawnPrefab").objectReferenceValue = Resources.Load<GameObject>("Prefabs/Pawn");
            serializedBoard.FindProperty("slotPrefab").objectReferenceValue = Resources.Load<GameObject>("Prefabs/IntersectionSlot");
            serializedBoard.FindProperty("wallPrefab").objectReferenceValue = Resources.Load<GameObject>("Prefabs/Wall");
            serializedBoard.FindProperty("ghostWallPrefab").objectReferenceValue = Resources.Load<GameObject>("Prefabs/GhostWall");
            serializedBoard.ApplyModifiedProperties();

            // 3. Setup GameController
            var coordinator = Object.FindFirstObjectByType<MatchCoordinator>();
            GameObject controllerObj;
            if (coordinator == null)
            {
                controllerObj = new GameObject("GameController");
                coordinator = controllerObj.AddComponent<MatchCoordinator>();
            }
            else
            {
                controllerObj = coordinator.gameObject;
            }

            var wallCtrl = controllerObj.GetComponent<WallPlacementController>();
            if (wallCtrl == null) wallCtrl = controllerObj.AddComponent<WallPlacementController>();

            var aiCtrl = controllerObj.GetComponent<AIController>();
            if (aiCtrl == null) aiCtrl = controllerObj.AddComponent<AIController>();

            var matchUI = controllerObj.GetComponent<MatchUI>();
            if (matchUI == null) matchUI = controllerObj.AddComponent<MatchUI>();

            // Link Serialized References on MatchCoordinator
            var serializedCoordinator = new SerializedObject(coordinator);
            serializedCoordinator.FindProperty("boardView").objectReferenceValue = boardView;
            serializedCoordinator.FindProperty("wallController").objectReferenceValue = wallCtrl;
            serializedCoordinator.FindProperty("aiController").objectReferenceValue = aiCtrl;
            serializedCoordinator.FindProperty("matchUI").objectReferenceValue = matchUI;
            serializedCoordinator.ApplyModifiedProperties();

            // 4. Save Scene cleanly to Assets/Scenes/MainGame.unity
            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
                AssetDatabase.Refresh();
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            string savePath = string.IsNullOrEmpty(activeScene.path) ? ScenePath : activeScene.path;

            EditorSceneManager.SaveScene(activeScene, savePath);
            RegisterSceneInBuildSettings(savePath);

            Debug.Log($"<color=#10B981>[Blockade2D]</color> Scene Hierarchy successfully configured and saved to '{savePath}'!");
        }

        private static void EnsureSpriteAssetsExist()
        {
            string spritesDir = "Assets/Resources/Sprites";
            if (!Directory.Exists(spritesDir))
            {
                Directory.CreateDirectory(spritesDir);
            }

            bool createdAny = false;

            string tilePath = $"{spritesDir}/TileRoundedSquare.png";
            if (!File.Exists(tilePath))
            {
                var sprite = SpriteUtils.CreateRoundedSquareSprite(128, 20);
                File.WriteAllBytes(tilePath, sprite.texture.EncodeToPNG());
                createdAny = true;
            }

            string pawnPath = $"{spritesDir}/PawnCircle.png";
            if (!File.Exists(pawnPath))
            {
                var sprite = SpriteUtils.CreateCircleSprite(128);
                File.WriteAllBytes(pawnPath, sprite.texture.EncodeToPNG());
                createdAny = true;
            }

            string wallPath = $"{spritesDir}/WallRoundedSquare.png";
            if (!File.Exists(wallPath))
            {
                var sprite = SpriteUtils.CreateRoundedSquareSprite(64, 12);
                File.WriteAllBytes(wallPath, sprite.texture.EncodeToPNG());
                createdAny = true;
            }

            if (createdAny)
            {
                AssetDatabase.Refresh();
                ConfigureTextureImporter(tilePath, 128);
                ConfigureTextureImporter(pawnPath, 128);
                ConfigureTextureImporter(wallPath, 64);
                AssetDatabase.Refresh();
            }
        }

        private static void ConfigureTextureImporter(string assetPath, int pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                    return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}

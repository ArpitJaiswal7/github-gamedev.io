#if UNITY_EDITOR
using ThisIsBlast.Arsenal;
using ThisIsBlast.Combat;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using ThisIsBlast.EnemyGrid;
using ThisIsBlast.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThisIsBlast.Editor
{
    public static class SceneSetupHelper
    {
        [MenuItem("This is Blast/Setup or Recreate Main Scene")]
        public static void SetupMainGameScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            string scenePath = "Assets/Scenes/MainGame.unity";
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, -0.4f, -11f);
            cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.98f);
            cam.orthographic = true;
            cam.orthographicSize = 7.2f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraShaker>();

            // 2. Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.color = Color.white;
            light.intensity = 1.15f;

            // 3. Systems
            var systemsGo = new GameObject("--- SYSTEMS ---");
            systemsGo.AddComponent<RemoteConfigService>();
            systemsGo.AddComponent<AnalyticsManager>();
            systemsGo.AddComponent<SoundManager>();
            systemsGo.AddComponent<ParticleEffectsManager>();
            systemsGo.AddComponent<ProjectilePool>();
            systemsGo.AddComponent<ArsenalMergeController>();
            systemsGo.AddComponent<EnemyGridManager>();
            systemsGo.AddComponent<ArsenalManager>();
            systemsGo.AddComponent<GameManager>();

            // 4. UI
            var uiGo = new GameObject("--- UI_MANAGER ---");
            uiGo.AddComponent<UIManager>();

            // Generate Prefabs & Levels
            PrefabGenerator.GenerateAllPrefabs();
            GenerateInitialLevels();

            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"<color=green>[SceneSetup] Successfully created and saved scene at {scenePath}</color>");

            // Add to Build Settings
            var sceneList = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = sceneList;

            AssetDatabase.SaveAssets();
        }

        public static void GenerateInitialLevels()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels")) AssetDatabase.CreateFolder("Assets/Resources", "Levels");

            for (int i = 1; i <= 10; i++)
            {
                var level = ProceduralLevelGenerator.GenerateLevel(i);
                byte[] binaryData = LevelDecoder.EncodeToBinary(level);
                string path = $"Assets/Resources/Levels/Level_{i}.bin";
                System.IO.File.WriteAllBytes(path, binaryData);
            }
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[SceneSetup] Successfully generated and saved Level 1 to 10 compact .bin files!</color>");
        }
    }
}
#endif

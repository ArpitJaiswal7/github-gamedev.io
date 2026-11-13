#if UNITY_EDITOR
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using UnityEditor;
using UnityEngine;

namespace ThisIsBlast.Editor
{
    public class LevelGeneratorEditor : EditorWindow
    {
        private int _testLevelNumber = 1;

        [MenuItem("This is Blast/Level & Progression Tool")]
        public static void ShowWindow()
        {
            GetWindow<LevelGeneratorEditor>("Level & Progression Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("Level Generation & Progression Testing", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("🌐 Open Web Level Studio (A/B Progression Architect)", GUILayout.Height(35)))
            {
                string studioBat = System.IO.Path.Combine(Application.dataPath, "..", "LevelStudio", "run_studio.bat");
                if (System.IO.File.Exists(studioBat))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = studioBat,
                        UseShellExecute = true
                    });
                }
                else
                {
                    string htmlPath = System.IO.Path.Combine(Application.dataPath, "..", "LevelStudio", "index.html");
                    Application.OpenURL("file:///" + System.IO.Path.GetFullPath(htmlPath).Replace("\\", "/"));
                }
            }

            EditorGUILayout.Space();
            _testLevelNumber = EditorGUILayout.IntField("Level Number to Test", _testLevelNumber);

            if (GUILayout.Button("Preview Generated Level Data", GUILayout.Height(30)))
            {
                var level = ProceduralLevelGenerator.GenerateLevel(_testLevelNumber);
                Debug.Log($"<color=yellow>=== Level {_testLevelNumber} Preview ===</color>\n" +
                          $"Tier: {level.tier}\n" +
                          $"Grid: {level.gridWidth}x{level.gridHeight}\n" +
                          $"Total Enemy Blocks: {level.enemyBlocks.Count}\n" +
                          $"Arsenal Columns: {level.arsenalColumns.Count}");
            }


            if (GUILayout.Button("Export Level to JSON & CDN Binary (.bin)", GUILayout.Height(30)))
            {
                var level = ProceduralLevelGenerator.GenerateLevel(_testLevelNumber);
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels")) AssetDatabase.CreateFolder("Assets/Resources", "Levels");

                string jsonPath = $"Assets/Resources/Levels/Level_{_testLevelNumber}.json";
                string binPath = $"Assets/Resources/Levels/Level_{_testLevelNumber}.bin";

                // 1. Export JSON
                string json = LevelDecoder.EncodeToJson(level, true);
                System.IO.File.WriteAllText(jsonPath, json);

                // 2. Export Binary (for CDN)
                byte[] binaryBytes = LevelDecoder.EncodeToBinary(level);
                System.IO.File.WriteAllBytes(binPath, binaryBytes);

                AssetDatabase.Refresh();
                Debug.Log($"<color=cyan>[LevelDecoder] Exported Level {_testLevelNumber}:</color>\n" +
                          $"• JSON ({json.Length} bytes): {jsonPath}\n" +
                          $"• CDN Binary ({binaryBytes.Length} bytes): {binPath} ({(100f - (float)binaryBytes.Length / json.Length * 100f):F1}% smaller!)");
            }

            if (GUILayout.Button("Regenerate All Levels (1-10) & Prefabs", GUILayout.Height(30)))
            {
                PrefabGenerator.GenerateAllPrefabs();
                SceneSetupHelper.GenerateInitialLevels();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Runtime Cheats & A/B Testing (Play Mode Only)", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (GUILayout.Button("Load This Level Now in Game"))
            {
                GameManager.Instance?.LoadLevel(_testLevelNumber);
            }

            if (GUILayout.Button("Force Victory"))
            {
                GameManager.Instance?.TriggerVictory();
            }

            if (GUILayout.Button("Force Defeat"))
            {
                GameManager.Instance?.TriggerDefeat("Manual Test Defeat");
            }

            if (GUILayout.Button("Toggle A/B Merge Mode (Simple <-> Tier Evolve)"))
            {
                string current = RemoteConfigService.Instance ? RemoteConfigService.Instance.GetString("bench_merge_mode", "simple_accumulate") : "simple_accumulate";
                string next = current == "simple_accumulate" ? "tier_evolve" : "simple_accumulate";
                RemoteConfigService.Instance?.SetConfigOverride("bench_merge_mode", next);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif

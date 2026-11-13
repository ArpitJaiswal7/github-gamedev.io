#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArrowStrike.ShapeSystem.Editor
{
    public static class WebGLBuildTools
    {
        private const string ShapeScenePath = "Assets/[Developer]/ShapeSystem/Scenes/ShapeEscapeScene.unity";
        private const string DefaultBuildFolder = "Builds/WebGL";

        [MenuItem("Tools/ArrowStrike WebGL/1. Build WebGL (Choose Folder)...", false, 10)]
        public static void BuildWebGLChooseFolder()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            string defaultDir = Path.Combine(projectRoot, DefaultBuildFolder);
            if (!Directory.Exists(defaultDir))
                Directory.CreateDirectory(defaultDir);

            string selectedPath = EditorUtility.SaveFolderPanel("Select WebGL Build Output Folder", defaultDir, "WebGL");
            if (string.IsNullOrEmpty(selectedPath))
                return;

            ExecuteWebGLBuild(selectedPath, BuildOptions.None);
        }

        [MenuItem("Tools/ArrowStrike WebGL/2. Quick Build WebGL (Builds/WebGL)", false, 11)]
        public static void QuickBuildWebGL()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            string outputPath = Path.Combine(projectRoot, DefaultBuildFolder);
            ExecuteWebGLBuild(outputPath, BuildOptions.None);
        }

        [MenuItem("Tools/ArrowStrike WebGL/3. Build && Run WebGL in Browser", false, 12)]
        public static void BuildAndRunWebGL()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            string outputPath = Path.Combine(projectRoot, DefaultBuildFolder);
            ExecuteWebGLBuild(outputPath, BuildOptions.AutoRunPlayer);
        }

        [MenuItem("Tools/ArrowStrike WebGL/4. Configure WebGL Settings && Scene Only", false, 30)]
        public static void ConfigureWebGLSettingsOnly()
        {
            ApplyRecommendedWebGLSettings();
            EnsureShapeSceneInBuildSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                bool switchNow = EditorUtility.DisplayDialog(
                    "Switch to WebGL?",
                    "WebGL settings and ShapeEscapeScene have been configured.\n\nWould you like to switch the active build target to WebGL now?",
                    "Switch to WebGL",
                    "Later"
                );

                if (switchNow)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "WebGL Configured",
                    "WebGL PlayerSettings and EditorBuildSettings (ShapeEscapeScene) are configured and ready!",
                    "OK"
                );
            }
        }

        private static void ExecuteWebGLBuild(string outputPath, BuildOptions extraOptions)
        {
            if (!File.Exists(ShapeScenePath))
            {
                Debug.LogError($"[WebGLBuildTools] Scene Not Found: {ShapeScenePath}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Scene Not Found",
                        $"Could not find scene at:\n{ShapeScenePath}",
                        "OK"
                    );
                }
                return;
            }

            ApplyRecommendedWebGLSettings();
            EnsureShapeSceneInBuildSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[WebGLBuildTools] Switching active build target to WebGL...");
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                if (!switched)
                {
                    Debug.LogError("[WebGLBuildTools] Failed to switch to WebGL target.");
                    if (!Application.isBatchMode)
                    {
                        EditorUtility.DisplayDialog(
                            "WebGL Module Required",
                            "Failed to switch to WebGL target. Please verify that the 'WebGL Build Support' module is installed in Unity Hub.",
                            "OK"
                        );
                    }
                    return;
                }
            }

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ShapeScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = extraOptions
            };

            Debug.Log($"[WebGLBuildTools] Starting WebGL build -> {outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[WebGLBuildTools] WebGL Build Succeeded! Size: {sizeMb:F2} MB | Time: {summary.totalTime.TotalSeconds:F1}s | Output: {outputPath}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(Path.Combine(outputPath, "index.html"));
                }
            }
            else
            {
                Debug.LogError($"[WebGLBuildTools] WebGL Build finished with status: {summary.result} ({summary.totalErrors} errors).");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void ApplyRecommendedWebGLSettings()
        {
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            // Use Disabled compression by default so the WebGL build can be hosted on any static server/itch.io/GitLab Pages without Content-Encoding header issues
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            AssetDatabase.SaveAssets();
        }

        private static void EnsureShapeSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Remove existing entry if present so we can place ShapeEscapeScene first (index 0)
            scenes.RemoveAll(s => s.path == ShapeScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ShapeScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif

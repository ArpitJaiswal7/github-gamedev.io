using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Meowsweeper.Core;

namespace Meowsweeper.Editor
{
    public static class WebGLBuildScript
    {
        public static void BuildWebGLHeadless()
        {
            Debug.Log("[WebGLBuildScript] Starting automated headless WebGL build...");

            // 1. Create Game Scene with Main Camera & Core Systems
            string scenePath = "Assets/Scenes/MeowsweeperMain.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Add Main Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f); // Dark #0d1117 background
            camObj.transform.position = new Vector3(0, 0, -10);

            // Add Core Game System Components
            var sysObj = new GameObject("MeowsweeperGameSystem");
            sysObj.AddComponent<LevelSyncService>();
            sysObj.AddComponent<MeowGameManager>();
            sysObj.AddComponent<MeowGameUI>();

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[WebGLBuildScript] Created scene at {scenePath}");

            // Configure WebGL Build Settings
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // For direct static hosting without server gzip/br headers
            PlayerSettings.productName = "Meowsweeper Core";
            PlayerSettings.companyName = "ArpitJaiswal";

            string buildOutputDir = Path.GetFullPath("../github-gamedev.io/labs/unity-meowsweeper");
            string indexPath = Path.Combine(buildOutputDir, "index.html");
            string customHtml = File.Exists(indexPath) ? File.ReadAllText(indexPath) : null;

            if (!Directory.Exists(buildOutputDir))
            {
                Directory.CreateDirectory(buildOutputDir);
            }

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new string[] { scenePath },
                locationPathName = buildOutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuildScript] WebGL Build Succeeded! Size: {summary.totalSize} bytes. Output: {buildOutputDir}");
                
                // Restore custom index.html if we preserved it
                if (!string.IsNullOrEmpty(customHtml))
                {
                    File.WriteAllText(indexPath, customHtml);
                    Debug.Log("[WebGLBuildScript] Restored custom responsive index.html.");
                }
            }
            else
            {
                Debug.LogError($"[WebGLBuildScript] WebGL Build Failed with result: {summary.result}");
                EditorApplication.Exit(1);
            }
        }
    }
}

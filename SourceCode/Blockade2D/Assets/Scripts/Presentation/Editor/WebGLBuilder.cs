using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Blockade2D.Presentation.Editor
{
    [InitializeOnLoad]
    public static class WebGLBuilder
    {
        public const string BuildOutputDirectory = "Builds/WebGL";
        public const string MainScenePath = "Assets/Scenes/MainGame.unity";
        private static readonly string TriggerFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "start_webgl_build.flag");

        static WebGLBuilder()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TriggerFilePath))
            {
                try
                {
                    File.Delete(TriggerFilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WebGLBuilder] Could not delete trigger file: {ex.Message}");
                }

                Debug.Log("<color=#10B981>[WebGLBuilder]</color> Detected build trigger flag! Launching WebGL build now...");
                PerformBuild(false);
            }
        }

        [MenuItem("Blockade2D/Build WebGL", priority = 100)]
        public static void BuildWebGL()
        {
            PerformBuild(false);
        }

        [MenuItem("Blockade2D/Build and Run WebGL", priority = 101)]
        public static void BuildAndRunWebGL()
        {
            PerformBuild(true);
        }

        public static bool PerformBuild(bool runAfterBuild)
        {
            Debug.Log("<color=#3B82F6>[WebGLBuilder]</color> Starting WebGL Build pipeline...");

            // 1. Ensure target directory exists
            string absoluteBuildPath = Path.Combine(Directory.GetCurrentDirectory(), BuildOutputDirectory);
            if (!Directory.Exists(absoluteBuildPath))
            {
                Directory.CreateDirectory(absoluteBuildPath);
            }

            // 2. Configure WebGL Player Settings for optimal compatibility and speed
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // Enables fallback decompression for local HTTP servers
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 600;
            PlayerSettings.runInBackground = true;

            // 3. Switch active build target if needed
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("<color=#3B82F6>[WebGLBuilder]</color> Switching active build target to WebGL...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            // 4. Collect scenes to build
            string[] scenes = { MainScenePath };
            if (!File.Exists(MainScenePath))
            {
                Debug.LogError($"[WebGLBuilder] Main scene not found at {MainScenePath}!");
                return false;
            }

            // 5. Configure BuildPlayerOptions
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildOutputDirectory,
                target = BuildTarget.WebGL,
                options = runAfterBuild ? BuildOptions.AutoRunPlayer : BuildOptions.None
            };

            Debug.Log($"<color=#3B82F6>[WebGLBuilder]</color> Building scenes: {string.Join(", ", scenes)} to {BuildOutputDirectory}...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#10B981>[WebGLBuilder] BUILD SUCCEEDED!</color> Size: {summary.totalSize / (1024f * 1024f):F2} MB in {summary.totalTime.TotalSeconds:F1}s.");
                return true;
            }
            else
            {
                Debug.LogError($"<color=#EF4444>[WebGLBuilder] BUILD FAILED!</color> Result: {summary.result}, Errors: {summary.totalErrors}");
                return false;
            }
        }
    }
}

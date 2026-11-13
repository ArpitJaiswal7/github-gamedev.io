using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ThisIsBlast.Editor
{
    public static class WebGLBuildScript
    {
        private static readonly string[] Scenes = { "Assets/Scenes/MainGame.unity" };
        private static readonly string OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL");

        [MenuItem("Build/Build WebGL for Live Git")]
        public static void BuildWebGL()
        {
            Debug.Log("<color=cyan>[WebGLBuild] Starting WebGL Build...</color>");

            // 1. Ensure target directory exists
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            // 2. Configure WebGL Player Settings for GitHub Pages compatibility
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            // GitHub Pages requires decompression fallback or disabled compression to avoid MIME/header errors
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.runInBackground = true;

            // 3. Build Player Options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[WebGLBuild] Building to: {OutputFolder}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[WebGLBuild] SUCCESS! Total size: {summary.totalSize / (1024 * 1024)} MB in {summary.totalTime.TotalSeconds:F1}s</color>");
                EditorUtility.RevealInFinder(OutputFolder);
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[WebGLBuild] FAILED with {summary.totalErrors} errors!");
            }
        }
    }
}

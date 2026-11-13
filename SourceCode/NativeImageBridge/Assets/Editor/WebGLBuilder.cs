using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Studio.NativeImagePipeline;

[InitializeOnLoad]
public static class WebGLBuilder
{
    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = "Assets/Scenes/NativeImagePipelineDemo.unity";

    static WebGLBuilder()
    {
        EditorApplication.delayCall += EnsureDemoSceneAndWebGLSettings;
    }

    [MenuItem("Studio/Native Image Pipeline/1. Setup Demo Scene & WebGL Settings")]
    public static void EnsureDemoSceneAndWebGLSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Always configure WebGL PlayerSettings for clean static hosting
        PlayerSettings.companyName = "Studio";
        PlayerSettings.productName = "Native Image Bridge Demo";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 800;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.decompressionFallback = false;

        if (!File.Exists(ScenePath))
        {
            CreateAndSaveDemoScene();
        }
    }

    private static void CreateAndSaveDemoScene()
    {
        if (!Directory.Exists(SceneDir))
        {
            Directory.CreateDirectory(SceneDir);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f, 1f);

        var harnessGo = new GameObject("NativeImagePipelineDemoHarness");
        var harness = harnessGo.AddComponent<NativeImagePipelineTestHarness>();
        harness.testPuzzleCount = 120;
        harness.includeFaultyTestUrls = true;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.Refresh();
        Debug.Log($"[WebGLBuilder] Auto-created '{ScenePath}' and configured WebGL PlayerSettings.");
    }

    [MenuItem("Studio/Native Image Pipeline/2. Build WebGL Demo (Build/WebGL)")]
    public static void BuildWebGL()
    {
        try
        {
            Debug.Log("[WebGLBuilder] Starting Native Image Bridge WebGL Demo Build...");

            if (!File.Exists(ScenePath))
            {
                CreateAndSaveDemoScene();
            }
            EnsureDemoSceneAndWebGLSettings();

            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Build", "WebGL");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuilder] SUCCESS! WebGL Demo built to: {outputPath} ({summary.totalSize} bytes)");
                if (!Application.isBatchMode)
                {
                    StartLocalServerAndOpenBrowser(outputPath);
                }
                else
                {
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                Debug.LogError($"[WebGLBuilder] FAILED: {summary.result} (Errors: {summary.totalErrors})");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebGLBuilder] Exception during build: {ex}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    [MenuItem("Studio/Native Image Pipeline/3. Serve & Open WebGL Build (http://localhost:8080)")]
    public static void ServeExistingWebGLBuild()
    {
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Build", "WebGL");
        if (!Directory.Exists(outputPath) || !File.Exists(Path.Combine(outputPath, "index.html")))
        {
            Debug.LogWarning("[WebGLBuilder] Build/WebGL/index.html not found yet. Running BuildWebGL first...");
            BuildWebGL();
            return;
        }
        StartLocalServerAndOpenBrowser(outputPath);
    }

    private static void StartLocalServerAndOpenBrowser(string buildDir)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c npx.cmd --yes serve \"{buildDir}\" -l 8080",
                UseShellExecute = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized
            };
            System.Diagnostics.Process.Start(psi);
            Application.OpenURL("http://localhost:8080");
            Debug.Log($"[WebGLBuilder] Started local server at http://localhost:8080 serving '{buildDir}'.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebGLBuilder] Could not auto-start npx.cmd server: {ex.Message}");
            EditorUtility.RevealInFinder(Path.Combine(buildDir, "index.html"));
        }
    }
}

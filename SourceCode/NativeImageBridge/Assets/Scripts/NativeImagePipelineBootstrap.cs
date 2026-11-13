using UnityEngine;
using Studio.NativeImagePipeline;

namespace Studio.NativeImagePipeline.Demo
{
    /// <summary>
    /// Auto-bootstraps the NativeImagePipelineTestHarness and Main Camera in WebGL / Standalone builds.
    /// Contains ONLY the Native Image Bridge Demo (no Calm Jigsaw or Jigsaw Solitaire data).
    /// </summary>
    public static class NativeImagePipelineBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeDemoScene()
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f, 1f);
            }

            if (Object.FindFirstObjectByType<NativeImagePipelineTestHarness>() == null)
            {
                var harnessGo = new GameObject("NativeImagePipelineDemoHarness");
                Object.DontDestroyOnLoad(harnessGo);
                var harness = harnessGo.AddComponent<NativeImagePipelineTestHarness>();
                harness.testPuzzleCount = 120;
                harness.includeFaultyTestUrls = true;
            }
        }
    }
}

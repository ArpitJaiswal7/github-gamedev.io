using ThisIsBlast.Arsenal;
using ThisIsBlast.Combat;
using ThisIsBlast.Effects;
using ThisIsBlast.EnemyGrid;
using ThisIsBlast.UI;
using UnityEngine;

namespace ThisIsBlast.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart()
        {
            if (GameManager.Instance != null) return;

            Debug.Log("<color=cyan>[Bootstrapper] Initializing Game Systems...</color>");

            // 1. Setup Camera if missing or adjust camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, -0.4f, -11f);
            cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f); // Slight 3D top-down tilt
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.98f); // Casual vibrant sky-blue background matching competitor reference
            cam.orthographic = true;
            cam.orthographicSize = 7.2f; // Accommodates Enemy Grid, Shooting Bench, and Player Stack with balanced margins!
            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
            if (cam.GetComponent<CameraShaker>() == null)
            {
                cam.gameObject.AddComponent<CameraShaker>();
            }

            // 2. Setup Directional Light
            var light = FindFirstObjectByType<Light>();
            if (light == null)
            {
                var lightGo = new GameObject("Directional Light");
                light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.color = Color.white;
                light.intensity = 1.15f;
            }

            // 3. Core Systems Container
            var systemsGo = new GameObject("--- SYSTEMS ---");

            systemsGo.AddComponent<RemoteConfigService>();
            systemsGo.AddComponent<LiveOpsManager>();
            systemsGo.AddComponent<AnalyticsManager>();
            systemsGo.AddComponent<SoundManager>();
            systemsGo.AddComponent<ParticleEffectsManager>();
            systemsGo.AddComponent<ProjectilePool>();
            systemsGo.AddComponent<ArsenalMergeController>();
            systemsGo.AddComponent<EnemyGridManager>();
            systemsGo.AddComponent<ArsenalManager>();
            systemsGo.AddComponent<GameManager>();

            // 4. UI Manager
            var uiGo = new GameObject("--- UI_MANAGER ---");
            uiGo.AddComponent<UIManager>();

            Debug.Log("<color=green>[Bootstrapper] All Systems Initialized Successfully!</color>");
        }
    }
}

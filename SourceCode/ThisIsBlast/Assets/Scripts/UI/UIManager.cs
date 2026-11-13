using System;
using ThisIsBlast.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ThisIsBlast.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Canvas _canvas;
        private HUDController _hud;
        private WinPopup _winPopup;
        private FailPopup _failPopup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupCanvasAndUI();
        }

        private void SetupCanvasAndUI()
        {
            // Canvas
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Mobile portrait resolution
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists in scene
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // HUD
            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(transform, false);
            var hudRect = hudGo.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
            _hud = hudGo.AddComponent<HUDController>();

            // Win Popup
            var winGo = new GameObject("WinPopup");
            winGo.transform.SetParent(transform, false);
            _winPopup = winGo.AddComponent<WinPopup>();
            _winPopup.Initialize(OnNextLevelClicked);

            // Fail Popup
            var failGo = new GameObject("FailPopup");
            failGo.transform.SetParent(transform, false);
            _failPopup = failGo.AddComponent<FailPopup>();
            _failPopup.Initialize(OnRetryClicked, OnReviveAdClicked);
        }

        public void ShowWinPopup(int stars, int gems)
        {
            _winPopup?.Show(stars, gems);
        }

        public void ShowFailPopup(string reason)
        {
            _failPopup?.Show(reason);
        }

        private void OnNextLevelClicked()
        {
            GameManager.Instance?.LoadNextLevel();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.RestartLevel();
        }

        private void OnReviveAdClicked()
        {
            GameManager.Instance?.ReviveWithAd();
        }
    }
}

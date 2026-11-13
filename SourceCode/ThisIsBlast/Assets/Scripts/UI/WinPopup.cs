using System;
using ThisIsBlast.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ThisIsBlast.UI
{
    public class WinPopup : MonoBehaviour
    {
        private Text _starsText;
        private Text _gemsText;
        private Action _onNextLevel;

        public void Initialize(Action onNextLevel)
        {
            _onNextLevel = onNextLevel;
            SetupVisuals();
            gameObject.SetActive(false);
        }

        private void SetupVisuals()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Panel background
            var img = gameObject.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Content Container
            var container = new GameObject("Container");
            container.transform.SetParent(transform, false);
            var contRect = container.AddComponent<RectTransform>();
            contRect.sizeDelta = new Vector2(450, 480);
            contRect.anchoredPosition = Vector2.zero;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(container.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 38;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "VICTORY!";
            titleText.color = new Color(1f, 0.85f, 0.15f);
            titleText.alignment = TextAnchor.MiddleCenter;

            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);
            titleRect.sizeDelta = new Vector2(400, 60);

            // Stars Text
            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(container.transform, false);
            _starsText = starsGo.AddComponent<Text>();
            _starsText.font = font;
            _starsText.fontSize = 44;
            _starsText.text = "★★★";
            _starsText.color = new Color(1f, 0.85f, 0.15f);
            _starsText.alignment = TextAnchor.MiddleCenter;

            var starsRect = starsGo.GetComponent<RectTransform>();
            starsRect.anchoredPosition = new Vector2(0, 75);
            starsRect.sizeDelta = new Vector2(400, 60);

            // Gems info
            var gemsGo = new GameObject("GemsReward");
            gemsGo.transform.SetParent(container.transform, false);
            _gemsText = gemsGo.AddComponent<Text>();
            _gemsText.font = font;
            _gemsText.fontSize = 28;
            _gemsText.text = "+10 GEMS 💎";
            _gemsText.color = new Color(0.2f, 0.95f, 1f);
            _gemsText.alignment = TextAnchor.MiddleCenter;

            var gemsRect = gemsGo.GetComponent<RectTransform>();
            gemsRect.anchoredPosition = new Vector2(0, 0);
            gemsRect.sizeDelta = new Vector2(400, 50);

            // Next Button
            var btnGo = new GameObject("NextButton");
            btnGo.transform.SetParent(container.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.82f, 0.4f);

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                _onNextLevel?.Invoke();
            });

            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -110);
            btnRect.sizeDelta = new Vector2(280, 70);

            var btnTextGo = new GameObject("BtnText");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = font;
            btnText.fontSize = 28;
            btnText.fontStyle = FontStyle.Bold;
            btnText.text = "NEXT LEVEL";
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;

            var btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
        }

        public void Show(int stars, int gemsEarned)
        {
            gameObject.SetActive(true);
            if (_starsText != null)
            {
                string s = "";
                for (int i = 0; i < stars; i++) s += "★";
                _starsText.text = s;
            }
            if (_gemsText != null)
            {
                _gemsText.text = $"+{gemsEarned} GEMS 💎";
            }
        }
    }
}

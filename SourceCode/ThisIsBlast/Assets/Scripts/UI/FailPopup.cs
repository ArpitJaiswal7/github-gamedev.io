using System;
using ThisIsBlast.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ThisIsBlast.UI
{
    public class FailPopup : MonoBehaviour
    {
        private Text _reasonText;
        private Action _onRetry;
        private Action _onReviveAd;

        public void Initialize(Action onRetry, Action onReviveAd)
        {
            _onRetry = onRetry;
            _onReviveAd = onReviveAd;
            SetupVisuals();
            gameObject.SetActive(false);
        }

        private void SetupVisuals()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Panel background
            var img = gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.08f, 0.1f, 0.94f);

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Container
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
            titleText.text = "LEVEL FAILED";
            titleText.color = new Color(0.96f, 0.28f, 0.32f);
            titleText.alignment = TextAnchor.MiddleCenter;

            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);
            titleRect.sizeDelta = new Vector2(400, 60);

            // Reason
            var reasonGo = new GameObject("Reason");
            reasonGo.transform.SetParent(container.transform, false);
            _reasonText = reasonGo.AddComponent<Text>();
            _reasonText.font = font;
            _reasonText.fontSize = 24;
            _reasonText.text = "Out of Ammo!";
            _reasonText.color = new Color(0.8f, 0.8f, 0.85f);
            _reasonText.alignment = TextAnchor.MiddleCenter;

            var reasonRect = reasonGo.GetComponent<RectTransform>();
            reasonRect.anchoredPosition = new Vector2(0, 75);
            reasonRect.sizeDelta = new Vector2(400, 50);

            // Revive / Watch Ad Button (+10 Bullets)
            var adBtnGo = new GameObject("ReviveButton");
            adBtnGo.transform.SetParent(container.transform, false);
            var adImg = adBtnGo.AddComponent<Image>();
            adImg.color = new Color(0.18f, 0.58f, 0.98f);

            var adBtn = adBtnGo.AddComponent<Button>();
            adBtn.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                _onReviveAd?.Invoke();
            });

            var adRect = adBtnGo.GetComponent<RectTransform>();
            adRect.anchoredPosition = new Vector2(0, -10);
            adRect.sizeDelta = new Vector2(300, 65);

            var adTextGo = new GameObject("AdText");
            adTextGo.transform.SetParent(adBtnGo.transform, false);
            var adText = adTextGo.AddComponent<Text>();
            adText.font = font;
            adText.fontSize = 22;
            adText.fontStyle = FontStyle.Bold;
            adText.text = "📺 +10 BULLETS (AD)";
            adText.color = Color.white;
            adText.alignment = TextAnchor.MiddleCenter;

            var adTextRect = adTextGo.GetComponent<RectTransform>();
            adTextRect.anchorMin = Vector2.zero;
            adTextRect.anchorMax = Vector2.one;
            adTextRect.offsetMin = Vector2.zero;
            adTextRect.offsetMax = Vector2.zero;

            // Retry Button
            var retryBtnGo = new GameObject("RetryButton");
            retryBtnGo.transform.SetParent(container.transform, false);
            var retryImg = retryBtnGo.AddComponent<Image>();
            retryImg.color = new Color(0.4f, 0.45f, 0.52f);

            var retryBtn = retryBtnGo.AddComponent<Button>();
            retryBtn.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                _onRetry?.Invoke();
            });

            var retryRect = retryBtnGo.GetComponent<RectTransform>();
            retryRect.anchoredPosition = new Vector2(0, -95);
            retryRect.sizeDelta = new Vector2(300, 65);

            var retryTextGo = new GameObject("RetryText");
            retryTextGo.transform.SetParent(retryBtnGo.transform, false);
            var retryText = retryTextGo.AddComponent<Text>();
            retryText.font = font;
            retryText.fontSize = 26;
            retryText.fontStyle = FontStyle.Bold;
            retryText.text = "RETRY";
            retryText.color = Color.white;
            retryText.alignment = TextAnchor.MiddleCenter;

            var retryTextRect = retryTextGo.GetComponent<RectTransform>();
            retryTextRect.anchorMin = Vector2.zero;
            retryTextRect.anchorMax = Vector2.one;
            retryTextRect.offsetMin = Vector2.zero;
            retryTextRect.offsetMax = Vector2.zero;
        }

        public void Show(string reason = "Out of Ammo!")
        {
            gameObject.SetActive(true);
            if (_reasonText != null) _reasonText.text = reason;
        }
    }
}

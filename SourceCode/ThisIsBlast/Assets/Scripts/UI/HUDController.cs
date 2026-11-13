using System.Collections;
using ThisIsBlast.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ThisIsBlast.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        private Text _levelText;
        private Text _gemsText;
        private Text _scoreText;
        private Text _bannerText;
        private CanvasGroup _bannerGroup;
        private Coroutine _bannerCoroutine;

        // OmniPace LiveOps HUD elements
        private Text _archetypeBtnText;
        private Text _archetypeSubText;
        private Button _archetypeToggleBtn;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetupHUDVisuals();

            GameEvents.OnGemsChanged += UpdateGems;
            GameEvents.OnScoreChanged += UpdateScore;
            GameEvents.OnComboChain += ShowComboBanner;

            if (LiveOpsManager.Instance != null)
            {
                LiveOpsManager.Instance.OnLiveOpsStateChanged += UpdateLiveOpsHUD;
            }
            UpdateLiveOpsHUD();
        }

        private void OnDestroy()
        {
            GameEvents.OnGemsChanged -= UpdateGems;
            GameEvents.OnScoreChanged -= UpdateScore;
            GameEvents.OnComboChain -= ShowComboBanner;

            if (LiveOpsManager.Instance != null)
            {
                LiveOpsManager.Instance.OnLiveOpsStateChanged -= UpdateLiveOpsHUD;
            }
        }

        private void SetupHUDVisuals()
        {
            // Level indicator (top-center pill style matching reference)
            var levelGo = new GameObject("LevelText");
            levelGo.transform.SetParent(transform, false);
            _levelText = levelGo.AddComponent<Text>();
            _levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _levelText.fontSize = 32;
            _levelText.fontStyle = FontStyle.Bold;
            _levelText.color = new Color(0.12f, 0.20f, 0.35f); // Deep navy on light banner
            _levelText.alignment = TextAnchor.MiddleCenter;

            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 1f);
            levelRect.anchorMax = new Vector2(0.5f, 1f);
            levelRect.pivot = new Vector2(0.5f, 1f);
            levelRect.anchoredPosition = new Vector2(0, -25);
            levelRect.sizeDelta = new Vector2(320, 55);

            // Gems indicator (top-right)
            var gemsGo = new GameObject("GemsText");
            gemsGo.transform.SetParent(transform, false);
            _gemsText = gemsGo.AddComponent<Text>();
            _gemsText.font = _levelText.font;
            _gemsText.fontSize = 28;
            _gemsText.fontStyle = FontStyle.Bold;
            _gemsText.color = new Color(0.2f, 0.95f, 1f);
            _gemsText.alignment = TextAnchor.MiddleRight;

            var gemsRect = gemsGo.GetComponent<RectTransform>();
            gemsRect.anchorMin = new Vector2(1, 1);
            gemsRect.anchorMax = new Vector2(1, 1);
            gemsRect.pivot = new Vector2(1, 1);
            gemsRect.anchoredPosition = new Vector2(-25, -25);
            gemsRect.sizeDelta = new Vector2(250, 50);

            // Combo / Chain banner (middle screen)
            var bannerGo = new GameObject("BannerText");
            bannerGo.transform.SetParent(transform, false);
            _bannerText = bannerGo.AddComponent<Text>();
            _bannerText.font = _levelText.font;
            _bannerText.fontSize = 40;
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.color = new Color(1f, 0.85f, 0.1f);
            _bannerText.alignment = TextAnchor.MiddleCenter;

            var bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.65f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.65f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = new Vector2(600, 100);

            _bannerGroup = bannerGo.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;

            // OmniPace LiveOps Archetype Toggle Button (top-left)
            var archBtnGo = new GameObject("ArchetypePillButton");
            archBtnGo.transform.SetParent(transform, false);

            var img = archBtnGo.AddComponent<Image>();
            img.color = new Color(0.06f, 0.10f, 0.18f, 0.90f);

            _archetypeToggleBtn = archBtnGo.AddComponent<Button>();
            _archetypeToggleBtn.onClick.AddListener(OnArchetypeToggleClicked);

            var archRect = archBtnGo.GetComponent<RectTransform>();
            archRect.anchorMin = new Vector2(0f, 1f);
            archRect.anchorMax = new Vector2(0f, 1f);
            archRect.pivot = new Vector2(0f, 1f);
            archRect.anchoredPosition = new Vector2(20, -20);
            archRect.sizeDelta = new Vector2(260, 56);

            // Title text inside Archetype Pill
            var archTitleGo = new GameObject("ArchetypeTitle");
            archTitleGo.transform.SetParent(archBtnGo.transform, false);
            _archetypeBtnText = archTitleGo.AddComponent<Text>();
            _archetypeBtnText.font = _levelText.font;
            _archetypeBtnText.fontSize = 17;
            _archetypeBtnText.fontStyle = FontStyle.Bold;
            _archetypeBtnText.color = new Color(0.2f, 0.95f, 1f);
            _archetypeBtnText.alignment = TextAnchor.UpperCenter;

            var titleRect = archTitleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.45f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Subtitle text inside Archetype Pill
            var archSubGo = new GameObject("ArchetypeSubtitle");
            archSubGo.transform.SetParent(archBtnGo.transform, false);
            _archetypeSubText = archSubGo.AddComponent<Text>();
            _archetypeSubText.font = _levelText.font;
            _archetypeSubText.fontSize = 12;
            _archetypeSubText.fontStyle = FontStyle.Normal;
            _archetypeSubText.color = new Color(0.7f, 0.85f, 0.95f);
            _archetypeSubText.alignment = TextAnchor.LowerCenter;

            var subRect = archSubGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0f);
            subRect.anchorMax = new Vector2(1, 0.5f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            UpdateLevel(1);
            UpdateGems(0, 0);
            UpdateLiveOpsHUD();
        }

        private void OnArchetypeToggleClicked()
        {
            if (LiveOpsManager.Instance == null) return;

            LiveOpsManager.Instance.ToggleArchetype();
            string newArch = LiveOpsManager.Instance.ActiveArchetype;

            string archLabel = "LINEAR (SMOOTH RAMP)";
            Color bannerColor = new Color(0.2f, 0.9f, 1f);

            if (newArch == "arc_sawtooth")
            {
                archLabel = "SAWTOOTH (4-ARC CLIMAX)";
                bannerColor = new Color(1f, 0.45f, 0.85f);
            }
            else if (newArch == "arc_royalRush")
            {
                archLabel = "ROYAL RUSH (CASCADE PACE)";
                bannerColor = new Color(0.85f, 0.45f, 1f);
            }

            ShowBannerMessage($"⚡ SWITCHED TO {archLabel}!", bannerColor);
        }

        public void UpdateLiveOpsHUD()
        {
            if (_archetypeBtnText == null || _archetypeSubText == null) return;

            if (LiveOpsManager.Instance == null)
            {
                _archetypeBtnText.text = "⚡ ARCH: LINEAR";
                _archetypeSubText.text = "SEED: CLB (DEFAULT)";
                return;
            }

            var lm = LiveOpsManager.Instance;
            if (lm.ActiveArchetype == "arc_sawtooth")
            {
                _archetypeBtnText.text = "⚡ ARCH: SAWTOOTH 🔄";
                _archetypeBtnText.color = new Color(1f, 0.45f, 0.9f);
            }
            else if (lm.ActiveArchetype == "arc_royalRush")
            {
                _archetypeBtnText.text = "👑 ARCH: ROYAL RUSH 🔄";
                _archetypeBtnText.color = new Color(0.85f, 0.45f, 1f);
            }
            else
            {
                _archetypeBtnText.text = "⚡ ARCH: LINEAR 🔄";
                _archetypeBtnText.color = new Color(0.2f, 0.95f, 1f);
            }

            string seedLabel = lm.ActiveSeed.ToUpper();
            if (lm.IsPityActive)
            {
                _archetypeSubText.text = $"SEED: {seedLabel} (🛡️ SILENT PITY)";
                _archetypeSubText.color = new Color(0.3f, 1f, 0.5f);
            }
            else if (lm.IsChallengeActive)
            {
                _archetypeSubText.text = $"SEED: {seedLabel} (⚡ MASTERY)";
                _archetypeSubText.color = new Color(0.9f, 0.6f, 1f);
            }
            else
            {
                _archetypeSubText.text = $"SEED: {seedLabel} • {lm.ActivePhaseName}";
                _archetypeSubText.color = new Color(0.7f, 0.85f, 0.95f);
            }
        }

        public void UpdateLevel(int level)
        {
            if (_levelText != null) _levelText.text = $"LEVEL {level}";
        }

        public void UpdateGems(int total, int delta)
        {
            if (_gemsText != null) _gemsText.text = $"💎 {total}";
        }

        public void UpdateScore(int total, int delta)
        {
            if (_scoreText != null) _scoreText.text = $"SCORE {total}";
        }

        public void ShowBannerMessage(string message, Color color)
        {
            if (_bannerText == null || _bannerGroup == null) return;
            _bannerText.text = message;
            _bannerText.color = color;

            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(DoBannerAnimation());
        }

        public void ShowComboBanner()
        {
            ShowBannerMessage("💥 CASCADE MERGE! 💥", new Color(1f, 0.85f, 0.15f));
        }

        private IEnumerator DoBannerAnimation()
        {
            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (t < 0.2f)
                {
                    _bannerGroup.alpha = t / 0.2f;
                }
                else if (t > 0.7f)
                {
                    _bannerGroup.alpha = (1f - t) / 0.3f;
                }
                else
                {
                    _bannerGroup.alpha = 1f;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _bannerGroup.alpha = 0f;
            _bannerCoroutine = null;
        }
    }
}

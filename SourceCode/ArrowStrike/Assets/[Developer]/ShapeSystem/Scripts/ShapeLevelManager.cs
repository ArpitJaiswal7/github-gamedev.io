using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowStrike.ShapeSystem
{
    public class ShapeLevelManager : MonoBehaviour
    {
        [Header("Core References")]
        public ShapeGridSystem gridManager;
        public GameObject arrowUnitPrefab;
        [Tooltip("Optional custom arrowhead sprite (pointing Up). If left null, a sleek chevron sprite is generated automatically.")]
        public Sprite arrowHeadSprite;
        public Transform arrowParent;

        [Header("Shape Level Configs (Geometric & Pixel Art)")]
        public List<ShapeLevelConfig> levelConfigs = new List<ShapeLevelConfig>();
        public int currentLevelIndex = 0;
        public bool cycleInOrder = true;
        public bool autoAdvanceOnClear = true;

        [Header("Lives Settings")]
        [Min(1)] public int maxLives = 3;
        public int currentLives = 3;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI currentLevelTxt;
        [SerializeField] private Button shuffleLevelBtn;
        [SerializeField] private Button hintBtn;

        private ShapeLevelConfig activeConfig;
        private readonly List<ShapeArrow> activeArrows = new List<ShapeArrow>();
        private string currentShapeLabel = "";
        private bool isLevelTransitioning;

        // Runtime UI references for 3 Lives & Toast Notification
        private readonly List<Image> heartImages = new List<Image>();
        private TextMeshProUGUI livesLabelTxt;
        private CanvasGroup toastCanvasGroup;
        private Image toastBgImage;
        private TextMeshProUGUI toastMessageTxt;
        private Coroutine activeToastCoroutine;

        // Runtime UI references for Level Cleared / Game Won Victory Screen
        private GameObject victoryPopupGO;
        private TextMeshProUGUI victoryTitleTxt;
        private TextMeshProUGUI victorySubtitleTxt;
        private TextMeshProUGUI victoryStarsTxt;

        private static Sprite cachedHeartSprite;

        private void Start()
        {
            if (arrowParent == null && transform.childCount > 0)
                arrowParent = transform.GetChild(transform.childCount - 1);

            if (shuffleLevelBtn != null)
                shuffleLevelBtn.onClick.AddListener(OnNextOrShuffleClicked);

            if (hintBtn != null)
                hintBtn.onClick.AddListener(TriggerHintArrow);

            EnsureLivesAndToastUI();
            StartCoroutine(LoadLevelRoutine(currentLevelIndex));
        }

        private void OnDisable()
        {
            if (shuffleLevelBtn != null)
                shuffleLevelBtn.onClick.RemoveListener(OnNextOrShuffleClicked);

            if (hintBtn != null)
                hintBtn.onClick.RemoveListener(TriggerHintArrow);
        }

        private void Update()
        {
            if (!isLevelTransitioning && Input.GetKeyDown(KeyCode.H))
            {
                TriggerHintArrow();
            }
        }

        private bool CanPlayerInteract()
        {
            return !isLevelTransitioning && currentLives > 0;
        }

        public void TriggerHintArrow()
        {
            if (!CanPlayerInteract())
                return;

            // Because activeArrows are generated in guaranteed solve order, the first remaining arrow
            // in activeArrows is always guaranteed to have a clear exit path!
            for (int i = 0; i < activeArrows.Count; i++)
            {
                if (activeArrows[i] != null)
                {
                    activeArrows[i].OnArrowClicked();
                    break;
                }
            }
        }

        private void OnNextOrShuffleClicked()
        {
            if (levelConfigs == null || levelConfigs.Count == 0)
                return;

            StopAllCoroutines();
            activeToastCoroutine = null;
            if (toastCanvasGroup != null)
                toastCanvasGroup.alpha = 0f;

            if (cycleInOrder)
                currentLevelIndex = (currentLevelIndex + 1) % levelConfigs.Count;
            else
                currentLevelIndex = Random.Range(0, levelConfigs.Count);

            StartCoroutine(LoadLevelRoutine(currentLevelIndex));
        }

        public IEnumerator LoadLevelRoutine(int index)
        {
            isLevelTransitioning = true;
            HideVictoryPopup();

            if (shuffleLevelBtn != null)
                shuffleLevelBtn.interactable = false;

            ClearPreviousArrows();

            currentLives = Mathf.Max(1, maxLives);

            if (levelConfigs != null && levelConfigs.Count > 0)
            {
                currentLevelIndex = Mathf.Clamp(index, 0, levelConfigs.Count - 1);
                activeConfig = levelConfigs[currentLevelIndex];
            }

            if (activeConfig == null)
            {
                Debug.LogError("[ShapeLevelManager] No ShapeLevelConfig assigned!");
                if (shuffleLevelBtn != null)
                    shuffleLevelBtn.interactable = true;
                isLevelTransitioning = false;
                yield break;
            }

            yield return StartCoroutine(gridManager.InitializeShapedGrid(activeConfig));

            currentShapeLabel = activeConfig.shapeType == BoardShapeType.CustomTexture && activeConfig.shapeTexture != null
                ? activeConfig.levelTitle
                : $"{activeConfig.levelTitle} ({activeConfig.shapeType})";

            yield return StartCoroutine(GenerateSolvableArrowsInShape());

            UpdateLevelHeaderUI();
            UpdateLivesUI();

            if (shuffleLevelBtn != null)
                shuffleLevelBtn.interactable = true;

            isLevelTransitioning = false;
        }

        private IEnumerator GenerateSolvableArrowsInShape()
        {
            List<GeneratedArrowPlan> plans = SolvableShapeGenerator.GenerateSolvableLayout(
                gridManager,
                activeConfig.arrowLength.min,
                activeConfig.arrowLength.max,
                activeConfig.maxArrows
            );

            for (int i = 0; i < plans.Count; i++)
            {
                SpawnArrowFromPlan(plans[i]);

                // Yield occasionally so WebGL stays responsive during level setup
                if ((i + 1) % 15 == 0)
                    yield return null;
            }
        }

        private void SpawnArrowFromPlan(GeneratedArrowPlan plan)
        {
            List<Vector2Int> path = plan.path;
            if (path == null || path.Count < 2)
                return;

            if (arrowParent == null)
                arrowParent = transform;

            GameObject arrowGO = new GameObject($"Arrow_{plan.solveOrderIndex}_{path[^1].x}_{path[^1].y}");
            arrowGO.transform.SetParent(arrowParent, false);

            ShapeArrow arrow = arrowGO.AddComponent<ShapeArrow>();

            // Sample color from the shape/pixel-art texture with a subtle per-arrow tint so neighbors are distinguishable
            Color baseArrowColor = gridManager.GetCellColor(path[^1]);
            float shift = Random.Range(-0.07f, 0.07f);
            Color tintedArrowColor = new Color(
                Mathf.Clamp01(baseArrowColor.r + shift),
                Mathf.Clamp01(baseArrowColor.g + shift),
                Mathf.Clamp01(baseArrowColor.b + shift),
                1f
            );

            List<ShapeArrowUnit> spawnedUnits = new List<ShapeArrowUnit>(path.Count);

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int cellCoord = path[i];
                Vector3 worldPos = gridManager.GetWorldPosition(cellCoord);
                bool isHead = (i == path.Count - 1);

                GameObject unitGO = Instantiate(arrowUnitPrefab, worldPos, Quaternion.identity, arrowGO.transform);
                unitGO.name = isHead ? $"Head_{cellCoord.x}_{cellCoord.y}" : $"Body_{cellCoord.x}_{cellCoord.y}";

                // Scale the directional arrowhead slightly larger than the line width, while body colliders cover their cell
                unitGO.transform.localScale = isHead
                    ? Vector3.one * (gridManager.cellSizeX * 1.22f)
                    : Vector3.one * gridManager.cellSizeX;

                if (!unitGO.TryGetComponent(out ShapeArrowUnit unit))
                    unit = unitGO.AddComponent<ShapeArrowUnit>();

                Color cellColor = (activeConfig.shapeType == BoardShapeType.CustomTexture && activeConfig.useTextureColors)
                    ? gridManager.GetCellColor(cellCoord)
                    : tintedArrowColor;

                unit.Initialize(arrow, cellCoord, worldPos, cellColor, isHead, arrowHeadSprite);
                spawnedUnits.Add(unit);
            }

            arrow.Initialize(
                gridManager,
                path,
                plan.direction,
                spawnedUnits,
                OnArrowClearedFromBoard,
                OnArrowBlocked,
                CanPlayerInteract
            );
            activeArrows.Add(arrow);
        }

        private void OnArrowBlocked(ShapeArrow blockedArrow)
        {
            if (isLevelTransitioning || currentLives <= 0)
                return;

            currentLives = Mathf.Max(0, currentLives - 1);
            UpdateLivesUI();
            UpdateLevelHeaderUI();

            if (currentLives <= 0)
            {
                // Game Over / Level Fail -> show toast and load a new level
                isLevelTransitioning = true;
                StartCoroutine(HandleLevelFailRoutine());
            }
            else
            {
                string lifeWord = currentLives == 1 ? "Life" : "Lives";
                ShowToast($"Blocked! {currentLives} {lifeWord} Remaining", new Color(0.88f, 0.42f, 0.12f, 0.94f), 1.15f);
            }
        }

        private IEnumerator HandleLevelFailRoutine()
        {
            ShowToast("Game Over! Out of Lives - Loading New Level...", new Color(0.85f, 0.18f, 0.24f, 0.96f), 1.5f);
            yield return new WaitForSeconds(1.55f);
            OnNextOrShuffleClicked();
        }

        private void OnArrowClearedFromBoard(ShapeArrow clearedArrow)
        {
            activeArrows.Remove(clearedArrow);
            UpdateLevelHeaderUI();

            if (activeArrows.Count == 0 && isActiveAndEnabled && !isLevelTransitioning)
            {
                isLevelTransitioning = true;
                ShowVictoryPopup();
            }
        }

        private void ShowVictoryPopup()
        {
            EnsureVictoryPopupUI();
            if (victoryPopupGO != null)
            {
                victoryPopupGO.SetActive(true);
                if (victorySubtitleTxt != null)
                    victorySubtitleTxt.text = $"Shape: {currentShapeLabel} Completed!";

                if (victoryStarsTxt != null)
                {
                    string stars = currentLives >= 3 ? "★ ★ ★" : (currentLives == 2 ? "★ ★ ☆" : "★ ☆ ☆");
                    victoryStarsTxt.text = stars;
                }

                ShowToast($"Victory! {currentShapeLabel} Complete!", new Color(0.12f, 0.72f, 0.42f, 0.96f), 1.5f);
            }
            else
            {
                StartCoroutine(AdvanceAfterClearDelay());
            }
        }

        private void HideVictoryPopup()
        {
            if (victoryPopupGO != null)
                victoryPopupGO.SetActive(false);
        }

        private IEnumerator AdvanceAfterClearDelay()
        {
            if (currentLevelTxt != null)
                currentLevelTxt.text = $"{currentShapeLabel} Cleared!";

            ShowToast($"Level Cleared! {currentShapeLabel} Complete!", new Color(0.12f, 0.72f, 0.42f, 0.96f), 1.35f);
            yield return new WaitForSeconds(1.4f);
            OnNextOrShuffleClicked();
        }

        private void UpdateLevelHeaderUI()
        {
            if (currentLevelTxt != null)
            {
                currentLevelTxt.text = $"Shape: {currentShapeLabel}  ({activeArrows.Count} left)";
            }
        }

        private void UpdateLivesUI()
        {
            EnsureLivesAndToastUI();

            Color activeHeartColor = new Color(1f, 0.25f, 0.35f, 1f);
            Color lostHeartColor = new Color(0.3f, 0.34f, 0.4f, 0.45f);

            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] != null)
                {
                    bool alive = i < currentLives;
                    heartImages[i].color = alive ? activeHeartColor : lostHeartColor;
                    heartImages[i].transform.localScale = alive ? Vector3.one : Vector3.one * 0.82f;
                }
            }

            if (livesLabelTxt != null)
            {
                livesLabelTxt.text = $"Lives: {currentLives}/{maxLives}";
            }
        }

        public void ShowToast(string message, Color bgColor, float duration = 1.3f)
        {
            EnsureLivesAndToastUI();
            if (toastCanvasGroup == null || toastMessageTxt == null)
                return;

            if (activeToastCoroutine != null)
                StopCoroutine(activeToastCoroutine);

            activeToastCoroutine = StartCoroutine(ToastAnimationRoutine(message, bgColor, duration));
        }

        private IEnumerator ToastAnimationRoutine(string message, Color bgColor, float holdDuration)
        {
            toastMessageTxt.text = message;
            if (toastBgImage != null)
                toastBgImage.color = bgColor;

            RectTransform rt = toastCanvasGroup.GetComponent<RectTransform>();
            toastCanvasGroup.alpha = 0f;
            if (rt != null)
                rt.localScale = new Vector3(0.88f, 0.88f, 1f);

            // Fade & pop in
            float inTime = 0.14f;
            float elapsed = 0f;
            while (elapsed < inTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / inTime);
                toastCanvasGroup.alpha = t;
                if (rt != null)
                    rt.localScale = Vector3.Lerp(new Vector3(0.88f, 0.88f, 1f), Vector3.one, t);
                yield return null;
            }

            toastCanvasGroup.alpha = 1f;
            if (rt != null)
                rt.localScale = Vector3.one;

            yield return new WaitForSeconds(holdDuration);

            // Fade out
            float outTime = 0.22f;
            elapsed = 0f;
            while (elapsed < outTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / outTime);
                toastCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            toastCanvasGroup.alpha = 0f;
            activeToastCoroutine = null;
        }

        private void EnsureLivesAndToastUI()
        {
            if (toastCanvasGroup != null && heartImages.Count >= maxLives)
                return;

            Canvas parentCanvas = currentLevelTxt != null ? currentLevelTxt.canvas : FindFirstObjectByType<Canvas>();
            if (parentCanvas == null)
                return;

            Transform canvasTransform = parentCanvas.transform;
            TMP_FontAsset font = currentLevelTxt != null ? currentLevelTxt.font : null;

            // 1. Create Top-Left Lives Panel (3 Heart Icons + Label)
            if (heartImages.Count == 0)
            {
                GameObject livesPanelGO = new GameObject("LivesPanel", typeof(RectTransform), typeof(Image));
                livesPanelGO.transform.SetParent(canvasTransform, false);

                RectTransform panelRT = livesPanelGO.GetComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0f, 1f);
                panelRT.anchorMax = new Vector2(0f, 1f);
                panelRT.pivot = new Vector2(0f, 1f);
                panelRT.anchoredPosition = new Vector2(40f, -45f);
                panelRT.sizeDelta = new Vector2(280f, 64f);

                Image panelBg = livesPanelGO.GetComponent<Image>();
                panelBg.color = new Color(0.08f, 0.1f, 0.15f, 0.75f);

                Sprite heartSprite = GetOrCreateHeartSprite();
                heartImages.Clear();

                for (int i = 0; i < maxLives; i++)
                {
                    GameObject heartGO = new GameObject($"Heart_{i + 1}", typeof(RectTransform), typeof(Image));
                    heartGO.transform.SetParent(livesPanelGO.transform, false);

                    RectTransform hrt = heartGO.GetComponent<RectTransform>();
                    hrt.anchorMin = new Vector2(0f, 0.5f);
                    hrt.anchorMax = new Vector2(0f, 0.5f);
                    hrt.pivot = new Vector2(0f, 0.5f);
                    hrt.anchoredPosition = new Vector2(16f + i * 44f, 0f);
                    hrt.sizeDelta = new Vector2(36f, 36f);

                    Image img = heartGO.GetComponent<Image>();
                    img.sprite = heartSprite;
                    img.raycastTarget = false;
                    heartImages.Add(img);
                }

                GameObject labelGO = new GameObject("LivesText", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(livesPanelGO.transform, false);

                RectTransform lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(150f, 0f);
                lrt.offsetMax = new Vector2(-12f, 0f);

                livesLabelTxt = labelGO.GetComponent<TextMeshProUGUI>();
                if (font != null)
                    livesLabelTxt.font = font;
                livesLabelTxt.fontSize = 24f;
                livesLabelTxt.color = Color.white;
                livesLabelTxt.alignment = TextAlignmentOptions.MidlineLeft;
                livesLabelTxt.raycastTarget = false;
                livesLabelTxt.text = $"Lives: {currentLives}/{maxLives}";
            }

            // 2. Create Centered Toast Banner
            if (toastCanvasGroup == null)
            {
                GameObject toastGO = new GameObject("ToastBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                toastGO.transform.SetParent(canvasTransform, false);

                RectTransform trt = toastGO.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.5f, 0.12f);
                trt.anchorMax = new Vector2(0.5f, 0.12f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = Vector2.zero;
                trt.sizeDelta = new Vector2(720f, 74f);

                toastBgImage = toastGO.GetComponent<Image>();
                toastBgImage.color = new Color(0.15f, 0.18f, 0.25f, 0.95f);
                toastBgImage.raycastTarget = false;

                toastCanvasGroup = toastGO.GetComponent<CanvasGroup>();
                toastCanvasGroup.alpha = 0f;
                toastCanvasGroup.interactable = false;
                toastCanvasGroup.blocksRaycasts = false;

                GameObject msgGO = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
                msgGO.transform.SetParent(toastGO.transform, false);

                RectTransform mrt = msgGO.GetComponent<RectTransform>();
                mrt.anchorMin = Vector2.zero;
                mrt.anchorMax = Vector2.one;
                mrt.offsetMin = new Vector2(20f, 6f);
                mrt.offsetMax = new Vector2(-20f, -6f);

                toastMessageTxt = msgGO.GetComponent<TextMeshProUGUI>();
                if (font != null)
                    toastMessageTxt.font = font;
                toastMessageTxt.fontSize = 28f;
                toastMessageTxt.color = Color.white;
                toastMessageTxt.alignment = TextAlignmentOptions.Center;
                toastMessageTxt.raycastTarget = false;
            }
        }

        private static Sprite GetOrCreateHeartSprite()
        {
            if (cachedHeartSprite != null)
                return cachedHeartSprite;

            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = (y / (float)(size - 1)) * 2.4f - 1.05f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)(size - 1)) * 2.4f - 1.2f;
                    float a = nx * nx + ny * ny - 1f;
                    bool inside = (a * a * a - nx * nx * ny * ny * ny) <= 0f;
                    pixels[y * size + x] = inside ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            cachedHeartSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size
            );
            return cachedHeartSprite;
        }

        private void ClearPreviousArrows()
        {
            if (arrowParent != null)
            {
                for (int i = arrowParent.childCount - 1; i >= 0; i--)
                {
                    Transform child = arrowParent.GetChild(i);
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }

            activeArrows.Clear();
            if (gridManager != null)
                gridManager.ClearAllOccupancy();
        }

        private void EnsureVictoryPopupUI()
        {
            if (victoryPopupGO != null)
                return;

            Canvas parentCanvas = currentLevelTxt != null ? currentLevelTxt.canvas : FindFirstObjectByType<Canvas>();
            if (parentCanvas == null)
                return;

            Transform canvasTransform = parentCanvas.transform;
            TMP_FontAsset font = currentLevelTxt != null ? currentLevelTxt.font : null;

            // Modal Root Card
            victoryPopupGO = new GameObject("VictoryPopupPanel", typeof(RectTransform), typeof(Image));
            victoryPopupGO.transform.SetParent(canvasTransform, false);

            RectTransform cardRT = victoryPopupGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.anchoredPosition = Vector2.zero;
            cardRT.sizeDelta = new Vector2(460f, 320f);

            Image cardBg = victoryPopupGO.GetComponent<Image>();
            cardBg.color = new Color(0.08f, 0.11f, 0.17f, 0.95f);

            // Title
            GameObject titleGO = new GameObject("VictoryTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(victoryPopupGO.transform, false);
            RectTransform trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -28f);
            trt.sizeDelta = new Vector2(0f, 50f);

            victoryTitleTxt = titleGO.GetComponent<TextMeshProUGUI>();
            if (font != null) victoryTitleTxt.font = font;
            victoryTitleTxt.fontSize = 36f;
            victoryTitleTxt.fontStyle = FontStyles.Bold;
            victoryTitleTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
            victoryTitleTxt.alignment = TextAlignmentOptions.Center;
            victoryTitleTxt.text = "LEVEL CLEARED!";

            // Stars
            GameObject starsGO = new GameObject("VictoryStars", typeof(RectTransform), typeof(TextMeshProUGUI));
            starsGO.transform.SetParent(victoryPopupGO.transform, false);
            RectTransform srt = starsGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, -80f);
            srt.sizeDelta = new Vector2(0f, 44f);

            victoryStarsTxt = starsGO.GetComponent<TextMeshProUGUI>();
            if (font != null) victoryStarsTxt.font = font;
            victoryStarsTxt.fontSize = 32f;
            victoryStarsTxt.color = new Color(1f, 0.82f, 0.15f, 1f);
            victoryStarsTxt.alignment = TextAlignmentOptions.Center;
            victoryStarsTxt.text = "★ ★ ★";

            // Subtitle
            GameObject subGO = new GameObject("VictorySubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(victoryPopupGO.transform, false);
            RectTransform ssubrt = subGO.GetComponent<RectTransform>();
            ssubrt.anchorMin = new Vector2(0f, 0.5f);
            ssubrt.anchorMax = new Vector2(1f, 0.5f);
            ssubrt.pivot = new Vector2(0.5f, 0.5f);
            ssubrt.anchoredPosition = new Vector2(0f, -8f);
            ssubrt.sizeDelta = new Vector2(-40f, 40f);

            victorySubtitleTxt = subGO.GetComponent<TextMeshProUGUI>();
            if (font != null) victorySubtitleTxt.font = font;
            victorySubtitleTxt.fontSize = 20f;
            victorySubtitleTxt.color = new Color(0.85f, 0.9f, 0.98f, 1f);
            victorySubtitleTxt.alignment = TextAlignmentOptions.Center;
            victorySubtitleTxt.text = "Shape Completed!";

            // Next Level Button
            GameObject nextBtnGO = new GameObject("NextLevelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnGO.transform.SetParent(victoryPopupGO.transform, false);
            RectTransform nrt = nextBtnGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(0.5f, 0f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = new Vector2(85f, 32f);
            nrt.sizeDelta = new Vector2(160f, 48f);

            Image nextImg = nextBtnGO.GetComponent<Image>();
            nextImg.color = new Color(0.2f, 0.65f, 0.95f, 1f);

            Button nextBtn = nextBtnGO.GetComponent<Button>();
            nextBtn.onClick.AddListener(() =>
            {
                OnNextOrShuffleClicked();
            });

            GameObject nextTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            nextTxtGO.transform.SetParent(nextBtnGO.transform, false);
            RectTransform ntrt = nextTxtGO.GetComponent<RectTransform>();
            ntrt.anchorMin = Vector2.zero;
            ntrt.anchorMax = Vector2.one;
            ntrt.sizeDelta = Vector2.zero;

            TextMeshProUGUI nextTxt = nextTxtGO.GetComponent<TextMeshProUGUI>();
            if (font != null) nextTxt.font = font;
            nextTxt.fontSize = 20f;
            nextTxt.fontStyle = FontStyles.Bold;
            nextTxt.color = Color.white;
            nextTxt.alignment = TextAlignmentOptions.Center;
            nextTxt.text = "NEXT LEVEL";

            // Replay Button
            GameObject replayBtnGO = new GameObject("ReplayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            replayBtnGO.transform.SetParent(victoryPopupGO.transform, false);
            RectTransform rrt = replayBtnGO.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0f);
            rrt.anchorMax = new Vector2(0.5f, 0f);
            rrt.pivot = new Vector2(0.5f, 0f);
            rrt.anchoredPosition = new Vector2(-85f, 32f);
            rrt.sizeDelta = new Vector2(140f, 48f);

            Image replayImg = replayBtnGO.GetComponent<Image>();
            replayImg.color = new Color(0.25f, 0.3f, 0.4f, 1f);

            Button replayBtn = replayBtnGO.GetComponent<Button>();
            replayBtn.onClick.AddListener(() =>
            {
                StartCoroutine(LoadLevelRoutine(currentLevelIndex));
            });

            GameObject replayTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            replayTxtGO.transform.SetParent(replayBtnGO.transform, false);
            RectTransform rtrt = replayTxtGO.GetComponent<RectTransform>();
            rtrt.anchorMin = Vector2.zero;
            rtrt.anchorMax = Vector2.one;
            rtrt.sizeDelta = Vector2.zero;

            TextMeshProUGUI replayTxt = replayTxtGO.GetComponent<TextMeshProUGUI>();
            if (font != null) replayTxt.font = font;
            replayTxt.fontSize = 20f;
            replayTxt.fontStyle = FontStyles.Bold;
            replayTxt.color = Color.white;
            replayTxt.alignment = TextAlignmentOptions.Center;
            replayTxt.text = "REPLAY";

            victoryPopupGO.SetActive(false);
        }
    }
}

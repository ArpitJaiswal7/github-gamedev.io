using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Drop-in UI component for Calm Jigsaw and Jigsaw Solitaire ScrollViews.
    /// Automatically detects which puzzle thumbnails are currently inside the visible ScrollRect viewport,
    /// promotes their RequestKey strings to High priority in the native queue, and releases off-screen
    /// Texture2D/Sprite references to keep Unity VRAM bounded.
    /// </summary>
    public sealed class PuzzleViewportScrollTracker : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private float checkIntervalSeconds = 0.15f;

        private readonly List<PuzzleImageCellView> registeredCells = new List<PuzzleImageCellView>();
        private readonly List<string> visibleKeysBuffer = new List<string>(32);
        private readonly Vector3[] worldCornersA = new Vector3[4];
        private readonly Vector3[] worldCornersB = new Vector3[4];
        private float nextCheckTime;
        private bool isDirty;

        private void Awake()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            if (viewportRect == null && scrollRect != null)
                viewportRect = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

            if (scrollRect != null)
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        public void RegisterCell(PuzzleImageCellView cell)
        {
            if (cell != null && !registeredCells.Contains(cell))
            {
                registeredCells.Add(cell);
                isDirty = true;
            }
        }

        public void UnregisterCell(PuzzleImageCellView cell)
        {
            if (cell != null && registeredCells.Remove(cell))
            {
                isDirty = true;
            }
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            isDirty = true;
        }

        private void Update()
        {
            if (!isDirty || Time.unscaledTime < nextCheckTime)
                return;

            isDirty = false;
            nextCheckTime = Time.unscaledTime + checkIntervalSeconds;
            EvaluateViewportVisibility();
        }

        public void EvaluateViewportVisibility()
        {
            if (viewportRect == null || registeredCells.Count == 0)
                return;

            Rect vpWorldRect = GetWorldRect(viewportRect, worldCornersA);
            visibleKeysBuffer.Clear();

            for (int i = 0; i < registeredCells.Count; i++)
            {
                var cell = registeredCells[i];
                if (cell == null || !cell.gameObject.activeInHierarchy || string.IsNullOrEmpty(cell.RequestKey))
                    continue;

                Rect cellWorldRect = GetWorldRect(cell.RectTransform, worldCornersB);
                bool isVisible = vpWorldRect.Overlaps(cellWorldRect, true);

                cell.SetViewportVisibility(isVisible);
                if (isVisible)
                {
                    visibleKeysBuffer.Add(cell.RequestKey);
                }
            }

            NativeImagePipeline.Instance.PrioritizeViewportItems(visibleKeysBuffer);
        }

        private static Rect GetWorldRect(RectTransform rt, Vector3[] cornersBuffer)
        {
            rt.GetWorldCorners(cornersBuffer);
            float xMin = cornersBuffer[0].x;
            float yMin = cornersBuffer[0].y;
            float xMax = cornersBuffer[2].x;
            float yMax = cornersBuffer[2].y;
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }

    /// <summary>
    /// Individual puzzle gallery card view that binds a puzzle RequestKey string (or URL)
    /// to a Unity UI Image component and manages automatic Sprite loading/releasing as it scrolls.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PuzzleImageCellView : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private int displayResolutionPx = (int)ImageResolutionTier.Icon256;
        [SerializeField] private bool releaseSpriteWhenOffscreen = true;

        private RectTransform rectTransform;
        private string requestKey;
        private bool isVisibleInViewport;
        private bool hasLoadedSprite;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
        public string RequestKey => requestKey;

        /// <summary>
        /// Binds this UI card to a puzzle URL or RequestKey string.
        /// </summary>
        public void Bind(string puzzleUrlOrKey, int resolutionPx = 256)
        {
            if (hasLoadedSprite && !string.IsNullOrEmpty(requestKey))
            {
                NativeImagePipeline.Instance.ReleaseSprite(requestKey, displayResolutionPx);
                hasLoadedSprite = false;
            }

            displayResolutionPx = resolutionPx > 0 ? resolutionPx : 256;
            requestKey = puzzleUrlOrKey.StartsWith("http", System.StringComparison.OrdinalIgnoreCase)
                ? NativeImagePipeline.Instance.EnqueueUrl(puzzleUrlOrKey, RequestPriority.Low)
                : puzzleUrlOrKey;

            if (isVisibleInViewport)
            {
                RequestCellSprite();
            }
        }

        internal void SetViewportVisibility(bool visible)
        {
            if (isVisibleInViewport == visible)
                return;

            isVisibleInViewport = visible;
            if (visible)
            {
                RequestCellSprite();
            }
            else if (releaseSpriteWhenOffscreen && hasLoadedSprite && !string.IsNullOrEmpty(requestKey))
            {
                if (targetImage != null)
                {
                    targetImage.sprite = null;
                }
                NativeImagePipeline.Instance.ReleaseSprite(requestKey, displayResolutionPx);
                hasLoadedSprite = false;
            }
        }

        private void RequestCellSprite()
        {
            if (string.IsNullOrEmpty(requestKey) || hasLoadedSprite)
                return;

            string boundKey = requestKey;
            int boundSize = displayResolutionPx;

            NativeImagePipeline.Instance.GetSpriteByCustomSize(
                boundKey,
                boundSize,
                sprite =>
                {
                    if (this == null || boundKey != requestKey || !isVisibleInViewport)
                    {
                        NativeImagePipeline.Instance.ReleaseSprite(boundKey, boundSize);
                        return;
                    }

                    hasLoadedSprite = true;
                    if (targetImage != null)
                    {
                        targetImage.sprite = sprite;
                    }
                });
        }

        private void OnDestroy()
        {
            if (hasLoadedSprite && !string.IsNullOrEmpty(requestKey))
            {
                NativeImagePipeline.Instance.ReleaseSprite(requestKey, displayResolutionPx);
                hasLoadedSprite = false;
            }
        }
    }
}

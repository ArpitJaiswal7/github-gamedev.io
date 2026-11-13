using System.Collections;
using UnityEngine;
using Blockade2D.Core.Model;

namespace Blockade2D.Presentation.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GhostWallView : MonoBehaviour
    {
        [Header("Ghost Colors")]
        [SerializeField] private Color validFadedColor = new Color(0.95f, 0.77f, 0.25f, 0.45f); // Faded Gold (same color as real wall with ~45% opacity)
        [SerializeField] private Color invalidFadedColor = new Color(0.92f, 0.22f, 0.22f, 0.55f); // Faded Crimson

        private SpriteRenderer _spriteRenderer;
        private float _length;
        private float _thickness;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sortingOrder = 6;
            gameObject.SetActive(false);
        }

        public void Setup(Sprite sprite, float length, float thickness)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                if (sprite != null)
                    _spriteRenderer.sprite = sprite;
                _spriteRenderer.sortingOrder = 6;
            }
            _length = length;
            _thickness = thickness;
        }

        public void UpdatePreview(Vector3 worldPos, WallOrientation orientation, bool isValid, Color? playerColor = null)
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            float len = _length > 0f ? _length : 1.8f;
            float thk = _thickness > 0f ? _thickness : 0.22f;

            transform.position = worldPos;
            transform.localScale = orientation == WallOrientation.Horizontal
                ? new Vector3(len, thk, 1f)
                : new Vector3(thk, len, 1f);

            if (isValid)
            {
                Color baseCol = playerColor ?? validFadedColor;
                _spriteRenderer.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.45f);
            }
            else
            {
                _spriteRenderer.color = invalidFadedColor;
            }
        }

        public void TriggerErrorShake()
        {
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);

            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private IEnumerator ShakeRoutine()
        {
            Vector3 originalPos = transform.position;
            float elapsed = 0f;
            float duration = 0.2f;
            float magnitude = 0.08f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = Mathf.Sin(elapsed * 50f) * magnitude;
                transform.position = originalPos + new Vector3(offsetX, 0f, 0f);
                yield return null;
            }

            transform.position = originalPos;
            _shakeCoroutine = null;
        }
    }
}

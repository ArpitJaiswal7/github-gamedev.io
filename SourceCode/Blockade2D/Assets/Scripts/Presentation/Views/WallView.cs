using System.Collections;
using UnityEngine;
using Blockade2D.Core.Model;

namespace Blockade2D.Presentation.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WallView : MonoBehaviour
    {
        public Wall WallData { get; private set; }

        [SerializeField] private Color wallColor = new Color(0.95f, 0.77f, 0.25f, 1f); // Vibrant Gold
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(
            Wall wall,
            Vector3 worldPos,
            Sprite sprite,
            float length,
            float thickness,
            Color? customColor = null)
        {
            WallData = wall;
            transform.position = worldPos;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = customColor ?? wallColor;
            _spriteRenderer.sortingOrder = 5; // Above tiles, below pawns

            Vector3 targetScale = wall.Orientation == WallOrientation.Horizontal
                ? new Vector3(length, thickness, 1f)
                : new Vector3(thickness, length, 1f);

            // Pop-in scale slam animation
            StartCoroutine(AnimateSlam(targetScale));
        }

        private IEnumerator AnimateSlam(Vector3 targetScale)
        {
            transform.localScale = targetScale * 1.35f;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale * 1.35f, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }
    }
}

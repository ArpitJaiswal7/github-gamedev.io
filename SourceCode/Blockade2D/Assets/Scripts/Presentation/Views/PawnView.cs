using System;
using System.Collections;
using UnityEngine;
using Blockade2D.Core.Model;

namespace Blockade2D.Presentation.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PawnView : MonoBehaviour
    {
        [Header("Player Assignment")]
        [SerializeField] private PlayerId player;

        public PlayerId Player => player;
        public BoardPosition CurrentPosition { get; private set; }

        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private float hopHeight = 0.35f;

        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _collider;
        private Coroutine _moveCoroutine;

        public event Action<PlayerId> OnPawnClicked;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
                _collider.radius = 0.5f;
            }
        }

        public void SetPlayer(PlayerId p)
        {
            player = p;
        }

        public void Initialize(PlayerId player, BoardPosition startPos, Vector3 worldPos, Sprite sprite, Color color, float size = 0.75f)
        {
            this.player = player;
            CurrentPosition = startPos;
            transform.position = worldPos;
            transform.localScale = new Vector3(size, size, 1f);

            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_collider == null) _collider = GetComponent<CircleCollider2D>();

            if (_spriteRenderer != null)
            {
                if (sprite != null)
                    _spriteRenderer.sprite = sprite;
                _spriteRenderer.color = color;
                _spriteRenderer.sortingOrder = 10; // Render above tiles
            }

            if (_collider != null)
            {
                _collider.isTrigger = true;
                _collider.radius = 0.5f;
            }
        }

        public void MoveTo(BoardPosition newGridPos, Vector3 targetWorldPos, Action onComplete = null)
        {
            CurrentPosition = newGridPos;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(AnimateHop(transform.position, targetWorldPos, onComplete));
        }

        private IEnumerator AnimateHop(Vector3 start, Vector3 target, Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                // Smooth ease-out curve
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 current = Vector3.Lerp(start, target, smoothT);

                // Add vertical hop arc
                float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
                current.y += arc;

                transform.position = current;
                yield return null;
            }

            transform.position = target;
            _moveCoroutine = null;
            onComplete?.Invoke();
        }

        private void OnMouseDown()
        {
            OnPawnClicked?.Invoke(Player);
        }
    }
}

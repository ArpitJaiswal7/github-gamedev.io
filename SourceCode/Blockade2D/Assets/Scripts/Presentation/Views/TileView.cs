using System;
using UnityEngine;
using Blockade2D.Core.Model;

namespace Blockade2D.Presentation.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class TileView : MonoBehaviour
    {
        [Header("Grid Coordinates")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        public BoardPosition Position => new BoardPosition(gridX, gridY);

        [Header("Appearance")]
        [SerializeField] private Color defaultColor = new Color(0.12f, 0.15f, 0.19f, 1f); // Dark Slate
        [SerializeField] private Color highlightColor = new Color(0.06f, 0.8f, 0.6f, 0.85f); // Emerald Neon
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.32f, 0.4f, 1f);

        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;
        private Color _baseColor;
        private bool _isHighlighted;
        private bool _isHovered;

        public event Action<BoardPosition> OnTileClicked;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            _baseColor = defaultColor;
        }

        public void SetGridCoordinates(int x, int y)
        {
            gridX = x;
            gridY = y;
        }

        public void Initialize(BoardPosition position, Sprite sprite, float tileSize, Color? customBaseColor = null)
        {
            gridX = position.X;
            gridY = position.Y;
            _baseColor = customBaseColor ?? defaultColor;
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();

            if (sprite != null)
                _spriteRenderer.sprite = sprite;

            _spriteRenderer.color = _baseColor;
            transform.localScale = new Vector3(tileSize, tileSize, 1f);
            if (_collider != null)
                _collider.size = Vector2.one;
        }

        public static bool InteractivityEnabled { get; set; } = true;

        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            if (_isHighlighted && InteractivityEnabled)
            {
                // If cursor is already resting on this tile when it becomes valid, detect hover immediately
                if (_collider != null && Camera.main != null)
                {
                    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    _isHovered = _collider.OverlapPoint(mouseWorld);
                }
                else
                {
                    _isHovered = false;
                }
            }
            else
            {
                _isHovered = false;
            }
            UpdateVisuals();
        }

        private void OnMouseEnter()
        {
            if (!InteractivityEnabled || !_isHighlighted) return;
            _isHovered = true;
            UpdateVisuals();
        }

        private void OnMouseOver()
        {
            // Fires every frame mouse rests on this collider - ensures immediate responsiveness
            if (!InteractivityEnabled || !_isHighlighted)
            {
                if (_isHovered)
                {
                    _isHovered = false;
                    UpdateVisuals();
                }
                return;
            }

            if (!_isHovered)
            {
                _isHovered = true;
                UpdateVisuals();
            }
        }

        private void OnMouseExit()
        {
            if (_isHovered)
            {
                _isHovered = false;
                UpdateVisuals();
            }
        }

        private void OnMouseDown()
        {
            if (!InteractivityEnabled || !_isHighlighted) return;
            OnTileClicked?.Invoke(Position);
        }

        private void UpdateVisuals()
        {
            if (_isHighlighted && InteractivityEnabled)
            {
                _spriteRenderer.color = _isHovered 
                    ? Color.Lerp(highlightColor, Color.white, 0.25f) 
                    : highlightColor;
            }
            else
            {
                _spriteRenderer.color = _baseColor;
            }
        }
    }
}

using System;
using UnityEngine;
using Blockade2D.Core.Model;

namespace Blockade2D.Presentation.Views
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class IntersectionSlotView : MonoBehaviour
    {
        [Header("Intersection Coordinates")]
        [SerializeField] private int intersectionX;
        [SerializeField] private int intersectionY;

        public BoardPosition Intersection => new BoardPosition(intersectionX, intersectionY);

        public event Action<BoardPosition> OnHovered;
        public event Action<BoardPosition> OnUnhovered;
        public event Action<BoardPosition> OnClicked;

        private BoxCollider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
        }

        public void SetCoordinates(int x, int y)
        {
            intersectionX = x;
            intersectionY = y;
        }

        public void Initialize(BoardPosition intersection, float triggerSize)
        {
            intersectionX = intersection.X;
            intersectionY = intersection.Y;
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider != null)
            {
                _collider.size = new Vector2(triggerSize, triggerSize);
                _collider.isTrigger = true;
            }
        }

        private void OnMouseEnter()
        {
            OnHovered?.Invoke(Intersection);
        }

        private void OnMouseExit()
        {
            OnUnhovered?.Invoke(Intersection);
        }

        private void OnMouseDown()
        {
            OnClicked?.Invoke(Intersection);
        }
    }
}

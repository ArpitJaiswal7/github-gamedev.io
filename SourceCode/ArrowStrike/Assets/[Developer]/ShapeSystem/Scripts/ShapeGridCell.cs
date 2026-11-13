using UnityEngine;

namespace ArrowStrike.ShapeSystem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShapeGridCell : MonoBehaviour
    {
        public Vector2Int gridPosition;
        public bool isOccupied;
        public GameObject occupant;
        public Color shapeColor = Color.white;

        private SpriteRenderer spriteRenderer;
        private Color baseCellColor = new Color(1f, 1f, 1f, 0.22f);

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Configure(Vector2Int pos, Color pixelColor, Color boardTint)
        {
            gridPosition = pos;
            shapeColor = pixelColor;
            baseCellColor = boardTint;
            isOccupied = false;
            occupant = null;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                spriteRenderer.color = baseCellColor;
        }

        public void OccupyCell(GameObject occupantObject)
        {
            isOccupied = true;
            occupant = occupantObject;
        }

        public void FreeCell()
        {
            isOccupied = false;
            occupant = null;
        }

        public bool IsFree() => !isOccupied;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

namespace ArrowStrike.ShapeSystem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShapeArrowUnit : MonoBehaviour, IPointerDownHandler
    {
        public Vector2Int currentCoord;
        public Color unitColor = Color.white;
        public bool isHeadUnit;

        private SpriteRenderer spriteRenderer;
        private ShapeArrow parentArrow;

        private static Sprite cachedProceduralHeadSprite;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(
            ShapeArrow owner,
            Vector2Int coord,
            Vector3 worldPos,
            Color color,
            bool isHead,
            Sprite customHeadSprite = null)
        {
            parentArrow = owner;
            currentCoord = coord;
            unitColor = color;
            isHeadUnit = isHead;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                if (isHead)
                {
                    spriteRenderer.enabled = true;
                    spriteRenderer.sprite = customHeadSprite != null ? customHeadSprite : GetOrCreateChevronSprite();
                    spriteRenderer.color = color;
                    spriteRenderer.sortingOrder = 4;
                }
                else
                {
                    // Body segments are rendered continuously by ShapeArrow's LineRenderer,
                    // while keeping this unit's collider active for click/touch detection.
                    spriteRenderer.enabled = false;
                }
            }

            transform.position = worldPos;
        }

        public void SetHeadFacingDirection(Vector2 dir)
        {
            if (!isHeadUnit || dir.sqrMagnitude < 0.0001f)
                return;

            // Default arrowhead sprite points Up (+Y)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static Sprite GetOrCreateChevronSprite()
        {
            if (cachedProceduralHeadSprite != null)
                return cachedProceduralHeadSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];

            // Rasterize an upward-pointing sleek chevron triangle
            // Apex at (0.5, 0.92), left wing at (0.08, 0.14), notch at (0.5, 0.30), right wing at (0.92, 0.14)
            Vector2 apex = new Vector2(0.5f, 0.92f);
            Vector2 leftWing = new Vector2(0.08f, 0.14f);
            Vector2 notch = new Vector2(0.5f, 0.30f);
            Vector2 rightWing = new Vector2(0.92f, 0.14f);

            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size;
                    Vector2 p = new Vector2(nx, ny);

                    bool inLeftHalf = PointInTriangle(p, apex, leftWing, notch);
                    bool inRightHalf = PointInTriangle(p, apex, notch, rightWing);

                    pixels[y * size + x] = (inLeftHalf || inRightHalf) ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            cachedProceduralHeadSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size
            );
            return cachedProceduralHeadSprite;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private void OnMouseDown()
        {
            if (parentArrow == null)
                parentArrow = GetComponentInParent<ShapeArrow>();

            parentArrow?.OnArrowClicked();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (parentArrow == null)
                parentArrow = GetComponentInParent<ShapeArrow>();

            parentArrow?.OnArrowClicked();
        }
    }
}

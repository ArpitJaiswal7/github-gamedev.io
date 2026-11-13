using System.Collections;
using System.Collections.Generic;
using ThisIsBlast.Core;
using UnityEngine;

namespace ThisIsBlast.EnemyGrid
{
    /// <summary>
    /// Row parent container that visually groups and highlights a horizontal row of cells.
    /// Contains child GridCellSlots and provides row-wide visual flash/highlighting.
    /// </summary>
    public class GridRowTrack : MonoBehaviour
    {
        public int RowIndex { get; private set; }
        public IReadOnlyList<GridCellSlot> Slots => _slots;

        private readonly List<GridCellSlot> _slots = new List<GridCellSlot>();
        private MeshRenderer _rowOverlayRenderer;
        private Material _rowOverlayMaterial;
        private Color _baseRowColor;
        private Coroutine _highlightCoroutine;

        public void Initialize(int rowIndex, float width, float height, float originX)
        {
            RowIndex = rowIndex;

            // Subtle alternating tint for rows to enhance depth and tactile board look
            _baseRowColor = (rowIndex % 2 == 0) ? new Color(0.04f, 0.05f, 0.08f, 0.85f) : new Color(0.05f, 0.06f, 0.09f, 0.85f);

            // Horizontal row overlay backing plate
            var rowOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rowOverlay.name = "RowOverlay";
            rowOverlay.transform.SetParent(transform, false);
            rowOverlay.transform.position = new Vector3(originX, transform.position.y, 0.16f);
            rowOverlay.transform.localScale = new Vector3(width, height * 0.96f, 0.04f);

            _rowOverlayMaterial = MaterialHelper.CreateStandardMaterial(_baseRowColor, 0.35f);
            _rowOverlayRenderer = rowOverlay.GetComponent<MeshRenderer>();
            _rowOverlayRenderer.material = _rowOverlayMaterial;
            Destroy(rowOverlay.GetComponent<Collider>());
        }

        public void AddSlot(GridCellSlot slot)
        {
            slot.transform.SetParent(transform, true);
            _slots.Add(slot);
        }

        public void HighlightRow(Color color, float duration = 0.25f)
        {
            if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = StartCoroutine(DoHighlightRow(color, duration));
        }

        private IEnumerator DoHighlightRow(Color color, float duration)
        {
            if (_rowOverlayMaterial == null) yield break;

            float elapsed = 0f;
            Color flashColor = Color.Lerp(_baseRowColor, color, 0.45f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _rowOverlayMaterial.color = Color.Lerp(flashColor, _baseRowColor, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _rowOverlayMaterial.color = _baseRowColor;
            _highlightCoroutine = null;
        }
    }
}

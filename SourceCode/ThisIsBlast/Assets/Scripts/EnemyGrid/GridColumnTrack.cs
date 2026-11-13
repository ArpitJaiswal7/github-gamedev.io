using System.Collections;
using System.Collections.Generic;
using ThisIsBlast.Core;
using UnityEngine;

namespace ThisIsBlast.EnemyGrid
{
    public class GridColumnTrack : MonoBehaviour
    {
        public int ColumnIndex { get; private set; }
        public IReadOnlyList<GridCellSlot> Slots => _slots;

        private readonly List<GridCellSlot> _slots = new List<GridCellSlot>();
        private MeshRenderer _trackRenderer;
        private Material _trackMaterial;
        private Color _baseTrackColor;
        private Coroutine _highlightCoroutine;

        public void Initialize(int columnIndex, float width, float height, float originY)
        {
            ColumnIndex = columnIndex;

            _baseTrackColor = (columnIndex % 2 == 0) ? new Color(0.035f, 0.045f, 0.07f, 0.92f) : new Color(0.045f, 0.055f, 0.085f, 0.92f);

            // Column vertical background track overlay
            var trackGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trackGo.name = $"TrackOverlay";
            trackGo.transform.SetParent(transform, false);
            trackGo.transform.position = new Vector3(transform.position.x, originY, 0.20f);
            trackGo.transform.localScale = new Vector3(width, height, 0.05f);

            _trackMaterial = MaterialHelper.CreateStandardMaterial(_baseTrackColor, 0.3f);
            _trackRenderer = trackGo.GetComponent<MeshRenderer>();
            _trackRenderer.material = _trackMaterial;
            Destroy(trackGo.GetComponent<Collider>());
        }

        public void AddSlot(GridCellSlot slot)
        {
            slot.transform.SetParent(transform, true);
            _slots.Add(slot);
        }

        public GridCellSlot GetLowestSlotWithBlock()
        {
            // Iterate from bottom up (highest Y coordinate index)
            GridCellSlot lowest = null;
            int maxY = -1;

            foreach (var slot in _slots)
            {
                if (slot != null && slot.HasBlock)
                {
                    if (slot.Coords.y > maxY)
                    {
                        maxY = slot.Coords.y;
                        lowest = slot;
                    }
                }
            }
            return lowest;
        }

        public void HighlightColumn(Color color, float duration = 0.2f)
        {
            if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = StartCoroutine(DoHighlightColumn(color, duration));
        }

        private IEnumerator DoHighlightColumn(Color color, float duration)
        {
            if (_trackMaterial == null) yield break;

            float elapsed = 0f;
            Color flashColor = Color.Lerp(_baseTrackColor, color, 0.35f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _trackMaterial.color = Color.Lerp(flashColor, _baseTrackColor, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _trackMaterial.color = _baseTrackColor;
            _highlightCoroutine = null;
        }
    }
}

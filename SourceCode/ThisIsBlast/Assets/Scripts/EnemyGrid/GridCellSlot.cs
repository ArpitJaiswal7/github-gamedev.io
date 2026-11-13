using System.Collections;
using ThisIsBlast.Core;
using UnityEngine;

namespace ThisIsBlast.EnemyGrid
{
    public class GridCellSlot : MonoBehaviour
    {
        public Vector2Int Coords { get; private set; }
        public EnemyBlock CurrentBlock { get; private set; }
        public bool HasBlock => CurrentBlock != null && !CurrentBlock.IsDestroyed;

        [Header("Slot Visuals")]
        [SerializeField] private MeshRenderer _socketRenderer;
        [SerializeField] private MeshRenderer _rimRenderer;

        private Material _socketMaterial;
        private Material _rimMaterial;
        private Color _baseSocketColor = new Color(0.09f, 0.12f, 0.17f);
        private Color _baseRimColor = new Color(0.05f, 0.07f, 0.10f);

        private Coroutine _highlightCoroutine;

        public void Initialize(int x, int y, float cellSize)
        {
            Coords = new Vector2Int(x, y);
            SetupVisuals(cellSize);
        }

        private void SetupVisuals(float cellSize)
        {
            // 1. Recessed dark socket plate (extends beyond block so background grid is partially visible)
            var socketGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            socketGo.name = "SlotSocket";
            socketGo.transform.SetParent(transform, false);
            socketGo.transform.localPosition = new Vector3(0, 0, 0.08f);
            socketGo.transform.localScale = new Vector3(cellSize * 1.0f, cellSize * 1.0f, 0.05f);

            _socketMaterial = MaterialHelper.CreateStandardMaterial(_baseSocketColor, 0.35f);
            _socketRenderer = socketGo.GetComponent<MeshRenderer>();
            _socketRenderer.material = _socketMaterial;
            Destroy(socketGo.GetComponent<Collider>());

            // 2. Beveled Rim Border / Grid Lattice Line
            var rimGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rimGo.name = "SlotRim";
            rimGo.transform.SetParent(transform, false);
            rimGo.transform.localPosition = new Vector3(0, 0, 0.11f);
            rimGo.transform.localScale = new Vector3(cellSize * 1.10f, cellSize * 1.10f, 0.04f);

            _rimMaterial = MaterialHelper.CreateStandardMaterial(_baseRimColor, 0.35f);
            _rimRenderer = rimGo.GetComponent<MeshRenderer>();
            _rimRenderer.material = _rimMaterial;
            Destroy(rimGo.GetComponent<Collider>());
        }

        public void SetBlock(EnemyBlock block)
        {
            CurrentBlock = block;
            if (block != null)
            {
                block.transform.SetParent(transform, true);
            }
        }

        public void ClearBlock()
        {
            CurrentBlock = null;
        }

        public void Highlight(Color highlightColor, float duration = 0.25f)
        {
            if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = StartCoroutine(DoHighlight(highlightColor, duration));
        }

        private IEnumerator DoHighlight(Color color, float duration)
        {
            if (_socketMaterial == null) yield break;

            float elapsed = 0f;
            Color flashColor = Color.Lerp(color, Color.white, 0.35f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _socketMaterial.color = Color.Lerp(flashColor, _baseSocketColor, t);
                if (_rimMaterial != null)
                {
                    _rimMaterial.color = Color.Lerp(flashColor, _baseRimColor, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            _socketMaterial.color = _baseSocketColor;
            if (_rimMaterial != null) _rimMaterial.color = _baseRimColor;
            _highlightCoroutine = null;
        }
    }
}

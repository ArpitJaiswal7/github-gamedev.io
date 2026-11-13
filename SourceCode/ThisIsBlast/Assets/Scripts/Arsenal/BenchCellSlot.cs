using System.Collections;
using ThisIsBlast.Core;
using UnityEngine;

namespace ThisIsBlast.Arsenal
{
    public class BenchCellSlot : MonoBehaviour
    {
        public int ColumnIndex { get; private set; }
        public ArsenalBlock CurrentBlock { get; private set; }
        public bool HasBlock => CurrentBlock != null && CurrentBlock.BulletCount > 0;
        public bool IsReserved { get; set; }
        public bool IsAvailable => !IsReserved && CurrentBlock == null;

        private MeshRenderer _socketRenderer;
        private MeshRenderer _rimRenderer;
        private Material _socketMaterial;
        private Material _rimMaterial;

        private Color _baseSocketColor = new Color(0.08f, 0.11f, 0.16f);
        private Color _baseRimColor = new Color(0.16f, 0.20f, 0.28f);
        private Coroutine _highlightCoroutine;

        public void Initialize(int columnIndex, float size = 1.05f)
        {
            ColumnIndex = columnIndex;
            SetupVisuals(size);
        }

        private void SetupVisuals(float size)
        {
            // 1. Recessed socket plate
            var socketGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            socketGo.name = "BenchSlotSocket";
            socketGo.transform.SetParent(transform, false);
            socketGo.transform.localPosition = new Vector3(0, 0, 0.08f);
            socketGo.transform.localScale = new Vector3(size, size, 0.06f);

            _socketMaterial = MaterialHelper.CreateStandardMaterial(_baseSocketColor, 0.4f);
            _socketRenderer = socketGo.GetComponent<MeshRenderer>();
            _socketRenderer.material = _socketMaterial;
            Destroy(socketGo.GetComponent<Collider>());

            // 2. Beveled rim border
            var rimGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rimGo.name = "BenchSlotRim";
            rimGo.transform.SetParent(transform, false);
            rimGo.transform.localPosition = new Vector3(0, 0, 0.11f);
            rimGo.transform.localScale = new Vector3(size + 0.10f, size + 0.10f, 0.05f);

            _rimMaterial = MaterialHelper.CreateStandardMaterial(_baseRimColor, 0.4f);
            _rimRenderer = rimGo.GetComponent<MeshRenderer>();
            _rimRenderer.material = _rimMaterial;
            Destroy(rimGo.GetComponent<Collider>());
        }

        public void AttachBlock(ArsenalBlock block)
        {
            CurrentBlock = block;
            IsReserved = false;
            if (block != null)
            {
                block.transform.SetParent(transform, true);
                block.SetDockedSlot(this);
            }
        }

        public void ClearBlock()
        {
            CurrentBlock = null;
            IsReserved = false;
        }

        public void Highlight(Color color, float duration = 0.22f)
        {
            if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = StartCoroutine(DoHighlight(color, duration));
        }

        private IEnumerator DoHighlight(Color color, float duration)
        {
            if (_socketMaterial == null) yield break;

            float elapsed = 0f;
            Color flashColor = Color.Lerp(color, Color.white, 0.4f);

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

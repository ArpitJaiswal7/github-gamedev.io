using System.Collections;
using System.Collections.Generic;
using ThisIsBlast.Core;
using UnityEngine;

namespace ThisIsBlast.Arsenal
{
    public class BenchRowTrack : MonoBehaviour
    {
        public IReadOnlyList<BenchCellSlot> Slots => _slots;
        private readonly List<BenchCellSlot> _slots = new List<BenchCellSlot>();

        private MeshRenderer _trayRenderer;
        private Material _trayMaterial;
        private Color _baseTrayColor = new Color(0.05f, 0.07f, 0.10f, 0.96f);
        private Coroutine _highlightCoroutine;

        public void Initialize(float width, float height)
        {
            // 1. Full horizontal black overlay tray
            var trayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trayGo.name = "Bench_BackgroundTray";
            trayGo.transform.SetParent(transform, false);
            trayGo.transform.localPosition = new Vector3(0, 0, 0.22f);
            trayGo.transform.localScale = new Vector3(width, height, 0.12f);

            _trayMaterial = MaterialHelper.CreateStandardMaterial(_baseTrayColor, 0.45f);
            _trayRenderer = trayGo.GetComponent<MeshRenderer>();
            _trayRenderer.material = _trayMaterial;
            Destroy(trayGo.GetComponent<Collider>());

            // 2. Outer beveled metallic border frame
            var rimGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rimGo.name = "Bench_TrayRim";
            rimGo.transform.SetParent(transform, false);
            rimGo.transform.localPosition = new Vector3(0, 0, 0.26f);
            rimGo.transform.localScale = new Vector3(width + 0.18f, height + 0.18f, 0.10f);

            var rimMat = new Material(_trayMaterial);
            rimMat.color = new Color(0.18f, 0.22f, 0.30f);
            rimGo.GetComponent<MeshRenderer>().material = rimMat;
            Destroy(rimGo.GetComponent<Collider>());
        }

        public void AddSlot(BenchCellSlot slot)
        {
            slot.transform.SetParent(transform, true);
            _slots.Add(slot);
        }

        public void HighlightRow(Color color, float duration = 0.3f)
        {
            if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = StartCoroutine(DoHighlightRow(color, duration));
        }

        private IEnumerator DoHighlightRow(Color color, float duration)
        {
            if (_trayMaterial == null) yield break;

            float elapsed = 0f;
            Color flashColor = Color.Lerp(_baseTrayColor, color, 0.3f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _trayMaterial.color = Color.Lerp(flashColor, _baseTrayColor, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _trayMaterial.color = _baseTrayColor;
            _highlightCoroutine = null;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace ThisIsBlast.Effects
{
    public class FloatingText : MonoBehaviour
    {
        public static void Create(Vector3 position, string text, Color color, float size = 0.35f, float duration = 0.65f)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position + new Vector3(0, 0.2f, -0.5f);

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 32;
            textMesh.characterSize = size * 0.1f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = color;

            var comp = go.AddComponent<FloatingText>();
            comp.StartCoroutine(comp.AnimateAndDestroy(textMesh, duration));
        }

        private IEnumerator AnimateAndDestroy(TextMesh tm, float duration)
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Color startColor = tm.color;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = startPos + new Vector3(0, t * 1.2f, 0);
                
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                tm.color = c;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

using System.Collections;
using UnityEngine;

namespace ThisIsBlast.Effects
{
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _originalPosition = transform.localPosition;
        }

        public void Shake(float duration = 0.12f, float magnitude = 0.15f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
        }

        private IEnumerator DoShake(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float strength = (1f - (elapsed / duration)) * magnitude;
                float x = (Random.value * 2f - 1f) * strength;
                float y = (Random.value * 2f - 1f) * strength;

                transform.localPosition = _originalPosition + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = _originalPosition;
            _shakeCoroutine = null;
        }
    }
}

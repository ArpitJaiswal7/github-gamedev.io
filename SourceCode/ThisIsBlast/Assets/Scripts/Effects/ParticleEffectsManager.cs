using System.Collections.Generic;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using UnityEngine;

namespace ThisIsBlast.Effects
{
    public class ParticleEffectsManager : MonoBehaviour
    {
        public static ParticleEffectsManager Instance { get; private set; }

        private Material _particleMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Simple additive or default particle material
            _particleMaterial = MaterialHelper.CreateParticleMaterial();
        }

        public void SpawnBlockHitParticles(Vector3 position, Color color)
        {
            SpawnBurst(position, color, 8, 0.25f, 2.5f, 0.15f);
        }

        public void SpawnBlockDestroyParticles(Vector3 position, Color color)
        {
            SpawnBurst(position, color, 20, 0.4f, 4.0f, 0.25f);
        }

        public void SpawnChestOpenParticles(Vector3 position)
        {
            SpawnBurst(position, BlockColorPalette.ChestGold, 35, 0.6f, 5.0f, 0.3f);
        }

        public void SpawnGemCollectParticles(Vector3 position)
        {
            SpawnBurst(position, new Color(0.2f, 0.95f, 1f), 15, 0.4f, 3.5f, 0.2f);
        }

        public void SpawnMergeParticles(Vector3 position, Color color)
        {
            SpawnBurst(position, color, 25, 0.5f, 4.5f, 0.35f);
        }

        private void SpawnBurst(Vector3 pos, Color color, int count, float duration, float speed, float size)
        {
            var go = new GameObject("Burst_VFX");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _particleMaterial;

            var main = ps.main;
            main.duration = duration;
            main.startLifetime = duration;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            ps.Play();
        }
    }
}

using System.Collections.Generic;
using ThisIsBlast.Data;
using ThisIsBlast.EnemyGrid;
using UnityEngine;

namespace ThisIsBlast.Combat
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [SerializeField] private int _initialPoolSize = 25;
        private readonly Queue<Projectile> _pool = new Queue<Projectile>();
        public int ActiveProjectilesCount { get; private set; } = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GrowPool(_initialPoolSize);
        }

        private void GrowPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Projectile_Pooled");
                go.transform.SetParent(transform);
                var p = go.AddComponent<Projectile>();
                go.SetActive(false);
                _pool.Enqueue(p);
            }
        }

        public Projectile SpawnProjectile(Vector3 startPos, EnemyBlock target, BlockColor color, int damage = 1)
        {
            if (_pool.Count == 0)
            {
                GrowPool(10);
            }

            ActiveProjectilesCount++;
            var proj = _pool.Dequeue();
            proj.Launch(startPos, target, color, damage);
            return proj;
        }

        public void Return(Projectile projectile)
        {
            ActiveProjectilesCount = Mathf.Max(0, ActiveProjectilesCount - 1);
            projectile.transform.SetParent(transform);
            _pool.Enqueue(projectile);
        }
    }
}

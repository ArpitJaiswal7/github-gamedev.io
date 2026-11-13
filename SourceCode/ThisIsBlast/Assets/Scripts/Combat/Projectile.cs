using System.Collections;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using ThisIsBlast.EnemyGrid;
using UnityEngine;

namespace ThisIsBlast.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 28f;
        private EnemyBlock _target;
        private int _targetColumn = -1;
        private int _damage = 1;
        private BlockColor _color;
        private bool _isActive = false;

        private MeshRenderer _renderer;
        private Material _material;
        private TrailRenderer _trail;

        private void Awake()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<MeshRenderer>();

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mf.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(sphere);

            _material = MaterialHelper.CreateStandardMaterial(Color.white, 0.8f);
            _renderer.material = _material;
            transform.localScale = Vector3.one * 0.32f;

            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.09f;
            _trail.startWidth = 0.24f;
            _trail.endWidth = 0.02f;
            _trail.material = MaterialHelper.CreateTrailMaterial();
        }

        public void Launch(Vector3 startPos, EnemyBlock target, BlockColor color, int damage = 1)
        {
            transform.position = startPos;
            _target = target;
            _targetColumn = target != null ? target.Data.x : -1;
            _color = color;
            _damage = damage;
            _isActive = true;

            if (_target != null)
            {
                _target.RegisterIncomingDamage(_damage);
            }

            Color c = BlockColorPalette.GetColor(color);
            _material.color = Color.Lerp(c, Color.white, 0.45f);

            if (_trail != null)
            {
                _trail.Clear();
                _trail.startColor = new Color(c.r, c.g, c.b, 0.8f);
                _trail.endColor = new Color(c.r, c.g, c.b, 0f);
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            // If original target was destroyed while projectile was in flight, check if next block
            // in this column that shifted to the front matches our color
            if (_target == null || _target.IsDestroyed)
            {
                if (_targetColumn >= 0)
                {
                    var newFront = EnemyGridManager.Instance?.GetFrontBlockOfColumn(_targetColumn);
                    if (newFront != null && !newFront.IsDestroyed && newFront.Data.color == _color && newFront.CanTakeDamage())
                    {
                        _target = newFront;
                        _target.RegisterIncomingDamage(_damage);
                    }
                }
            }

            Vector3 targetPos = _target != null && !_target.IsDestroyed 
                ? _target.transform.position 
                : transform.position + Vector3.up * 5f;

            // Move straight upward or towards target
            Vector3 dir = (targetPos - transform.position).normalized;
            float step = _speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetPos) <= step)
            {
                // Hit target!
                OnHitTarget();
            }
            else
            {
                transform.position += dir * step;

                // Offscreen cleanup
                if (transform.position.y > 10f)
                {
                    ReturnToPool();
                }
            }
        }

        private void OnHitTarget()
        {
            _isActive = false;

            if (_target != null)
            {
                _target.ResolveIncomingDamage(_damage);
                if (!_target.IsDestroyed)
                {
                    _target.TakeHit(_damage, _color);
                }
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (_isActive && _target != null)
            {
                _target.ResolveIncomingDamage(_damage);
            }
            _target = null;
            _targetColumn = -1;
            _isActive = false;
            _trail?.Clear();
            gameObject.SetActive(false);
            ProjectilePool.Instance?.Return(this);

            if (ProjectilePool.Instance != null && ProjectilePool.Instance.ActiveProjectilesCount == 0)
            {
                EnemyGridManager.Instance?.CheckRemainingBlocksAfterFlight();
            }
        }
    }
}

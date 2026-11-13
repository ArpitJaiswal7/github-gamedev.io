using System.Collections;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using UnityEngine;

namespace ThisIsBlast.EnemyGrid
{
    public class EnemyBlock : MonoBehaviour
    {
        public EnemyBlockData Data { get; private set; }
        public Vector3Int GridPos => new Vector3Int(Data.x, Data.y, Data.z);
        public int CurrentHp { get; protected set; }
        public bool IsDestroyed { get; protected set; }
        public int IncomingDamage { get; private set; }
        public int EffectiveHp => Mathf.Max(0, CurrentHp - IncomingDamage);

        public void RegisterIncomingDamage(int amount)
        {
            IncomingDamage += amount;
        }

        public void ResolveIncomingDamage(int amount)
        {
            IncomingDamage = Mathf.Max(0, IncomingDamage - amount);
        }

        protected MeshRenderer _meshRenderer;
        protected Material _blockMaterial;
        protected TextMesh _labelMesh;
        protected TextMesh _shadowMesh;
        protected GameObject _coverObject;
        protected GameObject _iconObject;

        protected Vector3 _baseScale;
        protected Coroutine _punchCoroutine;
        protected Coroutine _shiftCoroutine;

        public virtual void Initialize(EnemyBlockData data)
        {
            Data = data;
            CurrentHp = data.hp;
            IsDestroyed = false;
            IncomingDamage = 0;

            _baseScale = transform.localScale;

            SetupVisuals();
            UpdateVisuals();
        }

        protected virtual void SetupVisuals()
        {
            // Create mesh filter and renderer if not already present
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // Create primitive cube mesh
            var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshFilter.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempCube);

            // Create standard material with vibrant specular feel
            _blockMaterial = MaterialHelper.CreateStandardMaterial(Color.white, 0.7f, 0.1f);
            _meshRenderer.material = _blockMaterial;

            // Box Collider
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one;

            // Create 3D Text Label Drop Shadow for high contrast
            var shadowGo = new GameObject("HP_Label_Shadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0.012f, -0.012f, -0.515f);

            _shadowMesh = shadowGo.AddComponent<TextMesh>();
            _shadowMesh.fontSize = 42;
            _shadowMesh.characterSize = 0.08f;
            _shadowMesh.alignment = TextAlignment.Center;
            _shadowMesh.anchor = TextAnchor.MiddleCenter;
            _shadowMesh.color = new Color(0, 0, 0, 0.7f);
            _shadowMesh.fontStyle = FontStyle.Bold;

            // Create 3D Text Label for HP / Info
            var labelGo = new GameObject("HP_Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0, 0, -0.52f); // Front face

            _labelMesh = labelGo.AddComponent<TextMesh>();
            _labelMesh.fontSize = 42;
            _labelMesh.characterSize = 0.08f;
            _labelMesh.alignment = TextAlignment.Center;
            _labelMesh.anchor = TextAnchor.MiddleCenter;
            _labelMesh.color = Color.white;
            _labelMesh.fontStyle = FontStyle.Bold;
        }

        public virtual void UpdateVisuals()
        {
            if (_blockMaterial == null) return;

            switch (Data.type)
            {
                case EnemyBlockType.Standard:
                    _blockMaterial.color = BlockColorPalette.GetColor(Data.color);
                    _labelMesh.text = CurrentHp > 1 ? CurrentHp.ToString() : "";
                    break;

                case EnemyBlockType.Hidden:
                    _blockMaterial.color = BlockColorPalette.NeutralCover;
                    _labelMesh.text = "?";
                    break;

                case EnemyBlockType.Interlocked:
                    _blockMaterial.color = BlockColorPalette.GetColor(Data.color);
                    _labelMesh.text = "⛓ " + CurrentHp;
                    break;

                case EnemyBlockType.Locked:
                    _blockMaterial.color = BlockColorPalette.LockColor;
                    _labelMesh.text = "🔒";
                    break;

                case EnemyBlockType.Key:
                    _blockMaterial.color = BlockColorPalette.GetColor(Data.color);
                    _labelMesh.text = "🔑 " + (CurrentHp > 1 ? CurrentHp.ToString() : "");
                    break;

                case EnemyBlockType.Chest:
                    _blockMaterial.color = BlockColorPalette.ChestGold;
                    int hitsNeeded = Data.hitThreshold - (Data.hp - CurrentHp);
                    _labelMesh.text = "📦 " + Mathf.Max(0, CurrentHp);
                    break;

                case EnemyBlockType.PiggyBank:
                    _blockMaterial.color = BlockColorPalette.PiggyPink;
                    _labelMesh.text = "🐷 " + CurrentHp;
                    break;
            }

            if (_shadowMesh != null)
            {
                _shadowMesh.text = _labelMesh.text;
            }
        }

        public virtual bool CanTakeDamage()
        {
            if (IsDestroyed) return false;
            if (Data.type == EnemyBlockType.Locked) return false;
            return true;
        }

        public virtual void TakeHit(int damage = 1, BlockColor projectileColor = BlockColor.None)
        {
            if (!CanTakeDamage()) return;

            // Handle Hidden block reveal on hit
            if (Data.type == EnemyBlockType.Hidden)
            {
                Data.type = EnemyBlockType.Standard;
                FloatingText.Create(transform.position, "REVEALED!", BlockColorPalette.GetColor(Data.color));
                UpdateVisuals();
                PunchScale();
                SoundManager.Instance?.PlayHit();
                ParticleEffectsManager.Instance?.SpawnBlockHitParticles(transform.position, BlockColorPalette.GetColor(Data.color));
                return;
            }

            // Handle Chest Block
            if (Data.type == EnemyBlockType.Chest)
            {
                CurrentHp -= damage;
                FloatingText.Create(transform.position, "-1", Color.yellow, 0.3f);
                SoundManager.Instance?.PlayHit();
                ParticleEffectsManager.Instance?.SpawnBlockHitParticles(transform.position, BlockColorPalette.ChestGold);
                PunchScale();

                if (CurrentHp <= 0)
                {
                    OpenChest();
                }
                else
                {
                    UpdateVisuals();
                }
                return;
            }

            // Handle Piggy Bank
            if (Data.type == EnemyBlockType.PiggyBank)
            {
                CurrentHp -= damage;
                int gems = RemoteConfigService.Instance ? RemoteConfigService.Instance.GetInt("piggy_gem_per_hit", 1) : 1;
                GameEvents.TriggerPiggyBankHit(Data.x, Data.y);
                FloatingText.Create(transform.position, $"+{gems} 💎", Color.green, 0.4f);
                SoundManager.Instance?.PlayGem();
                ParticleEffectsManager.Instance?.SpawnGemCollectParticles(transform.position);
                PunchScale();

                if (CurrentHp <= 0)
                {
                    DestroyPiggyBank();
                }
                else
                {
                    UpdateVisuals();
                }
                return;
            }

            // Standard / Interlocked / Key / etc.
            CurrentHp -= damage;
            Color hitFxColor = BlockColorPalette.GetColor(Data.color);
            EnemyGridManager.Instance?.HighlightCell(Data.x, Data.y, hitFxColor);
            EnemyGridManager.Instance?.HighlightRow(Data.y, hitFxColor, 0.18f);
            EnemyGridManager.Instance?.HighlightColumn(Data.x, hitFxColor, 0.18f);

            GameEvents.TriggerBlockDamaged(Data.x, Data.y);
            SoundManager.Instance?.PlayHit();
            ParticleEffectsManager.Instance?.SpawnBlockHitParticles(transform.position, hitFxColor);
            PunchScale();

            if (CurrentHp <= 0)
            {
                bool isColorMatch = (projectileColor == Data.color && Data.color != BlockColor.None);

                if (Data.type == EnemyBlockType.Interlocked)
                {
                    // Check if partner is also dead
                    EnemyGridManager.Instance?.CheckInterlockedDestruction(this);
                }
                else if (Data.type == EnemyBlockType.Key)
                {
                    DestroyWithKeyDrop();
                }
                else
                {
                    DestroyBlock();
                }
            }
            else
            {
                UpdateVisuals();
            }
        }

        public virtual void DestroyBlock()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            IncomingDamage = 0;

            if (_shiftCoroutine != null)
            {
                StopCoroutine(_shiftCoroutine);
                _shiftCoroutine = null;
            }

            // Unparent dying block so it doesn't occupy or interfere with the cell slot
            transform.SetParent(null);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            SoundManager.Instance?.PlayDestroy();
            CameraShaker.Instance?.Shake(0.1f, 0.12f);
            ParticleEffectsManager.Instance?.SpawnBlockDestroyParticles(transform.position, BlockColorPalette.GetColor(Data.color));

            GameEvents.TriggerBlockDestroyed(Data.x, Data.y, Data.color, Data.type);
            EnemyGridManager.Instance?.UnregisterBlock(this);
            StartCoroutine(AnimateDeath());
        }

        public void DestroyInterlockedFinal()
        {
            DestroyBlock();
        }

        protected void DestroyWithKeyDrop()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            IncomingDamage = 0;

            if (_shiftCoroutine != null)
            {
                StopCoroutine(_shiftCoroutine);
                _shiftCoroutine = null;
            }

            transform.SetParent(null);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            SoundManager.Instance?.PlayDestroy();
            CameraShaker.Instance?.Shake(0.12f, 0.15f);
            ParticleEffectsManager.Instance?.SpawnBlockDestroyParticles(transform.position, BlockColorPalette.GetColor(Data.color));

            // Trigger Key drop projectile towards locked arsenal column
            EnemyGridManager.Instance?.SpawnKeyDropProjectile(transform.position, Data.unlockColIndex);

            GameEvents.TriggerBlockDestroyed(Data.x, Data.y, Data.color, Data.type);
            EnemyGridManager.Instance?.UnregisterBlock(this);
            StartCoroutine(AnimateDeath());
        }

        protected void OpenChest()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            IncomingDamage = 0;

            if (_shiftCoroutine != null)
            {
                StopCoroutine(_shiftCoroutine);
                _shiftCoroutine = null;
            }

            transform.SetParent(null);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            SoundManager.Instance?.PlayWin();
            CameraShaker.Instance?.Shake(0.2f, 0.25f);
            ParticleEffectsManager.Instance?.SpawnChestOpenParticles(transform.position);

            int reward = Data.gemReward > 0 ? Data.gemReward : 10;
            FloatingText.Create(transform.position, $"+{reward} GEMS! 💎", BlockColorPalette.ChestGold, 0.5f);
            GameEvents.TriggerChestOpened(Data.x, Data.y, reward);
            GameEvents.TriggerBlockDestroyed(Data.x, Data.y, Data.color, Data.type);
            EnemyGridManager.Instance?.UnregisterBlock(this);
            StartCoroutine(AnimateDeath());
        }

        protected void DestroyPiggyBank()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            IncomingDamage = 0;

            if (_shiftCoroutine != null)
            {
                StopCoroutine(_shiftCoroutine);
                _shiftCoroutine = null;
            }

            transform.SetParent(null);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            SoundManager.Instance?.PlayWin();
            CameraShaker.Instance?.Shake(0.15f, 0.2f);
            ParticleEffectsManager.Instance?.SpawnGemCollectParticles(transform.position);

            int bonusGems = 5;
            FloatingText.Create(transform.position, $"PIGGY BURST! +{bonusGems} 💎", Color.green, 0.45f);
            GameEvents.TriggerChestOpened(Data.x, Data.y, bonusGems);
            GameEvents.TriggerBlockDestroyed(Data.x, Data.y, Data.color, Data.type);
            EnemyGridManager.Instance?.UnregisterBlock(this);
            StartCoroutine(AnimateDeath());
        }

        /// <summary>
        /// Animates block smoothly sliding down to its new cell slot position when lower row blocks are popped.
        /// </summary>
        public void AnimateShiftDown(Vector3 targetWorldPos)
        {
            if (_shiftCoroutine != null) StopCoroutine(_shiftCoroutine);
            _shiftCoroutine = StartCoroutine(DoAnimateShiftDown(targetWorldPos));
        }

        private IEnumerator DoAnimateShiftDown(Vector3 targetWorldPos)
        {
            float duration = 0.12f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                if (this == null || gameObject == null || IsDestroyed) yield break;

                float t = elapsed / duration;
                // Snappy cubic ease-out
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(startPos, targetWorldPos, ease);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (this != null && gameObject != null && !IsDestroyed)
            {
                transform.position = targetWorldPos;
            }
            _shiftCoroutine = null;
        }

        protected void PunchScale()
        {
            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(DoPunchScale());
        }

        protected IEnumerator DoPunchScale()
        {
            float duration = 0.12f;
            float elapsed = 0f;
            Vector3 peakScale = _baseScale * 1.25f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(peakScale, _baseScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = _baseScale;
            _punchCoroutine = null;
        }

        protected IEnumerator AnimateDeath()
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

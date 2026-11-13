using System.Collections;
using ThisIsBlast.Core;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using ThisIsBlast.EnemyGrid;
using UnityEngine;

namespace ThisIsBlast.Arsenal
{
    public class ArsenalBlock : MonoBehaviour
    {
        public ArsenalBlockData Data { get; private set; }
        public BlockColor BlockColor => Data != null ? Data.color : BlockColor.None;
        public int BulletCount => Data != null ? Data.bulletCount : 0;
        public bool IsActivePeek { get; private set; }
        public BenchCellSlot DockedSlot { get; private set; }
        public bool IsOnBench => DockedSlot != null;
        public bool IsMoving { get; set; }
        public bool IsInterlocked => Data != null && Data.isInterlocked;

        private MeshRenderer _meshRenderer;
        private Material _material;
        private TextMesh _bulletLabel;
        private TextMesh _bulletLabelShadow;
        private Vector3 _baseScale;
        private Coroutine _animCoroutine;

        public void SetDockedSlot(BenchCellSlot slot)
        {
            DockedSlot = slot;
            IsActivePeek = true;
            UpdateVisuals();
        }

        public void ClearDockedSlot()
        {
            DockedSlot = null;
        }

        public void Initialize(ArsenalBlockData data, bool isPeek)
        {
            Data = data;
            if (Data != null && Data.bulletCount > 0)
            {
                Data.bulletCount = LevelDecoder.SanitizeBulletCount(Data.bulletCount);
            }
            IsActivePeek = isPeek;
            _baseScale = transform.localScale;

            SetupVisuals();
            UpdateVisuals();
        }

        private void SetupVisuals()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempCube);

            _material = MaterialHelper.CreateStandardMaterial(Color.white, 0.72f, 0.12f);
            _meshRenderer.material = _material;

            var col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            // Drop shadow for number
            var shadowGo = new GameObject("BulletLabel_Shadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0.015f, -0.015f, -0.515f);

            _bulletLabelShadow = shadowGo.AddComponent<TextMesh>();
            _bulletLabelShadow.fontSize = 50;
            _bulletLabelShadow.characterSize = 0.085f;
            _bulletLabelShadow.alignment = TextAlignment.Center;
            _bulletLabelShadow.anchor = TextAnchor.MiddleCenter;
            _bulletLabelShadow.color = new UnityEngine.Color(0, 0, 0, 0.75f);
            _bulletLabelShadow.fontStyle = FontStyle.Bold;

            var labelGo = new GameObject("BulletLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0, 0, -0.52f);

            _bulletLabel = labelGo.AddComponent<TextMesh>();
            _bulletLabel.fontSize = 50;
            _bulletLabel.characterSize = 0.085f;
            _bulletLabel.alignment = TextAlignment.Center;
            _bulletLabel.anchor = TextAnchor.MiddleCenter;
            _bulletLabel.color = UnityEngine.Color.white;
            _bulletLabel.fontStyle = FontStyle.Bold;
        }

        public void UpdateVisuals()
        {
            if (Data == null || _material == null) return;

            if (Data.isHidden && !IsActivePeek)
            {
                // Hidden queue block: dark mystery cover with "?" like competitor screenshot
                _material.color = new UnityEngine.Color(0.20f, 0.25f, 0.35f);
                _bulletLabel.text = "?";
                _bulletLabel.color = new UnityEngine.Color(0.75f, 0.85f, 0.95f, 0.7f);
                if (_bulletLabelShadow != null)
                {
                    _bulletLabelShadow.text = "?";
                    _bulletLabelShadow.color = new UnityEngine.Color(0, 0, 0, 0.5f);
                }
            }
            else
            {
                UnityEngine.Color baseColor = BlockColorPalette.GetColor(Data.color);
                // If deeper in stack, slightly dim or soften to emphasize active top block
                _material.color = IsActivePeek ? baseColor : UnityEngine.Color.Lerp(baseColor, UnityEngine.Color.gray, 0.35f);

                string countText = Data.bulletCount.ToString();
                if (Data.isInterlocked)
                {
                    countText = "⛓ " + countText;
                }
                _bulletLabel.text = countText;
                _bulletLabel.color = IsActivePeek ? UnityEngine.Color.white : new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.7f);

                if (_bulletLabelShadow != null)
                {
                    _bulletLabelShadow.text = _bulletLabel.text;
                    _bulletLabelShadow.color = IsActivePeek ? new UnityEngine.Color(0, 0, 0, 0.75f) : new UnityEngine.Color(0, 0, 0, 0.4f);
                }
            }

            transform.localScale = IsActivePeek ? _baseScale : _baseScale * 0.92f;
        }

        public void SetActivePeek(bool isPeek)
        {
            bool wasHidden = Data != null && Data.isHidden;
            IsActivePeek = isPeek;

            if (isPeek && wasHidden)
            {
                Data.isHidden = false;
                FloatingText.Create(transform.position, "REVEALED!", BlockColorPalette.GetColor(Data.color), 0.35f);
                ParticleEffectsManager.Instance?.SpawnBlockHitParticles(transform.position, BlockColorPalette.GetColor(Data.color));
                SoundManager.Instance?.PlayMerge();
            }

            UpdateVisuals();
        }

        public void DecrementBullet()
        {
            if (Data != null)
            {
                Data.bulletCount--;
                UpdateVisuals();
                PunchRecoil();
            }
        }

        public void AddBullets(int added)
        {
            if (Data != null)
            {
                Data.bulletCount += added;
                UpdateVisuals();
                PunchMergeGrow();
            }
        }

        private void PunchRecoil()
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(DoPunchRecoil());
        }

        private IEnumerator DoPunchRecoil()
        {
            Vector3 startPos = transform.localPosition;
            Vector3 recoilPos = startPos + new Vector3(0, -0.15f, 0);

            float halfDuration = 0.04f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                transform.localPosition = Vector3.Lerp(startPos, recoilPos, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                transform.localPosition = Vector3.Lerp(recoilPos, startPos, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = startPos;
            _animCoroutine = null;
        }

        private void PunchMergeGrow()
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(DoPunchMergeGrow());
        }

        private IEnumerator DoPunchMergeGrow()
        {
            Vector3 targetScale = _baseScale * 1.35f;
            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, _baseScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = _baseScale;
            _animCoroutine = null;
        }

        public void AnimateMergeInto(Vector3 targetWorldPos, System.Action onComplete)
        {
            StartCoroutine(DoAnimateMergeInto(targetWorldPos, onComplete));
        }

        private IEnumerator DoAnimateMergeInto(Vector3 targetWorldPos, System.Action onComplete)
        {
            IsMoving = true;
            float duration = 0.22f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetWorldPos, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        public bool TryShoot()
        {
            if (!IsOnBench || BulletCount <= 0 || IsMoving) return false;

            // Same colored shooting block from arsenal in bench can ONLY shoot same colored valid enemy block
            EnemyBlock targetEnemy = EnemyGridManager.Instance?.GetLowestBlockForColor(BlockColor, transform.position.x);
            if (targetEnemy == null)
            {
                // No exposed valid enemy block of the same color is reachable right now
                return false;
            }

            Vector3 spawnPos = transform.position + new Vector3(0, 0.55f, 0);
            Combat.ProjectilePool.Instance?.SpawnProjectile(spawnPos, targetEnemy, BlockColor, 1);

            DecrementBullet();
            SoundManager.Instance?.PlayShoot();
            DockedSlot?.Highlight(BlockColorPalette.GetColor(BlockColor), 0.15f);

            if (BulletCount <= 0)
            {
                OnDepleted();
            }

            return true;
        }

        public void OnDepleted()
        {
            ParticleEffectsManager.Instance?.SpawnBlockDestroyParticles(transform.position, BlockColorPalette.GetColor(BlockColor));
            if (DockedSlot != null)
            {
                DockedSlot.ClearBlock();
                DockedSlot = null;
            }
            Destroy(gameObject);
            ArsenalManager.Instance?.CheckFailCondition();
        }

        public void AnimateMoveToBenchSlot(BenchCellSlot slot, System.Action onComplete)
        {
            IsMoving = true;
            StartCoroutine(DoMoveToBenchSlot(slot, onComplete));
        }

        private IEnumerator DoMoveToBenchSlot(BenchCellSlot slot, System.Action onComplete)
        {
            float duration = 0.28f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = slot.transform.position;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 current = Vector3.Lerp(startPos, targetPos, smoothT);
                float arc = Mathf.Sin(t * Mathf.PI) * 0.45f;
                current.y += arc;

                transform.position = current;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
            slot.AttachBlock(this);
            SetActivePeek(true);
            IsMoving = false;
            PunchRecoil();
            SoundManager.Instance?.PlayHit();
            onComplete?.Invoke();
        }

        public void PunchBlockedWiggle()
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(DoPunchWiggle());
        }

        private IEnumerator DoPunchWiggle()
        {
            Vector3 startPos = transform.localPosition;
            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float offset = Mathf.Sin(elapsed * Mathf.PI * 8f) * 0.08f;
                transform.localPosition = startPos + new Vector3(offset, 0, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = startPos;
            _animCoroutine = null;
        }

        public void AnimatePopUp(Vector3 targetLocalPos)
        {
            StartCoroutine(DoAnimatePopUp(targetLocalPos));
        }

        private IEnumerator DoAnimatePopUp(Vector3 targetLocalPos)
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startLocal = transform.localPosition;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localPosition = Vector3.Lerp(startLocal, targetLocalPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = targetLocalPos;
        }
    }
}

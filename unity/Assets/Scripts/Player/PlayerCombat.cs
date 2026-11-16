using System.Collections;
using UnityEngine;

namespace ChibiARPG
{
    public class PlayerCombat : MonoBehaviour
    {
        public PlayerClassDefinition classDef;
        public CharacterStats stats;
        public ChibiAnim anim;
        public MeleeHitbox hitbox;
        public Transform muzzle;

        public bool IsBusy { get { return _attacking || _spinning; } }
        public float SkillCdLeft { get { return Mathf.Max(0f, _skillReadyAt - Time.time); } }
        public float SkillCdTotal { get { return classDef != null ? classDef.skillCooldown : 1f; } }
        public float AttackCdLeft { get { return Mathf.Max(0f, _atkReadyAt - Time.time); } }

        bool _attacking;
        bool _spinning;
        int _combo;
        float _comboExpire;
        float _atkReadyAt;
        float _skillReadyAt;
        Coroutine _atkCo;
        Coroutine _skCo;

        public void Setup(PlayerClassDefinition def, CharacterStats s, ChibiAnim a)
        {
            classDef = def;
            stats = s;
            anim = a;
            if (hitbox == null)
            {
                var box = new GameObject("近战判定");
                box.transform.SetParent(transform, false);
                box.transform.localPosition = new Vector3(0f, 0.5f, 0.85f);
                var sc = box.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.85f;
                hitbox = box.AddComponent<MeleeHitbox>();
            }

            hitbox.owner = stats;
            if (muzzle == null)
            {
                var m = new GameObject("Muzzle");
                m.transform.SetParent(transform, false);
                m.transform.localPosition = new Vector3(0.2f, 0.7f, 0.6f);
                muzzle = m.transform;
            }
        }

        public bool TryAttack()
        {
            if (stats == null || stats.IsDead || _spinning)
                return false;
            if (classDef == null)
                return false;
            if (_attacking)
            {
                if (classDef.attackStyle == AttackStyle.MeleeCombo && _combo < classDef.comboHits && Time.time < _comboExpire)
                    return true;
                return false;
            }

            if (Time.time < _atkReadyAt)
                return false;
            if (_atkCo != null)
                StopCoroutine(_atkCo);
            _atkCo = StartCoroutine(AttackRoutine());
            return true;
        }

        public bool TrySkill()
        {
            if (stats == null || stats.IsDead || classDef == null || _spinning)
                return false;
            if (Time.time < _skillReadyAt)
                return false;
            if (!stats.TrySpendMp(classDef.skillMpCost))
                return false;
            if (_skCo != null)
                StopCoroutine(_skCo);
            _skCo = StartCoroutine(SkillRoutine());
            return true;
        }

        IEnumerator AttackRoutine()
        {
            _attacking = true;
            if (classDef.attackStyle == AttackStyle.MeleeCombo)
                yield return MeleeCombo();
            else if (classDef.attackStyle == AttackStyle.MagicBolt)
                FireProjectile(false, 0f, classDef.attackDamage, classDef.projectileSpeed, classDef.projectileRange, ChibiFactory.Hex("B48CFF"), 0.16f, true, 0f);
            else
                FireProjectile(false, 0f, classDef.attackDamage, classDef.projectileSpeed, classDef.projectileRange, ChibiFactory.Hex("E8D9A0"), 0.08f, false, 0f);

            if (classDef.attackStyle != AttackStyle.MeleeCombo)
            {
                if (anim != null)
                {
                    anim.attacking = true;
                    anim.attackT = 0f;
                }

                float t = 0f;
                float dur = Mathf.Max(0.18f, classDef.attackCooldown * 0.7f);
                while (t < dur)
                {
                    t += Time.deltaTime;
                    if (anim != null)
                        anim.attackT = t / dur;
                    yield return null;
                }

                if (anim != null)
                    anim.attacking = false;
                _atkReadyAt = Time.time + classDef.attackCooldown;
            }

            _attacking = false;
            _atkCo = null;
        }

        IEnumerator MeleeCombo()
        {
            _combo = 0;
            bool queued = true;
            while (queued && _combo < classDef.comboHits && stats != null && !stats.IsDead)
            {
                queued = false;
                _combo++;
                float dmg = classDef.attackDamage * (0.85f + 0.25f * _combo);
                if (_combo == classDef.comboHits)
                    dmg *= 1.25f;
                float lunge = _combo == 3 ? 0.28f : 0.16f;
                float active = _combo == 3 ? 0.18f : 0.12f;
                float recover = _combo == 3 ? 0.22f : 0.14f;

                if (anim != null)
                {
                    anim.attacking = true;
                    anim.attackT = 0f;
                }

                // 小步前冲，第三段更明显
                float lunged = 0f;
                while (lunged < 0.08f)
                {
                    lunged += Time.deltaTime;
                    if (anim != null)
                        anim.attackT = 0.15f;
                    transform.position += transform.forward * (lunge * Time.deltaTime / 0.08f);
                    yield return null;
                }

                if (hitbox != null)
                    hitbox.Begin(dmg);
                float a = 0f;
                while (a < active)
                {
                    a += Time.deltaTime;
                    if (anim != null)
                        anim.attackT = Mathf.Clamp01(0.2f + a / active);
                    yield return null;
                }

                if (hitbox != null)
                    hitbox.End();

                _comboExpire = Time.time + 0.42f;
                float r = 0f;
                while (r < recover)
                {
                    r += Time.deltaTime;
                    if (anim != null)
                        anim.attackT = 1f - r / recover;
                    if (InputWantsAttack() && _combo < classDef.comboHits)
                        queued = true;
                    yield return null;
                }

                if (anim != null)
                    anim.attacking = false;
            }

            _atkReadyAt = Time.time + classDef.attackCooldown;
            _combo = 0;
        }

        IEnumerator SkillRoutine()
        {
            _skillReadyAt = Time.time + classDef.skillCooldown;
            if (classDef.skillStyle == SkillStyle.SpinSlash)
                yield return SpinSlash();
            else if (classDef.skillStyle == SkillStyle.AoeBurst)
                yield return AoeBurst();
            else
                yield return MultiShot();
            _skCo = null;
        }

        IEnumerator SpinSlash()
        {
            _spinning = true;
            if (anim != null)
                anim.spinning = true;
            CameraImpulse.Shake(0.35f);
            if (hitbox != null)
            {
                hitbox.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                var sc = hitbox.GetComponent<SphereCollider>();
                if (sc != null)
                    sc.radius = 1.6f;
                hitbox.Begin(classDef.skillDamage);
            }

            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }

            if (hitbox != null)
            {
                hitbox.End();
                hitbox.transform.localPosition = new Vector3(0f, 0.5f, 0.85f);
                var sc = hitbox.GetComponent<SphereCollider>();
                if (sc != null)
                    sc.radius = 0.85f;
            }

            if (anim != null)
                anim.spinning = false;
            _spinning = false;
        }

        IEnumerator AoeBurst()
        {
            if (anim != null)
            {
                anim.attacking = true;
                anim.attackT = 0f;
            }

            Vector3 center = transform.position + transform.forward * 2.2f + Vector3.up * 0.3f;
            SpawnBurstFx(center, ChibiFactory.Hex("B48CFF"), 2.2f);
            CameraImpulse.Shake(0.28f);
            yield return new WaitForSeconds(0.12f);
            var hits = Physics.OverlapSphere(center, 2.4f);
            for (int i = 0; i < hits.Length; i++)
            {
                var s = hits[i].GetComponentInParent<CharacterStats>();
                if (s == null || s == stats || s.IsDead || s.isPlayer)
                    continue;
                s.TakeDamage(classDef.skillDamage, hits[i].ClosestPoint(center), stats);
                var ai = s.GetComponent<EnemyAI>();
                if (ai != null)
                    ai.ApplyKnockback((s.transform.position - center).normalized * 3f);
            }

            yield return new WaitForSeconds(0.25f);
            if (anim != null)
                anim.attacking = false;
        }

        IEnumerator MultiShot()
        {
            if (anim != null)
                anim.attacking = true;
            float[] angles = { -24f, -12f, 0f, 12f, 24f };
            for (int i = 0; i < angles.Length; i++)
            {
                FireProjectile(true, angles[i], classDef.skillDamage, classDef.projectileSpeed + 2f, classDef.projectileRange, ChibiFactory.Hex("F0E2A0"), 0.08f, false, 0f);
                yield return new WaitForSeconds(0.04f);
            }

            yield return new WaitForSeconds(0.18f);
            if (anim != null)
                anim.attacking = false;
        }

        void FireProjectile(bool skill, float yaw, float dmg, float spd, float range, Color color, float radius, bool magic, float aoe)
        {
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * transform.forward;
            Vector3 pos = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.7f + transform.forward * 0.4f;
            if (anim != null)
            {
                anim.attacking = true;
                anim.attackT = 0.5f;
            }

            Projectile.Spawn(pos, dir, stats, dmg, spd, range, color, radius, magic, aoe);
        }

        void SpawnBurstFx(Vector3 pos, Color color, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "魔爆";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            Object.Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = ToonMaterialUtil.Get(new Color(color.r, color.g, color.b, 0.65f), 0.01f);
                r.sharedMaterial = mat;
            }

            Object.Destroy(go, 0.35f);
        }

        bool InputWantsAttack()
        {
            if (GameHud.Instance != null && GameHud.Instance.AttackHeld)
                return true;
            return Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.Space);
        }
    }
}

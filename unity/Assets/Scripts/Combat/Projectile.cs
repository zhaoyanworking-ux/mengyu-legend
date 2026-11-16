using UnityEngine;

namespace ChibiARPG
{
    public class Projectile : MonoBehaviour
    {
        public CharacterStats owner;
        public float damage = 10f;
        public float speed = 12f;
        public float life = 1.2f;
        public Vector3 dir = Vector3.forward;
        public bool isMagic;
        public float aoeRadius;
        public string fxName;

        float _t;
        bool _dead;

        public static Projectile Spawn(Vector3 pos, Vector3 direction, CharacterStats owner, float damage, float speed, float range, Color color, float radius, bool magic, float aoe)
        {
            var go = GameObject.CreatePrimitive(magic ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            go.name = magic ? "魔弹" : "箭矢";
            go.transform.position = pos;
            go.transform.localScale = magic ? Vector3.one * (radius * 2f) : new Vector3(0.08f, 0.28f, 0.08f);
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = ToonMaterialUtil.Get(color, 0.01f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var p = go.AddComponent<Projectile>();
            p.owner = owner;
            p.damage = damage;
            p.speed = speed;
            p.life = range / Mathf.Max(0.1f, speed);
            p.dir = direction.normalized;
            p.isMagic = magic;
            p.aoeRadius = aoe;
            if (!magic)
                go.transform.rotation = Quaternion.LookRotation(p.dir) * Quaternion.Euler(90f, 0f, 0f);
            return p;
        }

        void Update()
        {
            if (_dead)
                return;
            float dt = Time.deltaTime;
            _t += dt;
            transform.position += dir * speed * dt;
            if (_t >= life)
                Die();
        }

        void OnTriggerEnter(Collider other)
        {
            if (_dead || other == null)
                return;
            var stats = other.GetComponentInParent<CharacterStats>();
            if (stats == null || stats == owner || stats.IsDead)
                return;
            if (owner != null && stats.isPlayer == owner.isPlayer)
                return;
            Explode(stats, other);
        }

        void Explode(CharacterStats first, Collider col)
        {
            _dead = true;
            Vector3 p = col != null ? col.ClosestPoint(transform.position) : transform.position;
            if (aoeRadius > 0.1f)
            {
                var hits = Physics.OverlapSphere(transform.position, aoeRadius);
                for (int i = 0; i < hits.Length; i++)
                {
                    var s = hits[i].GetComponentInParent<CharacterStats>();
                    if (s == null || s == owner || s.IsDead)
                        continue;
                    if (owner != null && s.isPlayer == owner.isPlayer)
                        continue;
                    s.TakeDamage(damage, hits[i].ClosestPoint(transform.position), owner);
                }
            }
            else if (first != null)
            {
                first.TakeDamage(damage, p, owner);
                var ai = first.GetComponent<EnemyAI>();
                if (ai != null && owner != null)
                    ai.ApplyKnockback((first.transform.position - owner.transform.position).normalized * 1.4f);
            }

            Die();
        }

        void Die()
        {
            _dead = true;
            Destroy(gameObject);
        }
    }
}

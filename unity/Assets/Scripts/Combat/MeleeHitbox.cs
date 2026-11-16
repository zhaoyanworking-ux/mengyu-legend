using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 近战判定盒。开启期间碰到的敌人只结算一次。
    /// </summary>
    public class MeleeHitbox : MonoBehaviour
    {
        public CharacterStats owner;
        public float damage = 12f;
        public bool active;
        public float radius = 0.85f;

        readonly HashSet<int> _hitIds = new HashSet<int>();
        Collider _col;

        void Awake()
        {
            _col = GetComponent<Collider>();
            if (_col == null)
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.9f;
                _col = sc;
            }

            _col.isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            SetActive(false);
        }

        public void Begin(float dmg)
        {
            damage = dmg;
            _hitIds.Clear();
            SetActive(true);
        }

        public void End()
        {
            SetActive(false);
            _hitIds.Clear();
        }

        public void SetActive(bool on)
        {
            active = on;
            if (_col != null)
                _col.enabled = on;
        }

        void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        void OnTriggerStay(Collider other)
        {
            TryHit(other);
        }

        void TryHit(Collider other)
        {
            if (!active || other == null)
                return;

            var stats = other.GetComponentInParent<CharacterStats>();
            if (stats == null || stats == owner || stats.IsDead)
                return;
            if (owner != null && stats.isPlayer == owner.isPlayer)
                return;

            int id = stats.GetInstanceID();
            if (_hitIds.Contains(id))
                return;
            _hitIds.Add(id);

            Vector3 point = other.ClosestPoint(transform.position);
            stats.TakeDamage(damage, point, owner);

            var ai = stats.GetComponent<EnemyAI>();
            float knock = (owner != null && owner.isPlayer) ? 3.2f : 2.0f;
            if (ai != null && owner != null)
                ai.ApplyKnockback((stats.transform.position - owner.transform.position).normalized * knock);

            // 剑士连击：顿帧 + 镜头震动
            if (owner != null && owner.isPlayer)
            {
                bool heavy = damage > 24f;
                HitStop.Play(heavy ? 0.07f : 0.045f, heavy ? 0.08f : 0.15f);
                CameraImpulse.Shake(heavy ? 0.42f : 0.26f);
            }
        }
    }
}

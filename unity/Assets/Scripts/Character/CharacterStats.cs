using System;
using UnityEngine;

namespace ChibiARPG
{
    public class CharacterStats : MonoBehaviour
    {
        public float maxHp = 100f;
        public float maxMp = 60f;
        public float hp = 100f;
        public float mp = 60f;
        public bool isPlayer;
        public string displayName = "角色";

        public bool IsDead { get; private set; }

        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnMpChanged;
        public event Action<float, Vector3, CharacterStats> OnDamaged;
        public event Action<CharacterStats> OnDied;
        public event Action OnRevived;

        public float Hp01
        {
            get { return maxHp <= 0f ? 0f : Mathf.Clamp01(hp / maxHp); }
        }

        public float Mp01
        {
            get { return maxMp <= 0f ? 0f : Mathf.Clamp01(mp / maxMp); }
        }

        public void ResetStats()
        {
            IsDead = false;
            hp = maxHp;
            mp = maxMp;
            OnHpChanged -= null;
            RaiseHp();
            RaiseMp();
            OnRevived?.Invoke();
        }

        public void Init(float newMaxHp, float newMaxMp, bool player, string name)
        {
            maxHp = newMaxHp;
            maxMp = newMaxMp;
            hp = newMaxHp;
            mp = newMaxMp;
            isPlayer = player;
            displayName = name;
            IsDead = false;
            RaiseHp();
            RaiseMp();
        }

        public void TakeDamage(float amount, Vector3 hitPoint, CharacterStats attacker)
        {
            if (IsDead || amount <= 0f)
                return;

            hp = Mathf.Max(0f, hp - amount);
            RaiseHp();
            OnDamaged?.Invoke(amount, hitPoint, attacker);
            if (hp <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return;
            hp = Mathf.Min(maxHp, hp + amount);
            RaiseHp();
        }

        public bool TrySpendMp(float cost)
        {
            if (IsDead || cost > mp)
                return false;
            mp = Mathf.Max(0f, mp - cost);
            RaiseMp();
            return true;
        }

        public void RestoreMp(float amount)
        {
            if (IsDead || amount <= 0f)
                return;
            mp = Mathf.Min(maxMp, mp + amount);
            RaiseMp();
        }

        void Die()
        {
            if (IsDead)
                return;
            IsDead = true;
            hp = 0f;
            RaiseHp();
            OnDied?.Invoke(this);
        }

        void RaiseHp()
        {
            OnHpChanged?.Invoke(hp, maxHp);
        }

        void RaiseMp()
        {
            OnMpChanged?.Invoke(mp, maxMp);
        }
    }
}

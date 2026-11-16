using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    public class EnemySpawner : MonoBehaviour
    {
        public int maxAlive = 6;
        public float radius = 11f;
        public float respawnDelay = 4f;
        public bool isBossSpawner;

        readonly List<GameObject> _alive = new List<GameObject>();
        float _timer;
        bool _bossSpawned;

        public void TickCleanup()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] == null)
                    _alive.RemoveAt(i);
            }
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
                return;
            TickCleanup();
            if (isBossSpawner)
                return;
            if (_alive.Count >= maxAlive)
                return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                SpawnOne();
                _timer = respawnDelay;
            }
        }

        public GameObject SpawnOne()
        {
            Vector2 c = Random.insideUnitCircle * radius;
            Vector3 pos = transform.position + new Vector3(c.x, 0f, c.y);
            pos.y = 0f;
            var go = DemoWorldBuilder.SpawnEnemy(pos, EnemyKind.BlightBeast);
            if (go != null)
                _alive.Add(go);
            return go;
        }

        public GameObject SpawnBoss()
        {
            if (_bossSpawned && HasBoss())
                return null;
            TickCleanup();
            Vector3 pos = transform.position + Vector3.forward * 2f;
            var go = DemoWorldBuilder.SpawnEnemy(pos, EnemyKind.MiniBoss);
            if (go != null)
            {
                _alive.Add(go);
                _bossSpawned = true;
            }

            return go;
        }

        bool HasBoss()
        {
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i] == null)
                    continue;
                var ai = _alive[i].GetComponent<EnemyAI>();
                if (ai != null && ai.kind == EnemyKind.MiniBoss)
                    return true;
            }

            return false;
        }
    }
}

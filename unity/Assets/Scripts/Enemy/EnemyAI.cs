using UnityEngine;

namespace ChibiARPG
{
    public enum EnemyKind
    {
        BlightBeast,
        MiniBoss
    }

    public class EnemyAI : MonoBehaviour
    {
        public EnemyKind kind = EnemyKind.BlightBeast;
        public string enemyId = QuestCatalog.MonsterFodder;
        public CharacterStats stats;
        public ChibiAnim anim;
        public float moveSpeed = 3.2f;
        public float detectRange = 9f;
        public float attackRange = 1.35f;
        public float attackDamage = 8f;
        public float attackCooldown = 1.35f;
        public float wanderRadius = 4f;
        public bool dropShard;

        CharacterController _cc;
        Transform _player;
        Vector3 _home;
        Vector3 _wanderTo;
        Vector3 _knock;
        float _vy;
        float _atkReady;
        float _stateT;
        float _deadT;
        enum State { Idle, Chase, Attack, Die }
        State _state = State.Idle;

        public void Setup(EnemyKind k, CharacterStats s, ChibiAnim a)
        {
            kind = k;
            stats = s;
            anim = a;
            enemyId = k == EnemyKind.MiniBoss ? QuestCatalog.MiniBossId : QuestCatalog.MonsterFodder;
            if (k == EnemyKind.MiniBoss)
            {
                moveSpeed = 3.6f;
                detectRange = 16f;
                attackRange = 1.8f;
                attackDamage = 16f;
                attackCooldown = 1.5f;
                dropShard = false;
            }
            else
            {
                dropShard = true;
            }

            _cc = GetComponent<CharacterController>();
            _home = transform.position;
            _wanderTo = _home;
            if (stats != null)
                stats.OnDied += OnDied;
        }

        void Start()
        {
            if (_cc == null)
                _cc = GetComponent<CharacterController>();
            if (stats != null && !IsHooked())
                stats.OnDied += OnDied;
        }

        bool IsHooked()
        {
            return true;
        }

        public void ApplyKnockback(Vector3 v)
        {
            _knock += v;
            _knock.y = 0f;
        }

        void OnDied(CharacterStats s)
        {
            _state = State.Die;
            _deadT = 0f;
            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            if (anim != null)
                anim.dead = true;
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.NotifyKill(enemyId);
            if (dropShard && QuestSystem.Instance != null && QuestSystem.Instance.WantsItem(QuestCatalog.ItemShard))
                PickupItem.Spawn(transform.position + Vector3.up * 0.4f, QuestCatalog.ItemShard, "蚀核碎片");
            if (kind == EnemyKind.MiniBoss && GameManager.Instance != null)
                GameManager.Instance.OnMiniBossDefeated();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
                return;

            if (_state == State.Die)
            {
                _deadT += Time.deltaTime;
                if (_deadT > 1.2f)
                    Destroy(gameObject);
                return;
            }

            if (stats != null && stats.IsDead)
                return;

            FindPlayer();
            float dist = _player == null ? 999f : HorizontalDist(_player.position);

            switch (_state)
            {
                case State.Idle:
                    Wander();
                    if (_player != null && dist < detectRange && PlayerAlive())
                    {
                        _state = State.Chase;
                    }

                    break;
                case State.Chase:
                    if (_player == null || !PlayerAlive() || dist > detectRange * 1.4f)
                    {
                        _state = State.Idle;
                        break;
                    }

                    if (dist <= attackRange)
                    {
                        _state = State.Attack;
                        _stateT = 0f;
                        break;
                    }

                    MoveTowards(_player.position, moveSpeed);
                    Face(_player.position);
                    break;
                case State.Attack:
                    if (_player == null || !PlayerAlive())
                    {
                        _state = State.Idle;
                        break;
                    }

                    Face(_player.position);
                    _stateT += Time.deltaTime;
                    if (anim != null)
                    {
                        anim.attacking = true;
                        anim.attackT = Mathf.Clamp01(_stateT / 0.35f);
                    }

                    if (_stateT >= 0.35f && Time.time >= _atkReady)
                    {
                        _atkReady = Time.time + attackCooldown;
                        if (HorizontalDist(_player.position) <= attackRange + 0.35f)
                        {
                            var ps = _player.GetComponent<CharacterStats>();
                            if (ps != null)
                            {
                                ps.TakeDamage(attackDamage, _player.position + Vector3.up * 0.6f, stats);
                                var pc = _player.GetComponent<PlayerController>();
                                if (pc != null)
                                    pc.KnockVelocity += ( _player.position - transform.position).normalized * 2f;
                            }
                        }

                        if (anim != null)
                            anim.attacking = false;
                        _state = State.Chase;
                    }

                    MoveTowards(transform.position, 0f);
                    break;
            }
        }

        void Wander()
        {
            if (anim != null)
                anim.attacking = false;
            Vector3 to = _wanderTo - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.4f || Random.value < 0.005f)
            {
                Vector2 r = Random.insideUnitCircle * wanderRadius;
                _wanderTo = _home + new Vector3(r.x, 0f, r.y);
            }

            if (to.magnitude > 0.4f)
            {
                MoveTowards(_wanderTo, moveSpeed * 0.45f);
                Face(_wanderTo);
            }
            else
                MoveTowards(transform.position, 0f);
        }

        void MoveTowards(Vector3 pos, float speed)
        {
            if (_cc == null)
                return;
            Vector3 d = pos - transform.position;
            d.y = 0f;
            if (d.magnitude > 0.05f && speed > 0f)
                d = d.normalized * speed;
            else
                d = Vector3.zero;
            _knock = Vector3.Lerp(_knock, Vector3.zero, 7f * Time.deltaTime);
            d += _knock;
            if (!_cc.isGrounded)
                _vy += -24f * Time.deltaTime;
            else if (_vy < 0f)
                _vy = -2f;
            d.y = _vy;
            _cc.Move(d * Time.deltaTime);
            if (anim != null)
                anim.velocity = new Vector3(d.x, 0f, d.z);
        }

        void Face(Vector3 pos)
        {
            Vector3 d = pos - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.001f)
                return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 8f * Time.deltaTime);
        }

        void FindPlayer()
        {
            if (_player != null)
                return;
            if (GameManager.Instance != null && GameManager.Instance.Player != null)
                _player = GameManager.Instance.Player.transform;
            else
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                    _player = p.transform;
            }
        }

        bool PlayerAlive()
        {
            if (_player == null)
                return false;
            var s = _player.GetComponent<CharacterStats>();
            return s != null && !s.IsDead;
        }

        float HorizontalDist(Vector3 p)
        {
            Vector3 a = transform.position;
            a.y = 0f;
            p.y = 0f;
            return Vector3.Distance(a, p);
        }
    }
}

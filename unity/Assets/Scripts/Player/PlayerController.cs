using UnityEngine;

namespace ChibiARPG
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerClassDefinition classDef;
        public CharacterStats stats;
        public ChibiAnim anim;
        public PlayerCombat combat;
        public float moveSpeed = 5.6f;
        public float gravity = -24f;
        public float turnSpeed = 14f;

        CharacterController _cc;
        float _vy;
        Vector3 _lastVel;
        public Vector3 KnockVelocity;

        public void Setup(PlayerClassDefinition def, CharacterStats s, ChibiAnim a, PlayerCombat c)
        {
            classDef = def;
            stats = s;
            anim = a;
            combat = c;
            if (def != null)
                moveSpeed = def.moveSpeed;
            _cc = GetComponent<CharacterController>();
        }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (stats != null && stats.IsDead)
            {
                if (anim != null)
                {
                    anim.velocity = Vector3.zero;
                    anim.dead = true;
                }

                return;
            }

            if (GameManager.Instance != null && (GameManager.Instance.IsPaused || GameManager.Instance.InMenu))
            {
                if (anim != null)
                    anim.velocity = Vector3.zero;
                return;
            }

            Vector2 input = ReadMove();
            Vector3 camF = Vector3.forward;
            Vector3 camR = Vector3.right;
            Camera cam = Camera.main;
            if (cam != null)
            {
                camF = cam.transform.forward;
                camR = cam.transform.right;
                camF.y = 0f;
                camR.y = 0f;
                camF.Normalize();
                camR.Normalize();
            }

            Vector3 wish = camR * input.x + camF * input.y;
            if (wish.sqrMagnitude > 1f)
                wish.Normalize();

            bool locked = combat != null && combat.IsBusy;
            if (!locked && wish.sqrMagnitude > 0.001f)
            {
                Quaternion face = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }

            if (_cc.isGrounded && _vy < 0f)
                _vy = -2f;
            _vy += gravity * Time.deltaTime;

            Vector3 motion = (locked ? Vector3.zero : wish * moveSpeed);
            KnockVelocity = Vector3.Lerp(KnockVelocity, Vector3.zero, 8f * Time.deltaTime);
            motion += KnockVelocity;
            motion.y = _vy;
            _cc.Move(motion * Time.deltaTime);
            _lastVel = new Vector3(motion.x, 0f, motion.z);
            if (anim != null)
            {
                anim.velocity = _lastVel;
                anim.dead = false;
            }

            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space))
            {
                if (combat != null)
                    combat.TryAttack();
            }

            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Q))
            {
                if (combat != null)
                    combat.TrySkill();
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TryTalk();
            }
        }

        Vector2 ReadMove()
        {
            Vector2 v = Vector2.zero;
            if (VirtualJoystick.Instance != null && VirtualJoystick.Instance.Value.sqrMagnitude > 0.02f)
                v = VirtualJoystick.Instance.Value;
            else
                v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (v.sqrMagnitude > 1f)
                v.Normalize();
            return v;
        }
    }
}

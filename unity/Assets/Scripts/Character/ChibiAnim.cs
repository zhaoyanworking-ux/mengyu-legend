using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 用程序化摆动模拟 Q 版走路、待机呼吸、出拳和旋转技能，无需 Animator。
    /// </summary>
    public class ChibiAnim : MonoBehaviour
    {
        public Transform body;
        public Transform head;
        public Transform armL;
        public Transform armR;
        public Transform legL;
        public Transform legR;

        public Vector3 velocity;
        public bool attacking;
        public bool spinning;
        public bool dead;
        public float attackT;

        Vector3 _body0, _head0;
        Quaternion _armL0, _armR0, _legL0, _legR0;
        float _phase;
        float _deadT;

        public void CaptureRests()
        {
            if (body != null) _body0 = body.localPosition;
            if (head != null) _head0 = head.localPosition;
            if (armL != null) _armL0 = armL.localRotation;
            if (armR != null) _armR0 = armR.localRotation;
            if (legL != null) _legL0 = legL.localRotation;
            if (legR != null) _legR0 = legR.localRotation;
        }

        void LateUpdate()
        {
            if (body == null)
                return;

            if (dead)
            {
                _deadT += Time.deltaTime;
                float t = Mathf.Clamp01(_deadT / 0.55f);
                float squash = 1f - t * 0.75f;
                transform.localScale = new Vector3(1.15f + t * 0.4f, Mathf.Max(0.08f, squash), 1.15f + t * 0.4f);
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, 0.05f, t);
                return;
            }

            _deadT = 0f;
            transform.localScale = Vector3.one;

            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            bool moving = speed > 0.15f;
            _phase += Time.deltaTime * (moving ? 10f + speed * 1.5f : 2.2f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(_phase)) * 0.07f : Mathf.Sin(_phase) * 0.03f;

            if (body != null)
                body.localPosition = _body0 + new Vector3(0f, bob, 0f);
            if (head != null)
                head.localPosition = _head0 + new Vector3(0f, bob * 0.45f, 0f);

            if (spinning)
            {
                if (armL != null) armL.localRotation = _armL0 * Quaternion.Euler(0f, 0f, 80f);
                if (armR != null) armR.localRotation = _armR0 * Quaternion.Euler(0f, 0f, -80f);
                if (legL != null) legL.localRotation = _legL0;
                if (legR != null) legR.localRotation = _legR0;
                if (body != null)
                    body.localRotation = Quaternion.Euler(0f, Time.time * 720f, 12f);
                return;
            }

            if (body != null)
                body.localRotation = Quaternion.identity;

            if (attacking)
            {
                float w = Mathf.Sin(Mathf.Clamp01(attackT) * Mathf.PI);
                if (armR != null)
                    armR.localRotation = _armR0 * Quaternion.Euler(-20f - w * 70f, -w * 40f, -20f);
                if (armL != null)
                    armL.localRotation = _armL0 * Quaternion.Euler(-10f, 15f, 25f);
                if (legL != null) legL.localRotation = _legL0;
                if (legR != null) legR.localRotation = _legR0;
                return;
            }

            float swing = moving ? Mathf.Sin(_phase) * 32f : Mathf.Sin(_phase * 0.5f) * 6f;
            if (armL != null) armL.localRotation = _armL0 * Quaternion.Euler(swing, 0f, 0f);
            if (armR != null) armR.localRotation = _armR0 * Quaternion.Euler(-swing, 0f, 0f);
            if (legL != null) legL.localRotation = _legL0 * Quaternion.Euler(-swing * 0.9f, 0f, 0f);
            if (legR != null) legR.localRotation = _legR0 * Quaternion.Euler(swing * 0.9f, 0f, 0f);
        }
    }
}

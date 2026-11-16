using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 固定俯视角跟随，类似轻度等距的第三人称机位。
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 11.5f, -9.2f);
        public Vector3 fixedEuler = new Vector3(48f, 0f, 0f);
        public float follow = 8f;
        public Vector3 lookOffset = new Vector3(0f, 1.1f, 0f);

        public void Snap()
        {
            if (target == null)
                return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(fixedEuler);
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desired = target.position + offset;
            float t = 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            Quaternion look = Quaternion.LookRotation((target.position + lookOffset) - transform.position, Vector3.up);
            // 锁死滚转，保持固定俯仰/朝向感觉
            Vector3 euler = look.eulerAngles;
            euler.z = 0f;
            euler.x = fixedEuler.x;
            euler.y = fixedEuler.y;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}

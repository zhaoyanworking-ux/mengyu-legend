using UnityEngine;

namespace ChibiARPG
{
    public class Billboard : MonoBehaviour
    {
        public bool lockRoll = true;

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            Vector3 dir = transform.position - cam.transform.position;
            if (lockRoll)
                dir.y = Mathf.Max(dir.y, 0.01f);
            if (dir.sqrMagnitude < 0.0001f)
                return;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}

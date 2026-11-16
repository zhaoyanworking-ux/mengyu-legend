using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 轻量镜头震动，剑士受击/出刀时使用。
    /// </summary>
    public class CameraImpulse : MonoBehaviour
    {
        public static CameraImpulse Instance { get; private set; }

        Vector3 _extra;
        float _trauma;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Shake(float amount)
        {
            if (Instance == null)
                return;
            Instance._trauma = Mathf.Clamp01(Instance._trauma + amount);
        }

        void LateUpdate()
        {
            if (_trauma <= 0.0001f)
            {
                transform.localPosition = Vector3.zero;
                return;
            }

            _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 1.8f);
            float a = _trauma * _trauma;
            _extra = new Vector3(
                (Mathf.PerlinNoise(Time.unscaledTime * 28f, 0.3f) - 0.5f) * 0.35f,
                (Mathf.PerlinNoise(1.7f, Time.unscaledTime * 26f) - 0.5f) * 0.28f,
                0f) * a;
            transform.localPosition = _extra;
        }
    }
}

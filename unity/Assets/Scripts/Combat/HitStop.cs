using System.Collections;
using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 近战命中顿帧，强化剑士连击手感。
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }

        Coroutine _co;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Play(float seconds, float scale)
        {
            if (Instance == null)
                return;
            if (Time.timeScale <= 0.01f)
                return;
            if (Instance._co != null)
                Instance.StopCoroutine(Instance._co);
            Instance._co = Instance.StartCoroutine(Instance.Routine(seconds, scale));
        }

        IEnumerator Routine(float seconds, float scale)
        {
            float prev = Time.timeScale;
            if (prev <= 0.01f)
                yield break;
            Time.timeScale = Mathf.Clamp(scale, 0.02f, 1f);
            yield return new WaitForSecondsRealtime(seconds);
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
            _co = null;
        }
    }
}

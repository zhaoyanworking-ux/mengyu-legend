using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        readonly List<DamagePopup> _pool = new List<DamagePopup>();

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Show(Vector3 worldPos, float amount, bool critical)
        {
            if (Instance == null)
                return;
            Instance.Spawn(worldPos, amount, critical);
        }

        void Spawn(Vector3 worldPos, float amount, bool critical)
        {
            DamagePopup popup = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    popup = _pool[i];
                    break;
                }
            }

            if (popup == null)
            {
                var go = new GameObject("伤害数字");
                go.transform.SetParent(transform, true);
                var tm = go.AddComponent<TextMesh>();
                tm.font = BuiltinFont.Get();
                tm.fontSize = 48;
                tm.characterSize = 0.06f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                if (tm.font != null)
                    go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
                popup = go.AddComponent<DamagePopup>();
                _pool.Add(popup);
            }

            popup.transform.position = worldPos + Vector3.up * 0.3f;
            Color col = critical ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.95f, 0.95f);
            string text = Mathf.RoundToInt(amount).ToString();
            if (critical)
                text = "暴击 " + text;
            popup.Play(text, col, critical ? 1.35f : 1f);
        }
    }
}

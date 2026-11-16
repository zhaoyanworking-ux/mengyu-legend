using UnityEngine;

namespace ChibiARPG
{
    public class WorldHealthBar : MonoBehaviour
    {
        public CharacterStats stats;
        public Transform fill;
        public GameObject root;
        public Vector3 offset = new Vector3(0f, 1.55f, 0f);

        Transform _follow;

        public void Setup(CharacterStats s, Transform follow, Vector3 extraOffset)
        {
            stats = s;
            _follow = follow;
            offset = extraOffset;
            if (stats != null)
            {
                stats.OnHpChanged += OnHp;
                OnHp(stats.hp, stats.maxHp);
            }
        }

        void OnDestroy()
        {
            if (stats != null)
                stats.OnHpChanged -= OnHp;
        }

        void OnHp(float hp, float max)
        {
            float r = max <= 0f ? 0f : Mathf.Clamp01(hp / max);
            if (fill != null)
            {
                Vector3 s = fill.localScale;
                s.x = Mathf.Max(0.001f, r);
                fill.localScale = s;
                var rdr = fill.GetComponent<Renderer>();
                if (rdr != null && rdr.material != null && rdr.material.HasProperty("_Color"))
                {
                    Color c = Color.Lerp(new Color(0.95f, 0.25f, 0.25f), new Color(0.35f, 0.95f, 0.4f), r);
                    rdr.material.SetColor("_Color", c);
                }
            }

            if (root != null && stats != null)
                root.SetActive(!stats.IsDead);
        }

        void LateUpdate()
        {
            if (_follow == null)
                return;
            transform.position = _follow.position + offset;
        }

        public static WorldHealthBar Create(CharacterStats stats, Transform follow, Vector3 offset, float width = 0.9f)
        {
            var go = new GameObject("生命条");
            go.AddComponent<Billboard>();
            var bar = go.AddComponent<WorldHealthBar>();

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Bg";
            bg.transform.SetParent(go.transform, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(width, 0.11f, 1f);
            Object.Destroy(bg.GetComponent<Collider>());
            var bgR = bg.GetComponent<Renderer>();
            bgR.sharedMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            bgR.sharedMaterial.color = new Color(0.12f, 0.08f, 0.12f, 0.9f);
            bgR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var fillGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillGo.name = "Fill";
            fillGo.transform.SetParent(go.transform, false);
            fillGo.transform.localPosition = new Vector3(-width * 0.5f + 0.02f, 0f, -0.01f);
            fillGo.transform.localScale = new Vector3(width - 0.04f, 0.08f, 1f);
            Object.Destroy(fillGo.GetComponent<Collider>());
            var fillR = fillGo.GetComponent<Renderer>();
            fillR.sharedMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            fillR.sharedMaterial.color = new Color(0.35f, 0.95f, 0.4f, 1f);
            fillR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 以左侧为轴缩放
            var pivot = new GameObject("FillPivot").transform;
            pivot.SetParent(go.transform, false);
            pivot.localPosition = new Vector3(-(width - 0.04f) * 0.5f, 0f, -0.01f);
            fillGo.transform.SetParent(pivot, true);
            fillGo.transform.localPosition = new Vector3((width - 0.04f) * 0.5f, 0f, 0f);

            bar.root = go;
            bar.fill = pivot;
            bar.Setup(stats, follow, offset);
            return bar;
        }
    }
}

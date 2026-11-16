using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 缓存卡通材质。着色器放在 Resources 下并加入 Always Included，WebGL 打包时不会被裁掉。
    /// </summary>
    public static class ToonMaterialUtil
    {
        public const string ShaderName = "ChibiARPG/ToonOutline";

        static Shader _shader;
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Shader GetShader()
        {
            if (_shader != null)
                return _shader;

            _shader = Shader.Find(ShaderName);
            if (_shader == null)
            {
                var res = Resources.Load<Shader>("ToonOutline");
                if (res != null)
                    _shader = res;
            }

            if (_shader == null)
                _shader = Shader.Find("Diffuse");
            if (_shader == null)
                _shader = Shader.Find("Standard");
            if (_shader == null)
                _shader = Shader.Find("Unlit/Color");
            return _shader;
        }

        public static Material Get(Color color, float outlineWidth = 0.022f, Color? outline = null, Color? shadow = null)
        {
            string key = color.ToString() + outlineWidth.ToString("0.000") +
                         (outline.HasValue ? outline.Value.ToString() : "") +
                         (shadow.HasValue ? shadow.Value.ToString() : "");
            Material mat;
            if (Cache.TryGetValue(key, out mat) && mat != null)
                return mat;

            var shader = GetShader();
            mat = new Material(shader);
            mat.name = "Toon_" + ColorUtility.ToHtmlStringRGB(color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_OutlineWidth"))
                mat.SetFloat("_OutlineWidth", outlineWidth);
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", outline ?? new Color(0.16f, 0.1f, 0.22f, 1f));
            if (mat.HasProperty("_ShadowColor"))
            {
                Color s = shadow ?? Color.Lerp(color, new Color(0.35f, 0.25f, 0.5f, 1f), 0.45f);
                s.a = 1f;
                mat.SetColor("_ShadowColor", s);
            }

            mat.enableInstancing = false;
            Cache[key] = mat;
            return mat;
        }
    }
}

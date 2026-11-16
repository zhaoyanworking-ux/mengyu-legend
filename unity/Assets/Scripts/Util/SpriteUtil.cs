using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 运行时生成简单 UI 精灵（圆、圆角矩形），避免依赖外部贴图。
    /// </summary>
    public static class SpriteUtil
    {
        public static Sprite Circle(int size, float innerRadius = 1f)
        {
            size = Mathf.Max(8, size);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float c = (size - 1) * 0.5f;
            float r = c * innerRadius;
            float aa = 1.6f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float a = Mathf.Clamp01((r - d) / aa);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite RoundedRect(int width, int height, int radius)
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            radius = Mathf.Clamp(radius, 1, Mathf.Min(width, height) / 2);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float aa = 1.6f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= width - radius) dx = x - (width - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= height - radius) dy = y - (height - radius - 1);

                    float a = 1f;
                    if (dx > 0f && dy > 0f)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        a = Mathf.Clamp01((radius - d) / aa);
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite White()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}

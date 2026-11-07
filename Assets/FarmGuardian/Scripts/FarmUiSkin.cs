using UnityEngine;

namespace FarmGuardian
{
    /// <summary>Small reusable UI shapes generated in memory; no third-party artwork is required.</summary>
    public sealed class FarmUiSkin
    {
        public Sprite Rounded { get; }
        public Sprite Circle { get; }
        public FarmUiSkin() { Rounded = Shape(24, true); Circle = Shape(48, false); }

        static Sprite Shape(float radius, bool sliced)
        {
            const int size = 96;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2(Mathf.Abs(x + 0.5f - size / 2f), Mathf.Abs(y + 0.5f - size / 2f));
                    Vector2 q = point - Vector2.one * (size / 2f - radius - 1);
                    float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance));
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Farm UI shape", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect,
                sliced ? Vector4.one * 25 : Vector4.zero);
        }

        public void Dispose()
        {
            Object.Destroy(Rounded.texture); Object.Destroy(Circle.texture);
            Object.Destroy(Rounded); Object.Destroy(Circle);
        }
    }
}

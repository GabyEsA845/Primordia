using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Icono PROVISIONAL, hecho por código, de la semilla morada del HUD (16×16 px, 16 PPU): una gota morada con vetas y un brillo. Hay que
    /// sustituirlo por el dibujo de verdad (Opus, preguntando antes).
    /// </summary>
    public static class PurpleSeedIcon
    {
        const int Size = 16;
        static readonly Color32 Outline = new(24, 12, 40, 255);
        static readonly Color32 Dark = new(92, 40, 150, 255);
        static readonly Color32 Mid = new(150, 80, 220, 255);
        static readonly Color32 Light = new(214, 160, 255, 255);

        static Sprite sprite;

        public static Sprite Sprite => sprite != null ? sprite : sprite = Build();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => sprite = null;

        static Sprite Build()
        {
            var pixels = new Color32[Size * Size];
            // Gota: ancha abajo y en punta arriba.
            bool Inside(int x, int y)
            {
                if (y < 1 || y > 14)
                    return false;
                float t = (y - 1) / 13f;
                float halfWidth = Mathf.Lerp(5.5f, 0.6f, Mathf.Pow(t, 1.6f));
                return Mathf.Abs(x - 7.5f) <= halfWidth;
            }

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (!Inside(x, y))
                        continue;
                    bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
                    Color32 color = edge ? Outline : (x - 7.5f < -1f && y > 3 && y < 10) ? Light : (x + y) % 5 == 0 ? Dark : Mid;
                    pixels[y * Size + x] = color;
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "PurpleSeedIcon" };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}

using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Dibujo PROVISIONAL, hecho por código, del cofre de cono de Lepidodendron de las Ladronas (32×24 px, 16 PPU): un cono tumbado de
    /// escamas pardas que se abre por la mitad. Hay que sustituirlo por el arte de verdad (Opus, preguntando antes).
    /// </summary>
    public static class ConeChestPlaceholder
    {
        const int Width = 32;
        const int Height = 24;
        static readonly Color32 Outline = new(43, 26, 15, 255);
        static readonly Color32 Dark = new(84, 51, 26, 255);
        static readonly Color32 Mid = new(122, 74, 36, 255);
        static readonly Color32 Light = new(168, 112, 58, 255);
        static readonly Color32 Glow = new(240, 200, 100, 255);

        static Sprite[] idle;
        static Sprite[] open;
        static Sprite opened;

        public static Sprite[] Idle => Build()[0];

        public static Sprite[] Open => Build()[1];

        public static Sprite Opened => Build()[2][0];

        static Sprite[][] Build()
        {
            if (idle == null || idle.Length == 0 || idle[0] == null)
            {
                idle = new[] { Make(0) };
                open = new[] { Make(1), Make(3), Make(5) };
                opened = Make(6);
            }
            return new[] { idle, open, new[] { opened } };
        }

        static Sprite Make(int lift)
        {
            var pixels = new Color32[Width * Height];
            bool Inside(int x, int y, int dy)
            {
                // La mitad de arriba sube `lift` píxeles al abrirse.
                int sy = y >= 10 + lift ? y - lift : y;
                if (y > 9 && y < 10 + lift)
                    return false;
                float nx = (x - 15.5f) / 14f;
                float ny = (sy - 10f + dy) / 8f;
                return nx * nx + ny * ny <= 1f;
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!Inside(x, y, 0))
                    {
                        // El hueco que deja la tapa al subir brilla.
                        if (lift > 0 && y > 9 && y < 10 + lift && Mathf.Abs(x - 15.5f) < 12f)
                            pixels[y * Width + x] = Glow;
                        continue;
                    }
                    bool edge = !Inside(x - 1, y, 0) || !Inside(x + 1, y, 0) || !Inside(x, y - 1, 0) || !Inside(x, y + 1, 0);
                    // Escamas: diagonales que se cruzan.
                    bool line = (x + y) % 4 == 0 || (x - y + 64) % 4 == 0;
                    pixels[y * Width + x] = edge ? Outline : line ? Dark : (x + y) % 5 == 1 ? Light : Mid;
                }
            }

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "ConeChestPlaceholder" };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}

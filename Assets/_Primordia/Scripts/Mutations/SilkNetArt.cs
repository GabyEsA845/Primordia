using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Dibuja por código la telaraña de la red de las Patas hilanderas con la forma exacta del polígono (el algoritmo aprobado de
    /// docs/multimedia-temp/floor3-jump/generar_salto_piso3.py): radios del centro a cada esquina (uno cada ~14 px de contorno extra),
    /// anillos combados entre ellos, un hilo más claro que une las esquinas y gotas de rocío doradas. Da 9 cuadros: 4 en que se teje
    /// (los radios crecen del centro y los anillos aparecen hacia fuera), 2 en que brilla y 3 en que se deshilacha.
    /// </summary>
    public static class SilkNetArt
    {
        public const int GrowFrames = 4;
        public const int TwinkleFrames = 2;
        public const int FrayFrames = 3;

        const float PixelsPerUnit = 16f;
        const int Margin = 2;
        const int RingSpacing = 7;
        const float SpokeEvery = 14f;

        static readonly Color32[] Silk =
        {
            new(0x7a, 0x8a, 0x84, 255), new(0xae, 0xbc, 0xb4, 255), new(0xd8, 0xe2, 0xdc, 255), new(0xf2, 0xf8, 0xf4, 255),
        };
        static readonly Color32 DewDark = new(0xc8, 0xa0, 0x40, 255);
        static readonly Color32 DewLight = new(0xff, 0xe6, 0x80, 255);
        static readonly Color32 White = new(255, 255, 255, 255);

        static readonly float[] Grow = { 0.25f, 0.55f, 0.85f, 1f };
        static readonly float[] Fray = { 0.3f, 0.6f, 0.85f };

        /// <summary>
        /// Los cuadros de la red sobre ese contorno (en unidades del mundo). El centro del sprite es el centro de la caja del polígono:
        /// <paramref name="center"/> dice dónde ponerlo. Null si el contorno no tiene área.
        /// </summary>
        public static Sprite[] Build(IReadOnlyList<Vector2> polygon, out Vector2 center)
        {
            center = Vector2.zero;
            if (polygon == null || polygon.Count < 3)
                return null;

            var min = polygon[0];
            var max = polygon[0];
            foreach (var point in polygon)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            int width = Mathf.CeilToInt((max.x - min.x) * PixelsPerUnit) + Margin * 2 + 1;
            int height = Mathf.CeilToInt((max.y - min.y) * PixelsPerUnit) + Margin * 2 + 1;
            if (width < 8 || height < 8)
                return null;
            center = (min + max) / 2f;

            // El polígono en píxeles de la textura (el centro de la caja en el centro de la textura).
            var shape = new List<Vector2>();
            foreach (var point in polygon)
                shape.Add((point - min) * PixelsPerUnit + new Vector2(Margin, Margin));

            var frames = new List<Sprite>();
            foreach (float grow in Grow)
                frames.Add(Frame(shape, width, height, grow, 0f, 0));
            for (int i = 1; i <= TwinkleFrames; i++)
                frames.Add(Frame(shape, width, height, 1f, 0f, i));
            foreach (float fray in Fray)
                frames.Add(Frame(shape, width, height, 1f, fray, 0));
            return frames.ToArray();
        }

        static Sprite Frame(List<Vector2> shape, int width, int height, float grow, float fray, int twinkle)
        {
            var pixels = new Dictionary<Vector2Int, Color32>();
            Weave(shape, grow, pixels);

            var rnd = new System.Random(20);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var data = new Color32[width * height];
            foreach (var pair in pixels)
            {
                var p = pair.Key;
                if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height)
                    continue;
                if (fray > 0f && rnd.NextDouble() < fray)
                    continue;
                var c = pair.Value;
                bool dew = c.Equals(DewDark) || c.Equals(DewLight);
                if (dew && twinkle > 0 && (p.x + p.y + twinkle) % 3 == 0)
                    c = White;
                c.a = (byte)(c.a * (1f - 0.55f * fray));
                data[p.y * width + p.x] = c;
            }
            texture.SetPixels32(data);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        static void Put(Dictionary<Vector2Int, Color32> pixels, int x, int y, Color32 color, byte alpha)
        {
            var key = new Vector2Int(x, y);
            if (pixels.TryGetValue(key, out var old) && old.a >= alpha)
                return;
            color.a = alpha;
            pixels[key] = color;
        }

        static IEnumerable<Vector2Int> Line(Vector2 from, Vector2 to)
        {
            int steps = (int)Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y)) + 1;
            for (int k = 0; k <= steps; k++)
            {
                var p = Vector2.Lerp(from, to, k / (float)steps);
                yield return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
            }
        }

        static void Weave(List<Vector2> shape, float grow, Dictionary<Vector2Int, Color32> pixels)
        {
            var rnd = new System.Random(3);
            var center = Vector2.zero;
            foreach (var point in shape)
                center += point;
            center /= shape.Count;

            // Anclajes: las esquinas y, en los lados largos, uno cada ~14 px.
            var anchors = new List<Vector2>();
            for (int i = 0; i < shape.Count; i++)
            {
                var a = shape[i];
                var b = shape[(i + 1) % shape.Count];
                int parts = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b) / SpokeEvery));
                for (int k = 0; k < parts; k++)
                    anchors.Add(Vector2.Lerp(a, b, k / (float)parts));
            }
            float reach = 0f;
            foreach (var anchor in anchors)
                reach = Mathf.Max(reach, Vector2.Distance(center, anchor));
            int rings = Mathf.Max(2, (int)(reach / RingSpacing));

            // Radios.
            foreach (var anchor in anchors)
            {
                foreach (var p in Line(center, center + (anchor - center) * grow))
                    Put(pixels, p.x, p.y, Silk[2], 235);
            }

            // Anillos, combados un poco hacia el centro entre cada par de radios.
            for (int k = 1; k <= rings; k++)
            {
                float f = k / (rings + 0.35f);
                if (grow < f)
                    continue;
                for (int i = 0; i < anchors.Count; i++)
                {
                    var a = anchors[i];
                    var b = anchors[(i + 1) % anchors.Count];
                    var p0 = center + (a - center) * f;
                    var p1 = center + (b - center) * f;
                    var mid = (p0 + p1) / 2f;
                    float sag = 0.1f * Mathf.Min(1f, Vector2.Distance(p0, p1) / 10f);
                    mid += (center - mid) * sag;
                    foreach (var p in Line(p0, mid))
                        Put(pixels, p.x, p.y, Silk[1], 190);
                    foreach (var p in Line(mid, p1))
                        Put(pixels, p.x, p.y, Silk[1], 190);
                    if (rnd.NextDouble() < 0.35)
                        Put(pixels, Mathf.RoundToInt(p0.x), Mathf.RoundToInt(p0.y), rnd.NextDouble() < 0.5 ? DewLight : DewDark, 255);
                }
            }

            // Contorno: el hilo que une los extremos.
            if (grow >= 1f)
            {
                for (int i = 0; i < shape.Count; i++)
                {
                    foreach (var p in Line(shape[i], shape[(i + 1) % shape.Count]))
                        Put(pixels, p.x, p.y, Silk[3], 255);
                }
            }
            Put(pixels, Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), Silk[3], 255);
        }
    }
}

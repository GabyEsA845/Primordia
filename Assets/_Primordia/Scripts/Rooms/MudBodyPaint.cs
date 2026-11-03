using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El lodo pintado sobre un sprite del cuerpo (jugador o enemigo hundiéndose en el lodo profundo del piso 3), píxel a píxel
    /// en una copia del cuadro: baja el cuerpo lo hundido, cubre de textura de lodo desde los pies hasta el nivel con un borde
    /// grumoso que ondula y deja manchas al salir. Sigue la receta aprobada en docs/multimedia-temp/floor3-polish. No toca el
    /// contorno ni el blanco de los ojos. Código puro (sin escena), por eso se prueba aparte.
    /// </summary>
    public static class MudBodyPaint
    {
        // Rampa del lodo profundo (de oscuro a claro) y brillo húmedo; el índice 5 es el brillo.
        static readonly Color32[] Ramp =
        {
            new(0x22, 0x16, 0x0f, 255), new(0x33, 0x22, 0x1a, 255), new(0x4a, 0x33, 0x24, 255),
            new(0x63, 0x48, 0x30, 255), new(0x86, 0x68, 0x3f, 255), new(0xb0, 0x94, 0x68, 255),
        };
        // Rampa del barro seco (Fx_MudBody_TexDry, de oscuro a claro): placas y grietas más claras y apagadas.
        static readonly Color32[] RampDry =
        {
            new(46, 36, 28, 255), new(78, 62, 44, 255), new(110, 90, 64, 255), new(142, 122, 90, 255), new(176, 160, 124, 255),
        };
        static readonly Color32 Outline = new(24, 20, 37, 255);
        static readonly Color32 EyeWhite = new(244, 244, 232, 255);

        /// <summary>Lo que cambia de un cuadro a otro.</summary>
        public struct Settings
        {
            /// <summary>Alto cubierto de lodo sobre los pies, en píxeles.</summary>
            public float level;
            /// <summary>Alto de las manchas que quedan al salir (0 = ninguna).</summary>
            public float stain;
            /// <summary>0..1: cuánto se ha limpiado (con 1 no queda mancha).</summary>
            public float clean;
            /// <summary>Píxeles que baja el cuerpo, hundido bajo la superficie.</summary>
            public int sunk;
            /// <summary>Fila (contada desde abajo) de los pies, la superficie del lodo.</summary>
            public int feetRow;
            /// <summary>Tiempo en cuadros (mueve el borde y la textura).</summary>
            public int t;
            /// <summary>Enemigos: dos tonos por la luz del cuerpo y el borde, sin textura ni manchas.</summary>
            public bool simple;
            /// <summary>0..1: qué parte del barro está seca (por manchas; el Eryops). Pide <see cref="Tables.dryIndex"/>.</summary>
            public float dry;
        }

        /// <summary>Datos de un sprite leído una vez: sus píxeles, dónde empieza y acaba el dibujo y las luces del cuerpo.</summary>
        public class Data
        {
            public int width, height;
            public Color32[] pixels;
            /// <summary>Primera y última fila con dibujo, contadas desde abajo.</summary>
            public int minRow, maxRow;
            /// <summary>Luminosidad del primer y el tercer cuartil del cuerpo (sin contorno ni ojos).</summary>
            public float lo, hi;
        }

        /// <summary>El ruido (32×32, gris) y la textura (16×16) del lodo, ya leídos.</summary>
        public class Tables
        {
            public Color32[] noise;
            public int noiseSize;
            public int[] texIndex;
            public int texSize;
            /// <summary>La textura del barro seco (16×16, como la del lodo) en índices de <see cref="RampDry"/>; null si no hay.</summary>
            public int[] dryIndex;
        }

        static readonly Dictionary<Sprite, Data> dataCache = new();
        static readonly Dictionary<Texture, Color32[]> textureCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            dataCache.Clear();
            textureCache.Clear();
        }

        // ---------------------------------------------------------------------------------------------
        // Lectura de sprites (aunque la textura no sea legible) y de las tablas
        // ---------------------------------------------------------------------------------------------
        /// <summary>Los píxeles del sprite y sus datos, o null si no se pueden leer.</summary>
        public static Data DataOf(Sprite sprite)
        {
            if (sprite == null)
                return null;
            if (dataCache.TryGetValue(sprite, out var cached))
                return cached;

            var pixels = TexturePixels(sprite.texture);
            var rect = sprite.textureRect;
            Data data = null;
            if (pixels != null)
            {
                int x0 = Mathf.RoundToInt(rect.x), y0 = Mathf.RoundToInt(rect.y);
                int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
                data = new Data { width = w, height = h, pixels = new Color32[w * h], minRow = h, maxRow = -1 };
                int texWidth = sprite.texture.width;
                var lums = new List<float>();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var c = pixels[(y0 + y) * texWidth + x0 + x];
                        data.pixels[y * w + x] = c;
                        if (c.a == 0)
                            continue;
                        data.minRow = Mathf.Min(data.minRow, y);
                        data.maxRow = Mathf.Max(data.maxRow, y);
                        if (!IsOutline(c) && !IsEye(c))
                            lums.Add(Luminance(c));
                    }
                }
                lums.Sort();
                if (lums.Count > 0)
                {
                    data.lo = lums[lums.Count / 4];
                    data.hi = lums[3 * lums.Count / 4];
                }
                if (data.maxRow < 0)
                    data.minRow = data.maxRow = 0;
            }
            dataCache[sprite] = data;
            return data;
        }

        static Color32[] TexturePixels(Texture2D texture)
        {
            if (texture == null)
                return null;
            if (textureCache.TryGetValue(texture, out var cached))
                return cached;

            Color32[] pixels = null;
            if (texture.isReadable)
                pixels = texture.GetPixels32();
            else
            {
                // Una textura no legible se pasa por una textura de render para leerla una sola vez.
                var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                try
                {
                    Graphics.Blit(texture, target);
                    RenderTexture.active = target;
                    var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    copy.Apply();
                    pixels = copy.GetPixels32();
                    Object.Destroy(copy);
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(target);
                }
            }
            textureCache[texture] = pixels;
            return pixels;
        }

        /// <summary>El ruido y la textura leídos del arte, o null si falta alguno.</summary>
        public static Tables TablesFrom(Sprite noiseSprite, Sprite textureSprite, Sprite drySprite = null)
        {
            var noise = DataOf(noiseSprite);
            var texture = DataOf(textureSprite);
            if (noise == null || texture == null || noise.width < 1 || texture.width < 1)
                return null;

            var index = new int[texture.pixels.Length];
            for (int i = 0; i < index.Length; i++)
                index[i] = NearestRamp(texture.pixels[i]);
            int[] dryIndex = null;
            var dry = DataOf(drySprite);
            if (dry != null && dry.width == texture.width && dry.height == texture.height)
            {
                dryIndex = new int[dry.pixels.Length];
                for (int i = 0; i < dryIndex.Length; i++)
                    dryIndex[i] = Nearest(RampDry, dry.pixels[i]);
            }
            return new Tables { noise = noise.pixels, texIndex = index, noiseSize = noise.width, texSize = texture.width, dryIndex = dryIndex };
        }

        static int NearestRamp(Color32 c) => Nearest(Ramp, c);

        static int Nearest(Color32[] ramp, Color32 c)
        {
            int best = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < ramp.Length; i++)
            {
                int dr = c.r - ramp[i].r, dg = c.g - ramp[i].g, db = c.b - ramp[i].b;
                int distance = dr * dr + dg * dg + db * db;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        // ---------------------------------------------------------------------------------------------
        // Pintar
        // ---------------------------------------------------------------------------------------------
        /// <summary>
        /// Pinta el cuadro: <paramref name="destination"/> (del tamaño del sprite) recibe el cuerpo bajado lo hundido y cubierto de
        /// lodo. Sin nada que pintar, copia el cuadro tal cual.
        /// </summary>
        public static void Paint(Data source, Color32[] destination, Settings s, Tables tables)
        {
            System.Array.Clear(destination, 0, destination.Length);
            int w = source.width, height = source.height;
            for (int r = 0; r < height; r++)
            {
                int above = r - s.feetRow;
                // Lo que queda por debajo de la superficie no se ve.
                if (above < s.sunk)
                    continue;
                int row = r - s.sunk;
                if (row < 0)
                    continue;
                int rowTop = height - 1 - r;

                for (int x = 0; x < w; x++)
                {
                    var c = source.pixels[r * w + x];
                    if (c.a == 0)
                        continue;
                    if (!IsOutline(c) && !IsEye(c))
                        c = Cover(c, source, tables, s, x, above, rowTop);
                    destination[row * w + x] = c;
                }
            }
        }

        static Color32 Cover(Color32 c, Data source, Tables tables, Settings s, int x, int above, int rowTop)
        {
            float edge = s.level + (Noise(tables, (x + s.t) % 32, x % 32) - 0.5f) * 3.2f + Mathf.Sin(x * 1.3f + s.t * 0.9f) * 0.6f;
            bool covered = s.level > 0f && above <= edge;
            bool rim = covered && above > edge - 1f;
            bool stained = !s.simple && !covered && s.stain > 0f && above <= s.stain + Noise(tables, x % 32, (x * 2) % 32) * 2f
                           && Noise(tables, (rowTop * 3 + 5) % 32, (x + 9) % 32) > s.clean;
            if (!covered && !stained)
                return c;

            float light = Luminance(c);
            int i;
            if (s.simple)
                i = rim ? 4 : light > source.hi ? 3 : light > source.lo ? 2 : 1;
            else
            {
                int size = tables.texSize;
                int textureRowTop = (rowTop + s.t / 3) % size;
                int cell = (size - 1 - textureRowTop) * size + x % size;
                i = tables.texIndex[cell];

                // Barro seco (Eryops): por manchas, donde el ruido queda por debajo de lo seco; el borde no se seca.
                if (covered && !rim && s.dry > 0f && tables.dryIndex != null && Noise(tables, (rowTop * 2 + 3) % 32, (x * 3 + 7) % 32) < s.dry)
                {
                    int d = tables.dryIndex[cell];
                    d = Mathf.Clamp(d + (light < source.lo ? -1 : light > source.hi ? 1 : 0), 0, RampDry.Length - 1);
                    return RampDry[d];
                }

                if (i != 5)
                    i = Mathf.Clamp(i + (light < source.lo ? -1 : light > source.hi ? 1 : 0), 0, 4);
                if (rim)
                    i = (x + s.t) % 3 != 0 ? 4 : 5;
                if (stained && i == 5)
                    i = 3;
            }
            return Ramp[i];
        }

        // El ruido se pide como (fila contada desde arriba, columna), como en el generador.
        static float Noise(Tables tables, int rowTop, int column)
        {
            int size = tables.noiseSize;
            return tables.noise[(size - 1 - rowTop % size) * size + column % size].r / 255f;
        }

        static bool IsOutline(Color32 c) => c.r == Outline.r && c.g == Outline.g && c.b == Outline.b;

        static bool IsEye(Color32 c) => c.r == EyeWhite.r && c.g == EyeWhite.g && c.b == EyeWhite.b;

        static float Luminance(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}

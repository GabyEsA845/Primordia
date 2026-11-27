using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Puntos de un sprite que caen sobre su dibujo (no sobre el hueco transparente de su rectángulo), en unidades locales
    /// respecto al pivote y sin voltear. Los usa <see cref="BurningMarker"/> para pegar las llamas al cuerpo de los jefes.
    /// Las texturas del juego no son legibles, así que cada una se copia una sola vez por la tarjeta gráfica y se guarda su alfa.
    /// </summary>
    public static class SpriteBodyPoints
    {
        const byte Opaque = 128;
        // Cuántos puntos se guardan como mucho por sprite y a cuántos píxeles del borde queda un punto «interior».
        const int TargetPoints = 300;
        const int Margin = 2;

        class Alpha
        {
            public int width;
            public int height;
            public byte[] a;
        }

        static readonly Dictionary<Texture2D, Alpha> textures = new();
        static readonly Dictionary<Sprite, Vector2[]> sprites = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            textures.Clear();
            sprites.Clear();
        }

        /// <summary>Los puntos del dibujo; vacío si el sprite no tiene nada opaco o no se puede leer.</summary>
        public static Vector2[] For(Sprite sprite)
        {
            if (sprite == null)
                return System.Array.Empty<Vector2>();
            if (!sprites.TryGetValue(sprite, out var points))
                sprites[sprite] = points = Find(sprite);
            return points;
        }

        static Vector2[] Find(Sprite sprite)
        {
            // Un sprite empaquetado con malla ajustada no tiene rectángulo en la textura.
            if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
                return System.Array.Empty<Vector2>();
            var alpha = Read(sprite.texture);
            if (alpha == null)
                return System.Array.Empty<Vector2>();

            Rect rect = sprite.textureRect;
            Vector2 offset = sprite.textureRectOffset;
            int x0 = Mathf.RoundToInt(rect.x), y0 = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width), height = Mathf.RoundToInt(rect.height);
            int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(width * height / (float)TargetPoints)));

            bool Drawn(int x, int y) =>
                x >= 0 && y >= 0 && x < width && y < height && x0 + x < alpha.width && y0 + y < alpha.height
                && alpha.a[(y0 + y) * alpha.width + x0 + x] >= Opaque;

            var inner = new List<Vector2>();
            var all = new List<Vector2>();
            for (int y = step / 2; y < height; y += step)
            {
                for (int x = step / 2; x < width; x += step)
                {
                    if (!Drawn(x, y))
                        continue;
                    var local = (offset + new Vector2(x + 0.5f, y + 0.5f) - sprite.pivot) / sprite.pixelsPerUnit;
                    all.Add(local);
                    if (Drawn(x - Margin, y) && Drawn(x + Margin, y) && Drawn(x, y - Margin) && Drawn(x, y + Margin))
                        inner.Add(local);
                }
            }
            // En dibujos muy finos (alas, patas) casi no hay interior: vale cualquier píxel dibujado.
            return (inner.Count >= 4 ? inner : all).ToArray();
        }

        static Alpha Read(Texture2D texture)
        {
            if (texture == null)
                return null;
            if (textures.TryGetValue(texture, out var cached))
                return cached;

            Alpha result = null;
            if (texture.isReadable)
                result = Copy(texture);
            else if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                Graphics.Blit(texture, target);
                RenderTexture.active = target;
                var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                result = Copy(copy);
                Object.Destroy(copy);
            }
            textures[texture] = result;
            return result;
        }

        static Alpha Copy(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var a = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                a[i] = pixels[i].a;
            return new Alpha { width = texture.width, height = texture.height, a = a };
        }
    }
}

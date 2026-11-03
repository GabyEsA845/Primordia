using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Marcador del salto cargado de las Patas de salto: la diana de huellas (un anillo punteado que gira con una huella de rana, que late
    /// en amarillo con la carga al máximo) y cuatro esquinas tenues alrededor con el radio del impacto. Sin arte, se dibujan unos anillos
    /// por código. También crea la sombra del salto.
    /// </summary>
    public class JumpMarker : MonoBehaviour
    {
        const int Order = -4;
        const float SpotSeconds = 0.11f;
        // Las esquinas se colocan a esta fracción del radio en cada eje: así su distancia al centro es el radio del impacto.
        const float CornerAxis = 0.7f;

        static Sprite smallRing;
        static Sprite bigRing;
        static Sprite shadowSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            smallRing = null;
            bigRing = null;
            shadowSprite = null;
        }

        SpriteRenderer spot;
        SpriteFramePlayer spotPlayer;
        Sprite[] spotFrames, fullFrames;
        bool full;

        /// <summary>True con la carga al máximo (la diana late en amarillo).</summary>
        public bool IsFull => full;

        public static JumpMarker Create(float impactRadius, Sprite[] spotFrames = null, Sprite[] fullFrames = null, Sprite[] cornerFrames = null)
        {
            var obj = new GameObject("JumpMarker");
            var marker = obj.AddComponent<JumpMarker>();
            marker.spotFrames = spotFrames;
            marker.fullFrames = fullFrames;
            if (spotFrames != null && spotFrames.Length > 0)
            {
                marker.spot = Add(obj.transform, "Spot", spotFrames[0], Color.white);
                marker.spotPlayer = marker.spot.gameObject.AddComponent<SpriteFramePlayer>();
                marker.spotPlayer.Play(marker.spot, spotFrames, SpotSeconds, newLoop: true);
            }
            else
                marker.spot = Add(obj.transform, "Spot", Ring(ref smallRing, 32, 3, 0.9f), new Color(0.75f, 1f, 0.45f, 0.95f));

            if (cornerFrames != null && cornerFrames.Length >= 4)
            {
                float reach = impactRadius * CornerAxis;
                // Esquinas: arriba-izquierda, arriba-derecha, abajo-derecha, abajo-izquierda (el orden de la hoja).
                var corners = new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
                for (int i = 0; i < 4; i++)
                    Add(obj.transform, $"Corner{i}", cornerFrames[i], Color.white).transform.localPosition = corners[i] * reach;
            }
            else
                Add(obj.transform, "Impact", Ring(ref bigRing, 128, 2, impactRadius * 2f), new Color(0.75f, 1f, 0.45f, 0.35f));
            return marker;
        }

        /// <summary>Con la carga al máximo la diana cambia a la versión que late en amarillo.</summary>
        public void SetFull(bool value)
        {
            if (full == value)
                return;
            full = value;
            var frames = value ? fullFrames : spotFrames;
            if (spotPlayer != null && frames != null && frames.Length > 0)
                spotPlayer.Play(spot, frames, SpotSeconds, newLoop: true);
            else if (spotPlayer == null && spot != null)
                spot.color = value ? new Color(1f, 0.9f, 0.5f, 1f) : new Color(0.75f, 1f, 0.45f, 0.95f);
        }

        /// <summary>La sombra del salto: un óvalo oscuro pegado al suelo, hijo de <paramref name="parent"/> (el código la coloca en el suelo).</summary>
        public static SpriteRenderer CreateShadow(Transform parent)
        {
            if (shadowSprite == null)
                shadowSprite = OvalSprite(24, 8, 0.38f);
            var obj = new GameObject("JumpShadow");
            obj.transform.SetParent(parent, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = shadowSprite;
            renderer.sortingOrder = Order - 1;
            return renderer;
        }

        static SpriteRenderer Add(Transform parent, string name, Sprite sprite, Color color)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = Order;
            return renderer;
        }

        static Sprite OvalSprite(int width, int height, float alpha)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + .5f - width / 2f) / (width / 2f), dy = (y + .5f - height / 2f) / (height / 2f);
                    pixels[y * width + x] = dx * dx + dy * dy <= 1f ? new Color32(20, 16, 24, (byte)(alpha * 255)) : new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
        }

        /// <summary>Un anillo de ese grosor en píxeles, dibujado para que mida ese diámetro en unidades.</summary>
        static Sprite Ring(ref Sprite cache, int size, int thickness, float diameter)
        {
            if (cache != null && Mathf.Abs(cache.bounds.size.x - diameter) < 0.01f)
                return cache;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            float center = (size - 1) / 2f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    bool onRing = distance <= size / 2f && distance >= size / 2f - thickness;
                    pixels[y * size + x] = onRing ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            cache = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / diameter);
            return cache;
        }
    }
}

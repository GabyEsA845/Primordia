using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Marcador del salto cargado de las Patas de salto: un anillo pequeño en el sitio donde caerás y otro tenue con el radio del
    /// impacto. Provisional: los anillos se dibujan por código hasta que haya arte propio.
    /// </summary>
    public class JumpMarker : MonoBehaviour
    {
        const int Order = -4;

        static Sprite smallRing;
        static Sprite bigRing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            smallRing = null;
            bigRing = null;
        }

        public static JumpMarker Create(float impactRadius)
        {
            var obj = new GameObject("JumpMarker");
            var marker = obj.AddComponent<JumpMarker>();
            Add(obj.transform, "Spot", Ring(ref smallRing, 32, 3, 0.9f), new Color(0.75f, 1f, 0.45f, 0.95f));
            Add(obj.transform, "Impact", Ring(ref bigRing, 128, 2, impactRadius * 2f), new Color(0.75f, 1f, 0.45f, 0.35f));
            return marker;
        }

        static void Add(Transform parent, string name, Sprite sprite, Color color)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = Order;
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

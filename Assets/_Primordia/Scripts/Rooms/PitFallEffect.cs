using System.Collections;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Caída a una fosa hecha por código, igual para cualquier cuerpo: encoge, se oscurece hacia el fondo y suelta unas
    /// burbujas que suben. No hace falta una animación por criatura.
    /// </summary>
    public static class PitFallEffect
    {
        public const float Duration = 0.45f;
        const float EndScale = 0.15f;
        static readonly Color Dark = new(0.08f, 0.1f, 0.16f, 1f);

        // Burbujas: unas pocas, pequeñas, que suben y se desvanecen.
        const int Bubbles = 4;
        const float BubbleRise = 0.9f;
        const float BubbleLife = 0.6f;

        static Sprite bubbleSprite;

        /// <summary>Encoge y oscurece <paramref name="root"/> y todos sus sprites. Devuelve cómo dejarlo como estaba.</summary>
        public static IEnumerator Play(Transform root, System.Action<System.Action> onRestore = null)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>();
            var colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                colors[i] = renderers[i].color;
            var scale = root.localScale;

            onRestore?.Invoke(() =>
            {
                root.localScale = scale;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null)
                        renderers[i].color = colors[i];
            });

            SpawnBubbles(root.position);
            for (float t = 0f; t < Duration; t += Time.deltaTime)
            {
                float k = t / Duration;
                root.localScale = scale * Mathf.Lerp(1f, EndScale, k * k);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                        continue;
                    var dark = Color.Lerp(colors[i], Dark, k);
                    dark.a = colors[i].a * Mathf.Lerp(1f, 0.4f, k);
                    renderers[i].color = dark;
                }
                yield return null;
            }
            root.localScale = scale * EndScale;
        }

        static void SpawnBubbles(Vector2 position)
        {
            if (bubbleSprite == null)
                bubbleSprite = CreateBubbleSprite();
            for (int i = 0; i < Bubbles; i++)
            {
                var bubble = new GameObject("PitBubble");
                bubble.transform.position = position + Random.insideUnitCircle * 0.35f;
                var renderer = bubble.AddComponent<SpriteRenderer>();
                renderer.sprite = bubbleSprite;
                renderer.sortingOrder = 5;
                renderer.color = new Color(0.8f, 0.95f, 1f, 0.8f);
                bubble.AddComponent<PitBubble>().Configure(BubbleRise * Random.Range(0.7f, 1.2f), BubbleLife * Random.Range(0.8f, 1.2f), i * 0.08f);
            }
        }

        // Círculo hueco de 4 px a 16 px por unidad.
        static Sprite CreateBubbleSprite()
        {
            var texture = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    bool edge = (x == 0 || x == 3) != (y == 0 || y == 3);
                    texture.SetPixel(x, y, edge ? Color.white : Color.clear);
                }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}

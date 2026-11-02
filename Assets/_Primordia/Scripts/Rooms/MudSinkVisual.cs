using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Lo que se ve de alguien que se hunde en el lodo: el lodo le tapa de abajo hacia arriba (una máscara de sprites oculta
    /// la parte inferior de todos sus sprites) y, mientras está hundido, suben burbujas verdosas. Igual para el jugador y para
    /// los enemigos, sin una animación por criatura. Se añade solo al llamar a <see cref="SetLevel"/> y se limpia solo al volver
    /// a 0.
    /// </summary>
    public class MudSinkVisual : MonoBehaviour
    {
        // Cuánto del alto del cuerpo tapa el lodo con el hundimiento al máximo.
        const float MaxCover = 0.85f;
        const float RefreshSeconds = 0.2f;
        const float BubbleEvery = 0.3f;
        static readonly Color BubbleTint = new(0.62f, 0.7f, 0.38f, 0.85f);

        static Sprite whiteSprite;

        readonly List<SpriteRenderer> renderers = new();
        SpriteMask mask;
        float level;
        float nextRefresh;
        float nextBubble;

        /// <summary>Hundimiento visible, de 0 (nada) a 1 (tapado del todo).</summary>
        public float Level => level;

        /// <summary>El hundimiento de <paramref name="root"/> (se crea el componente si hace falta). Con 0 lo deja como estaba.</summary>
        public static void SetLevel(Transform root, float level)
        {
            var visual = root.GetComponent<MudSinkVisual>();
            if (visual == null)
            {
                if (level <= 0f)
                    return;
                visual = root.gameObject.AddComponent<MudSinkVisual>();
            }
            visual.Apply(Mathf.Clamp01(level));
        }

        /// <summary>
        /// Hunde a <paramref name="root"/> en <paramref name="seconds"/> s (de 0 a 1) y lo deja hundido; quien llama decide qué
        /// pasa después (un enemigo muere). El sonido y la muerte los pone quien lo llama.
        /// </summary>
        public static IEnumerator SinkAway(Transform root, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (root == null)
                    yield break;
                SetLevel(root, t / seconds);
                yield return null;
            }
            if (root != null)
                SetLevel(root, 1f);
        }

        void Apply(float newLevel)
        {
            level = newLevel;
            if (level <= 0f)
            {
                Release();
                Destroy(this);
                return;
            }
            if (mask == null)
                CreateMask();
            if (Time.time >= nextRefresh)
            {
                Refresh();
                nextRefresh = Time.time + RefreshSeconds;
            }
            Place();
        }

        void CreateMask()
        {
            if (whiteSprite == null)
            {
                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            // Fuera del cuerpo: si fuera hijo heredaría su escala y su volteo.
            var obj = new GameObject("MudSinkMask");
            mask = obj.AddComponent<SpriteMask>();
            mask.sprite = whiteSprite;
        }

        // Los sprites del cuerpo pueden cambiar (mutaciones, efectos): se recogen otra vez cada poco.
        void Refresh()
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.maskInteraction = SpriteMaskInteraction.None;
            }
            renderers.Clear();
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                renderers.Add(renderer);
            }
        }

        void Place()
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null)
                    continue;
                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }
            if (!any)
                return;

            // De un poco por debajo de los pies hasta la altura del hundimiento: lo de dentro de la máscara no se ve.
            float bottom = bounds.min.y - 0.5f;
            float top = bounds.min.y + level * MaxCover * bounds.size.y;
            mask.transform.position = new Vector3(bounds.center.x, (bottom + top) / 2f, 0f);
            mask.transform.localScale = new Vector3(bounds.size.x + 1f, top - bottom, 1f);

            if (level > 0.15f && Time.time >= nextBubble)
            {
                nextBubble = Time.time + BubbleEvery;
                PitFallEffect.SpawnBubbles(new Vector2(bounds.center.x, top), 1, BubbleTint);
            }
        }

        void Release()
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.maskInteraction = SpriteMaskInteraction.None;
            }
            renderers.Clear();
            if (mask != null)
                Destroy(mask.gameObject);
            mask = null;
        }

        void OnDisable() => Release();

        void OnDestroy() => Release();
    }
}

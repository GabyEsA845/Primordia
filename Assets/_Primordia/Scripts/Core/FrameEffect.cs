using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Efecto de un solo uso hecho con unos cuadros de sprite, sin Animator: los reproduce una vez y se destruye. Lo usan
    /// las mutaciones especiales de las almejas (destello de listo de los Flagelos, reflejo y aturdimiento de la Cutícula).
    /// </summary>
    public class FrameEffect : MonoBehaviour
    {
        Sprite[] frames;
        float frameDuration;
        SpriteRenderer spriteRenderer;
        float elapsed;

        /// <summary>
        /// Crea el efecto colgando de <paramref name="parent"/> (lo sigue) o, sin padre, en esa posición. Sin cuadros no crea
        /// nada y devuelve null.
        /// </summary>
        public static FrameEffect Play(string name, Sprite[] frames, float frameDuration, int sortingOrder, Transform parent,
            Vector2 position = default)
        {
            if (frames == null || frames.Length == 0)
                return null;

            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go.transform.position = position;
            }

            var effect = go.AddComponent<FrameEffect>();
            effect.frames = frames;
            effect.frameDuration = Mathf.Max(0.01f, frameDuration);
            effect.spriteRenderer = go.AddComponent<SpriteRenderer>();
            effect.spriteRenderer.sortingOrder = sortingOrder;
            effect.spriteRenderer.sprite = frames[0];
            return effect;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(elapsed / frameDuration);
            if (frame >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            spriteRenderer.sprite = frames[frame];
        }
    }
}

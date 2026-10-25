using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Efecto de un solo uso hecho con unos cuadros de sprite, sin Animator: los reproduce una vez y se destruye. Lo usan
    /// las mutaciones especiales de las almejas (destello de listo de los Flagelos, reflejo y aturdimiento de la Cutícula) y,
    /// como prefab, el destello de la boca de los Cnidocitos.
    /// </summary>
    public class FrameEffect : MonoBehaviour
    {
        [Tooltip("Cuadros, en orden; se reproducen una vez.")]
        [SerializeField] Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.06f;
        SpriteRenderer spriteRenderer;
        float elapsed;

        public float Duration => frames.Length * frameDuration;

        /// <summary>Para prefabs (el destello de la boca de los Cnidocitos): cuadros y duración de cada uno.</summary>
        internal void Configure(Sprite[] newFrames, float newFrameDuration)
        {
            frames = newFrames ?? System.Array.Empty<Sprite>();
            frameDuration = Mathf.Max(0.01f, newFrameDuration);
        }

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

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
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }
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

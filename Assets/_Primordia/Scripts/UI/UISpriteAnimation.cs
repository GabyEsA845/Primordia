using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Anima una imagen de la interfaz cuadro a cuadro, en tiempo real (sigue en la pausa).
    /// En bucle puede esperar entre pasadas (el brillo del logo); sin bucle puede destruirse al acabar (burbujas).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UISpriteAnimation : MonoBehaviour
    {
        [SerializeField] Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.1f;
        [SerializeField] bool loop = true;
        [Tooltip("Solo en bucle: segundos sin nada entre una pasada y la siguiente.")]
        [SerializeField, Min(0f)] float loopDelay;
        [Tooltip("Sin bucle: destruye el objeto al acabar.")]
        [SerializeField] bool destroyWhenDone;

        Image image;
        float startTime;

        public bool IsPlaying { get; private set; }
        public int CurrentFrame { get; private set; } = -1;

        void Awake() => image = GetComponent<Image>();

        void OnEnable() => Restart();

        internal void Configure(Sprite[] newFrames, float newFrameDuration, bool newLoop, float newLoopDelay = 0f, bool newDestroyWhenDone = false)
        {
            frames = newFrames ?? System.Array.Empty<Sprite>();
            frameDuration = Mathf.Max(0.01f, newFrameDuration);
            loop = newLoop;
            loopDelay = Mathf.Max(0f, newLoopDelay);
            destroyWhenDone = newDestroyWhenDone;
            if (image == null)
                image = GetComponent<Image>();
            Restart();
        }

        public void Restart()
        {
            startTime = Time.unscaledTime;
            IsPlaying = frames.Length > 0;
            CurrentFrame = -1;
            Show(IsPlaying ? 0 : -1);
        }

        void Update()
        {
            if (!IsPlaying)
                return;

            float length = frames.Length * frameDuration;
            float elapsed = Time.unscaledTime - startTime;

            if (loop)
            {
                // Cada ciclo: la pasada y luego la espera, con la imagen oculta.
                float t = elapsed % (length + loopDelay);
                Show(t < length ? Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t / frameDuration)) : -1);
                return;
            }

            if (elapsed < length)
            {
                Show(Mathf.FloorToInt(elapsed / frameDuration));
                return;
            }

            IsPlaying = false;
            if (destroyWhenDone)
                Destroy(gameObject);
        }

        void Show(int frame)
        {
            if (frame == CurrentFrame || image == null)
                return;

            CurrentFrame = frame;
            image.enabled = frame >= 0;
            if (frame >= 0)
                image.sprite = frames[frame];
        }
    }
}

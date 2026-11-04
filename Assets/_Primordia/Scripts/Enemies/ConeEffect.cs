using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Dibujo del cono de fuego del escarabajo en tres fases: salida (la llama crece una vez), bucle (mientras dura el chorro) y final
    /// (se recoge en una nube que se deshace en humo, y el efecto se destruye). <see cref="Finish"/> pasa del bucle (o de la salida)
    /// al final; el brazo del cono lo llama al acabar el chorro.
    /// </summary>
    public class ConeEffect : MonoBehaviour
    {
        public enum Stage { Start, Loop, End }

        [SerializeField] Sprite[] start = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] loop = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] end = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameSeconds = 0.06f;
        [SerializeField, Min(0.01f)] float endFrameSeconds = 0.07f;

        SpriteRenderer body;
        float stageStart;

        public Stage Current { get; private set; } = Stage.Start;

        public void Configure(Sprite[] startFrames, Sprite[] loopFrames, Sprite[] endFrames, float frame = 0.06f, float endFrame = 0.07f)
        {
            start = startFrames;
            loop = loopFrames;
            end = endFrames;
            frameSeconds = frame;
            endFrameSeconds = endFrame;
        }

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            stageStart = Time.time;
            Show();
        }

        /// <summary>Pasa a la fase final: el cono se recoge y desaparece. No hace nada si ya está en ella.</summary>
        public void Finish()
        {
            if (Current == Stage.End)
                return;
            Current = Stage.End;
            stageStart = Time.time;
            Show();
        }

        void Update()
        {
            float elapsed = Time.time - stageStart;
            if (Current == Stage.Start && elapsed >= start.Length * frameSeconds)
            {
                Current = Stage.Loop;
                stageStart = Time.time;
            }
            else if (Current == Stage.End && elapsed >= end.Length * endFrameSeconds)
            {
                Destroy(gameObject);
                return;
            }
            Show();
        }

        void Show()
        {
            if (body == null)
                return;
            var frames = Current switch { Stage.Start => start, Stage.End => end, _ => loop };
            if (frames == null || frames.Length == 0)
                return;
            float seconds = Current == Stage.End ? endFrameSeconds : frameSeconds;
            int frame = Mathf.FloorToInt((Time.time - stageStart) / seconds);
            frame = Current == Stage.Loop ? frame % frames.Length : Mathf.Clamp(frame, 0, frames.Length - 1);
            body.sprite = frames[frame];
        }
    }
}

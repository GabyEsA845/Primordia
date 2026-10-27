using System.Collections;
using Primordia.Audio;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Entrada de oleadas: una grieta del suelo que, al llegar una oleada, se abre, brilla en rojo y burbujea mientras salen
    /// los enemigos (<see cref="Room"/> la activa), y luego vuelve a reposo.
    /// </summary>
    public class WaveVent : MonoBehaviour
    {
        const float IdleFrameDuration = 0.2f;
        const float ActiveFrameDuration = 0.1f;

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] active = System.Array.Empty<Sprite>();

        public bool IsActive { get; private set; }

        /// <summary>Lo que dura abrirse antes de que salgan los enemigos.</summary>
        public float OpenDuration => Mathf.Max(0.3f, active.Length * ActiveFrameDuration);

        internal void Configure(SpriteRenderer newBody, Sprite[] newIdle, Sprite[] newActive)
        {
            body = newBody;
            idle = newIdle ?? System.Array.Empty<Sprite>();
            active = newActive ?? System.Array.Empty<Sprite>();
        }

        void Update()
        {
            if (IsActive || body == null || idle.Length == 0)
                return;
            body.sprite = idle[Mathf.FloorToInt(Time.time / IdleFrameDuration) % idle.Length];
        }

        /// <summary>Se abre y brilla mientras sale una oleada.</summary>
        public void Activate()
        {
            if (!isActiveAndEnabled)
                return;
            StartCoroutine(Play());
            // Varias grietas a la vez suenan una sola vez (minInterval del sonido).
            Sfx.Play(Sounds.WaveVent);
        }

        IEnumerator Play()
        {
            IsActive = true;
            // Dos vueltas a la animación: se abre y burbujea mientras salen.
            for (int loop = 0; loop < 2; loop++)
            {
                foreach (var frame in active)
                {
                    if (body != null)
                        body.sprite = frame;
                    yield return new WaitForSeconds(ActiveFrameDuration);
                }
            }
            IsActive = false;
        }
    }
}

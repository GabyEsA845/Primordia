using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>
    /// Planta luminosa de la sala del Anomalocaris (marca L): sus bulbos laten y dan casi toda la luz de la sala. Se puede
    /// apagar y volver a encender (escena de los ojos del jefe).
    /// </summary>
    public class LightPlant : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] SpriteLoop loop;
        [Tooltip("De encendida a apagada; al revés, se enciende.")]
        [SerializeField] Sprite[] offFrames;
        [SerializeField] Light2D glow;
        [SerializeField] GlowPulse pulse;

        Coroutine fading;
        Coroutine dimming;

        // Brillo con la planta atenuada (la silueta del Anomalocaris ronda la sala).
        const float DimFactor = 0.35f;

        /// <summary>False desde que empieza a apagarse hasta que empieza a encenderse.</summary>
        public bool Lit { get; private set; } = true;

        public Light2D Glow => glow;

        internal void Configure(SpriteRenderer newTarget, SpriteLoop newLoop, Sprite[] newOffFrames, Light2D newGlow, GlowPulse newPulse)
        {
            target = newTarget;
            loop = newLoop;
            offFrames = newOffFrames;
            glow = newGlow;
            pulse = newPulse;
        }

        /// <summary>Se apaga o se enciende en <paramref name="duration"/> segundos (0: al momento).</summary>
        public void SetLit(bool lit, float duration)
        {
            if (lit == Lit)
                return;
            Lit = lit;
            if (fading != null)
                StopCoroutine(fading);
            if (!isActiveAndEnabled || duration <= 0f)
            {
                fading = null;
                Apply(lit ? 1f : 0f);
                return;
            }
            fading = StartCoroutine(Fade(lit, duration));
        }

        /// <summary>Baja (true) o recupera su brillo en <paramref name="duration"/> segundos, sin dejar de latir.</summary>
        public void SetDimmed(bool dimmed, float duration)
        {
            if (pulse == null)
                return;
            if (dimming != null)
                StopCoroutine(dimming);
            float goal = dimmed ? DimFactor : 1f;
            if (!isActiveAndEnabled || duration <= 0f)
            {
                dimming = null;
                pulse.Multiplier = goal;
                return;
            }
            dimming = StartCoroutine(Dim(goal, duration));
        }

        /// <summary>1 con su brillo normal; menos, atenuada.</summary>
        public float Brightness => pulse != null ? pulse.Multiplier : 1f;

        IEnumerator Dim(float goal, float duration)
        {
            float start = pulse.Multiplier;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                pulse.Multiplier = Mathf.Lerp(start, goal, t / duration);
                yield return null;
            }
            pulse.Multiplier = goal;
            dimming = null;
        }

        IEnumerator Fade(bool lit, float duration)
        {
            float from = lit ? 0f : 1f, to = lit ? 1f : 0f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Apply(Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            Apply(to);
            fading = null;
        }

        // 1 encendida (latiendo), 0 apagada; en medio, los cuadros de apagarse y la luz a medias.
        void Apply(float amount)
        {
            bool full = amount >= 1f;
            if (loop != null)
                loop.enabled = full;
            if (pulse != null)
                pulse.enabled = full;
            if (!full && target != null && offFrames != null && offFrames.Length > 0)
            {
                int frame = Mathf.Clamp(Mathf.FloorToInt((1f - amount) * offFrames.Length), 0, offFrames.Length - 1);
                target.sprite = offFrames[frame];
            }
            if (glow != null && !full)
                glow.intensity = (pulse != null ? pulse.Intensity.y : 1f) * amount;
        }
    }
}

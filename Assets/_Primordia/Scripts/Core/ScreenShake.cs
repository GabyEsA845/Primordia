using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Primordia.Core
{
    /// <summary>
    /// Temblor de pantalla para todo el juego: golpes que se apagan solos (<see cref="Pulse"/>) y temblores continuos con
    /// dueño (<see cref="SetRumble"/>, p. ej. el derrumbe de la cueva del Euriptérido). Mueve la cámara principal a píxeles
    /// enteros solo mientras se dibuja el cuadro (Cinemachine no lo nota) y nunca en pausa. Se mueve al empezar el dibujado
    /// del cuadro, antes que cada cámara: la Pixel Perfect Camera de URP fija su matriz de vista al empezar a dibujar la
    /// cámara, con la posición que tenga entonces. El jugador elige en la pausa cuánto:
    /// normal, suave (la mitad y más lento) o apagado; se guarda en PlayerPrefs.
    /// </summary>
    public static class ScreenShake
    {
        public enum Level { Normal, Reduced, Off }

        const string Key = "Primordia.ScreenShake";
        const float PixelsPerUnit = 16f;
        // Cada cuánto cambia la posición (segundos): más lento en suave, que marea menos.
        const float NormalInterval = 0.04f;
        const float ReducedInterval = 0.08f;

        static readonly Dictionary<object, float> rumbles = new();
        static float pulseStrength;
        static float pulseStart;
        static float pulseDuration;
        static float nextChange;
        static Vector2 offset;
        static Camera shaken;
        static Vector3 restore;
        static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            rumbles.Clear();
            pulseStrength = pulseDuration = pulseStart = nextChange = 0f;
            offset = Vector2.zero;
            shaken = null;
            if (hooked)
            {
                RenderPipelineManager.beginContextRendering -= OnBeginFrame;
                RenderPipelineManager.endContextRendering -= OnEndFrame;
                hooked = false;
            }
        }

        public static Level Setting
        {
            get => (Level)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)Level.Normal), 0, 2);
            set
            {
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static float Multiplier => Setting switch
        {
            Level.Normal => 1f,
            Level.Reduced => 0.5f,
            _ => 0f
        };

        /// <summary>Golpe de <paramref name="pixels"/> que se apaga en <paramref name="duration"/> s (si ya hay uno más fuerte, gana ese).</summary>
        public static void Pulse(float pixels, float duration)
        {
            Hook();
            if (PulseNow >= pixels)
                return;
            pulseStrength = pixels;
            pulseStart = Time.time;
            pulseDuration = Mathf.Max(0.01f, duration);
        }

        /// <summary>Temblor continuo de <paramref name="pixels"/> mientras su dueño no lo quite. Con 0, lo quita.</summary>
        public static void SetRumble(object owner, float pixels)
        {
            if (owner == null)
                return;
            if (pixels <= 0f)
            {
                rumbles.Remove(owner);
                return;
            }
            Hook();
            rumbles[owner] = pixels;
        }

        public static void ClearRumble(object owner)
        {
            if (owner != null)
                rumbles.Remove(owner);
        }

        public static float RumbleOf(object owner) => owner != null && rumbles.TryGetValue(owner, out var pixels) ? pixels : 0f;

        static float PulseNow
        {
            get
            {
                if (pulseStrength <= 0f)
                    return 0f;
                float t = (Time.time - pulseStart) / pulseDuration;
                return t >= 1f ? 0f : pulseStrength * (1f - t);
            }
        }

        /// <summary>Fuerza actual en píxeles, con el ajuste del jugador (0 en pausa o apagado).</summary>
        public static float Amplitude
        {
            get
            {
                if (Time.timeScale <= 0f)
                    return 0f;
                float strongest = PulseNow;
                foreach (var pixels in rumbles.Values)
                    strongest = Mathf.Max(strongest, pixels);
                return strongest * Multiplier;
            }
        }

        /// <summary>Desplazamiento de la cámara en este momento (unidades, múltiplo de un píxel).</summary>
        public static Vector2 Offset
        {
            get
            {
                float amplitude = Amplitude;
                if (amplitude <= 0.01f)
                    return offset = Vector2.zero;
                if (Time.unscaledTime >= nextChange)
                {
                    nextChange = Time.unscaledTime + (Setting == Level.Reduced ? ReducedInterval : NormalInterval);
                    // Al menos 1 px con cualquier fuerza: en pixel art no hay medio píxel.
                    int pixels = Mathf.Max(1, Mathf.RoundToInt(amplitude));
                    offset = new Vector2(Random.Range(-pixels, pixels + 1), Random.Range(-pixels, pixels + 1)) / PixelsPerUnit;
                }
                return offset;
            }
        }

        static void Hook()
        {
            if (hooked)
                return;
            RenderPipelineManager.beginContextRendering += OnBeginFrame;
            RenderPipelineManager.endContextRendering += OnEndFrame;
            hooked = true;
        }

        static void OnBeginFrame(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (!Application.isPlaying)
                return;
            // Si el dibujado anterior no llegó a su final (no se devolvió la posición), se devuelve ahora: si no, el
            // temblor se quedaría bloqueado para siempre.
            if (shaken != null)
            {
                shaken.transform.position = restore;
                shaken = null;
            }
            Camera main = null;
            foreach (var camera in cameras)
            {
                if (camera != null && camera.CompareTag("MainCamera"))
                {
                    main = camera;
                    break;
                }
            }
            if (main == null)
                return;
            var shift = Offset;
            if (shift == Vector2.zero)
                return;
            shaken = main;
            restore = main.transform.position;
            main.transform.position = restore + (Vector3)shift;
        }

        static void OnEndFrame(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (shaken == null)
                return;
            shaken.transform.position = restore;
            shaken = null;
        }
    }
}

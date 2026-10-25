using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>Latido de una luz: su intensidad sube y baja entre dos valores.</summary>
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField] Light2D target;
        [SerializeField] Vector2 intensity = new(0.4f, 0.7f);
        [SerializeField, Min(0.05f)] float period = 1f;
        [SerializeField] float phase;

        internal void Configure(Light2D newTarget, Vector2 newIntensity, float newPeriod, float newPhase)
        {
            target = newTarget;
            intensity = newIntensity;
            period = Mathf.Max(0.05f, newPeriod);
            phase = newPhase;
        }

        void Update()
        {
            if (target == null)
                return;
            float t = (Mathf.Sin((Time.time + phase) / period * Mathf.PI * 2f) + 1f) / 2f;
            target.intensity = Mathf.Lerp(intensity.x, intensity.y, t);
        }
    }
}

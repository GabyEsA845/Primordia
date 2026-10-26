using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Destello de curación: el cuerpo del enemigo se tiñe de verde y vuelve poco a poco a su color (la Esponja Vauxia
    /// al curarlo). Solo tiñe el cuerpo ("Body"), no la sombra ni las marcas de estado.
    /// </summary>
    public class HealGlow : MonoBehaviour
    {
        static readonly Color Tint = new(0.45f, 1f, 0.55f);
        const float Duration = 0.5f;

        SpriteRenderer[] renderers;
        Color[] original;
        float start = float.NegativeInfinity;

        /// <summary>True mientras dura el destello.</summary>
        public bool IsGlowing => Time.time < start + Duration;

        /// <summary>Hace destellar en verde a ese enemigo.</summary>
        public static void Play(Component target)
        {
            if (target == null)
                return;
            if (!target.TryGetComponent<HealGlow>(out var glow))
                glow = target.gameObject.AddComponent<HealGlow>();
            glow.Begin();
        }

        void Begin()
        {
            // Los colores originales se guardan al empezar, no a mitad de otro destello.
            if (!IsGlowing)
            {
                var body = transform.Find("Body");
                renderers = (body != null ? body : transform).GetComponentsInChildren<SpriteRenderer>();
                original = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                    original[i] = renderers[i].color;
            }
            start = Time.time;
            Apply(0f);
        }

        void LateUpdate()
        {
            if (renderers == null)
                return;
            float t = (Time.time - start) / Duration;
            Apply(t);
            if (t >= 1f)
                renderers = null;
        }

        void OnDisable()
        {
            if (renderers != null)
                Apply(1f);
            renderers = null;
        }

        void Apply(float t)
        {
            // Sube rápido y se va despacio.
            float amount = 1f - Mathf.Clamp01(t);
            amount *= amount;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = Color.Lerp(original[i], original[i] * Tint, amount);
            }
        }
    }
}

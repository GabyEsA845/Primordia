using UnityEngine;

namespace Primordia.Core
{
    /// <summary>Efecto visual de un solo uso: crece un poco, se desvanece y se destruye.</summary>
    public class FadeOutEffect : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float duration = 0.5f;
        [Tooltip("Escala final respecto a la inicial. 1 = no crece.")]
        [SerializeField, Min(0f)] float endScale = 1.2f;

        SpriteRenderer[] renderers;
        Color[] startColors;
        Vector3 startScale;
        float elapsed;

        void Start()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                startColors[i] = renderers[i].color;
            startScale = transform.localScale;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.localScale = startScale * Mathf.Lerp(1f, endScale, t);
            for (int i = 0; i < renderers.Length; i++)
            {
                var color = startColors[i];
                color.a *= 1f - t;
                renderers[i].color = color;
            }

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}

using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Rótulo del HUD con el linaje activo ("Tiburón · Carnívoro"), del color de su clase. Se oculta sin linaje y
    /// da un saltito al completarse uno, como el contador de biomasa.
    /// </summary>
    public class LineageLabel : MonoBehaviour
    {
        [Tooltip("Linaje a mostrar. Si está vacío, usa el del objeto con la etiqueta Player.")]
        [SerializeField] PlayerLineage lineage;
        [SerializeField] Text label;
        [Tooltip("Colores por clase, en el orden Carnívoro, Herbívoro, Venenoso, Hongo, Carroñero.")]
        [SerializeField] Color[] classColors =
        {
            new(0.95f, 0.45f, 0.4f), new(0.55f, 0.9f, 0.45f), new(0.8f, 0.6f, 0.95f), new(0.95f, 0.75f, 0.4f), new(0.78f, 0.64f, 0.42f),
        };
        [Tooltip("Tamaño al empezar el salto; vuelve a 1 en popDuration segundos (tiempo real).")]
        [SerializeField, Min(1f)] float popScale = 1.4f;
        [SerializeField, Min(0.01f)] float popDuration = 0.3f;

        float popStart = -1f;

        public string Shown => label != null && label.enabled ? label.text : "";

        void Start()
        {
            if (lineage == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    lineage = player.GetComponent<PlayerLineage>();
            }

            if (lineage == null)
            {
                // Escenas sin linajes (pruebas antiguas): no se muestra.
                if (label != null)
                    label.enabled = false;
                return;
            }

            lineage.ActiveChanged += OnActiveChanged;
            Show(lineage.Active);
        }

        void OnDestroy()
        {
            if (lineage != null)
                lineage.ActiveChanged -= OnActiveChanged;
        }

        internal void Configure(PlayerLineage newLineage, Text newLabel)
        {
            lineage = newLineage;
            label = newLabel;
        }

        void OnActiveChanged(LineageData old, LineageData active)
        {
            Show(active);
            if (active != null)
                popStart = Time.unscaledTime;
        }

        void Show(LineageData active)
        {
            if (label == null)
                return;

            label.enabled = active != null;
            if (active == null)
                return;

            label.text = $"{active.DisplayName} · {MutationText.ClassName(active.Class)}";
            label.color = MutationText.ClassColor(active.Class, classColors);
        }

        void Update()
        {
            if (popStart < 0f || label == null)
                return;

            float t = (Time.unscaledTime - popStart) / popDuration;
            if (t >= 1f)
            {
                popStart = -1f;
                t = 1f;
            }
            label.rectTransform.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, t);
        }
    }
}

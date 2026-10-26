using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Cartel del HUD al completar un linaje: el animal en grande, del color de su clase, y debajo su beneficio
    /// ("Rematar · Sangre en el agua"). Da un saltito, se queda un momento y se desvanece. El tiempo se para con el
    /// juego, así no se pierde mientras sigue abierta la elección de mutación que lo ha completado. Con la
    /// evolución (<see cref="LineageEvolution"/>), sale cuando empieza su animación.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LineageBanner : MonoBehaviour
    {
        [Tooltip("Linaje a vigilar. Si está vacío, usa el del objeto con la etiqueta Player.")]
        [SerializeField] PlayerLineage lineage;
        [SerializeField] Text title;
        [SerializeField] Text subtitle;
        [Tooltip("Colores por clase, en el orden Carnívoro, Herbívoro, Venenoso, Hongo, Carroñero (los del rótulo del HUD).")]
        [SerializeField] Color[] classColors =
        {
            new(0.95f, 0.45f, 0.4f), new(0.55f, 0.9f, 0.45f), new(0.8f, 0.6f, 0.95f), new(0.95f, 0.75f, 0.4f), new(0.78f, 0.64f, 0.42f),
        };
        [Tooltip("Segundos que se ve entero y que tarda en desvanecerse.")]
        [SerializeField, Min(0f)] float holdTime = 2f;
        [SerializeField, Min(0.01f)] float fadeTime = 0.5f;
        [SerializeField, Min(1f)] float popScale = 1.5f;
        [SerializeField, Min(0.01f)] float popDuration = 0.25f;

        CanvasGroup group;
        LineageEvolution evolution;
        float shownFor = -1f;
        float popStart;

        public bool IsShowing => shownFor >= 0f;
        public string Title => title != null ? title.text : "";
        public string Subtitle => subtitle != null ? subtitle.text : "";

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void Start()
        {
            if (lineage == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    lineage = player.GetComponent<PlayerLineage>();
            }

            if (lineage == null)
                return;

            if (lineage.TryGetComponent(out evolution))
                evolution.Evolving += Show;
            else
                lineage.ActiveChanged += OnActiveChanged;
        }

        void OnDestroy()
        {
            if (evolution != null)
                evolution.Evolving -= Show;
            else if (lineage != null)
                lineage.ActiveChanged -= OnActiveChanged;
        }

        internal void Configure(PlayerLineage newLineage, Text newTitle, Text newSubtitle)
        {
            lineage = newLineage;
            title = newTitle;
            subtitle = newSubtitle;
        }

        void OnActiveChanged(LineageData old, LineageData active)
        {
            if (active != null)
                Show(active);
        }

        /// <summary>Muestra el cartel de ese linaje.</summary>
        public void Show(LineageData active)
        {
            if (title != null)
            {
                title.text = active.DisplayName.ToUpperInvariant();
                title.color = MutationText.ClassColor(active.Class, classColors);
            }
            if (subtitle != null)
                subtitle.text = MutationText.BenefitTitle(active);

            shownFor = 0f;
            popStart = Time.unscaledTime;
            group.alpha = 1f;
        }

        void Update()
        {
            if (shownFor < 0f)
                return;

            // El saltito, en tiempo real; lo que dura, con el tiempo del juego.
            float pop = Mathf.Clamp01((Time.unscaledTime - popStart) / popDuration);
            transform.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, pop);

            shownFor += Time.deltaTime;
            float fade = (shownFor - holdTime) / fadeTime;
            group.alpha = 1f - Mathf.Clamp01(fade);
            if (fade >= 1f)
                shownFor = -1f;
        }
    }
}

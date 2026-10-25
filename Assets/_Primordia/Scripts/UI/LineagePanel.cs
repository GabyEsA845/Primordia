using Primordia.Mutations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Fila del linaje en el panel de estadísticas de la pausa: el animal y su clase (del color de la clase) y debajo
    /// los nombres de su beneficio ("Rematar · Sangre en el agua"). Al pasar el ratón, el cuadro de información
    /// (<see cref="MutationTooltip"/>) explica el beneficio entero. Sin linaje completo dice "Ninguno". Si una mutación
    /// especial completa dos linajes a la vez, un clic cambia al otro.
    /// </summary>
    public class LineagePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("Linaje a mostrar. Si está vacío, usa el del objeto con la etiqueta Player.")]
        [SerializeField] PlayerLineage lineage;
        [SerializeField] Text nameLabel;
        [SerializeField] Text benefitLabel;
        [Tooltip("Cuadro de información de la pausa, el de las ranuras.")]
        [SerializeField] MutationTooltip tooltip;
        [Tooltip("Colores por clase, en el orden Carnívoro, Herbívoro, Venenoso, Hongo (los del rótulo del HUD).")]
        [SerializeField] Color[] classColors =
        {
            new(0.95f, 0.45f, 0.4f), new(0.55f, 0.9f, 0.45f), new(0.8f, 0.6f, 0.95f), new(0.95f, 0.75f, 0.4f),
        };
        [SerializeField] Color emptyColor = new(0.6f, 0.65f, 0.6f);

        public string ShownName => nameLabel != null ? nameLabel.text : "";
        public string ShownBenefit => benefitLabel != null ? benefitLabel.text : "";

        void OnEnable() => Refresh();

        void OnDisable()
        {
            if (tooltip != null)
                tooltip.Hide();
        }

        internal void Configure(PlayerLineage newLineage, Text newName, Text newBenefit, MutationTooltip newTooltip = null)
        {
            lineage = newLineage;
            nameLabel = newName;
            benefitLabel = newBenefit;
            tooltip = newTooltip;
        }

        LineageData Active
        {
            get
            {
                if (lineage == null)
                {
                    var player = GameObject.FindWithTag("Player");
                    if (player != null)
                        lineage = player.GetComponent<PlayerLineage>();
                }
                return lineage != null ? lineage.Active : null;
            }
        }

        Color ColorOf(LineageData active)
        {
            int index = active != null ? (int)active.Class : -1;
            return index >= 0 && classColors != null && index < classColors.Length ? classColors[index] : emptyColor;
        }

        public void Refresh()
        {
            var active = Active;
            if (nameLabel != null)
            {
                nameLabel.text = active != null ? $"{active.DisplayName} · {MutationText.ClassName(active.Class)}" : "Linaje: ninguno";
                nameLabel.color = ColorOf(active);
            }

            if (benefitLabel != null)
            {
                var other = Other;
                benefitLabel.text = MutationText.BenefitTitle(active) + (other != null ? $"\nClic: cambiar a {other.DisplayName}" : "");
                benefitLabel.gameObject.SetActive(active != null);
            }
        }

        /// <summary>El otro linaje completo cuando el comodín completa dos (null si no).</summary>
        LineageData Other
        {
            get
            {
                if (lineage == null || lineage.Candidates.Count < 2)
                    return null;
                foreach (var candidate in lineage.Candidates)
                {
                    if (candidate != lineage.Active)
                        return candidate;
                }
                return null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var other = Active != null ? Other : null;
            if (other == null)
                return;
            lineage.Choose(other);
            Refresh();
            OnPointerEnter(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip == null)
                return;

            var active = Active;
            if (active != null)
                tooltip.Show(active.DisplayName, $"Linaje {MutationText.ClassName(active.Class).ToLowerInvariant()}", Describe(active),
                    ColorOf(active), (RectTransform)transform);
            else
                tooltip.Show("Sin linaje", "", "Lleva las 4 mutaciones de un animal para completar su linaje.", emptyColor, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.Hide();
        }

        /// <summary>"Rematar: … \n Sangre en el agua: …".</summary>
        public static string Describe(LineageData active)
        {
            var (name, description) = MutationText.ClassBenefit(active.Class);
            string text = $"{name}: {description}";
            if (!string.IsNullOrEmpty(active.TouchName))
                text += $"\n{active.TouchName}: {active.Benefit}";
            return text;
        }
    }
}

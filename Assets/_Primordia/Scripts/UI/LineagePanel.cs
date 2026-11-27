using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// El linaje en la pestaña Estado de la pausa: el animal y su clase (del color de la clase) y debajo lo que da.
    /// Sin linaje completo dice "ninguno". Si una mutación especial completa dos linajes a la vez, pulsarlo cambia
    /// al otro.
    /// </summary>
    public class LineagePanel : MonoBehaviour
    {
        [Tooltip("Linaje a mostrar. Si está vacío, usa el del objeto con la etiqueta Player.")]
        [SerializeField] PlayerLineage lineage;
        [SerializeField] Text nameLabel;
        [SerializeField] Text benefitLabel;
        [Tooltip("Colores por clase, en el orden Carnívoro, Herbívoro, Venenoso, Hongo, Carroñero (los del rótulo del HUD).")]
        [SerializeField] Color[] classColors =
        {
            new(0.95f, 0.45f, 0.4f), new(0.55f, 0.9f, 0.45f), new(0.8f, 0.6f, 0.95f), new(0.95f, 0.75f, 0.4f), new(0.78f, 0.64f, 0.42f),
        };
        [SerializeField] Color emptyColor = new(0.6f, 0.65f, 0.6f);

        public string ShownName => nameLabel != null ? nameLabel.text : "";
        public string ShownBenefit => benefitLabel != null ? benefitLabel.text : "";

        /// <summary>True si hay otro linaje completo al que cambiar.</summary>
        public bool CanSwitch => Active != null && Other != null;

        internal void Configure(PlayerLineage newLineage, Text newName, Text newBenefit)
        {
            lineage = newLineage;
            nameLabel = newName;
            benefitLabel = newBenefit;
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

        public void Refresh()
        {
            var active = Active;
            if (nameLabel != null)
            {
                nameLabel.text = active != null ? $"{active.DisplayName} · {MutationText.ClassName(active.Class)}" : "Ninguno";
                nameLabel.color = active != null ? MutationText.ClassColor(active.Class, classColors) : emptyColor;
            }

            if (benefitLabel != null)
            {
                var other = Other;
                benefitLabel.text = active == null
                    ? "Lleva las 4 mutaciones de un animal."
                    : Describe(active) + (other != null ? $"\nPulsa para cambiar a {other.DisplayName}" : "");
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

        public void Switch()
        {
            if (!CanSwitch)
                return;
            lineage.Choose(Other);
            Refresh();
        }

        /// <summary>"Rematar: … \n Sangre en el agua: …".</summary>
        public static string Describe(LineageData active)
        {
            var (name, description) = MutationText.ClassBenefit(active.Class, active.Floor);
            string text = $"{name}: {description}";
            if (!string.IsNullOrEmpty(active.TouchName))
                text += $"\n{active.TouchName}: {active.Benefit}";
            return text;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

namespace Primordia.UI
{
    /// <summary>
    /// Fila "Inestable" del panel de estadísticas de la pausa (la crea <see cref="PlayerStatsPanel"/> al llevar una
    /// mutación especial). Al pasar el ratón, el cuadro de información dice cuánto daño extra recibes por golpe.
    /// </summary>
    public class UnstableRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        MutationTooltip tooltip;
        Color color;

        /// <summary>Medios corazones de daño extra por golpe.</summary>
        public int ExtraHalves { get; set; }

        public string Description =>
            $"Recibes {Hearts(ExtraHalves)} de daño extra por cada golpe (no con el veneno ni las caídas).";

        internal void Configure(MutationTooltip newTooltip, Color newColor)
        {
            tooltip = newTooltip;
            color = newColor;
        }

        void OnDisable()
        {
            if (tooltip != null)
                tooltip.Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.Show("Inestable", "Precio de las mutaciones especiales", Description, color, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.Hide();
        }

        /// <summary>"medio corazón", "1 corazón", "1 corazón y medio", "2 corazones"…</summary>
        internal static string Hearts(int halves)
        {
            int whole = halves / 2;
            bool half = halves % 2 == 1;
            if (whole == 0)
                return half ? "medio corazón" : "0 corazones";
            string hearts = whole == 1 ? "1 corazón" : $"{whole} corazones";
            return half ? $"{hearts} y medio" : hearts;
        }
    }
}

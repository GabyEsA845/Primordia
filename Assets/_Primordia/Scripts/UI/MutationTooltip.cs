using Primordia.Mutations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Cuadro de información de una ranura (pausa): franja del color de la ranura, nombre, ranura con su icono y
    /// nivel, lo que hace ahora (la descripción y solo la mejora de su nivel) y sus linajes en una línea corta. Se
    /// coloca al lado de su recuadro, hacia el centro del panel, con un piquito que lo señala. Con la ranura libre
    /// dice "Ranura libre". También lo usa la fila del linaje de las estadísticas, con su propio texto.
    /// </summary>
    public class MutationTooltip : MonoBehaviour
    {
        [SerializeField] Image stripe;
        [SerializeField] Text title;
        [SerializeField, FormerlySerializedAs("lineageIcon")] Image slotIcon;
        [SerializeField] Text details;
        [SerializeField] Text description;
        [Tooltip("Piquito, dibujado apuntando a la izquierda.")]
        [SerializeField] RectTransform pointer;
        [Tooltip("Iconos de ranura en el orden Boca, Extremidades, Piel, Órgano.")]
        [SerializeField, FormerlySerializedAs("lineageIcons")] Sprite[] slotIcons = new Sprite[4];
        [Tooltip("Separación entre el recuadro y el cuadro.")]
        [SerializeField] float gap = 4f;
        [SerializeField] Color emptyStripeColor = new(0.45f, 0.52f, 0.5f);

        RectTransform Rect => (RectTransform)transform;

        public bool IsShown => gameObject.activeSelf;
        public Text Title => title;
        public Text Details => details;
        public Text Description => description;

        /// <summary>Jugador del que se cuenta el progreso de los linajes (lo pone <see cref="CreaturePanel"/>).</summary>
        internal PlayerMutations Player { get; set; }

        internal void Configure(Image newStripe, Text newTitle, Image newSlotIcon, Text newDetails, Text newDescription, RectTransform newPointer)
        {
            stripe = newStripe;
            title = newTitle;
            slotIcon = newSlotIcon;
            details = newDetails;
            description = newDescription;
            pointer = newPointer;
        }

        internal void SetSlotIcons(Sprite[] sprites) => slotIcons = sprites;

        public void Show(SlotCallout callout)
        {
            var mutation = callout.Mutation;
            if (mutation != null)
            {
                title.text = mutation.DisplayName;
                details.text = $"{MutationText.SlotName(mutation.Slot)} · Nivel {MutationText.LevelName(Mathf.Max(1, callout.Level))}";
                description.text = MutationText.WithLine(MutationText.CurrentDescription(mutation, callout.Level),
                    MutationText.LineageProgress(mutation, Player));
                stripe.color = mutation.Color;
            }
            else
            {
                title.text = MutationText.SlotName(callout.Slot);
                details.text = "Ranura libre";
                description.text = "";
                stripe.color = emptyStripeColor;
            }

            // El icono de la ranura sale también con la ranura libre.
            int slot = (int)callout.Slot;
            slotIcon.sprite = slotIcons != null && slot < slotIcons.Length ? slotIcons[slot] : null;
            slotIcon.enabled = slotIcon.sprite != null;
            slotIcon.gameObject.SetActive(slotIcon.sprite != null);
            description.gameObject.SetActive(description.text.Length > 0);

            Place(callout.Rect);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Muestra un texto cualquiera al lado de ese elemento (la fila del linaje de las estadísticas).</summary>
        public void Show(string newTitle, string newDetails, string newDescription, Color stripeColor, RectTransform box)
        {
            title.text = newTitle;
            details.text = newDetails;
            description.text = newDescription;
            stripe.color = stripeColor;
            slotIcon.gameObject.SetActive(false);
            description.gameObject.SetActive(!string.IsNullOrEmpty(newDescription));

            Place(box);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Los recuadros de la izquierda abren el cuadro a su derecha y los de la derecha a su izquierda; los de
        /// arriba lo alinean por arriba y los de abajo por abajo, para que no se salga del panel.
        /// </summary>
        void Place(RectTransform box)
        {
            // El centro del elemento en el espacio del panel del cuadro, venga de donde venga.
            var parent = (RectTransform)transform.parent;
            Vector2 center = parent.InverseTransformPoint(box.TransformPoint(box.rect.center));
            center -= parent.rect.center;
            Vector2 half = box.rect.size / 2f;
            bool opensRight = center.x < 0f;
            bool alignTop = center.y > 0f;

            var rect = Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(opensRight ? 0f : 1f, alignTop ? 1f : 0f);
            rect.anchoredPosition = new Vector2(
                center.x + (opensRight ? half.x + gap : -half.x - gap),
                center.y + (alignTop ? half.y : -half.y));

            if (pointer == null)
                return;

            // A la altura del centro del recuadro; pisa 2 px el borde del cuadro.
            pointer.anchorMin = pointer.anchorMax = new Vector2(opensRight ? 0f : 1f, alignTop ? 1f : 0f);
            // Fila central (3 de 7) sobre la línea de píxel, sin medios píxeles.
            pointer.pivot = new Vector2(1f, 3f / 7f);
            pointer.localScale = new Vector3(opensRight ? 1f : -1f, 1f, 1f);
            pointer.anchoredPosition = new Vector2(opensRight ? 2f : -2f, alignTop ? -half.y : half.y);
        }
    }
}

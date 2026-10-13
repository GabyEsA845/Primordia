using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Cuadro de información de una ranura (pausa): franja del color del linaje, nombre, ranura y linaje con su
    /// icono, y descripción. Se coloca al lado de su recuadro, hacia el centro del panel, con un piquito que
    /// lo señala. Con la ranura libre dice "Ranura libre".
    /// </summary>
    public class MutationTooltip : MonoBehaviour
    {
        [SerializeField] Image stripe;
        [SerializeField] Text title;
        [SerializeField] Image lineageIcon;
        [SerializeField] Text details;
        [SerializeField] Text description;
        [Tooltip("Piquito, dibujado apuntando a la izquierda.")]
        [SerializeField] RectTransform pointer;
        [Tooltip("Iconos de linaje en el orden Depredador, Acorazado, Tóxico.")]
        [SerializeField] Sprite[] lineageIcons = new Sprite[3];
        [Tooltip("Separación entre el recuadro y el cuadro.")]
        [SerializeField] float gap = 4f;
        [SerializeField] Color emptyStripeColor = new(0.45f, 0.52f, 0.5f);

        RectTransform Rect => (RectTransform)transform;

        public bool IsShown => gameObject.activeSelf;
        public Text Title => title;
        public Text Details => details;
        public Text Description => description;

        internal void Configure(Image newStripe, Text newTitle, Image newLineageIcon, Text newDetails, Text newDescription, RectTransform newPointer)
        {
            stripe = newStripe;
            title = newTitle;
            lineageIcon = newLineageIcon;
            details = newDetails;
            description = newDescription;
            pointer = newPointer;
        }

        internal void SetLineageIcons(Sprite[] sprites) => lineageIcons = sprites;

        public void Show(SlotCallout callout)
        {
            var mutation = callout.Mutation;
            if (mutation != null)
            {
                title.text = mutation.DisplayName;
                details.text = $"{MutationText.SlotName(mutation.Slot)} · {MutationText.LineageName(mutation.Lineage)} · " +
                               $"Nivel {MutationText.LevelName(Mathf.Max(1, callout.Level))}";
                description.text = MutationText.FullDescription(mutation, callout.Level);
                stripe.color = mutation.Color;
                int lineage = (int)mutation.Lineage;
                lineageIcon.sprite = lineageIcons != null && lineage < lineageIcons.Length ? lineageIcons[lineage] : null;
            }
            else
            {
                title.text = MutationText.SlotName(callout.Slot);
                details.text = "Ranura libre";
                description.text = "";
                stripe.color = emptyStripeColor;
                lineageIcon.sprite = null;
            }

            lineageIcon.enabled = lineageIcon.sprite != null;
            lineageIcon.gameObject.SetActive(lineageIcon.sprite != null);
            description.gameObject.SetActive(description.text.Length > 0);

            Place(callout.Rect);
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
            Vector2 center = box.anchoredPosition;
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

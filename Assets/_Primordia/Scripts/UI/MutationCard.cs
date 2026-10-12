using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>Tarjeta de una mutación en la pantalla de elección.</summary>
    [RequireComponent(typeof(Button))]
    public class MutationCard : MonoBehaviour
    {
        [SerializeField] Image colorStrip;
        [SerializeField] Text title;
        [SerializeField] Text details;
        [SerializeField] Text description;
        [SerializeField] Text replaces;
        [Tooltip("Icono grande de la mutación (32×32).")]
        [SerializeField] Image icon;
        [Tooltip("Icono del linaje, junto a la ranura y el linaje.")]
        [SerializeField] Image lineageIcon;
        [Tooltip("Iconos de linaje en el orden Depredador, Acorazado, Tóxico.")]
        [SerializeField] Sprite[] lineageIcons = new Sprite[3];

        public Button Button => GetComponent<Button>();

        /// <param name="current">Mutación que hay ahora en esa ranura, o null si está libre.</param>
        public void Show(MutationData mutation, MutationData current)
        {
            if (colorStrip != null)
                colorStrip.color = mutation.Color;

            title.text = mutation.DisplayName;
            details.text = $"{MutationText.SlotName(mutation.Slot)} · {MutationText.LineageName(mutation.Lineage)}";
            description.text = mutation.Description;
            replaces.text = current != null ? $"Reemplaza: {current.DisplayName}" : "Ranura libre";

            SetSprite(icon, mutation.LargeIcon);
            int lineage = (int)mutation.Lineage;
            SetSprite(lineageIcon, lineageIcons != null && lineage < lineageIcons.Length ? lineageIcons[lineage] : null);
        }

        /// <summary>Sin sprite, la imagen se oculta (pero sigue ocupando su sitio en la tarjeta).</summary>
        static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        internal Text Details => details;

        internal void ConfigureIcons(Image newIcon, Image newLineageIcon)
        {
            icon = newIcon;
            lineageIcon = newLineageIcon;
        }

        internal void SetLineageIcons(Sprite[] sprites) => lineageIcons = sprites;

        internal void Configure(Image strip, Text newTitle, Text newDetails, Text newDescription, Text newReplaces)
        {
            colorStrip = strip;
            title = newTitle;
            details = newDetails;
            description = newDescription;
            replaces = newReplaces;
        }
    }
}

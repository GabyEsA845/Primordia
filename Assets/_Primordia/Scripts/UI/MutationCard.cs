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
        }

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

using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Cartel del HUD con lo que hay en el pedestal de la tienda que el jugador tiene delante: nombre, detalles
    /// (ranura y linaje, o cambio de nivel), descripción y un aviso (qué mutación reemplaza, vida llena…).
    /// Va abajo en el centro de la pantalla.
    /// </summary>
    public class ShopTooltip : MonoBehaviour
    {
        [SerializeField] Image stripe;
        [SerializeField] Text title;
        [SerializeField] Text details;
        [SerializeField] Text description;
        [SerializeField] Text note;

        public bool IsShown => gameObject.activeSelf;
        public string Title => title != null ? title.text : "";
        public string Note => note != null && note.gameObject.activeSelf ? note.text : "";

        internal void Configure(Image newStripe, Text newTitle, Text newDetails, Text newDescription, Text newNote)
        {
            stripe = newStripe;
            title = newTitle;
            details = newDetails;
            description = newDescription;
            note = newNote;
        }

        public void Show(string titleText, string detailsText, string descriptionText, string noteText, Color stripeColor)
        {
            if (title != null)
                title.text = titleText;
            SetLine(details, detailsText);
            SetLine(description, descriptionText);
            SetLine(note, noteText);
            if (stripe != null)
                stripe.color = stripeColor;

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        static void SetLine(Text text, string content)
        {
            if (text == null)
                return;

            text.text = content ?? "";
            text.gameObject.SetActive(!string.IsNullOrEmpty(content));
        }
    }
}

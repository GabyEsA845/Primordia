using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Lista de las 4 ranuras con la mutación que hay en cada una (nombre y descripción).
    /// Se actualiza cada vez que se muestra; va dentro del panel de pausa.
    /// </summary>
    public class MutationListUI : MonoBehaviour
    {
        const string EmptySlotText = "— vacía —";
        static readonly Color EmptyColor = new(0.55f, 0.6f, 0.58f);

        [Tooltip("Un texto de nombre por ranura, en el orden Boca, Extremidades, Piel, Órgano especial.")]
        [SerializeField] Text[] nameTexts = new Text[4];
        [Tooltip("Un texto de descripción por ranura, en el mismo orden.")]
        [SerializeField] Text[] descriptionTexts = new Text[4];

        void OnEnable()
        {
            var player = GameObject.FindWithTag("Player");
            Refresh(player != null ? player.GetComponent<PlayerMutations>() : null);
        }

        internal void Refresh(PlayerMutations player)
        {
            // Los textos pueden faltar si aún no se ha configurado (por ejemplo, justo al añadir el componente).
            for (int i = 0; i < nameTexts.Length; i++)
            {
                var mutation = player != null ? player.Get((MutationSlot)i) : null;

                if (nameTexts[i] != null)
                {
                    nameTexts[i].text = mutation != null ? mutation.DisplayName : EmptySlotText;
                    nameTexts[i].color = mutation != null ? mutation.Color : EmptyColor;
                }

                if (i < descriptionTexts.Length && descriptionTexts[i] != null)
                    descriptionTexts[i].text = mutation != null ? mutation.Description : "";
            }
        }

        internal void Configure(Text[] names, Text[] descriptions)
        {
            nameTexts = names;
            descriptionTexts = descriptions;
        }
    }
}

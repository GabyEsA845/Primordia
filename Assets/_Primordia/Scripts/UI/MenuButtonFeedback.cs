using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>Avisa al <see cref="MenuSelector"/> cuando el ratón entra, se selecciona o se pulsa el botón.</summary>
    public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler
    {
        internal MenuSelector Selector;

        Button Button => GetComponent<Button>();

        void Notify(System.Action<MenuSelector> action)
        {
            if (Selector != null)
                action(Selector);
        }

        public void OnPointerEnter(PointerEventData eventData) => Notify(s => s.MoveTo(Button));

        public void OnSelect(BaseEventData eventData) => Notify(s => s.MoveTo(Button));

        public void OnPointerClick(PointerEventData eventData) => Notify(s => s.Burst(Button));
    }
}

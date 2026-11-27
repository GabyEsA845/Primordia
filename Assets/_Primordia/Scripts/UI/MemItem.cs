using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Primordia.UI
{
    /// <summary>
    /// Algo que se puede señalar en un menú de membrana, con el ratón encima o con el teclado. La pantalla
    /// (<see cref="MemScreen"/>) decide cuál tiene el foco.
    /// </summary>
    public sealed class MemItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        MemScreen screen;

        /// <summary>Cambia el aspecto al ganar o perder el foco.</summary>
        internal Action<bool> Look;
        internal Action Submit;
        /// <summary>Izquierda (−1) y derecha (+1): si existe, no cambian de elemento.</summary>
        internal Action<int> Adjust;
        /// <summary>False: solo con el ratón (pestañas, que van con Q y E).</summary>
        internal bool Keyboard = true;

        internal RectTransform Rect => (RectTransform)transform;

        internal void Bind(MemScreen owner, Action<bool> look, Action submit, Action<int> adjust, bool keyboard)
        {
            screen = owner;
            Look = look;
            Submit = submit;
            Adjust = adjust;
            Keyboard = keyboard;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (screen != null)
                screen.Focus(this, sound: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (screen != null && !Keyboard)
                screen.Unfocus(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (screen != null)
                screen.Press(this);
        }
    }
}

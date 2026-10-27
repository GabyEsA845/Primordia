using Primordia.Audio;
using Primordia.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Opción "Temblor" de la pausa: al pulsarla pasa de normal a suave, a apagado y vuelta a normal
    /// (<see cref="ScreenShake.Setting"/>). La monta el menú Primordia > Audio > Añadir volumen a la pausa.
    /// </summary>
    public class ScreenShakeOption : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] Text valueLabel;

        internal void Configure(Text newValueLabel) => valueLabel = newValueLabel;

        void OnEnable() => Refresh();

        public static string NameOf(ScreenShake.Level level) => level switch
        {
            ScreenShake.Level.Normal => "Normal",
            ScreenShake.Level.Reduced => "Suave",
            _ => "No"
        };

        public void Refresh()
        {
            if (valueLabel != null)
                valueLabel.text = NameOf(ScreenShake.Setting);
        }

        /// <summary>Pasa al siguiente ajuste.</summary>
        public void Next()
        {
            ScreenShake.Setting = (ScreenShake.Level)(((int)ScreenShake.Setting + 1) % 3);
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Next();
            Sfx.Play(Sounds.UIClick);
        }

        public void OnPointerEnter(PointerEventData eventData) => Sfx.Play(Sounds.UIHover);
    }
}

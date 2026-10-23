using Primordia.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Barra de volumen de la pausa (música o efectos): muestra el valor guardado al abrirse, lo cambia al moverla
    /// y escribe el porcentaje. La de efectos suena al soltarla, para oír cómo queda; la música ya se oye debajo.
    /// La monta el menú Primordia > Audio > Añadir volumen a la pausa.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour, IPointerEnterHandler, IPointerUpHandler
    {
        [SerializeField] SoundGroup group = SoundGroup.Music;
        [SerializeField] Text valueLabel;

        Slider slider;

        public SoundGroup Group => group;
        public Slider Slider => slider != null ? slider : slider = GetComponent<Slider>();

        void Awake()
        {
            Slider.minValue = 0f;
            Slider.maxValue = 1f;
            Slider.onValueChanged.AddListener(OnValueChanged);
        }

        void OnEnable() => Refresh();

        void OnDisable() => AudioVolumes.Save();

        void OnDestroy()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        /// <summary>Pone la barra en el volumen guardado, sin cambiarlo.</summary>
        public void Refresh()
        {
            float value = AudioVolumes.Get(group);
            Slider.SetValueWithoutNotify(value);
            ShowValue(value);
        }

        void OnValueChanged(float value)
        {
            AudioVolumes.Set(group, value);
            ShowValue(value);
        }

        void ShowValue(float value)
        {
            if (valueLabel != null)
                valueLabel.text = $"{Mathf.RoundToInt(value * 100f)} %";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Slider.IsInteractable())
                Sfx.Play(Sounds.UIHover);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (group != SoundGroup.Music)
                Sfx.Play(Sounds.UIClick);
        }

        internal void Configure(SoundGroup newGroup, Text newValueLabel)
        {
            group = newGroup;
            valueLabel = newValueLabel;
        }
    }
}

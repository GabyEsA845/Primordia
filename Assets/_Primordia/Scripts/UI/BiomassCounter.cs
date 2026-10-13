using Primordia.Biomass;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Contador de biomasa del HUD: icono y cantidad. Da un saltito al cambiar, como las ranuras de mutación.
    /// </summary>
    public class BiomassCounter : MonoBehaviour
    {
        [Tooltip("Biomasa a mostrar. Si está vacío, usa la del objeto con la etiqueta Player.")]
        [SerializeField] PlayerBiomass biomass;
        [SerializeField] Image icon;
        [SerializeField] Text label;
        [Tooltip("Lo que salta al cambiar la cantidad (el icono si está vacío).")]
        [SerializeField] RectTransform popTarget;
        [Tooltip("Tamaño al empezar el salto; vuelve a 1 en popDuration segundos (tiempo real).")]
        [SerializeField, Min(1f)] float popScale = 1.25f;
        [SerializeField, Min(0.01f)] float popDuration = 0.2f;

        float popStart = -1f;

        public string AmountText => label != null ? label.text : "";

        void Start()
        {
            if (biomass == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    biomass = player.GetComponent<PlayerBiomass>();
            }

            if (biomass == null)
            {
                // Escenas sin biomasa (pruebas antiguas): no se muestra.
                gameObject.SetActive(false);
                return;
            }

            biomass.Changed += OnChanged;
            Refresh();
        }

        void OnDestroy()
        {
            if (biomass != null)
                biomass.Changed -= OnChanged;
        }

        internal void Configure(PlayerBiomass newBiomass, Image newIcon, Text newLabel)
        {
            biomass = newBiomass;
            icon = newIcon;
            label = newLabel;
        }

        internal void SetIcon(Sprite sprite)
        {
            if (icon == null)
                return;

            icon.sprite = sprite;
            icon.color = Color.white;
            icon.enabled = sprite != null;
        }

        void OnChanged(int amount, int change)
        {
            Refresh();
            popStart = Time.unscaledTime;
        }

        void Refresh()
        {
            if (label != null)
                label.text = biomass.Amount.ToString();
        }

        void LateUpdate()
        {
            if (popStart < 0f)
                return;

            var target = popTarget != null ? popTarget : icon != null ? icon.rectTransform : null;
            if (target == null)
                return;

            float t = (Time.unscaledTime - popStart) / popDuration;
            if (t >= 1f)
            {
                popStart = -1f;
                target.localScale = Vector3.one;
            }
            else
            {
                target.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, t);
            }
        }
    }
}

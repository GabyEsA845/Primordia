using Primordia.Mutations;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Barra bajo el jugador que se llena mientras se recarga la habilidad del Órgano especial.
    /// Solo se ve durante la recarga; al estar lista, desaparece.
    /// </summary>
    public class CooldownBar : MonoBehaviour
    {
        [SerializeField] PlayerAbility ability;
        [Tooltip("Fondo de la barra; marca su ancho total.")]
        [SerializeField] SpriteRenderer background;
        [Tooltip("Relleno; se estira de izquierda a derecha.")]
        [SerializeField] SpriteRenderer fill;

        Vector3 fullFillScale;
        float fullWidth;

        void Awake()
        {
            if (ability == null)
                ability = GetComponentInParent<PlayerAbility>();

            fullFillScale = fill.transform.localScale;
            fullWidth = background.bounds.size.x / transform.lossyScale.x;
            SetVisible(false);
        }

        void LateUpdate()
        {
            bool charging = ability != null && ability.IsCharging;
            SetVisible(charging);
            if (!charging)
                return;

            float progress = ability.CooldownProgress;
            fill.transform.localScale = new Vector3(fullFillScale.x * progress, fullFillScale.y, fullFillScale.z);
            // Anclado a la izquierda: el centro se desplaza a medida que crece.
            fill.transform.localPosition = new Vector3(-fullWidth * (1f - progress) / 2f, 0f, 0f);
        }

        void SetVisible(bool visible)
        {
            background.enabled = visible;
            fill.enabled = visible;
        }

        internal void Configure(PlayerAbility newAbility, SpriteRenderer newBackground, SpriteRenderer newFill)
        {
            ability = newAbility;
            background = newBackground;
            fill = newFill;
        }
    }
}

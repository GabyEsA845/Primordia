using Primordia.Mutations;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Barra bajo el jugador que se llena mientras se recarga la habilidad del Órgano especial.
    /// Solo se ve durante la recarga; al estar lista, destella (si tiene cuadros de destello) y desaparece.
    /// </summary>
    public class CooldownBar : MonoBehaviour
    {
        [SerializeField] PlayerAbility ability;
        [Tooltip("Fondo de la barra.")]
        [SerializeField] SpriteRenderer background;
        [Tooltip("Relleno; se estira de izquierda a derecha.")]
        [SerializeField] SpriteRenderer fill;
        [Tooltip("Cuadros del destello al quedar lista; se dibujan en el fondo. Vacío = sin destello.")]
        [SerializeField] Sprite[] readyFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float readyFrameDuration = 0.05f;

        Sprite backgroundSprite;
        Vector3 fullFillScale;
        Vector3 fillCenter;
        float fullFillWidth;
        bool wasCharging;
        float readyStart = -1f;

        /// <summary>True mientras se ve el destello de habilidad lista.</summary>
        public bool IsFlashing => readyStart >= 0f;

        void Awake()
        {
            if (ability == null)
                ability = GetComponentInParent<PlayerAbility>();

            backgroundSprite = background.sprite;
            fullFillScale = fill.transform.localScale;
            fillCenter = fill.transform.localPosition;
            // Ancho del relleno lleno, en unidades locales de la barra.
            fullFillWidth = fill.sprite != null ? fill.sprite.bounds.size.x * fullFillScale.x : fullFillScale.x;
            SetVisible(false);
        }

        void LateUpdate()
        {
            bool charging = ability != null && ability.IsCharging;
            if (wasCharging && !charging && readyFrames.Length > 0)
                readyStart = Time.time;
            wasCharging = charging;

            if (charging)
            {
                readyStart = -1f;
                background.sprite = backgroundSprite;
                SetVisible(true);

                float progress = ability.CooldownProgress;
                fill.transform.localScale = new Vector3(fullFillScale.x * progress, fullFillScale.y, fullFillScale.z);
                // Anclado a la izquierda: el centro se desplaza a medida que crece.
                fill.transform.localPosition = fillCenter - new Vector3(fullFillWidth * (1f - progress) / 2f, 0f, 0f);
                return;
            }

            if (IsFlashing)
            {
                int frame = Mathf.FloorToInt((Time.time - readyStart) / readyFrameDuration);
                if (frame < readyFrames.Length)
                {
                    background.sprite = readyFrames[frame];
                    background.enabled = true;
                    fill.enabled = false;
                    return;
                }

                readyStart = -1f;
                background.sprite = backgroundSprite;
            }

            SetVisible(false);
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

        internal void SetReadyFrames(Sprite[] frames, float frameDuration)
        {
            readyFrames = frames ?? System.Array.Empty<Sprite>();
            readyFrameDuration = Mathf.Max(0.01f, frameDuration);
        }
    }
}

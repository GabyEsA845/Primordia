using Primordia.Core;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Pieza de sala que se usa con E (almejas, el coral del corazón morado): con el jugador cerca, la tecla E flota encima
    /// (pulsada mientras se mantiene) y el cartel de la tienda dice qué es y qué hace.
    /// </summary>
    public abstract class PromptInteractable : Interactable
    {
        const float PixelSize = 1f / 16f;

        [SerializeField] SpriteRenderer keyPrompt;
        [Tooltip("Tecla normal y pulsada.")]
        [SerializeField] Sprite[] keyFrames = System.Array.Empty<Sprite>();
        [Tooltip("Altura de la tecla sobre la pieza, en unidades.")]
        [SerializeField] float keyHeight = 1.1f;

        ShopTooltip tooltip;
        PlayerInteractor interactor;

        protected abstract string TooltipTitle { get; }
        protected abstract string TooltipDetails { get; }
        protected virtual Color TooltipColor => new(0.45f, 0.75f, 0.95f);

        internal void ConfigurePrompt(Sprite[] newKeyFrames, float newKeyHeight)
        {
            keyFrames = newKeyFrames ?? System.Array.Empty<Sprite>();
            keyHeight = newKeyHeight;
            if (keyPrompt == null && keyFrames.Length > 0)
            {
                var key = new GameObject("KeyPrompt");
                key.transform.SetParent(transform, false);
                keyPrompt = key.AddComponent<SpriteRenderer>();
                keyPrompt.sortingOrder = 50;
                keyPrompt.sprite = keyFrames[0];
                keyPrompt.enabled = false;
            }
        }

        protected virtual void Start() => tooltip = FindAnyObjectByType<ShopTooltip>(FindObjectsInactive.Include);

        protected virtual void LateUpdate()
        {
            if (keyPrompt == null || !keyPrompt.enabled)
                return;
            bool holding = interactor != null && interactor.IsHolding;
            if (keyFrames.Length > 1 && keyFrames[1] != null)
                keyPrompt.sprite = holding ? keyFrames[1] : keyFrames[0];
            float bob = Mathf.Sin(Time.time * 4f) > 0f ? PixelSize : 0f;
            var position = Position + new Vector2(0f, keyHeight + bob);
            keyPrompt.transform.position = new Vector3(Mathf.Round(position.x / PixelSize) * PixelSize, Mathf.Round(position.y / PixelSize) * PixelSize, 0f);
        }

        protected override void OnFocusChanged(bool focused, GameObject player)
        {
            interactor = focused && player != null ? player.GetComponent<PlayerInteractor>() : null;
            if (keyPrompt != null)
                keyPrompt.enabled = focused;
            if (tooltip == null)
                return;
            if (focused)
                tooltip.Show(TooltipTitle, TooltipDetails, "", "", TooltipColor);
            else
                tooltip.Hide();
        }

        /// <summary>Quita la tecla y el cartel (ya usada).</summary>
        protected void HidePrompt()
        {
            if (keyPrompt != null)
                keyPrompt.enabled = false;
            if (IsFocused && tooltip != null)
                tooltip.Hide();
        }
    }
}

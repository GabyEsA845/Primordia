using System.Collections;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Shop
{
    public enum ShopItemKind
    {
        None,
        Mutation,
        /// <summary>Sube de nivel una mutación que el jugador ya lleva.</summary>
        Upgrade,
        /// <summary>Medio corazón.</summary>
        Heal,
        /// <summary>Un corazón más de vida máxima.</summary>
        HeartContainer
    }

    /// <summary>
    /// Pedestal de la tienda con un artículo encima, que flota. Al acercarse el jugador brilla y aparece la tecla
    /// de Interactuar; al pulsarla, la <see cref="ShopRoom"/> intenta vendérselo. Debajo, el precio (en rojo si no
    /// le llega) y, en las mejoras, el cambio de nivel. Vendido, el artículo deja en su sitio la señal de vendido;
    /// cerrado (ya no se podrá comprar nunca), el letrero de no disponible.
    /// </summary>
    public class ShopPedestal : Interactable
    {
        [Header("Artículo")]
        [Tooltip("Lo que flota: el artículo y el icono.")]
        [SerializeField] Transform floating;
        [SerializeField] SpriteRenderer item;
        [SerializeField] Animator itemAnimator;
        [Tooltip("Icono de la mutación, encima de la burbuja.")]
        [SerializeField] SpriteRenderer icon;
        [SerializeField] GameObject shadow;
        [Tooltip("Altura del vaivén, en unidades (1 px = 0,0625).")]
        [SerializeField, Min(0f)] float floatHeight = 0.0625f;
        [SerializeField, Min(0.1f)] float floatPeriod = 1.6f;

        [Header("Cerca del jugador")]
        [SerializeField] GameObject glow;
        [SerializeField] SpriteRenderer keyPrompt;
        [Tooltip("Tecla normal y pulsada.")]
        [SerializeField] Sprite[] keyFrames = new Sprite[2];

        [Header("Textos")]
        [SerializeField] GameObject priceRoot;
        [SerializeField] Text priceText;
        [SerializeField] Text levelText;
        [SerializeField] Color priceColor = new(0.9f, 0.97f, 0.9f);
        [SerializeField] Color unaffordableColor = new(1f, 0.35f, 0.35f);

        const float PixelSize = 1f / 16f;

        ShopRoom shop;
        PlayerInteractor interactor;
        Vector3 keyBase;
        float floatBase;
        float phase;
        bool bobbing = true;
        Coroutine shaking;
        float shakeOffset;

        /// <summary>Lo que se puso al surtir la tienda.</summary>
        public ShopItemKind Kind { get; private set; }
        /// <summary>
        /// Lo que vende ahora: en los pedestales de mutación cambia entre mutación nueva y mejora según lo que lleve
        /// el jugador.
        /// </summary>
        public ShopItemKind Offer { get; private set; }
        public MutationData Mutation { get; private set; }
        public bool Sold { get; private set; }
        /// <summary>Ya no se podrá comprar nunca (la mutación llegó al nivel máximo).</summary>
        public bool Closed { get; private set; }
        public bool HasItem => Kind != ShopItemKind.None && !Sold && !Closed;
        public override bool IsAvailable => HasItem;
        public string PriceText => priceText != null ? priceText.text : "";
        public string LevelText => levelText != null && levelText.gameObject.activeSelf ? levelText.text : "";
        public bool PriceIsRed => priceText != null && priceText.color == unaffordableColor;

        void Awake()
        {
            phase = Random.value;
            if (keyPrompt != null)
                keyBase = keyPrompt.transform.localPosition;
            if (floating != null)
                floatBase = floating.localPosition.y;
            ShowFocus(false);
            if (Kind == ShopItemKind.None)
                SetEmpty();
        }

        internal void SetShop(ShopRoom newShop) => shop = newShop;

        internal void ConfigureLook(Transform newFloating, SpriteRenderer newItem, Animator newItemAnimator, SpriteRenderer newIcon,
            GameObject newShadow, GameObject newGlow, SpriteRenderer newKeyPrompt, Sprite[] newKeyFrames,
            GameObject newPriceRoot, Text newPriceText, Text newLevelText)
        {
            floating = newFloating;
            item = newItem;
            itemAnimator = newItemAnimator;
            icon = newIcon;
            shadow = newShadow;
            glow = newGlow;
            keyPrompt = newKeyPrompt;
            keyFrames = newKeyFrames;
            priceRoot = newPriceRoot;
            priceText = newPriceText;
            levelText = newLevelText;
        }

        /// <summary>
        /// Pone el artículo: su animación y, si es una mutación o una mejora, el icono encima. <paramref name="center"/>
        /// y <paramref name="top"/> son la altura del centro y del borde de arriba del artículo sobre la base del
        /// pedestal, en unidades: la tecla se pone 2 px por encima.
        /// </summary>
        internal void SetItem(ShopItemKind kind, MutationData mutation, RuntimeAnimatorController look, float center = 1.625f, float top = 2.375f)
        {
            Kind = kind;
            Offer = kind;
            Mutation = mutation;
            Sold = false;
            Closed = false;

            if (kind == ShopItemKind.None)
            {
                SetEmpty();
                return;
            }

            ShowFloating(center, look, bob: true, withShadow: true);
            if (keyPrompt != null)
            {
                keyBase = new Vector3(0f, top + 2f * PixelSize, 0f);
                keyPrompt.transform.localPosition = keyBase;
            }
            if (priceRoot != null)
                priceRoot.SetActive(true);
            ShowIcon();
        }

        /// <summary>
        /// Cambia lo que vende (mutación nueva o mejora) sin moverlo: el aspecto pasa a la burbuja o a la mejora,
        /// con el mismo icono.
        /// </summary>
        internal void SetOffer(ShopItemKind offer, RuntimeAnimatorController look)
        {
            if (!HasItem || offer == Offer)
                return;

            Offer = offer;
            SetLook(look);
            ShowIcon();
        }

        void ShowFloating(float center, RuntimeAnimatorController look, bool bob, bool withShadow)
        {
            floatBase = center;
            bobbing = bob;
            if (floating != null)
            {
                floating.gameObject.SetActive(true);
                floating.localPosition = new Vector3(0f, center, 0f);
            }
            if (shadow != null)
                shadow.SetActive(withShadow);
            SetLook(look);
        }

        void SetLook(RuntimeAnimatorController look)
        {
            if (itemAnimator != null)
            {
                itemAnimator.runtimeAnimatorController = look;
                itemAnimator.enabled = look != null;
            }

            if (item != null)
                item.color = look == null && Mutation != null && HasItem ? Mutation.Color : Color.white;
        }

        void ShowIcon()
        {
            if (icon == null)
                return;

            bool showIcon = HasItem && Mutation != null && (Offer == ShopItemKind.Mutation || Offer == ShopItemKind.Upgrade);
            icon.sprite = showIcon ? Mutation.Icon : null;
            icon.enabled = showIcon && Mutation.Icon != null;
        }

        /// <summary>Precio y cambio de nivel; el precio se pone rojo si no se puede comprar.</summary>
        internal void SetLabels(int price, bool canBuy, string level)
        {
            if (priceText != null)
            {
                priceText.text = price.ToString();
                priceText.color = canBuy ? priceColor : unaffordableColor;
            }

            if (levelText != null)
            {
                levelText.gameObject.SetActive(!string.IsNullOrEmpty(level));
                levelText.text = level ?? "";
            }
        }

        /// <summary>
        /// Vendido: el artículo desaparece con el efecto de compra y en su sitio flota la señal de vendido
        /// (<paramref name="soldLook"/>, centrada a <paramref name="center"/> sobre la base).
        /// </summary>
        internal void Sell(GameObject purchaseEffect, RuntimeAnimatorController soldLook = null, float center = 1.375f)
        {
            var effectPosition = floating != null ? floating.position : transform.position;
            Sold = true;
            SetEmpty();
            if (soldLook != null)
                ShowFloating(center, soldLook, bob: true, withShadow: true);
            ShowIcon();
            OneShotEffect.Spawn(purchaseEffect, effectPosition);
        }

        /// <summary>
        /// Cerrado para siempre: el artículo desaparece y queda clavado encima el letrero de no disponible
        /// (<paramref name="closedLook"/>, centrado a <paramref name="center"/> sobre la base, sin flotar).
        /// </summary>
        internal void Close(RuntimeAnimatorController closedLook = null, float center = 1.125f)
        {
            if (!HasItem)
                return;

            Closed = true;
            SetEmpty();
            if (closedLook != null)
                ShowFloating(center, closedLook, bob: false, withShadow: false);
            ShowIcon();
        }

        void SetEmpty()
        {
            if (floating != null)
                floating.gameObject.SetActive(false);
            if (shadow != null)
                shadow.SetActive(false);
            if (priceRoot != null)
                priceRoot.SetActive(false);
            if (levelText != null)
                levelText.gameObject.SetActive(false);
            ShowFocus(false);
        }

        /// <summary>No se pudo comprar: el artículo tiembla.</summary>
        internal void Shake()
        {
            if (shaking != null)
                StopCoroutine(shaking);
            shaking = StartCoroutine(ShakeRoutine());
        }

        IEnumerator ShakeRoutine()
        {
            // 1 px a cada lado, cada vez más flojo.
            float[] steps = { 1f, -1f, 1f, -1f, 0.5f, -0.5f, 0f };
            foreach (float step in steps)
            {
                shakeOffset = Mathf.Round(step) * PixelSize;
                yield return new WaitForSeconds(0.04f);
            }

            shakeOffset = 0f;
            shaking = null;
        }

        void Update()
        {
            if (floating != null && floating.gameObject.activeSelf)
            {
                // Ajustado a píxeles enteros, como el resto del arte.
                float offset = bobbing ? Mathf.Sin((Time.time / floatPeriod + phase) * 2f * Mathf.PI) * floatHeight : 0f;
                floating.localPosition = new Vector3(shakeOffset, floatBase + Mathf.Round(offset / PixelSize) * PixelSize, 0f);
            }

            if (keyPrompt != null && keyPrompt.enabled)
            {
                bool holding = interactor != null && interactor.IsHolding;
                if (keyFrames != null && keyFrames.Length > 1 && keyFrames[0] != null)
                    keyPrompt.sprite = holding && keyFrames[1] != null ? keyFrames[1] : keyFrames[0];
                float bob = Mathf.Sin(Time.time * 4f) > 0f ? PixelSize : 0f;
                keyPrompt.transform.localPosition = keyBase + new Vector3(0f, bob, 0f);
            }
        }

        protected override void OnFocusChanged(bool focused, GameObject player)
        {
            interactor = focused && player != null ? player.GetComponent<PlayerInteractor>() : null;
            ShowFocus(focused && HasItem);
            if (shop != null)
                shop.OnPedestalFocus(this, focused, player);
        }

        void ShowFocus(bool focused)
        {
            if (glow != null)
                glow.SetActive(focused);
            if (keyPrompt != null)
                keyPrompt.enabled = focused;
        }

        public override void Interact(GameObject player)
        {
            if (shop != null && HasItem)
                shop.TryBuy(this, player);
        }
    }
}

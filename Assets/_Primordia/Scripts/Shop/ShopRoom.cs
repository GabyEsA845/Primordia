using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Primordia.Biomass;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Primordia.Shop
{
    /// <summary>
    /// La tienda de Darwin: 4 pedestales (2 mutaciones, 1 mejora de nivel y la curación) que se surten la primera
    /// vez que el jugador entra en la sala y ya no cambian. Sin mutaciones que puedan subir, la mejora es una tercera
    /// mutación; con la vida llena, la curación es un corazón extra. Comprar gasta biomasa y Darwin lo celebra.
    /// Los pedestales de mutación ofrecen lo que le sirve al jugador en cada momento: la mutación si no la lleva, su
    /// mejora si la lleva (aunque la consiguiera o subiera fuera de la tienda) y, si ya está al nivel máximo, se
    /// cierran para siempre. Cada pedestal se vende una sola vez.
    /// </summary>
    public class ShopRoom : MonoBehaviour
    {
        [SerializeField] MutationPool pool;
        [Tooltip("En el orden en que se surten: mutación, mutación, mejora, curación.")]
        [SerializeField] ShopPedestal[] pedestals = Array.Empty<ShopPedestal>();

        [Header("Precios")]
        [SerializeField, Min(0)] int mutationPrice = 15;
        [Tooltip("Mejora a nivel II.")]
        [SerializeField, Min(0)] int upgradePrice = 20;
        [Tooltip("Mejora a nivel III (la evolución).")]
        [SerializeField, Min(0)] int evolutionPrice = 30;
        [Tooltip("Medio corazón.")]
        [SerializeField, Min(0)] int healPrice = 5;
        [SerializeField, Min(0)] int heartContainerPrice = 25;

        [Header("Aspecto")]
        [Tooltip("Burbuja por ranura, en el orden Boca, Extremidades, Piel, Órgano.")]
        [SerializeField, FormerlySerializedAs("lineageBubbles")] RuntimeAnimatorController[] slotBubbles = new RuntimeAnimatorController[4];
        [SerializeField] RuntimeAnimatorController upgradeLook;
        [SerializeField] RuntimeAnimatorController healLook;
        [SerializeField] RuntimeAnimatorController heartContainerLook;
        [SerializeField] GameObject purchaseEffect;
        [Tooltip("Señal que flota sobre un pedestal ya vendido.")]
        [SerializeField] RuntimeAnimatorController soldLook;
        [Tooltip("Letrero clavado sobre un pedestal que ya no se podrá comprar.")]
        [SerializeField] RuntimeAnimatorController closedLook;
        [Tooltip("Alto en píxeles de cada artículo: burbuja (mutación y mejora), curación y corazón extra. Flotan con " +
                 "el borde de abajo 2 px por debajo de lo alto del pedestal.")]
        [SerializeField] Vector3Int itemHeights = new(24, 12, 16);
        [Tooltip("Alto del pedestal en píxeles.")]
        [SerializeField, Min(1)] int pedestalHeight = 16;
        [Tooltip("Alto en píxeles de la señal de vendido y del letrero de no disponible; el letrero se clava con su " +
                 "borde de abajo a esta altura sobre la base del pedestal (x).")]
        [SerializeField] Vector2Int markLayout = new(10, 16);
        [SerializeField] Color healColor = new(0.9f, 0.25f, 0.3f);
        [SerializeField] Color upgradeColor = new(0.95f, 0.75f, 0.25f);

        [Header("Darwin")]
        [SerializeField] Transform counter;
        [SerializeField] Animator darwin;
        [Tooltip("Lo que dura su animación al vender; luego vuelve al reposo.")]
        [SerializeField, Min(0f)] float buyDuration = 0.55f;

        // Estados del Animator de Darwin (un clip por estado, con el nombre de la hoja sin el prefijo Shop_).
        const string IdleState = "Darwin_Idle";
        const string BuyState = "Darwin_Buy";
        const float PixelSize = 1f / 16f;

        Room room;
        ShopTooltip tooltip;
        GameObject customer;
        Coroutine celebrating;

        public IReadOnlyList<ShopPedestal> Pedestals => pedestals;
        public bool Stocked { get; private set; }
        public ShopTooltip Tooltip => tooltip;

        /// <summary>Se lanza tras cada compra.</summary>
        public event Action<ShopPedestal> Purchased;

        void Awake()
        {
            room = GetComponentInParent<Room>();
            foreach (var pedestal in pedestals)
            {
                if (pedestal != null)
                    pedestal.SetShop(this);
            }
        }

        void OnEnable()
        {
            if (room != null)
                room.Entered += OnEntered;
        }

        void OnDisable()
        {
            if (room != null)
                room.Entered -= OnEntered;
        }

        void Start()
        {
            tooltip = FindAnyObjectByType<ShopTooltip>(FindObjectsInactive.Include);
            if (tooltip != null)
                tooltip.Hide();
        }

        internal void Configure(MutationPool newPool, ShopPedestal[] newPedestals, Transform newCounter, Animator newDarwin, float newBuyDuration)
        {
            pool = newPool;
            pedestals = newPedestals ?? Array.Empty<ShopPedestal>();
            counter = newCounter;
            darwin = newDarwin;
            buyDuration = Mathf.Max(0f, newBuyDuration);
            foreach (var pedestal in pedestals)
            {
                if (pedestal != null)
                    pedestal.SetShop(this);
            }
        }

        internal void ConfigureLook(RuntimeAnimatorController[] newBubbles, RuntimeAnimatorController newUpgrade,
            RuntimeAnimatorController newHeal, RuntimeAnimatorController newHeartContainer, GameObject newPurchaseEffect,
            RuntimeAnimatorController newSold = null, RuntimeAnimatorController newClosed = null)
        {
            slotBubbles = newBubbles;
            upgradeLook = newUpgrade;
            healLook = newHeal;
            heartContainerLook = newHeartContainer;
            purchaseEffect = newPurchaseEffect;
            soldLook = newSold;
            closedLook = newClosed;
        }

        internal void SetTooltip(ShopTooltip newTooltip) => tooltip = newTooltip;

        /// <summary>Aparta el mostrador (y a Darwin) en horizontal, por ejemplo para no tapar una puerta de arriba.</summary>
        public void MoveCounter(float x)
        {
            if (counter != null)
                counter.localPosition = new Vector3(x, counter.localPosition.y, counter.localPosition.z);
        }

        void OnEntered(Room _)
        {
            if (Stocked)
                return;

            var playerObject = GameObject.FindWithTag("Player");
            Stock(playerObject != null ? playerObject.GetComponent<PlayerMutations>() : null);
        }

        /// <summary>Surte los pedestales para ese jugador. Solo la primera vez: después la mercancía queda fija.</summary>
        public void Stock(PlayerMutations player)
        {
            if (Stocked)
                return;

            Stocked = true;
            customer = player != null ? player.gameObject : null;

            var upgradable = player != null ? player.Equipped.Where(player.CanLevelUp).ToList() : new List<MutationData>();
            var upgrade = upgradable.Count > 0 ? upgradable[UnityEngine.Random.Range(0, upgradable.Count)] : null;

            int mutationCount = upgrade != null ? 2 : 3;
            var exclude = player != null ? player.Equipped : Enumerable.Empty<MutationData>();
            var mutations = pool != null ? pool.PickOffer(mutationCount, exclude) : new List<MutationData>();

            var health = player != null ? player.Health : null;
            bool fullHealth = health != null && health.Current >= health.Max;

            var items = new List<(ShopItemKind kind, MutationData mutation)>();
            foreach (var mutation in mutations)
                items.Add((ShopItemKind.Mutation, mutation));
            // Si la reserva se queda corta, el pedestal de la mejora (o el que falte) queda vacío.
            while (items.Count < 3)
                items.Add((ShopItemKind.None, null));
            if (upgrade != null)
                items[2] = (ShopItemKind.Upgrade, upgrade);
            items.Add((fullHealth ? ShopItemKind.HeartContainer : ShopItemKind.Heal, null));

            for (int i = 0; i < pedestals.Length; i++)
            {
                var pedestal = pedestals[i];
                if (pedestal == null)
                    continue;

                var kind = i < items.Count ? items[i].kind : ShopItemKind.None;
                var mutation = i < items.Count ? items[i].mutation : null;
                int height = kind switch
                {
                    ShopItemKind.Heal => itemHeights.y,
                    ShopItemKind.HeartContainer => itemHeights.z,
                    _ => itemHeights.x
                };
                float bottom = (pedestalHeight - 2) * PixelSize;
                pedestal.SetItem(kind, mutation, LookOf(kind, mutation), bottom + height * PixelSize / 2f, bottom + height * PixelSize);
            }

            RefreshLabels();
        }

        RuntimeAnimatorController LookOf(ShopItemKind kind, MutationData mutation) => kind switch
        {
            ShopItemKind.Mutation => mutation != null && slotBubbles != null && (int)mutation.Slot < slotBubbles.Length
                ? slotBubbles[(int)mutation.Slot]
                : null,
            ShopItemKind.Upgrade => upgradeLook,
            ShopItemKind.Heal => healLook,
            ShopItemKind.HeartContainer => heartContainerLook,
            _ => null
        };

        void Update()
        {
            if (Stocked)
                RefreshLabels();
        }

        /// <summary>
        /// Lo que vende ahora un pedestal de mutación: la mutación si el jugador no la lleva, la mejora si puede
        /// subirla y nada (<see cref="ShopItemKind.None"/>) si ya está al nivel máximo. Los demás, lo de siempre.
        /// </summary>
        public static ShopItemKind OfferOf(ShopPedestal pedestal, PlayerMutations player)
        {
            if (pedestal.Kind != ShopItemKind.Mutation && pedestal.Kind != ShopItemKind.Upgrade)
                return pedestal.Kind;
            if (player == null || pedestal.Mutation == null || !player.IsEquipped(pedestal.Mutation))
                return ShopItemKind.Mutation;
            return player.CanLevelUp(pedestal.Mutation) ? ShopItemKind.Upgrade : ShopItemKind.None;
        }

        /// <summary>
        /// Pone al día lo que vende cada pedestal según lo que lleve el jugador: cierra para siempre los de las
        /// mutaciones al nivel máximo.
        /// </summary>
        void RefreshOffers(PlayerMutations mutations)
        {
            foreach (var pedestal in pedestals)
            {
                if (pedestal == null || !pedestal.HasItem)
                    continue;

                var offer = OfferOf(pedestal, mutations);
                if (offer == ShopItemKind.None)
                    pedestal.Close(closedLook, (markLayout.x + markLayout.y / 2f) * PixelSize);
                else
                    pedestal.SetOffer(offer, LookOf(offer, pedestal.Mutation));
            }
        }

        void RefreshLabels()
        {
            var mutations = customer != null ? customer.GetComponent<PlayerMutations>() : null;
            var wallet = customer != null ? customer.GetComponent<PlayerBiomass>() : null;
            RefreshOffers(mutations);
            foreach (var pedestal in pedestals)
            {
                if (pedestal == null || !pedestal.HasItem)
                    continue;

                int price = PriceOf(pedestal, mutations);
                bool canBuy = IsUsable(pedestal, customer) && wallet != null && wallet.CanAfford(price);
                string level = pedestal.Offer == ShopItemKind.Upgrade && mutations != null && mutations.CanLevelUp(pedestal.Mutation)
                    ? LevelChange(mutations.LevelOf(pedestal.Mutation))
                    : null;
                pedestal.SetLabels(price, canBuy, level);
            }
        }

        /// <summary>"I > II": la fuente pixel no tiene flecha.</summary>
        static string LevelChange(int from) => $"{MutationText.LevelName(from)} > {MutationText.LevelName(from + 1)}";

        /// <summary>Lo que cuesta ahora: la mejora a III es más cara que a II.</summary>
        public int PriceOf(ShopPedestal pedestal, PlayerMutations player)
        {
            switch (OfferOf(pedestal, player))
            {
                case ShopItemKind.Mutation:
                    return mutationPrice;
                case ShopItemKind.Upgrade:
                    int level = player != null ? player.LevelOf(pedestal.Mutation) : 1;
                    return level + 1 >= MutationData.MaxLevel ? evolutionPrice : upgradePrice;
                case ShopItemKind.Heal:
                    return healPrice;
                case ShopItemKind.HeartContainer:
                    return heartContainerPrice;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Si al jugador le sirve ahora: la mutación o su mejora, mientras no esté al nivel máximo; la curación, si le
        /// falta vida.
        /// </summary>
        bool IsUsable(ShopPedestal pedestal, GameObject player)
        {
            if (player == null || !pedestal.HasItem)
                return false;

            var mutations = player.GetComponent<PlayerMutations>();
            var health = player.GetComponent<Health>();
            return OfferOf(pedestal, mutations) switch
            {
                ShopItemKind.Mutation => mutations != null,
                ShopItemKind.Upgrade => mutations != null,
                ShopItemKind.Heal => health != null && health.Current < health.Max,
                ShopItemKind.HeartContainer => health != null,
                _ => false
            };
        }

        /// <summary>Vende el artículo si al jugador le sirve y le llega la biomasa; si no, el artículo tiembla.</summary>
        public bool TryBuy(ShopPedestal pedestal, GameObject player)
        {
            if (pedestal == null || !pedestal.HasItem || player == null)
                return false;

            var wallet = player.GetComponent<PlayerBiomass>();
            var mutations = player.GetComponent<PlayerMutations>();
            int price = PriceOf(pedestal, mutations);
            if (!IsUsable(pedestal, player) || wallet == null || !wallet.TrySpend(price))
            {
                pedestal.Shake();
                return false;
            }

            string title = Describe(pedestal, mutations).title;
            Give(pedestal, player, mutations);
            float bottom = (pedestalHeight - 2) * PixelSize;
            pedestal.Sell(purchaseEffect, soldLook, bottom + markLayout.y * PixelSize / 2f);
            Celebrate();
            Debug.Log($"Tienda: vendido {title} por {price} de biomasa.", this);

            if (pedestal.IsFocused && tooltip != null)
                tooltip.Hide();

            Purchased?.Invoke(pedestal);
            return true;
        }

        static void Give(ShopPedestal pedestal, GameObject player, PlayerMutations mutations)
        {
            var health = player.GetComponent<Health>();
            switch (OfferOf(pedestal, mutations))
            {
                case ShopItemKind.Mutation:
                    mutations.Equip(pedestal.Mutation);
                    break;
                case ShopItemKind.Upgrade:
                    mutations.LevelUp(pedestal.Mutation.Slot);
                    break;
                case ShopItemKind.Heal:
                    health.Heal(1);
                    break;
                case ShopItemKind.HeartContainer:
                    // Un corazón son 2 puntos de vida; llega lleno.
                    health.SetMax(health.Max + 2, healDifference: true);
                    break;
            }
        }

        void Celebrate()
        {
            if (darwin == null || !darwin.isActiveAndEnabled)
                return;

            if (celebrating != null)
                StopCoroutine(celebrating);
            celebrating = StartCoroutine(CelebrateRoutine());
        }

        IEnumerator CelebrateRoutine()
        {
            darwin.Play(BuyState, 0, 0f);
            yield return new WaitForSeconds(buyDuration);
            darwin.Play(IdleState, 0, 0f);
            celebrating = null;
        }

        internal void OnPedestalFocus(ShopPedestal pedestal, bool focused, GameObject player)
        {
            if (tooltip == null)
                return;

            if (!focused || !pedestal.HasItem)
            {
                tooltip.Hide();
                return;
            }

            var (title, details, description, note, color) = Describe(pedestal, player != null ? player.GetComponent<PlayerMutations>() : null);
            tooltip.Show(title, details, description, note, color);
        }

        /// <summary>Lo que dice el cartel de ese artículo.</summary>
        public (string title, string details, string description, string note, Color color) Describe(ShopPedestal pedestal, PlayerMutations player)
        {
            var mutation = pedestal.Mutation;
            var health = player != null ? player.Health : null;
            switch (OfferOf(pedestal, player))
            {
                case ShopItemKind.Mutation:
                {
                    string note = null;
                    var current = player != null ? player.Get(mutation.Slot) : null;
                    if (current != null)
                        note = $"Reemplaza: {current.DisplayName} (nivel {MutationText.LevelName(player.LevelOf(current.Slot))})";
                    return (mutation.DisplayName, MutationText.SlotName(mutation.Slot),
                        MutationText.WithLine(mutation.Description, MutationText.LineageList(mutation, player)), note, mutation.Color);
                }
                case ShopItemKind.Upgrade:
                {
                    int level = player.LevelOf(mutation);
                    return (mutation.DisplayName, MutationText.LevelChange(level, level + 1), mutation.LevelDescription(level + 1),
                        null, upgradeColor);
                }
                case ShopItemKind.Heal:
                    return ("Curación", "Medio corazón", "Recupera medio corazón de vida.",
                        health != null && health.Current >= health.Max ? "Tienes la vida llena" : null, healColor);
                case ShopItemKind.HeartContainer:
                    return ("Corazón extra", "Vida máxima", "Un corazón más de vida máxima, ya lleno.", null, healColor);
                default:
                    return ("", "", "", null, Color.white);
            }
        }
    }
}

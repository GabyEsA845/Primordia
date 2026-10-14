using System;
using Primordia.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mutación en el suelo: una burbuja del color de su ranura con el icono dentro. Al tocarla, el jugador
    /// la equipa y la burbuja revienta. Burbuja e icono flotan juntos (la sombra se queda en el suelo).
    /// Sin arte de burbuja (escenas de prueba antiguas), tiñe su sprite con el color de la mutación.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MutationPickup : MonoBehaviour
    {
        [SerializeField] MutationData mutation;

        [Header("Aspecto")]
        [Tooltip("Lo que flota: la burbuja y el icono.")]
        [SerializeField] Transform floating;
        [SerializeField] Animator bubble;
        [SerializeField] SpriteRenderer icon;
        [Tooltip("Animación de la burbuja por ranura, en el orden Boca, Extremidades, Piel, Órgano.")]
        [SerializeField, FormerlySerializedAs("lineageBubbles")] RuntimeAnimatorController[] slotBubbles = new RuntimeAnimatorController[4];
        [Tooltip("Efecto al reventar por ranura, en el mismo orden.")]
        [SerializeField, FormerlySerializedAs("lineageBursts")] GameObject[] slotBursts = new GameObject[4];
        [Tooltip("Altura del vaivén, en unidades (1 px = 0,0625).")]
        [SerializeField, Min(0f)] float floatHeight = 0.125f;
        [Tooltip("Segundos que tarda en subir y bajar.")]
        [SerializeField, Min(0.1f)] float floatPeriod = 1.6f;

        const float PixelSize = 1f / 16f;

        public MutationData Mutation => mutation;

        /// <summary>Se lanza cuando el jugador la coge (antes de destruirse).</summary>
        public event Action<MutationPickup> PickedUp;

        float phase;

        // Quien la tocó; se equipa en el siguiente Update, fuera del callback de físicas (al reemplazar una
        // mutación, PlayerMutations destruye componentes al momento, y Unity no lo permite dentro de un trigger).
        PlayerMutations pendingPlayer;
        bool burst;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            // Cada burbuja empieza en un punto distinto del vaivén para que no floten a la vez.
            phase = UnityEngine.Random.value;
            ApplyLook();
        }

        void Update()
        {
            if (pendingPlayer != null)
            {
                var player = pendingPlayer;
                pendingPlayer = null;
                Collect(player);
                return;
            }

            if (floating == null)
                return;

            // Ajustado a píxeles enteros, como el resto del arte.
            float offset = Mathf.Sin((Time.time / floatPeriod + phase) * 2f * Mathf.PI) * floatHeight;
            floating.localPosition = new Vector3(0f, Mathf.Round(offset / PixelSize) * PixelSize, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body != null && body.TryGetComponent<PlayerMutations>(out var player))
                pendingPlayer = player;
        }

        /// <summary>El jugador la equipa y la burbuja revienta.</summary>
        internal void Collect(PlayerMutations player)
        {
            // Ya reventó (el jugador cogió otra del grupo en el mismo cuadro).
            if (mutation == null || burst)
                return;

            player.Equip(mutation);
            Debug.Log($"Mutación equipada: {mutation.DisplayName} ({MutationText.SlotName(mutation.Slot)})", this);
            PickedUp?.Invoke(this);
            Burst();
        }

        /// <summary>Revienta sin equiparse; por ejemplo, cuando el jugador coge otra del mismo grupo.</summary>
        public void Burst()
        {
            if (burst)
                return;

            burst = true;
            if (mutation != null)
                OneShotEffect.Spawn(SlotItem(slotBursts), floating != null ? floating.position : transform.position);
            Destroy(gameObject);
        }

        internal void SetMutation(MutationData newMutation)
        {
            mutation = newMutation;
            ApplyLook();
        }

        internal void ConfigureLook(Transform newFloating, Animator newBubble, SpriteRenderer newIcon,
            RuntimeAnimatorController[] newBubbles, GameObject[] newBursts)
        {
            floating = newFloating;
            bubble = newBubble;
            icon = newIcon;
            slotBubbles = newBubbles;
            slotBursts = newBursts;
        }

        void ApplyLook()
        {
            if (mutation == null)
                return;

            var controller = SlotItem(slotBubbles);
            if (bubble != null && controller != null)
            {
                bubble.runtimeAnimatorController = controller;
                if (icon != null)
                {
                    icon.sprite = mutation.Icon;
                    icon.enabled = mutation.Icon != null;
                }
            }
            else if (TryGetComponent<SpriteRenderer>(out var sprite))
            {
                sprite.color = mutation.Color;
            }
        }

        T SlotItem<T>(T[] items) where T : class
        {
            int index = (int)mutation.Slot;
            return items != null && index < items.Length ? items[index] : null;
        }
    }
}

using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Mutación en el suelo: al tocarla, el jugador la equipa y desaparece.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class MutationPickup : MonoBehaviour
    {
        [SerializeField] MutationData mutation;

        public MutationData Mutation => mutation;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (mutation != null && TryGetComponent<SpriteRenderer>(out var sprite))
                sprite.color = mutation.Color;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (mutation == null || body == null || !body.TryGetComponent<PlayerMutations>(out var player))
                return;

            player.Equip(mutation);
            Debug.Log($"Mutación equipada: {mutation.DisplayName} ({MutationText.SlotName(mutation.Slot)})", this);
            Destroy(gameObject);
        }

        internal void SetMutation(MutationData newMutation) => mutation = newMutation;
    }
}

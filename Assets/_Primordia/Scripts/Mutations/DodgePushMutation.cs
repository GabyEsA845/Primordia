using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Patas articuladas: durante la esquiva, los enemigos que tocas salen empujados, aturdidos y dañados.</summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Dodge Push Mutation", fileName = "Mutation_")]
    public class DodgePushMutation : MutationData
    {
        [Tooltip("Velocidad inicial del empujón, en unidades por segundo.")]
        [SerializeField, Min(0f)] float pushSpeed = 12f;
        [SerializeField, Min(0f)] float stunDuration = 0.5f;
        [SerializeField, Min(0)] int damage = 1;

        public override void Apply(PlayerMutations player) =>
            player.AddBehaviour<DodgePush>(Slot).Configure(player.Controller, pushSpeed, stunDuration, damage);

        internal void Configure(float newPushSpeed, float newStunDuration, int newDamage)
        {
            pushSpeed = newPushSpeed;
            stunDuration = newStunDuration;
            damage = newDamage;
        }
    }
}

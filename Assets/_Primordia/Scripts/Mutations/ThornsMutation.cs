using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Piel urticante: los enemigos que tocan al jugador reciben daño.</summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Thorns Mutation", fileName = "Mutation_")]
    public class ThornsMutation : MutationData
    {
        [SerializeField, Min(1)] int damage = 2;
        [Tooltip("Segundos mínimos entre dos daños al mismo enemigo.")]
        [SerializeField, Min(0.05f)] float interval = 0.5f;

        public override void Apply(PlayerMutations player) =>
            player.AddBehaviour<Thorns>(Slot).Configure(damage, interval);

        internal void Configure(int newDamage, float newInterval)
        {
            damage = newDamage;
            interval = newInterval;
        }
    }
}

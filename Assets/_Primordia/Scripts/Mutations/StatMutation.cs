using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Mutación que cambia estadísticas: velocidad, distancia de esquiva y vida máxima (Aletas, Caparazón).</summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Stat Mutation", fileName = "Mutation_")]
    public class StatMutation : MutationData
    {
        [Tooltip("Multiplica la velocidad. 1,3 = +30 %; 0,9 = −10 %.")]
        [SerializeField, Min(0.1f)] float moveSpeedMultiplier = 1f;
        [Tooltip("Multiplica la distancia de la esquiva.")]
        [SerializeField, Min(0.1f)] float dodgeDistanceMultiplier = 1f;
        [Tooltip("Vida máxima extra (2 = un corazón). Al equiparla también cura esa cantidad.")]
        [SerializeField, Min(0)] int maxHealthBonus;

        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float DodgeDistanceMultiplier => dodgeDistanceMultiplier;
        public int MaxHealthBonus => maxHealthBonus;

        public override void Apply(PlayerMutations player)
        {
            player.Controller.MoveSpeed *= moveSpeedMultiplier;
            player.Controller.DodgeDistance *= dodgeDistanceMultiplier;
            if (maxHealthBonus > 0)
                player.Health.SetMax(player.Health.Max + maxHealthBonus, healDifference: true);
        }

        public override void Remove(PlayerMutations player)
        {
            player.Controller.MoveSpeed /= moveSpeedMultiplier;
            player.Controller.DodgeDistance /= dodgeDistanceMultiplier;
            if (maxHealthBonus > 0)
                player.Health.SetMax(player.Health.Max - maxHealthBonus, healDifference: false);
        }

        internal void Configure(float speedMultiplier, float dodgeMultiplier, int healthBonus)
        {
            moveSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            dodgeDistanceMultiplier = Mathf.Max(0.1f, dodgeMultiplier);
            maxHealthBonus = Mathf.Max(0, healthBonus);
        }
    }
}

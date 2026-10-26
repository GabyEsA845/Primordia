using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Placas de trilobite (Piel, piso 2): +1 corazón, y 1 de cada 4 proyectiles que te alcanzan rebota sin hacerte daño.
    /// Nivel II: rebota 1 de cada 3. Nivel III (Enrollarse): durante la esquiva te enrollas y destruyes los proyectiles
    /// contra los que esquivas.
    /// Ver <see cref="PlatesBounce"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Trilobite Plates", fileName = "Mutation_TrilobitePlates")]
    public class TrilobitePlatesMutation : MutationData
    {
        [Tooltip("Vida máxima extra (2 = un corazón).")]
        [SerializeField, Min(0)] int healthBonus = 2;
        [Tooltip("Rebota 1 de cada tantos proyectiles, por nivel (I, II, III).")]
        [SerializeField] int[] bounceEvery = { 4, 3, 3 };
        [SerializeField] Sprite[] bounceFrames = System.Array.Empty<Sprite>();
        [Tooltip("Enrollarse (nivel III): la bola de placas durante la esquiva.")]
        [SerializeField] Sprite[] rollFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.06f;

        public int BounceEveryAt(int level) => bounceEvery[Mathf.Clamp(level - 1, 0, bounceEvery.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            if (healthBonus > 0)
                player.Health.SetMax(player.Health.Max + healthBonus, healDifference: true);
            player.AddBehaviour<PlatesBounce>(Slot).Configure(player.Controller, BounceEveryAt(level), level >= MaxLevel,
                bounceFrames, rollFrames, frameDuration);
        }

        public override void Remove(PlayerMutations player, int level)
        {
            if (healthBonus > 0)
                player.Health.SetMax(player.Health.Max - healthBonus, healDifference: false);
        }

        internal void ConfigureLook(Sprite[] newBounce, Sprite[] newRoll)
        {
            bounceFrames = newBounce ?? System.Array.Empty<Sprite>();
            rollFrames = newRoll ?? System.Array.Empty<Sprite>();
        }
    }
}

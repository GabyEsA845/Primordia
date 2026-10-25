using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Yema clonal (Órgano, especial de las almejas): clic derecho, brota una copia translúcida que ataca sola con tu Boca
    /// durante 6 s. El efecto llega en el paso 4 de feature/clam-room; de momento no se puede usar (no gasta la recarga) y
    /// solo cuenta como especial (comodín e Inestable).
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Clonal Bud", fileName = "Mutation_ClonalBud")]
    public class ClonalBudMutation : AbilityMutation
    {
        public override bool CanActivate(PlayerMutations player) => false;

        public override void Activate(PlayerMutations player) { }
    }
}

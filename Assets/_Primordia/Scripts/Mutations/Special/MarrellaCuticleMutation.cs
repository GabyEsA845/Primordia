using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cutícula de Marrella (Piel, especial de las almejas): si un ataque te alcanza mientras esquivas, lo refleja (los
    /// proyectiles vuelven a quien los lanzó; los golpes cuerpo a cuerpo aturden al atacante). El efecto llega en el paso 3
    /// de feature/clam-room; de momento solo cuenta como especial (comodín e Inestable).
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Marrella Cuticle", fileName = "Mutation_MarrellaCuticle")]
    public class MarrellaCuticleMutation : MutationData
    {
        public override void Apply(PlayerMutations player, int level) { }
    }
}

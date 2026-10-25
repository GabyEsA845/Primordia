using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Flagelos de Leanchoilia (Extremidades, especial de las almejas): cuando el ataque principal alcanza a un enemigo, unos
    /// flagelos aparecen junto a él y lo azotan con el daño de 1 s de la Boca (uno cada 3 s). No se dibujan en el cuerpo.
    /// El efecto llega en el paso 2 de feature/clam-room; de momento solo cuenta como especial (comodín e Inestable).
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Leanchoilia Flagella", fileName = "Mutation_LeanchoiliaFlagella")]
    public class LeanchoiliaFlagellaMutation : MutationData
    {
        public override void Apply(PlayerMutations player, int level) { }
    }
}

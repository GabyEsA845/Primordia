using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Todas las mutaciones que pueden aparecer en una partida.</summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Mutation Pool", fileName = "MutationPool")]
    public class MutationPool : ScriptableObject
    {
        [SerializeField] List<MutationData> mutations = new();

        public IReadOnlyList<MutationData> Mutations => mutations;

        /// <summary>Hasta <paramref name="count"/> mutaciones distintas al azar, sin las de <paramref name="exclude"/>.</summary>
        public List<MutationData> PickOffer(int count, IEnumerable<MutationData> exclude)
        {
            var excluded = new HashSet<MutationData>(exclude ?? Enumerable.Empty<MutationData>());
            var candidates = mutations.Where(m => m != null && !excluded.Contains(m)).Distinct().ToList();

            // Fisher-Yates: baraja y se queda con las primeras.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            return candidates.Take(count).ToList();
        }

        internal void Configure(IEnumerable<MutationData> newMutations) => mutations = newMutations.ToList();
    }
}

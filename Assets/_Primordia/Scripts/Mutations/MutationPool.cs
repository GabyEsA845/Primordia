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
        [Tooltip("Desmarcado (desde el piso 2): no se ofrecen Bocas nuevas, solo las mejoras de la que lleva el jugador. "
            + "La Boca se elige en la sala de paso y solo se cambia en la sala de cambio.")]
        [SerializeField] bool offersNewMouths = true;

        public IReadOnlyList<MutationData> Mutations => mutations;
        public bool OffersNewMouths => offersNewMouths;

        /// <summary>
        /// Oferta para ese jugador: mutaciones nuevas (las que no lleva) y mejoras (las que lleva y aún pueden
        /// subir de nivel). Si hay alguna mejora posible, al menos una de las cartas es una mejora; las demás salen
        /// al azar entre todo lo que queda. El orden final es al azar.
        /// </summary>
        public List<MutationData> PickOffer(int count, PlayerMutations player)
        {
            if (player == null)
                return PickOffer(count, (IEnumerable<MutationData>)null);

            var candidates = mutations.Where(m => m != null && (!player.IsEquipped(m) || player.CanLevelUp(m))
                && (offersNewMouths || m.Slot != MutationSlot.Mouth || player.IsEquipped(m))).Distinct().ToList();
            // Las especiales no están en la reserva (solo salen de las almejas), pero una vez equipadas sus mejoras sí se ofrecen.
            candidates.AddRange(player.Equipped.Where(m => m != null && m.IsSpecial && player.CanLevelUp(m) && !candidates.Contains(m)));
            var upgrades = candidates.Where(player.IsEquipped).ToList();
            Shuffle(candidates);

            var offer = new List<MutationData>();
            if (upgrades.Count > 0 && count > 0)
            {
                var upgrade = upgrades[Random.Range(0, upgrades.Count)];
                offer.Add(upgrade);
                candidates.Remove(upgrade);
            }

            offer.AddRange(candidates.Take(count - offer.Count));
            Shuffle(offer);
            return offer;
        }

        static void Shuffle<T>(IList<T> list)
        {
            // Fisher-Yates.
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Hasta <paramref name="count"/> mutaciones distintas al azar, sin las de <paramref name="exclude"/> (ni Bocas, si
        /// la reserva no ofrece Bocas nuevas).
        /// </summary>
        public List<MutationData> PickOffer(int count, IEnumerable<MutationData> exclude)
        {
            var excluded = new HashSet<MutationData>(exclude ?? Enumerable.Empty<MutationData>());
            var candidates = mutations.Where(m => m != null && !excluded.Contains(m)
                && (offersNewMouths || m.Slot != MutationSlot.Mouth)).Distinct().ToList();

            // Fisher-Yates: baraja y se queda con las primeras.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            return candidates.Take(count).ToList();
        }

        internal void Configure(IEnumerable<MutationData> newMutations, bool newOffersNewMouths = true)
        {
            mutations = newMutations.ToList();
            offersNewMouths = newOffersNewMouths;
        }
    }
}

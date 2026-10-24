using System.Collections.Generic;
using System.Linq;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cambio de Boca al pasar de piso. La nueva empieza en el nivel I; los niveles que la anterior tenía por encima del I
    /// se reparten al azar, uno a uno, entre las demás mutaciones equipadas que aún puedan subir. Los que no caben en
    /// ninguna se pierden (no vuelven a la Boca): así no compensa llegar al jefe con solo la Boca mejorada.
    /// </summary>
    public static class MouthChange
    {
        /// <summary>Un nivel de la Boca anterior: a qué ranura va (null si se pierde).</summary>
        public readonly struct Transfer
        {
            public readonly MutationSlot? Slot;

            public Transfer(MutationSlot? slot) => Slot = slot;

            public bool Lost => Slot == null;
        }

        /// <summary>Niveles de la Boca actual por encima del I (0 sin Boca).</summary>
        public static int ExtraLevels(PlayerMutations player) =>
            player != null && player.Get(MutationSlot.Mouth) != null ? player.LevelOf(MutationSlot.Mouth) - 1 : 0;

        /// <summary>A dónde va cada nivel extra de la Boca actual, en orden. No cambia nada todavía.</summary>
        public static List<Transfer> Plan(PlayerMutations player, System.Random random)
        {
            var result = new List<Transfer>();
            int extra = ExtraLevels(player);
            if (extra <= 0)
                return result;

            // Nivel que tendrá cada una de las otras ranuras, contando los que ya se le han dado.
            var levels = new[] { MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ }
                .Where(slot => player.Get(slot) != null)
                .ToDictionary(slot => slot, player.LevelOf);

            for (int i = 0; i < extra; i++)
            {
                var open = levels.Where(pair => pair.Value < MutationData.MaxLevel).Select(pair => pair.Key).ToList();
                if (open.Count == 0)
                {
                    result.Add(new Transfer(null));
                    continue;
                }

                var slot = open[random.Next(open.Count)];
                levels[slot]++;
                result.Add(new Transfer(slot));
            }

            return result;
        }

        /// <summary>Equipa la Boca nueva en el nivel I y sube las mutaciones del plan.</summary>
        public static void Apply(PlayerMutations player, MutationData newMouth, IEnumerable<Transfer> plan)
        {
            if (player == null || newMouth == null)
                return;

            player.Equip(newMouth);
            foreach (var transfer in plan)
            {
                if (transfer.Slot is MutationSlot slot)
                    player.LevelUp(slot);
            }
        }
    }
}

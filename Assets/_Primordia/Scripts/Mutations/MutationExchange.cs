using System.Collections.Generic;
using System.Linq;

namespace Primordia.Mutations
{
    /// <summary>
    /// Las reglas de la sala de cambio (el laboratorio de Darwin): entregas la mutación de una ranura y recibes otra del piso
    /// de esa misma ranura, al mismo nivel. Si la que entregas es de un piso anterior, la nueva entra un nivel por debajo
    /// (como mínimo el I), porque el nivel I de un piso vale como el II del anterior. Las especiales de las almejas ni se
    /// entregan ni se reciben.
    /// </summary>
    public static class MutationExchange
    {
        /// <summary>True si la mutación de esa ranura se puede entregar (hay una y no es especial).</summary>
        public static bool CanGive(PlayerMutations player, MutationSlot slot)
        {
            var mutation = player != null ? player.Get(slot) : null;
            return mutation != null && !mutation.IsSpecial;
        }

        /// <summary>Nivel con el que entra la nueva al entregar la de esa ranura en ese piso.</summary>
        public static int LevelFor(PlayerMutations player, MutationSlot slot, int floor)
        {
            var given = player != null ? player.Get(slot) : null;
            if (given == null)
                return 1;
            int level = player.LevelOf(slot);
            return given.Floor < floor ? System.Math.Max(1, level - 1) : level;
        }

        /// <summary>
        /// Por qué se puede cambiar la mutación de esa ranura: las del piso (de la reserva) de esa ranura que no son
        /// especiales ni la que ya llevas. Vacío si esa ranura no se puede entregar.
        /// </summary>
        public static List<MutationData> Options(MutationPool pool, PlayerMutations player, MutationSlot slot, int floor)
        {
            if (pool == null || !CanGive(player, slot))
                return new List<MutationData>();
            var current = player.Get(slot);
            return pool.Mutations
                .Where(m => m != null && m.Slot == slot && !m.IsSpecial && m != current && m.Floor == floor)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Hace el cambio: la de la ranura se va y entra <paramref name="received"/> con el nivel que le toca. Devuelve ese
        /// nivel (0 si no se pudo).
        /// </summary>
        public static int Apply(PlayerMutations player, MutationData received, int floor)
        {
            if (player == null || received == null || !CanGive(player, received.Slot))
                return 0;
            int level = LevelFor(player, received.Slot, floor);
            player.Equip(received);
            for (int i = 1; i < level; i++)
                player.LevelUp(received.Slot);
            return player.LevelOf(received.Slot);
        }

        /// <summary>True si cambiar a esa Boca cambia también el cuerpo (otra familia de arma).</summary>
        public static bool ChangesBody(PlayerMutations player, MutationData received)
        {
            if (player == null || received == null || received.Slot != MutationSlot.Mouth)
                return false;
            var current = player.Get(MutationSlot.Mouth);
            return current == null || current.Family != received.Family;
        }
    }
}

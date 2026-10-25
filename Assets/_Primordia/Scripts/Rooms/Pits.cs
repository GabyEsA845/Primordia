using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Fosas del piso montado: dónde están, para que el jugador y los enemigos que andan caigan en ellas. Lo rellena
    /// <see cref="FloorGenerator"/> al montar el piso y lo vacía al borrarlo.
    /// </summary>
    public static class Pits
    {
        /// <summary>Mapa del piso actual, o null si no hay (menú, pruebas sin piso).</summary>
        public static FloorMap Map { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Map = null;

        /// <summary>True si en esa posición del mundo hay fosa.</summary>
        public static bool IsPit(Vector2 position) => Map != null && Map.Get(NavigationGrid.CellOf(position)) == FloorMap.Kind.Pit;

        /// <summary>True si no hay fosa ni en esa posición ni a <paramref name="margin"/> en ninguna de las cuatro direcciones.</summary>
        public static bool IsSafe(Vector2 position, float margin)
        {
            if (Map == null)
                return true;
            return !IsPit(position) && !IsPit(position + Vector2.up * margin) && !IsPit(position + Vector2.down * margin)
                && !IsPit(position + Vector2.left * margin) && !IsPit(position + Vector2.right * margin);
        }
    }
}

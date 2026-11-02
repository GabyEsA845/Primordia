using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Fosas y lodo profundo del piso montado: dónde están, para que el jugador y los enemigos que andan caigan o se hundan
    /// en ellos. Lo rellena <see cref="FloorGenerator"/> al montar el piso y lo vacía al borrarlo. La fosa tira al instante;
    /// el lodo (piso 3) hunde poco a poco (<see cref="PlayerMudSink"/>), así que para el jugador son cosas distintas y para los
    /// enemigos que andan un mismo agujero (<see cref="IsHole"/>).
    /// </summary>
    public static class Pits
    {
        /// <summary>Mapa del piso actual, o null si no hay (menú, pruebas sin piso).</summary>
        public static FloorMap Map { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Map = null;

        /// <summary>True si en esa posición del mundo hay fosa.</summary>
        public static bool IsPit(Vector2 position) => Map != null && Map.Get(NavigationGrid.CellOf(position)) == FloorMap.Kind.Pit;

        /// <summary>True si en esa posición del mundo hay lodo profundo.</summary>
        public static bool IsMud(Vector2 position) => Map != null && Map.Get(NavigationGrid.CellOf(position)) == FloorMap.Kind.Mud;

        /// <summary>True si hay fosa o lodo profundo: lo que un enemigo que anda no pisa por su cuenta.</summary>
        public static bool IsHole(Vector2 position) => IsPit(position) || IsMud(position);

        /// <summary>
        /// True si esa posición está dentro de la fosa al menos <paramref name="grace"/> por los cuatro lados: asomarse al borde
        /// (y pisar el labio de suelo que dibuja el arte) no hace caer.
        /// </summary>
        public static bool IsDeepInPit(Vector2 position, float grace)
        {
            if (Map == null || !IsPit(position))
                return false;
            return IsPit(position + Vector2.up * grace) && IsPit(position + Vector2.down * grace)
                && IsPit(position + Vector2.left * grace) && IsPit(position + Vector2.right * grace);
        }

        /// <summary>Como <see cref="IsDeepInPit"/> pero con fosa o lodo: para los enemigos que andan.</summary>
        public static bool IsDeepInHole(Vector2 position, float grace)
        {
            if (Map == null || !IsHole(position))
                return false;
            return IsHole(position + Vector2.up * grace) && IsHole(position + Vector2.down * grace)
                && IsHole(position + Vector2.left * grace) && IsHole(position + Vector2.right * grace);
        }

        /// <summary>True si no hay fosa ni en esa posición ni a <paramref name="margin"/> en ninguna de las cuatro direcciones.</summary>
        public static bool IsSafe(Vector2 position, float margin)
        {
            if (Map == null)
                return true;
            return !IsPit(position) && !IsPit(position + Vector2.up * margin) && !IsPit(position + Vector2.down * margin)
                && !IsPit(position + Vector2.left * margin) && !IsPit(position + Vector2.right * margin);
        }

        /// <summary>True si es tierra firme: ni fosa ni lodo en esa posición ni a <paramref name="margin"/> en las cuatro direcciones.</summary>
        public static bool IsFirm(Vector2 position, float margin)
        {
            if (Map == null)
                return true;
            return !IsHole(position) && !IsHole(position + Vector2.up * margin) && !IsHole(position + Vector2.down * margin)
                && !IsHole(position + Vector2.left * margin) && !IsHole(position + Vector2.right * margin);
        }
    }
}

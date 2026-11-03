using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Dónde está "mojado" el jugador a efectos de la Piel anfibia: sobre lodo profundo, un charco de lodo o de agua del escenario o
    /// un charco de veneno.
    /// </summary>
    public static class Wetness
    {
        public static bool IsWet(Vector2 feet) => Pits.IsMud(feet) || GroundPuddle.IsOver(feet) || PoisonPuddle.IsOver(feet);
    }
}

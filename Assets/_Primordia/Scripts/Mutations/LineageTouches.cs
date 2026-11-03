using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Consulta rápida del toque del linaje activo del jugador, para el código de combate (la bola del Escupitajo, el chorro...).</summary>
    public static class LineageTouches
    {
        /// <summary>Toque del linaje activo de quien lleva ese componente (el arma del jugador); None si no hay jugador o linaje.</summary>
        public static LineageTouch Of(Component source)
        {
            if (source == null)
                return LineageTouch.None;
            var lineage = source.GetComponentInParent<PlayerLineage>();
            return lineage != null ? lineage.ActiveTouch : LineageTouch.None;
        }
    }
}

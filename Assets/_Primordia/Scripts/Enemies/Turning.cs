using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Giro limitado de una dirección 2D hacia otra (jefes que se orientan despacio).</summary>
    public static class Turning
    {
        /// <summary>
        /// Gira <paramref name="from"/> hacia <paramref name="to"/> como mucho <paramref name="maxRadians"/>. Si
        /// <paramref name="to"/> queda justo detrás, Vector3.RotateTowards no sabe hacia qué lado girar (se sale del
        /// plano y, de vuelta en 2D, no gira nada): entonces se empieza por el costado izquierdo.
        /// </summary>
        public static Vector2 Towards(Vector2 from, Vector2 to, float maxRadians)
        {
            if (Vector2.Dot(from, to) < -0.9999f)
                to = new Vector2(-from.y, from.x);
            return ((Vector2)Vector3.RotateTowards(from, to, maxRadians, 0f)).normalized;
        }
    }
}

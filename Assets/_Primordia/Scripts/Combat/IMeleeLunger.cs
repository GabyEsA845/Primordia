using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Quien puede lanzarse hacia delante al morder (el jugador con Mandíbulas desde el nivel II).
    /// <see cref="Weapon"/> lo busca en su objeto o en sus padres.
    /// </summary>
    public interface IMeleeLunger
    {
        /// <summary>
        /// Empieza el avance. Devuelve cuánto durará (más corto si choca antes con una pared o un cuerpo), o 0 si
        /// no puede moverse.
        /// </summary>
        float Lunge(Vector2 direction, float distance, float duration);

        /// <summary>Corta el avance (ya ha mordido).</summary>
        void StopLunge();
    }
}

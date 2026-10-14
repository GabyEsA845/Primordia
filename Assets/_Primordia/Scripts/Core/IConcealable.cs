using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Algo que puede ocultarse de los enemigos (el jugador con Camuflaje). Mientras está oculto, los enemigos no
    /// saben dónde está: lo buscan alrededor del último sitio donde lo vieron y no le disparan. Si se les acerca a
    /// menos de <see cref="NoticeRange"/> durante <see cref="NoticeTime"/> segundos, se dan cuenta y lo descubren.
    /// </summary>
    public interface IConcealable
    {
        bool IsConcealed { get; }

        /// <summary>Dónde estaba al ocultarse.</summary>
        Vector2 LastSeenPosition { get; }

        float NoticeRange { get; }
        float NoticeTime { get; }

        /// <summary>Marca sobre el enemigo mientras sospecha ("?"). Puede ser null.</summary>
        GameObject SuspectMarker { get; }

        /// <summary>Un enemigo lo ha descubierto: deja de estar oculto.</summary>
        void Reveal();
    }
}

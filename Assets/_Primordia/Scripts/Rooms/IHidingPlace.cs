using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Un sitio donde un enemigo puede esconderse (el helecho de la Araña de las frondas). Mientras alguien se esconde dentro, el
    /// sitio lo delata temblando; si deja de estar entero (el helecho arde), el escondido sale.
    /// </summary>
    public interface IHidingPlace
    {
        Vector2 Position { get; }

        /// <summary>False si ya no sirve de escondite (ardiendo o quemado).</summary>
        bool Intact { get; }

        /// <summary>True mientras hay alguien dentro: el sitio tiembla.</summary>
        bool Trembling { get; set; }
    }
}

using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Lo que puede arder (piso 3): la maleza que sella atajos y los helechos. Quien produce fuego (el fuego en el suelo, una
    /// explosión de gas, un ataque de fuego) busca lo inflamable cercano con <see cref="Flammables"/> y llama a
    /// <see cref="Ignite"/>.
    /// </summary>
    public interface IFlammable
    {
        /// <summary>Dónde está, en el mundo.</summary>
        Vector2 Position { get; }

        /// <summary>False si ya está ardiendo o quemado.</summary>
        bool CanIgnite { get; }

        /// <summary>Lo prende. No hace nada si no se puede.</summary>
        void Ignite();
    }
}

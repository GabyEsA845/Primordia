using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Metano que se puede volver nube tóxica en lugar de explotar (Aguijón de gas, del Pulmonoscorpius): la nube del Saco, el odre y la
    /// niebla. Se registra en <see cref="Flammables"/> como el resto de lo inflamable.
    /// </summary>
    public interface IToxifiable
    {
        bool CanToxify { get; }

        /// <summary>Zona que ocupa, en el mundo.</summary>
        Bounds ToxifyBounds { get; }

        /// <summary>Se vuelve nube tóxica sin explotar.</summary>
        void Toxify();
    }
}

namespace Primordia.Rooms
{
    /// <summary>
    /// Lo que puede arder (piso 3): la maleza que sella atajos y, desde el paso de las reacciones, los helechos. Quien produce
    /// fuego (un ataque de fuego, una explosión, un charco ardiendo) llama a <see cref="Ignite"/>.
    /// </summary>
    public interface IFlammable
    {
        /// <summary>False si ya está ardiendo o quemado.</summary>
        bool CanIgnite { get; }

        /// <summary>Lo prende. No hace nada si no se puede.</summary>
        void Ignite();
    }
}

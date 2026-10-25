using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>Importación del arte de las mutaciones especiales de las almejas (la usa <see cref="SpecialMutationsSetup"/>).</summary>
    public static partial class ArtSetup
    {
        /// <summary>Copia la imagen de la carpeta temporal y la importa como un solo sprite de pixel art.</summary>
        internal static Sprite ImportLooseSprite(string source, string target) => ImportSprite(source, target, Vector4.zero);

        /// <summary>Copia la tira de cuadros y la corta en sprites de ese ancho.</summary>
        internal static Sprite[] ImportLooseSheet(string source, string target, int cellWidth) =>
            CopyArt(source, target) ? SliceSheet(target, cellWidth) : System.Array.Empty<Sprite>();

        /// <summary>Como <see cref="ImportLooseSheet(string, string, int)"/>, con el pivote de cada cuadro en ese punto (0–1).</summary>
        internal static Sprite[] ImportLooseSheet(string source, string target, int cellWidth, Vector2 pivot) =>
            CopyArt(source, target) ? SliceSheet(target, cellWidth, pivot) : System.Array.Empty<Sprite>();
    }
}

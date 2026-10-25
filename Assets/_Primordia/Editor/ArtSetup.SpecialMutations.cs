using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>Importación del arte de las mutaciones especiales de las almejas (la usa <see cref="SpecialMutationsSetup"/>).</summary>
    public static partial class ArtSetup
    {
        /// <summary>
        /// Animaciones de una capa sobre un cuerpo de criatura (la Cutícula sobre la medusa…): hojas
        /// <c>{name}_Idle/Move/Attack_side/down/up.png</c> de esa carpeta, al ritmo del cuerpo (6, 10 y 14 cuadros por segundo).
        /// Null si no están las hojas.
        /// </summary>
        internal static RuntimeAnimatorController ImportCreatureLayer(string name, string sourceFolder, string artFolder)
        {
            if (!System.IO.Directory.Exists(sourceFolder) || System.IO.Directory.GetFiles(sourceFolder, name + "_*.png").Length == 0)
                return null;
            var art = Import(new Character
            {
                Name = name,
                SourceFolder = sourceFolder,
                ArtFolder = artFolder,
                DefaultState = "Idle_down",
                Timings = new[] { ("Idle", 6f, true), ("Move", 10f, true), ("Attack", 14f, false) },
            });
            return art != null ? art.Controller : null;
        }

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

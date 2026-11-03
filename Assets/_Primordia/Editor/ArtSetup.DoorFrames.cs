using System.IO;
using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Coronas de las puertas de arriba, marcos de las puertas laterales y adornos de las paredes laterales de los 10 temas de los
    /// pisos 1-3 (docs/multimedia-temp/doors-frames, una carpeta por tema): los copia a Assets/_Primordia/Art/DoorFrames/&lt;Tema&gt; y
    /// los pone en el <see cref="RoomTheme"/> de cada uno. No toca nada más del tema; se puede repetir sin miedo.
    /// </summary>
    public static partial class ArtSetup
    {
        const string DoorFramesSourceFolder = "docs/multimedia-temp/doors-frames";
        const string DoorFramesArtFolder = "Assets/_Primordia/Art/DoorFrames";

        // Piso 1: Soup, Boss, Amoeba. Piso 2: Shore (sala de paso), Shallows, Cambrian, Abyss. Piso 3: Swamp, Smoke, Burnt.
        static readonly string[] DoorFrameThemes =
        {
            "Soup", "Boss", "Amoeba", "Shore", "Shallows", "Cambrian", "Abyss", "Swamp", "Smoke", "Burnt",
        };

        [MenuItem("Primordia/Art/Importar coronas y marcos de las puertas")]
        internal static void ImportDoorFrames()
        {
            int done = 0;
            foreach (string name in DoorFrameThemes)
            {
                var theme = FindTheme(name);
                if (theme == null)
                {
                    Debug.LogWarning($"ArtSetup: no encuentro el tema {name} (crea antes el arte de su piso); sus puertas se quedan como estaban.");
                    continue;
                }

                string source = $"{DoorFramesSourceFolder}/{name}";
                string target = $"{DoorFramesArtFolder}/{name}";
                Sprite Frame(string file) => ImportSprite($"{source}/{file}.png", $"{target}/{file}.png", Vector4.zero);

                var crown = Frame("Doorway_Top_Crown");
                var back = Frame("Doorway_Side_Back");
                var front = Frame("Doorway_Side_Front");
                var deco = new[] { Frame("Wall_Side_Deco_0"), Frame("Wall_Side_Deco_1"), Frame("Wall_Side_Deco_2") };
                if (crown == null || back == null || front == null || deco.Any(sprite => sprite == null))
                {
                    Debug.LogWarning($"ArtSetup: faltan piezas de las puertas del tema {name} en {source}.");
                    continue;
                }

                theme.ConfigureDoorFrames(crown, back, front, deco);
                EditorUtility.SetDirty(theme);
                done++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: coronas y marcos de las puertas puestos en {done} de {DoorFrameThemes.Length} temas.");
        }

        internal static RoomTheme FindTheme(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets($"Theme_{name} t:RoomTheme"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == $"Theme_{name}")
                    return AssetDatabase.LoadAssetAtPath<RoomTheme>(path);
            }
            return null;
        }
    }
}

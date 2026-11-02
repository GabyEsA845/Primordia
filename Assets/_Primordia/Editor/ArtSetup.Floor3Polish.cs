using Primordia.Core;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Pulido del piso 3 (docs/multimedia-temp/floor3-polish): lodo en el cuerpo, llamas, iconos de barras, agua de charca,
    /// ráfaga de la fumarola y capas de las puertas. Importa las hojas a Art/Floor3/Polish y las reúne en Resources/PolishArt.
    /// </summary>
    public static partial class ArtSetup
    {
        const string PolishSourceFolder = "docs/multimedia-temp/floor3-polish";
        const string PolishArtFolder = "Assets/_Primordia/Art/Floor3/Polish";
        const string PolishAssetPath = "Assets/_Primordia/Resources/PolishArt.asset";

        static readonly string[] DoorThemes =
            { "Soup", "Amoeba", "Shallows", "Cambrian", "Abyss", "Shore", "Boss", "Swamp", "Smoke", "Burnt" };

        [MenuItem("Primordia/Art/Importar pulido del piso 3")]
        internal static void ImportFloor3Polish()
        {
            System.IO.Directory.CreateDirectory(PolishArtFolder);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PolishAssetPath));

            var center = new Vector2(0.5f, 0.5f);
            var feet = new Vector2(0.5f, 0f);
            var top = new Vector2(0.5f, 1f);
            var right = new Vector2(1f, 0.5f);

            var art = AssetDatabase.LoadAssetAtPath<PolishArt>(PolishAssetPath);
            bool isNew = art == null;
            if (isNew)
                art = ScriptableObject.CreateInstance<PolishArt>();

            // Lodo en el cuerpo. La textura y el ruido se repiten (el sombreador los lee con coordenadas del mundo).
            art.mudTexture = First(PolishSheet("Effects", "Fx_MudBody_Tex", 16, center, repeat: true));
            art.mudNoise = First(PolishSheet("Effects", "Fx_MudBody_Noise", 32, center, repeat: true));
            art.mudRingFront = new PolishArt.Sized
            {
                s = PolishSheet("Effects", "Fx_MudRing_S", 16, center),
                m = PolishSheet("Effects", "Fx_MudRing_M", 30, center),
                l = PolishSheet("Effects", "Fx_MudRing_L", 44, center),
            };
            art.mudRingBack = new PolishArt.Sized
            {
                s = PolishSheet("Effects", "Fx_MudRing_S_Back", 16, center),
                m = PolishSheet("Effects", "Fx_MudRing_M_Back", 30, center),
                l = PolishSheet("Effects", "Fx_MudRing_L_Back", 44, center),
            };
            art.mudBubble = PolishSheet("Effects", "Fx_MudBubble", 8, feet);
            art.mudDrip = PolishSheet("Effects", "Fx_MudDrip", 6, top);

            // Llamas.
            art.bodyFlame = new PolishArt.Sized
            {
                s = PolishSheet("Effects", "Fx_BodyFlame_S", 16, feet),
                m = PolishSheet("Effects", "Fx_BodyFlame_M", 24, feet),
                l = PolishSheet("Effects", "Fx_BodyFlame_L", 32, feet),
            };
            art.flameLick = PolishSheet("Effects", "Fx_FlameLick", 8, feet);

            // Iconos de las barras (el vacío y, encima, el lleno recortado).
            art.burnEmpty = First(PolishSheet("UI", "Icon_StatusBurn_Empty", 0, center));
            art.burnFull = First(PolishSheet("UI", "Icon_StatusBurn_Full", 0, center));
            art.toxicEmpty = First(PolishSheet("UI", "Icon_StatusToxic_Empty", 0, center));
            art.toxicFull = First(PolishSheet("UI", "Icon_StatusToxic_Full", 0, center));

            // Agua de charca y fumarola.
            art.waterRipple = PolishSheet("Effects", "Fx_WaterRipple", 20, center);
            art.waterSplash = PolishSheet("Effects", "Fx_WaterSplash", 24, feet);
            art.waterDrip = PolishSheet("Effects", "Fx_WaterDrip", 4, top);
            art.steamHiss = PolishSheet("Effects", "Fx_SteamHiss", 32, feet);
            art.ventGust = PolishSheet("Effects", "Fx_VentGust", 24, right);

            // Capas de las puertas, una pareja por tema.
            var doors = new PolishArt.DoorLayers[DoorThemes.Length];
            for (int i = 0; i < DoorThemes.Length; i++)
            {
                string theme = DoorThemes[i];
                doors[i] = new PolishArt.DoorLayers
                {
                    theme = theme,
                    front = First(PolishSheet($"Doors/{theme}", "Doorway_Top_Front", 0, center, $"Door_{theme}_")),
                    shade = First(PolishSheet($"Doors/{theme}", "Doorway_Top_Shade", 0, center, $"Door_{theme}_")),
                };
            }
            art.doors = doors;

            if (isNew)
                AssetDatabase.CreateAsset(art, PolishAssetPath);
            else
                EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();

            // La marca de «arde» (jugador y enemigos) pasa a llevar las llamas por tamaño y el velo naranja.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BurningMarkerPrefabPath) != null)
                EditPrefab(BurningMarkerPrefabPath, obj => GetOrAdd<BurningMarker>(obj));
            else
                Debug.LogWarning("ArtSetup: no existe Effect_Burning; ejecuta antes Primordia > Floor 3 > Crear reacciones (fuego) y vuelve a importar el pulido.");
            Debug.Log("ArtSetup: pulido del piso 3 importado en Resources/PolishArt (lodo, llamas, iconos, agua, fumarola y puertas).");
        }

        /// <summary>
        /// Copia una hoja de floor3-polish a Art/Floor3/Polish y la corta. cellWidth 0 = cuadrada. Las hojas de las puertas
        /// llevan el tema en el nombre del archivo copiado, porque todas se llaman igual.
        /// </summary>
        static Sprite[] PolishSheet(string group, string name, int cellWidth, Vector2 pivot, string prefix = "", bool repeat = false)
        {
            string file = $"{prefix}{name}.png";
            string target = $"{PolishArtFolder}/{file}";
            if (!CopyArt($"{PolishSourceFolder}/{group}/{name}.png", target))
                return System.Array.Empty<Sprite>();

            if (repeat)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(target);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            return SliceSheet(target, cellWidth, pivot);
        }
    }
}

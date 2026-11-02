using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Editor
{
    /// <summary>
    /// Tilesets del piso 3 (Swamp, Smoke, Burnt): crea un <see cref="RoomTheme"/> por tramo con los tiles, las variantes de la
    /// tapa, las caras en pares, los bordes junto a la cara, la decoración del suelo y las puertas de raíces. Parte del arte
    /// que ya importó Primordia > Floor 3 > Importar todo el arte del piso 3. Obstáculos, lodo y maleza llegan con sus subtandas.
    /// </summary>
    public static partial class ArtSetup
    {
        internal const string SwampThemeName = "Swamp";
        internal const string SmokeThemeName = "Smoke";
        internal const string BurntThemeName = "Burnt";

        static readonly string[] Floor3ThemeNames = { SwampThemeName, SmokeThemeName, BurntThemeName };

        const string Floor3EnvironmentFolder = Floor3ArtRoot + "/floor3-environment";

        // Más hierba y detalles que en el piso 1: el pantano va poblado.
        const float Floor3DecorationChance = 0.09f;

        [MenuItem("Primordia/Floor 3/Crear tilesets del piso 3")]
        static void ImportFloor3ThemesMenu()
        {
            var themes = ImportFloor3Themes();
            Debug.Log($"ArtSetup: {themes.Count(theme => theme != null)} de 3 tilesets del piso 3 listos (Swamp, Smoke, Burnt).");
        }

        /// <summary>Los tres tramos del piso 3 en orden (pantano, bosque humeante, tierra quemada). Null en el que no tenga arte.</summary>
        internal static RoomTheme[] ImportFloor3Themes()
        {
            var missing = CreateMissingTile();
            var themes = Floor3ThemeNames.Select(name => ImportFloor3Theme(name, missing)).ToArray();
            AssetDatabase.SaveAssets();
            return themes;
        }

        internal static RoomTheme LoadFloor3Theme(string themeName) =>
            AssetDatabase.LoadAssetAtPath<RoomTheme>($"{Floor3EnvironmentFolder}/{themeName}/Theme_{themeName}.asset");

        // Burbujas del lodo: 6 cuadros de 16×16 a ~140 ms.
        const float MudBubblesFps = 1f / 0.14f;

        /// <summary>
        /// Lodo profundo del tramo (README del escenario): 16 tiles por máscara de vecinos que no son lodo, las esquinas de
        /// dentro (mismo orden que las fosas: NE, NO, SE, SO) y las burbujas. Sin ellos el lodo se dibuja con el suelo teñido.
        /// </summary>
        static void ImportMud(RoomTheme theme, string folder, string tileAssets)
        {
            if (!File.Exists($"{folder}/Mud_0.png"))
            {
                theme.ConfigureMud(null, null, null);
                return;
            }

            TileBase Single(string name) =>
                File.Exists($"{folder}/{name}.png") ? CreateTile($"{tileAssets}/{name}.asset", ImportSingleSprite($"{folder}/{name}.png"), collides: false) : null;

            var tiles = Enumerable.Range(0, 16).Select(mask => Single($"Mud_{mask}")).ToArray();
            var corners = PitCorners.Select(corner => Single($"Mud_Corner_{corner}")).ToArray();
            TileBase bubbles = null;
            if (File.Exists($"{folder}/Mud_Bubbles.png"))
                bubbles = CreateAnimatedTile($"{tileAssets}/Mud_Bubbles.asset", SliceSheet($"{folder}/Mud_Bubbles.png", 16), MudBubblesFps);
            theme.ConfigureMud(tiles, corners, bubbles);
        }

        static RoomTheme ImportFloor3Theme(string themeName, TileBase missing)
        {
            string folder = $"{Floor3EnvironmentFolder}/{themeName}";
            string tileAssets = $"{folder}/TileAssets";
            if (!Directory.Exists($"{folder}/tiles"))
            {
                Debug.LogWarning($"ArtSetup: no hay tiles de {themeName} en {folder}; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return null;
            }
            Directory.CreateDirectory(tileAssets);

            // Tiles sueltos: las paredes con colisión, el suelo sin ella.
            var tiles = new Dictionary<string, TileBase>();
            foreach (var path in AssetPaths($"{folder}/tiles", "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                tiles[name] = CreateTile($"{tileAssets}/{name}.asset", ImportSingleSprite(path), collides: name.StartsWith("Wall_"));
            }

            TileBase Named(string name) => tiles.TryGetValue(name, out var tile) ? tile : null;
            TileBase[] Numbered(string prefix) => tiles
                .Where(pair => Regex.IsMatch(pair.Key, $"^{prefix}_\\d+$"))
                .OrderBy(pair => int.Parse(pair.Key.Substring(prefix.Length + 1)))
                .Select(pair => pair.Value)
                .ToArray();

            // Detalles del suelo: Deco_Ground es una hoja de 6 variantes de 16×16.
            var decorations = new List<TileBase>();
            string groundPath = $"{folder}/props/Deco_Ground.png";
            if (File.Exists(groundPath))
                decorations.AddRange(SliceSheet(groundPath, 16).Select(sprite => CreateTile($"{tileAssets}/{sprite.name}.asset", sprite, collides: false)));

            // Arco y puertas de raíces: como las losas del piso 2 (cerrar y abrir con duración por cuadro).
            var doors = new Dictionary<string, Sprite[]>();
            foreach (var path in AssetPaths(folder, "Door*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                doors[name] = name == "Doorway_Top"
                    ? new[] { ImportSingleSprite(path) }
                    : SliceSheet(path, EnvironmentCellWidths.TryGetValue(name, out int width) ? width : 0);
            }

            string themePath = $"{folder}/Theme_{themeName}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<RoomTheme>();
                AssetDatabase.CreateAsset(theme, themePath);
            }

            theme.Configure(Named("Floor_0"), Numbered("Floor").Where(tile => tile != Named("Floor_0")).ToArray(),
                Numbered("Floor_ShadowTop"), Named("Wall_Cap"), Numbered("Wall_FaceUpper"), Numbered("Wall_FaceLower"),
                Named("Wall_EdgeLeft"), Named("Wall_EdgeRight"), Named("Wall_EdgeBottom"),
                Named("Wall_CornerBL"), Named("Wall_CornerBR"), missing);
            theme.ConfigureShapes(Named("Wall_FaceUpper_EndLeft"), Named("Wall_FaceUpper_EndRight"),
                Named("Wall_FaceLower_EndLeft"), Named("Wall_FaceLower_EndRight"),
                Named("Wall_OuterTopLeft"), Named("Wall_OuterTopRight"));
            theme.ConfigureWallExtras(Numbered("Wall_Cap"), Named("Wall_EdgeLeft_FaceUpper"), Named("Wall_EdgeLeft_FaceLower"),
                Named("Wall_EdgeRight_FaceUpper"), Named("Wall_EdgeRight_FaceLower"));
            theme.ConfigureDecorations(decorations.ToArray(), Floor3DecorationChance);
            theme.ConfigureDoors(doors.TryGetValue("Doorway_Top", out var doorway) ? doorway[0] : null, null, null, null);

            DoorAnimation Roots(string side) =>
                doors.TryGetValue($"Door_{side}_Close", out var close) && doors.TryGetValue($"Door_{side}_Open", out var open)
                    ? new DoorAnimation(close, SlabCloseDurations, open, SlabOpenDurations)
                    : null;
            theme.ConfigureSlabs(Roots("Top"), Roots("Bottom"), Roots("Side"));
            ImportMud(theme, $"{folder}/mud", tileAssets);
            EditorUtility.SetDirty(theme);

            Debug.Log($"ArtSetup: tema {themeName}: {tiles.Count} tiles, {decorations.Count} detalles de suelo, {doors.Count} hojas de puertas, "
                + $"{Numbered("Wall_Cap").Length} variantes de tapa, {Numbered("Wall_FaceUpper").Length} caras en pares.");
            return theme;
        }
    }
}

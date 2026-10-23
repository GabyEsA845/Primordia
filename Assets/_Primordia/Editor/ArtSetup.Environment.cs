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
    /// Importa el escenario (tiles, decoración y puertas) de docs/multimedia-temp/environment y crea un
    /// <see cref="RoomTheme"/> por variante: Soup (salas normales) y Boss (sala del jefe).
    /// </summary>
    public static partial class ArtSetup
    {
        const string EnvironmentSourceFolder = "docs/multimedia-temp/environment";
        // Piezas extra para salas con formas, en una carpeta aparte con las mismas subcarpetas (Soup/tiles…).
        const string EnvironmentShapesSourceFolder = "docs/multimedia-temp/environment-shapes";
        const string EnvironmentArtFolder = "Assets/_Primordia/Art/Environment";
        const string MissingTilePath = EnvironmentArtFolder + "/Tile_Missing.asset";
        const float DecorationChance = 0.05f;

        static readonly string[] Themes = { "Soup", "Boss", AmoebaThemeName };

        // Decoración animada, en cuadros por segundo (README del arte: burbujas ~400 ms, algas ~200 ms).
        static readonly Dictionary<string, float> AnimatedDecorations = new() { ["Deco_Bubbles"] = 2.5f, ["Deco_Algae"] = 5f };

        // Hojas cuyas celdas no son cuadradas (ancho en píxeles).
        static readonly Dictionary<string, int> EnvironmentCellWidths = new()
        {
            ["Deco_Goo"] = 32,
            ["Door_Bottom"] = 32,
            ["Door_Side"] = 16,
        };

        [MenuItem("Primordia/Art/Importar escenario")]
        static void ImportEnvironment()
        {
            Directory.CreateDirectory(EnvironmentArtFolder);
            var missing = CreateMissingTile();

            int imported = Themes.Count(theme => ImportTheme(theme, missing));
            AssetDatabase.SaveAssets();

            if (imported == 0)
                EditorUtility.DisplayDialog("Primordia", $"No hay escenario en {EnvironmentSourceFolder} ni en {EnvironmentArtFolder}.", "Vale");
            else
                Debug.Log($"ArtSetup: escenario importado ({imported} temas). Regenera el piso para verlo (Primordia > UI > Regenerar escena Game).");
        }

        static bool ImportTheme(string themeName, TileBase missing)
        {
            string source = $"{EnvironmentSourceFolder}/{themeName}";
            string target = $"{EnvironmentArtFolder}/{themeName}";
            string tileAssets = $"{target}/TileAssets";

            CopyFiles($"{source}/tiles", $"{target}/Tiles", "*.png");
            CopyFiles($"{EnvironmentShapesSourceFolder}/{themeName}/tiles", $"{target}/Tiles", "*.png");
            CopyFiles($"{source}/deco", $"{target}/Deco", "Deco_*.png");
            CopyFiles(source, $"{target}/Doors", "Door*.png");

            if (!Directory.Exists($"{target}/Tiles"))
            {
                Debug.LogWarning($"ArtSetup: no hay tiles de {themeName} en {source} ni en {target}.");
                return false;
            }

            Directory.CreateDirectory(tileAssets);

            // Tiles sueltos: las paredes con colisión, el suelo sin ella.
            var tiles = new Dictionary<string, TileBase>();
            foreach (var path in AssetPaths($"{target}/Tiles", "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                tiles[name] = CreateTile($"{tileAssets}/{name}.asset", ImportSingleSprite(path), collides: name.StartsWith("Wall_"));
            }

            var decorations = new List<TileBase>();
            foreach (var path in AssetPaths($"{target}/Deco", "Deco_*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var sprites = SliceSheet(path, EnvironmentCellWidths.TryGetValue(name, out int width) ? width : 0);
                if (AnimatedDecorations.TryGetValue(name, out float fps))
                    decorations.Add(CreateAnimatedTile($"{tileAssets}/{name}.asset", sprites, fps));
                else
                    decorations.AddRange(sprites.Select(sprite => CreateTile($"{tileAssets}/{sprite.name}.asset", sprite, collides: false)));
            }

            var doors = new Dictionary<string, Sprite[]>();
            foreach (var path in AssetPaths($"{target}/Doors", "Door*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                doors[name] = name == "Doorway_Top"
                    ? new[] { ImportSingleSprite(path) }
                    : SliceSheet(path, EnvironmentCellWidths.TryGetValue(name, out int width) ? width : 0);
            }

            string themePath = $"{target}/Theme_{themeName}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<RoomTheme>();
                AssetDatabase.CreateAsset(theme, themePath);
            }

            TileBase Named(string name) => tiles.TryGetValue(name, out var tile) ? tile : null;
            TileBase[] Numbered(string prefix) => tiles
                .Where(pair => Regex.IsMatch(pair.Key, $"^{prefix}_\\d+$"))
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value)
                .ToArray();

            theme.Configure(Named("Floor_0"), Numbered("Floor").Where(tile => tile != Named("Floor_0")).ToArray(),
                Numbered("Floor_ShadowTop"), Named("Wall_Cap"), Numbered("Wall_FaceUpper"), Numbered("Wall_FaceLower"),
                Named("Wall_EdgeLeft"), Named("Wall_EdgeRight"), Named("Wall_EdgeBottom"),
                Named("Wall_CornerBL"), Named("Wall_CornerBR"), missing);
            theme.ConfigureShapes(Named("Wall_FaceUpper_EndLeft"), Named("Wall_FaceUpper_EndRight"),
                Named("Wall_FaceLower_EndLeft"), Named("Wall_FaceLower_EndRight"),
                Named("Wall_OuterTopLeft"), Named("Wall_OuterTopRight"));
            theme.ConfigureDecorations(decorations.ToArray(), DecorationChance);
            theme.ConfigureDoors(doors.TryGetValue("Doorway_Top", out var doorway) ? doorway[0] : null,
                doors.GetValueOrDefault("Door_Top"), doors.GetValueOrDefault("Door_Bottom"), doors.GetValueOrDefault("Door_Side"));
            EditorUtility.SetDirty(theme);

            Debug.Log($"ArtSetup: tema {themeName}: {tiles.Count} tiles, {decorations.Count} decoraciones, {doors.Count} hojas de puertas.");
            return true;
        }

        static void CopyFiles(string sourceFolder, string targetFolder, string pattern)
        {
            if (!Directory.Exists(sourceFolder))
                return;

            Directory.CreateDirectory(targetFolder);
            foreach (var source in Directory.GetFiles(sourceFolder, pattern))
            {
                var target = $"{targetFolder}/{Path.GetFileName(source)}";
                File.Copy(source, target, overwrite: true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            }
        }

        static IEnumerable<string> AssetPaths(string folder, string pattern) =>
            Directory.Exists(folder)
                ? Directory.GetFiles(folder, pattern).Select(path => path.Replace('\\', '/')).OrderBy(path => path)
                : Enumerable.Empty<string>();

        /// <summary>Importa una imagen como un único sprite de pixel art con el pivote en el centro.</summary>
        static Sprite ImportSingleSprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Tile CreateTile(string path, Sprite sprite, bool collides)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.colliderType = collides ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static AnimatedTile CreateAnimatedTile(string path, Sprite[] sprites, float framesPerSecond)
        {
            var tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<AnimatedTile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.m_AnimatedSprites = sprites;
            tile.m_MinSpeed = framesPerSecond;
            tile.m_MaxSpeed = framesPerSecond;
            tile.m_TileColliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>Tile magenta para las celdas que el arte aún no cubre: se ven enseguida al probar.</summary>
        static Tile CreateMissingTile()
        {
            var tile = CreateTile(MissingTilePath, CoreSetup.CreateSquareSprite(), collides: true);
            tile.color = Color.magenta;
            EditorUtility.SetDirty(tile);
            return tile;
        }
    }
}

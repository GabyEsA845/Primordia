using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Editor
{
    /// <summary>
    /// Importa el escenario (tiles, decoración y puertas) de docs/multimedia-temp/environment y crea un
    /// <see cref="RoomTheme"/> por variante: Soup (salas normales), Boss (sala del jefe), Amoeba (sala de la ameba) y,
    /// para la sala de paso, Shore (intermedio) y Cambrian (océano cámbrico, el del piso 2).
    /// </summary>
    public static partial class ArtSetup
    {
        const string EnvironmentSourceFolder = "docs/multimedia-temp/environment";
        // Piezas extra para salas con formas, en una carpeta aparte con las mismas subcarpetas (Soup/tiles…).
        const string EnvironmentShapesSourceFolder = "docs/multimedia-temp/environment-shapes";
        const string EnvironmentArtFolder = "Assets/_Primordia/Art/Environment";
        const string MissingTilePath = EnvironmentArtFolder + "/Tile_Missing.asset";
        const float DecorationChance = 0.05f;

        // Franjas del piso 2: Someras, Arrecife (el tema Cambrian de la sala de paso) y Abismo.
        internal const string ShallowsThemeName = "Shallows";
        internal const string AbyssThemeName = "Abyss";

        static readonly string[] Themes =
            { "Soup", "Boss", AmoebaThemeName, ShoreThemeName, CambrianThemeName, ShallowsThemeName, AbyssThemeName };

        // Decoración animada, en cuadros por segundo (README del arte: burbujas ~400 ms, algas ~200 ms).
        static readonly Dictionary<string, float> AnimatedDecorations = new()
        {
            ["Deco_Bubbles"] = 2.5f, ["Deco_Algae"] = 5f, ["Deco_Crinoid"] = 4f,
            // Piso 2 (README del escenario): hierba marina ~200 ms, pluma de mar ~250 ms, medusa peine ~150 ms, motas ~300 ms.
            ["Deco_Seagrass"] = 5f, ["Deco_SeaPen"] = 4f, ["Deco_Ctenophore"] = 1f / 0.15f, ["Deco_GlowMotes"] = 1f / 0.3f,
        };

        // El fondo marino va más poblado que la sopa: así se nota el cambio al acercarse a la fosa.
        static readonly Dictionary<string, float> ThemeDecorationChances = new()
        {
            [ShoreThemeName] = 0.06f, [CambrianThemeName] = 0.09f, [ShallowsThemeName] = 0.09f, [AbyssThemeName] = 0.08f,
        };

        // Hojas cuyas celdas no son cuadradas (ancho en píxeles).
        static readonly Dictionary<string, int> EnvironmentCellWidths = new()
        {
            ["Deco_Goo"] = 32,
            ["Door_Bottom"] = 32,
            ["Door_Side"] = 16,
            ["Door_Bottom_Close"] = 32,
            ["Door_Bottom_Open"] = 32,
            ["Door_Side_Close"] = 16,
            ["Door_Side_Open"] = 16,
            ["Overlay_LightRay"] = 48,
        };

        // Puertas de losas (README del escenario): duración de cada cuadro de cerrar y de abrir, en milisegundos.
        static readonly float[] SlabCloseDurations = { 0.07f, 0.06f, 0.06f, 0.06f, 0.06f, 0.11f, 0.14f };
        static readonly float[] SlabOpenDurations = { 0.13f, 0.09f, 0.06f, 0.06f, 0.06f, 0.06f };

        // Ambiente del piso 2: cáusticas a ~180 ms por cuadro (mosaico de 64 px), rayos a ~250 ms y copos por sala normal.
        const float CausticsFrameSeconds = 0.18f;
        const int CausticsPeriod = 64;
        const float LightRayFrameSeconds = 0.25f;
        static readonly Dictionary<string, int> MarineSnowPerTheme = new()
        {
            [ShallowsThemeName] = 10, [CambrianThemeName] = 30, [AbyssThemeName] = 70,
        };

        // Bioluminiscencia del Abismo (README): pieza, color, radio, intensidad mínima y máxima y latido en segundos.
        static readonly (string piece, string color, float radius, Vector2 intensity, float period)[] AbyssGlows =
        {
            ("Deco_SeaPen", "#6ae8f0", 1.5f, new Vector2(0.4f, 0.7f), 1f),
            ("Deco_Ctenophore", "#a0c8ff", 1f, new Vector2(0.3f, 0.3f), 0f),
            ("Obstacle_Spire", "#6ae8f0", 2f, new Vector2(0.3f, 0.3f), 0f),
        };

        const string OceanArtFolder = EnvironmentArtFolder + "/Ocean";
        const string MarineSnowMaterialPath = OceanArtFolder + "/Mat_MarineSnow.mat";

        /// <summary>Importa las tres franjas del piso 2 (Someras, Arrecife, Abismo). Las que no tengan tiles salen null.</summary>
        internal static RoomTheme[] ImportFloor2Themes()
        {
            Directory.CreateDirectory(EnvironmentArtFolder);
            var missing = CreateMissingTile();
            var names = new[] { ShallowsThemeName, CambrianThemeName, AbyssThemeName };
            var imported = names.Select(name => ImportTheme(name, missing)).ToArray();
            AssetDatabase.SaveAssets();
            return names.Select((name, i) => imported[i] ? LoadTheme(name) : null).ToArray();
        }

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
            CopyFiles($"{source}/deco", $"{target}/Obstacles", "Obstacle_*.png");
            CopyFiles($"{source}/deco", $"{target}/Ambience", "Overlay_*.png");
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
            theme.ConfigureDecorations(decorations.ToArray(), ThemeDecorationChances.GetValueOrDefault(themeName, DecorationChance));
            theme.ConfigureDoors(doors.TryGetValue("Doorway_Top", out var doorway) ? doorway[0] : null,
                doors.GetValueOrDefault("Door_Top"), doors.GetValueOrDefault("Door_Bottom"), doors.GetValueOrDefault("Door_Side"));

            DoorAnimation Slab(string side) =>
                doors.TryGetValue($"Door_{side}_Close", out var close) && doors.TryGetValue($"Door_{side}_Open", out var open)
                    ? new DoorAnimation(close, SlabCloseDurations, open, SlabOpenDurations)
                    : null;
            theme.ConfigureSlabs(Slab("Top"), Slab("Bottom"), Slab("Side"));

            // Obstáculos: la roca pequeña de 1×1; las grandes (roca y las propias del tema) de 2×2.
            var obstacles = AssetPaths($"{target}/Obstacles", "Obstacle_*.png")
                .ToDictionary(path => Path.GetFileNameWithoutExtension(path), ImportSingleSprite);
            var outlines = obstacles.Keys.ToDictionary(name => name, name => CreateOutline($"{target}/Obstacles/{name}.png", $"{target}/Obstacles/Outlines"));
            var smallNames = obstacles.Keys.Where(name => name == "Obstacle_Rock").ToArray();
            var bigNames = obstacles.Keys.Where(name => name != "Obstacle_Rock").ToArray();
            theme.ConfigureObstacles(smallNames.Select(name => obstacles[name]).ToArray(), bigNames.Select(name => obstacles[name]).ToArray(),
                smallNames.Select(name => outlines[name]).ToArray(), bigNames.Select(name => outlines[name]).ToArray());

            var caustics = ImportCaustics($"{target}/Ambience/Overlay_Caustics.png", tileAssets);
            string rayPath = $"{target}/Ambience/Overlay_LightRay.png";
            var lightRay = File.Exists(rayPath) ? SliceSheet(rayPath, EnvironmentCellWidths["Overlay_LightRay"]) : null;
            var glows = themeName == AbyssThemeName
                ? AbyssGlows.Select(entry => Glow(entry, decorations, obstacles)).Where(glow => glow.source != null).ToArray()
                : null;
            theme.ConfigureAmbience(caustics, lightRay, LightRayFrameSeconds, MarineSnowPerTheme.GetValueOrDefault(themeName), glows);
            EditorUtility.SetDirty(theme);

            Debug.Log($"ArtSetup: tema {themeName}: {tiles.Count} tiles, {decorations.Count} decoraciones, {doors.Count} hojas de puertas, "
                + $"{obstacles.Count} obstáculos{(caustics != null ? ", cáusticas" : "")}{(lightRay != null ? ", rayos" : "")}.");
            return true;
        }

        /// <summary>
        /// Contorno blanco de 1 px alrededor de la silueta de un sprite (lo que es transparente y toca un píxel opaco por un
        /// lado), 2 px más ancho y más alto para que quede centrado sobre él. Se tiñe con el color del tileset.
        /// </summary>
        static Sprite CreateOutline(string spritePath, string folder)
        {
            var source = new Texture2D(2, 2);
            if (!source.LoadImage(File.ReadAllBytes(spritePath)))
                return null;

            int width = source.width, height = source.height;
            bool Opaque(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && source.GetPixel(x, y).a > 0.1f;

            var outline = new Texture2D(width + 2, height + 2, TextureFormat.RGBA32, false);
            for (int y = 0; y < height + 2; y++)
            {
                for (int x = 0; x < width + 2; x++)
                {
                    int sx = x - 1, sy = y - 1;
                    bool edge = !Opaque(sx, sy) && (Opaque(sx - 1, sy) || Opaque(sx + 1, sy) || Opaque(sx, sy - 1) || Opaque(sx, sy + 1));
                    outline.SetPixel(x, y, edge ? Color.white : Color.clear);
                }
            }

            Directory.CreateDirectory(folder);
            string path = $"{folder}/{Path.GetFileNameWithoutExtension(spritePath)}_Outline.png";
            File.WriteAllBytes(path, outline.EncodeToPNG());
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(outline);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return ImportSingleSprite(path);
        }

        static Glow Glow((string piece, string color, float radius, Vector2 intensity, float period) entry,
            List<TileBase> decorations, Dictionary<string, Sprite> obstacles)
        {
            Object source = obstacles.TryGetValue(entry.piece, out var sprite) ? sprite
                : decorations.FirstOrDefault(tile => tile != null && tile.name == entry.piece);
            ColorUtility.TryParseHtmlString(entry.color, out var color);
            return new Glow(source, color, entry.radius, entry.intensity, entry.period);
        }

        /// <summary>
        /// Cáusticas: la hoja de 4 cuadros de 64×64 se corta en tiles de 16 y se hace un tile animado por cada celda del
        /// mosaico de 4×4 (índice x + 4 · y, con y desde abajo). Null si el tema no tiene cáusticas.
        /// </summary>
        static TileBase[] ImportCaustics(string path, string tileAssets)
        {
            if (!File.Exists(path))
                return null;

            const int cell = 16, cells = CausticsPeriod / cell;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int width, out _);
            int frames = width / CausticsPeriod;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previousIds = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);

            var rects = new List<SpriteRect>();
            for (int frame = 0; frame < frames; frame++)
            {
                for (int y = 0; y < cells; y++)
                {
                    for (int x = 0; x < cells; x++)
                    {
                        string name = $"Overlay_Caustics_{frame}_{x}_{y}";
                        rects.Add(new SpriteRect
                        {
                            name = name,
                            rect = new Rect(frame * CausticsPeriod + x * cell, y * cell, cell, cell),
                            alignment = SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                            spriteID = previousIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                        });
                    }
                }
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var result = new TileBase[cells * cells];
            for (int y = 0; y < cells; y++)
            {
                for (int x = 0; x < cells; x++)
                {
                    var animation = Enumerable.Range(0, frames).Select(frame => sprites[$"Overlay_Caustics_{frame}_{x}_{y}"]).ToArray();
                    result[x + cells * y] = CreateAnimatedTile($"{tileAssets}/Overlay_Caustics_{x}_{y}.asset", animation, 1f / CausticsFrameSeconds);
                }
            }
            return result;
        }

        /// <summary>
        /// Copos de nieve marina (docs/multimedia-temp/environment/Ocean) y el material de sus partículas. Nulls si no hay arte.
        /// </summary>
        internal static (Sprite[] flakes, Material material) ImportMarineSnow()
        {
            CopyFiles($"{EnvironmentSourceFolder}/Ocean", OceanArtFolder, "Particle_*.png");
            string path = $"{OceanArtFolder}/Particle_MarineSnow.png";
            if (!File.Exists(path))
                return (null, null);

            var flakes = SliceSheet(path, 8);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MarineSnowMaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MarineSnowMaterialPath);
            }
            material.mainTexture = flakes.Length > 0 ? flakes[0].texture : null;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return (flakes, material);
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

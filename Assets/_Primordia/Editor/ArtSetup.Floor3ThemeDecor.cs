using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Decorado de las salas con tema del piso 3 (docs/multimedia-temp/floor3-theme-decor): copia las hojas de los tres tramos
    /// a Art/Floor3/floor3-theme-decor, las corta (pivote en el pie las piezas, en el centro lo demás) y las reúne en
    /// Resources/ThemeDecorArt, junto a los tiles de la gran charca de agua.
    /// </summary>
    public static partial class ArtSetup
    {
        const string ThemeDecorSourceFolder = "docs/multimedia-temp/floor3-theme-decor";
        const string ThemeDecorArtFolder = "Assets/_Primordia/Art/Floor3/floor3-theme-decor";
        const string ThemeDecorAssetPath = "Assets/_Primordia/Resources/ThemeDecorArt.asset";

        static readonly string[] ThemeDecorBands = { "Swamp", "Smoke", "Burnt" };

        // Piezas con pie y estados (Idle / Burn / Burnt): el ancho de la celda es el de su hoja _Burnt.
        static readonly string[] ThemeDecorProps =
        {
            "Nest_Big", "Nest_Small", "Nest_Long", "LeafPile_A", "LeafPile_B", "LeafPile_C",
            "SilkWrap_Fallen", "SilkWrap_Trunk", "SilkWrap_Stump", "Cocoon_Small", "Cocoon_Medium", "Cocoon_Large",
            "EggSac_A", "EggSac_B", "EggSac_C", "ShoreReeds_A", "ShoreReeds_B", "ShoreReeds_C",
            "CalamitesClump_A", "CalamitesClump_B", "CalamitesClump_C", "Perch_A", "Perch_B", "Perch_C",
        };

        // Sin estados: (hoja, ancho de celda, pivote en el pie). Calcomanías, efectos y piedras.
        static readonly (string id, int cell, bool foot)[] ThemeDecorPlain =
        {
            ("Deco_BeetleScorch", 34, false), ("Deco_FallenCones", 20, false), ("Deco_HorsetailGrass", 36, false),
            ("Deco_ToadSpawn", 34, false), ("Deco_WetMud", 38, false), ("TadpolePool", 24, false),
            ("Deco_LightPool_A", 48, false), ("Deco_LightPool_B", 64, false),
            ("Fx_LightShaft_A", 52, false), ("Fx_LightShaft_B", 72, false), ("Fx_SporeDrift", 24, false),
            ("Fx_DragonflyShadow", 56, false),
            ("MossRock_A", 18, true), ("MossRock_B", 26, true), ("MossRock_C", 28, true),
        };

        static readonly string[] ThemeDecorCorners = { "NE", "NO", "SE", "SO" };

        [MenuItem("Primordia/Floor 3/Importar decorado de las salas con tema")]
        internal static void ImportThemeDecor()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThemeDecorAssetPath));
            var art = AssetDatabase.LoadAssetAtPath<ThemeDecorArt>(ThemeDecorAssetPath);
            bool isNew = art == null;
            if (isNew)
                art = ScriptableObject.CreateInstance<ThemeDecorArt>();

            var foot = new Vector2(0.5f, 0f);
            var center = new Vector2(0.5f, 0.5f);
            var pieces = new List<ThemeDecorArt.Piece>();
            var missing = new List<string>();

            foreach (string id in ThemeDecorProps)
            {
                var piece = new ThemeDecorArt.Piece { id = id };
                for (int band = 0; band < ThemeDecorBands.Length; band++)
                {
                    string folder = ThemeDecorBands[band];
                    // El ancho de la celda sale de la hoja quemada del tramo con fuego (la de la tierra quemada solo tiene _Idle).
                    int cell = PngWidth($"{ThemeDecorSourceFolder}/Swamp/{id}_Burnt.png");
                    if (cell <= 0)
                    {
                        missing.Add($"{id} ({folder})");
                        continue;
                    }
                    piece.bands[band] = new ThemeDecorArt.Look
                    {
                        idle = ThemeDecorSheet(folder, $"{id}_Idle", cell, foot),
                        burn = ThemeDecorSheet(folder, $"{id}_Burn", cell, foot, optional: true),
                        burnt = First(ThemeDecorSheet(folder, $"{id}_Burnt", cell, foot, optional: true)),
                    };
                }
                pieces.Add(piece);
            }

            foreach (var (id, cell, atFoot) in ThemeDecorPlain)
            {
                var piece = new ThemeDecorArt.Piece { id = id };
                for (int band = 0; band < ThemeDecorBands.Length; band++)
                {
                    var frames = ThemeDecorSheet(ThemeDecorBands[band], id, cell, atFoot ? foot : center, optional: true);
                    piece.bands[band] = new ThemeDecorArt.Look { idle = frames };
                }
                pieces.Add(piece);
            }
            art.pieces = pieces.ToArray();

            art.waters = new ThemeDecorArt.Water[ThemeDecorBands.Length];
            for (int band = 0; band < ThemeDecorBands.Length; band++)
            {
                string folder = ThemeDecorBands[band];
                art.waters[band] = new ThemeDecorArt.Water
                {
                    tiles = Enumerable.Range(0, 16).Select(mask => ThemeDecorSingle(folder, $"Water_{mask}")).ToArray(),
                    corners = ThemeDecorCorners.Select(corner => ThemeDecorSingle(folder, $"Water_Corner_{corner}")).ToArray(),
                    shimmer = ThemeDecorSheet(folder, "Water_Shimmer", 16, center, optional: true),
                };
            }

            if (isNew)
                AssetDatabase.CreateAsset(art, ThemeDecorAssetPath);
            else
                EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            if (missing.Count > 0)
                Debug.LogWarning($"ArtSetup: faltan hojas del decorado: {string.Join(", ", missing)}.");
            Debug.Log($"ArtSetup: decorado de las salas con tema importado en Resources/ThemeDecorArt ({art.pieces.Length} piezas, 3 tramos, agua).");
        }

        static Sprite[] ThemeDecorSheet(string band, string name, int cellWidth, Vector2 pivot, bool optional = false)
        {
            string source = $"{ThemeDecorSourceFolder}/{band}/{name}.png";
            string target = $"{ThemeDecorArtFolder}/{band}/{name}.png";
            if (!File.Exists(source) && !File.Exists(target))
            {
                if (!optional)
                    Debug.LogWarning($"ArtSetup: falta {source}.");
                return System.Array.Empty<Sprite>();
            }
            if (!CopyArt(source, target))
                return System.Array.Empty<Sprite>();
            return SliceSheet(target, cellWidth, pivot);
        }

        static Sprite ThemeDecorSingle(string band, string name)
        {
            string target = $"{ThemeDecorArtFolder}/{band}/{name}.png";
            return CopyArt($"{ThemeDecorSourceFolder}/{band}/{name}.png", target) ? ImportSingleSprite(target) : null;
        }

        // Ancho de un PNG leído de su cabecera (bytes 16-19, big endian); 0 si no existe.
        static int PngWidth(string path)
        {
            if (!File.Exists(path))
                return 0;
            using var stream = File.OpenRead(path);
            var header = new byte[24];
            if (stream.Read(header, 0, 24) < 24)
                return 0;
            return (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        }
    }
}

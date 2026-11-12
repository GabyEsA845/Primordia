using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    public static partial class ArtSetup
    {
        internal const string LepidodendronArtPath = "Assets/_Primordia/Data/Bosses/LepidodendronArt.asset";
        const string LepidodendronArtFolder = Floor3ArtRoot + "/floor3-bosses/lepidodendron";

        [MenuItem("Primordia/Boss/Importar hojas del Lepidodendron")]
        internal static DirectionalArt ImportLepidodendronSheets()
        {
            ImportFloor3Art();
            var sheets = new List<DirectionalArt.Sheet>();
            foreach (string folder in new[] { "Tree", "Canopy", "Decor", "Walls" })
            {
                string path = $"{LepidodendronArtFolder}/{folder}";
                if (!Directory.Exists(path))
                    throw new FileNotFoundException($"Falta el arte aprobado: {path}");
                foreach (string file in Directory.GetFiles(path, "*.png").OrderBy(f => f))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                    importer.maxTextureSize = 8192;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.GetSourceTextureWidthAndHeight(out _, out int height);
                    var (width, pivot) = LepidodendronCell(folder, name, height);
                    var sprites = SliceFloor3Sheet(file.Replace('\\', '/'), width, pivot);
                    sheets.Add(new DirectionalArt.Sheet { name = name, rows = 1, frames = sprites.Length, sprites = sprites });
                }
            }
            foreach (string required in LepidodendronBoss.RequiredSheets)
                if (!sheets.Any(s => s.name == required && s.sprites.Length > 0))
                    throw new FileNotFoundException($"Falta la hoja aprobada {required}.");

            Directory.CreateDirectory(Path.GetDirectoryName(LepidodendronArtPath));
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>(LepidodendronArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<DirectionalArt>();
                AssetDatabase.CreateAsset(art, LepidodendronArtPath);
            }
            art.SetSheets(sheets.ToArray());
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Lepidodendron: {sheets.Count} hojas importadas (una fila por hoja).");
            return art;
        }

        static (int width, Vector2 pivot) LepidodendronCell(string folder, string name, int height)
        {
            if (folder == "Tree")
            {
                if (name == "Lepido_BackRoots") return (520, new Vector2(260f / 520, 1f - 196f / 210));
                if (name.StartsWith("Lepido_Mound")) return (480, new Vector2(0.5f, 1f - 96f / 230));
                return (288, new Vector2(0.5f, 1f - 352f / 384));
            }
            if (folder == "Canopy")
            {
                int width = name.StartsWith("Canopy_Arena") ? 704
                    : name.StartsWith("Canopy_Strip") || name == "Fx_Dapple" ? 256
                    : name == "Fx_LightShaft" ? 118 : name == "Fx_LightPool" ? 52
                    : name == "Fx_FallingLeaf" ? 16 : 6;
                var pivot = name.StartsWith("Canopy_") ? new Vector2(0.5f, 1f)
                    : name == "Fx_LightShaft" ? new Vector2(19f / 118, 1f) : new Vector2(0.5f, 0.5f);
                return (width, pivot);
            }
            if (folder == "Walls")
            {
                if (name.Contains("_Knot") || name.StartsWith("Fx_")) return (height, new Vector2(0.5f, 0.5f));
                int wall = name[11] - '1';
                int width = new[] { 124, 132, 140 }[wall];
                int foot = new[] { 325, 332, 341 }[wall];
                return (width, new Vector2(0.5f, 1f - foot / (float)height));
            }
            int cell = name.StartsWith("Lepido_Fungus_") ? (name.EndsWith("_S") ? 34 : name.EndsWith("_M") ? 42 : 50)
                : name.StartsWith("Lepido_Shroom") ? 28 : name.StartsWith("Lepido_Flowerlets") ? 24
                : name.StartsWith("Lepido_Liana") ? 24 : name == "Lepido_DarkPuddle" ? 52
                : name.StartsWith("Critter_Millipede") ? 28 : name.StartsWith("Critter_Springtail") ? 12
                : name.StartsWith("Critter_") ? 20 : name == "Remains_Pulmonoscorpius" ? 68
                : name == "Remains_PaleoWings" ? 52 : name == "Remains_Trigonotarbid" ? 24 : 42;
            return (cell, new Vector2(0.5f, name.StartsWith("Lepido_Fungus") ? 0.5f : name.StartsWith("Lepido_Liana") ? 1f : 0f));
        }
    }
}

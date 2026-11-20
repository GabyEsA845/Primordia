using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Primordia.Editor
{
    public static partial class ArtSetup
    {
        internal const string MegarachneArtPath = "Assets/_Primordia/Data/Bosses/MegarachneArt.asset";
        internal const string MegarachneFolder = Floor3ArtRoot + "/floor3-bosses/megarachne";
        const string MegarachneSource = "docs/multimedia-temp/floor3-bosses/megarachne";

        [MenuItem("Primordia/Art/Importar Megarachne")]
        public static void ImportMegarachne() => ImportMegarachneSheets();

        internal static DirectionalArt ImportMegarachneSheets()
        {
            string[] animations = { "Idle", "Walk", "Weave", "Hurt", "Stunned", "Recover", "Drop", "Climb", "DropWarn", "Land", "Fall", "SilkShoot" };
            var files = new List<string>();
            foreach (string animation in animations)
            {
                files.Add("Sheets/Megarachne_" + animation + ".png");
                files.Add("Shadows/Megarachne_Shadow_" + animation + ".png");
            }
            files.AddRange(new[] { "Nido/Nido_Tree_Wet.png", "Nido/Nido_Web.png", "Nido/Nido_Eyes.png" });
            CopyMegarachneFiles(files.Concat(MegarachneCableFiles));
            var sheets = new List<DirectionalArt.Sheet>();
            foreach (string file in files.Where(f => f.StartsWith("Sheets/") || f.StartsWith("Shadows/")))
                sheets.Add(SliceDirectional(MegarachneFolder + "/" + file));
            foreach (string file in files.Where(f => f.StartsWith("Nido/")))
                if (!file.EndsWith("Nido_Tree_Wet.png") || !AssetDatabase.LoadAllAssetsAtPath(MegarachneFolder + "/" + file).OfType<Sprite>().Any(s => s.name == "Nido_Tree_Wet_Copa"))
                    SliceFloor3Sheet(MegarachneFolder + "/" + file, 160, new Vector2(0.5f, 12f / 176f));
            ImportMegarachneCables(copyFiles: false);
            SliceMegarachneTree();
            Directory.CreateDirectory(Path.GetDirectoryName(MegarachneArtPath));
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>(MegarachneArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<DirectionalArt>();
                AssetDatabase.CreateAsset(art, MegarachneArtPath);
            }
            art.SetSheets(sheets.ToArray());
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("Megarachne: 24 hojas de cuerpo/sombra y 9 archivos del Nido/cables importados.");
            return art;
        }
        static string[] MegarachneCableFiles => new[]
        {
            "Nido/Nido_CableRoot.png", "Effects/Cable_Normal.png", "Effects/Cable_Thin.png",
            "Effects/Cable_Grow.png", "Effects/Cable_Burning.png", "Effects/Cable_Charred.png"
        };

        internal static Sprite[] ImportMegarachneCables(bool copyFiles = true)
        {
            var files = MegarachneCableFiles;
            if (copyFiles) CopyMegarachneFiles(files);
            foreach (string file in files.Skip(1))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(MegarachneFolder + "/" + file);
                importer.textureType = TextureImporterType.Default;
                importer.spriteImportMode = SpriteImportMode.None;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return SliceFloor3Sheet(MegarachneFolder + "/" + files[0], 256, new Vector2(0.5f, 36f / 224f));
        }

        static void CopyMegarachneFiles(IEnumerable<string> files)
        {
            // Se valida todo antes de importar; nunca se genera ni se sustituye arte.
            foreach (string file in files)
                if (!File.Exists(MegarachneSource + "/" + file)) throw new FileNotFoundException("Falta el arte aprobado: " + file);
            foreach (string file in files)
            {
                string source = MegarachneSource + "/" + file, destination = MegarachneFolder + "/" + file;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (!File.Exists(destination) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
                    File.Copy(source, destination, true);
                AssetDatabase.ImportAsset(destination);
            }
        }

        internal static MegarachneBoss.EffectSheet[] ImportMegarachneAttacks()
        {
            var specs = new (string name, int width, Vector2 pivot)[]
            {
                ("Proj_MegarachneSilk", 10, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneSilk_Muzzle", 24, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneWeb_Appear", 40, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneWeb_Idle", 40, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneWeb_Burn", 40, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneNet_Shadow", 64, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneNet_Fall", 64, new Vector2(0.5f, 24f / 88f)),
                ("Fx_MegarachneNet_Idle", 64, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneNet_Trapped", 40, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneNet_Break", 64, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneDrop_Track", 40, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneDrop_Locked", 40, new Vector2(0.5f, 0.5f)),
                ("Fx_MegarachneDust", 72, new Vector2(0.5f, 0.5f))
            };
            foreach (var spec in specs)
                if (!File.Exists(MegarachneSource + "/Effects/" + spec.name + ".png"))
                    throw new FileNotFoundException("Falta el arte aprobado: " + spec.name);
            var result = new List<MegarachneBoss.EffectSheet>();
            foreach (var spec in specs)
            {
                string source = MegarachneSource + "/Effects/" + spec.name + ".png";
                string destination = MegarachneFolder + "/Effects/" + spec.name + ".png";
                if (!File.Exists(destination) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
                    File.Copy(source, destination, true);
                AssetDatabase.ImportAsset(destination);
                result.Add(new MegarachneBoss.EffectSheet { name = spec.name,
                    frames = SliceFloor3Sheet(destination, spec.width, spec.pivot) });
            }
            return result.ToArray();
        }

        internal static MegarachneBoss.EffectSheet[] ImportMegarachneBrood()
        {
            var specs = new (string name, int height, int count)[]
            {
                ("Megarachne_Sac_Idle", 32, 4),
                ("Megarachne_Sac_Tremble", 32, 4),
                ("Megarachne_Sac_Burst", 32, 5),
                ("Megarachne_Sac_Break", 32, 5),
                ("Megarachne_Brood_Move", 24, 4),
                ("Megarachne_Brood_Bite", 24, 4),
                ("Megarachne_Brood_Hurt", 24, 2),
                ("Megarachne_Brood_Death", 24, 5)
            };
            const int cellWidth = 24;
            foreach (var spec in specs)
            {
                string path = MegarachneFolder + "/Effects/" + spec.name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("Falta la hoja aprobada: " + path);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                if (width != cellWidth * spec.count || height != spec.height)
                    throw new InvalidDataException("Dimensiones inesperadas en " + path);
            }
            return specs.Select(spec => new MegarachneBoss.EffectSheet
            {
                name = spec.name,
                frames = SliceFloor3Sheet(MegarachneFolder + "/Effects/" + spec.name + ".png", cellWidth,
                    new Vector2(0.5f, spec.height == 32 ? 1f : 0.5f))
            }).ToArray();
        }

        // Dos recortes de la misma imagen, sin modificar píxeles: el pie no pierde opacidad con la copa.
        static void SliceMegarachneTree()
        {
            string path = MegarachneFolder + "/Nido/Nido_Tree_Wet.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new List<SpriteRect>(provider.GetSpriteRects().Where(r => r.name == "Nido_Tree_Wet_0"));
            foreach (bool crown in new[] { false, true })
            {
                string name = "Nido_Tree_Wet_" + (crown ? "Copa" : "Tronco");
                rects.Add(new SpriteRect { name = name,
                    rect = crown ? new Rect(0, 32, 160, 144) : new Rect(0, 0, 160, 32),
                    alignment = SpriteAlignment.Custom,
                    pivot = crown ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 12f / 32f),
                    spriteID = previous.TryGetValue(name, out var id) ? id : GUID.Generate() });
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }
    }
}


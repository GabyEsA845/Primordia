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

        internal static MegarachneBoss.EffectSheet[] ImportMegarachneRage()
        {
            string[] animations = { "Idle", "Walk", "Weave", "Hurt", "Stunned", "Recover", "Drop", "Climb", "DropWarn", "Land", "Fall", "SilkShoot", "Threat" };
            string[] trees = { "Nido_Tree_Dry", "Nido_Tree_Charred", "Nido_Tree_Shake", "Nido_Tree_Drying", "Nido_Burn", "Nido_Tree_Charred_Smolder" };
            var bodyFiles = animations.Select(a => "Sheets/Rage/Megarachne_" + a + "_Rage.png")
                .Concat(new[] { "Sheets/Megarachne_Screech.png", "Sheets/Megarachne_IntroEmerge.png" }).ToArray();
            CopyMegarachneFiles(bodyFiles.Concat(trees.Select(t => "Nido/" + t + ".png"))
                .Concat(new[] { "Nido/Fx_Nido_Rain.png", "Nido/Nido_WebDecay.png", "Effects/Fx_Megarachne_Screech.png" }));
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>(MegarachneArtPath);
            if (art == null) throw new FileNotFoundException("Falta el arte base de Megarachne (M1).");
            var sheets = art.Sheets.ToDictionary(s => s.name);
            foreach (string file in bodyFiles)
            {
                var sheet = SliceDirectional(MegarachneFolder + "/" + file);
                sheets[sheet.name] = sheet;
            }
            art.SetSheets(sheets.Values.ToArray());
            EditorUtility.SetDirty(art);
            var result = new List<MegarachneBoss.EffectSheet>();
            foreach (string name in trees) SliceRageTree(name, result);
            foreach (string name in new[] { "Fx_Nido_Rain", "Nido_WebDecay" })
                result.Add(new MegarachneBoss.EffectSheet { name = name,
                    frames = SliceFloor3Sheet(MegarachneFolder + "/Nido/" + name + ".png", 160, new Vector2(0.5f, 12f / 176f)) });
            result.Add(new MegarachneBoss.EffectSheet { name = "Fx_Megarachne_Screech",
                frames = SliceFloor3Sheet(MegarachneFolder + "/Effects/Fx_Megarachne_Screech.png", 192, new Vector2(0.5f, 0.5f)) });
            AssetDatabase.SaveAssets();
            return result.ToArray();
        }

        // Recorta copa y pie por separado para conservar la transparencia solo sobre la copa.
        static void SliceRageTree(string name, List<MegarachneBoss.EffectSheet> result)
        {
            string path = MegarachneFolder + "/Nido/" + name + ".png";
            if (!AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any(s => s.name == name + "_Copa_0"))
                SliceFloor3Sheet(path, 160, new Vector2(0.5f, 12f / 176f));
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (height != 176 || width % 160 != 0) throw new InvalidDataException("Dimensiones inesperadas: " + path);
            int count = width / 160;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new List<SpriteRect>();
            foreach (bool crown in new[] { true, false })
                for (int i = 0; i < count; i++)
                {
                    string spriteName = name + (crown ? "_Copa_" : "_Tronco_") + i;
                    rects.Add(new SpriteRect { name = spriteName,
                        rect = crown ? new Rect(i * 160, 32, 160, 144) : new Rect(i * 160, 0, 160, 32),
                        alignment = SpriteAlignment.Custom,
                        pivot = crown ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 12f / 32f),
                        spriteID = previous.TryGetValue(spriteName, out var id) ? id : GUID.Generate() });
                }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            result.Add(new MegarachneBoss.EffectSheet { name = name, frames = rects.Take(count).Select(r => sprites[r.name]).ToArray() });
            result.Add(new MegarachneBoss.EffectSheet { name = name + "_Tronco", frames = rects.Skip(count).Select(r => sprites[r.name]).ToArray() });
        }

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

        internal static MegarachneBoss.CableFireArt ImportMegarachneFire()
        {
            var art = new MegarachneBoss.CableFireArt
            {
                burning = LoadCableTexture("Cable_Burning", 3),
                charred = LoadCableTexture("Cable_Charred", 3),
                growing = LoadCableTexture("Cable_Grow", 6),
                sacIdle = LoadMethaneSprites("floor3-environment/Smoke/props/Methane_Sac_Idle"),
                sacBurst = LoadMethaneSprites("floor3-reactions/Smoke/Methane_Sac_Burst"),
                sacRemains = LoadMethaneSprites("floor3-reactions/Smoke/Methane_Sac_Remains").Single()
            };
            var specs = new (string name, int width, int height, int frames, Vector2 pivot)[]
            {
                ("Cable_Stub", 16, 40, 4, new Vector2(0.5f, 1f)),
                ("Fx_CableFlame", 16, 24, 6, new Vector2(0.5f, 0.25f)),
                ("Fx_CableFront", 24, 24, 4, new Vector2(0.5f, 0.5f)),
                ("Fx_CableAsh", 16, 32, 6, new Vector2(0.5f, 0.5f)),
                ("Fx_CableSmoke", 24, 40, 6, new Vector2(0.5f, 0f)),
                ("Fx_Megarachne_Stunned", 64, 28, 8, new Vector2(0.5f, 0.5f))
            };
            foreach (var spec in specs) FireImporter(spec.name, spec.width * spec.frames, spec.height);
            art.effects = specs.Select(spec => new MegarachneBoss.EffectSheet
            {
                name = spec.name,
                frames = SliceFloor3Sheet(MegarachneFolder + "/Effects/" + spec.name + ".png", spec.width, spec.pivot)
            }).ToArray();
            return art;
        }

        static TextureImporter FireImporter(string name, int width, int height)
        {
            string path = MegarachneFolder + "/Effects/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Falta la hoja aprobada: " + path);
            importer.GetSourceTextureWidthAndHeight(out int actualWidth, out int actualHeight);
            if (actualWidth != width || actualHeight != height)
                throw new InvalidDataException("Dimensiones inesperadas en " + path);
            return importer;
        }

        static Texture2D LoadCableTexture(string name, int frames)
        {
            var importer = FireImporter(name, 64, 29 * frames);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(importer.assetPath);
            if (importer.textureType != TextureImporterType.Default || importer.spriteImportMode != SpriteImportMode.None ||
                importer.npotScale != TextureImporterNPOTScale.None || importer.wrapMode != TextureWrapMode.Repeat ||
                importer.filterMode != FilterMode.Point || importer.mipmapEnabled || !importer.alphaIsTransparency ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
                throw new InvalidDataException("La textura del cable requiere los ajustes de importación de cables v2: " + name);
            return texture;
        }

        static Sprite[] LoadMethaneSprites(string relative)
        {
            string path = Floor3ArtRoot + "/" + relative + ".png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int frame) ? frame : 0).ToArray();
            if (sprites.Length == 0) throw new FileNotFoundException("Falta el arte importado del odre de metano: " + path);
            return sprites;
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


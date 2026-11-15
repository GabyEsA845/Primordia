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
            string[] animations = { "Idle", "Walk", "Weave", "Hurt", "Stunned", "Recover", "Drop", "Climb", "DropWarn", "Land", "Fall" };
            var files = new List<string>();
            foreach (string animation in animations)
            {
                files.Add("Sheets/Megarachne_" + animation + ".png");
                files.Add("Shadows/Megarachne_Shadow_" + animation + ".png");
            }
            files.AddRange(new[] { "Nido/Nido_Tree_Wet.png", "Nido/Nido_Web.png", "Nido/Nido_Eyes.png",
                "Effects/Cable_Normal.png", "Effects/Cable_Thin.png" });
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
            var sheets = new List<DirectionalArt.Sheet>();
            foreach (string file in files.Where(f => f.StartsWith("Sheets/") || f.StartsWith("Shadows/")))
                sheets.Add(SliceDirectional(MegarachneFolder + "/" + file));
            foreach (string file in files.Where(f => f.StartsWith("Nido/")))
                if (!file.EndsWith("Nido_Tree_Wet.png") || !AssetDatabase.LoadAllAssetsAtPath(MegarachneFolder + "/" + file).OfType<Sprite>().Any(s => s.name == "Nido_Tree_Wet_Copa"))
                    SliceFloor3Sheet(MegarachneFolder + "/" + file, 160, new Vector2(0.5f, 12f / 176f));
            foreach (string file in files.Where(f => f.StartsWith("Effects/")))
            {
                SliceFloor3Sheet(MegarachneFolder + "/" + file, 32, new Vector2(0.5f, 0.5f));
                var importer = (TextureImporter)AssetImporter.GetAtPath(MegarachneFolder + "/" + file);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
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
            Debug.Log("Megarachne: 22 hojas de cuerpo/sombra y 5 hojas del Nido/cables importadas.");
            return art;
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


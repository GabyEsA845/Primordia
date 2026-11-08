using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Meganeura (jefe 2 del piso 3, arte en docs/multimedia-temp/floor3-bosses/meganeura): corta sus hojas con direcciones
    /// (Sheets/ y Shadows/: 9 filas de 192 px, o de 64/112 las de la ninfa, o una sola fila las de DeathFall y WingDetached) y
    /// las reúne en un <see cref="DirectionalArt"/>. Las hojas de una fila (efectos, huevos, barra, escenario) se cortan al usarlas.
    /// </summary>
    public static partial class ArtSetup
    {
        const string MeganeuraArtFolder = Floor3ArtRoot + "/floor3-bosses/meganeura";
        internal const string MeganeuraArtPath = "Assets/_Primordia/Data/Bosses/MeganeuraArt.asset";

        /// <summary>Corta una hoja de una fila del arte del piso 3 (la usan los montajes de jefes).</summary>
        internal static Sprite[] SliceFloor3Sheet(string path, int cellWidth, Vector2 pivot) => SliceSheet(path, cellWidth, pivot);

        [MenuItem("Primordia/Boss/Importar hojas de la Meganeura")]
        internal static DirectionalArt ImportMeganeuraSheets()
        {
            ImportFloor3Art();
            Directory.CreateDirectory(Path.GetDirectoryName(MeganeuraArtPath));
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>(MeganeuraArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<DirectionalArt>();
                AssetDatabase.CreateAsset(art, MeganeuraArtPath);
            }

            var sheets = new List<DirectionalArt.Sheet>();
            foreach (string folder in new[] { "Sheets", "Shadows" })
            {
                string path = $"{MeganeuraArtFolder}/{folder}";
                if (!Directory.Exists(path))
                {
                    Debug.LogWarning($"ArtSetup: falta {path}.");
                    continue;
                }
                foreach (string file in Directory.GetFiles(path, "*.png").OrderBy(f => f))
                    sheets.Add(SliceDirectional(file.Replace('\\', '/')));
            }

            art.SetSheets(sheets.ToArray());
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: Meganeura importada, {sheets.Count} hojas en {MeganeuraArtPath}.");
            return art;
        }

        /// <summary>
        /// Corta una hoja en cuadrícula: con 9 filas si el alto es múltiplo de 9 (192 → 1728; ninfa 64 → 576), con una sola si no
        /// (DeathFall, WingDetached). El cuadro es cuadrado. Pivote en el centro.
        /// </summary>
        static DirectionalArt.Sheet SliceDirectional(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int rows = height % DirectionalArt.DirectionRows == 0 ? DirectionalArt.DirectionRows : 1;
            int cell = height / rows;
            int frames = Mathf.Max(1, width / cell);
            string baseName = Path.GetFileNameWithoutExtension(path);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previousIds = new Dictionary<string, GUID>();
            foreach (var rect in provider.GetSpriteRects())
                previousIds[rect.name] = rect.spriteID;

            var rects = new SpriteRect[rows * frames];
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < frames; col++)
                {
                    string name = $"{baseName}_{row}_{col}";
                    // La fila 0 es la de arriba de la imagen; Unity cuenta el eje Y desde abajo.
                    rects[row * frames + col] = new SpriteRect
                    {
                        name = name,
                        rect = new Rect(col * cell, height - (row + 1) * cell, cell, cell),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = previousIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                    };
                }
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            var byName = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            return new DirectionalArt.Sheet
            {
                name = baseName,
                frames = frames,
                rows = rows,
                sprites = rects.Select(r => byName[r.name]).ToArray(),
            };
        }
    }
}

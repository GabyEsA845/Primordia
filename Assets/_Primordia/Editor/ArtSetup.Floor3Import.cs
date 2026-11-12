using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Importación en bloque del arte del piso 3 (docs/multimedia-temp/floor3-*): copia los PNG a Assets/_Primordia/Art/Floor3
    /// conservando las subcarpetas y los deja como sprites a 16 PPU, Point y sin compresión. No corta hojas ni crea prefabs:
    /// eso lo hace cada paso del PLAN al usar su arte (SliceSheet con la celda que toque). Se puede repetir sin miedo:
    /// los archivos que no han cambiado se saltan y los cortes ya hechos no se tocan.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor3SourceRoot = "docs/multimedia-temp";
        const string Floor3ArtRoot = "Assets/_Primordia/Art/Floor3";

        static readonly string[] Floor3Groups =
        {
            "floor3-environment", "floor3-reactions", "floor3-bodies", "floor3-enemies",
            "floor3-lineages", "floor3-mutations", "floor3-bosses", "floor3-spit", "floor3-jet", "floor3-top-clone", "floor3-log-stump",
            "floor3-poison-fire",
        };

        // Carpetas y archivos que son material de revisión (GIF, escenas, vistas previas, cachés de Python), no del juego.
        static readonly string[] Floor3SkippedFolders = { "gif", "escenas", "__pycache__" };

        [MenuItem("Primordia/Floor 3/Importar todo el arte del piso 3")]
        internal static void ImportFloor3Art()
        {
            var files = new List<string>();
            foreach (string group in Floor3Groups)
            {
                string groupFolder = $"{Floor3SourceRoot}/{group}";
                if (!Directory.Exists(groupFolder))
                {
                    Debug.LogWarning($"ArtSetup: falta {groupFolder}.");
                    continue;
                }
                CollectFloor3Pngs(groupFolder, files);
            }

            int copied = 0, skipped = 0;
            var imported = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string source in files)
                {
                    string relative = source.Substring(Floor3SourceRoot.Length + 1).Replace('\\', '/');
                    string target = $"{Floor3ArtRoot}/{relative}";
                    if (File.Exists(target) && new FileInfo(target).Length == new FileInfo(source).Length
                        && System.MemoryExtensions.SequenceEqual<byte>(File.ReadAllBytes(target), File.ReadAllBytes(source)))
                    {
                        skipped++;
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target, overwrite: true);
                    imported.Add(target);
                    copied++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string target in imported)
                    ConfigureFloor3Texture(target);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: arte del piso 3 importado en {Floor3ArtRoot} ({copied} archivos nuevos o cambiados, {skipped} sin cambios).");
        }

        static void CollectFloor3Pngs(string folder, List<string> files)
        {
            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                string name = Path.GetFileName(file);
                if (name.StartsWith("vista_previa") || name.StartsWith("maqueta") || name.StartsWith("Escena") || name.StartsWith("Sala_muestra"))
                    continue;
                files.Add(file.Replace('\\', '/'));
            }

            foreach (string sub in Directory.GetDirectories(folder))
            {
                if (System.Array.IndexOf(Floor3SkippedFolders, Path.GetFileName(sub)) >= 0)
                    continue;
                CollectFloor3Pngs(sub, files);
            }
        }

        static void ConfigureFloor3Texture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}

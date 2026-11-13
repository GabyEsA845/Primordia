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
            // Importación limitada al jefe; no regenera arte ni actualiza otros jefes.
            const string source = "docs/multimedia-temp/floor3-bosses/lepidodendron";
            foreach (string folder in new[] { "Tree", "Canopy", "Decor", "Walls", "Cones", "Attacks", "Death" })
                foreach (string file in Directory.GetFiles(source + "/" + folder, "*.png"))
                {
                    string target = LepidodendronArtFolder + "/" + folder + "/" + Path.GetFileName(file);
                    if (!SplitWideLepidodendronSheet(file, target, folder))
                        CopyArt(file, target);
                }
            var sheets = new List<DirectionalArt.Sheet>();
            foreach (string folder in new[] { "Tree", "Canopy", "Decor", "Walls", "Cones", "Attacks", "Death" })
            {
                var parts = new Dictionary<string, List<Sprite>>();
                string path = $"{LepidodendronArtFolder}/{folder}";
                if (!Directory.Exists(path))
                    throw new FileNotFoundException($"Falta el arte aprobado: {path}");
                foreach (string file in Directory.GetFiles(path, "*.png").OrderBy(f => f))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                    importer.maxTextureSize = folder == "Death" ? 16384 : 8192; // Fall ocupa 15 ? 1056 px; no reducir la hoja aprobada.
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.GetSourceTextureWidthAndHeight(out _, out int height);
                    string cellName = name.Contains(WidePartMark) ? name.Substring(0, name.IndexOf(WidePartMark, StringComparison.Ordinal)) : name;
                    var (width, pivot) = LepidodendronCell(folder, cellName, height);
                    var sprites = SliceFloor3Sheet(file.Replace('\\', '/'), width, pivot);
                    int split = name.IndexOf(WidePartMark, StringComparison.Ordinal);
                    if (split < 0)
                        sheets.Add(new DirectionalArt.Sheet { name = name, rows = 1, frames = sprites.Length, sprites = sprites });
                    else
                    {
                        string whole = name.Substring(0, split);
                        if (!parts.TryGetValue(whole, out var list)) parts[whole] = list = new List<Sprite>();
                        list.AddRange(sprites);
                    }
                }
                foreach (var part in parts)
                    sheets.Add(new DirectionalArt.Sheet { name = part.Key, rows = 1, frames = part.Value.Count, sprites = part.Value.ToArray() });
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

        // Unity no crea texturas de mas de 8192 px de ancho en batchmode ("Failed to initialize texture"): las hojas
        // aprobadas mas anchas (desplome) se parten al importar en trozos de celdas completas, sin tocar el original.
        const string WidePartMark = "__p";
        const int MaxSheetWidth = 8192;

        static bool SplitWideLepidodendronSheet(string source, string target, string folder)
        {
            byte[] bytes = File.ReadAllBytes(source);
            if (bytes.Length < 24) return false;
            int sourceWidth = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int sourceHeight = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            string name = Path.GetFileNameWithoutExtension(target);
            string dir = Path.GetDirectoryName(target).Replace('\\', '/');
            if (sourceWidth <= MaxSheetWidth)
            {
                // Una hoja que antes se partio y ya cabe: fuera sus trozos.
                foreach (string old in Directory.GetFiles(dir, name + WidePartMark + "*.png"))
                    AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
                return false;
            }
            var (cell, _) = LepidodendronCell(folder, name, sourceHeight);
            int perPart = Mathf.Max(1, MaxSheetWidth / cell);
            int cells = sourceWidth / cell;
            byte[] rgba = DecodeRgbaPng(bytes, sourceWidth, sourceHeight);
            for (int part = 0, first = 0; first < cells; part++, first += perPart)
            {
                int count = Mathf.Min(perPart, cells - first);
                var piece = new Texture2D(count * cell, sourceHeight, TextureFormat.RGBA32, false);
                try
                {
                    var data = new byte[count * cell * sourceHeight * 4];
                    for (int y = 0; y < sourceHeight; y++)
                        Buffer.BlockCopy(rgba, (y * sourceWidth + first * cell) * 4, data, y * count * cell * 4, count * cell * 4);
                    // PNG guarda la fila de arriba primero; Texture2D, la de abajo.
                    var flipped = new byte[data.Length];
                    for (int y = 0; y < sourceHeight; y++)
                        Buffer.BlockCopy(data, y * count * cell * 4, flipped, (sourceHeight - 1 - y) * count * cell * 4, count * cell * 4);
                    piece.SetPixelData(flipped, 0);
                    piece.Apply();
                    string partPath = $"{dir}/{name}{WidePartMark}{part}.png";
                    File.WriteAllBytes(partPath, piece.EncodeToPNG());
                    AssetDatabase.ImportAsset(partPath, ImportAssetOptions.ForceUpdate);
                }
                finally { UnityEngine.Object.DestroyImmediate(piece); }
            }
            // El original ancho no puede importarse: no se deja en Assets.
            if (File.Exists(target)) AssetDatabase.DeleteAsset(target);
            return true;
        }

        // Decodificador minimo de PNG RGBA de 8 bits sin entrelazar (el formato de las hojas del jefe).
        static byte[] DecodeRgbaPng(byte[] png, int width, int height)
        {
            if (png[24] != 8 || png[25] != 6 || png[28] != 0)
                throw new NotSupportedException("La hoja ancha debe ser PNG RGBA de 8 bits sin entrelazar.");
            using var idat = new MemoryStream();
            for (int pos = 8; pos + 8 <= png.Length;)
            {
                int length = (png[pos] << 24) | (png[pos + 1] << 16) | (png[pos + 2] << 8) | png[pos + 3];
                string type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
                if (type == "IDAT") idat.Write(png, pos + 8, length);
                if (type == "IEND") break;
                pos += 12 + length;
            }
            idat.Position = 2; // cabecera zlib
            int stride = width * 4;
            var raw = new byte[(stride + 1) * height];
            using (var inflate = new System.IO.Compression.DeflateStream(idat, System.IO.Compression.CompressionMode.Decompress))
                for (int read = 0; read < raw.Length;)
                {
                    int n = inflate.Read(raw, read, raw.Length - read);
                    if (n <= 0) throw new InvalidDataException("PNG truncado.");
                    read += n;
                }
            var pixels = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                int filter = raw[y * (stride + 1)];
                int src = y * (stride + 1) + 1, dst = y * stride, up = dst - stride;
                for (int x = 0; x < stride; x++)
                {
                    int a = x >= 4 ? pixels[dst + x - 4] : 0;
                    int b = y > 0 ? pixels[up + x] : 0;
                    int c = x >= 4 && y > 0 ? pixels[up + x - 4] : 0;
                    int add = filter switch
                    {
                        1 => a, 2 => b, 3 => (a + b) / 2,
                        4 => Paeth(a, b, c), _ => 0
                    };
                    pixels[dst + x] = (byte)(raw[src + x] + add);
                }
            }
            return pixels;
        }

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c, pa = Mathf.Abs(p - a), pb = Mathf.Abs(p - b), pc = Mathf.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        static (int width, Vector2 pivot) LepidodendronCell(string folder, string name, int height)
        {
            if (folder == "Death")
            {
                if (name == "Lepido_Death_Fall" || name == "Lepido_FallenTrunk") return (1056, new Vector2(944f / 1056, 1f - 352f / 416));
                if (name.StartsWith("Lepido_Death_") || name.StartsWith("Lepido_Stump_")) return (288, new Vector2(0.5f, 1f - 352f / 384));
                if (name.StartsWith("Canopy_")) return (704, new Vector2(0.5f, 1f));
                if (name == "Fx_FallShadow") return (848, new Vector2(0f, 1f));
                if (name == "Fx_TrunkDust") return (880, new Vector2(0f, 1f));
                if (name == "Fx_TrunkSplinters") return (160, new Vector2(0.5f, 0.5f));
                if (name == "Fx_CrownCrash") return (320, new Vector2(0.5f, 0.5f));
                if (name == "Lepido_WallRubble") return (448, new Vector2(0.5f, 0.5f));
                if (name == "Fx_GoldColumn") return (128, new Vector2(0.5f, 0f));
                return (24, new Vector2(0.5f, 0f));
            }
            if (folder == "Attacks")
            {
                if (name.StartsWith("Lepido_Whip_")) return (352, new Vector2(0f, 1f - 34f / 56));
                if (name.StartsWith("Lepido_Spikes_")) return (80, new Vector2(0.5f, 1f - 78f / 96));
                if (name == "Proj_LepidoSpore") return (12, new Vector2(0.5f, 1f - 18f / 24));
                if (name == "Fx_LepidoSporeSplash") return (24, new Vector2(0.5f, 1f - 11f / 16));
                return (34, new Vector2(0.5f, 0.5f));
            }
            if (folder == "Cones")
            {
                if (name == "Lepido_ConeBranch_Tile") return (72, new Vector2(0.5f, 0.5f));
                if (name.Contains("Shatter")) return (128, new Vector2(0.5f, 1f - 104f / 112));
                if (name == "Lepido_Cone_Burst") return (128, new Vector2(0.5f, 1f - 114f / 120));
                if (name == "Fx_ConeShadow" || name == "Fx_ConeCrater") return (72, new Vector2(0.5f, 0.5f));
                if (name == "Fx_ConeSlam") return (128, new Vector2(0.5f, 1f - 36f / 64));
                if (name == "Fx_ConeLift") return (64, new Vector2(0.5f, 1f - 40f / 48));
                if (name.StartsWith("Fx_ArmorChip")) return (40, new Vector2(0.5f, 0.5f));
                return (72, new Vector2(0.5f, 1f - 98f / 104));
            }
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

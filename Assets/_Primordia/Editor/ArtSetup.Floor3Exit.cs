using System;
using System.IO;
using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Editor
{
    public static partial class ArtSetup
    {
        internal const string Floor3ExitFolder = "Assets/_Primordia/Art/FloorTransition/Sand";
        const string Floor3ExitSource = "docs/multimedia-temp/floor3-exit";

        internal static (RoomTheme mid, RoomTheme next, TransitionPitArt pit) ImportFloor3Exit()
        {
            var burnt = LoadFloor3Theme(BurntThemeName);
            if (burnt == null)
                throw new InvalidOperationException("Falta el tema Burnt aprobado; no se puede crear la salida del piso 3.");

            // Solo se copian los PNG aprobados. No se ejecutan generadores ni se transforman imágenes.
            foreach (string subfolder in new[] { "BurntSand", "Permian", "blend" })
            {
                string source = $"{Floor3ExitSource}/{subfolder}";
                foreach (string path in Directory.GetFiles(source, "*.png", SearchOption.AllDirectories))
                {
                    string relative = path.Substring(Floor3ExitSource.Length).Replace('\\', '/');
                    string target = Floor3ExitFolder + relative;
                    if (!CopyArt(path, target))
                        throw new FileNotFoundException(path);
                    ImportSingleSprite(target);
                }
            }
            var missing = CreateMissingTile();
            var mid = ImportFloor3Theme("BurntSand", missing, $"{Floor3ExitFolder}/BurntSand");
            var next = ImportFloor3Theme("Permian", missing, $"{Floor3ExitFolder}/Permian");
            string blend = $"{Floor3ExitFolder}/blend";
            Directory.CreateDirectory($"{blend}/TileAssets");
            TileBase Tile(string name, bool collides = false)
            {
                string path = $"{blend}/{name}.png";
                if (!File.Exists(path))
                    throw new FileNotFoundException("Falta una pieza de mezcla aprobada", path);
                return CreateTile($"{blend}/TileAssets/{name}.asset", ImportSingleSprite(path), collides);
            }
            BlendSet Set(string prefix, RoomTheme from, RoomTheme[] to, bool collides = false) => new(from, to,
                Enumerable.Range(0, 16).Select(mask => mask == 0 ? null : Tile($"{prefix}_{mask}", collides)).ToArray(),
                new[] { "NE", "NO", "SE", "SO" }.Select(corner => Tile($"{prefix}_Corner_{corner}", collides)).ToArray());

            // Las reglas viven en el tema intermedio; Burnt se conserva intacto para el resto del piso.
            var cap = Set("SandCap", burnt, new[] { next }, true);
            mid.ConfigureBlend(new[]
            {
                Set("DustEdge", burnt, new[] { mid }),
                Set("SandEdge", mid, new[] { next }),
                Set("AshEdge", next, new[] { burnt, mid }),
            }, new[] { cap, new BlendSet(mid, cap.neighbors, cap.tiles, cap.corners) },
                (from side in new[] { "BurntToSand", "SandToBurnt" }
                 from height in new[] { "Upper", "Lower" }
                 from variant in new[] { 0, 1 }
                 select Tile($"Wall_Face{height}_{side}_{variant}", true)).ToArray(),
                Enumerable.Range(0, 3).Select(i => ImportSingleSprite($"{blend}/Sand_Drift_{i}.png")).ToArray());
            EditorUtility.SetDirty(mid);

            string pit = $"{Floor3ExitFolder}/Transition_Pit_Sand.png";
            string dust = $"{Floor3ExitFolder}/Transition_Dust_Sand.png";
            if (!CopyArt($"{Floor3ExitSource}/Transition_Pit_Sand.png", pit)
                || !CopyArt($"{Floor3ExitSource}/Transition_Dust_Sand.png", dust))
                throw new FileNotFoundException("Falta el sumidero o el polvo aprobado.");
            // La tecla ya forma parte del arte importado de las salas anteriores.
            var key = AssetDatabase.LoadAllAssetsAtPath($"{ShopArtFolder}/UI_Key_E.png").OfType<Sprite>()
                .OrderBy(sprite => sprite.name).ToArray();
            AssetDatabase.SaveAssets();
            return (mid, next, new TransitionPitArt
            {
                Pit = SliceSheet(pit, PitCellWidth), Bubble = SliceSheet(dust, BubbleCellWidth), Key = key,
            });
        }

        internal const string ExitDecorAssetPath = "Assets/_Primordia/Data/Rooms/ExitDecorArt_Floor3.asset";

        /// <summary>
        /// Importa el decorado aprobado de la sala de paso (docs/multimedia-temp/floor3-exit/decor) y lo reúne en un
        /// <see cref="ExitDecorArt"/>. Pivote abajo-centro salvo las calcomanías del suelo (huellas, cola, barro, sal), que lo llevan
        /// en el centro; las hojas se cortan por el número de cuadros (plantas: 4 de balanceo y 6 de ráfaga).
        /// </summary>
        internal static ExitDecorArt ImportFloor3ExitDecor()
        {
            string source = $"{Floor3ExitSource}/decor";
            string target = $"{Floor3ExitFolder}/decor";
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException(source);

            var pieces = new System.Collections.Generic.List<ExitDecorArt.Piece>();
            foreach (string path in Directory.GetFiles(source, "*.png").OrderBy(file => file))
            {
                string id = Path.GetFileNameWithoutExtension(path);
                string imported = $"{target}/{id}.png";
                if (!CopyArt(path, imported))
                    throw new FileNotFoundException(path);

                int frames = id.EndsWith("_Sway") ? 4 : id.EndsWith("_Rustle") || id.StartsWith("Fx_SandGust") ? 6 : 1;
                bool ground = id.StartsWith("Deco_Track") || id.StartsWith("Deco_TailDrag") || id.StartsWith("Deco_MudCracks")
                    || id.StartsWith("Deco_SaltCrust") || id.StartsWith("Fx_");
                var pivot = ground ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
                ((TextureImporter)AssetImporter.GetAtPath(imported)).GetSourceTextureWidthAndHeight(out int width, out _);
                pieces.Add(new ExitDecorArt.Piece { id = id, frames = SliceSheet(imported, width / frames, pivot) });
            }

            var art = AssetDatabase.LoadAssetAtPath<ExitDecorArt>(ExitDecorAssetPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<ExitDecorArt>();
                AssetDatabase.CreateAsset(art, ExitDecorAssetPath);
            }
            art.pieces = pieces.ToArray();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art;
        }
    }
}

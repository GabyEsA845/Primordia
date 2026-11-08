using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte del Tronco caído, el Tocón hueco y los cofres de cono (docs/multimedia-temp/floor3-log-stump y floor3-environment/pieces):
    /// corta las hojas del tronco de cada tramo, las burbujas de lodo, el rayo de luz y los dos cofres de cono en el
    /// <see cref="SpecialRoomArt"/>, y crea el tileset propio del tocón (<c>Theme_Stump</c>). Parte del arte que ya copió
    /// Primordia > Floor 3 > Importar todo el arte del piso 3 (esta importación lo repite antes de cortar).
    /// </summary>
    public static partial class ArtSetup
    {
        internal const string StumpThemeName = "Stump";
        const string LogStumpFolder = Floor3ArtRoot + "/floor3-log-stump";
        const string ConePiecesFolder = Floor3EnvironmentFolder + "/pieces";
        static readonly string[] LogBands = { "Swamp", "Smoke", "Burnt" };

        /// <summary>Tileset del Tocón hueco (carpeta Stump del arte). Null si falta el arte.</summary>
        internal static RoomTheme ImportStumpTheme()
        {
            ImportFloor3Art();
            var theme = ImportFloor3Theme(StumpThemeName, CreateMissingTile(), $"{LogStumpFolder}/{StumpThemeName}");
            if (theme == null)
                return null;

            // Los obstáculos de cobertura del tocón: trozos de madera podrida y matas de setas (1×1, el pie en la base de la casilla).
            var small = new[] { ("Deco_WoodChunk_A", 22), ("Deco_WoodChunk_B", 16), ("Deco_Mushrooms_A", 18), ("Deco_Mushrooms_B", 18), ("Deco_Mushrooms_C", 18) }
                .Select(piece =>
                {
                    string path = $"{LogStumpFolder}/pieces/{piece.Item1}.png";
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.GetSourceTextureWidthAndHeight(out _, out int height);
                    return SliceSheet(path, piece.Item2, new Vector2(0.5f, 8f / height)).FirstOrDefault();
                })
                .Where(sprite => sprite != null)
                .ToArray();
            theme.ConfigureObstacles(small, new Sprite[0]);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>Rellena el tronco por tramo, las burbujas de lodo, el rayo de luz y los cofres de cono del arte de las salas especiales.</summary>
        internal static void ImportLogStumpArt(SpecialRoomArt art)
        {
            ImportFloor3Art();
            var bottomLeft = new Vector2(0f, 8f / 48f);
            var endPivot = new Vector2(0f, 16f / 64f);

            art.log = new SpecialRoomArt.LogBand[SpecialRoomArt.Bands];
            for (int band = 0; band < LogBands.Length; band++)
            {
                string folder = $"{LogStumpFolder}/{LogBands[band]}";
                SpecialRoomArt.Frames Sheet(string name, int cell, Vector2 pivot) =>
                    new() { frames = SliceSheet($"{folder}/{name}.png", cell, pivot) };

                var mid = new SpecialRoomArt.LogPiece[6];
                for (int variant = 0; variant < mid.Length; variant++)
                {
                    mid[variant] = new SpecialRoomArt.LogPiece
                    {
                        idle = Sheet($"Log_Mid_{variant}_Idle", 16, bottomLeft),
                        step = Sheet($"Log_Mid_{variant}_Step", 16, bottomLeft),
                        dip = Sheet($"Log_Mid_{variant}_Dip", 16, bottomLeft),
                    };
                }

                art.log[band] = new SpecialRoomArt.LogBand
                {
                    mid = mid,
                    ground = Enumerable.Range(0, 2).Select(i => SliceSheet($"{folder}/Log_Ground_{i}.png", 16, bottomLeft).FirstOrDefault()).ToArray(),
                    endLeft = SliceSheet($"{folder}/Log_EndLeft.png", 40, endPivot).FirstOrDefault(),
                    endRight = SliceSheet($"{folder}/Log_EndRight.png", 48, endPivot).FirstOrDefault(),
                };
            }

            var center = new Vector2(0.5f, 0.5f);
            art.mudBubble = SliceSheet($"{LogStumpFolder}/bubbles/Fx_MudBubble.png", 12, center);
            art.mudBubbleSmall = SliceSheet($"{LogStumpFolder}/bubbles/Fx_MudBubble_Small.png", 8, center);
            // El centro del charco de luz, (38, 100) contando desde arriba, es el pivote: va bajo el pie del cono.
            art.stumpLight = SliceSheet($"{LogStumpFolder}/pieces/Fx_StumpLight.png", 64, new Vector2(38f / 64f, 1f - 100f / 112f));

            // Cofres de cono: el pie en el borde de abajo de su casilla (el cofre está a medio tile sobre él).
            art.coneBig = ConeArt("Big", 32, 42);
            art.coneSmall = ConeArt("Small", 18, 26);
        }

        /// <summary>Muro de raíces de cada tramo y flores moradas del camino secreto (arte de floor3-environment, ya copiado).</summary>
        internal static void ImportSecretRoomArt(SpecialRoomArt art)
        {
            ImportFloor3Art();
            var foot = new Vector2(0.5f, 0f);
            art.rootWall = new SpecialRoomArt.RootWallArt[SpecialRoomArt.Bands];
            for (int band = 0; band < LogBands.Length; band++)
            {
                string folder = $"{Floor3EnvironmentFolder}/{LogBands[band]}/pieces";
                Sprite[] Sheet(string name) => SliceSheet($"{folder}/{name}.png", 128, foot);
                art.rootWall[band] = new SpecialRoomArt.RootWallArt
                {
                    locked = Sheet("RootWall_Locked").FirstOrDefault(),
                    idle = Sheet("RootWall_Idle"),
                    insertHeart = Sheet("RootWall_InsertHeart"),
                    insertSeed = Sheet("RootWall_InsertSeed"),
                    open = Sheet("RootWall_Open"),
                    opened = Sheet("RootWall_Opened").FirstOrDefault(),
                    openedFront = Sheet("RootWall_OpenedFront").FirstOrDefault(),
                };
            }
            art.purpleFlower = SliceSheet($"{ConePiecesFolder}/Flower_Purple_Idle.png", 16, foot);
            art.purpleFlowerPick = SliceSheet($"{ConePiecesFolder}/Flower_Purple_Pick.png", 16, foot);
        }

        static SpecialRoomArt.ConeArt ConeArt(string size, int width, int height)
        {
            var pivot = new Vector2(0.5f, 8f / height);
            Sprite[] Sheet(string state) => SliceSheet($"{ConePiecesFolder}/Cone_{size}_{state}.png", width, pivot);
            return new SpecialRoomArt.ConeArt { idle = Sheet("Idle"), open = Sheet("Open"), opened = Sheet("Opened").FirstOrDefault() };
        }
    }
}

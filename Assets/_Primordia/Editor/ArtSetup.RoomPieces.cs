using System.IO;
using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Piezas de las salas especiales del piso 2 (docs/multimedia-temp/floor2-rooms/Pieces): el esqueleto del Fósil gigante,
    /// las almejas, el coral del corazón morado y las entradas de oleadas. De momento se usa el primer cuadro de cada una
    /// (abrirlas, cogerlas y las oleadas llegan en el paso 4 de feature/floor-2-structure).
    /// </summary>
    public static partial class ArtSetup
    {
        const string RoomPiecesSourceFolder = "docs/multimedia-temp/floor2-rooms/Pieces";
        const string RoomPiecesArtFolder = "Assets/_Primordia/Art/Rooms/Pieces";
        const string PurpleInlaySourceFolder = "docs/multimedia-temp/ui-purple-heart";

        internal static FloorConfig.RoomPieces ImportRoomPieces()
        {
            CopyFiles(RoomPiecesSourceFolder, RoomPiecesArtFolder, "*.png");
            var center = new Vector2(0.5f, 0.5f);
            // La almeja grande (32×24) sobresale 8 px por arriba de sus dos celdas: el pivote, en el centro de las celdas.
            var bigClamPivot = new Vector2(0.5f, 8f / 24f);
            var pieces = new FloorConfig.RoomPieces
            {
                // El esqueleto ocupa la sala entera y se coloca por su esquina de arriba a la izquierda.
                fossil = ImportPivoted($"{RoomPiecesArtFolder}/Fossil_Skeleton.png", new Vector2(0f, 1f)),
                clamSmallIdle = Frames("Clam_Small_Idle", 16, center),
                clamSmallOpen = Frames("Clam_Small_Open", 16, center),
                clamBigIdle = Frames("Clam_Big_Idle", 32, bigClamPivot),
                clamBigOpen = Frames("Clam_Big_Open", 32, bigClamPivot),
                heartCoralIdle = Frames("HeartCoral_Idle", 32, center),
                waveVentIdle = Frames("WaveVent_Idle", 16, center),
                waveVentActive = Frames("WaveVent_Active", 16, center),
                keyPrompt = File.Exists($"{ShopSourceFolder}/UI_Key_E.png") && CopyArt($"{ShopSourceFolder}/UI_Key_E.png", $"{ShopArtFolder}/UI_Key_E.png")
                    ? SliceSheet($"{ShopArtFolder}/UI_Key_E.png", KeyCellWidth, new Vector2(0.5f, 0f))
                    : System.Array.Empty<Sprite>(),
            };
            pieces.clamSmallOpened = First(Frames("Clam_Small_Opened", 16, center));
            pieces.clamBigOpened = First(Frames("Clam_Big_Opened", 32, bigClamPivot));
            pieces.heartCoralTaken = First(Frames("HeartCoral_Taken", 32, center));
            pieces.clamSmall = First(pieces.clamSmallIdle);
            pieces.clamBig = First(pieces.clamBigIdle);
            pieces.heartCoral = First(pieces.heartCoralIdle);
            pieces.waveVent = First(pieces.waveVentIdle);

            // El corazón morado en el HUD, detrás de los verdes.
            var purple = ImportSprite($"{RoomPiecesSourceFolder}/UI_Heart_Purple.png", $"{UIArtFolder}/UI_Heart_Purple.png", Vector4.zero);
            // Con el corazón morado, el morado se incrusta en los rojos: contorno y grietas (docs/multimedia-temp/ui-purple-heart).
            var inlays = Enumerable.Range(0, 3)
                .Select(i => ImportSprite($"{PurpleInlaySourceFolder}/UI_Heart_PurpleInlay_{i}.png", $"{UIArtFolder}/UI_Heart_PurpleInlay_{i}.png", Vector4.zero))
                .Where(sprite => sprite != null).ToArray();
            if (purple != null || inlays.Length > 0)
            {
                EditPrefab(UISetup.GameUIPath, root =>
                {
                    foreach (var display in root.GetComponentsInChildren<UI.HeartsDisplay>(true))
                    {
                        display.SetPurpleSprite(purple);
                        display.SetPurpleInlays(inlays);
                    }
                });
            }
            return pieces;
        }

        static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        static Sprite[] Frames(string name, int cellWidth, Vector2 pivot)
        {
            string path = $"{RoomPiecesArtFolder}/{name}.png";
            return File.Exists(path) ? SliceSheet(path, cellWidth, pivot) : System.Array.Empty<Sprite>();
        }

        static Sprite ImportPivoted(string path, Vector2 pivot)
        {
            if (!File.Exists(path))
                return null;
            var sprite = ImportSingleSprite(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? sprite;
        }
    }
}

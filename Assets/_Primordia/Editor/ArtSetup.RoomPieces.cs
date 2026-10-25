using System.IO;
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

        internal static FloorConfig.RoomPieces ImportRoomPieces()
        {
            CopyFiles(RoomPiecesSourceFolder, RoomPiecesArtFolder, "*.png");
            return new FloorConfig.RoomPieces
            {
                // El esqueleto ocupa la sala entera y se coloca por su esquina de arriba a la izquierda.
                fossil = ImportPivoted($"{RoomPiecesArtFolder}/Fossil_Skeleton.png", new Vector2(0f, 1f)),
                clamSmall = FirstFrame("Clam_Small_Idle", 16, new Vector2(0.5f, 0.5f)),
                // La almeja grande (32×24) sobresale 8 px por arriba de sus dos celdas: el pivote, en el centro de las celdas.
                clamBig = FirstFrame("Clam_Big_Idle", 32, new Vector2(0.5f, 8f / 24f)),
                heartCoral = FirstFrame("HeartCoral_Idle", 32, new Vector2(0.5f, 0.5f)),
                waveVent = FirstFrame("WaveVent_Idle", 16, new Vector2(0.5f, 0.5f)),
            };
        }

        static Sprite FirstFrame(string name, int cellWidth, Vector2 pivot)
        {
            string path = $"{RoomPiecesArtFolder}/{name}.png";
            if (!File.Exists(path))
                return null;
            var frames = SliceSheet(path, cellWidth, pivot);
            return frames.Length > 0 ? frames[0] : null;
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

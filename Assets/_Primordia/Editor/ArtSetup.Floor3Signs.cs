using System.IO;
using System.Linq;
using Primordia.Core;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Letrero de las salas con tema del piso 3 (docs/multimedia-temp/floor3-room-signs): importa el tronco, los 5 nombres y
    /// los 5 iconos a Art/Floor3/floor3-room-signs y los reúne en Resources/RoomSignArt, que el letrero busca al jugar.
    /// </summary>
    public static partial class ArtSetup
    {
        const string RoomSignsSourceFolder = "docs/multimedia-temp/floor3-room-signs";
        const string RoomSignsArtFolder = "Assets/_Primordia/Art/Floor3/floor3-room-signs";
        const string RoomSignsAssetPath = "Assets/_Primordia/Resources/RoomSignArt.asset";

        static readonly string[] RoomSignKeys = { "CalamitesMeadow", "BeetleNest", "ToadPond", "SpiderLoom", "DragonflySky" };

        [MenuItem("Primordia/Floor 3/Importar letreros de las salas con tema")]
        internal static void ImportRoomSigns()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RoomSignsAssetPath));
            var art = AssetDatabase.LoadAssetAtPath<RoomSignArt>(RoomSignsAssetPath);
            bool isNew = art == null;
            if (isNew)
                art = ScriptableObject.CreateInstance<RoomSignArt>();

            art.log = RoomSignSprite("UI_RoomSign_Log");
            art.entries = RoomSignKeys
                .Select(key => new RoomSignArt.Entry
                {
                    key = key,
                    title = RoomSignSprite($"UI_RoomSign_Title_{key}"),
                    icon = RoomSignSprite($"UI_RoomSign_Icon_{key}"),
                })
                .Where(entry => entry.title != null)
                .ToArray();

            if (isNew)
                AssetDatabase.CreateAsset(art, RoomSignsAssetPath);
            else
                EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: letreros de las salas con tema importados en Resources/RoomSignArt ({art.entries.Length} temas).");
        }

        static Sprite RoomSignSprite(string name)
        {
            string target = $"{RoomSignsArtFolder}/{name}.png";
            if (!CopyArt($"{RoomSignsSourceFolder}/{name}.png", target))
                return null;
            return ImportSingleSprite(target);
        }
    }
}

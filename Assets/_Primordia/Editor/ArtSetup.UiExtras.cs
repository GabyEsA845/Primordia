using Primordia.Core;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Extras de interfaz (docs/multimedia-temp/ui-extras): medio corazón del suelo, corazón verde entero del HUD, iconos del
    /// mapa y corazón morado. Importa las hojas a Art/UIExtras, las reúne en Resources/UiExtrasArt (que lo provisional por
    /// código busca al arrancar) y pone la fuente pixel al letrero del mapa grande.
    /// </summary>
    public static partial class ArtSetup
    {
        const string UiExtrasSourceFolder = "docs/multimedia-temp/ui-extras";
        const string UiExtrasArtFolder = "Assets/_Primordia/Art/UIExtras";
        const string UiExtrasAssetPath = "Assets/_Primordia/Resources/UiExtrasArt.asset";

        [MenuItem("Primordia/Art/Importar extras de interfaz")]
        internal static void ImportUiExtras()
        {
            System.IO.Directory.CreateDirectory(UiExtrasArtFolder);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(UiExtrasAssetPath));

            var center = new Vector2(0.5f, 0.5f);
            var art = AssetDatabase.LoadAssetAtPath<UiExtrasArt>(UiExtrasAssetPath);
            bool isNew = art == null;
            if (isNew)
                art = ScriptableObject.CreateInstance<UiExtrasArt>();

            art.roomHeartIdle = ExtrasSheet("Items", "Item_RoomHeart_Idle", 16, center);
            art.roomHeartShadow = First(ExtrasSheet("Items", "Item_RoomHeart_Shadow", 16, center));
            art.roomHeartCollect = ExtrasSheet("Items", "Item_RoomHeart_Collect", 16, center);
            art.roomHeartFly = ExtrasSheet("Items", "Item_RoomHeart_Fly", 16, center);
            art.barrierFull = First(ExtrasSheet("HUD", "UI_Heart_BarrierFull", 16, center));
            art.mapHeart = First(ExtrasSheet("Map", "UI_Map_Heart", 7, center));
            art.mapTeleport = ExtrasSheet("Map", "UI_Map_Teleport", 7, center);
            art.mapTeleportHover = ExtrasSheet("Map", "UI_Map_Teleport_Hover", 7, center);
            art.mapTeleportDim = First(ExtrasSheet("Map", "UI_Map_Teleport_Dim", 7, center));
            art.mapReturn = First(ExtrasSheet("Map", "UI_Map_Return", 9, center));
            art.mapReturnHover = First(ExtrasSheet("Map", "UI_Map_Return_Hover", 9, center));
            art.mapReturnDim = First(ExtrasSheet("Map", "UI_Map_Return_Dim", 9, center));
            art.purpleAura = ExtrasSheet("Effects", "Fx_PurpleHeart_Aura", 32, center);
            art.purpleMote = ExtrasSheet("Effects", "Fx_PurpleHeart_Mote", 4, center);
            art.purpleBlock = ExtrasSheet("Effects", "Fx_PurpleHeart_Block", 48, center);

            if (isNew)
                AssetDatabase.CreateAsset(art, UiExtrasAssetPath);
            else
                EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();

            // El letrero del mapa grande, con la fuente pixel del juego.
            var font = AssetDatabase.LoadAssetAtPath<Font>(UISetup.FontPath);
            if (font != null)
            {
                EditPrefab(UISetup.GameUIPath, root =>
                {
                    foreach (var map in root.GetComponentsInChildren<MapUI>(true))
                        map.SetHintFont(font);
                });
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: extras de interfaz importados en Resources/UiExtrasArt (corazón del suelo, corazón verde entero, iconos del mapa y corazón morado).");
        }

        static Sprite[] ExtrasSheet(string group, string name, int cellWidth, Vector2 pivot)
        {
            string file = $"{name}.png";
            if (!CopyArt($"{UiExtrasSourceFolder}/{group}/{file}", $"{UiExtrasArtFolder}/{file}"))
            {
                Debug.LogWarning($"ArtSetup: falta {UiExtrasSourceFolder}/{group}/{file}.");
                return System.Array.Empty<Sprite>();
            }
            return SliceSheet($"{UiExtrasArtFolder}/{file}", cellWidth, pivot);
        }
    }
}

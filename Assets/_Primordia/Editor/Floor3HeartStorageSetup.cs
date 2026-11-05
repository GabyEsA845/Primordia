using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Almacén de corazones y cucaracha ladrona del piso 3 (paso 6, bloque 3): importa la vaina de la tienda
    /// (docs/multimedia-temp/floor3-environment/pieces) y las hojas de la cucaracha (floor3-roach) y las reúne en
    /// Resources/HeartStorageArt, que el juego busca al montar la tienda y al robar un corazón.
    /// </summary>
    public static class Floor3HeartStorageSetup
    {
        const string PodSource = "docs/multimedia-temp/floor3-environment/pieces";
        const string RoachSource = "docs/multimedia-temp/floor3-roach";
        const string ArtFolder = "Assets/_Primordia/Art/Floor3/HeartStorage";
        const string AssetPath = "Assets/_Primordia/Resources/HeartStorageArt.asset";

        [MenuItem("Primordia/Floor 3/Importar almacén de corazones y cucaracha")]
        internal static void Import()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AssetPath));
            var art = AssetDatabase.LoadAssetAtPath<HeartStorageArt>(AssetPath);
            bool isNew = art == null;
            if (isNew)
                art = ScriptableObject.CreateInstance<HeartStorageArt>();

            var center = new Vector2(0.5f, 0.5f);
            // La vaina apoya en el borde de abajo.
            var foot = new Vector2(0.5f, 0f);
            art.podStored = Sheet(PodSource, "HeartPod_Stored", 32, foot);
            art.podReceive = Sheet(PodSource, "HeartPod_Receive", 32, foot);

            art.idle = Sheet(RoachSource, "Roach_Idle", 40, center);
            art.run = Sheet(RoachSource, "Roach_Run", 40, center);
            art.grab = Sheet(RoachSource, "Roach_Grab", 40, center);
            art.hurt = Sheet(RoachSource, "Roach_Hurt", 40, center);
            art.death = Sheet(RoachSource, "Roach_Death", 40, center);
            // La grieta de la que sale está en (10, 14) de 50×28; la de la que se esconde, en (39, 14).
            art.emerge = Sheet(RoachSource, "Roach_Emerge", 50, new Vector2(10f / 50f, 0.5f));
            art.hide = Sheet(RoachSource, "Roach_Hide", 50, new Vector2(39f / 50f, 0.5f));
            var crack = Sheet(RoachSource, "Deco_RoachCrack", 20, center);
            art.crack = crack.Length > 0 ? crack[0] : null;

            if (isNew)
                AssetDatabase.CreateAsset(art, AssetPath);
            else
                EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3HeartStorageSetup: vaina ({art.podStored.Length}+{art.podReceive.Length} cuadros) y cucaracha ({art.run.Length} de carrera, {art.emerge.Length} al salir, {art.hide.Length} al esconderse) en Resources/HeartStorageArt.");
        }

        static Sprite[] Sheet(string source, string name, int cell, Vector2 pivot) =>
            ArtSetup.ImportLooseSheet($"{source}/{name}.png", $"{ArtFolder}/{name}.png", cell, pivot);
    }
}

using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Escena para probar las 5 salas con tema del piso 3 de golpe: un piso corto (inicio, 5 salas de combate —una por tema,
    /// en orden—, antesala y jefe), sin tienda, sin salas especiales, sin laterales y sin atajos. Su configuración
    /// (<c>Floor_3_Themes</c>) es una copia de Floor_3, así que se vuelve a generar cada vez para recoger sus cambios.
    /// </summary>
    public static class Floor3ThemeShowcaseSetup
    {
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_3_Themes.asset";
        const string ScenePath = "Assets/_Primordia/Scenes/Test_Floor3Themes.unity";

        [MenuItem("Primordia/Floor 3/Crear escena de prueba de las salas con tema")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor1 == null || floor3 == null || floor3.EncounterThemes.Length == 0)
            {
                Debug.LogError("Floor3ThemeShowcaseSetup: faltan Floor_1 o Floor_3 con sus salas con tema (Primordia > Floor 3 > Crear salas con tema).");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FloorConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            EditorUtility.CopySerialized(floor3, config);
            int themes = floor3.EncounterThemes.Length;
            config.ConfigureRooms(floor3.StartRoom, floor3.BossRoom, floor3.CombatRooms, floor3.SideRooms, new Vector2Int(themes, themes), Vector2Int.zero);
            config.ConfigureShortcuts(Vector2Int.zero);
            config.ConfigureSpecialRooms(System.Array.Empty<FloorConfig.SpecialRoom>(), Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureThemeShowcase(true);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            // La creación de la escena no sobrescribe una existente: se borra para regenerarla.
            AssetDatabase.DeleteAsset(ScenePath);
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            if (!CoreSetup.TryCreateSceneFromTemplate(ScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Al abrir la escena Unity descarga los assets sin usar: se vuelve a cargar la configuración.
            config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: config);
            Floor2Setup.AddTestLoadout(scene, floor1);
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3ThemeShowcaseSetup: escena Test_Floor3Themes lista ({themes} salas con tema seguidas, una por tema).");
        }
    }
}

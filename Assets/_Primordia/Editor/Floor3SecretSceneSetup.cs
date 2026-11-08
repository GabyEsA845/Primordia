using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Escena de prueba de la sala secreta del piso 3 (paso 6, bloque 5): un piso corto con la antesala y su muro de raíces sí o sí,
    /// con atajos para dar el corazón morado, romperlo, dar la semilla o ir al muro y a las flores (<see cref="SecretTestKeys"/>).
    /// </summary>
    public static class Floor3SecretSceneSetup
    {
        const string ScenePath = "Assets/_Primordia/Scenes/Test_Floor3Secret.unity";
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_3_Secret.asset";

        [MenuItem("Primordia/Floor 3/Crear escena de prueba de la sala secreta")]
        internal static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            var secret = floor3 != null ? floor3.SpecialRooms.Where(room => room.secretChamber != null).ToArray() : null;
            if (floor1 == null || secret == null || secret.Length == 0)
            {
                Debug.LogError("Floor3SecretSceneSetup: faltan Floor_1/Floor_3 o la antesala secreta (Primordia > Floor 3 > Crear salas especiales).");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FloorConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            EditorUtility.CopySerialized(floor3, config);
            secret[0].always = true;
            config.ConfigureRooms(floor3.StartRoom, floor3.BossRoom, floor3.CombatRooms, floor3.SideRooms, new Vector2Int(7, 7), new Vector2Int(2, 2));
            config.ConfigureShortcuts(Vector2Int.zero);
            config.ConfigureSpecialRooms(new[] { secret[0] }, new Vector2Int(1, 1));
            config.ConfigureShop(null, null);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            AssetDatabase.DeleteAsset(ScenePath);
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            if (!CoreSetup.TryCreateSceneFromTemplate(ScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: config);
            Floor2Setup.AddTestLoadout(scene, floor1);
            Floor2Setup.AddHUD(scene);
            new GameObject("SecretTestKeys").AddComponent<SecretTestKeys>();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3SecretSceneSetup: escena Test_Floor3Secret lista.");
        }
    }
}

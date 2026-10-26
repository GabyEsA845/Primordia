using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Anomalocaris, jefe del piso 2: importa las algas y plantas luminosas de su sala en Floor_2 y crea la escena
    /// Test_BossAnomalocaris: la sala de inicio del piso 2 con el equipo de prueba y, tras la antesala, la sala del
    /// Anomalocaris. Se puede volver a ejecutar: actualiza el arte y rehace la escena; lo demás no lo sobrescribe.
    /// </summary>
    public static class AnomalocarisBossSetup
    {
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        const string RoomName = "Room_Boss_Anomalocaris";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossAnomalocaris.unity";
        const string TestConfigPath = RoomsFolder + "/Floor_BossAnomalocarisTest.asset";

        [MenuItem("Primordia/Boss/Crear Anomalocaris y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor2 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            if (floor2 == null)
            {
                Debug.LogError($"AnomalocarisBossSetup: falta {Floor2Setup.Floor2ConfigPath}; crea antes el piso 2 con Primordia > Rooms.");
                return;
            }

            floor2.ConfigurePieces(ArtSetup.ImportRoomPieces());
            EditorUtility.SetDirty(floor2);
            AssetDatabase.SaveAssets();

            var entry = floor2.BossOptions().FirstOrDefault(boss => boss.room != null && boss.room.name == RoomName);
            if (entry.room == null)
            {
                Debug.LogError($"AnomalocarisBossSetup: Floor_2 no tiene la sala {RoomName} entre sus jefes; vuelve a crear el piso 2.");
                return;
            }

            if (CreateTestConfig(floor2, entry) == null)
                return;
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"AnomalocarisBossSetup: {TestScenePath} creada: inicio del piso 2 y, tras la antesala, la sala del Anomalocaris.");
        }

        /// <summary>Copia de Floor_2 con el camino inicio → antesala → Anomalocaris, sin combates, especiales, tienda ni recompensas.</summary>
        static FloorConfig CreateTestConfig(FloorConfig floor2, FloorConfig.BossEntry entry)
        {
            AssetDatabase.DeleteAsset(TestConfigPath);
            var config = Object.Instantiate(floor2);
            AssetDatabase.CreateAsset(config, TestConfigPath);

            config.ConfigureRooms(floor2.StartRoom, entry.room, floor2.CombatRooms, System.Array.Empty<RoomTemplate>(),
                Vector2Int.zero, Vector2Int.zero);
            config.ConfigureSpecialRooms(System.Array.Empty<FloorConfig.SpecialRoom>(), Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureFixedRooms(null, floor2.AntechamberVertical, floor2.AntechamberHorizontal);
            config.ConfigureRewards(floor2.RewardPool, System.Array.Empty<int>());
            config.ConfigureBosses(new[] { entry });

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        static void CreateTestScene()
        {
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);

            // Otra escena abierta antes de borrar, por si era la abierta.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se vuelve a cargar: al abrir la escena nueva Unity descarga los assets sin usar, y la copia de antes ya no vale.
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

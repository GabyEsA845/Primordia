using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Euriptérido, jefe del piso 2: importa las piezas de su sala (las rocas agrietadas que se derrumban con la muda y el
    /// derrumbe de la cueva), vuelve a leer su plano (con las zonas Z que se hunden), pone la opción de temblor en la pausa y
    /// crea la escena Test_BossEurypterid: la sala de inicio del piso 2 con el equipo de prueba y, tras la antesala, la sala del
    /// Euriptérido. De momento pelea en ella el jefe provisional del piso 1 (el Euriptérido llega en el paso 2). Se puede
    /// volver a ejecutar: rehace la escena.
    /// </summary>
    public static class EurypteridBossSetup
    {
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        const string RoomName = "Room_Boss_Eurypterid";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossEurypterid.unity";
        const string TestConfigPath = RoomsFolder + "/Floor_BossEurypteridTest.asset";
        const string LayoutPath = "docs/multimedia-temp/floor2-rooms/Layouts/" + RoomName + ".txt";

        [MenuItem("Primordia/Boss/Crear Euriptérido y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor2 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            if (floor2 == null)
            {
                Debug.LogError($"EurypteridBossSetup: falta {Floor2Setup.Floor2ConfigPath}; crea antes el piso 2 con Primordia > Rooms.");
                return;
            }

            floor2.ConfigurePieces(ArtSetup.ImportRoomPieces());
            EditorUtility.SetDirty(floor2);
            AssetDatabase.SaveAssets();
            if (floor2.Pieces.crackedRock == null)
                Debug.LogWarning("EurypteridBossSetup: falta el arte en docs/multimedia-temp/floor2-bosses/EurypteridRoom (generar_sala_euripterido.py); las rocas R serán rocas normales.");

            var options = floor2.BossOptions();
            int index = System.Array.FindIndex(options, boss => boss.room != null && boss.room.name == RoomName);
            if (index < 0)
            {
                Debug.LogError($"EurypteridBossSetup: Floor_2 no tiene la sala {RoomName} entre sus jefes; vuelve a crear el piso 2.");
                return;
            }

            // El piso 2 no sobrescribe las plantillas que ya existen: la de esta sala se actualiza aquí con su plano.
            var template = options[index].room;
            if (System.IO.File.Exists(LayoutPath))
            {
                template.SetLayout(System.IO.File.ReadAllText(LayoutPath));
                EditorUtility.SetDirty(template);
                var problems = template.Validate();
                if (problems.Count > 0)
                    Debug.LogWarning($"EurypteridBossSetup: la plantilla {RoomName} tiene problemas:\n{string.Join("\n", problems)}");
                AssetDatabase.SaveAssets();
            }

            ArtSetup.RebuildPauseOptions();

            if (CreateTestConfig(floor2, options[index]) == null)
                return;
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"EurypteridBossSetup: {TestScenePath} creada: inicio del piso 2 y, tras la antesala, la sala del Euriptérido (F9 derrumba sus rocas, F10 cambia la fase del derrumbe).");
        }

        /// <summary>Copia de Floor_2 con el camino inicio → antesala → Euriptérido, sin combates, especiales, tienda ni recompensas.</summary>
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

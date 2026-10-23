using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Escena Test_Lineages para probar todas las mutaciones y linajes: un piso fijo con la sala de inicio (grande)
    /// llena de burbujas, 3 de cada mutación (<see cref="MutationBench"/>), una sala grande llena de enemigos y la
    /// sala del jefe. Sin salas laterales, sin tienda y sin elecciones de mutación. Usa su propia configuración de
    /// piso (copia de Floor_1), así no toca la del juego. Se puede volver a ejecutar: rehace la escena.
    /// </summary>
    public static class LineageTestSetup
    {
        const string ScenePath = "Assets/_Primordia/Scenes/Test_Lineages.unity";
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_LineageTest.asset";
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations";
        const string PickupPath = "Assets/_Primordia/Prefabs/Mutations/MutationPickup.prefab";

        // Presupuesto de enemigos de la sala de combate: perseguidor 1, tirador 2; en la sala grande se multiplica
        // por la raíz de su área (unos 18 puntos).
        const float EnemyBudget = 12f;

        [MenuItem("Primordia/Mutations/Crear escena de prueba de linajes")]
        static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var config = CreateConfig();
            if (config == null)
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            UISetup.CreateUIAssets();

            // Otra escena abierta antes de borrar, por si era la abierta.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(ScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(ScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se vuelve a cargar: al abrir la escena nueva Unity descarga los assets sin usar, y la copia de antes ya no vale.
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UISetup.GameUIPath), scene);
            UISetup.CreateEventSystem();

            var mutations = AssetDatabase.FindAssets("t:MutationData", new[] { MutationsFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(m => m != null)
                .ToArray();
            var pickup = AssetDatabase.LoadAssetAtPath<MutationPickup>(PickupPath);
            if (pickup == null)
                Debug.LogWarning("LineageTestSetup: falta MutationPickup; créalo con Primordia > Art > Importar objetos e interfaz.");

            var bench = new GameObject("MutationBench").AddComponent<MutationBench>();
            bench.Configure(pickup, mutations, newCopies: 3);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"LineageTestSetup: {ScenePath} creada con {mutations.Length} mutaciones (3 burbujas de cada una).");
        }

        /// <summary>Copia de Floor_1 con el camino inicio → combate → jefe, sin laterales, tienda ni recompensas.</summary>
        static FloorConfig CreateConfig()
        {
            var floor = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            if (floor == null)
            {
                Debug.LogError($"LineageTestSetup: falta {FloorSetup.FloorConfigPath}; crea antes el juego con Primordia > UI.");
                return null;
            }

            AssetDatabase.DeleteAsset(ConfigPath);
            var config = Object.Instantiate(floor);
            AssetDatabase.CreateAsset(config, ConfigPath);

            var large = AssetDatabase.LoadAssetAtPath<RoomTemplate>($"{RoomsFolder}/{RoomLayouts.Large}.asset");
            if (large == null)
            {
                Debug.LogError($"LineageTestSetup: falta la plantilla {RoomLayouts.Large}.");
                return null;
            }

            config.ConfigureRooms(large, floor.BossRoom, new[] { large }, System.Array.Empty<RoomTemplate>(),
                new Vector2Int(1, 1), Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureRewards(floor.RewardPool, System.Array.Empty<int>());
            // El tirador también desde la primera sala, que es la única.
            config.ConfigureEnemies(floor.Enemies.Select(e => new FloorConfig.EnemyEntry(e.prefab, e.cost, 1)).ToArray(),
                floor.Boss, EnemyBudget, 0f);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}

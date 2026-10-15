using System.IO;
using System.Linq;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Primordia.Editor
{
    /// <summary>
    /// Prepara el piso aleatorio: las plantillas de sala (<see cref="RoomLayouts"/>), la configuración del piso
    /// (<see cref="FloorConfig"/>) y, en la escena, el <see cref="FloorGenerator"/> que monta un piso distinto en
    /// cada partida. La cámara sigue al jugador, adelantada hacia el cursor (<see cref="CameraLookAhead"/>).
    /// </summary>
    public static class FloorSetup
    {
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Floor.unity";
        const string TemplatesFolder = "Assets/_Primordia/Data/Rooms";
        internal const string FloorConfigPath = TemplatesFolder + "/Floor_1.asset";
        internal const string SoupThemePath = "Assets/_Primordia/Art/Environment/Soup/Theme_Soup.asset";
        internal const string BossThemePath = "Assets/_Primordia/Art/Environment/Boss/Theme_Boss.asset";

        static readonly string[] CombatTemplates =
            { RoomLayouts.Normal, RoomLayouts.Pillars, RoomLayouts.Cross, RoomLayouts.LShape, RoomLayouts.Large };
        static readonly string[] SideTemplates = { RoomLayouts.Small, RoomLayouts.Normal, RoomLayouts.Pillars };

        [MenuItem("Primordia/Rooms/Crear piso de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            CreateAssets(square);

            CreateTestScene(cameraRig, gameManager, player);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Todo lo que el piso necesita: combate, enemigos, jefe, plantillas y configuración del piso.</summary>
        internal static void CreateAssets(Sprite square)
        {
            CombatSetup.CreateAssets(square);
            EnemySetup.CreateAssets(square);
            BossSetup.CreateAssets(square);
            CreateTemplates();
            CreateFloorConfig(square);
        }

        /// <summary>Un asset por forma del catálogo. No sobrescribe los que ya existen, que se pueden retocar a mano.</summary>
        static void CreateTemplates()
        {
            Directory.CreateDirectory(TemplatesFolder);
            foreach (var (name, layout) in RoomLayouts.All)
            {
                string path = TemplatePath(name);
                if (AssetDatabase.LoadAssetAtPath<RoomTemplate>(path) != null)
                    continue;

                var template = ScriptableObject.CreateInstance<RoomTemplate>();
                template.SetLayout(layout);
                AssetDatabase.CreateAsset(template, path);
            }
            AssetDatabase.SaveAssets();

            foreach (var name in RoomLayouts.All.Keys)
            {
                var problems = AssetDatabase.LoadAssetAtPath<RoomTemplate>(TemplatePath(name)).Validate();
                if (problems.Count > 0)
                    Debug.LogWarning($"FloorSetup: la plantilla {name} tiene problemas:\n{string.Join("\n", problems)}");
            }
        }

        static string TemplatePath(string name) => $"{TemplatesFolder}/{name}.asset";

        static RoomTemplate LoadTemplate(string name) => AssetDatabase.LoadAssetAtPath<RoomTemplate>(TemplatePath(name));

        /// <summary>
        /// Configuración del piso 1. Si ya existe, solo se rellenan las referencias vacías: los números
        /// (salas, presupuesto…) se pueden ajustar a mano en el asset.
        /// </summary>
        static void CreateFloorConfig(Sprite square)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorConfigPath);
            bool isNew = config == null;
            if (isNew)
            {
                config = ScriptableObject.CreateInstance<FloorConfig>();
                AssetDatabase.CreateAsset(config, FloorConfigPath);
            }

            if (isNew || config.StartRoom == null || config.CombatRooms.Length == 0)
                config.ConfigureRooms(LoadTemplate(RoomLayouts.Small), LoadTemplate(RoomLayouts.Normal),
                    CombatTemplates.Select(LoadTemplate).ToArray(), SideTemplates.Select(LoadTemplate).ToArray(),
                    config.MainCombatRooms, config.SideRoomCount);

            if (config.Theme == null || config.BossTheme == null || config.ExitSprite == null)
            {
                var soup = AssetDatabase.LoadAssetAtPath<RoomTheme>(SoupThemePath);
                var boss = AssetDatabase.LoadAssetAtPath<RoomTheme>(BossThemePath);
                if (soup == null)
                    Debug.LogWarning("FloorSetup: falta el arte del escenario. Ejecuta Primordia > Art > Importar escenario y vuelve a generar.");
                config.ConfigureArt(config.Theme != null ? config.Theme : soup, config.BossTheme != null ? config.BossTheme : boss,
                    config.ExitSprite != null ? config.ExitSprite : square);
            }

            // Perseguidor: 1 punto. Tirador: 2, desde la segunda sala, como en el piso fijo de antes.
            if (isNew || config.Enemies.Length == 0 || config.Boss == null)
                config.ConfigureEnemies(new[]
                {
                    new FloorConfig.EnemyEntry(EnemySetup.LoadPrefab("Enemy_Chaser"), cost: 1),
                    new FloorConfig.EnemyEntry(EnemySetup.LoadPrefab("Enemy_Shooter"), cost: 2, minDepth: 2)
                }, BossSetup.LoadPrefab(), config.BaseBudget, config.BudgetPerRoom);

            if (config.RewardPool == null)
                config.ConfigureRewards(MutationSetup.LoadPool(), config.RewardAfter);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            BuildFloor(scene, rig, playerPrefab);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Coloca en la escena el jugador, el punto que sigue la cámara y el generador del piso. El piso no
        /// existe en el editor: se monta al empezar cada partida.
        /// </summary>
        internal static void BuildFloor(Scene scene, GameObject rig, GameObject playerPrefab, MutationPool rewardPool = null,
            FloorConfig floorConfig = null)
        {
            var config = floorConfig != null ? floorConfig : AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorConfigPath);
            if (config == null)
            {
                Debug.LogError($"FloorSetup: falta {FloorConfigPath}.");
                return;
            }
            if (rewardPool != null && config.RewardPool == null)
            {
                config.ConfigureRewards(rewardPool, config.RewardAfter);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);

            var cameraTarget = new GameObject("CameraTarget").AddComponent<CameraLookAhead>();
            cameraTarget.Configure(player.GetComponent<PlayerController>());
            var vcam = rig.GetComponentInChildren<CinemachineCamera>();
            vcam.Follow = cameraTarget.transform;

            var generator = new GameObject("FloorGenerator").AddComponent<FloorGenerator>();
            generator.Configure(config, player.transform, cameraTarget, vcam);
        }
    }
}

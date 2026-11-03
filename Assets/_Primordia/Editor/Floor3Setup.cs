using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Piso 3 (pantano carbonífero), paso 1 de feature/floor-3 (estructura): el <see cref="FloorConfig"/> del piso 3 con sus tres
    /// tramos (pantano, bosque humeante, tierra quemada) y una escena de prueba que empieza en él. Mientras no lleguen sus
    /// pasos, el piso 3 sale sin enemigos y con las salas, la tienda y los jefes provisionales del piso 2.
    /// </summary>
    public static class Floor3Setup
    {
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        internal const string Floor3ConfigPath = RoomsFolder + "/Floor_3.asset";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Floor3.unity";
        const string TeleporterPath = "Assets/_Primordia/Prefabs/Rooms/Teleporter_Floor3.prefab";

        // Tipos de sala de combate (paredes incluidas; la vista es de 30×16,9): normales, alargadas (50×18 y 22×30), en L y una
        // grande de unas 2×2 casillas. Con lagos de lodo en unas y sin ellos en otras.
        static readonly FloorConfig.CaveSpec[] CaveSpecs =
        {
            new(new Vector2Int(34, 22), CaveGenerator.CaveShape.Oval, lakes: false, weight: 2f),
            new(new Vector2Int(38, 24), CaveGenerator.CaveShape.Oval, lakes: true, weight: 2f),
            new(new Vector2Int(44, 26), CaveGenerator.CaveShape.Oval, lakes: true, weight: 1.5f),
            new(new Vector2Int(50, 18), CaveGenerator.CaveShape.Oval, lakes: false, weight: 1.5f),
            new(new Vector2Int(22, 30), CaveGenerator.CaveShape.Oval, lakes: false, weight: 1f),
            new(new Vector2Int(40, 26), CaveGenerator.CaveShape.L, lakes: false, weight: 2f),
            new(new Vector2Int(44, 28), CaveGenerator.CaveShape.L, lakes: true, weight: 1.5f),
            new(new Vector2Int(52, 34), CaveGenerator.CaveShape.Oval, lakes: true, weight: 1f),
        };

        [MenuItem("Primordia/Floor 3/Crear piso 3 y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var floor2 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            if (floor1 == null || floor2 == null || floor2.StartRoom == null)
            {
                Debug.LogError("Floor3Setup: faltan Floor_1 o Floor_2; crea antes el piso 2 (Primordia > Rooms > Crear piso 2 y escena de prueba).");
                return;
            }

            var bands = ArtSetup.ImportFloor3Themes();
            if (bands.Any(band => band == null))
            {
                Debug.LogError("Floor3Setup: falta el arte de algún tramo; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            CreateConfig(floor2, bands);
            CreateTestScene(floor1);
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3Setup: piso 3 listo. Empieza en Test_Floor3; Av Pág / Re Pág saltan de tramo.");
        }

        /// <summary>Configuración del piso 3. Si ya existe, se conservan sus números (se pueden ajustar a mano en el asset).</summary>
        static void CreateConfig(FloorConfig floor2, RoomTheme[] bands)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3ConfigPath);
            bool isNew = config == null;
            if (isNew)
            {
                config = ScriptableObject.CreateInstance<FloorConfig>();
                AssetDatabase.CreateAsset(config, Floor3ConfigPath);

                // 12–14 salas de combate y 2–3 laterales (GDD); cuevas generadas como en el piso 2 hasta el generador nuevo.
                config.ConfigureRooms(floor2.StartRoom, floor2.BossRoom, floor2.CombatRooms, floor2.SideRooms,
                    new Vector2Int(12, 14), new Vector2Int(2, 3));
                config.ConfigureCaves(true, floor2.CaveSizes, floor2.SmallCaveSizes);
                config.ConfigureCorridors(floor2.CorridorLength, floor2.CorridorWidth, floor2.Path);
                // Sin enemigos hasta el paso 5: las salas se abren solas y se puede recorrer el piso.
                config.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), floor2.Boss, newBaseBudget: 0f, newBudgetPerRoom: 0f);
                config.ConfigureShopDepth(new Vector2(1f / 3f, 2f / 3f));
            }

            // Salas de combate de varias siluetas y tamaños, con lagos de lodo en parte de ellas, y las salas de frontera mixtas.
            // Se ponen la primera vez; luego se pueden ajustar a mano en el asset.
            if (config.CaveSpecs.Length == 0)
                config.ConfigureCaveSpecs(CaveSpecs, newBlendBandBorders: true);

            // Atajos de maleza quemable: 1–2 por partida.
            if (config.ShortcutCount.y == 0)
                config.ConfigureShortcuts(new Vector2Int(1, 2));

            // Arte de las reacciones (fuego del suelo, llama de los enemigos): lo carga el juego al montar el piso.
            config.SetReactions(ArtSetup.CreateFloor3ReactionKit());

            config.ConfigureFloor(3, config.NextFloor);
            config.ConfigureArt(bands[0], floor2.BossTheme, floor2.ExitSprite);
            config.ConfigureBands(bands);
            config.ConfigureBosses(floor2.Bosses);
            config.ConfigureBiomass(floor2.SmallBiomass, floor2.BigBiomass, floor2.MinimumBiomassPerRoom);
            config.ConfigureShop(floor2.ShopTemplate, floor2.ShopPrefab);
            // Reserva propia del piso 3 (solo mutaciones del piso 3; las Bocas, como mejora): recompensas, tienda y sala de cambio.
            config.ConfigureRewards(Floor3MutationsSetup.EnsurePool(), isNew ? floor2.RewardAfter : config.RewardAfter);
            config.ConfigureTransition(null, null, null, null);
            config.ConfigureFixedRooms(floor2.ExchangeRoom, floor2.AntechamberVertical, floor2.AntechamberHorizontal);
            config.ConfigurePieces(floor2.Pieces);
            config.ConfigureClams(floor2.Clams);
            config.SetExchangeCapsule(floor2.ExchangeCapsule);
            // Sin ??: en el editor los objetos de Unity destruidos o ausentes no son null para ??.
            var teleporter = AssetDatabase.LoadAssetAtPath<Teleporter>(TeleporterPath);
            config.SetTeleporter(teleporter != null ? teleporter : floor2.TeleporterPrefab);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Escena que empieza en el piso 3, con el jugador del piso 1 y una Boca y una mutación por ranura a nivel II.</summary>
        static void CreateTestScene(FloorConfig floor1)
        {
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Al abrir la escena Unity descarga los assets sin usar: se vuelve a cargar la configuración.
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3ConfigPath);
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: config);
            Floor2Setup.AddTestLoadout(scene, floor1);
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

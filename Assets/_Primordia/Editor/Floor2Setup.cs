using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Piso 2 (océano cámbrico), primera entrega de feature/floor-2-structure: el <see cref="FloorConfig"/> del piso 2
    /// (10–12 salas de combate, presupuesto 4 + 1,4, recompensas tras las salas 2, 4, 6, 8 y 10, tienda en el tercio del
    /// medio), sus tres franjas (Someras, Arrecife, Abismo), una reserva de mutaciones que no ofrece Bocas nuevas, el
    /// enlace desde el piso 1 (la fosa lleva al piso 2) y una escena de prueba que empieza en él. Desde la segunda entrega,
    /// cuevas generadas con un catálogo de reserva, túneles irregulares y un camino que tiende a bajar. Mientras no lleguen
    /// sus ramas, el piso 2 usa los enemigos, los jefes y las mutaciones (sin Bocas) del piso 1.
    /// </summary>
    public static class Floor2Setup
    {
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        internal const string Floor2ConfigPath = RoomsFolder + "/Floor_2.asset";
        const string TransitionTestConfigPath = RoomsFolder + "/Floor_TransitionTest.asset";
        const string PoolPath = "Assets/_Primordia/Data/Mutations/MutationPool_Floor2.asset";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Floor2.unity";

        static readonly int[] RewardAfter = { 2, 4, 6, 8, 10 };

        // Tamaños de las cuevas generadas (paredes incluidas). La vista es de 30×16,9: las de combate son algo más grandes
        // que la pantalla, la de inicio y las laterales caben casi enteras.
        static readonly Vector2Int[] CaveSizes = { new(30, 20), new(34, 22), new(38, 24), new(44, 26) };
        static readonly Vector2Int[] SmallCaveSizes = { new(26, 18), new(28, 19), new(30, 20) };

        // Catálogo de reserva: cuevas fijas (de una semilla) que se guardan como plantillas y se pueden retocar a mano.
        static readonly (string name, Vector2Int size, int seed)[] CombatCatalog =
        {
            ("Room_Cave_1", new Vector2Int(34, 22), 101),
            ("Room_Cave_2", new Vector2Int(38, 24), 202),
            ("Room_Cave_3", new Vector2Int(30, 20), 303),
        };
        static readonly (string name, Vector2Int size, int seed)[] SmallCatalog =
        {
            ("Room_CaveSmall_1", new Vector2Int(26, 18), 404),
            ("Room_CaveSmall_2", new Vector2Int(28, 19), 505),
        };

        [MenuItem("Primordia/Rooms/Crear piso 2 y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            if (floor1 == null || floor1.StartRoom == null)
            {
                Debug.LogError($"Floor2Setup: falta {FloorSetup.FloorConfigPath}; crea antes el juego con Primordia > UI.");
                return;
            }

            var bands = ArtSetup.ImportFloor2Themes();
            if (bands.Any(band => band == null))
                Debug.LogWarning("Floor2Setup: falta el arte de alguna franja (Someras, Arrecife o Abismo) en docs/multimedia-temp/environment; "
                    + "esas salas usarán el tileset del Arrecife.");

            var pool = CreatePool(floor1);
            var floor2 = CreateConfig(floor1, bands, pool);

            // La fosa del piso 1 (y la de la escena de prueba de la sala de paso) lleva al piso 2.
            floor1.ConfigureFloor(1, floor2);
            EditorUtility.SetDirty(floor1);
            var transitionTest = AssetDatabase.LoadAssetAtPath<FloorConfig>(TransitionTestConfigPath);
            if (transitionTest != null)
            {
                transitionTest.ConfigureFloor(1, floor2);
                EditorUtility.SetDirty(transitionTest);
            }
            AssetDatabase.SaveAssets();

            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Floor2Setup: piso 2 listo. Pruébalo saltando a la fosa (Game o Test_FloorTransition) o empezando en Test_Floor2.");
        }

        /// <summary>
        /// Reserva del piso 2: hasta feature/floor2-mutations, las mutaciones del piso 1 sin sus Bocas, más las 3 Bocas del
        /// piso 2 (solo se ofrece la mejora de la que se lleva).
        /// </summary>
        static MutationPool CreatePool(FloorConfig floor1)
        {
            var pool = AssetDatabase.LoadAssetAtPath<MutationPool>(PoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<MutationPool>();
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            var floor1Pool = floor1.RewardPool != null ? floor1.RewardPool.Mutations : System.Array.Empty<MutationData>();
            var mutations = floor1Pool.Where(m => m != null && m.Slot != MutationSlot.Mouth).Concat(floor1.NextFloorMouths.Where(m => m != null));
            pool.Configure(mutations, newOffersNewMouths: false);
            EditorUtility.SetDirty(pool);
            return pool;
        }

        /// <summary>Configuración del piso 2. Si ya existe, se conservan sus números (se pueden ajustar a mano en el asset).</summary>
        static FloorConfig CreateConfig(FloorConfig floor1, RoomTheme[] bands, MutationPool pool)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2ConfigPath);
            bool isNew = config == null;
            if (isNew)
            {
                config = ScriptableObject.CreateInstance<FloorConfig>();
                AssetDatabase.CreateAsset(config, Floor2ConfigPath);
            }

            var reef = bands[1] != null ? bands[1] : floor1.Theme;
            var fallbackBands = bands.Select(band => band != null ? band : reef).ToArray();

            if (isNew)
            {
                config.ConfigureRooms(floor1.StartRoom, floor1.BossRoom, floor1.CombatRooms, floor1.SideRooms,
                    new Vector2Int(10, 12), new Vector2Int(2, 3));
                config.ConfigureEnemies(floor1.Enemies, floor1.Boss, newBaseBudget: 4f, newBudgetPerRoom: 1.4f);
                config.ConfigureShopDepth(new Vector2(1f / 3f, 2f / 3f));
            }

            // Cuevas, túneles y camino que baja (entrega 1b): se ponen la primera vez; luego se pueden ajustar a mano.
            if (!config.GeneratesCaves)
            {
                var combat = CreateCatalog(CombatCatalog);
                var small = CreateCatalog(SmallCatalog);
                if (combat.Length > 0 && small.Length > 0)
                    config.ConfigureRooms(small[0], config.BossRoom, combat, small, config.MainCombatRooms, config.SideRoomCount);
                config.ConfigureCaves(true, CaveSizes, SmallCaveSizes);
                config.ConfigureCorridors(new Vector2Int(4, 6), new Vector2Int(3, 4), FloorConfig.PathWeights.Descending);
            }

            config.ConfigureFloor(2, config.NextFloor);
            config.ConfigureArt(reef, floor1.BossTheme, floor1.ExitSprite);
            config.ConfigureBands(fallbackBands);
            config.ConfigureBosses(floor1.Bosses);
            config.ConfigureBiomass(floor1.SmallBiomass, floor1.BigBiomass, floor1.MinimumBiomassPerRoom);
            config.ConfigureShop(floor1.ShopTemplate, floor1.ShopPrefab);
            config.ConfigureRewards(pool, isNew ? RewardAfter : config.RewardAfter);
            // El piso 2 todavía no tiene sala de paso: su jefe deja la salida de siempre, que da la victoria.
            config.ConfigureTransition(null, null, null, null);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        /// <summary>
        /// Plantillas del catálogo de reserva de cuevas, sacadas del generador con una semilla fija. No sobrescribe las que ya
        /// existen (se pueden retocar a mano); avisa si alguna no pasa la comprobación.
        /// </summary>
        static RoomTemplate[] CreateCatalog((string name, Vector2Int size, int seed)[] entries)
        {
            var result = new System.Collections.Generic.List<RoomTemplate>();
            foreach (var (name, size, seed) in entries)
            {
                string path = $"{RoomsFolder}/{name}.asset";
                var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(path);
                if (template == null)
                {
                    var layout = CaveGenerator.Generate(size, new System.Random(seed));
                    if (layout == null)
                    {
                        Debug.LogWarning($"Floor2Setup: no salió ninguna cueva para {name}; prueba con otra semilla.");
                        continue;
                    }
                    template = ScriptableObject.CreateInstance<RoomTemplate>();
                    template.SetLayout(layout);
                    AssetDatabase.CreateAsset(template, path);
                }

                var problems = template.Validate();
                if (problems.Count > 0)
                    Debug.LogWarning($"Floor2Setup: la plantilla {name} tiene problemas:\n{string.Join("\n", problems)}");
                result.Add(template);
            }
            AssetDatabase.SaveAssets();
            return result.ToArray();
        }

        /// <summary>Escena que empieza en el piso 2, con el jugador del piso 1 (sin Boca del océano).</summary>
        static void CreateTestScene()
        {
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Al abrir la escena Unity descarga los assets sin usar: se vuelve a cargar la configuración.
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2ConfigPath);
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: config);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

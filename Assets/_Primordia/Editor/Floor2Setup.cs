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
        /// Reserva del piso 2: solo mutaciones del piso 2 (las 3 Bocas y las que ya tenga de Primordia > Mutations > Crear
        /// mutaciones del piso 2); de las Bocas solo se ofrece la mejora de la que se lleva. Las mejoras de lo que se trae
        /// del piso 1 se ofrecen aunque no estén aquí (<see cref="MutationPool.PickOffer(int, PlayerMutations)"/>).
        /// </summary>
        static MutationPool CreatePool(FloorConfig floor1)
        {
            var pool = AssetDatabase.LoadAssetAtPath<MutationPool>(PoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<MutationPool>();
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            var mutations = pool.Mutations.Where(m => m != null && m.Floor >= 2 && !m.IsSpecial)
                .Concat(floor1.NextFloorMouths.Where(m => m != null))
                .Distinct();
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
                // Los enemigos del piso 2 si ya existen (Primordia > Enemies > Crear enemigos del piso 2); si no, los del 1.
                var floor2Enemies = ArtSetup.Floor2EnemyEntries();
                config.ConfigureEnemies(floor2Enemies.Length > 0 ? floor2Enemies : floor1.Enemies, floor1.Boss,
                    newBaseBudget: 4f, newBudgetPerRoom: 1.4f);
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

            // Ambiente (entrega 1c): nieve marina y luces 2D que se vean también en WebGL.
            var (flakes, snowMaterial) = ArtSetup.ImportMarineSnow();
            config.ConfigureMarineSnow(flakes, snowMaterial);
            AllowBrightLights();

            config.ConfigureFloor(2, config.NextFloor);
            config.ConfigureArt(reef, floor1.BossTheme, floor1.ExitSprite);
            config.ConfigureBands(fallbackBands);
            config.ConfigureBosses(floor1.Bosses);
            config.ConfigureBiomass(floor1.SmallBiomass, floor1.BigBiomass, floor1.MinimumBiomassPerRoom);
            config.ConfigureShop(floor1.ShopTemplate, floor1.ShopPrefab);
            config.ConfigureRewards(pool, isNew ? RewardAfter : config.RewardAfter);
            // El piso 2 todavía no tiene sala de paso: su jefe deja la salida de siempre, que da la victoria.
            config.ConfigureTransition(null, null, null, null);

            // Paso 2: salas especiales y fijas (tienda, sala de cambio, antesala y salas de los jefes) hechas a mano.
            ConfigureHandmadeRooms(config, floor1, fallbackBands);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        const string LayoutsSourceFolder = "docs/multimedia-temp/floor2-rooms/Layouts";
        const string Floor2RoomsFolder = RoomsFolder + "/Floor2";

        // Las 10 salas especiales (GDD, "Salas especiales"): dónde salen, si cuentan como combate, cuántos enemigos llevan
        // respecto a una sala normal (en las de oleadas, cada oleada), si salen siempre, en qué franja, si ocupan dos casillas,
        // cuántas oleadas llegan por sus entradas, si ofrecen mutación al final y si son candidatas a la sala de la almeja
        // (llevan almeja grande y cada partida sale una de ellas aparte del sorteo).
        static readonly (string name, FloorConfig.SpecialPlacement placement, bool combat, float enemies, bool always, int band, bool tall,
            int waves, bool reward, bool clam)[] Specials =
        {
            ("Parkour", FloorConfig.SpecialPlacement.Path, false, 0f, false, -1, true, 0, false, false),
            ("CorazonMorado", FloorConfig.SpecialPlacement.Side, false, 2f, true, 2, false, 0, false, false),
            ("AnilloSima", FloorConfig.SpecialPlacement.Path, true, 1f, false, -1, false, 0, false, true),
            // Desde la otra orilla, en 2 oleadas.
            ("PuenteRoto", FloorConfig.SpecialPlacement.Path, true, 0.7f, false, -1, false, 2, false, false),
            ("BosqueCoral", FloorConfig.SpecialPlacement.Path, true, 1f, false, -1, false, 0, false, false),
            ("FosilGigante", FloorConfig.SpecialPlacement.Path, true, 1f, false, -1, false, 0, false, false),
            ("JardinEsponjas", FloorConfig.SpecialPlacement.Path, true, 1f, false, -1, false, 0, false, true),
            // Tres oleadas seguidas desde las esquinas y una mutación al final.
            ("SalaSellada", FloorConfig.SpecialPlacement.Path, true, 0.6f, false, -1, false, 3, true, false),
            ("Islas", FloorConfig.SpecialPlacement.Path, true, 1f, false, -1, false, 0, false, false),
            ("GrutaEscondida", FloorConfig.SpecialPlacement.Side, false, 0f, false, -1, false, 0, false, true),
        };

        const string MutationPickupPath = "Assets/_Primordia/Prefabs/Mutations/MutationPickup.prefab";

        // Salas de los jefes del piso 2 y su jefe (si ya está creado; si no, pelea en ella un jefe del piso 1, por orden).
        static readonly (string room, string prefab)[] BossRooms =
        {
            ("Room_Boss_Anomalocaris", ArtSetup.AnomalocarisPrefabPath),
            ("Room_Boss_Eurypterid", ArtSetup.EurypteridPrefabPath),
        };

        /// <summary>
        /// Planos hechos a mano del piso 2 (docs/multimedia-temp/floor2-rooms/Layouts, de generar_salas_especiales.py y
        /// generar_salas_fijas.py) como plantillas en Data/Rooms/Floor2, y su configuración: las 10 salas especiales (de 2 a 4
        /// por partida), la tienda con la sala de cambio detrás, la antesala de los jefes, sus salas (en el Abismo) y las piezas.
        /// No sobrescribe las plantillas que ya existen (se pueden retocar a mano).
        /// </summary>
        static void ConfigureHandmadeRooms(FloorConfig config, FloorConfig floor1, RoomTheme[] bands)
        {
            var templates = ImportLayouts();
            RoomTemplate Named(string name) => templates.TryGetValue(name, out var template) ? template : null;

            var specials = Specials
                .Select(s => new FloorConfig.SpecialRoom
                {
                    template = Named($"Room_Special_{s.name}"),
                    placement = s.placement,
                    countsAsCombat = s.combat,
                    enemyMultiplier = s.enemies,
                    always = s.always,
                    band = s.band,
                    tall = s.tall,
                    waves = s.waves,
                    rewardAtEnd = s.reward,
                    clamRoom = s.clam,
                })
                .Where(s => s.IsValid).ToArray();
            config.ConfigureSpecialRooms(specials, new Vector2Int(2, 4));

            if (Named("Room_Shop_Floor2") != null)
                config.ConfigureShop(Named("Room_Shop_Floor2"), floor1.ShopPrefab);
            config.ConfigureFixedRooms(Named("Room_Exchange"), Named("Room_Antechamber_Vertical"), Named("Room_Antechamber_Horizontal"));

            var abyss = bands.Length > 2 ? bands[2] : null;
            var floor1Bosses = floor1.BossOptions();
            var bosses = BossRooms.Select((slot, i) =>
            {
                var provisional = floor1Bosses[i % floor1Bosses.Length];
                var own = slot.prefab != null ? AssetDatabase.LoadAssetAtPath<Enemies.Enemy>(slot.prefab) : null;
                return own != null
                    ? new FloorConfig.BossEntry(own, Named(slot.room) ?? provisional.room, abyss ?? provisional.theme, Vector2.zero)
                    : new FloorConfig.BossEntry(provisional.prefab, Named(slot.room) ?? provisional.room, abyss ?? provisional.theme, provisional.offset);
            }).ToArray();
            config.ConfigureBosses(bosses);

            config.ConfigurePieces(ArtSetup.ImportRoomPieces());

            // Almejas: biomasa y, la grande, una mutación especial (Primordia > Mutations > Crear mutaciones especiales).
            var clams = config.Clams;
            if (clams.smallBiomass == Vector2Int.zero)
                clams = FloorConfig.ClamContents.Default;
            if (clams.bigMutations == null || clams.bigMutations.Length == 0)
                clams.bigMutations = SpecialMutationsSetup.LoadAll();
            clams.mutationPickup = AssetDatabase.LoadAssetAtPath<MutationPickup>(MutationPickupPath);
            config.ConfigureClams(clams);
        }

        static System.Collections.Generic.Dictionary<string, RoomTemplate> ImportLayouts()
        {
            var result = new System.Collections.Generic.Dictionary<string, RoomTemplate>();
            if (!System.IO.Directory.Exists(LayoutsSourceFolder))
            {
                Debug.LogWarning($"Floor2Setup: no encuentro los planos en {LayoutsSourceFolder}; el piso 2 sale sin salas especiales.");
                return result;
            }

            System.IO.Directory.CreateDirectory(Floor2RoomsFolder);
            foreach (var file in System.IO.Directory.GetFiles(LayoutsSourceFolder, "Room_*.txt"))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                string path = $"{Floor2RoomsFolder}/{name}.asset";
                var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(path);
                if (template == null)
                {
                    template = ScriptableObject.CreateInstance<RoomTemplate>();
                    template.SetLayout(System.IO.File.ReadAllText(file));
                    AssetDatabase.CreateAsset(template, path);
                }

                var problems = template.Validate();
                if (problems.Count > 0)
                    Debug.LogWarning($"Floor2Setup: la plantilla {name} tiene problemas:\n{string.Join("\n", problems)}");
                result[name] = template;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        const float HdrEmulationScale = 2f;

        /// <summary>
        /// Las luces de la bioluminiscencia suman sobre la luz global (intensidad 1). Donde la textura de luces no admite
        /// valores mayores que 1 (WebGL sin texturas de coma flotante) se recortarían; con la emulación de HDR a 2 se ven igual.
        /// </summary>
        static void AllowBrightLights()
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath(Renderer2DPath);
            if (renderer == null)
            {
                Debug.LogWarning($"Floor2Setup: no encuentro {Renderer2DPath}; las luces del Abismo pueden verse más apagadas.");
                return;
            }
            var serialized = new SerializedObject(renderer);
            var scale = serialized.FindProperty("m_HDREmulationScale");
            if (scale != null && scale.floatValue < HdrEmulationScale)
            {
                scale.floatValue = HdrEmulationScale;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(renderer);
            }
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
            AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Pone el HUD (corazones, ranuras, pausa…) en Test_Floor2 si no lo tiene: las escenas hechas antes del 25 de octubre
        /// no lo llevaban. Abre la escena (pidiendo antes guardar la actual) y la guarda.
        /// </summary>
        internal static void EnsureTestSceneHUD()
        {
            if (!System.IO.File.Exists(TestScenePath) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(TestScenePath);
            if (AddHUD(scene))
                EditorSceneManager.SaveScene(scene);
        }

        /// <summary>El prefab GameUI y su EventSystem, si la escena aún no tiene HUD. True si lo ha añadido.</summary>
        internal static bool AddHUD(UnityEngine.SceneManagement.Scene scene)
        {
            bool hasHUD = scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<UI.HeartsDisplay>(true) != null);
            if (hasHUD)
                return false;
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UISetup.GameUIPath), scene);
            if (!scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null))
                UISetup.CreateEventSystem();
            return true;
        }

        /// <summary>
        /// El jugador de la escena de prueba empieza como si llegara del piso 1: con una Boca del piso 2 y una mutación del
        /// piso 1 en cada otra ranura, todas a nivel II.
        /// </summary>
        internal static void AddTestLoadout(UnityEngine.SceneManagement.Scene scene, FloorConfig floor1)
        {
            var player = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<PlayerMutations>()).FirstOrDefault(p => p != null);
            if (player == null || floor1 == null)
                return;

            var mouth = floor1.NextFloorMouths.FirstOrDefault(m => m != null);
            var others = (floor1.RewardPool != null ? floor1.RewardPool.Mutations : System.Array.Empty<MutationData>())
                .Where(m => m != null && m.Slot != MutationSlot.Mouth)
                .GroupBy(m => m.Slot).Select(group => group.First());
            var loadout = new[] { mouth }.Where(m => m != null).Concat(others).ToArray();

            // Sin ??: en el editor GetComponent devuelve un null de Unity que ?? no ve como null.
            var component = player.GetComponent<TestLoadout>();
            if (component == null)
                component = player.gameObject.AddComponent<TestLoadout>();
            var serialized = new SerializedObject(component);
            var list = serialized.FindProperty("mutations");
            list.arraySize = loadout.Length;
            for (int i = 0; i < loadout.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = loadout[i];
            serialized.FindProperty("level").intValue = 2;
            serialized.ApplyModifiedProperties();
            Debug.Log($"Floor2Setup: Test_Floor2 empieza con {string.Join(", ", loadout.Select(m => m.name))} a nivel II.");
        }
    }
}

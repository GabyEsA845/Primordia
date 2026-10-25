using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Ameba gigante, segundo jefe del piso 1: proyectiles, ataques, datos, prefab (con su arte, si está en
    /// docs/multimedia-temp), la sala circular con su tema, y la añade a Floor_1 junto al virus (sale uno de los dos al
    /// azar). También crea la escena Test_BossAmoeba: la sala de inicio con 3 burbujas de cada mutación y, al lado, la
    /// sala de la ameba. Se puede volver a ejecutar: actualiza el arte y rehace la escena; lo demás no lo sobrescribe.
    /// </summary>
    public static class AmoebaBossSetup
    {
        const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Amoeba.prefab";
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossAmoeba.unity";
        const string TestConfigPath = RoomsFolder + "/Floor_BossAmoebaTest.asset";
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations";
        const string PickupPath = "Assets/_Primordia/Prefabs/Mutations/MutationPickup.prefab";

        // Radios de colisión: el cuerpo grande mide unas 4,3 unidades (68 px); las medianas, un 60 %.
        const float BodyRadius = 2.1f;
        const float HalfRadius = 1.25f;
        const int SortingOrder = 6;

        [MenuItem("Primordia/Boss/Crear ameba gigante y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            UISetup.CreateUIAssets();

            var boss = CreateAssets(square);
            if (boss == null)
                return;

            var circle = CreateCircleRoom();
            var theme = ArtSetup.ImportAmoebaTheme();
            if (theme == null)
                Debug.LogWarning("AmoebaBossSetup: falta el tema Amoeba (docs/multimedia-temp/environment/Amoeba); la sala usará el del virus.");
            ArtSetup.ApplyBossBarLock();
            AddToFloor(boss, circle, theme);
            AssetDatabase.SaveAssets();

            CreateTestScene(cameraRig, gameManager, player, boss, circle, theme);
        }

        internal static Enemy LoadPrefab() => AssetDatabase.LoadAssetAtPath<Enemy>(PrefabPath);

        /// <summary>Proyectiles, ataques, datos y prefab. Devuelve el prefab.</summary>
        static Enemy CreateAssets(Sprite square)
        {
            // Anillos rosa, remolino lila y abanico rojo: se distinguen en mitad de la lluvia.
            var ringProjectile = CombatSetup.CreateProjectilePrefab("Projectile_AmoebaRing", new Color(1f, 0.4f, 0.65f), 0.45f, square);
            var spiralProjectile = CombatSetup.CreateProjectilePrefab("Projectile_AmoebaSpiral", new Color(0.75f, 0.45f, 0.95f), 0.4f, square);
            var fanProjectile = CombatSetup.CreateProjectilePrefab("Projectile_AmoebaFan", new Color(1f, 0.25f, 0.3f), 0.55f, square);

            // La cadencia la marca AmoebaBarrage; aquí solo importan daño, velocidad y alcance (la sala mide 30).
            var ring = CombatSetup.CreateAttack("Attack_AmoebaRing", ringProjectile, fireRate: 1f, damage: 1, speed: 3.6f, lifetime: 11f);
            var spiral = CombatSetup.CreateAttack("Attack_AmoebaSpiral", spiralProjectile, fireRate: 1f, damage: 1, speed: 3.2f, lifetime: 12f);
            var fan = CombatSetup.CreateAttack("Attack_AmoebaFan", fanProjectile, fireRate: 1f, damage: 1, speed: 6.5f, lifetime: 6f);

            var data = EnemySetup.CreateData("Enemy_AmoebaBoss", d =>
            {
                d.Configure(EnemyBehaviour.Chaser, maxHealth: 180, moveSpeed: 1.2f, detectionRange: 0f, contactDamage: 1);
                // Como el virus: una recompensa fija de biomasa al morir.
                d.ConfigureBiomass(0f, 0f, 10);
            });
            AssetDatabase.SaveAssets();

            if (LoadPrefab() == null)
                CreatePrefab(data, ring, spiral, fan, square);

            var art = ArtSetup.ImportBossAmoebaArt();
            if (art == null)
                Debug.LogWarning("AmoebaBossSetup: no hay arte en docs/multimedia-temp/enemies/BossAmoeba; la ameba se verá con cuadrados. "
                    + "Genera el arte con generar_ameba_gigante.py y vuelve a ejecutar el menú.");
            else
                ApplyArt(art);

            AssetDatabase.SaveAssets();
            return LoadPrefab();
        }

        static void CreatePrefab(EnemyData data, AttackData ring, AttackData spiral, AttackData fan, Sprite square)
        {
            int enemyLayer = LayerMask.NameToLayer(CombatLayers.Enemy);
            var root = new GameObject("Boss_Amoeba") { layer = enemyLayer };
            try
            {
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;
                root.AddComponent<Health>();

                var body = CreateBody(root.transform, "Body", BodyRadius, square, new Color(0.96f, 0.75f, 0.82f), 4.3f, active: true);
                var halfA = CreateBody(root.transform, "Half_A", HalfRadius, square, new Color(0.9f, 0.45f, 0.58f), 2.6f, active: false);
                var halfB = CreateBody(root.transform, "Half_B", HalfRadius, square, new Color(0.9f, 0.45f, 0.58f), 2.6f, active: false);

                var weapon = root.AddComponent<Weapon>();
                CombatSetup.ConfigureWeapon(weapon, ring, Team.Enemy, null);

                var enemy = root.AddComponent<Enemy>();
                var serializedEnemy = new SerializedObject(enemy);
                serializedEnemy.FindProperty("data").objectReferenceValue = data;
                serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

                root.AddComponent<AmoebaBarrage>().Configure(ring, spiral, fan);
                root.AddComponent<AmoebaBoss>().Configure(body, new[] { halfA, halfB });

                var pickup = AssetDatabase.LoadAssetAtPath<MutationPickup>(PickupPath);
                if (pickup != null)
                    root.AddComponent<MutationDrop>().Configure(MutationSetup.LoadPool(), pickup);
                else
                    Debug.LogWarning("AmoebaBossSetup: falta MutationPickup; la ameba no soltará mutaciones (Primordia > Art > Importar objetos e interfaz).");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Un cuerpo: física y un hijo Visual con el dibujo (que es lo que tiembla). El daño por contacto lo hace AmoebaBody.</summary>
        static AmoebaBody CreateBody(Transform parent, string name, float radius, Sprite square, Color color, float size, bool active)
        {
            var body = new GameObject(name) { layer = parent.gameObject.layer };
            body.transform.SetParent(parent, false);

            body.AddComponent<Rigidbody2D>().gravityScale = 0f;
            body.AddComponent<CircleCollider2D>().radius = radius;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(body.transform, false);
            // Provisional hasta importar el arte: un cuadrado del tamaño del cuerpo.
            visual.transform.localScale = new Vector3(size, size, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = SortingOrder;
            var animator = visual.AddComponent<Animator>();

            var amoebaBody = body.AddComponent<AmoebaBody>();
            amoebaBody.Configure(animator, visual.transform, LayerMask.GetMask("Default"));
            body.SetActive(active);
            return amoebaBody;
        }

        internal static void ApplyArt(ArtSetup.AmoebaArt art)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var boss = root.GetComponent<AmoebaBoss>();
                SetVisual(boss.MainBody, art.Big, art.BigSprite);
                foreach (var half in boss.Halves)
                    SetVisual(half, art.Half, art.HalfSprite);

                // Las primeras versiones dañaban con ContactDamage (por choque); ahora el cuerpo no choca con el jugador.
                foreach (var contact in root.GetComponentsInChildren<ContactDamage>(true))
                    Object.DestroyImmediate(contact);

                // La ameba se ve morir antes de desaparecer.
                var serializedEnemy = new SerializedObject(root.GetComponent<Enemy>());
                serializedEnemy.FindProperty("deathDuration").floatValue = art.DeathLength;
                serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SetVisual(AmoebaBody body, RuntimeAnimatorController controller, Sprite sprite)
        {
            if (body == null || controller == null)
                return;

            var animator = body.Animator;
            animator.runtimeAnimatorController = controller;
            var visual = animator.transform;
            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            var renderer = animator.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            EditorUtility.SetDirty(body);
        }

        /// <summary>La plantilla de la sala circular, si todavía no existe (se puede retocar a mano).</summary>
        static RoomTemplate CreateCircleRoom()
        {
            string path = $"{RoomsFolder}/{RoomLayouts.BossCircle}.asset";
            var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(path);
            if (template != null)
                return template;

            template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.SetLayout(RoomLayouts.All[RoomLayouts.BossCircle]);
            AssetDatabase.CreateAsset(template, path);
            var problems = template.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"AmoebaBossSetup: la sala circular tiene problemas:\n{string.Join("\n", problems)}");
            return template;
        }

        /// <summary>Pone la ameba en Floor_1 junto al virus: en cada partida sale uno de los dos, al 50 %.</summary>
        static void AddToFloor(Enemy boss, RoomTemplate circle, RoomTheme theme)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            if (config == null)
            {
                Debug.LogWarning($"AmoebaBossSetup: falta {FloorSetup.FloorConfigPath}; crea antes el juego con Primordia > UI.");
                return;
            }

            var amoeba = new FloorConfig.BossEntry(boss, circle, theme, Vector2.zero);
            var others = config.BossOptions().Where(entry => entry.prefab != boss);
            config.ConfigureBosses(others.Append(amoeba).ToArray());
            EditorUtility.SetDirty(config);
            Debug.Log($"AmoebaBossSetup: jefes de {config.name}: {string.Join(", ", config.Bosses.Select(entry => entry.prefab.name))}.");
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject player, Enemy boss, RoomTemplate circle,
            RoomTheme theme)
        {
            var config = CreateTestConfig(boss, circle, theme);
            if (config == null)
                return;

            // Otra escena abierta antes de borrar, por si era la abierta.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se vuelve a cargar: al abrir la escena nueva Unity descarga los assets sin usar, y la copia de antes ya no vale.
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UISetup.GameUIPath), scene);
            UISetup.CreateEventSystem();

            var mutations = AssetDatabase.FindAssets("t:MutationData", new[] { MutationsFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(m => m != null)
                .ToArray();
            var bench = new GameObject("MutationBench").AddComponent<MutationBench>();
            bench.Configure(AssetDatabase.LoadAssetAtPath<MutationPickup>(PickupPath), mutations, newCopies: 3);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"AmoebaBossSetup: {TestScenePath} creada: inicio con {mutations.Length} mutaciones (3 de cada) y la sala de la ameba.");
        }

        /// <summary>Copia de Floor_1 con el camino inicio → ameba, sin combates, laterales, tienda ni recompensas.</summary>
        static FloorConfig CreateTestConfig(Enemy boss, RoomTemplate circle, RoomTheme theme)
        {
            var floor = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var large = AssetDatabase.LoadAssetAtPath<RoomTemplate>($"{RoomsFolder}/{RoomLayouts.Large}.asset");
            if (floor == null || large == null)
            {
                Debug.LogError($"AmoebaBossSetup: faltan {FloorSetup.FloorConfigPath} o la plantilla {RoomLayouts.Large}; crea antes el juego con Primordia > UI.");
                return null;
            }

            AssetDatabase.DeleteAsset(TestConfigPath);
            var config = Object.Instantiate(floor);
            AssetDatabase.CreateAsset(config, TestConfigPath);

            config.ConfigureRooms(large, circle, new[] { large }, System.Array.Empty<RoomTemplate>(), Vector2Int.zero, Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureRewards(floor.RewardPool, System.Array.Empty<int>());
            config.ConfigureBosses(new[] { new FloorConfig.BossEntry(boss, circle, theme, Vector2.zero) });

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}

using System.IO;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Configura las capas de física del combate y crea proyectiles, ataques, el arma del jugador
    /// y la escena de prueba del módulo Combat. No sobrescribe nada que ya exista.
    /// </summary>
    public static class CombatSetup
    {
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Combat";
        const string AttacksFolder = "Assets/_Primordia/Data/Attacks";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Combat.unity";

        static readonly Color PlayerProjectileColor = new(1f, 0.9f, 0.35f);
        static readonly Color EnemyProjectileColor = new(1f, 0.3f, 0.45f);
        static readonly Color TargetColor = new(0.85f, 0.55f, 0.3f);
        static readonly Color TurretColor = new(0.8f, 0.25f, 0.3f);

        [MenuItem("Primordia/Combat/Crear prefabs y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);

            CreateAssets(square);
            CreateTestScene(cameraRig, gameManager, player, square);

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Capas, proyectiles, ataques y arma del jugador. Los guarda en disco antes de volver,
        /// así se pueden abrir escenas después sin perder las referencias.
        /// </summary>
        internal static void CreateAssets(Sprite square)
        {
            ConfigureLayers();
            PlayerSetup.CreatePlayerPrefab(square);

            Directory.CreateDirectory(PrefabsFolder);
            Directory.CreateDirectory(AttacksFolder);

            var playerProjectile = CreateProjectilePrefab("Projectile_Player", PlayerProjectileColor, 0.375f, square);
            var enemyProjectile = CreateProjectilePrefab("Projectile_Enemy", EnemyProjectileColor, 0.5f, square);

            var playerBasic = CreateAttack("Attack_PlayerBasic", playerProjectile, fireRate: 5f, damage: 1, speed: 14f, lifetime: 2f);
            CreateAttack("Attack_EnemyBasic", enemyProjectile, fireRate: 0.8f, damage: 1, speed: 6f, lifetime: 4f);
            CreateAttack("Attack_EnemyFan", enemyProjectile, fireRate: 0.6f, damage: 1, speed: 5f, lifetime: 4f, count: 3, spread: 40f);

            // Guardar antes de abrir la escena: si no, Unity descarta los assets recién creados
            // y las referencias que se les asignen después quedan vacías.
            AssetDatabase.SaveAssets();

            AddCombatToPlayer(playerBasic);
            AssetDatabase.SaveAssets();
        }

        internal static AttackData LoadAttack(string assetName) =>
            AssetDatabase.LoadAssetAtPath<AttackData>($"{AttacksFolder}/{assetName}.asset");

        [MenuItem("Primordia/Combat/Configurar capas de física")]
        internal static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            EnsureLayer(layers, CombatLayers.Player);
            EnsureLayer(layers, CombatLayers.Enemy);
            EnsureLayer(layers, CombatLayers.PlayerProjectile);
            EnsureLayer(layers, CombatLayers.EnemyProjectile);
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            int player = LayerMask.NameToLayer(CombatLayers.Player);
            int enemy = LayerMask.NameToLayer(CombatLayers.Enemy);
            int playerProjectile = LayerMask.NameToLayer(CombatLayers.PlayerProjectile);
            int enemyProjectile = LayerMask.NameToLayer(CombatLayers.EnemyProjectile);

            // Cada equipo atraviesa sus propios proyectiles, y los proyectiles no chocan entre sí.
            var physics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
            var matrix = physics.FindProperty("m_LayerCollisionMatrix");
            SetCollision(matrix, player, playerProjectile, false);
            SetCollision(matrix, enemy, enemyProjectile, false);
            SetCollision(matrix, playerProjectile, playerProjectile, false);
            SetCollision(matrix, enemyProjectile, enemyProjectile, false);
            SetCollision(matrix, playerProjectile, enemyProjectile, false);
            physics.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Debug.Log("Capas de combate configuradas: Player, Enemy, PlayerProjectile, EnemyProjectile.");
        }

        static void EnsureLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    return;
            }

            // Las capas 0 a 5 son de Unity.
            for (int i = 6; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    return;
                }
            }

            Debug.LogError($"No hay capas libres para '{layerName}'.");
        }

        static void SetCollision(SerializedProperty matrix, int a, int b, bool collide)
        {
            SetBit(matrix.GetArrayElementAtIndex(a), b, collide);
            SetBit(matrix.GetArrayElementAtIndex(b), a, collide);
        }

        static void SetBit(SerializedProperty row, int bit, bool value)
        {
            uint mask = (uint)row.longValue;
            mask = value ? mask | (1u << bit) : mask & ~(1u << bit);
            row.longValue = mask;
        }

        internal static Projectile CreateProjectilePrefab(string prefabName, Color color, float size, Sprite square) =>
            CreateProjectilePrefab(prefabName, color, new Vector2(size, size), square);

        internal static Projectile CreateProjectilePrefab(string prefabName, Color color, Vector2 size, Sprite square)
        {
            string path = $"{PrefabsFolder}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Projectile>(path);
            if (existing != null)
                return existing;

            var root = new GameObject(prefabName);
            root.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = 20;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            root.AddComponent<Rigidbody2D>().gravityScale = 0f;
            root.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Projectile>();
        }

        internal static AttackData CreateAttack(string assetName, Projectile projectile, float fireRate, int damage, float speed,
            float lifetime, int count = 1, float spread = 0f, bool automatic = false, int poisonDamagePerSecond = 0, float poisonDuration = 0f)
        {
            string path = $"{AttacksFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<AttackData>();
            data.Configure(projectile, fireRate, damage, speed, lifetime, count, spread, automatic, poisonDamagePerSecond, poisonDuration);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        /// <summary>Añade al prefab del jugador el arma, el ataque, la capa y el destello de daño.</summary>
        static void AddCombatToPlayer(AttackData attack)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                root.tag = "Player";
                root.layer = LayerMask.NameToLayer(CombatLayers.Player);

                if (!root.TryGetComponent<Weapon>(out var weapon))
                {
                    weapon = root.AddComponent<Weapon>();
                    ConfigureWeapon(weapon, attack, Team.Player, root.transform.Find("AimPivot/Mouth"));
                }

                if (!root.TryGetComponent<PlayerAttack>(out _))
                    root.AddComponent<PlayerAttack>();
                if (!root.TryGetComponent<DamageFlash>(out _))
                    root.AddComponent<DamageFlash>();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void ConfigureWeapon(Weapon weapon, AttackData attack, Team team, Transform muzzle)
        {
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("attack").objectReferenceValue = attack;
            serialized.FindProperty("team").enumValueIndex = (int)team;
            serialized.FindProperty("muzzle").objectReferenceValue = muzzle;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab, Sprite square)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se carga desde disco después de abrir la escena, para que la referencia sea válida.
            var turretAttack = LoadAttack("Attack_EnemyFan");

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            rig.GetComponentInChildren<CinemachineCamera>().Follow = player.transform;

            PlayerSetup.CreateTestRoom(square);

            // Dianas: reciben daño, destellan y desaparecen al llegar a 0.
            var targets = new GameObject("Targets").transform;
            foreach (float y in new[] { 4f, 0f, -4f })
                CreateTarget(targets, "Target", new Vector2(14f, y), square, TargetColor, maxHealth: 5);

            // Torreta: dispara un abanico de 3 proyectiles al jugador cuando se acerca.
            var turret = CreateTarget(targets, "Turret", new Vector2(-14f, -7f), square, TurretColor, maxHealth: 10);
            ConfigureWeapon(turret.AddComponent<Weapon>(), turretAttack, Team.Enemy, null);
            turret.AddComponent<Turret>().Target = player.transform;

            EditorSceneManager.SaveScene(scene);
        }

        static GameObject CreateTarget(Transform parent, string targetName, Vector2 position, Sprite square, Color color,
            int maxHealth)
        {
            var target = new GameObject(targetName) { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            target.transform.SetParent(parent, false);
            target.transform.position = position;

            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = 5;
            target.AddComponent<BoxCollider2D>();

            var health = target.AddComponent<Health>();
            var serialized = new SerializedObject(health);
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            target.AddComponent<DamageFlash>();
            target.AddComponent<DisableOnDeath>();
            return target;
        }
    }
}

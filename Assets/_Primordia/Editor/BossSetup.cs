using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Crea el jefe del piso 1 (virus gigante): proyectiles, ataques, datos y prefab,
    /// y la escena de prueba Test_Boss. No sobrescribe nada que ya exista.
    /// </summary>
    public static class BossSetup
    {
        const string BossPrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Virus.prefab";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Boss.unity";

        static readonly Color BodyColor = new(0.75f, 0.25f, 0.5f);
        static readonly Color SpikeColor = new(0.95f, 0.55f, 0.75f);

        [MenuItem("Primordia/Boss/Crear jefe y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            FloorSetup.CreateAssets(square);
            MutationSetup.CreateAssets(square);
            UISetup.CreateUIAssets();

            CreateTestScene(cameraRig, gameManager, player);
            AssetDatabase.SaveAssets();
        }

        internal static Enemy LoadPrefab() => AssetDatabase.LoadAssetAtPath<Enemy>(BossPrefabPath);

        /// <summary>Proyectiles, ataques, datos y prefab del jefe. Necesita los enemigos ya creados (invoca perseguidores).</summary>
        internal static void CreateAssets(Sprite square)
        {
            var spiralProjectile = CombatSetup.CreateProjectilePrefab("Projectile_BossSpiral", new Color(1f, 0.35f, 0.6f), 0.5f, square);
            var burstProjectile = CombatSetup.CreateProjectilePrefab("Projectile_BossBurst", new Color(1f, 0.6f, 0.2f), 0.75f, square);

            // La cadencia la marca BossAI; aquí solo importan daño, velocidad y alcance.
            var spiralAttack = CombatSetup.CreateAttack("Attack_BossSpiral", spiralProjectile, fireRate: 1f, damage: 1, speed: 5f, lifetime: 7f);
            var burstAttack = CombatSetup.CreateAttack("Attack_BossBurst", burstProjectile, fireRate: 1f, damage: 1, speed: 4f, lifetime: 8f);

            var data = EnemySetup.CreateData("Enemy_VirusBoss", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 120, moveSpeed: 1.5f, detectionRange: 0f, contactDamage: 1));

            AssetDatabase.SaveAssets();
            CreateBossPrefab(square, data, spiralAttack, burstAttack, EnemySetup.LoadPrefab("Enemy_Chaser"));
            AssetDatabase.SaveAssets();
        }

        static void CreateBossPrefab(Sprite square, EnemyData data, AttackData spiral, AttackData burst, Enemy minion)
        {
            if (LoadPrefab() != null)
                return;

            // 3 × 3 unidades (48 px): cuerpo y 8 púas provisionales.
            var root = new GameObject("Boss_Virus") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            root.transform.localScale = new Vector3(3f, 3f, 1f);

            AddSprite(root.transform, "Body", Vector2.zero, Vector2.one, BodyColor, 6);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                var position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.6f;
                AddSprite(root.transform, "Spike", position, Vector2.one * 0.2f, SpikeColor, 7);
            }

            root.AddComponent<Rigidbody2D>().gravityScale = 0f;
            root.AddComponent<CircleCollider2D>().radius = 0.45f;
            root.AddComponent<Health>();
            root.AddComponent<DamageFlash>();

            var contact = root.AddComponent<ContactDamage>();
            var serializedContact = new SerializedObject(contact);
            serializedContact.FindProperty("targets").intValue = LayerMask.GetMask(CombatLayers.Player);
            serializedContact.ApplyModifiedPropertiesWithoutUndo();

            CombatSetup.ConfigureWeapon(root.AddComponent<Weapon>(), spiral, Team.Enemy, null);

            var enemy = root.AddComponent<Enemy>();
            var serializedEnemy = new SerializedObject(enemy);
            serializedEnemy.FindProperty("data").objectReferenceValue = data;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

            var boss = root.AddComponent<BossAI>();
            var serializedBoss = new SerializedObject(boss);
            serializedBoss.FindProperty("spiralAttack").objectReferenceValue = spiral;
            serializedBoss.FindProperty("burstAttack").objectReferenceValue = burst;
            serializedBoss.FindProperty("minionPrefab").objectReferenceValue = minion;
            serializedBoss.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            Object.DestroyImmediate(root);
        }

        static void AddSprite(Transform parent, string name, Vector2 position, Vector2 scale, Color color, int order)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = CoreSetup.CreateSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        /// <summary>Hace que el jefe orbite alrededor de ese punto (el centro de su sala).</summary>
        internal static void SetArenaCenter(BossAI boss, Transform center)
        {
            // Con SerializedObject para que quede guardado como cambio de la instancia del prefab.
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("arenaCenter").objectReferenceValue = center;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se cargan desde disco después de abrir la escena, para que las referencias sean válidas.
            var square = CoreSetup.CreateSquareSprite();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(MutationSetup.CircleSpritePath);
            var pool = MutationSetup.LoadPool();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0f, -8f, 0f);
            rig.GetComponentInChildren<CinemachineCamera>().Follow = player.transform;

            PlayerSetup.CreateTestRoom(square);

            var center = new GameObject("ArenaCenter").transform;
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab().gameObject, scene);
            boss.transform.position = new Vector3(0f, 4f, 0f);
            SetArenaCenter(boss.GetComponent<BossAI>(), center);

            // Mutaciones abajo para probar el combate con distintas combinaciones.
            var pickups = new GameObject("Pickups").transform;
            for (int i = 0; i < pool.Mutations.Count; i++)
                MutationSetup.CreatePickup(pickups, pool.Mutations[i], new Vector2(-16f + i * 4f, -10f), circle);

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/UI/GameUI.prefab"), scene);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene);
        }
    }
}

using System.IO;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Crea los enemigos genéricos (datos y prefabs) y la escena de prueba del módulo Enemies.
    /// No sobrescribe nada que ya exista.
    /// </summary>
    public static class EnemySetup
    {
        internal const string DataFolder = "Assets/_Primordia/Data/Enemies";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Enemies";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Enemies.unity";

        static readonly Color ChaserColor = new(0.9f, 0.45f, 0.3f);
        static readonly Color SmallColor = new(0.95f, 0.65f, 0.4f);
        static readonly Color ShooterColor = new(0.65f, 0.4f, 0.85f);

        [MenuItem("Primordia/Enemies/Crear enemigos y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            CombatSetup.CreateAssets(square);

            CreateAssets(square);
            CreateTestScene(cameraRig, gameManager, player);

            AssetDatabase.SaveAssets();
        }

        internal static void CreateAssets(Sprite square)
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(PrefabsFolder);

            // Perseguidor genérico (bacteria): poca vida, rápido, daña al tocar.
            var chaserData = CreateData("Enemy_Chaser", data => data.Configure(EnemyBehaviour.Chaser,
                maxHealth: 6, moveSpeed: 3.5f, detectionRange: 9f, contactDamage: 1));
            CreatePrefab("Enemy_Chaser", chaserData, square, ChaserColor, scale: 1f, withWeapon: false);

            // Versión pequeña que aparece al dividirse un tirador.
            var smallData = CreateData("Enemy_Small", data => data.Configure(EnemyBehaviour.Chaser,
                maxHealth: 2, moveSpeed: 4.5f, detectionRange: 12f, contactDamage: 1));
            var smallPrefab = CreatePrefab("Enemy_Small", smallData, square, SmallColor, scale: 0.625f, withWeapon: false);

            // Tirador genérico (ameba): mantiene la distancia, dispara y se divide en 2 al morir.
            var shooterData = CreateData("Enemy_Shooter", data => data.Configure(EnemyBehaviour.Shooter,
                maxHealth: 8, moveSpeed: 2f, detectionRange: 10f, contactDamage: 1, preferredDistance: 6f,
                attack: CombatSetup.LoadAttack("Attack_EnemyBasic"), splitPrefab: smallPrefab, splitCount: 2));
            CreatePrefab("Enemy_Shooter", shooterData, square, ShooterColor, scale: 1f, withWeapon: true);

            AssetDatabase.SaveAssets();
        }

        internal static Enemy LoadPrefab(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<Enemy>($"{PrefabsFolder}/{prefabName}.prefab");

        internal static EnemyData CreateData(string assetName, System.Action<EnemyData> configure)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<EnemyData>();
            configure(data);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static Enemy CreatePrefab(string prefabName, EnemyData data, Sprite square, Color color, float scale, bool withWeapon)
        {
            string path = $"{PrefabsFolder}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Enemy>(path);
            if (existing != null)
                return existing;

            var root = new GameObject(prefabName) { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            root.transform.localScale = new Vector3(scale, scale, 1f);

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = 5;

            root.AddComponent<Rigidbody2D>().gravityScale = 0f;
            root.AddComponent<CircleCollider2D>().radius = 0.45f;
            root.AddComponent<Health>();
            root.AddComponent<DamageFlash>();

            var contact = root.AddComponent<ContactDamage>();
            var serializedContact = new SerializedObject(contact);
            serializedContact.FindProperty("targets").intValue = LayerMask.GetMask(CombatLayers.Player);
            serializedContact.ApplyModifiedPropertiesWithoutUndo();

            if (withWeapon)
                CombatSetup.ConfigureWeapon(root.AddComponent<Weapon>(), data.Attack, Team.Enemy, null);

            var enemy = root.AddComponent<Enemy>();
            var serializedEnemy = new SerializedObject(enemy);
            serializedEnemy.FindProperty("data").objectReferenceValue = data;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<EnemyAI>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Enemy>();
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se cargan desde disco después de abrir la escena, para que las referencias sean válidas.
            var square = CoreSetup.CreateSquareSprite();
            var chaser = LoadPrefab("Enemy_Chaser");
            var shooter = LoadPrefab("Enemy_Shooter");

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            rig.GetComponentInChildren<CinemachineCamera>().Follow = player.transform;

            PlayerSetup.CreateTestRoom(square);

            var enemies = new GameObject("Enemies").transform;
            foreach (var position in new[] { new Vector2(12f, 6f), new Vector2(15f, 0f), new Vector2(13f, -7f) })
                PlaceEnemy(chaser, enemies, position);
            foreach (var position in new[] { new Vector2(-14f, 7f), new Vector2(-13f, -6f) })
                PlaceEnemy(shooter, enemies, position);

            EditorSceneManager.SaveScene(scene);
        }

        static void PlaceEnemy(Enemy prefab, Transform parent, Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
            instance.transform.position = position;
        }
    }
}

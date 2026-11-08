using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Meganeura, jefe 2 del piso 3 (paso 7a): importa sus hojas, crea la sala de jefe (Tierra quemada, suelo de 40×24 u), sus datos y su
    /// prefab (sombra + cuerpo elevado + presentación), la pone como jefe de Floor_3 y crea la escena Test_BossMeganeura: la sala de
    /// inicio del piso 3 con el equipo de prueba y, tras la antesala, la sala de la Meganeura. Se puede volver a ejecutar.
    /// </summary>
    public static class MeganeuraBossSetup
    {
        const string RoomName = "Room_Boss_Meganeura";
        const string RoomPath = "Assets/_Primordia/Data/Rooms/Floor3/" + RoomName + ".asset";
        const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Meganeura.prefab";
        const string DataPath = "Assets/_Primordia/Data/Enemies/Floor3/Enemy_MeganeuraBoss.asset";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossMeganeura.unity";
        const string TestConfigPath = "Assets/_Primordia/Data/Rooms/Floor_BossMeganeuraTest.asset";
        const string StumpPath = "Assets/_Primordia/Art/Floor3/floor3-bosses/meganeura/Burnt/Lepido_Split_Idle.png";

        // Suelo de 40×24 u: la plantilla rectangular suma 2 de pared a los lados y 4 arriba y abajo.
        const int FloorWidth = 40, FloorHeight = 24;
        const int BossMaxHealth = 450;

        [MenuItem("Primordia/Boss/Crear Meganeura y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor3 == null)
            {
                Debug.LogError($"MeganeuraBossSetup: falta {Floor3Setup.Floor3ConfigPath}; crea antes el piso 3 (Primordia > Floor 3).");
                return;
            }

            var art = ArtSetup.ImportMeganeuraSheets();
            var template = CreateRoom();
            var boss = CreatePrefab(art);
            if (boss == null)
                return;
            AssetDatabase.SaveAssets();
            ArtSetup.ImportBossIntro("Meganeura");

            // La sala del jefe usa el tileset de la Tierra quemada (la tercera franja del piso).
            var themes = floor3.BandThemes;
            var theme = themes.Length > 2 && themes[2] != null ? themes[2] : floor3.BossTheme;
            var entry = new FloorConfig.BossEntry(boss, template, theme, Vector2.zero);
            floor3.ConfigureBosses(new[] { entry });
            EditorUtility.SetDirty(floor3);
            AssetDatabase.SaveAssets();

            if (CreateTestConfig(floor3, entry) == null)
                return;
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"MeganeuraBossSetup: {TestScenePath} creada: inicio del piso 3 y, tras la antesala, la Meganeura en su sala.");
        }

        /// <summary>Sala de jefe rectangular de 40×24 u de suelo (el Lepidodendron partido lo pone el jefe).</summary>
        static RoomTemplate CreateRoom()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RoomPath));
            var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(RoomPath);
            bool isNew = template == null;
            if (isNew)
                template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.SetLayout(RoomLayouts.Rectangle(FloorWidth + 2, FloorHeight + 4));
            if (isNew)
                AssetDatabase.CreateAsset(template, RoomPath);
            else
                EditorUtility.SetDirty(template);
            var problems = template.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"MeganeuraBossSetup: la plantilla {RoomName} tiene problemas:\n{string.Join("\n", problems)}");
            return template;
        }

        /// <summary>Prefab del jefe, rehecho cada vez (conserva el GUID): raíz = punto del suelo, sombra y cuerpo elevado.</summary>
        static Enemy CreatePrefab(DirectionalArt art)
        {
            if (art == null || art.Find("Meganeura_Perch") == null)
            {
                Debug.LogError("MeganeuraBossSetup: faltan las hojas de la Meganeura (Primordia > Boss > Importar hojas de la Meganeura).");
                return null;
            }

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(DataPath);
            if (data == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }
            // No anda ni hace daño por contacto: sus ataques llegan con el combate (7b-7d).
            data.Configure(EnemyBehaviour.Chaser, maxHealth: BossMaxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            data.ConfigureSwimming(false);
            data.ConfigureBiomass(0f, 0f, 10);
            EditorUtility.SetDirty(data);

            // Lepidodendron partido de la sala: 4 cuadros de 64×96, pie en y = 90.
            var stump = ArtSetup.SliceFloor3Sheet(StumpPath, 64, new Vector2(0.5f, 6f / 96f));

            var root = new GameObject("Boss_Meganeura") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                // La sombra va en el suelo; el cuerpo, por encima de los enemigos del suelo.
                var shadowObject = new GameObject("Shadow") { layer = root.layer };
                shadowObject.transform.SetParent(root.transform, false);
                var shadow = shadowObject.AddComponent<SpriteRenderer>();
                shadow.sortingOrder = -3;
                var shadowSprite = shadowObject.AddComponent<DirectionalSprite>();
                shadowSprite.Configure(shadow, art);

                var bodyObject = new GameObject("Body") { layer = root.layer };
                bodyObject.transform.SetParent(root.transform, false);
                var body = bodyObject.AddComponent<SpriteRenderer>();
                body.sortingOrder = 30;
                var bodySprite = bodyObject.AddComponent<DirectionalSprite>();
                bodySprite.Configure(body, art);

                // La colisión queda donde se ve el tórax posado (3 u de altura × 0,57 = 1,7 u por encima del punto del suelo).
                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 1.5f;
                collider.offset = new Vector2(0f, 1.7f);

                var rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                root.AddComponent<Health>();

                var enemy = root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                enemy.SetAnimationTimings(1.2f, 0f);

                var boss = root.AddComponent<MeganeuraBoss>();
                boss.Configure(art, body, bodySprite, shadow, shadowSprite, stump);

                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return prefab.GetComponent<Enemy>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Copia de Floor_3 con el camino inicio → antesala → Meganeura, sin combates, especiales, tienda ni recompensas.</summary>
        static FloorConfig CreateTestConfig(FloorConfig floor3, FloorConfig.BossEntry entry)
        {
            AssetDatabase.DeleteAsset(TestConfigPath);
            var config = Object.Instantiate(floor3);
            AssetDatabase.CreateAsset(config, TestConfigPath);

            config.ConfigureRooms(floor3.StartRoom, entry.room, floor3.CombatRooms, System.Array.Empty<RoomTemplate>(),
                Vector2Int.zero, Vector2Int.zero);
            config.ConfigureSpecialRooms(System.Array.Empty<FloorConfig.SpecialRoom>(), Vector2Int.zero);
            config.ConfigureShortcuts(Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureFixedRooms(null, floor3.AntechamberVertical, floor3.AntechamberHorizontal);
            config.ConfigureRewards(floor3.RewardPool, System.Array.Empty<int>());
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

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

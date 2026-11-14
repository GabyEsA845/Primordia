using System;
using System.IO;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Primordia.Editor
{
    /// <summary>
    /// Arthropleura, subpaso A: importa sus hojas, crea la sala de prueba (suelo de 60×33 u de la F2), sus datos y su prefab, y la escena
    /// Test_BossArthropleura. No la conecta a Floor_3 (el enganche al piso va al final). Se puede volver a ejecutar.
    /// </summary>
    public static class ArthropleuraBossSetup
    {
        internal const string RoomPath = "Assets/_Primordia/Data/Rooms/Floor3/Room_Boss_Arthropleura.asset";
        internal const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Arthropleura.prefab";
        internal const string DataPath = "Assets/_Primordia/Data/Enemies/Floor3/Enemy_ArthropleuraBoss.asset";
        internal const string TestConfigPath = "Assets/_Primordia/Data/Rooms/Floor_BossArthropleuraTest.asset";
        internal const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossArthropleura.unity";
        // La plantilla rectangular suma 2 de pared a los lados y 4 arriba y abajo.
        const int FloorWidth = 60, FloorHeight = 33;

        [MenuItem("Primordia/Boss/Crear Arthropleura y escena de prueba")]
        public static void CreateAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor3 == null || floor3.BandThemes.Length < 3 || floor3.BandThemes[2] == null)
                throw new InvalidOperationException("Falta Floor_3 o su tema de Tierra quemada.");
            var art = ArtSetup.ImportArthropleuraSheets();
            var room = CreateRoom();
            var prefab = CreatePrefab(art);
            var entry = new FloorConfig.BossEntry(prefab, room, floor3.BandThemes[2], Vector2.zero);
            CreateTestConfig(floor3, entry);
            AssetDatabase.SaveAssets();
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Arthropleura A creada: cadena de 30 piezas en la sala de 60×33 u (Test_BossArthropleura).");
        }

        static RoomTemplate CreateRoom()
        {
            var room = LoadOrCreate<RoomTemplate>(RoomPath);
            room.SetLayout(RoomLayouts.Rectangle(FloorWidth + 2, FloorHeight + 4));
            var problems = room.Validate();
            if (problems.Count > 0)
                throw new InvalidOperationException("Sala Arthropleura inválida:\n" + string.Join("\n", problems));
            EditorUtility.SetDirty(room);
            return room;
        }

        static Enemy CreatePrefab(DirectionalArt art)
        {
            var data = LoadOrCreate<EnemyData>(DataPath);
            // No anda por la física ni hace daño por contacto: su combate llega con los subpasos B-E.
            data.Configure(EnemyBehaviour.Chaser, maxHealth: ArthropleuraBoss.MaxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            data.ConfigureSwimming(false);
            data.ConfigureBiomass(0f, 0f, 0);
            EditorUtility.SetDirty(data);
            var root = new GameObject("Boss_Arthropleura") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                root.AddComponent<Health>().IsInvulnerable = true;
                root.AddComponent<BossDamageLimiter>();
                var enemy = root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<ArthropleuraBoss>().Configure(art);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<Enemy>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void CreateTestConfig(FloorConfig floor3, FloorConfig.BossEntry entry)
        {
            // CopySerialized conserva el GUID del asset de prueba al repetir el menú.
            var config = LoadOrCreate<FloorConfig>(TestConfigPath);
            EditorUtility.CopySerialized(floor3, config);
            config.name = Path.GetFileNameWithoutExtension(TestConfigPath);
            config.ConfigureRooms(floor3.StartRoom, entry.room, floor3.CombatRooms, Array.Empty<RoomTemplate>(), Vector2Int.zero, Vector2Int.zero);
            config.ConfigureSpecialRooms(Array.Empty<FloorConfig.SpecialRoom>(), Vector2Int.zero);
            config.ConfigureShortcuts(Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureFixedRooms(null, floor3.AntechamberVertical, floor3.AntechamberHorizontal);
            config.ConfigureRewards(floor3.RewardPool, Array.Empty<int>());
            config.ConfigureEnemies(Array.Empty<FloorConfig.EnemyEntry>(), entry.prefab);
            config.ConfigureBosses(new[] { entry });
            config.ConfigureTransition(null, null, null, null);
            config.ConfigureEncounterThemes(Array.Empty<FloorConfig.EncounterTheme>(), Vector2Int.zero);
            EditorUtility.SetDirty(config);
        }

        static void CreateTestScene()
        {
            var cameraRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Core/CameraRig.prefab");
            var gameManager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Core/GameManager.prefab");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSetup.PlayerPath);
            if (cameraRig == null || gameManager == null || player == null)
                throw new InvalidOperationException("Faltan los prefabs base de cámara, GameManager o jugador.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                throw new InvalidOperationException("No se pudo crear Test_BossArthropleura.");
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

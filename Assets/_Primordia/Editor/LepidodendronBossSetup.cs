using System;
using System.IO;
using System.Linq;
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
    /// <summary>L1: crea solamente los assets de prueba del Lepidodendron; no lo conecta al piso real.</summary>
    public static class LepidodendronBossSetup
    {
        internal const string RoomPath = "Assets/_Primordia/Data/Rooms/Floor3/Room_Boss_Lepidodendron.asset";
        internal const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Lepidodendron.prefab";
        internal const string DataPath = "Assets/_Primordia/Data/Enemies/Floor3/Enemy_LepidodendronBoss.asset";
        internal const string TestConfigPath = "Assets/_Primordia/Data/Rooms/Floor_BossLepidodendronTest.asset";
        internal const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossLepidodendron.unity";
        const int Width = 158, Height = 30, CorridorSouth = 5, CorridorNorth = 22, ArenaWest = 117;

        [MenuItem("Primordia/Boss/Crear Lepidodendron y escena de prueba")]
        public static void CreateAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor3 == null || floor3.BandThemes.Length < 3 || floor3.BandThemes[2] == null)
                throw new InvalidOperationException("Falta Floor_3 o su tema de Tierra quemada.");
            var art = ArtSetup.ImportLepidodendronSheets();
            var room = CreateRoom();
            var prefab = CreatePrefab(art, floor3);
            var entry = new FloorConfig.BossEntry(prefab, room, floor3.BandThemes[2], Vector2.zero);
            CreateTestConfig(floor3, entry);
            AssetDatabase.SaveAssets();
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Lepidodendron L2 creado: paredes, nudos, fuego y oleadas; escena L1 ampliada.");
        }

        static RoomTemplate CreateRoom()
        {
            var room = LoadOrCreate<RoomTemplate>(RoomPath);
            // Rectángulo base, recortado a un pasillo centrado de 18 u y una arena de 26 u.
            var rows = RoomLayouts.Rectangle(Width, Height).TrimEnd('\n', '\r').Split('\n');
            for (int row = 0; row < rows.Length; row++)
            {
                int y = Height - 1 - row;
                var chars = rows[row].TrimEnd('\r').ToCharArray();
                for (int x = 0; x < Width; x++)
                {
                    bool floor = x >= 1 && x <= Width - 2 && y >= 1 && y <= Height - 4
                        && (x >= ArenaWest || y >= CorridorSouth && y <= CorridorNorth);
                    chars[x] = floor ? '.' : '#';
                }
                // La única entrada está al oeste, a media altura del pasillo.
                if (y == 13 || y == 14)
                    chars[0] = '<';
                rows[row] = new string(chars);
            }
            room.SetLayout(string.Join("\n", rows) + "\n");
            var problems = room.Validate();
            if (problems.Count > 0)
                throw new InvalidOperationException("Sala Lepidodendron inválida:\n" + string.Join("\n", problems));
            EditorUtility.SetDirty(room);
            return room;
        }

        static Enemy CreatePrefab(DirectionalArt art, FloorConfig floor3)
        {
            var data = LoadOrCreate<EnemyData>(DataPath);
            data.Configure(EnemyBehaviour.Chaser, maxHealth: LepidodendronBoss.MaxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            data.ConfigureSwimming(false);
            data.ConfigureBiomass(0f, 0f, 0);
            EditorUtility.SetDirty(data);
            var root = new GameObject("Boss_Lepidodendron") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                root.AddComponent<Health>().IsInvulnerable = true;
                root.AddComponent<BossDamageLimiter>();
                var enemy = root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var boss = root.AddComponent<LepidodendronBoss>();
                boss.Configure(art);
                var enemies = new[] { "Calamites", "Toad", "Spider", "Beetle", "Dragonfly" }
                    .Select(name => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Primordia/Prefabs/Enemies/Enemy_{name}.prefab")?.GetComponent<Enemy>()).ToArray();
                var fern = floor3.BandThemes.Where(t => t != null).SelectMany(t => t.Ferns).FirstOrDefault(f => f != null && f.IsValid && f.IsBurnable);
                var methane = floor3.BandThemes.Where(t => t != null).Select(t => t.Methane).FirstOrDefault(m => m != null && m.HasSacs);
                if (enemies.Any(e => e == null) || fern == null || methane == null || floor3.Reactions == null || floor3.Reactions.GroundFire.Length == 0)
                    throw new InvalidOperationException("Faltan prefabs o arte aprobado de enemigos, helechos, odres o fuego para L2.");
                // Los cinco prefabs salen de uno en uno: el tope de seis cuenta enemigos, no grupos.
                if (enemies.Any(e => floor3.Enemies.Any(entry => entry.prefab == e && entry.GroupSize != 1)))
                    throw new InvalidOperationException("Una especie de L2 está configurada como grupo; no se puede respetar su cola individual.");
                boss.ConfigureWallCombat(floor3, enemies, fern, methane);
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
            // Reutiliza los prefabs existentes; crearlos de nuevo tocaría assets ajenos a L1.
            var cameraRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Core/CameraRig.prefab");
            var gameManager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Core/GameManager.prefab");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSetup.PlayerPath);
            if (cameraRig == null || gameManager == null || player == null)
                throw new InvalidOperationException("Faltan los prefabs base de cámara, GameManager o jugador.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                throw new InvalidOperationException("No se pudo crear Test_BossLepidodendron.");
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            // El generador común elige cualquier dirección. Esta escena fija una semilla que entra por el oeste.
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath);
            int seed = FindWestEntrySeed(config);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!root.TryGetComponent<FloorGenerator>(out var generator))
                    continue;
                var serialized = new SerializedObject(generator);
                serialized.FindProperty("fixedSeed").intValue = seed;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene);
        }

        internal static int FindWestEntrySeed(FloorConfig config)
        {
            for (int seed = 1; seed <= 1000; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                bool west = layout.Boss.Doors.Count == 1 && layout.Boss.Doors[0] == Vector2Int.left;
                layout.ReleaseGenerated();
                if (west)
                    return seed;
            }
            throw new InvalidOperationException("No se encontró una semilla con entrada por el oeste.");
        }
    }
}

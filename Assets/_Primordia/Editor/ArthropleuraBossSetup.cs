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
        const string ArtFolder = "Assets/_Primordia/Art/Floor3/floor3-bosses/arthropleura";
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
            var prefab = CreatePrefab(art, floor3);
            ArtSetup.ImportBossIntro("Arthropleura");
            var entry = new FloorConfig.BossEntry(prefab, room, floor3.BandThemes[2], Vector2.zero);
            CreateTestConfig(floor3, entry);
            AssetDatabase.SaveAssets();
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Arthropleura creada: presentación, cadena, fuego, odres y decorado del claro (Test_BossArthropleura).");
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

        static Enemy CreatePrefab(DirectionalArt art, FloorConfig floor3)
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
                var boss = root.AddComponent<ArthropleuraBoss>();
                boss.Configure(art);
                root.AddComponent<BossIntro>().enabled = false;
                boss.ConfigureClearing(CreateClearingArt());
                var fern = floor3.BandThemes.Where(t => t != null).SelectMany(t => t.Ferns).FirstOrDefault(f => f != null && f.IsValid && f.IsBurnable);
                if (fern == null)
                    throw new InvalidOperationException("Falta un helecho inflamable aprobado en los temas del piso 3.");
                boss.ConfigureFern(fern);
                boss.ConfigureEffects(new ArthropleuraBoss.Effects
                {
                    edgeBreak = Sheet("Effects/Fx_Arthropleura_EdgeBreak", 96, new Vector2(0.5f, 1f)),
                    tremor = Sheet("Effects/Fx_Arthropleura_Tremor", 48, new Vector2(0.5f, 0f)),
                    chargeAim = Sheet("Effects/Fx_Arthropleura_ChargeAim", 16),
                    chargeDust = Sheet("Effects/Fx_Arthropleura_ChargeDust", 48, new Vector2(1f, 0.5f)),
                    wallBreak = Sheet("Effects/Fx_Arthropleura_WallBreak", 96),
                    stomp = Sheet("Effects/Fx_Arthropleura_Stomp", 32, new Vector2(0.5f, 0f)),
                    shockwave = Sheet("Effects/Fx_Arthropleura_Shockwave", 16),
                });
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<Enemy>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        static Sprite[] Sheet(string relative, int cell, Vector2? pivot = null) =>
            ArtSetup.SliceFloor3Sheet($"{ArtFolder}/{relative}.png", cell, pivot ?? new Vector2(0.5f, 0.5f));

        static Sprite[] ExistingSprites(string relative)
        {
            string path = "Assets/_Primordia/Art/Floor3/" + relative + ".png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int frame) ? frame : 0).ToArray();
            if (sprites.Length == 0)
                throw new InvalidOperationException("Falta arte aprobado importado: " + path);
            return sprites;
        }

        static ArthropleuraBoss.ClearingArt CreateClearingArt()
        {
            const string props = "floor3-environment/Smoke/props/";
            return new ArthropleuraBoss.ClearingArt
            {
                sacIdle = ExistingSprites(props + "Methane_Sac_Idle"),
                sacBurst = ExistingSprites("floor3-reactions/Smoke/Methane_Sac_Burst"),
                sacRemains = ExistingSprites("floor3-reactions/Smoke/Methane_Sac_Remains")[0],
                ignite = ArtSetup.SliceFloor3Sheet("Assets/_Primordia/Art/Floor3/floor3-reactions/Effects/Fx_Ignite.png", 16, new Vector2(0.5f, 0.5f)),
                decor = new[]
                {
                    ExistingSprites(props + "Fern_Leafy_Sway")[0], ExistingSprites(props + "Fern_Tree_Sway")[0],
                    ExistingSprites(props + "Fern_Bent_Sway")[0], ExistingSprites(props + "Fern_Young_Sway")[0],
                    ExistingSprites(props + "Fern_Sparse_Sway")[0], ExistingSprites(props + "Puddle_Sludge_Island")[0],
                    ExistingSprites(props + "Puddle_Sludge_Island")[0], ExistingSprites(props + "Obstacle_Stump")[0],
                },
                decorPositions = new[]
                {
                    new Vector2(-10f, 5f), new Vector2(10f, 2f), new Vector2(-7f, -6f), new Vector2(7f, -6f),
                    new Vector2(12f, -3f), new Vector2(0f, -6.5f), new Vector2(-10f, -4f), new Vector2(11f, 6f),
                },
            };
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

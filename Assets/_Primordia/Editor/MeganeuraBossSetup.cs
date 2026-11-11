using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using Primordia.UI;
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
        const string NymphPrefabPath = "Assets/_Primordia/Prefabs/Enemies/Enemy_MeganeuraNymph.prefab";
        const string NymphDataPath = "Assets/_Primordia/Data/Enemies/Floor3/Enemy_MeganeuraNymph.asset";
        const string DataPath = "Assets/_Primordia/Data/Enemies/Floor3/Enemy_MeganeuraBoss.asset";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossMeganeura.unity";
        const string TestConfigPath = "Assets/_Primordia/Data/Rooms/Floor_BossMeganeuraTest.asset";
        const string ArtFolder = "Assets/_Primordia/Art/Floor3/floor3-bosses/meganeura";
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

            // Lepidodendron partido de la sala: 4 cuadros de 64×96, pie en y = 90 (reposo, golpe y herida abierta).
            var stumpPivot = new Vector2(0.5f, 6f / 96f);
            // Racimos de 40×32 con el pie en y = 26.
            var eggPivot = new Vector2(0.5f, 6f / 32f);
            var stump = ArtSetup.SliceFloor3Sheet(StumpPath, 64, stumpPivot);
            var effects = new MeganeuraBoss.Effects
            {
                diveMarkFollow = Sheet("Effects/Fx_Meganeura_DiveMark_Follow", 64),
                diveMarkLocked = Sheet("Effects/Fx_Meganeura_DiveMark_Locked", 64),
                diveImpact = Sheet("Effects/Fx_Meganeura_DiveImpact", 112),
                diveCrack = Sheet("Effects/Fx_Meganeura_DiveCrack", 64),
                // La ráfaga sale del borde izquierdo hacia la derecha: pivote a la izquierda para girarla desde su origen.
                gust = Sheet("Effects/Fx_Meganeura_Gust", 112, new Vector2(0f, 0.5f)),
                crash = Sheet("Effects/Fx_Meganeura_Crash", 96),
                splinters = Sheet("Effects/Fx_Meganeura_Splinters", 64),
                stumpHit = Sheet("Burnt/Lepido_Split_Hit", 64, stumpPivot),
                stumpGash = Sheet("Burnt/Lepido_Split_Gash", 64, stumpPivot),
                eggAppear = Sheet("Eggs/Meganeura_Eggs_Appear", 40, eggPivot),
                eggIdle = Sheet("Eggs/Meganeura_Eggs_Idle", 40, eggPivot),
                eggHatch = Sheet("Eggs/Meganeura_Eggs_Hatch", 40, eggPivot),
                eggBreak = Sheet("Eggs/Meganeura_Eggs_Break", 40, eggPivot),
                screech = Sheet("Effects/Fx_Meganeura_Screech", 160),
                sigillaria = Sheet("Burnt/Sigillaria_Struck", 72, new Vector2(0.5f, 6f / 72f)).Take(1).ToArray(),
                sigillariaStruck = Sheet("Burnt/Sigillaria_Struck", 72, new Vector2(0.5f, 6f / 72f)),
                sigillariaBurning = Sheet("Burnt/Sigillaria_Burning", 72, new Vector2(0.5f, 6f / 72f)),
                sigillariaSmolder = Sheet("Burnt/Sigillaria_Smolder", 72, new Vector2(0.5f, 6f / 72f)),
                sigillariaSplit = Sheet("Burnt/Sigillaria_Split", 72, new Vector2(0.5f, 6f / 72f)),
                // Rayos: el pie del rayo (abajo-centro) en el punto de impacto; el suelo del impacto grande en y = 60 de 96.
                boltBigMark = Sheet("Effects/Fx_Bolt_Big_Mark", 96),
                boltBig = Sheet("Effects/Fx_Bolt_Big", 96, new Vector2(0.5f, 8f / 360f)),
                boltBigImpact = Sheet("Effects/Fx_Bolt_Big_Impact", 128, new Vector2(0.5f, 36f / 96f)),
                boltSmallMark = Sheet("Effects/Fx_Bolt_Small_Mark", 48),
                boltSmall = Sheet("Effects/Fx_Bolt_Small", 48, new Vector2(0.5f, 8f / 360f)),
                boltSmallImpact = Sheet("Effects/Fx_Bolt_Small_Impact", 64, new Vector2(0.5f, 0.3f)),
                boltScorch = Sheet("Effects/Fx_Bolt_Scorch", 64),
                boltRing = Sheet("Effects/Fx_Bolt_Ring", 96),
                boltHuge = Sheet("Effects/Fx_Bolt_Huge", 192, new Vector2(0.5f, 8f / 420f)),
                rain = Sheet("Effects/Fx_Storm_Rain", 128),
                ash = Sheet("Effects/Fx_Storm_WindAsh", 128),
                puddleElectric = new[] { ("Round", 34), ("Kidney", 46), ("Lobed", 46), ("Splash", 44) }
                    .Select(p => new MeganeuraBoss.Effects.FrameSet { frames = Sheet($"Burnt/Puddle_Water_{p.Item1}_Electric", p.Item2) }).ToArray(),
                puddleGrow = new[] { ("Round", 34), ("Kidney", 46), ("Splash", 44) }
                    .Select(p => new MeganeuraBoss.Effects.FrameSet { frames = Sheet($"Burnt/Puddle_Water_{p.Item1}_Grow", p.Item2) }).ToArray(),
                puddles = new[] { "Round", "Kidney", "Lobed" }.Select(shape => PuddleSprite(shape)).Where(s => s != null).ToArray(),
            };
            var nymph = CreateNymph(art);
            var barSkin = ArtSetup.CreateMeganeuraBarSkin();

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

                // Capa de fuego sobre el cuerpo (cae y queda en el suelo con las alas ardiendo).
                var fireObject = new GameObject("Fire") { layer = root.layer };
                fireObject.transform.SetParent(bodyObject.transform, false);
                var fire = fireObject.AddComponent<SpriteRenderer>();
                fire.sortingOrder = 31;
                fire.enabled = false;
                var fireSprite = fireObject.AddComponent<DirectionalSprite>();
                fireSprite.Configure(fire, art);

                // La colisión queda donde se ve el tórax posado (3 u de altura × 0,57 = 1,7 u por encima del punto del suelo).
                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 1.5f;
                collider.offset = new Vector2(0f, 1.7f);

                var rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                root.AddComponent<Health>();
                root.AddComponent<BossDamageLimiter>();

                var enemy = root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                enemy.SetAnimationTimings(1.2f, 0f);

                var boss = root.AddComponent<MeganeuraBoss>();
                boss.Configure(art, body, bodySprite, shadow, shadowSprite, stump);
                boss.ConfigureFight(fire, fireSprite, effects, nymph);
                boss.ConfigureDeath(ImportDeathEffects());
                if (barSkin != null)
                    root.AddComponent<BossBarAppearance>().Configure(barSkin);

                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return prefab.GetComponent<Enemy>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Charca de agua del tramo de Tierra quemada (una sola imagen).</summary>
        static Sprite PuddleSprite(string shape)
        {
            string path = $"Assets/_Primordia/Art/Floor3/floor3-environment/Burnt/props/Puddle_Water_{shape}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                return null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null ? sprite : AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        /// <summary>Ninfa de la Meganeura (prefab y datos): 8 de vida, anda deprisa y lanza el labio.</summary>
        static Enemy CreateNymph(DirectionalArt art)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(NymphDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, NymphDataPath);
            }
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 8, moveSpeed: 3.4f, detectionRange: 0f, contactDamage: 0);
            data.ConfigureSwimming(false);
            data.ConfigureBiomass(0f, 0f, 0);
            EditorUtility.SetDirty(data);

            var root = new GameObject("Enemy_MeganeuraNymph") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var shadowObject = new GameObject("Shadow") { layer = root.layer };
                shadowObject.transform.SetParent(root.transform, false);
                var shadow = shadowObject.AddComponent<SpriteRenderer>();
                shadow.sortingOrder = -3;
                shadow.color = new Color(1f, 1f, 1f, 0.45f);
                var shadowSprite = shadowObject.AddComponent<DirectionalSprite>();
                shadowSprite.Configure(shadow, art);

                var bodyObject = new GameObject("Body") { layer = root.layer };
                bodyObject.transform.SetParent(root.transform, false);
                var body = bodyObject.AddComponent<SpriteRenderer>();
                body.sortingOrder = 6;
                var bodySprite = bodyObject.AddComponent<DirectionalSprite>();
                bodySprite.Configure(body, art);

                root.AddComponent<CircleCollider2D>().radius = 0.5f;
                var rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                root.AddComponent<Health>();
                var enemy = root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                enemy.SetAnimationTimings(0.7f, 0f);
                root.AddComponent<MeganeuraNymph>().Configure(body, bodySprite, shadowSprite);
                return PrefabUtility.SaveAsPrefabAsset(root, NymphPrefabPath).GetComponent<Enemy>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Importa solo los efectos del final y actualiza el prefab sin reconstruir la escena.</summary>
        [MenuItem("Primordia/Boss/Importar efectos de muerte de la Meganeura")]
        public static void ImportDeath()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError("Falta el prefab Boss_Meganeura.");
                return;
            }
            var effects = ImportDeathEffects();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.GetComponent<MeganeuraBoss>().ConfigureDeath(effects);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Efectos de muerte de la Meganeura importados y asignados.");
        }

        static MeganeuraBoss.DeathEffects ImportDeathEffects() => new()
        {
            // El agua queda a 70 px desde arriba: 26 px desde abajo en la celda de 96.
            splash = Sheet("Effects/Fx_Meganeura_Splash", 128, new Vector2(0.5f, 26f / 96f)),
            ripple = Sheet("Effects/Fx_Meganeura_DeadRipple", 160),
            drizzle = Sheet("Effects/Fx_Storm_Drizzle", 128),
            steam = Sheet("Effects/Fx_Storm_Steam", 48),
        };

        static Sprite[] Sheet(string relative, int cell, Vector2? pivot = null) =>
            ArtSetup.SliceFloor3Sheet($"{ArtFolder}/{relative}.png", cell, pivot ?? new Vector2(0.5f, 0.5f));

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

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
    public static class MegarachneBossSetup
    {
        internal const string RoomPath = "Assets/_Primordia/Data/Rooms/Floor3/Room_Boss_Megarachne.asset";
        internal const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Megarachne.prefab";
        internal const string DataPath = "Assets/_Primordia/Data/Enemies/Floor3/Enemy_MegarachneBoss.asset";
        internal const string TestConfigPath = "Assets/_Primordia/Data/Rooms/Floor_BossMegarachneTest.asset";
        internal const string TestScenePath = "Assets/_Primordia/Scenes/Test_BossMegarachne.unity";
        const string MaterialPath = "Assets/_Primordia/Data/Bosses/MegarachneCable.mat";

        const string ThinMaterialPath = "Assets/_Primordia/Data/Bosses/MegarachneCableThin.mat";
        const string CableRootPath = ArtSetup.MegarachneFolder + "/Nido/Nido_CableRoot.png";

        [MenuItem("Primordia/Boss/Crear sala Megarachne")]
        public static void CreateAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor3 == null || floor3.BandThemes.Length < 2 || floor3.BandThemes[1] == null)
                throw new InvalidOperationException("Falta el tema Bosque humeante del piso 3.");
            var art = ArtSetup.ImportMegarachneSheets();
            var room = LoadOrCreate<RoomTemplate>(RoomPath);
            room.SetLayout(RoomLayouts.Rectangle(34, 24));
            if (room.Validate().Count > 0) throw new InvalidOperationException("Sala Megarachne inválida.");
            EditorUtility.SetDirty(room);
            var prefab = CreatePrefab(art);
            var entry = new FloorConfig.BossEntry(prefab, room, floor3.BandThemes[1], Vector2.zero);
            CreateTestConfig(floor3, entry);
            AssetDatabase.SaveAssets();
            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Megarachne M1: prefab y Test_BossMegarachne creados (32×20 u, cámara ×1).");
        }

        [MenuItem("Primordia/Boss/Actualizar ataques Megarachne (M2)")]
        public static void UpdateAttacks()
        {
            ArtSetup.ImportMegarachneSheets();
            var effects = ArtSetup.ImportMegarachneAttacks();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.GetComponent<MegarachneBoss>().ConfigureAttacks(effects);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Megarachne M2: ataques y efectos actualizados; Ctrl+Mayus+F4 salto, F5 rafaga, F6 red.");
        }

        [MenuItem("Primordia/Boss/Actualizar cables Megarachne (v2)")]
        public static void UpdateCables()
        {
            var roots = ArtSetup.ImportMegarachneCables();
            if (roots.Length != 5) throw new InvalidOperationException("Se esperaban cinco arranques de cable.");
            var (material, thinMaterial) = PrepareCableMaterials();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.GetComponent<MegarachneBoss>().ConfigureCableArt(roots, material, thinMaterial);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("No se pudo guardar el prefab de Megarachne.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Megarachne: cables v2 importados y prefab actualizado.");
        }

        [MenuItem("Primordia/Boss/Actualizar saco y crías Megarachne (M3)")]
        public static void UpdateBrood()
        {
            var effects = ArtSetup.ImportMegarachneBrood();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.GetComponent<MegarachneBoss>().ConfigureBrood(effects);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("No se pudo guardar el prefab de Megarachne.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Megarachne M3: ocho hojas importadas y prefab actualizado.");
        }

        [MenuItem("Primordia/Boss/Actualizar fuego Megarachne (M4)")]
        public static void UpdateFire()
        {
            var art = ArtSetup.ImportMegarachneFire();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.GetComponent<MegarachneBoss>().ConfigureFire(art);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("No se pudo guardar el prefab de Megarachne.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Megarachne M4: fuego importado y prefab actualizado.");
        }

        static (Material cable, Material thread) PrepareCableMaterials() =>
            (PrepareCableMaterial(MaterialPath, "Cable_Normal", 1),
             PrepareCableMaterial(ThinMaterialPath, "Cable_Thin", 3));

        static Material PrepareCableMaterial(string path, string textureName, int frames)
        {
            // El shader aplica la escala y el desplazamiento de la textura a las UV de la línea.
            var shader = Shader.Find("Unlit/Transparent");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtSetup.MegarachneFolder + "/Effects/" + textureName + ".png");
            material.mainTextureScale = new Vector2(0.25f, 1f / frames);
            material.mainTextureOffset = Vector2.zero;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Enemy CreatePrefab(DirectionalArt art)
        {
            var data = LoadOrCreate<EnemyData>(DataPath);
            data.Configure(EnemyBehaviour.Chaser, maxHealth: MegarachneBoss.MaxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            data.ConfigureSwimming(false);
            data.ConfigureBiomass(0f, 0f, 0);
            EditorUtility.SetDirty(data);
            var roots = AssetDatabase.LoadAllAssetsAtPath(CableRootPath).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            var (material, thinMaterial) = PrepareCableMaterials();

            var root = new GameObject("Boss_Megarachne") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            root.SetActive(false);
            try
            {
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                root.AddComponent<Health>();
                root.AddComponent<BossDamageLimiter>();
                var enemy = root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var boss = root.AddComponent<MegarachneBoss>();
                boss.Configure(AddSprite(root, "Cuerpo", art, "Megarachne_Idle", 5),
                    AddSprite(root, "Sombra", art, "Megarachne_Shadow_Idle", 1), true);
                boss.ConfigureScenery(TreePiece("Copa"), TreePiece("Tronco"),
                    SpriteAt(ArtSetup.MegarachneFolder + "/Nido/Nido_Web.png"),
                    SpriteAt("Assets/_Primordia/Art/Floor3/floor3-environment/Smoke/props/Obstacle_Sigillaria.png"), roots, material, thinMaterial);
                boss.ConfigureAttacks(ArtSetup.ImportMegarachneAttacks());
                boss.ConfigureBrood(ArtSetup.ImportMegarachneBrood());
                boss.ConfigureFire(ArtSetup.ImportMegarachneFire());
                root.SetActive(true);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<Enemy>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        static DirectionalSprite AddSprite(GameObject parent, string name, DirectionalArt art, string sheet, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = art.Find(sheet).Get(8, 0);
            renderer.sortingOrder = order;
            var directional = go.AddComponent<DirectionalSprite>();
            directional.Configure(renderer, art);
            return directional;
        }

        static Sprite TreePiece(string name) => AssetDatabase.LoadAllAssetsAtPath(ArtSetup.MegarachneFolder + "/Nido/Nido_Tree_Wet.png").OfType<Sprite>().Single(s => s.name == "Nido_Tree_Wet_" + name);

        static Sprite SpriteAt(string path)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
            if (sprite == null) throw new FileNotFoundException("Falta arte aprobado importado: " + path);
            return sprite;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
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
                throw new InvalidOperationException("No se pudo crear Test_BossMegarachne.");
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}



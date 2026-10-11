using System.IO;
using Primordia.Core;
using Primordia.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Crea el prefab del jugador y la escena de prueba del módulo Player.
    /// No sobrescribe nada que ya exista, así que se puede ejecutar varias veces.
    /// </summary>
    public static class PlayerSetup
    {
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Player";
        const string PlayerPath = PrefabsFolder + "/Player.prefab";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Player.unity";

        // Sala de prueba más grande que la vista (30×17 unidades) para ver cómo sigue la cámara.
        const float RoomWidth = 40f;
        const float RoomHeight = 24f;

        static readonly Color WallColor = new(0.35f, 0.35f, 0.4f);

        [MenuItem("Primordia/Player/Crear prefab y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = CreatePlayerPrefab(square);
            CreateTestScene(cameraRig, gameManager, player, square);
        }

        static GameObject CreatePlayerPrefab(Sprite square)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (existing != null)
                return existing;

            Directory.CreateDirectory(PrefabsFolder);
            var root = new GameObject("Player");

            root.AddComponent<Rigidbody2D>();
            root.AddComponent<CircleCollider2D>().radius = 0.4f;

            var health = root.AddComponent<Health>();
            health.InvulnerabilityAfterHit = 1f;

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = square;
            bodyRenderer.color = new Color(0.45f, 0.85f, 0.45f);
            bodyRenderer.sortingOrder = 10;

            // Marca provisional de la "boca": muestra hacia dónde apunta el jugador.
            var aimPivot = new GameObject("AimPivot");
            aimPivot.transform.SetParent(root.transform, false);
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(aimPivot.transform, false);
            mouth.transform.localPosition = new Vector3(0.75f, 0f, 0f);
            mouth.transform.localScale = new Vector3(0.375f, 0.375f, 1f);
            var mouthRenderer = mouth.AddComponent<SpriteRenderer>();
            mouthRenderer.sprite = square;
            mouthRenderer.color = new Color(0.95f, 0.85f, 0.3f);
            mouthRenderer.sortingOrder = 11;

            var controller = root.AddComponent<PlayerController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("aimPivot").objectReferenceValue = aimPivot.transform;
            serialized.FindProperty("body").objectReferenceValue = bodyRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab, Sprite square)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            rig.GetComponentInChildren<CinemachineCamera>().Follow = player.transform;

            var walls = new GameObject("Walls").transform;
            float halfWidth = RoomWidth / 2f;
            float halfHeight = RoomHeight / 2f;
            CreateBlock(walls, "Wall_Top", new Vector2(0f, halfHeight), new Vector2(RoomWidth + 1f, 1f), square);
            CreateBlock(walls, "Wall_Bottom", new Vector2(0f, -halfHeight), new Vector2(RoomWidth + 1f, 1f), square);
            CreateBlock(walls, "Wall_Left", new Vector2(-halfWidth, 0f), new Vector2(1f, RoomHeight + 1f), square);
            CreateBlock(walls, "Wall_Right", new Vector2(halfWidth, 0f), new Vector2(1f, RoomHeight + 1f), square);

            // Obstáculos para probar choques y esquivas contra paredes.
            CreateBlock(walls, "Obstacle", new Vector2(-8f, 4f), new Vector2(3f, 3f), square);
            CreateBlock(walls, "Obstacle", new Vector2(9f, -5f), new Vector2(2f, 6f), square);
            CreateBlock(walls, "Obstacle", new Vector2(5f, 6f), new Vector2(6f, 1f), square);

            EditorSceneManager.SaveScene(scene);
        }

        static void CreateBlock(Transform parent, string name, Vector2 position, Vector2 size, Sprite square)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = WallColor;

            block.AddComponent<BoxCollider2D>();
        }
    }
}

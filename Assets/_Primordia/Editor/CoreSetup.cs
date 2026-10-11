using System.IO;
using Primordia.Core;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Primordia.Editor
{
    /// <summary>
    /// Crea los prefabs base (CameraRig y GameManager) y la escena de prueba del módulo Core.
    /// No sobrescribe nada que ya exista, así que se puede ejecutar varias veces.
    /// </summary>
    public static class CoreSetup
    {
        const int PixelsPerUnit = 16;
        const int ReferenceWidth = 480;
        const int ReferenceHeight = 270;

        const string Root = "Assets/_Primordia";
        const string PrefabsFolder = Root + "/Prefabs/Core";
        const string CameraRigPath = PrefabsFolder + "/CameraRig.prefab";
        const string GameManagerPath = PrefabsFolder + "/GameManager.prefab";
        const string PlaceholderFolder = Root + "/Art/Placeholder";
        const string SquareSpritePath = PlaceholderFolder + "/Square16.png";
        const string TemplateScenePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        const string TestScenePath = Root + "/Scenes/Test_Core.unity";

        [MenuItem("Primordia/Core/Crear prefabs y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;


            var square = CreateSquareSprite();
            var cameraRig = CreateCameraRigPrefab();
            var gameManager = CreateGameManagerPrefab();
            CreateTestScene(cameraRig, gameManager, square);
        }

        internal static Sprite CreateSquareSprite()
        {
            Directory.CreateDirectory(PlaceholderFolder);
            if (!File.Exists(SquareSpritePath))
            {
                // Cuadrado blanco de 16×16 con borde oscuro de 1 px, para ver los píxeles en pantalla.
                var texture = new Texture2D(PixelsPerUnit, PixelsPerUnit);
                var pixels = new Color32[PixelsPerUnit * PixelsPerUnit];
                for (int y = 0; y < PixelsPerUnit; y++)
                for (int x = 0; x < PixelsPerUnit; x++)
                {
                    bool border = x == 0 || y == 0 || x == PixelsPerUnit - 1 || y == PixelsPerUnit - 1;
                    pixels[y * PixelsPerUnit + x] = border ? new Color32(40, 40, 40, 255) : new Color32(255, 255, 255, 255);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(SquareSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(SquareSpritePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        }

        internal static GameObject CreateCameraRigPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPath);
            if (existing != null)
                return existing;

            Directory.CreateDirectory(PrefabsFolder);
            var root = new GameObject("CameraRig");
            float orthographicSize = ReferenceHeight / (2f * PixelsPerUnit);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            var pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = PixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceWidth;
            pixelPerfect.refResolutionY = ReferenceHeight;

            cameraObject.AddComponent<CinemachineBrain>();

            var virtualCameraObject = new GameObject("CinemachineCamera");
            virtualCameraObject.transform.SetParent(root.transform, false);
            virtualCameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);

            var virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = orthographicSize;
            virtualCamera.Lens = lens;

            var composer = virtualCameraObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.3f, 0.3f, 0f);

            virtualCameraObject.AddComponent<CinemachinePixelPerfect>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CameraRigPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        internal static GameObject CreateGameManagerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GameManagerPath);
            if (existing != null)
                return existing;

            Directory.CreateDirectory(PrefabsFolder);
            var root = new GameObject("GameManager");
            root.AddComponent<GameManager>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GameManagerPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Crea una escena a partir de la plantilla 2D URP con el CameraRig y el GameManager.
        /// Si la escena ya existe, solo la abre y devuelve false.
        /// </summary>
        internal static bool TryCreateSceneFromTemplate(string path, GameObject cameraRig, GameObject gameManager,
            out Scene scene, out GameObject rig)
        {
            if (File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path);
                rig = null;
                return false;
            }

            // La plantilla ya trae la Global Light 2D; solo cambiamos su cámara por el CameraRig.
            AssetDatabase.CopyAsset(TemplateScenePath, path);
            scene = EditorSceneManager.OpenScene(path);

            var templateCamera = GameObject.Find("Main Camera");
            if (templateCamera != null)
                Object.DestroyImmediate(templateCamera);

            rig = (GameObject)PrefabUtility.InstantiatePrefab(cameraRig, scene);
            PrefabUtility.InstantiatePrefab(gameManager, scene);
            return true;
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, Sprite square)
        {
            if (!TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            var dummy = new GameObject("Dummy");
            var renderer = dummy.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = new Color(0.45f, 0.85f, 0.45f);
            dummy.AddComponent<Health>();

            // Marcas fijas en el borde de la vista para comprobar la resolución de referencia.
            float halfWidth = ReferenceWidth / (2f * PixelsPerUnit) - 0.5f;
            float halfHeight = ReferenceHeight / (2f * PixelsPerUnit) - 0.5f;
            var markers = new GameObject("Markers").transform;
            foreach (var corner in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 1) })
            {
                var marker = new GameObject("Marker");
                marker.transform.SetParent(markers, false);
                marker.transform.position = new Vector3(corner.x * halfWidth, corner.y * halfHeight, 0f);
                var markerRenderer = marker.AddComponent<SpriteRenderer>();
                markerRenderer.sprite = square;
                markerRenderer.color = new Color(0.5f, 0.5f, 0.5f);
            }

            rig.GetComponentInChildren<CinemachineCamera>().Follow = dummy.transform;

            EditorSceneManager.SaveScene(scene);
        }
    }
}

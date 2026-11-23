using System;
using System.IO;
using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Primordia.Editor
{
    public static partial class FloorTransitionSetup
    {
        internal const string SandPitPrefabPath = PrefabsFolder + "/TransitionPit_Sand.prefab";
        internal const string Floor3TestScenePath = "Assets/_Primordia/Scenes/Test_Floor3Transition.unity";
        internal const string Floor3TestConfigPath = RoomsFolder + "/Floor_3TransitionTest.asset";

        [MenuItem("Primordia/Floor transition/Crear sala de paso del piso 3 (sumidero) y escena de prueba")]
        internal static void CreateFloor3Transition()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            var chaser = EnemySetup.LoadPrefab("Enemy_Chaser");
            if (floor3 == null || chaser == null)
                throw new InvalidOperationException("Falta Floor_3 o Enemy_Chaser; crea antes los assets del juego.");

            var (mid, next, art) = ArtSetup.ImportFloor3Exit();
            if (mid == null || next == null || art.Pit.Length != 8 || art.Bubble.Length != 4)
                throw new InvalidOperationException("La importación de la salida del piso 3 está incompleta.");
            var cave = CreateCaveTemplate();
            var pit = CreatePitPrefab(null, art, SandPitPrefabPath, 2);
            // Duración exacta del arte aprobado; no cambia la velocidad de las fosas anteriores.
            var serializedPit = new SerializedObject(pit);
            serializedPit.FindProperty("pitFramesPerSecond").floatValue = 1f / 0.167f;
            serializedPit.ApplyModifiedPropertiesWithoutUndo();
            floor3.ConfigureTransition(cave, pit, mid, next);
            floor3.ConfigureFloor(3, null);
            floor3.ConfigureNextFloor(Array.Empty<MutationData>(), "");
            EditorUtility.SetDirty(floor3);

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3TestConfigPath);
            if (config == null)
            {
                config = Object.Instantiate(floor3);
                AssetDatabase.CreateAsset(config, Floor3TestConfigPath);
            }
            else
                EditorUtility.CopySerialized(floor3, config);
            var normal = AssetDatabase.LoadAssetAtPath<RoomTemplate>($"{RoomsFolder}/{RoomLayouts.Normal}.asset");
            config.ConfigureRooms(floor3.StartRoom, normal, new[] { normal }, Array.Empty<RoomTemplate>(), Vector2Int.zero, Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureRewards(floor3.RewardPool, Array.Empty<int>());
            config.ConfigureBosses(new[] { new FloorConfig.BossEntry(chaser, normal, ArtSetup.LoadFloor3Theme("Burnt"), Vector2.zero) });
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(Floor3TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(Floor3TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                throw new InvalidOperationException("No se pudo crear Test_Floor3Transition.");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Player/Player.prefab");
            if (player == null)
                throw new InvalidOperationException("Falta el prefab del jugador.");
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("FloorTransitionSetup: Test_Floor3Transition creada (Burnt > BurntSand > Permian, sumidero). Sin piso 4 ni Bocas: victoria.");
        }

        [MenuItem("Primordia/Floor transition/Capturar revisión de la sala del piso 3")]
        internal static void CaptureFloor3Transition()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3TestConfigPath);
            var root = new GameObject("RevisionSalidaPiso3");
            var layout = FloorLayoutGenerator.Generate(config, new System.Random(42));
            var built = FloorBuilder.Build(layout, config, root.transform, new System.Random(42));
            var room = built.MapRooms.Single(item => item.Role == RoomRole.Transition);
            var light = new GameObject("LuzRevision").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            var camera = new GameObject("CamaraRevision").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = (room.Rect.height + 4) / 2f;
            camera.transform.position = new Vector3(room.Rect.center.x, room.Rect.center.y, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var texture = new RenderTexture((room.Rect.width + 4) * 32, (room.Rect.height + 4) * 32, 24);
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/tarea20-sala.png", image.EncodeToPNG());
                Debug.Log("FloorTransitionSetup: captura de revisión en Logs/tarea20-sala.png (no modifica el arte).");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                texture.Release();
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(camera.gameObject);
                Object.DestroyImmediate(light.gameObject);
                Object.DestroyImmediate(root);
                layout.ReleaseGenerated();
            }
        }
    }
}

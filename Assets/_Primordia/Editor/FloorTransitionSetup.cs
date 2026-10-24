using System.IO;
using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Sala de paso: la plantilla de la cueva, los temas Shore y Cambrian, el prefab de la fosa (con su metamorfosis) y
    /// su icono del mapa, las Bocas del piso 2 con sus criaturas, la ventana de elección de Boca y la criatura en la
    /// victoria, y la añade a Floor_1 (pegada a la sala del jefe, se abre al vencerlo). También crea la escena Test_FloorTransition:
    /// inicio con unas burbujas de mutación, un "jefe" de prueba (un perseguidor en una sala normal) y la sala de paso.
    /// Se puede volver a ejecutar: actualiza el arte, el prefab y la escena; la plantilla no la sobrescribe.
    /// </summary>
    public static partial class FloorTransitionSetup
    {
        const string RoomsFolder = "Assets/_Primordia/Data/Rooms";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Rooms";
        internal const string PitPrefabPath = PrefabsFolder + "/TransitionPit.prefab";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_FloorTransition.unity";
        const string TestConfigPath = RoomsFolder + "/Floor_TransitionTest.asset";
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations";
        const string PickupPath = "Assets/_Primordia/Prefabs/Mutations/MutationPickup.prefab";

        // La fosa mide 7×5 tiles; su borde sólido, algo menos, para que el jugador se acerque al labio.
        static readonly Vector2 EdgeRadii = new(3.1f, 2.1f);
        static readonly Vector2 ColliderSize = new(6.0f, 4.0f);
        const float InteractRadius = 1.4f;
        const int PitSortingOrder = -12;
        const int KeySortingOrder = 20;

        [MenuItem("Primordia/Floor transition/Crear sala de paso y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            UISetup.CreateUIAssets();

            var cave = CreateCaveTemplate();
            var (mid, next) = ArtSetup.ImportTransitionThemes();
            if (mid == null || next == null)
                Debug.LogWarning("FloorTransitionSetup: faltan los temas Shore o Cambrian; genera el arte con "
                    + "docs/multimedia-temp/environment/generar_tema_cambrico.py. La sala se verá con el tema de la sopa.");

            var pit = CreatePitPrefab(square);
            ArtSetup.ApplyTransitionMapIcon();
            var mouths = CreateFloor2Mouths();
            EnsureMouthChoice();
            ArtSetup.EnsureVictoryCreature();
            AddToFloor(cave, pit, mid, next, mouths);
            AssetDatabase.SaveAssets();

            CreateTestScene(cameraRig, gameManager, player, cave, pit, mid, next, mouths);
        }

        internal static TransitionPit LoadPitPrefab() => AssetDatabase.LoadAssetAtPath<TransitionPit>(PitPrefabPath);

        /// <summary>La plantilla de la cueva, si todavía no existe (se puede retocar a mano).</summary>
        static RoomTemplate CreateCaveTemplate()
        {
            string path = $"{RoomsFolder}/{RoomLayouts.TransitionCave}.asset";
            var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(path);
            if (template == null)
            {
                template = ScriptableObject.CreateInstance<RoomTemplate>();
                template.SetLayout(RoomLayouts.All[RoomLayouts.TransitionCave]);
                AssetDatabase.CreateAsset(template, path);
            }

            var problems = template.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"FloorTransitionSetup: la cueva tiene problemas:\n{string.Join("\n", problems)}");
            return template;
        }

        /// <summary>Prefab de la fosa: borde sólido, dibujo que late, tecla E y burbujas (estas las crea el componente).</summary>
        static TransitionPit CreatePitPrefab(Sprite square)
        {
            Directory.CreateDirectory(PrefabsFolder);
            var art = ArtSetup.ImportTransitionPitArt();
            if (art == null)
                Debug.LogWarning("FloorTransitionSetup: no está el arte de la fosa (docs/multimedia-temp/floor-transition, "
                    + "generar_fosa.py); se verá como un cuadrado.");

            bool exists = LoadPitPrefab() != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(PitPrefabPath) : new GameObject("TransitionPit");
            try
            {
                if (!root.TryGetComponent<CapsuleCollider2D>(out var collider))
                    collider = root.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Horizontal;
                collider.size = ColliderSize;

                var pitRenderer = Child(root.transform, "Pit", Vector3.zero);
                pitRenderer.sortingOrder = PitSortingOrder;
                pitRenderer.sprite = art != null && art.Pit.Length > 0 ? art.Pit[0] : square;
                pitRenderer.color = art != null ? Color.white : new Color(0.05f, 0.12f, 0.25f);
                pitRenderer.transform.localScale = art != null ? Vector3.one : new Vector3(7f, 5f, 1f);

                var key = Child(root.transform, "Key", Vector3.zero);
                key.sortingOrder = KeySortingOrder;
                key.sprite = art != null && art.Key.Length > 0 ? art.Key[0] : null;

                if (!root.TryGetComponent<TransitionPit>(out var pit))
                    pit = root.AddComponent<TransitionPit>();
                pit.Configure(pitRenderer, art?.Pit, art?.Bubble, key, art?.Key, EdgeRadii);
                pit.SetRadius(InteractRadius);

                var meta = ArtSetup.ImportMetamorphosisArt();
                if (!root.TryGetComponent<Metamorphosis>(out var metamorphosis))
                    metamorphosis = root.AddComponent<Metamorphosis>();
                metamorphosis.Configure(meta.Background, meta.Rays, meta.Motes, art?.Bubble, meta.Aura, meta.Burst);
                if (meta.Aura.Length == 0)
                    Debug.LogWarning("FloorTransitionSetup: falta el arte de la metamorfosis (docs/multimedia-temp/metamorphosis, "
                        + "generar_metamorfosis.py).");

                PrefabUtility.SaveAsPrefabAsset(root, PitPrefabPath);
            }
            finally
            {
                if (exists)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }

            return LoadPitPrefab();
        }

        static SpriteRenderer Child(Transform parent, string name, Vector3 position)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }
            child.localPosition = position;
            return child.TryGetComponent<SpriteRenderer>(out var renderer) ? renderer : child.gameObject.AddComponent<SpriteRenderer>();
        }

        /// <summary>La sala de paso en Floor_1: el jefe deja de soltar la salida y el piso se deja por la fosa.</summary>
        static void AddToFloor(RoomTemplate cave, TransitionPit pit, RoomTheme mid, RoomTheme next, MutationData[] mouths)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            if (config == null)
            {
                Debug.LogWarning($"FloorTransitionSetup: falta {FloorSetup.FloorConfigPath}; crea antes el juego con Primordia > UI.");
                return;
            }

            config.ConfigureTransition(cave, pit, mid, next);
            config.ConfigureNextFloor(mouths, NextFloorName);
            EditorUtility.SetDirty(config);
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject player, RoomTemplate cave,
            TransitionPit pit, RoomTheme mid, RoomTheme next, MutationData[] mouths)
        {
            var config = CreateTestConfig(cave, pit, mid, next, mouths);
            if (config == null)
                return;

            // Otra escena abierta antes de borrar, por si era la abierta.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se vuelve a cargar: al abrir la escena nueva Unity descarga los assets sin usar, y la copia de antes ya no vale.
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(TestConfigPath));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UISetup.GameUIPath), scene);
            UISetup.CreateEventSystem();

            // Burbujas en el inicio para probar el reparto (Mandíbulas hasta el III y otras para recibir los niveles) y,
            // si se dejan sin coger, el aviso de la fosa de que te dejas mutaciones.
            var mutations = new[] { "Mutation_Mandibles", "Mutation_Fins", "Mutation_Carapace", "Mutation_InkSac" }
                .Select(name => AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationsFolder}/{name}.asset"))
                .Where(m => m != null)
                .ToArray();
            var bench = new GameObject("MutationBench").AddComponent<MutationBench>();
            bench.Configure(AssetDatabase.LoadAssetAtPath<MutationPickup>(PickupPath), mutations, newCopies: 3);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"FloorTransitionSetup: {TestScenePath} creada: inicio → sala con un perseguidor como jefe → sala de paso.");
        }

        /// <summary>Copia de Floor_1 con el camino inicio → "jefe" (un perseguidor) → sala de paso, sin más salas.</summary>
        static FloorConfig CreateTestConfig(RoomTemplate cave, TransitionPit pit, RoomTheme mid, RoomTheme next, MutationData[] mouths)
        {
            var floor = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var normal = AssetDatabase.LoadAssetAtPath<RoomTemplate>($"{RoomsFolder}/{RoomLayouts.Normal}.asset");
            var small = AssetDatabase.LoadAssetAtPath<RoomTemplate>($"{RoomsFolder}/{RoomLayouts.Small}.asset");
            var chaser = EnemySetup.LoadPrefab("Enemy_Chaser");
            if (floor == null || normal == null || small == null || chaser == null)
            {
                Debug.LogError($"FloorTransitionSetup: faltan {FloorSetup.FloorConfigPath}, las plantillas o Enemy_Chaser; "
                    + "crea antes el juego con Primordia > UI.");
                return null;
            }

            AssetDatabase.DeleteAsset(TestConfigPath);
            var config = Object.Instantiate(floor);
            AssetDatabase.CreateAsset(config, TestConfigPath);

            config.ConfigureRooms(small, normal, new[] { normal }, System.Array.Empty<RoomTemplate>(), Vector2Int.zero, Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureRewards(floor.RewardPool, System.Array.Empty<int>());
            config.ConfigureBosses(new[] { new FloorConfig.BossEntry(chaser, normal, floor.BossTheme, Vector2.zero) });
            config.ConfigureTransition(cave, pit, mid, next);
            config.ConfigureNextFloor(mouths, NextFloorName);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}

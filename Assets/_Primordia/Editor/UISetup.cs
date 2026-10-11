using System.Linq;
using Primordia.Core;
using Primordia.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Crea los iconos de corazón, la interfaz de partida, el menú principal y las escenas
    /// MainMenu y Game, y las pone en la lista de escenas del build.
    /// Los textos usan la fuente integrada de Unity hasta que haya una fuente pixel.
    /// </summary>
    public static class UISetup
    {
        const string ArtFolder = "Assets/_Primordia/Art/UI";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/UI";
        const string GameUIPath = PrefabsFolder + "/GameUI.prefab";
        const string MainMenuUIPath = PrefabsFolder + "/MainMenuUI.prefab";
        const string ScenesFolder = "Assets/_Primordia/Scenes";

        // La interfaz se diseña sobre la misma resolución que el juego.
        static readonly Vector2 ReferenceResolution = new(480f, 270f);

        static readonly Color OverlayColor = new(0f, 0f, 0f, 0.75f);
        static readonly Color MenuBackground = new(0.06f, 0.1f, 0.08f, 1f);
        static readonly Color ButtonColor = new(0.2f, 0.32f, 0.26f, 1f);
        static readonly Color TextColor = new(0.9f, 0.97f, 0.9f, 1f);
        static readonly Color TitleColor = new(0.55f, 0.95f, 0.6f, 1f);

        static Font UIFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem("Primordia/UI/Crear menú principal y escena de juego")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            FloorSetup.CreateAssets(square);

            CreateHeartSprites();
            CreateGameUIPrefab();
            CreateMainMenuUIPrefab();
            AssetDatabase.SaveAssets();

            string mainMenuPath = $"{ScenesFolder}/{SceneNames.MainMenu}.unity";
            string gamePath = $"{ScenesFolder}/{SceneNames.Game}.unity";
            CreateMainMenuScene(mainMenuPath, cameraRig, gameManager);
            CreateGameScene(gamePath, cameraRig, gameManager, player);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(gamePath, true)
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(mainMenuPath);
        }

        // ---------- Corazones ----------

        // Corazón de 16×16: '#' es relleno. Se dibuja de arriba abajo.
        static readonly string[] HeartShape =
        {
            "................",
            "................",
            "...###....###...",
            "..#####..#####..",
            ".##############.",
            ".##############.",
            ".##############.",
            ".##############.",
            "..############..",
            "...##########...",
            "....########....",
            ".....######.....",
            "......####......",
            ".......##.......",
            "................",
            "................"
        };

        static void CreateHeartSprites()
        {
            var red = new Color32(230, 60, 75, 255);
            var empty = new Color32(60, 40, 50, 255);
            CreateHeartSprite("Heart_Full", x => red, empty);
            CreateHeartSprite("Heart_Half", x => x < 8 ? red : empty, empty);
            CreateHeartSprite("Heart_Empty", x => empty, empty);
        }

        static Sprite LoadHeart(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

        static void CreateHeartSprite(string name, System.Func<int, Color32> fillAt, Color32 emptyColor)
        {
            const int size = 16;
            var outline = new Color32(20, 10, 15, 255);
            var pixels = new Color32[size * size];

            bool Filled(int x, int row) =>
                x >= 0 && x < size && row >= 0 && row < size && HeartShape[row][x] == '#';

            for (int row = 0; row < size; row++)
            for (int x = 0; x < size; x++)
            {
                int y = size - 1 - row; // Texture2D va de abajo arriba.
                if (!Filled(x, row))
                {
                    pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                bool edge = !Filled(x - 1, row) || !Filled(x + 1, row) || !Filled(x, row - 1) || !Filled(x, row + 1);
                pixels[y * size + x] = edge ? outline : fillAt(x);
            }

            CoreSetup.CreatePixelSprite($"{ArtFolder}/{name}.png", size, size, pixels);
        }

        // ---------- Prefabs de interfaz ----------

        static void CreateGameUIPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameUIPath) != null)
                return;

            var canvas = CreateCanvas("GameUI");

            // HUD: corazones arriba a la izquierda.
            var hearts = CreateRect("Hearts", canvas.transform);
            hearts.anchorMin = hearts.anchorMax = hearts.pivot = new Vector2(0f, 1f);
            hearts.anchoredPosition = new Vector2(8f, -8f);
            hearts.sizeDelta = new Vector2(200f, 16f);
            var layout = hearts.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var heartsDisplay = hearts.gameObject.AddComponent<HeartsDisplay>();
            var serializedHearts = new SerializedObject(heartsDisplay);
            serializedHearts.FindProperty("fullHeart").objectReferenceValue = LoadHeart("Heart_Full");
            serializedHearts.FindProperty("halfHeart").objectReferenceValue = LoadHeart("Heart_Half");
            serializedHearts.FindProperty("emptyHeart").objectReferenceValue = LoadHeart("Heart_Empty");
            serializedHearts.ApplyModifiedPropertiesWithoutUndo();

            var pause = CreatePanel(canvas.transform, "PausePanel", "PAUSA", OverlayColor,
                ("Continuar", FlowAction.Resume), ("Reiniciar", FlowAction.Restart), ("Menú principal", FlowAction.MainMenu));
            var defeat = CreatePanel(canvas.transform, "DefeatPanel", "HAS MUERTO", OverlayColor,
                ("Reintentar", FlowAction.Restart), ("Menú principal", FlowAction.MainMenu));
            var victory = CreatePanel(canvas.transform, "VictoryPanel", "¡HAS EVOLUCIONADO!", OverlayColor,
                ("Jugar otra vez", FlowAction.Restart), ("Menú principal", FlowAction.MainMenu));

            var gameUI = canvas.AddComponent<GameUI>();
            var serialized = new SerializedObject(gameUI);
            serialized.FindProperty("pausePanel").objectReferenceValue = pause;
            serialized.FindProperty("defeatPanel").objectReferenceValue = defeat;
            serialized.FindProperty("victoryPanel").objectReferenceValue = victory;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(canvas, GameUIPath);
        }

        static void CreateMainMenuUIPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuUIPath) != null)
                return;

            var canvas = CreateCanvas("MainMenuUI");
            var panel = CreatePanel(canvas.transform, "MainPanel", "PRIMORDIA", MenuBackground,
                ("Jugar", FlowAction.Play), ("Salir", FlowAction.Quit));
            panel.SetActive(true);

            var subtitle = CreateText(panel.transform, "De la sopa primordial a la cima", 12, TextColor);
            subtitle.transform.SetSiblingIndex(1);

            SavePrefab(canvas, MainMenuUIPath);
        }

        static GameObject CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            return canvasObject;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return (RectTransform)rectObject.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Panel a pantalla completa con un título y botones en columna. Empieza oculto.</summary>
        static GameObject CreatePanel(Transform parent, string name, string title, Color background,
            params (string label, FlowAction action)[] buttons)
        {
            var panel = CreateRect(name, parent);
            Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = background;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var titleText = CreateText(panel, title, 28, TitleColor);
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            foreach (var (label, action) in buttons)
                CreateButton(panel, label, action);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        static Text CreateText(Transform parent, string content, int size, Color color)
        {
            var rect = CreateRect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UIFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void CreateButton(Transform parent, string label, FlowAction action)
        {
            var rect = CreateRect($"Button_{action}", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;

            var layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 140f;
            layoutElement.preferredHeight = 22f;

            var text = CreateText(rect, label, 12, TextColor);
            Stretch((RectTransform)text.transform);

            var flowButton = rect.gameObject.AddComponent<GameFlowButton>();
            var serialized = new SerializedObject(flowButton);
            serialized.FindProperty("action").enumValueIndex = (int)action;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SavePrefab(GameObject root, string path)
        {
            System.IO.Directory.CreateDirectory(PrefabsFolder);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ---------- Escenas ----------

        static void CreateMainMenuScene(string path, GameObject cameraRig, GameObject gameManager)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(path, cameraRig, gameManager, out var scene, out _))
                return;

            // El GameManager de esta escena empieza en el menú: el jugador no puede actuar.
            var manager = Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).First();
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("initialState").enumValueIndex = (int)GameState.MainMenu;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuUIPath), scene);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene);
        }

        static void CreateGameScene(string path, GameObject cameraRig, GameObject gameManager, GameObject player)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(path, cameraRig, gameManager, out var scene, out var rig))
                return;

            FloorSetup.BuildFloor(scene, rig, player);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GameUIPath), scene);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene);
        }

        static void CreateEventSystem()
        {
            // El módulo del Input System nuevo; sin él los botones no reciben el ratón.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}

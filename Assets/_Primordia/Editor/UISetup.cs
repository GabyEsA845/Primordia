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
        internal const string GameUIPath = PrefabsFolder + "/GameUI.prefab";
        internal const string MainMenuUIPath = PrefabsFolder + "/MainMenuUI.prefab";
        const string ScenesFolder = "Assets/_Primordia/Scenes";

        // La interfaz se diseña sobre la misma resolución que el juego.
        static readonly Vector2 ReferenceResolution = new(480f, 270f);

        // Igual que los sprites: así un píxel del arte es un píxel de la interfaz (y los bordes 9-slice no se agrandan).
        internal const float ReferencePixelsPerUnit = 16f;

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
            MutationSetup.CreateAssets(square);
            CreateUIAssets();

            CreateMainMenuScene(MainMenuScenePath, cameraRig, gameManager);
            CreateGameScene(GameScenePath, cameraRig, gameManager, player);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MainMenuScenePath);
        }

        static string MainMenuScenePath => $"{ScenesFolder}/{SceneNames.MainMenu}.unity";
        static string GameScenePath => $"{ScenesFolder}/{SceneNames.Game}.unity";

        [MenuItem("Primordia/UI/Regenerar escena Game (borra la actual)")]
        static void RegenerateGameScene()
        {
            if (!EditorUtility.DisplayDialog("Regenerar escena Game",
                    "Se borrará Game.unity y se volverá a crear con el piso actual. " +
                    "Los cambios hechos a mano en esa escena se perderán.", "Regenerar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            FloorSetup.CreateAssets(square);
            MutationSetup.CreateAssets(square);
            CreateUIAssets();

            // Abrir otra escena antes de borrar, por si Game.unity era la abierta.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(GameScenePath);
            CreateGameScene(GameScenePath, cameraRig, gameManager, player);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Primordia/UI/Actualizar prefabs de interfaz")]
        static void UpdateUIPrefabs()
        {
            CreateUIAssets();
            Debug.Log("Prefabs de interfaz actualizados. Las escenas que los usan reciben los cambios solas.");
        }

        /// <summary>Iconos de corazón y prefabs de interfaz. Lo que ya existe no se toca, salvo para añadir lo que le falte.</summary>
        internal static void CreateUIAssets()
        {
            CreateHeartSprites();
            CreateGameUIPrefab();
            EnsureMutationChoice();
            EnsureMutationList();
            EnsureSeedLabel();
            EnsureBossBar();
            EnsureMutationIcons();
            EnsureHudSlotsAndCrosshair();
            CreateMainMenuUIPrefab();
            AssetDatabase.SaveAssets();
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

        internal static void CreateHeartSprites()
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
            scaler.referencePixelsPerUnit = ReferencePixelsPerUnit;

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
            var button = CreatePlainButton(parent, label, $"Button_{action}");

            var flowButton = button.gameObject.AddComponent<GameFlowButton>();
            var serialized = new SerializedObject(flowButton);
            serialized.FindProperty("action").enumValueIndex = (int)action;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Button CreatePlainButton(Transform parent, string label, string name)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 140f;
            layoutElement.preferredHeight = 22f;

            var text = CreateText(rect, label, 12, TextColor);
            Stretch((RectTransform)text.transform);
            return button;
        }

        // ---------- Elección de mutación ----------

        /// <summary>Añade la pantalla "elige 1 de 3" al prefab GameUI si todavía no la tiene.</summary>
        static void EnsureMutationChoice()
        {
            var root = PrefabUtility.LoadPrefabContents(GameUIPath);
            try
            {
                if (root.GetComponent<MutationChoiceUI>() != null)
                    return;

                AddMutationChoice(root);
                PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void AddMutationChoice(GameObject canvas)
        {
            var panel = CreateRect("MutationChoicePanel", canvas.transform);
            Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = OverlayColor;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var title = CreateText(panel, "ELIGE UNA MUTACIÓN", 20, TitleColor);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            var row = CreateRect("Cards", panel);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

            var cards = new MutationCard[3];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = CreateMutationCard(row);

            var skip = CreatePlainButton(panel, "Saltar", "Button_Skip");

            canvas.AddComponent<MutationChoiceUI>().Configure(panel.gameObject, cards, skip);
            panel.gameObject.SetActive(false);
        }

        // ---------- Barra de vida del jefe ----------

        /// <summary>Añade la barra de vida del jefe al prefab GameUI si todavía no la tiene.</summary>
        static void EnsureBossBar()
        {
            var root = PrefabUtility.LoadPrefabContents(GameUIPath);
            try
            {
                if (root.GetComponent<BossHealthBar>() != null)
                    return;

                AddBossBar(root);
                PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Nombre del jefe y barra de 220 × 10 px, arriba en el centro.</summary>
        static void AddBossBar(GameObject canvas)
        {
            var bar = CreateRect("BossBar", canvas.transform);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(0f, -6f);
            bar.sizeDelta = new Vector2(220f, 24f);

            var title = CreateText(bar, "Jefe", 10, TextColor);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 12f);
            titleRect.anchoredPosition = Vector2.zero;

            var back = CreateRect("Back", bar);
            back.anchorMin = Vector2.zero;
            back.anchorMax = new Vector2(1f, 0f);
            back.pivot = new Vector2(0.5f, 0f);
            back.sizeDelta = new Vector2(0f, 10f);
            back.anchoredPosition = Vector2.zero;
            back.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            // El relleno ocupa el fondo con 1 px de margen; la barra mueve su ancla derecha.
            var fill = CreateRect("Fill", back);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(1f, 1f);
            fill.offsetMax = new Vector2(-1f, -1f);
            var fillImage = fill.gameObject.AddComponent<Image>();

            canvas.AddComponent<BossHealthBar>().Configure(bar.gameObject, fill, fillImage, title);
            bar.gameObject.SetActive(false);
        }

        // ---------- Ranuras del cuerpo y mira ----------

        /// <summary>
        /// Añade al prefab GameUI la fila de ranuras bajo los corazones y la mira, si todavía no los tiene.
        /// Sus sprites los pone Primordia > Art > Importar objetos e interfaz.
        /// </summary>
        internal static void EnsureHudSlotsAndCrosshair()
        {
            var root = PrefabUtility.LoadPrefabContents(GameUIPath);
            try
            {
                bool changed = false;

                if (root.GetComponentInChildren<MutationSlotsDisplay>(true) == null)
                {
                    // Bajo los corazones (8, -8, 16 de alto), con 2 px de aire.
                    var slots = CreateRect("MutationSlots", root.transform);
                    slots.anchorMin = slots.anchorMax = slots.pivot = new Vector2(0f, 1f);
                    slots.anchoredPosition = new Vector2(8f, -26f);
                    slots.sizeDelta = new Vector2(200f, 20f);
                    var hearts = root.transform.Find("Hearts");
                    if (hearts != null)
                        slots.SetSiblingIndex(hearts.GetSiblingIndex() + 1);

                    var layout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 2f;
                    layout.childAlignment = TextAnchor.UpperLeft;
                    layout.childControlWidth = layout.childControlHeight = false;
                    layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                    slots.gameObject.AddComponent<MutationSlotsDisplay>();
                    changed = true;
                }

                if (root.GetComponentInChildren<Crosshair>(true) == null)
                {
                    var crosshair = CreateRect("Crosshair", root.transform);
                    crosshair.anchorMin = crosshair.anchorMax = crosshair.pivot = new Vector2(0.5f, 0.5f);
                    crosshair.sizeDelta = new Vector2(15f, 15f);
                    var image = crosshair.gameObject.AddComponent<Image>();
                    image.raycastTarget = false;
                    // La enciende el juego al empezar a jugar.
                    image.enabled = false;
                    crosshair.gameObject.AddComponent<Crosshair>();
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Lista de mutaciones en la pausa ----------

        /// <summary>Añade al panel de pausa del prefab GameUI la lista de ranuras si todavía no la tiene.</summary>
        static void EnsureMutationList()
        {
            var root = PrefabUtility.LoadPrefabContents(GameUIPath);
            try
            {
                var pausePanel = root.transform.Find("PausePanel");
                if (pausePanel == null || pausePanel.GetComponentInChildren<MutationListUI>(true) != null)
                    return;

                AddMutationList(pausePanel);
                PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Añade al panel de pausa la semilla del piso, abajo a la izquierda, si todavía no la tiene.</summary>
        static void EnsureSeedLabel()
        {
            var root = PrefabUtility.LoadPrefabContents(GameUIPath);
            try
            {
                var pausePanel = root.transform.Find("PausePanel");
                if (pausePanel == null || pausePanel.GetComponentInChildren<SeedLabel>(true) != null)
                    return;

                var label = CreateText(pausePanel, "", 9, new Color(0.6f, 0.7f, 0.65f));
                label.name = "Seed";
                label.alignment = TextAnchor.LowerLeft;
                var rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(8f, 8f);
                rect.sizeDelta = new Vector2(200f, 14f);
                // Fuera de la columna centrada de botones del panel.
                label.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                label.gameObject.AddComponent<SeedLabel>();

                PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Recuadro a la derecha del panel de pausa con las 4 ranuras.</summary>
        static void AddMutationList(Transform pausePanel)
        {
            var list = CreateRect("MutationList", pausePanel);
            list.anchorMin = list.anchorMax = list.pivot = new Vector2(1f, 0.5f);
            list.anchoredPosition = new Vector2(-12f, 0f);
            list.sizeDelta = new Vector2(150f, 0f);

            // Fuera de la columna centrada de botones del panel.
            list.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            list.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.1f, 0.08f, 0.95f);

            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = CreateText(list, "MUTACIONES", 12, TitleColor);
            title.alignment = TextAnchor.MiddleLeft;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            string[] slotNames = { "Boca", "Extremidades", "Piel", "Órgano especial" };
            var names = new Text[slotNames.Length];
            var descriptions = new Text[slotNames.Length];

            for (int i = 0; i < slotNames.Length; i++)
            {
                var slotLabel = CreateText(list, slotNames[i].ToUpperInvariant(), 8, new Color(0.6f, 0.7f, 0.65f));
                slotLabel.alignment = TextAnchor.MiddleLeft;
                // Un poco de aire antes de cada ranura, salvo la primera.
                slotLabel.gameObject.AddComponent<LayoutElement>().minHeight = i == 0 ? 10f : 16f;

                names[i] = CreateText(list, "— vacía —", 11, TextColor);
                names[i].alignment = TextAnchor.MiddleLeft;

                descriptions[i] = CreateText(list, "", 8, TextColor);
                descriptions[i].alignment = TextAnchor.UpperLeft;
                descriptions[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var listUI = list.gameObject.AddComponent<MutationListUI>();
            listUI.Configure(names, descriptions);
            AddListIcons(listUI);
        }

        static MutationCard CreateMutationCard(Transform parent)
        {
            var card = CreateRect("Card", parent);
            var image = card.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            card.gameObject.AddComponent<Button>().targetGraphic = image;

            var element = card.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 130f;
            element.preferredHeight = 150f;

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var strip = CreateRect("ColorStrip", card).gameObject.AddComponent<Image>();
            strip.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

            var title = CreateText(card, "Mutación", 14, TitleColor);
            var details = CreateText(card, "Ranura · Linaje", 9, TextColor);
            var description = CreateText(card, "Descripción", 9, TextColor);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.alignment = TextAnchor.UpperCenter;
            description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var replaces = CreateText(card, "Ranura libre", 9, new Color(1f, 0.8f, 0.5f));

            var mutationCard = card.gameObject.AddComponent<MutationCard>();
            mutationCard.Configure(strip, title, details, description, replaces);
            AddCardIcons(mutationCard);
            return mutationCard;
        }

        // ---------- Iconos de mutación ----------

        /// <summary>Añade los huecos de los iconos a las tarjetas y a la lista de la pausa del prefab GameUI si no los tienen.</summary>
        internal static void EnsureMutationIcons()
        {
            var root = PrefabUtility.LoadPrefabContents(GameUIPath);
            try
            {
                bool changed = false;
                foreach (var card in root.GetComponentsInChildren<MutationCard>(true))
                    changed |= AddCardIcons(card);
                foreach (var list in root.GetComponentsInChildren<MutationListUI>(true))
                    changed |= AddListIcons(list);

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Icono de 32×32 bajo la franja de color, y el icono de linaje a la izquierda de "Ranura · Linaje".
        /// Devuelve false si la tarjeta ya los tenía.
        /// </summary>
        static bool AddCardIcons(MutationCard card)
        {
            if (card.transform.Find("Icon") != null)
                return false;

            var icon = CreateIcon("Icon", card.transform, 32f);
            icon.preserveAspect = true;
            icon.transform.SetSiblingIndex(1);

            var details = card.Details;
            var row = CreateRow("Info", card.transform, details.transform.GetSiblingIndex(), TextAnchor.MiddleCenter);
            var lineageIcon = CreateIcon("LineageIcon", row, 12f);
            details.transform.SetParent(row, false);

            card.ConfigureIcons(icon, lineageIcon);
            return true;
        }

        /// <summary>Icono de 16×16 a la izquierda del nombre de cada ranura. Devuelve false si ya los tenía.</summary>
        static bool AddListIcons(MutationListUI list)
        {
            var names = list.NameTexts;
            if (names.Length == 0 || names[0] == null || names[0].transform.parent != list.transform)
                return false;

            var icons = new Image[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var row = CreateRow($"Slot{i}", list.transform, names[i].transform.GetSiblingIndex(), TextAnchor.MiddleLeft);
                icons[i] = CreateIcon("Icon", row, 16f);
                names[i].transform.SetParent(row, false);
                names[i].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }

            list.ConfigureIcons(icons);
            return true;
        }

        static RectTransform CreateRow(string name, Transform parent, int siblingIndex, TextAnchor alignment)
        {
            var row = CreateRect(name, parent);
            row.SetSiblingIndex(siblingIndex);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3f;
            layout.childAlignment = alignment;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }

        /// <summary>Imagen cuadrada de ese tamaño en píxeles de la interfaz, vacía hasta que se le pone un sprite.</summary>
        static Image CreateIcon(string name, Transform parent, float size)
        {
            var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.enabled = false;
            image.raycastTarget = false;
            var element = image.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = size;
            element.preferredHeight = element.minHeight = size;
            return image;
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
            var manager = Object.FindObjectsByType<GameManager>().First();
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

            FloorSetup.BuildFloor(scene, rig, player, MutationSetup.LoadPool());
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

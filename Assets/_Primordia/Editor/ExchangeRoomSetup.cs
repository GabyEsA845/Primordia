using System.IO;
using System.Linq;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Sala de cambio (el laboratorio de Darwin): importa el arte de la cápsula (docs/multimedia-temp/exchange-room), crea su
    /// prefab y lo pone en Floor_2, y monta la ventana <see cref="ExchangeUI"/> en el prefab GameUI clonando piezas que ya
    /// tienen su arte (el retrato de la criatura de la pausa, las tarjetas de las recompensas, un botón y la cinta del
    /// título). Rehace la cápsula y la ventana cada vez.
    /// </summary>
    public static class ExchangeRoomSetup
    {
        const string SourceFolder = "docs/multimedia-temp/exchange-room";
        const string ArtFolder = "Assets/_Primordia/Art/Environment/Exchange";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Rooms";
        internal const string CapsulePrefabPath = PrefabsFolder + "/ExchangeCapsule.prefab";
        const string PanelName = "ExchangePanel";

        // La cápsula mide 40×48 px con el apoyo abajo en el centro; su bloque del plano es de 2×2 tiles.
        static readonly Vector2 BottomCenter = new(0.5f, 0f);
        const int CapsuleCell = 40;
        const int CapsuleOrder = -4;

        // Medidas de la ventana, en píxeles de la interfaz (480×270).
        const int CardCount = 2;
        static readonly Color OverlayColor = new(0.01f, 0.05f, 0.06f, 0.82f);
        static readonly Color NoteColor = new(1f, 0.8f, 0.5f);
        static readonly Color MarkColor = new(1f, 0.85f, 0.3f);

        [MenuItem("Primordia/Rooms/Crear sala de cambio")]
        static void CreateAll()
        {
            var capsule = CreateCapsule();
            if (capsule == null)
                return;
            AssetDatabase.SaveAssets();

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            if (config != null)
            {
                config.SetExchangeCapsule(capsule);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning("ExchangeRoomSetup: no existe Floor_2; créalo con Primordia > Rooms > Crear piso 2 y escena de prueba.");
            }

            BuildWindow();
            Debug.Log("ExchangeRoomSetup: cápsula del laboratorio de Darwin en Floor_2 y su ventana en GameUI. F7 lleva a la sala de cambio.");
        }

        // ---------- Cápsula ----------

        static ExchangeCapsule CreateCapsule()
        {
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(PrefabsFolder);
            var idle = Sheet("Capsule_Idle");
            var used = Sheet("Capsule_Used");
            if (idle.Length == 0)
            {
                Debug.LogWarning($"ExchangeRoomSetup: falta el arte de la cápsula en {SourceFolder} (python generar_sala_cambio.py).");
                return null;
            }

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            var keyPrompt = config != null ? config.Pieces.keyPrompt : null;

            var root = new GameObject("ExchangeCapsule");
            try
            {
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                // El apoyo del dibujo va en el borde de abajo del bloque de 2×2; el vidrio sube por encima.
                body.transform.localPosition = new Vector3(0f, -1f, 0f);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = idle[0];
                renderer.sortingOrder = CapsuleOrder;

                root.AddComponent<BoxCollider2D>().size = new Vector2(2f, 2f);
                var capsule = root.AddComponent<ExchangeCapsule>();
                capsule.Configure(renderer, idle, used);
                capsule.ConfigurePrompt(keyPrompt, 2.4f);
                capsule.SetRadius(2.2f);
                return PrefabUtility.SaveAsPrefabAsset(root, CapsulePrefabPath).GetComponent<ExchangeCapsule>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Sprite[] Sheet(string name) =>
            ArtSetup.ImportLooseSheet($"{SourceFolder}/{name}.png", $"{ArtFolder}/{name}.png", CapsuleCell, BottomCenter);

        // ---------- Ventana ----------

        static void BuildWindow()
        {
            var root = PrefabUtility.LoadPrefabContents(UISetup.GameUIPath);
            try
            {
                var old = root.transform.Find(PanelName);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                var oldWindow = root.GetComponent<ExchangeUI>();
                if (oldWindow != null)
                    Object.DestroyImmediate(oldWindow);

                var pause = root.transform.Find("PausePanel");
                var zone = pause != null ? pause.Find("Creature") : null;
                var choice = root.transform.Find("MutationChoicePanel");
                var cardTemplate = choice != null ? choice.GetComponentInChildren<MutationCard>(true) : null;
                var buttonTemplate = pause != null ? pause.GetComponentsInChildren<Button>(true).FirstOrDefault() : null;
                var banner = pause != null ? pause.Find("TitleBanner") : null;
                if (zone == null || cardTemplate == null || buttonTemplate == null)
                {
                    Debug.LogWarning("ExchangeRoomSetup: a GameUI le falta la pausa con la criatura o las tarjetas; usa antes Primordia > UI y Primordia > Art > Importar menús.");
                    return;
                }

                var panel = UISetup.CreateRect(PanelName, root.transform);
                UISetup.Stretch(panel);
                panel.SetAsLastSibling();
                panel.gameObject.AddComponent<Image>().color = OverlayColor;

                // Título y subtítulo arriba.
                if (banner != null)
                {
                    var title = Object.Instantiate(banner.gameObject, panel).GetComponent<RectTransform>();
                    title.name = "TitleBanner";
                    Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(170f, 16f));
                    foreach (var text in title.GetComponentsInChildren<Text>(true))
                        text.text = "LABORATORIO DE DARWIN";
                }
                else
                {
                    var title = UISetup.CreateText(panel, "LABORATORIO DE DARWIN", 14, UISetup.TitleColor);
                    Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(240f, 16f));
                }
                var subtitle = UISetup.CreateText(panel, "Un cambio por piso, gratis. Elige la ranura que entregas.", 8, UISetup.TextColor);
                Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(440f, 12f));

                // La criatura con sus ranuras, a la izquierda: un clon del de la pausa con un botón en cada ranura.
                var creature = Object.Instantiate(zone.gameObject, panel).GetComponent<RectTransform>();
                creature.name = "Creature";
                Place(creature, new Vector2(0f, 0.5f), new Vector2(14f, -6f), creature.sizeDelta);
                var callouts = creature.GetComponentsInChildren<SlotCallout>(true);
                var slotButtons = new Button[4];
                var slotMarks = new Image[4];
                foreach (var callout in callouts)
                {
                    int index = (int)callout.Slot;
                    var frame = callout.GetComponent<Image>();
                    var button = callout.gameObject.AddComponent<Button>();
                    button.targetGraphic = frame;
                    slotButtons[index] = button;

                    var mark = UISetup.CreateRect("ExchangeMark", callout.transform).gameObject.AddComponent<Image>();
                    mark.transform.SetAsFirstSibling();
                    var highlighted = new SerializedObject(callout).FindProperty("highlightedFrame").objectReferenceValue as Sprite;
                    mark.sprite = highlighted != null ? highlighted : frame != null ? frame.sprite : null;
                    mark.color = MarkColor;
                    mark.raycastTarget = false;
                    mark.rectTransform.anchorMin = Vector2.zero;
                    mark.rectTransform.anchorMax = Vector2.one;
                    mark.rectTransform.offsetMin = new Vector2(-3f, -3f);
                    mark.rectTransform.offsetMax = new Vector2(3f, 3f);
                    mark.enabled = false;
                    slotMarks[index] = mark;
                }

                // Las opciones, a la derecha: lo que entregas, las tarjetas y la nota.
                var offers = UISetup.CreateRect("Offers", panel);
                Place(offers, new Vector2(1f, 0.5f), new Vector2(-14f, 2f), new Vector2(272f, 196f));
                var layout = offers.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 4f;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;

                var heading = UISetup.CreateText(offers, "Entregas:", 10, UISetup.TitleColor);
                heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

                var row = UISetup.CreateRect("Cards", offers);
                var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.spacing = 8f;
                rowLayout.childControlWidth = rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
                var cards = new MutationCard[CardCount];
                for (int i = 0; i < CardCount; i++)
                {
                    var card = Object.Instantiate(cardTemplate.gameObject, row);
                    card.name = $"Card_{i}";
                    card.SetActive(true);
                    cards[i] = card.GetComponent<MutationCard>();
                }

                var note = UISetup.CreateText(offers, "", 8, NoteColor);
                note.name = "Note";
                note.horizontalOverflow = HorizontalWrapMode.Wrap;
                note.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

                // Aceptar y Salir, abajo.
                var buttons = UISetup.CreateRect("Buttons", panel);
                Place(buttons, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(300f, 22f));
                var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
                buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
                buttonsLayout.spacing = 12f;
                buttonsLayout.childControlWidth = buttonsLayout.childControlHeight = true;
                buttonsLayout.childForceExpandWidth = buttonsLayout.childForceExpandHeight = false;
                var accept = CloneButton(buttonTemplate, buttons, "Accept", "Aceptar");
                var exit = CloneButton(buttonTemplate, buttons, "Exit", "Salir");

                var window = root.AddComponent<ExchangeUI>();
                window.Configure(panel.gameObject, creature.GetComponent<CreaturePanel>(), slotButtons, slotMarks, heading, note, cards, accept, exit);
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, UISetup.GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Un botón como los de la pausa, sin su acción (la pone la ventana) y con otro texto.</summary>
        static Button CloneButton(Button template, Transform parent, string name, string label)
        {
            var clone = Object.Instantiate(template.gameObject, parent);
            clone.name = name;
            clone.SetActive(true);
            var flow = clone.GetComponent<GameFlowButton>();
            if (flow != null)
                Object.DestroyImmediate(flow);
            var button = clone.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            foreach (var text in clone.GetComponentsInChildren<Text>(true))
                text.text = label;
            return button;
        }

        /// <summary>Ancla el rectángulo en ese punto del panel (0–1), con su pivote ahí mismo.</summary>
        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}

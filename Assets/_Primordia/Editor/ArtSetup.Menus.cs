using System.Linq;
using Primordia.Mutations;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Menú de pausa y menú principal (docs/multimedia-temp/menus/), y la fuente pixel de toda la interfaz.
    /// Pausa: estadísticas a la izquierda, título en su cinta y botones en el centro, y a la derecha la criatura
    /// en una lente con líneas hacia los recuadros de sus 4 ranuras y un cuadro de información al pasar el ratón.
    /// Menú principal: fondo de caldo primordial en 4 capas, logo con brillo, la célula y un marcador animado.
    /// Lo que crea se rehace entero cada vez; el resto de los prefabs no se toca.
    /// </summary>
    public static partial class ArtSetup
    {
        const string MenusSourceFolder = "docs/multimedia-temp/menus";
        const string MenusArtFolder = "Assets/_Primordia/Art/UI/Menus";
        const string PlayerIdleDownPath = "Assets/_Primordia/Art/Characters/Player/Player_Idle_down.png";

        // Medidas en píxeles de la interfaz (lienzo de 480×270).
        static readonly Vector2 MenuButtonSize = new(96f, 16f);
        const float StatsWidth = 120f;
        const float TooltipWidth = 140f;
        static readonly Vector2 CreatureZoneSize = new(170f, 164f);
        // Centros de los recuadros respecto al centro de la lente: Boca y Piel arriba, Extremidades y Órgano abajo.
        static readonly (MutationSlot slot, Vector2 position)[] CalloutPositions =
        {
            (MutationSlot.Mouth, new Vector2(-75f, 72f)), (MutationSlot.Skin, new Vector2(75f, 72f)),
            (MutationSlot.Limbs, new Vector2(-75f, -72f)), (MutationSlot.Organ, new Vector2(75f, -72f)),
        };

        [MenuItem("Primordia/Art/Importar menús")]
        static void ImportMenus()
        {
            ImportFonts();
            RemoveOldMutationList();
            BuildPauseMenu();
            BuildMainMenu();
            ApplyPixelFont(UISetup.GameUIPath);
            ApplyPixelFont(UISetup.MainMenuUIPath);
            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: menús importados.");
        }

        // ---------- Fuente ----------

        /// <summary>Sin suavizado: cada píxel de la letra cae en un píxel de pantalla a tamaño 8 y sus múltiplos.</summary>
        static void ImportFonts()
        {
            foreach (var path in new[] { UISetup.FontPath, UISetup.BoldFontPath })
            {
                if (AssetImporter.GetAtPath(path) is not TrueTypeFontImporter importer)
                {
                    Debug.LogWarning($"ArtSetup: falta la fuente {path}.");
                    continue;
                }

                importer.fontRenderingMode = FontRenderingMode.HintedRaster;
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Pone la fuente pixel a todos los textos del prefab. Los tamaños que no son múltiplo de 8 se ajustan:
        /// títulos grandes a 16, nombres de las tarjetas a 8 en negrita y el resto a 8. Los botones, a 96×16.
        /// </summary>
        static void ApplyPixelFont(string prefabPath)
        {
            var regular = AssetDatabase.LoadAssetAtPath<Font>(UISetup.FontPath);
            var bold = AssetDatabase.LoadAssetAtPath<Font>(UISetup.BoldFontPath);
            if (regular == null)
                return;

            EditPrefab(prefabPath, root =>
            {
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                {
                    if (text.font != bold || text.fontSize % 8 != 0)
                        text.font = regular;

                    if (text.fontSize % 8 != 0)
                    {
                        if (text.fontSize >= 20)
                            text.fontSize = 16;
                        else
                        {
                            if (text.fontSize >= 13 && bold != null)
                                text.font = bold;
                            text.fontSize = 8;
                        }
                    }

                    text.fontStyle = FontStyle.Normal;
                    text.lineSpacing = 1f;
                    if (text.GetComponentInParent<MutationCard>(true) != null)
                        text.horizontalOverflow = HorizontalWrapMode.Wrap;
                }

                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.GetComponent<MutationCard>() != null)
                        continue;
                    if (button.TryGetComponent<LayoutElement>(out var element))
                    {
                        element.preferredWidth = MenuButtonSize.x;
                        element.preferredHeight = MenuButtonSize.y;
                    }
                    ((RectTransform)button.transform).sizeDelta = MenuButtonSize;
                }
            });
        }

        // ---------- Pausa ----------

        /// <summary>
        /// Quita la lista de mutaciones que tenía antes la pausa (su script ya no existe) y cualquier componente
        /// que haya perdido su script: Unity no deja guardar un prefab con scripts que faltan.
        /// </summary>
        static void RemoveOldMutationList()
        {
            var root = PrefabUtility.LoadPrefabContents(UISetup.GameUIPath);
            try
            {
                var oldList = root.transform.Find("PausePanel/MutationList");
                if (oldList != null)
                    Object.DestroyImmediate(oldList.gameObject);

                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, UISetup.GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void BuildPauseMenu()
        {
            var title = MenuSprite("Pause", "UI_Pause_Title", new Vector4(8, 0, 8, 0));
            var statsPanel = MenuSprite("Pause", "UI_Stats_Panel", 10);
            var statsRow = MenuSprite("Pause", "UI_Stats_Row", new Vector4(4, 0, 4, 0));
            var statIcons = new[] { MenuSprite("Pause", "UI_Stat_Health"), MenuSprite("Pause", "UI_Stat_Damage"), MenuSprite("Pause", "UI_Stat_Speed") };
            var up = MenuSprite("Pause", "UI_Stat_Up");
            var down = MenuSprite("Pause", "UI_Stat_Down");

            var lens = MenuSprite("Pause", "UI_Pause_Lens");
            var glint = MenuSprite("Pause", "UI_Pause_Lens_Glint");
            var bubbles = MenuSheet("Pause", "UI_Pause_LensBubbles", 112);
            var line = MenuSprite("Pause", "UI_Callout_Line", 1);
            var dot = MenuSheet("Pause", "UI_Callout_Dot", 5);
            var highlighted = MenuSprite("Pause", "UI_Slot_Frame_Highlighted");
            var tooltip = MenuSprite("Pause", "UI_Tooltip", 6);
            var pointer = MenuSprite("Pause", "UI_Tooltip_Pointer");

            // Del HUD y de las tarjetas (Primordia > Art > Importar objetos e interfaz).
            var slotFrame = ImportUISprite("HUD", "UI_Slot_Frame");
            var stripe = ImportUISprite("Panels", "UI_Card_Stripe", new Vector4(4, 0, 4, 0));
            var slotIcons = System.Array.ConvertAll(SlotNames, slot => ImportUISprite("Icons", $"Icon_Slot_{slot}"));
            var emptyIcons = new[] { "Mouth", "Limbs", "Skin", "Organ" }.ToDictionary(
                slot => System.Enum.Parse<MutationSlot>(slot), slot => ImportUISprite("HUD", $"UI_Slot_{slot}_Empty"));

            EditPrefab(UISetup.GameUIPath, root =>
            {
                var pause = root.transform.Find("PausePanel");
                if (pause == null)
                {
                    Debug.LogWarning("ArtSetup: GameUI no tiene PausePanel; créalo con Primordia > UI.");
                    return;
                }

                // Título arriba y, tras un hueco, la columna de botones.
                var layout = pause.GetComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.padding = new RectOffset(0, 0, 24, 0);
                layout.spacing = 6f;

                var banner = TitleBanner(pause, "PAUSA", title);
                var spacer = Recreate("Spacer", pause);
                spacer.SetSiblingIndex(banner.GetSiblingIndex() + 1);
                spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

                BuildStats(pause, statsPanel, statsRow, statIcons, up, down);

                var zone = Recreate("Creature", pause);
                zone.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                zone.anchorMin = zone.anchorMax = zone.pivot = new Vector2(1f, 0.5f);
                zone.anchoredPosition = new Vector2(-12f, -8f);
                zone.sizeDelta = CreatureZoneSize;

                // De atrás adelante: lente, burbujas, líneas, criatura, reflejo, puntos, recuadros y cuadro de información.
                CenteredImage("Lens", zone, lens);
                var bubbleImage = CenteredImage("LensBubbles", zone, bubbles.FirstOrDefault());
                bubbleImage.gameObject.AddComponent<UISpriteAnimation>().Configure(bubbles, 0.16f, newLoop: true);
                var lines = CenteredRect("Lines", zone);
                var portrait = CenteredRect("Portrait", zone).gameObject.AddComponent<CreaturePortrait>();
                CenteredImage("Glint", zone, glint);
                var dots = CenteredRect("Dots", zone);

                var callouts = CalloutPositions.Select(c => Callout(zone, c.slot, c.position, slotFrame, highlighted, emptyIcons[c.slot])).ToArray();
                var info = Tooltip(zone, tooltip, pointer, stripe, slotIcons);

                var panel = zone.gameObject.AddComponent<CreaturePanel>();
                panel.Configure(portrait, callouts, info, lines, dots);
                panel.ConfigureLook(line, dot, 0.18f);
            });
        }

        /// <summary>Mete el título del panel (el texto con ese contenido) en su cinta 9-slice.</summary>
        static Transform TitleBanner(Transform panel, string titleText, Sprite sprite)
        {
            var banner = panel.Find("TitleBanner");
            if (banner == null)
            {
                var title = panel.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.text == titleText && t.transform.parent == panel);
                banner = UISetup.CreateRect("TitleBanner", panel);
                banner.SetSiblingIndex(title != null ? title.transform.GetSiblingIndex() : 0);
                banner.gameObject.AddComponent<Image>();
                var element = banner.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 96f;
                element.preferredHeight = 16f;

                if (title != null)
                {
                    title.transform.SetParent(banner, false);
                    UISetup.Stretch(title.rectTransform);
                    title.raycastTarget = false;
                }
            }

            var image = banner.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return banner;
        }

        /// <summary>Panel de estadísticas a la izquierda: título y una fila por estadística (icono, nombre, valor y flecha).</summary>
        static void BuildStats(Transform pause, Sprite panelSprite, Sprite rowSprite, Sprite[] icons, Sprite up, Sprite down)
        {
            string[] labels = { "Vida", "Daño", "Velocidad" };

            var stats = Recreate("Stats", pause);
            stats.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(0f, 1f);
            stats.anchoredPosition = new Vector2(16f, -64f);
            stats.sizeDelta = new Vector2(StatsWidth, 0f);
            SlicedImage(stats.gameObject, panelSprite);

            var layout = stats.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 10);
            layout.spacing = 2f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            stats.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var heading = Label(stats, "ESTADÍSTICAS", UISetup.TitleColor, TextAnchor.MiddleLeft);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            var values = new Text[labels.Length];
            var arrows = new Image[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var row = UISetup.CreateRow($"Row_{labels[i]}", stats, stats.childCount, TextAnchor.MiddleLeft);
                row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(3, 3, 1, 1);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
                // Filas alternas con veta de fondo.
                if (i % 2 == 0)
                    SlicedImage(row.gameObject, rowSprite);

                var icon = UISetup.CreateIcon("Icon", row, 12f);
                icon.sprite = i < icons.Length ? icons[i] : null;
                icon.enabled = icon.sprite != null;

                var label = Label(row, labels[i], UISetup.TextColor, TextAnchor.MiddleLeft);
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                values[i] = Label(row, "-", UISetup.TextColor, TextAnchor.MiddleRight);
                arrows[i] = UISetup.CreateIcon("Arrow", row, 7f);
            }

            stats.gameObject.AddComponent<PlayerStatsPanel>().Configure(values, arrows, up, down);
        }

        static SlotCallout Callout(RectTransform zone, MutationSlot slot, Vector2 position, Sprite frame, Sprite highlighted, Sprite empty)
        {
            var box = CenteredRect($"Slot_{slot}", zone);
            box.anchoredPosition = position;
            box.sizeDelta = new Vector2(20f, 20f);
            var frameImage = box.gameObject.AddComponent<Image>();
            frameImage.sprite = frame;
            // Recibe el ratón para el cuadro de información.
            frameImage.raycastTarget = true;

            var icon = CenteredImage("Icon", box, null);
            icon.rectTransform.sizeDelta = new Vector2(16f, 16f);

            var callout = box.gameObject.AddComponent<SlotCallout>();
            callout.Configure(slot, frameImage, icon);
            callout.ConfigureLook(frame, highlighted, empty);
            return callout;
        }

        static MutationTooltip Tooltip(RectTransform zone, Sprite background, Sprite pointerSprite, Sprite stripeSprite, Sprite[] slotIcons)
        {
            var box = CenteredRect("Tooltip", zone);
            box.sizeDelta = new Vector2(TooltipWidth, 0f);
            SlicedImage(box.gameObject, background).raycastTarget = false;

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 7);
            layout.spacing = 3f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var stripe = SlicedImage(UISetup.CreateRect("Stripe", box).gameObject, stripeSprite);
            stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = stripeSprite != null ? stripeSprite.rect.height : 4f;

            var title = Label(box, "Mutación", UISetup.TitleColor, TextAnchor.MiddleLeft);
            var bold = AssetDatabase.LoadAssetAtPath<Font>(UISetup.BoldFontPath);
            if (bold != null)
                title.font = bold;

            var row = UISetup.CreateRow("Info", box, box.childCount, TextAnchor.UpperLeft);
            var slotIcon = UISetup.CreateIcon("SlotIcon", row, 12f);
            var details = Label(row, "Ranura · Linaje", UISetup.TextColor, TextAnchor.UpperLeft);
            details.horizontalOverflow = HorizontalWrapMode.Wrap;
            details.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var description = Label(box, "Descripción", UISetup.TextColor, TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;

            var pointer = UISetup.CreateRect("Pointer", box);
            pointer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var pointerImage = pointer.gameObject.AddComponent<Image>();
            pointerImage.sprite = pointerSprite;
            pointerImage.raycastTarget = false;
            pointer.sizeDelta = pointerSprite != null ? pointerSprite.rect.size : new Vector2(6f, 7f);

            var tooltip = box.gameObject.AddComponent<MutationTooltip>();
            tooltip.Configure(stripe, title, slotIcon, details, description, pointer);
            tooltip.SetSlotIcons(slotIcons);
            box.gameObject.SetActive(false);
            return tooltip;
        }

        // ---------- Menú principal ----------

        static void BuildMainMenu()
        {
            var far = MenuSprite("Main", "Menu_Bg_Far");
            var rays = MenuSheet("Main", "Menu_Bg_Rays", 480);
            var particles = MenuTexture("Main", "Menu_Bg_Particles");
            var near = MenuTexture("Main", "Menu_Bg_Near");
            var logo = MenuSprite("Main", "Menu_Logo");
            var shine = MenuSheet("Main", "Menu_Logo_Shine", 200);
            var selector = MenuSheet("Main", "Menu_Selector", 9);
            var bubble = MenuSheet("Main", "Menu_Bubble", 8);
            var cellFrames = AssetDatabase.LoadAllAssetsAtPath(PlayerIdleDownPath).OfType<Sprite>()
                .OrderBy(s => s.rect.x).ToArray();

            EditPrefab(UISetup.MainMenuUIPath, root =>
            {
                var panel = root.transform.Find("MainPanel");
                if (panel == null)
                {
                    Debug.LogWarning("ArtSetup: MainMenuUI no tiene MainPanel; créalo con Primordia > UI.");
                    return;
                }

                // Fondo en capas, detrás de todo: agua, rayos, partículas que suben y siluetas que pasan.
                var background = Recreate("Background", root.transform);
                background.SetAsFirstSibling();
                UISetup.Stretch(background);
                StretchedImage("Far", background, far);
                var raysImage = StretchedImage("Rays", background, rays.FirstOrDefault());
                raysImage.gameObject.AddComponent<UISpriteAnimation>().Configure(rays, 0.18f, newLoop: true);
                ScrollingImage("Particles", background, particles, new Vector2(0f, 6f));
                ScrollingImage("Near", background, near, new Vector2(-10f, 0f));

                // El panel ya no pinta fondo ni ordena a sus hijos: cada cosa va en su sitio.
                if (panel.TryGetComponent<Image>(out var panelImage))
                    panelImage.enabled = false;
                if (panel.TryGetComponent<VerticalLayoutGroup>(out var layout))
                    Object.DestroyImmediate(layout);

                foreach (var text in panel.GetComponentsInChildren<Text>(true).Where(t => t.transform.parent == panel))
                {
                    if (text.text == "PRIMORDIA")
                        text.gameObject.SetActive(false);
                    else if (text.text.StartsWith("De la sopa"))
                        PlaceTop(text.rectTransform, new Vector2(0f, -64f), new Vector2(240f, 10f));
                }

                var logoImage = TopImage("Logo", panel, logo, new Vector2(0f, -14f));
                var shineImage = StretchedImage("Shine", logoImage.transform, shine.FirstOrDefault());
                shineImage.gameObject.AddComponent<UISpriteAnimation>().Configure(shine, 0.05f, newLoop: true, newLoopDelay: 3f);

                var cell = TopImage("Cell", panel, cellFrames.FirstOrDefault(), new Vector2(0f, -78f), scale: 3);
                cell.gameObject.AddComponent<UISpriteAnimation>().Configure(cellFrames, 1f / 6f, newLoop: true);

                var buttons = panel.GetComponentsInChildren<Button>(true).Where(b => b.transform.parent == panel).ToArray();
                for (int i = 0; i < buttons.Length; i++)
                    PlaceTop((RectTransform)buttons[i].transform, new Vector2(0f, -156f - 22f * i), MenuButtonSize);

                var marker = TopImage("Selector", panel, selector.FirstOrDefault(), Vector2.zero);
                marker.gameObject.AddComponent<UISpriteAnimation>().Configure(selector, 0.15f, newLoop: true);
                marker.gameObject.AddComponent<MenuSelector>().Configure(buttons, bubble, 0.09f);

                var version = Recreate("Version", panel);
                version.anchorMin = version.anchorMax = version.pivot = new Vector2(1f, 0f);
                version.anchoredPosition = new Vector2(-4f, 3f);
                version.sizeDelta = new Vector2(80f, 10f);
                var versionText = version.gameObject.AddComponent<Text>();
                versionText.font = UISetup.UIFont;
                versionText.fontSize = 8;
                versionText.color = new Color(0.6f, 0.75f, 0.7f);
                versionText.alignment = TextAnchor.LowerRight;
                versionText.raycastTarget = false;
                version.gameObject.AddComponent<VersionLabel>();
            });
        }

        // ---------- Piezas ----------

        /// <summary>Borra el hijo con ese nombre (si lo hay) y crea uno vacío en su lugar.</summary>
        static RectTransform Recreate(string name, Transform parent)
        {
            var old = parent.Find(name);
            int index = old != null ? old.GetSiblingIndex() : parent.childCount;
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var rect = UISetup.CreateRect(name, parent);
            rect.SetSiblingIndex(Mathf.Min(index, parent.childCount - 1));
            return rect;
        }

        static RectTransform CenteredRect(string name, Transform parent)
        {
            var rect = Recreate(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        static Image CenteredImage(string name, Transform parent, Sprite sprite)
        {
            var image = CenteredRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            if (sprite != null)
                image.rectTransform.sizeDelta = sprite.rect.size;
            return image;
        }

        static Image StretchedImage(string name, Transform parent, Sprite sprite)
        {
            var rect = Recreate(name, parent);
            UISetup.Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        static void ScrollingImage(string name, Transform parent, Texture2D texture, Vector2 velocity)
        {
            var rect = Recreate(name, parent);
            UISetup.Stretch(rect);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.enabled = texture != null;
            rect.gameObject.AddComponent<ScrollingLayer>().Configure(velocity);
        }

        /// <summary>Imagen anclada arriba en el centro, a escala entera del tamaño de su sprite.</summary>
        static Image TopImage(string name, Transform parent, Sprite sprite, Vector2 position, int scale = 1)
        {
            var rect = Recreate(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            PlaceTop(rect, position, sprite != null ? sprite.rect.size * scale : Vector2.one * 16f);
            return image;
        }

        static void PlaceTop(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static Image SlicedImage(GameObject target, Sprite sprite)
        {
            var image = GetOrAdd<Image>(target);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.raycastTarget = false;
            if (sprite == null)
                image.color = new Color(0.06f, 0.1f, 0.08f, 0.9f);
            return image;
        }

        static Text Label(Transform parent, string content, Color color, TextAnchor alignment)
        {
            var text = UISetup.CreateText(parent, content, 8, color);
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        static Sprite MenuSprite(string folder, string file, float border) =>
            MenuSprite(folder, file, new Vector4(border, border, border, border));

        static Sprite MenuSprite(string folder, string file) => MenuSprite(folder, file, Vector4.zero);

        static Sprite MenuSprite(string folder, string file, Vector4 border) =>
            ImportSprite($"{MenusSourceFolder}/{folder}/{file}.png", $"{MenusArtFolder}/{file}.png", border);

        static Sprite[] MenuSheet(string folder, string file, int cellWidth)
        {
            string target = $"{MenusArtFolder}/{file}.png";
            return CopyArt($"{MenusSourceFolder}/{folder}/{file}.png", target) ? SliceSheet(target, cellWidth) : System.Array.Empty<Sprite>();
        }

        /// <summary>Textura que se repite (para las capas que se desplazan).</summary>
        static Texture2D MenuTexture(string folder, string file)
        {
            if (MenuSprite(folder, file) == null)
                return null;

            string target = $"{MenusArtFolder}/{file}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(target);
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Menú de pausa de membrana y bestiario (docs/multimedia-temp/pause-menu/): importa las hojas, escribe
    /// Resources/PauseMenuArt (con los datos del bestiario) y monta la pausa en GameUI, el botón Bestiario del menú
    /// principal y la ventana de la sala de cambio. Se puede repetir.
    /// </summary>
    public static partial class ArtSetup
    {
        const string PauseMemSourceFolder = "docs/multimedia-temp/pause-menu";
        const string PauseMemArtFolder = "Assets/_Primordia/Art/UI/PauseMem";
        const string BestiaryArtFolder = "Assets/_Primordia/Art/UI/Bestiary";
        const string PauseMenuArtPath = "Assets/_Primordia/Resources/PauseMenuArt.asset";

        [MenuItem("Primordia/Art/Importar menú de pausa")]
        internal static void ImportPauseMenu()
        {
            var art = BuildPauseMenuArt();
            SetUpPausePanel(art);
            AddBestiaryToMainMenu();
            ApplyPixelFont(UISetup.GameUIPath);
            ApplyPixelFont(UISetup.MainMenuUIPath);
            // La ventana de la sala de cambio copia la lente, la cinta y los botones de la pausa.
            ExchangeRoomSetup.RebuildWindow();
            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: menú de pausa importado ({art.entries.Length} criaturas en el bestiario).");
        }

        static Sprite Mem(string file, Vector4 border = default)
        {
            string target = $"{PauseMemArtFolder}/{file}.png";
            var sprite = ImportSprite($"{PauseMemSourceFolder}/PauseMem/{file}.png", target, border);
            if (sprite == null)
                return null;
            // Las piezas en mosaico necesitan la malla entera.
            UseFullRectMesh(target);
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }

        static Sprite[] MemSheet(string file, int cell) =>
            ImportLooseSheet($"{PauseMemSourceFolder}/PauseMem/{file}.png", $"{PauseMemArtFolder}/{file}.png", cell);

        static PauseMenuArt BuildPauseMenuArt()
        {
            var art = AssetDatabase.LoadAssetAtPath<PauseMenuArt>(PauseMenuArtPath);
            if (art == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PauseMenuArtPath));
                art = ScriptableObject.CreateInstance<PauseMenuArt>();
                AssetDatabase.CreateAsset(art, PauseMenuArtPath);
            }

            var sides = new System.Func<float, Vector4>(b => new Vector4(b, 0f, b, 0f));
            Sprite P(string name, Vector4 border = default) => Mem($"UI_PauseMem_{name}", border);
            Sprite[] S(string name, int cell) => MemSheet($"UI_PauseMem_{name}", cell);

            art.font = AssetDatabase.LoadAssetAtPath<Font>(UISetup.FontPath);
            art.boldFont = AssetDatabase.LoadAssetAtPath<Font>(UISetup.BoldFontPath);

            art.panel = P("Panel", new Vector4(14f, 14f, 14f, 14f));
            art.title = P("Title", sides(20f));
            art.buttonNormal = P("Button_Normal", sides(9f));
            art.buttonHighlighted = P("Button_Highlighted", sides(9f));
            art.buttonPressed = P("Button_Pressed", sides(9f));
            art.dangerNormal = P("Button_Danger_Normal", sides(9f));
            art.dangerHighlighted = P("Button_Danger_Highlighted", sides(9f));
            art.dangerPressed = P("Button_Danger_Pressed", sides(9f));
            art.nucleus = S("Nucleus_On", 13);
            art.nucleusDanger = S("Nucleus_Danger_On", 13);

            art.tabActive = P("Tab_Active", sides(12f));
            art.tabIdle = P("Tab_Idle", sides(12f));
            art.tabHover = P("Tab_Hover", sides(12f));
            art.characterIcons = new[] { P("TabIcon_Creature"), P("TabIcon_State"), P("TabIcon_Items"), P("TabIcon_Run") };
            art.bestiaryIcons = new[] { P("TabIcon_Floor1"), P("TabIcon_Floor2"), P("TabIcon_Floor3"), P("RunIcon_Bosses") };
            art.keyQ = S("Key_Q", 13);
            art.keyE = S("Key_E", 13);

            art.slot = P("Slot_Normal");
            art.slotHighlighted = P("Slot_Highlighted");
            art.row = P("Row", sides(6f));
            art.tooltip = P("Tooltip", new Vector4(10f, 10f, 10f, 10f));
            art.tooltipPod = P("Tooltip_Pod");
            art.tooltipStripe = ImportUISprite("Panels", "UI_Card_Stripe", new Vector4(4f, 0f, 4f, 0f));

            art.heartExtraFull = S("Heart_Extra_Full", 16);
            art.heartExtraHalf = P("Heart_Extra_Half");
            art.heartExtraEmpty = P("Heart_Extra_Empty");
            art.damageIcon = MenuSprite("Pause", "UI_Stat_Damage");
            art.speedIcon = MenuSprite("Pause", "UI_Stat_Speed");
            art.upArrow = MenuSprite("Pause", "UI_Stat_Up");
            art.downArrow = MenuSprite("Pause", "UI_Stat_Down");
            art.unstableIcon = P("StatIcon_Unstable");

            art.floorIcon = P("RunIcon_Floor");
            art.seedIcon = P("RunIcon_Seed");
            art.timeIcon = art.characterIcons[3];
            art.enemiesIcon = P("RunIcon_Enemies");
            art.bossesIcon = art.bestiaryIcons[3];

            art.musicIcon = MenuSprite("Pause", "UI_Stat_Music");
            art.effectsIcon = MenuSprite("Pause", "UI_Stat_Sfx");
            art.shakeIcon = P("StatIcon_Shake");
            art.sliderTrack = P("Slider_Track", sides(5f));
            art.sliderFill = P("Slider_Fill", sides(5f));
            art.sliderKnob = P("Slider_Knob");
            art.toggleOff = P("Toggle_Off");
            art.toggleOn = P("Toggle_On");

            art.beastSlot = P("BeastSlot_Normal");
            art.beastSlotHighlighted = P("BeastSlot_Highlighted");
            art.beastSlotLocked = P("BeastSlot_Locked");
            art.vacuole = P("Beast_Vacuole");
            art.vacuoleGlint = P("Beast_Glint");

            ReadHudSprites(art);
            art.entries = BestiaryEntries();

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art;
        }

        /// <summary>Los corazones y la biomasa son los del HUD que ya usa GameUI.</summary>
        static void ReadHudSprites(PauseMenuArt art)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UISetup.GameUIPath);
            var hearts = prefab != null ? prefab.GetComponentInChildren<HeartsDisplay>(true) : null;
            if (hearts != null)
            {
                var serialized = new SerializedObject(hearts);
                art.heartFull = serialized.FindProperty("fullHeart").objectReferenceValue as Sprite;
                art.heartHalf = serialized.FindProperty("halfHeart").objectReferenceValue as Sprite;
                art.heartEmpty = serialized.FindProperty("emptyHeart").objectReferenceValue as Sprite;
            }

            var biomass = prefab != null ? prefab.GetComponentInChildren<BiomassCounter>(true) : null;
            if (biomass != null && new SerializedObject(biomass).FindProperty("icon").objectReferenceValue is Image icon)
                art.biomassIcon = icon.sprite;
        }

        // ---------- Pausa ----------

        static void SetUpPausePanel(PauseMenuArt art)
        {
            var lens = Mem("UI_PauseMem_Lens");
            var glint = Mem("UI_PauseMem_Lens_Glint");
            var organelles = MemSheet("UI_PauseMem_Lens_Organelles", 112);
            var line = Mem("UI_PauseMem_Callout_Line", new Vector4(1f, 1f, 1f, 1f));
            var dot = MemSheet("UI_PauseMem_Callout_Dot", 7);
            var empty = new[] { "Mouth", "Limbs", "Skin", "Organ" }.ToDictionary(
                slot => System.Enum.Parse<MutationSlot>(slot), slot => ImportUISprite("HUD", $"UI_Slot_{slot}_Empty"));

            EditPrefab(UISetup.GameUIPath, root =>
            {
                var pause = root.transform.Find("PausePanel");
                var zone = pause != null ? pause.Find("Creature") as RectTransform : null;
                if (zone == null)
                {
                    Debug.LogWarning("ArtSetup: la pausa no tiene la zona de la criatura; usa antes Primordia > Art > Importar menús.");
                    return;
                }

                // Todo lo demás de la pausa antigua se va: el menú nuevo se dibuja por código.
                foreach (var child in pause.Cast<Transform>().Where(c => c != zone).ToArray())
                    Object.DestroyImmediate(child.gameObject);
                if (pause.TryGetComponent<VerticalLayoutGroup>(out var layout))
                    Object.DestroyImmediate(layout);

                BuildPauseTemplates(pause, art);
                RestyleCreatureZone(zone, art, lens, glint, organelles, line, dot, empty);
                zone.SetAsLastSibling();
                zone.gameObject.SetActive(false);

                var menu = GetOrAdd<PauseMenu>(pause.gameObject);
                menu.Configure(zone.gameObject);
            });
        }

        /// <summary>La cinta y un botón de membrana, ocultos: de ellos copia la sala de cambio.</summary>
        static void BuildPauseTemplates(Transform pause, PauseMenuArt art)
        {
            var templates = UISetup.CreateRect("Templates", pause);

            var banner = UISetup.CreateRect("TitleBanner", templates);
            banner.sizeDelta = new Vector2(96f, 22f);
            var bannerImage = banner.gameObject.AddComponent<Image>();
            bannerImage.sprite = art.title;
            bannerImage.type = Image.Type.Sliced;
            bannerImage.raycastTarget = false;
            var title = Label(banner, "PAUSA", MemScreen.Bright, TextAnchor.MiddleCenter);
            title.font = art.boldFont;
            UISetup.Stretch(title.rectTransform);

            var rect = UISetup.CreateRect("Button", templates);
            rect.sizeDelta = new Vector2(98f, 17f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = art.buttonNormal;
            image.type = Image.Type.Tiled;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = art.buttonHighlighted,
                pressedSprite = art.buttonPressed,
                selectedSprite = art.buttonNormal,
                disabledSprite = art.buttonNormal,
            };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 98f;
            element.preferredHeight = 17f;
            var text = Label(rect, "BOTÓN", MemScreen.Cream, TextAnchor.MiddleCenter);
            UISetup.Stretch(text.rectTransform);

            templates.gameObject.SetActive(false);
        }

        static void RestyleCreatureZone(RectTransform zone, PauseMenuArt art, Sprite lens, Sprite glint, Sprite[] organelles,
            Sprite line, Sprite[] dot, Dictionary<MutationSlot, Sprite> empty)
        {
            zone.anchorMin = zone.anchorMax = zone.pivot = new Vector2(0.5f, 0.5f);
            // El centro de la lente en la maqueta: (178, 154) de 480×270.
            zone.anchoredPosition = new Vector2(-62f, -19f);
            zone.sizeDelta = new Vector2(176f, 170f);

            Restyle(zone, "Lens", lens);
            Restyle(zone, "Glint", glint);
            var bubbles = Restyle(zone, "LensBubbles", organelles.FirstOrDefault());
            if (bubbles != null)
                GetOrAdd<UISpriteAnimation>(bubbles.gameObject).Configure(organelles, 0.16f, newLoop: true);

            foreach (var callout in zone.GetComponentsInChildren<SlotCallout>(true))
            {
                callout.Rect.sizeDelta = new Vector2(26f, 26f);
                if (callout.TryGetComponent<Image>(out var frame))
                    frame.sprite = art.slot;
                callout.ConfigureLook(art.slot, art.slotHighlighted, empty[callout.Slot]);
            }

            var panel = zone.GetComponent<CreaturePanel>();
            panel.ConfigureLook(line, dot, 0.18f);
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("corridor").floatValue = 55f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var tooltip = zone.GetComponentInChildren<MutationTooltip>(true);
            if (tooltip == null)
                return;
            if (tooltip.TryGetComponent<Image>(out var background))
            {
                background.sprite = art.tooltip;
                background.type = Image.Type.Tiled;
            }
            if (tooltip.TryGetComponent<VerticalLayoutGroup>(out var box))
                box.padding = new RectOffset(8, 8, 6, 8);
            // Detrás de los textos, un fondo opaco por dentro del marco.
            var backing = Recreate("Backing", tooltip.transform);
            backing.SetAsFirstSibling();
            backing.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UISetup.Stretch(backing);
            backing.offsetMin = new Vector2(5f, 5f);
            backing.offsetMax = new Vector2(-5f, -5f);
            var fill = backing.gameObject.AddComponent<Image>();
            fill.raycastTarget = false;
            MemScreen.Opaque(fill);
            tooltip.Title.color = MemScreen.Bright;
            tooltip.Details.color = MemScreen.Heading;
            tooltip.Description.color = MemScreen.Dim;

            var pod = tooltip.transform.Find("Pointer") as RectTransform;
            if (pod != null)
            {
                pod.GetComponent<Image>().sprite = art.tooltipPod;
                pod.sizeDelta = new Vector2(12f, 14f);
            }
            tooltip.ConfigurePointer(4f, 0.5f, 8f);
        }

        static Image Restyle(Transform zone, string name, Sprite sprite)
        {
            var child = zone.Find(name);
            if (child == null || !child.TryGetComponent<Image>(out var image) || sprite == null)
                return null;
            image.sprite = sprite;
            image.enabled = true;
            image.rectTransform.sizeDelta = sprite.rect.size;
            return image;
        }

        // ---------- Menú principal ----------

        static void AddBestiaryToMainMenu()
        {
            EditPrefab(UISetup.MainMenuUIPath, root =>
            {
                var panel = root.transform.Find("MainPanel");
                var buttons = panel != null
                    ? panel.GetComponentsInChildren<Button>(true).Where(b => b.transform.parent == panel).ToList()
                    : new List<Button>();
                var play = buttons.FirstOrDefault(b => b.TryGetComponent<GameFlowButton>(out var flow) && flow.Action == FlowAction.Play);
                if (play == null)
                {
                    Debug.LogWarning("ArtSetup: el menú principal no tiene el botón Jugar; no se añade el bestiario.");
                    return;
                }

                var bestiary = buttons.FirstOrDefault(b => b.name == "Bestiary");
                if (bestiary == null)
                {
                    var clone = Object.Instantiate(play.gameObject, panel);
                    clone.name = "Bestiary";
                    Object.DestroyImmediate(clone.GetComponent<GameFlowButton>());
                    bestiary = clone.GetComponent<Button>();
                    foreach (var text in clone.GetComponentsInChildren<Text>(true))
                        text.text = "Bestiario";
                }
                buttons.Remove(bestiary);
                // Entre Jugar y Salir.
                buttons.Insert(buttons.IndexOf(play) + 1, bestiary);
                bestiary.transform.SetSiblingIndex(play.transform.GetSiblingIndex() + 1);
                for (int i = 0; i < buttons.Count; i++)
                    PlaceTop((RectTransform)buttons[i].transform, new Vector2(0f, -156f - 22f * i), MenuButtonSize);

                var selector = panel.GetComponentInChildren<MenuSelector>(true);
                if (selector != null)
                {
                    var serialized = new SerializedObject(selector);
                    var list = serialized.FindProperty("buttons");
                    list.arraySize = buttons.Count;
                    for (int i = 0; i < buttons.Count; i++)
                        list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                GetOrAdd<MainMenuBestiary>(root).Configure(bestiary, panel.gameObject);
            });
        }

        // ---------- Bestiario ----------

        static BestiaryEntry[] BestiaryEntries()
        {
            BestiaryEntry E(string id, string name, BestiaryTab tab, string damage, params string[] notes) =>
                Entry(id, name, tab, damage, null, notes);

            BestiaryEntry Boss(string id, string name, string damage, string speed, params string[] notes) =>
                Entry(id, name, BestiaryTab.Bosses, damage, speed, notes);

            BestiaryEntry Trap(string id, string name, string damage, string speed, params string[] notes)
            {
                var entry = Entry(id, name, BestiaryTab.Floor3, damage, speed, notes);
                entry.trap = true;
                return entry;
            }

            var list = new List<BestiaryEntry>
            {
                E("Chaser", "BACTERIA", BestiaryTab.Floor1, "1",
                    "BACTERIA FLAGELADA. UN SOLO", "LÁTIGO LE BASTA PARA", "CAZAR TODO LO QUE SE MUEVE."),
                E("Shooter", "AMEBA", BestiaryTab.Floor1, "1",
                    "CAMBIA DE FORMA SIN PARAR.", "ESCUPE LO QUE NO DIGIERE Y", "AL MORIR SE PARTE EN DOS."),
                E("Small", "AMEBITA", BestiaryTab.Floor1, "1",
                    "MITAD DE UNA AMEBA. LE", "FALTA TAMAÑO, PERO NO", "GANAS DE MORDER."),

                E("Trilobite", "TRILOBITE", BestiaryTab.Floor2, "1",
                    "SUS OJOS ERAN CRISTALES", "DE CALCITA. EMBISTE Y SE", "ENROSCA SI LO VOLTEAN."),
                E("Pikaia", "PIKAIA", BestiaryTab.Floor2, "1",
                    "UNO DE LOS PRIMEROS", "CORDADOS: UNA ESPINA", "FLEXIBLE Y MUCHA PRISA."),
                E("Ottoia", "OTTOIA", BestiaryTab.Floor2, "1",
                    "GUSANO PRIAPÚLIDO. ESPERA", "ENTERRADO Y SACA UNA", "TROMPA LLENA DE GANCHOS."),
                E("Vauxia", "VAUXIA", BestiaryTab.Floor2, "0",
                    "ESPONJA DEL ARRECIFE. NO", "SE MUEVE, PERO CURA A", "TODO LO QUE LA RODEA."),
                E("BlueJelly", "MEDUSA AZUL", BestiaryTab.Floor2, "1",
                    "MEDUSA DEL ABISMO. SU", "CAMPANA BRILLA ANTES DE", "SOLTAR SUS DESCARGAS."),

                E("Beetle", "ESCARABAJO", BestiaryTab.Floor3, "1",
                    "ESCARABAJO DEL CARBONÍFERO.", "ACORAZADO; EMBISTE CUANDO", "SE SIENTE ACORRALADO."),
                E("Spider", "ARAÑA", BestiaryTab.Floor3, "1",
                    "SE OCULTA ENTRE LA MALEZA", "Y SALTA SOBRE LO QUE PASA.", "SUS PATAS LO SIENTEN TODO."),
                E("Toad", "SAPO", BestiaryTab.Floor3, "1",
                    "ANFIBIO DEL PANTANO. SE", "HUNDE EN EL LODO Y SALTA", "CUANDO TE ACERCAS."),
                E("Dragonfly", "LIBÉLULA", BestiaryTab.Floor3, "1",
                    "LIBÉLULA DEL CARBONÍFERO.", "CON TANTO OXÍGENO EN EL", "AIRE, LOS INSECTOS CRECÍAN."),
                E("MeganeuraNymph", "NINFA", BestiaryTab.Floor3, "1",
                    "NINFA DE MEGANEURA. CAZA", "BAJO EL AGUA CON UNA", "MANDÍBULA QUE SE DISPARA."),
                E("Calamites", "CALAMITES", BestiaryTab.Floor3, "0",
                    "COLA DE CABALLO GIGANTE.", "SUS TALLOS HUECOS SE", "ALZABAN COMO ÁRBOLES."),
                E("CalamitesSprout", "BROTE", BestiaryTab.Floor3, "1",
                    "BROTE DE CALAMITES. CRECE", "DEPRISA Y CORRE A", "MORDERTE LOS TOBILLOS."),
                E("Medullosa", "MEDULLOSA", BestiaryTab.Floor3, "1",
                    "HELECHO CON SEMILLAS. LAS", "LANZA COMO PROYECTILES", "CUANDO ALGO SE ACERCA."),
                Trap("CarnivorousPlant", "PLANTA CARNÍVORA", "1", "QUIETA",
                    "NO PERSIGUE: ESPERA A", "QUE PASES CERCA PARA", "CERRAR LA BOCA."),
                Trap("Mayflies", "EFÍMERAS", "1", "LENTA",
                    "VIVEN UN SOLO DÍA Y LO", "PASAN EN UN ENJAMBRE", "QUE NO TE DEJA EN PAZ."),

                Boss("AmoebaBoss", "AMEBA GIGANTE", "1", null,
                    "SE HA COMIDO A TODAS SUS", "HERMANAS. AL PARTIRLA, SE", "DIVIDE EN DOS MÁS RÁPIDAS."),
                Boss("VirusBoss", "VIRUS", "1", null,
                    "NO ESTÁ VIVO DEL TODO.", "INYECTA SU CÓDIGO Y", "MULTIPLICA LO QUE TOCA."),
                Boss("AnomalocarisBoss", "ANOMALOCARIS", "2", null,
                    "EL GRAN DEPREDADOR DEL", "CÁMBRICO. ATRAPABA PRESAS", "CON SUS DOS GARFIOS."),
                Boss("EurypteridBoss", "EURIPTÉRIDO", "2", null,
                    "ESCORPIÓN MARINO. SUS", "PALAS LE HACÍAN NADAR Y", "SU COLA, DERRUMBAR ROCAS."),
                // Los jefes del piso 3 se mueven con su propia lógica: la velocidad va escrita a mano.
                Boss("ArthropleuraBoss", "ARTHROPLEURA", "2", "MEDIA",
                    "EL MAYOR ARTRÓPODO DE", "TIERRA: MÁS DE DOS METROS", "DE PATAS Y CORAZA."),
                Boss("MeganeuraBoss", "MEGANEURA", "2", "RÁPIDA",
                    "LIBÉLULA GIGANTE: SETENTA", "CENTÍMETROS DE ALAS.", "LLAMA A LA TORMENTA."),
                Boss("MegarachneBoss", "MEGARACHNE", "1", "MEDIA",
                    "SU NOMBRE SIGNIFICA", "GRAN ARAÑA, AUNQUE ERA UN", "EURIPTÉRIDO. AQUÍ TEJE."),
                Boss("LepidodendronBoss", "LEPIDODENDRON", "2", "QUIETA",
                    "ÁRBOL ESCAMOSO DEL", "CARBONÍFERO. SUS RESTOS", "SON HOY CARBÓN."),
            };
            list[list.Count - 1].secret = true;
            return list.ToArray();
        }

        static BestiaryEntry Entry(string id, string name, BestiaryTab tab, string damage, string speed, string[] notes)
        {
            var entry = new BestiaryEntry
            {
                id = id,
                displayName = name,
                tab = tab,
                damage = damage,
                notes = notes,
                data = AssetDatabase.FindAssets($"Enemy_{id} t:EnemyData")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => Path.GetFileNameWithoutExtension(p) == $"Enemy_{id}")
                    .Select(AssetDatabase.LoadAssetAtPath<EnemyData>)
                    .FirstOrDefault(),
                portrait = Portrait($"Portrait_{id}"),
                lockedPortrait = Portrait($"Portrait_{id}_Locked"),
            };
            entry.speed = speed ?? SpeedName(entry.data != null ? entry.data.MoveSpeed : 0f);
            entry.frames = CreatureFrames(id);
            MeasureContent(entry);
            return entry;
        }

        static Sprite Portrait(string file) => ImportSprite($"{PauseMemSourceFolder}/Bestiary/{file}.png",
            $"{BestiaryArtFolder}/{file}.png", Vector4.zero);

        static string SpeedName(float speed) =>
            speed <= 0f ? "QUIETA" : speed <= 2f ? "LENTA" : speed < 3.5f ? "MEDIA" : speed < 4.5f ? "RÁPIDA" : "MUY RÁPIDA";

        /// <summary>Los cuadros de moverse (o reposo) del prefab de la criatura.</summary>
        static Sprite[] CreatureFrames(string id)
        {
            if (id is "CarnivorousPlant" or "Mayflies")
                return TrapFrames(id);

            string file = id.EndsWith("Boss") ? $"Boss_{id.Substring(0, id.Length - 4)}" : $"Enemy_{id}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Primordia/Prefabs/Enemies/{file}.prefab");
            if (prefab == null)
                return System.Array.Empty<Sprite>();

            var animator = prefab.GetComponentInChildren<Animator>(true);
            var controller = animator != null ? animator.runtimeAnimatorController : null;
            if (controller != null)
            {
                var clips = controller.animationClips;
                var clip = clips.FirstOrDefault(c => c.name.EndsWith("Move")) ?? clips.FirstOrDefault(c => c.name.Contains("Idle"))
                    ?? clips.FirstOrDefault();
                if (clip != null)
                {
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    {
                        var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding)
                            .Select(k => k.value as Sprite).Where(s => s != null).ToArray();
                        if (frames.Length > 0)
                            return frames;
                    }
                }
            }

            // Los que se dibujan por direcciones (la ninfa): moverse, mirando de tres cuartos a la derecha.
            foreach (var directional in prefab.GetComponentsInChildren<DirectionalSprite>(true))
            {
                if (new SerializedObject(directional).FindProperty("art").objectReferenceValue is not DirectionalArt sheets)
                    continue;
                var body = sheets.Sheets.Where(s => !s.name.Contains("Shadow")).ToArray();
                var sheet = body.FirstOrDefault(s => s.name.EndsWith("Nymph_Move"));
                if (sheet == null)
                    continue;
                int row = sheet.rows == DirectionalArt.DirectionRows ? DirectionalArt.RowFor(-45f, out _) : 0;
                return Enumerable.Range(0, sheet.frames).Select(i => sheet.Get(row, i)).Where(s => s != null).ToArray();
            }

            var renderer = prefab.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.sprite != null);
            return renderer != null ? new[] { renderer.sprite } : System.Array.Empty<Sprite>();
        }

        static Sprite[] TrapFrames(string id)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Primordia/Prefabs/Traps" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                Component trap = id == "Mayflies"
                    ? prefab.GetComponentInChildren<MayflySwarm>(true)
                    : prefab.GetComponentInChildren<CarnivorousPlant>(true);
                if (trap == null)
                    continue;

                var serialized = new SerializedObject(trap);
                var list = serialized.FindProperty("idle") ?? serialized.FindProperty("swarm");
                if (list == null || !list.isArray)
                    break;
                return Enumerable.Range(0, list.arraySize)
                    .Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as Sprite)
                    .Where(s => s != null).ToArray();
            }
            return System.Array.Empty<Sprite>();
        }

        /// <summary>Caja de lo dibujado en todos los cuadros, para centrarlo y elegir la escala en la ficha.</summary>
        static void MeasureContent(BestiaryEntry entry)
        {
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            var textures = new Dictionary<string, Texture2D>();
            foreach (var sprite in entry.frames)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                if (!textures.TryGetValue(path, out var texture))
                {
                    texture = new Texture2D(2, 2);
                    texture.LoadImage(File.ReadAllBytes(path));
                    textures[path] = texture;
                }

                var rect = sprite.rect;
                for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
                for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                {
                    if (texture.GetPixel(x, y).a < 0.5f)
                        continue;
                    Vector2 local = new Vector2(x, y) - rect.center;
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local + Vector2.one);
                }
            }
            foreach (var texture in textures.Values)
                Object.DestroyImmediate(texture);

            bool empty = max.x < min.x;
            entry.contentSize = empty ? Vector2.zero : max - min;
            entry.contentOffset = empty ? Vector2.zero : (min + max) / 2f;
        }
    }
}

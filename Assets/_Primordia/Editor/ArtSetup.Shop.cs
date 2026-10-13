using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Player;
using Primordia.Rooms;
using Primordia.Shop;
using Primordia.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Tienda (docs/multimedia-temp/shop/): importa a Darwin, el mostrador, el pedestal y los artículos, monta el
    /// prefab Prefabs/Shop/Shop (mostrador arriba, 4 pedestales con precio y tecla E), se lo da a Floor_1 con la
    /// sala pequeña, pone el icono del mapa, el cartel del HUD y el <see cref="PlayerInteractor"/> del jugador.
    /// Necesita antes Importar objetos e interfaz (burbujas y sombra), Importar biomasa (icono del precio) e
    /// Importar menús (fondo del cartel). Las señales de vendido y de no disponible vienen de
    /// docs/multimedia-temp/shop-unavailable/.
    /// </summary>
    public static partial class ArtSetup
    {
        const string ShopSourceFolder = "docs/multimedia-temp/shop";
        const string ShopMarksSourceFolder = "docs/multimedia-temp/shop-unavailable";
        const string ShopArtFolder = "Assets/_Primordia/Art/Shop";
        const string ShopPrefabsFolder = "Assets/_Primordia/Prefabs/Shop";
        const string ShopPrefabPath = ShopPrefabsFolder + "/Shop.prefab";

        // Distribución en la sala pequeña (suelo de 20×11), en unidades desde el centro del suelo.
        const float CounterY = 3f;
        const float PedestalY = -1.5f;
        static readonly float[] PedestalX = { -4.5f, -1.5f, 1.5f, 4.5f };

        // Darwin, mostrador y pedestal: pivote abajo en el centro (los pies, la base).
        static readonly Character ShopArt = new()
        {
            Name = "Shop",
            SourceFolder = ShopSourceFolder,
            ArtFolder = ShopArtFolder,
            ControllerPerClip = true,
            Pivot = new Vector2(0.5f, 0f),
            CellWidths = new Dictionary<string, int>
            {
                ["Darwin_Idle"] = 24, ["Darwin_Buy"] = 24, ["Counter"] = 48, ["Pedestal"] = 20, ["Pedestal_Glow"] = 20,
            },
            // Pedestal_Glow antes que Pedestal: gana el primer prefijo que encaje.
            Timings = new[]
            {
                ("Darwin_Idle", 6f, true), ("Darwin_Buy", 10f, false), ("Counter", 1f, false),
                ("Pedestal_Glow", 1f / 0.15f, true), ("Pedestal", 1f, false),
            },
            FrameDurations = new Dictionary<string, float[]>
            {
                ["Darwin_Idle"] = new[] { 0.22f, 0.16f, 0.09f, 0.18f, 0.18f, 0.22f },
                ["Darwin_Buy"] = new[] { 0.07f, 0.08f, 0.11f, 0.08f, 0.09f, 0.12f },
            },
        };

        // Artículos y efecto de compra: pivote en el centro.
        static readonly Character ShopItemsArt = new()
        {
            Name = "Item",
            SourceFolder = ShopSourceFolder,
            ArtFolder = ShopArtFolder + "/Items",
            ControllerPerClip = true,
            Timings = new[]
            {
                ("Upgrade_Idle", 1f / 0.12f, true), ("Heal_Idle", 1f / 0.15f, true), ("HeartContainer_Idle", 1f / 0.13f, true),
                ("Purchase", 1f / 0.06f, false),
            },
        };

        // Señal de vendido (Shop_Unavailable) y letrero de no disponible (Shop_Unavailable_Sign): pivote en el centro.
        static readonly Character ShopMarksArt = new()
        {
            Name = "Shop",
            SourceFolder = ShopMarksSourceFolder,
            ArtFolder = ShopArtFolder + "/Marks",
            ControllerPerClip = true,
            // Unavailable_Sign antes que Unavailable: gana el primer prefijo que encaje.
            Timings = new[] { ("Unavailable_Sign", 1f / 0.15f, true), ("Unavailable", 1f / 0.15f, true) },
        };

        [MenuItem("Primordia/Art/Importar tienda")]
        static void ImportShop()
        {
            var art = Import(ShopArt);
            var items = Import(ShopItemsArt);
            if (art == null || items == null)
                return;
            var marks = Import(ShopMarksArt);

            Directory.CreateDirectory(ShopPrefabsFolder);
            Directory.CreateDirectory(EffectPrefabsFolder);
            var purchase = CreateEffectPrefab("Effect_ShopPurchase", items, "Purchase", sortingOrder: 12);

            // Darwin: un solo Animator con reposo y venta.
            var darwinController = CreateController($"{ShopArt.AnimationsFolder}/Shop_Darwin.controller",
                new[] { art.Clips["Darwin_Idle"], art.Clips["Darwin_Buy"] }, "Darwin_Idle");
            AssetDatabase.SaveAssets();

            string keyPath = $"{ShopArtFolder}/UI_Key_E.png";
            var keyFrames = CopyArt($"{ShopSourceFolder}/UI_Key_E.png", keyPath)
                ? SliceSheet(keyPath, 13, new Vector2(0.5f, 0f))
                : System.Array.Empty<Sprite>();

            var shop = CreateShopPrefab(art, items, marks, darwinController, keyFrames, purchase);
            if (shop == null)
                return;

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>($"Assets/_Primordia/Data/Rooms/{RoomLayouts.Small}.asset");
            if (config != null && template != null)
            {
                config.ConfigureShop(template, shop);
                EditorUtility.SetDirty(config);
            }
            else
            {
                Debug.LogWarning("ArtSetup: falta Floor_1 o la plantilla Room_Small (créalos con Primordia > Floor); el piso no tendrá tienda.");
            }

            EditPrefab(PlayerSetup.PlayerPath, root => GetOrAdd<PlayerInteractor>(root));

            var mapIcon = ImportSprite($"{ShopSourceFolder}/UI_Map_Shop.png", $"{MapArtFolder}/UI_Map_Shop.png", Vector4.zero);
            var tooltipBackground = MenuSprite("Pause", "UI_Tooltip", 6);
            var stripe = ImportUISprite("Panels", "UI_Card_Stripe", new Vector4(4, 0, 4, 0));
            EditPrefab(UISetup.GameUIPath, root =>
            {
                foreach (var map in root.GetComponentsInChildren<MapUI>(true))
                    map.SetShopIcon(mapIcon);
                BuildShopTooltip(root.transform, tooltipBackground, stripe);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: tienda importada (Darwin, pedestales, artículos, prefab Shop, Floor_1, mapa, cartel y tecla E).");
        }

        static ShopRoom CreateShopPrefab(ImportedArt art, ImportedArt items, ImportedArt marks, AnimatorController darwinController,
            Sprite[] keyFrames, GameObject purchase)
        {
            var bubbles = LineageNames
                .Select(lineage => AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ItemsArtFolder}/Animations/{lineage}_Idle.controller"))
                .ToArray();
            if (bubbles.Any(bubble => bubble == null))
                Debug.LogWarning("ArtSetup: faltan las burbujas de mutación; ejecuta antes Primordia > Art > Importar objetos e interfaz.");

            var shadow = AssetDatabase.LoadAssetAtPath<Sprite>($"{ItemsArtFolder}/Item_Mutation_Shadow.png");
            var priceIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIArtFolder}/UI_Biomass.png");
            if (priceIcon == null)
                Debug.LogWarning("ArtSetup: falta UI_Biomass; ejecuta antes Primordia > Art > Importar biomasa.");

            items.Controllers.TryGetValue("Upgrade_Idle", out var upgrade);
            items.Controllers.TryGetValue("Heal_Idle", out var heal);
            items.Controllers.TryGetValue("HeartContainer_Idle", out var heart);
            art.Controllers.TryGetValue("Pedestal_Glow", out var glowController);
            AnimatorController sold = null, closed = null;
            if (marks != null)
            {
                marks.Controllers.TryGetValue("Unavailable", out sold);
                marks.Controllers.TryGetValue("Unavailable_Sign", out closed);
            }
            if (sold == null || closed == null)
                Debug.LogWarning($"ArtSetup: faltan Shop_Unavailable y Shop_Unavailable_Sign en {ShopMarksSourceFolder}; los pedestales vendidos o cerrados quedarán vacíos.");

            var prefab = EditPrefab(ShopPrefabPath, root =>
            {
                // Se rehace entero cada vez: así el menú se puede repetir al cambiar el arte.
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

                // Mostrador arriba en el centro, con Darwin detrás asomando de cintura para arriba (5 px más alto).
                var counter = new GameObject("Counter").transform;
                counter.SetParent(root.transform, false);
                counter.localPosition = new Vector3(0f, CounterY, 0f);
                var counterSprite = ShopPart(counter, "Sprite", Vector3.zero, art.FirstSprites["Counter"], sortingOrder: 5);
                var counterCollider = counterSprite.gameObject.AddComponent<BoxCollider2D>();
                counterCollider.size = new Vector2(2.75f, 0.85f);
                counterCollider.offset = new Vector2(0f, 0.5f);

                var darwinSprite = ShopPart(counter, "Darwin", new Vector3(0f, 5f / PixelsPerUnit, 0f), art.FirstSprites["Darwin_Idle"], sortingOrder: 4);
                var darwin = darwinSprite.gameObject.AddComponent<Animator>();
                darwin.runtimeAnimatorController = darwinController;

                var pedestals = new ShopPedestal[PedestalX.Length];
                for (int i = 0; i < PedestalX.Length; i++)
                    pedestals[i] = CreatePedestal(root.transform, i, art, glowController, shadow, keyFrames, priceIcon);

                var shop = GetOrAdd<ShopRoom>(root);
                shop.Configure(MutationSetup.LoadPool(), pedestals, counter, darwin, art.Length("Darwin_Buy"));
                shop.ConfigureLook(bubbles, upgrade, heal, heart, purchase, sold, closed);
            }, create: true);

            return prefab != null ? prefab.GetComponent<ShopRoom>() : null;
        }

        /// <summary>
        /// Pedestal con su base apoyada en el suelo: brillo encima, sombra en la cara de arriba, el artículo flotando
        /// con su icono, la tecla E y, debajo, un lienzo en el mundo con el cambio de nivel y el precio.
        /// </summary>
        static ShopPedestal CreatePedestal(Transform parent, int index, ImportedArt art, RuntimeAnimatorController glowController,
            Sprite shadowSprite, Sprite[] keyFrames, Sprite priceIcon)
        {
            var root = new GameObject($"Pedestal_{index}").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(PedestalX[index], PedestalY, 0f);

            var baseSprite = ShopPart(root, "Base", Vector3.zero, art.FirstSprites["Pedestal"], sortingOrder: 4);
            var collider = baseSprite.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 0.5f);
            collider.offset = new Vector2(0f, 0.3f);

            var glow = ShopPart(root, "Glow", Vector3.zero, art.FirstSprites["Pedestal_Glow"], sortingOrder: 5);
            glow.gameObject.AddComponent<Animator>().runtimeAnimatorController = glowController;
            glow.gameObject.SetActive(false);

            // La sombra en la cara de arriba del pedestal (centrada a 12 px de la base).
            var shadow = ShopPart(root, "Shadow", new Vector3(0f, 12f / PixelsPerUnit, 0f), shadowSprite, sortingOrder: 5);

            var floating = new GameObject("Float").transform;
            floating.SetParent(root, false);
            floating.localPosition = new Vector3(0f, 26f / PixelsPerUnit, 0f);
            var item = ShopPart(floating, "Item", Vector3.zero, null, sortingOrder: 6);
            var itemAnimator = item.gameObject.AddComponent<Animator>();
            var icon = ShopPart(floating, "Icon", Vector3.zero, null, sortingOrder: 7);

            var key = ShopPart(root, "Key", new Vector3(0f, 40f / PixelsPerUnit, 0f), keyFrames.Length > 0 ? keyFrames[0] : null, sortingOrder: 20);
            key.enabled = false;

            var (labels, level, price) = PriceCanvas(root, priceIcon);

            var pedestal = root.gameObject.AddComponent<ShopPedestal>();
            pedestal.SetRadius(1.5f);
            pedestal.ConfigureLook(floating, item, itemAnimator, icon, shadow.gameObject, glow.gameObject, key, keyFrames,
                labels, price, level);
            return pedestal;
        }

        /// <summary>Lienzo en el mundo bajo el pedestal (1 px de interfaz = 1 px del juego): nivel arriba y precio debajo.</summary>
        static (GameObject root, Text level, Text price) PriceCanvas(Transform parent, Sprite priceIcon)
        {
            var canvasObject = new GameObject("Labels", typeof(RectTransform), typeof(Canvas));
            var rect = (RectTransform)canvasObject.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one / PixelsPerUnit;
            rect.sizeDelta = new Vector2(40f, 20f);
            // Arriba del lienzo justo bajo la base del pedestal.
            rect.pivot = new Vector2(0.5f, 1f);
            rect.localPosition = new Vector3(0f, -1f / PixelsPerUnit, 0f);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 15;

            var layout = canvasObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 1f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var level = Label(rect, "I > II", new Color(0.95f, 0.8f, 0.35f), TextAnchor.MiddleCenter);
            level.name = "Level";
            AddTextShadow(level);

            var row = UISetup.CreateRow("Price", rect, rect.childCount, TextAnchor.MiddleCenter);
            var icon = UISetup.CreateIcon("Icon", row, 12f);
            icon.sprite = priceIcon;
            icon.enabled = priceIcon != null;
            var price = Label(row, "15", UISetup.TextColor, TextAnchor.MiddleLeft);
            price.name = "Amount";
            AddTextShadow(price);

            return (row.gameObject, level, price);
        }

        static void AddTextShadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(0x18, 0x14, 0x25, 0xff);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        static SpriteRenderer ShopPart(Transform parent, string name, Vector3 position, Sprite sprite, int sortingOrder)
        {
            var part = new GameObject(name).transform;
            part.SetParent(parent, false);
            part.localPosition = position;
            var renderer = part.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>Cartel del HUD abajo en el centro con lo que hay en el pedestal que el jugador tiene delante.</summary>
        static void BuildShopTooltip(Transform canvas, Sprite background, Sprite stripeSprite)
        {
            var box = Recreate("ShopTooltip", canvas);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, 10f);
            box.sizeDelta = new Vector2(180f, 0f);
            SlicedImage(box.gameObject, background);

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 7);
            layout.spacing = 3f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var stripe = SlicedImage(UISetup.CreateRect("Stripe", box).gameObject, stripeSprite);
            stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = stripeSprite != null ? stripeSprite.rect.height : 4f;

            var title = Label(box, "Artículo", UISetup.TitleColor, TextAnchor.MiddleLeft);
            var bold = AssetDatabase.LoadAssetAtPath<Font>(UISetup.BoldFontPath);
            if (bold != null)
                title.font = bold;

            var details = Label(box, "Detalles", UISetup.TextColor, TextAnchor.UpperLeft);
            details.horizontalOverflow = HorizontalWrapMode.Wrap;
            var description = Label(box, "Descripción", UISetup.TextColor, TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            var note = Label(box, "Aviso", new Color(1f, 0.72f, 0.35f), TextAnchor.UpperLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;

            box.gameObject.AddComponent<ShopTooltip>().Configure(stripe, title, details, description, note);
            box.gameObject.SetActive(false);
        }
    }
}

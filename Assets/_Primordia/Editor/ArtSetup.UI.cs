using System.IO;
using Primordia.Mutations;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Objetos e interfaz (docs/multimedia-temp/ui/): corazones, botones, tarjetas, panel de la pausa, iconos de
    /// mutación y de linaje, barra de recarga y las burbujas de mutación que suelta el jefe.
    /// También la mira y las ranuras del cuerpo del HUD. La biomasa se importará cuando exista su código (con la tienda).
    /// </summary>
    public static partial class ArtSetup
    {
        const string UISourceFolder = "docs/multimedia-temp/ui";
        const string UIArtFolder = "Assets/_Primordia/Art/UI";
        const string ItemsArtFolder = "Assets/_Primordia/Art/Items";
        const string MutationPickupPath = MutationPrefabsFolder + "/MutationPickup.prefab";

        // En el orden del enum Lineage.
        static readonly string[] LineageNames = { "Predator", "Armored", "Toxic" };

        static readonly string[] MutationNames =
            { "Mandibles", "Stinger", "Spores", "Fins", "JointedLegs", "Carapace", "StingingSkin", "InkSac", "ShockGland" };

        // Burbujas de mutación (Item_Mutation_Predator_Idle.png → Predator_Idle): 120 ms por cuadro en reposo, 60 al reventar.
        static readonly Character MutationItemsArt = new()
        {
            Name = "Item_Mutation",
            SourceFolder = UISourceFolder + "/Items",
            ArtFolder = ItemsArtFolder,
            ControllerPerClip = true,
            SkipClips = new System.Collections.Generic.HashSet<string> { "Shadow" },
            Timings = new[]
            {
                ("Predator_Idle", 1f / 0.12f, true), ("Armored_Idle", 1f / 0.12f, true), ("Toxic_Idle", 1f / 0.12f, true),
                ("Predator_Collect", 1f / 0.06f, false), ("Armored_Collect", 1f / 0.06f, false), ("Toxic_Collect", 1f / 0.06f, false),
            },
        };

        [MenuItem("Primordia/Art/Importar objetos e interfaz")]
        static void ImportUI()
        {
            ImportMutationIcons();
            ApplyUISkin(UISetup.GameUIPath);
            ApplyUISkin(UISetup.MainMenuUIPath);
            ApplyCooldownBar();

            var pickup = CreateMutationPickupPrefab();
            if (pickup != null)
                EditPrefab($"{EnemyPrefabsFolder}/Boss_Virus.prefab",
                    root => GetOrAdd<MutationDrop>(root).Configure(MutationSetup.LoadPool(), pickup.GetComponent<MutationPickup>()));

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: objetos e interfaz importados.");
        }

        static void ImportMutationIcons()
        {
            foreach (var name in MutationNames)
            {
                var small = ImportUISprite("Icons", $"Icon_{name}_16");
                var large = ImportUISprite("Icons", $"Icon_{name}_32");
                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationDataFolder}/Mutation_{name}.asset");
                if (mutation == null)
                {
                    Debug.LogWarning($"ArtSetup: no existe Mutation_{name}; créalo con Primordia > Mutations.");
                    continue;
                }

                mutation.SetIcons(small, large);
                EditorUtility.SetDirty(mutation);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Viste un prefab de interfaz: escala de píxeles 1:1, corazones, botones y tarjetas en 9-slice con un
        /// sprite por estado, franja e iconos de las tarjetas y fondo de la lista de mutaciones.
        /// </summary>
        static void ApplyUISkin(string prefabPath)
        {
            if (prefabPath == UISetup.GameUIPath)
            {
                UISetup.EnsureMutationIcons();
                UISetup.EnsureHudSlotsAndCrosshair();
            }

            var button = new[]
            {
                ImportUISprite("Panels", "UI_Button_Normal", 7), ImportUISprite("Panels", "UI_Button_Highlighted", 7),
                ImportUISprite("Panels", "UI_Button_Pressed", 7), null,
            };
            var card = new[]
            {
                ImportUISprite("Panels", "UI_Card_Normal", 12), ImportUISprite("Panels", "UI_Card_Highlighted", 12),
                ImportUISprite("Panels", "UI_Card_Pressed", 12), ImportUISprite("Panels", "UI_Card_Disabled", 12),
            };
            var panel = ImportUISprite("Panels", "UI_Panel", 12);
            var stripe = ImportUISprite("Panels", "UI_Card_Stripe", new Vector4(4, 0, 4, 0));
            var lineageIcons = System.Array.ConvertAll(LineageNames, lineage => ImportUISprite("Icons", $"Icon_Lineage_{lineage}"));
            var hearts = new[]
            {
                ImportUISprite("HUD", "UI_Heart_Full"), ImportUISprite("HUD", "UI_Heart_Half"), ImportUISprite("HUD", "UI_Heart_Empty"),
            };
            var slotFrame = ImportUISprite("HUD", "UI_Slot_Frame");
            // En el orden de MutationSlotsDisplay: Boca, Extremidades, Piel, Órgano.
            var emptySlots = System.Array.ConvertAll(new[] { "Mouth", "Limbs", "Skin", "Organ" },
                slot => ImportUISprite("HUD", $"UI_Slot_{slot}_Empty"));
            var crosshair = ImportUISprite("HUD", "UI_Crosshair");
            var crosshairFire = ImportUISheet("HUD", "UI_Crosshair_Fire", cellWidth: 15);

            EditPrefab(prefabPath, root =>
            {
                if (root.TryGetComponent<CanvasScaler>(out var scaler))
                    scaler.referencePixelsPerUnit = UISetup.ReferencePixelsPerUnit;

                foreach (var display in root.GetComponentsInChildren<HeartsDisplay>(true))
                    SetFields(display, ("fullHeart", hearts[0]), ("halfHeart", hearts[1]), ("emptyHeart", hearts[2]));

                foreach (var slots in root.GetComponentsInChildren<MutationSlotsDisplay>(true))
                    slots.ConfigureLook(slotFrame, emptySlots);

                foreach (var cursor in root.GetComponentsInChildren<Crosshair>(true))
                    cursor.ConfigureLook(crosshair, crosshairFire, 0.04f);

                foreach (var selectable in root.GetComponentsInChildren<Button>(true))
                    SkinButton(selectable, selectable.TryGetComponent<MutationCard>(out _) ? card : button);

                foreach (var mutationCard in root.GetComponentsInChildren<MutationCard>(true))
                {
                    mutationCard.SetLineageIcons(lineageIcons);
                    var strip = mutationCard.transform.Find("ColorStrip");
                    if (strip != null && stripe != null)
                    {
                        SetSlicedSprite(strip.GetComponent<Image>(), stripe, keepColor: true);
                        strip.GetComponent<LayoutElement>().preferredHeight = stripe.rect.height;
                    }
                }

                foreach (var list in root.GetComponentsInChildren<MutationListUI>(true))
                {
                    if (panel != null && list.TryGetComponent<Image>(out var background))
                        SetSlicedSprite(background, panel, keepColor: false);
                    if (list.TryGetComponent<VerticalLayoutGroup>(out var layout))
                        layout.padding = new RectOffset(10, 10, 10, 10);
                }
            });
        }

        /// <summary>Sprite por estado (normal, ratón encima, pulsado, desactivado); sin sprite, ese estado usa el normal.</summary>
        static void SkinButton(Button selectable, Sprite[] sprites)
        {
            if (sprites[0] == null || selectable.targetGraphic is not Image image)
                return;

            SetSlicedSprite(image, sprites[0], keepColor: false);
            selectable.transition = Selectable.Transition.SpriteSwap;
            selectable.spriteState = new SpriteState
            {
                highlightedSprite = sprites[1],
                pressedSprite = sprites[2],
                disabledSprite = sprites[3],
            };
        }

        static void SetSlicedSprite(Image image, Sprite sprite, bool keepColor)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            if (!keepColor)
                image.color = Color.white;
        }

        /// <summary>Barra de recarga del jugador: fondo de 18×4, relleno de 16×2 dentro y destello al quedar lista.</summary>
        static void ApplyCooldownBar()
        {
            var back = ImportUISprite("HUD", "UI_Cooldown_Back");
            var fillSprite = ImportUISprite("HUD", "UI_Cooldown_Fill");
            var ready = ImportUISheet("HUD", "UI_Cooldown_Ready", cellWidth: 18);
            if (back == null || fillSprite == null)
                return;

            EditPrefab(PlayerSetup.PlayerPath, root =>
            {
                var bar = root.GetComponentInChildren<CooldownBar>(true);
                if (bar == null)
                {
                    Debug.LogWarning("ArtSetup: el jugador no tiene CooldownBar; créala con Primordia > Mutations.");
                    return;
                }

                var background = bar.transform.Find("Background").GetComponent<SpriteRenderer>();
                var fill = bar.transform.Find("Fill").GetComponent<SpriteRenderer>();
                foreach (var (renderer, sprite) in new[] { (background, back), (fill, fillSprite) })
                {
                    renderer.sprite = sprite;
                    renderer.color = Color.white;
                    renderer.transform.localPosition = Vector3.zero;
                    renderer.transform.localScale = Vector3.one;
                }

                bar.SetReadyFrames(ready, 0.05f);
            });
        }

        /// <summary>
        /// Prefab de la burbuja de mutación: sombra en el suelo y, flotando, la burbuja del linaje con el icono
        /// encima. Crea también los efectos de reventar de cada linaje.
        /// </summary>
        static GameObject CreateMutationPickupPrefab()
        {
            var art = Import(MutationItemsArt);
            if (art == null)
                return null;

            var shadow = ImportSprite($"{UISourceFolder}/Items/Item_Mutation_Shadow.png", $"{ItemsArtFolder}/Item_Mutation_Shadow.png", Vector4.zero);

            Directory.CreateDirectory(EffectPrefabsFolder);
            var bubbles = new RuntimeAnimatorController[LineageNames.Length];
            var bursts = new GameObject[LineageNames.Length];
            for (int i = 0; i < LineageNames.Length; i++)
            {
                art.Controllers.TryGetValue($"{LineageNames[i]}_Idle", out var idle);
                bubbles[i] = idle;
                bursts[i] = CreateEffectPrefab($"Effect_MutationBurst_{LineageNames[i]}", art, $"{LineageNames[i]}_Collect", sortingOrder: 12);
            }

            return EditPrefab(MutationPickupPath, root =>
            {
                // El círculo provisional de las escenas de prueba no se usa aquí.
                if (root.TryGetComponent<SpriteRenderer>(out var oldSprite))
                    Object.DestroyImmediate(oldSprite);

                var collider = GetOrAdd<CircleCollider2D>(root);
                collider.isTrigger = true;
                collider.radius = 0.6f;

                // Por debajo del jugador (10) y por encima del suelo.
                var shadowRenderer = PickupPart(root.transform, "Shadow", new Vector3(0f, -10f / PixelsPerUnit, 0f), sortingOrder: 3);
                shadowRenderer.sprite = shadow;

                var floating = root.transform.Find("Float") ?? new GameObject("Float").transform;
                floating.SetParent(root.transform, false);

                var bubble = PickupPart(floating, "Bubble", Vector3.zero, sortingOrder: 6);
                bubble.sprite = art.FirstSprites.TryGetValue($"{LineageNames[0]}_Idle", out var first) ? first : null;
                var animator = GetOrAdd<Animator>(bubble.gameObject);
                animator.runtimeAnimatorController = bubbles[0];

                var icon = PickupPart(floating, "Icon", Vector3.zero, sortingOrder: 7);

                GetOrAdd<MutationPickup>(root).ConfigureLook(floating, animator, icon, bubbles, bursts);
            }, create: true);
        }

        static SpriteRenderer PickupPart(Transform parent, string name, Vector3 position, int sortingOrder)
        {
            var part = parent.Find(name);
            if (part == null)
            {
                part = new GameObject(name).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = position;
            var renderer = GetOrAdd<SpriteRenderer>(part.gameObject);
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        static Sprite ImportUISprite(string folder, string file, float border) =>
            ImportUISprite(folder, file, new Vector4(border, border, border, border));

        static Sprite ImportUISprite(string folder, string file) => ImportUISprite(folder, file, Vector4.zero);

        /// <param name="border">Bordes 9-slice en píxeles: izquierda, abajo, derecha, arriba.</param>
        static Sprite ImportUISprite(string folder, string file, Vector4 border) =>
            ImportSprite($"{UISourceFolder}/{folder}/{file}.png", $"{UIArtFolder}/{file}.png", border);

        /// <summary>Copia la imagen (si está en la carpeta temporal) y la importa como un solo sprite de pixel art.</summary>
        static Sprite ImportSprite(string source, string target, Vector4 border)
        {
            if (!CopyArt(source, target))
                return null;

            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = border;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }

        /// <summary>Tira de cuadros sin animación propia (el código elige el cuadro).</summary>
        static Sprite[] ImportUISheet(string folder, string file, int cellWidth)
        {
            string target = $"{UIArtFolder}/{file}.png";
            return CopyArt($"{UISourceFolder}/{folder}/{file}.png", target) ? SliceSheet(target, cellWidth) : System.Array.Empty<Sprite>();
        }

        /// <summary>Copia de la carpeta temporal al proyecto. Sin original, vale la copia que ya hubiera.</summary>
        static bool CopyArt(string source, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(source))
            {
                File.Copy(source, target, overwrite: true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
                return true;
            }

            if (File.Exists(target))
                return true;

            Debug.LogWarning($"ArtSetup: falta {source}.");
            return false;
        }
    }
}

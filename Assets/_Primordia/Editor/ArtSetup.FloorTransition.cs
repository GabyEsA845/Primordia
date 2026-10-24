using System.Linq;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de la sala de paso: los temas Shore (intermedio) y Cambrian (piso 2) de docs/multimedia-temp/environment,
    /// generados con generar_tema_cambrico.py, y la fosa, su burbuja y el icono del mapa de
    /// docs/multimedia-temp/floor-transition (generar_fosa.py). Lo usa <see cref="FloorTransitionSetup"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        internal const string ShoreThemeName = "Shore";
        internal const string CambrianThemeName = "Cambrian";
        const string FloorTransitionSourceFolder = "docs/multimedia-temp/floor-transition";
        const string FloorTransitionArtFolder = "Assets/_Primordia/Art/FloorTransition";

        // Medidas de las hojas de generar_fosa.py: la fosa es de 7×5 tiles; la burbuja, de 8×8 píxeles.
        const int PitCellWidth = 112;
        const int BubbleCellWidth = 8;
        const int KeyCellWidth = 13;

        /// <summary>Sprites de la fosa, ya importados.</summary>
        internal sealed class TransitionPitArt
        {
            public Sprite[] Pit;
            public Sprite[] Bubble;
            public Sprite[] Key;
        }

        /// <summary>Importa los temas Shore y Cambrian. Cualquiera puede ser null si faltan sus tiles.</summary>
        internal static (RoomTheme mid, RoomTheme next) ImportTransitionThemes()
        {
            var missing = CreateMissingTile();
            bool shore = ImportTheme(ShoreThemeName, missing);
            bool cambrian = ImportTheme(CambrianThemeName, missing);
            AssetDatabase.SaveAssets();
            return (shore ? LoadTheme(ShoreThemeName) : null, cambrian ? LoadTheme(CambrianThemeName) : null);
        }

        static RoomTheme LoadTheme(string themeName) =>
            AssetDatabase.LoadAssetAtPath<RoomTheme>($"{EnvironmentArtFolder}/{themeName}/Theme_{themeName}.asset");

        /// <summary>Importa la fosa, la burbuja y la tecla E (la de la tienda). Null si no está la fosa.</summary>
        internal static TransitionPitArt ImportTransitionPitArt()
        {
            string pitPath = $"{FloorTransitionArtFolder}/Transition_Pit.png";
            if (!CopyArt($"{FloorTransitionSourceFolder}/Transition_Pit.png", pitPath))
                return null;

            string bubblePath = $"{FloorTransitionArtFolder}/Transition_Bubble.png";
            string keyPath = $"{ShopArtFolder}/UI_Key_E.png";
            return new TransitionPitArt
            {
                Pit = SliceSheet(pitPath, PitCellWidth),
                Bubble = CopyArt($"{FloorTransitionSourceFolder}/Transition_Bubble.png", bubblePath)
                    ? SliceSheet(bubblePath, BubbleCellWidth)
                    : System.Array.Empty<Sprite>(),
                Key = CopyArt($"{ShopSourceFolder}/UI_Key_E.png", keyPath)
                    ? SliceSheet(keyPath, KeyCellWidth, new Vector2(0.5f, 0f))
                    : System.Array.Empty<Sprite>(),
            };
        }

        // Cuerpos e iconos del piso 2 (docs/multimedia-temp/floor2-bodies: generar_cuerpos_piso2.py y generar_iconos_piso2.py).
        const string Floor2BodiesSourceFolder = "docs/multimedia-temp/floor2-bodies";
        const string Floor2BodiesArtFolder = CharactersFolder + "/Player/Floor2";
        const int Floor2BodyCellWidth = 24;

        // Metamorfosis (docs/multimedia-temp/metamorphosis: generar_metamorfosis.py).
        const string MetamorphosisSourceFolder = "docs/multimedia-temp/metamorphosis";
        const string MetamorphosisArtFolder = "Assets/_Primordia/Art/Metamorphosis";

        /// <summary>
        /// Arte de una Boca del piso 2: sus iconos, la criatura de la vista previa (de lado), las animaciones del cuerpo y
        /// la silueta de su metamorfosis.
        /// </summary>
        internal sealed class MouthArt
        {
            public Sprite Icon;
            public Sprite LargeIcon;
            public Sprite[] Idle;
            public Sprite[] Attack;
            public RuntimeAnimatorController Body;
            public Sprite[] Morph;
            public Sprite[] Reveal;
        }

        /// <summary>El abismo y la luz de la metamorfosis.</summary>
        internal sealed class MetamorphosisArt
        {
            public Sprite Background;
            public Sprite Rays;
            public Sprite[] Motes;
            public Sprite[] Aura;
            public Sprite[] Burst;
        }

        // Cuerpo de criatura del piso 2: los estados del jugador más el ataque. Esquiva de 5 cuadros en 0,25 s, como la
        // célula; ataque de 6 cuadros en unos 0,43 s (el golpe, en el tercero o el cuarto).
        static Character Floor2Body(string body) => new()
        {
            Name = $"Player_{body}",
            SourceFolder = $"{Floor2BodiesSourceFolder}/{body}",
            ArtFolder = $"{Floor2BodiesArtFolder}/{body}",
            DefaultState = "Idle_down",
            Timings = new[]
            {
                ("Idle", 6f, true), ("Move", 10f, true), ("Dodge", 20f, false), ("Attack", 14f, false), ("Hurt", 10f, false),
                ("Death", 10f, false),
            },
        };

        internal sealed class Floor2MouthArt
        {
            public MouthArt Cnidocytes;
            public MouthArt JawPlates;
        }

        internal static Floor2MouthArt ImportFloor2MouthArt() => new()
        {
            Cnidocytes = ImportMouthArt("Cnidocytes", "Jellyfish"),
            JawPlates = ImportMouthArt("JawPlates", "Placoderm"),
        };

        static MouthArt ImportMouthArt(string icon, string body)
        {
            Sprite Icon(int size) => ImportSprite($"{Floor2BodiesSourceFolder}/Icons/Icon_{icon}_{size}.png",
                $"{UIArtFolder}/Icon_{icon}_{size}.png", Vector4.zero);

            Sprite[] Sheet(string animation)
            {
                string file = $"Player_{body}_{animation}.png";
                string target = $"{Floor2BodiesArtFolder}/{body}/{file}";
                return CopyArt($"{Floor2BodiesSourceFolder}/{body}/{file}", target)
                    ? SliceSheet(target, Floor2BodyCellWidth)
                    : System.Array.Empty<Sprite>();
            }

            Sprite[] Metamorphosis(string kind)
            {
                string file = $"{kind}_{body}.png";
                string target = $"{MetamorphosisArtFolder}/{file}";
                return CopyArt($"{MetamorphosisSourceFolder}/{file}", target) ? SliceSheet(target, 32) : System.Array.Empty<Sprite>();
            }

            var animations = Import(Floor2Body(body));
            return new MouthArt
            {
                Icon = Icon(16), LargeIcon = Icon(32), Idle = Sheet("Idle_side"), Attack = Sheet("Attack_side"),
                Body = animations != null ? animations.Controller : null,
                Morph = Metamorphosis("Morph"), Reveal = Metamorphosis("Reveal"),
            };
        }

        internal static MetamorphosisArt ImportMetamorphosisArt()
        {
            Sprite[] Sheet(string file, int width)
            {
                string target = $"{MetamorphosisArtFolder}/{file}.png";
                return CopyArt($"{MetamorphosisSourceFolder}/{file}.png", target) ? SliceSheet(target, width) : System.Array.Empty<Sprite>();
            }

            Sprite Single(string file) =>
                ImportSprite($"{MetamorphosisSourceFolder}/{file}.png", $"{MetamorphosisArtFolder}/{file}.png", Vector4.zero);

            return new MetamorphosisArt
            {
                Background = Single("Abyss_Background"),
                Rays = Single("Abyss_Rays"),
                Motes = Sheet("Abyss_Mote", 4),
                Aura = Sheet("Metamorphosis_Aura", 64),
                Burst = Sheet("Metamorphosis_Burst", 96),
            };
        }

        /// <summary>Hueco para la criatura del jugador en el panel de victoria, bajo el título.</summary>
        internal static void EnsureVictoryCreature()
        {
            EditPrefab(UISetup.GameUIPath, root =>
            {
                var gameUI = root.GetComponent<GameUI>();
                var panel = root.transform.Find("VictoryPanel");
                if (gameUI == null || panel == null)
                    return;

                var creature = panel.Find("Creature");
                Image image;
                if (creature == null)
                {
                    image = UISetup.CreateIcon("Creature", panel, 72f);
                    image.preserveAspect = true;
                    image.enabled = true;
                    image.transform.SetSiblingIndex(Mathf.Min(1, panel.childCount - 1));
                    image.gameObject.SetActive(false);
                }
                else
                {
                    image = creature.GetComponent<Image>();
                }
                gameUI.SetVictoryCreature(image);
            });
        }

        /// <summary>Viste la ventana de Bocas: cartas como las de elección de mutación, franja e insignias de nivel.</summary>
        internal static void ApplyMouthChoiceSkin()
        {
            var card = new[]
            {
                ImportUISprite("Panels", "UI_Card_Normal", 12), ImportUISprite("Panels", "UI_Card_Highlighted", 12),
                ImportUISprite("Panels", "UI_Card_Pressed", 12), ImportUISprite("Panels", "UI_Card_Disabled", 12),
            };
            var stripe = ImportUISprite("Panels", "UI_Card_Stripe", new Vector4(4, 0, 4, 0));
            var badges = new[] { 1, 2, 3 }.Select(level => AssetDatabase.LoadAssetAtPath<Sprite>($"{UIArtFolder}/UI_Level_{level}.png"))
                .ToArray();

            EditPrefab(UISetup.GameUIPath, root =>
            {
                foreach (var window in root.GetComponentsInChildren<MouthChoiceUI>(true))
                    window.SetLevelBadges(badges);
                foreach (var mouthCard in root.GetComponentsInChildren<MouthCard>(true))
                {
                    SkinButton(mouthCard.Button, card);
                    var strip = mouthCard.transform.Find("ColorStrip");
                    if (strip != null && stripe != null)
                    {
                        SetSlicedSprite(strip.GetComponent<Image>(), stripe, keepColor: true);
                        strip.GetComponent<LayoutElement>().preferredHeight = stripe.rect.height;
                    }
                }
            });
        }

        /// <summary>Pone el icono de la sala de paso (UI_Map_Transition) en el minimapa y el mapa grande.</summary>
        internal static void ApplyTransitionMapIcon()
        {
            var icon = ImportSprite($"{FloorTransitionSourceFolder}/UI_Map_Transition.png", $"{MapArtFolder}/UI_Map_Transition.png",
                Vector4.zero);
            if (icon == null)
                return;

            EditPrefab(UISetup.GameUIPath, root =>
            {
                foreach (var map in root.GetComponentsInChildren<MapUI>(true))
                    map.SetTransitionIcon(icon);
            });
        }
    }
}

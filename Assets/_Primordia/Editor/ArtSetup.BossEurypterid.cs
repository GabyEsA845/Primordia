using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Euriptérido (docs/multimedia-temp/floor2-bosses, generado con generar_euripterido.py): sus animaciones con los ritmos
    /// del README, la púa venenosa, la muda vacía que deja (cobertura que se rompe a golpes), el barrido de cola, sus datos
    /// (300 de vida; se reconfiguran siempre), su barra de vida propia y su prefab, que se rehace cada vez (conserva el
    /// GUID). Lo usa <see cref="EurypteridBossSetup"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        const string EurypteridArtFolder = CharactersFolder + "/Enemies/Floor2/Eurypterid";
        internal const string EurypteridPrefabPath = EnemyPrefabsFolder + "/Boss_Eurypterid.prefab";
        const string EurypteridBarArtFolder = UIArtFolder + "/BossBar/Eurypterid";
        const string EurypteridBarSkinPath = BossIntroDataFolder + "/BossBar_Eurypterid.asset";

        // Celda de 128×128, cuadros del README de floor2-bosses (ms → cuadros por segundo). Las de la fase blanda van antes
        // que las de la acorazada: gana el primer prefijo que encaja ("Walk_Soft" antes que "Walk").
        static readonly Character EurypteridArt = new()
        {
            Name = "Eurypterid",
            SourceFolder = Floor2BossesSourceFolder + "/Eurypterid",
            ArtFolder = EurypteridArtFolder,
            DefaultState = EurypteridBoss.WalkState,
            Timings = new[]
            {
                ("Walk_Soft", Fps(60), true), ("Hurt_Soft", Fps(90), false), ("Walk", Fps(100), true),
                ("PincerWarn", Fps(150), false), ("PincerStrike", Fps(60), false), ("SpineWarn", Fps(150), false),
                ("SpineShoot", Fps(80), false), ("Hurt", Fps(90), false), ("Molt", Fps(250), false), ("TailWarn", Fps(150), false),
                ("TailSweep", Fps(50), false), ("DoublePincer", Fps(70), false), ("Death", Fps(140), false),
                ("Intro", IntroPoseFps, false),
                // Último aliento (generar_euripterido_final.py): "Downed_Idle" antes que "Downed".
                ("Downed_Idle", Fps(250), true), ("Downed", Fps(120), false), ("Rise", Fps(110), false), ("Fury", Fps(120), true),
            },
            // La muda vacía es una pieza aparte (cobertura), no una animación del jefe.
            SkipClips = { "Exuvia" },
        };

        // Cuerpo sólido: unas 5,6 u de largo con la cola; la cápsula cubre del abdomen a la cabeza (la cola fina no choca).
        static readonly Vector2 EurypteridColliderSize = new(4.4f, 1.8f);
        static readonly Vector2 EurypteridColliderOffset = new(0.4f, 0f);

        internal static Enemy CreateEurypterid()
        {
            var art = Import(EurypteridArt);
            if (art == null)
                return null;

            string effects = $"{EurypteridArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            var spine = CreateEurypteridSpine(ImportLooseSheet($"{Floor2BossesSourceFolder}/Effects/Proj_EurypteridSpine.png",
                $"{effects}/Proj_EurypteridSpine.png", 16));
            var exuvia = CreateEurypteridExuvia(ImportLooseSheet($"{Floor2BossesSourceFolder}/Eurypterid/Eurypterid_Exuvia.png",
                $"{effects}/Eurypterid_Exuvia.png", 128));
            // Gota venenosa del último aliento (12×12): la misma lógica que la púa, con otro dibujo y más alcance.
            var venom = CreateEurypteridVenom(ImportLooseSheet($"{Floor2BossesSourceFolder}/Effects/Proj_EurypteridVenom.png",
                $"{effects}/Proj_EurypteridVenom.png", 12));
            // La media luna del barrido, centrada en el jefe (128×128): gira con él.
            var tailSweep = ImportLooseSheet($"{Floor2BossesSourceFolder}/Effects/Fx_Eurypterid_TailSweep.png",
                $"{effects}/Fx_Eurypterid_TailSweep.png", 128);

            // 300 de vida; anda (no nada) y no hace daño por contacto: solo las pinzas y las púas.
            var data = Floor2EnemyData("Enemy_EurypteridBoss", d =>
            {
                d.Configure(EnemyBehaviour.Chaser, maxHealth: 300, moveSpeed: 2.5f, detectionRange: 0f, contactDamage: 0);
                d.ConfigureSwimming(false);
                // Como los demás jefes: una recompensa fija de biomasa al morir.
                d.ConfigureBiomass(0f, 0f, 10);
            });
            AssetDatabase.SaveAssets();
            var barSkin = CreateEurypteridBarSkin();

            var root = new GameObject("Boss_Eurypterid") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var body = new GameObject("Body") { layer = root.layer };
                body.transform.SetParent(root.transform, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = art.DefaultSprite;
                renderer.sortingOrder = 5;
                var animator = body.AddComponent<Animator>();
                animator.runtimeAnimatorController = art.Controller;
                // La colisión gira con el cuerpo: los disparos le dan en toda su largura y el jugador no lo atraviesa.
                var collider = body.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Horizontal;
                collider.size = EurypteridColliderSize;
                collider.offset = EurypteridColliderOffset;

                var rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                root.AddComponent<Health>();

                var enemy = root.AddComponent<Enemy>();
                SetFields(enemy, ("data", data));
                enemy.SetAnimationTimings(art.Length(EurypteridBoss.DeathState), 0f);

                var boss = root.AddComponent<EurypteridBoss>();
                boss.Configure(body.transform, animator, renderer, spine);
                boss.ConfigurePhases(exuvia, tailSweep);
                boss.ConfigureLastStand(venom);
                if (barSkin != null)
                    root.AddComponent<BossBarAppearance>().Configure(barSkin);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, EurypteridPrefabPath);
                return prefab.GetComponent<Enemy>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Muda vacía: el mismo dibujo y la misma colisión que el cuerpo, sólida en la capa Default (para a los dos bandos) y
        /// con 15 golpes de aguante.
        /// </summary>
        static EurypteridExuvia CreateEurypteridExuvia(Sprite[] frames)
        {
            var root = new GameObject("Exuvia_Eurypterid");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                // En el suelo: por debajo de los personajes, por encima de las rocas.
                renderer.sortingOrder = -4;
                var collider = root.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Horizontal;
                collider.size = EurypteridColliderSize;
                collider.offset = EurypteridColliderOffset;
                var health = root.AddComponent<Health>();
                SetFields(health, ("maxHealth", 15));
                root.AddComponent<EurypteridExuvia>().Configure(renderer, 15);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{EnemyPrefabsFolder}/Exuvia_Eurypterid.prefab")
                    .GetComponent<EurypteridExuvia>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Barra de vida propia del Euriptérido (floor2-bosses/BossBar, generar_barra_euripterido.py): el escorpión de
        /// 258×32 con la ventana de 200×8 en (38, 12), un marco por fase (acorazado verde, blando rosa y el del último
        /// aliento con la cola encendida), el caparazón agrietándose mientras muda (inmune), la placa del 50 % y el
        /// caparazón saltando en pedazos al pasar a la fase blanda. Rehace el asset conservando su GUID. Null si falta el arte.
        /// </summary>
        internal static BossBarSkin CreateEurypteridBarSkin()
        {
            const string prefix = "UI_BossBar_Eurypterid_";
            Sprite[] Sheet(string name, int cell) => ImportLooseSheet($"{Floor2BossesSourceFolder}/BossBar/{prefix}{name}.png",
                $"{EurypteridBarArtFolder}/{prefix}{name}.png", cell);
            Sprite One(string name, int cell) => Sheet(name, cell).FirstOrDefault();

            const int frameWidth = 258, windowWidth = 200;
            var armor = Sheet("Frame_Armor", frameWidth);
            if (armor.Length == 0)
            {
                Debug.LogWarning("ArtSetup: falta la barra del Euriptérido (floor2-bosses/generar_barra_euripterido.py); usará la barra lisa.");
                return null;
            }

            var phases = new[]
            {
                new BossBarSkin.PhaseLook { fill = Sheet("Fill_Armor", windowWidth), eyes = Sheet("Eyes_Armor", frameWidth), frame = armor },
                new BossBarSkin.PhaseLook { fill = Sheet("Fill_Soft", windowWidth), eyes = Sheet("Eyes_Soft", frameWidth), frame = Sheet("Frame_Soft", frameWidth) },
                new BossBarSkin.PhaseLook { fill = Sheet("Fill_Venom", windowWidth), eyes = Sheet("Eyes_Fury", frameWidth), frame = Sheet("Frame_Fury", frameWidth) },
            };
            // Mientras muda (inmune): el caparazón agrietado con las grietas que brillan.
            var molt = new BossBarSkin.PhaseLook { fill = Sheet("Fill_Molt", windowWidth), eyes = Sheet("Eyes_Molt", frameWidth), frame = Sheet("Frame_Crack", frameWidth) };

            Directory.CreateDirectory(BossIntroDataFolder);
            var skin = AssetDatabase.LoadAssetAtPath<BossBarSkin>(EurypteridBarSkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<BossBarSkin>();
                AssetDatabase.CreateAsset(skin, EurypteridBarSkinPath);
            }
            skin.Configure(armor[0], armor[0], new RectInt(38, 12, windowWidth, 8), One("Back", windowWidth),
                One("Fill_Trail", windowWidth), phases, molt, One("Notch", 8), One("Notch_Broken", 8), Sheet("Fx_NotchBreak", 16));
            skin.ConfigurePhaseChange(Sheet("Fx_ShellBurst", frameWidth), 0.08f);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        /// <summary>Gota venenosa del bullet hell: medio corazón y ralentiza, como la púa; vive lo bastante para cruzar la sala.</summary>
        static EurypteridSpine CreateEurypteridVenom(Sprite[] frames)
        {
            var root = new GameObject("Venom_Eurypterid") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = 20;
                if (frames.Length > 1)
                    root.AddComponent<Rooms.SpriteLoop>().Configure(renderer, frames, 0.15f, 0f);
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var drop = root.AddComponent<EurypteridSpine>();
                drop.Configure(1, 7f, 0.7f, 1.5f);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{EnemyPrefabsFolder}/Venom_Eurypterid.prefab")
                    .GetComponent<EurypteridSpine>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Púa venenosa (16×8, hacia la derecha): gira hacia donde vuela; no se puede disparar.</summary>
        static EurypteridSpine CreateEurypteridSpine(Sprite[] frames)
        {
            var root = new GameObject("Spine_Eurypterid") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = 20;
                if (frames.Length > 1)
                    root.AddComponent<Rooms.SpriteLoop>().Configure(renderer, frames, 0.08f, 0f);
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var spine = root.AddComponent<EurypteridSpine>();
                spine.Configure(1, 2.5f, 0.7f, 1.5f);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{EnemyPrefabsFolder}/Spine_Eurypterid.prefab")
                    .GetComponent<EurypteridSpine>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

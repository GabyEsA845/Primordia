using System.IO;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Anomalocaris (docs/multimedia-temp/floor2-bosses, generado con generar_anomalocaris.py y generar_sala_anomalocaris.py):
    /// sus animaciones con los ritmos del README, la burbuja que daña con su reventón, el tajo del zarpazo, sus datos (300 de
    /// vida, nada; se reconfiguran siempre, como los de los enemigos del piso 2) y su prefab, que se rehace cada vez (conserva
    /// el GUID). Lo usa <see cref="AnomalocarisBossSetup"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor2BossesSourceFolder = "docs/multimedia-temp/floor2-bosses";
        const string AnomalocarisArtFolder = CharactersFolder + "/Enemies/Floor2/Anomalocaris";
        internal const string AnomalocarisPrefabPath = EnemyPrefabsFolder + "/Boss_Anomalocaris.prefab";

        // Celda de 112×112, cuadros del README de floor2-bosses (ms → cuadros por segundo).
        static readonly Character AnomalocarisArt = new()
        {
            Name = "Anomalocaris",
            SourceFolder = Floor2BossesSourceFolder + "/Anomalocaris",
            ArtFolder = AnomalocarisArtFolder,
            DefaultState = AnomalocarisBoss.SwimState,
            Timings = new[]
            {
                ("SwimFast", Fps(70), true), ("Swim", Fps(110), true), ("ClawWarn", Fps(140), false), ("ClawStrike", Fps(60), false),
                ("Vulnerable", Fps(250), true), ("Charge", Fps(90), true), ("Hurt", Fps(90), false), ("EyesDark", Fps(220), true),
                ("Death", Fps(140), false), ("Intro", IntroPoseFps, false),
            },
        };

        // Cuerpo que hace daño y recibe golpes: unas 4,7 u de largo con la cola; la cápsula cubre del tronco a la cabeza.
        static readonly Vector2 AnomalocarisColliderSize = new(4f, 1.3f);
        static readonly Vector2 AnomalocarisColliderOffset = new(0.15f, 0f);

        internal static Enemy CreateAnomalocaris()
        {
            var art = Import(AnomalocarisArt);
            if (art == null)
                return null;

            string effects = $"{AnomalocarisArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            string fxSource = $"{Floor2BossesSourceFolder}/Effects";
            // El tajo está dibujado hacia la derecha con el origen en su borde izquierdo (0, 32): sale de la cabeza.
            var swipe = Floor2MouthsSetup.CreateFrameEffect("Effect_Anomalocaris_Swipe",
                ImportLooseSheet($"{fxSource}/Fx_Anomalocaris_Swipe.png", $"{effects}/Fx_Anomalocaris_Swipe.png", 64, new Vector2(0f, 0.5f)), 0.06f);
            var pop = Floor2MouthsSetup.CreateFrameEffect("Effect_AnomalocarisBubble_Pop",
                ImportLooseSheet($"{fxSource}/Fx_AnomalocarisBubble_Pop.png", $"{effects}/Fx_AnomalocarisBubble_Pop.png", 16), 0.06f);
            var bubble = CreateAnomalocarisBubble(
                ImportLooseSheet($"{fxSource}/Proj_AnomalocarisBubble.png", $"{effects}/Proj_AnomalocarisBubble.png", 16), pop);

            // 300 de vida; nada por encima de todo. El daño por contacto lo hace AnomalocarisBoss (no choca con el jugador).
            var data = Floor2EnemyData("Enemy_AnomalocarisBoss", d =>
            {
                d.Configure(EnemyBehaviour.Chaser, maxHealth: 300, moveSpeed: 5f, detectionRange: 0f, contactDamage: 0);
                d.ConfigureSwimming(true);
                // Como los jefes del piso 1: una recompensa fija de biomasa al morir.
                d.ConfigureBiomass(0f, 0f, 10);
            });
            AssetDatabase.SaveAssets();

            var root = new GameObject("Boss_Anomalocaris") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var body = new GameObject("Body") { layer = root.layer };
                body.transform.SetParent(root.transform, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = art.DefaultSprite;
                renderer.sortingOrder = 5;
                var animator = body.AddComponent<Animator>();
                animator.runtimeAnimatorController = art.Controller;
                // La colisión gira con el cuerpo: así los disparos le dan en toda su largura, mire adonde mire.
                var collider = body.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Horizontal;
                collider.size = AnomalocarisColliderSize;
                collider.offset = AnomalocarisColliderOffset;

                var rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                root.AddComponent<Health>();

                var enemy = root.AddComponent<Enemy>();
                SetFields(enemy, ("data", data));
                enemy.SetAnimationTimings(art.Length(AnomalocarisBoss.DeathState), 0f);

                root.AddComponent<AnomalocarisBoss>().Configure(body.transform, animator, renderer, bubble, swipe);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, AnomalocarisPrefabPath);
                return prefab.GetComponent<Enemy>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Burbuja que daña: 1 de vida en la capa Enemy (los disparos la revientan) y su reventón.</summary>
        static AnomalocarisBubble CreateAnomalocarisBubble(Sprite[] frames, GameObject pop)
        {
            var root = new GameObject("Bubble_Anomalocaris") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = 20;
                if (frames.Length > 1)
                    root.AddComponent<Rooms.SpriteLoop>().Configure(renderer, frames, 0.12f, 0f);

                root.AddComponent<Rigidbody2D>().gravityScale = 0f;
                root.AddComponent<CircleCollider2D>().radius = 0.28f;
                var health = root.AddComponent<Health>();
                SetFields(health, ("maxHealth", 1));
                var bubble = root.AddComponent<AnomalocarisBubble>();
                bubble.Configure(1, 4f, pop);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{EnemyPrefabsFolder}/Bubble_Anomalocaris.prefab")
                    .GetComponent<AnomalocarisBubble>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Arte compartido de las reacciones del piso 3 (fuego, metano, veneno): lo que el código necesita cargar en tiempo de
    /// juego. Lo crea Primordia > Floor 3 > Crear piso 3 y lo lleva el <see cref="FloorConfig"/>; <see cref="FloorGenerator"/>
    /// lo deja en <see cref="Current"/> al montar el piso.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Rooms/Reaction Kit", fileName = "ReactionKit_")]
    public class ReactionKit : ScriptableObject
    {
        [Header("Fuego")]
        [Tooltip("Fuego en el suelo (32×24, en bucle). Pivote: abajo.")]
        [SerializeField] Sprite[] groundFire = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float groundFireFrameSeconds = 0.1f;
        [Tooltip("El fuego se apaga (32×32). Pivote: abajo.")]
        [SerializeField] Sprite[] fireOut = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float fireOutFrameSeconds = 0.09f;
        [Tooltip("Marca sobre un enemigo que arde (efecto en bucle).")]
        [SerializeField] GameObject burningMarker;

        [Header("Metano")]
        [Tooltip("Explosión del odre (64×64, una pasada). Pivote: centro.")]
        [SerializeField] Sprite[] explosion = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float explosionFrameSeconds = 0.07f;
        [Tooltip("Casilla de niebla que se prende (32×24, una pasada). Pivote: abajo.")]
        [SerializeField] Sprite[] mistIgnite = System.Array.Empty<Sprite>();
        [Tooltip("Llamarada suelta al final del fogonazo (24×40). Pivote: abajo.")]
        [SerializeField] Sprite[] mistFlare = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float mistIgniteFrameSeconds = 0.07f;

        [Header("Veneno")]
        [Tooltip("Charco grande (48×48, centro): cae (una pasada), burbujea (bucle) y se seca (una pasada).")]
        [SerializeField] Sprite[] puddleAppear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] puddleIdle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] puddleFade = System.Array.Empty<Sprite>();
        [Tooltip("El fuego corre por el charco (48×48, una pasada). El cuadro 3 es el que suelta la nube.")]
        [SerializeField] Sprite[] puddleIgnite = System.Array.Empty<Sprite>();
        [Tooltip("Nube tóxica (48×64): nace, se mantiene (bucle) y se deshace. Pivote: la base de la columna.")]
        [SerializeField] Sprite[] cloudForm = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] cloudIdle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] cloudFade = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float cloudFrameSeconds = 0.11f;
        [Tooltip("Marca de gotas sobre quien está envenenado (efecto en bucle).")]
        [SerializeField] GameObject poisonedMarker;

        [Header("Escupitajo venenoso")]
        [Tooltip("Reventón de la bola (64×64, centro, una pasada). El cuadro 1 es el que daña.")]
        [SerializeField] Sprite[] spitBurst = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float spitBurstFrameSeconds = 0.06f;
        [Tooltip("Charco salpicado que deja el reventón (64×64, centro): cae, burbujea (bucle) y se seca. El cuerpo mide ~14 px de radio.")]
        [SerializeField] Sprite[] splatAppear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] splatIdle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] splatFade = System.Array.Empty<Sprite>();
        [Tooltip("Sellos de la banda de baba, 8 cuadros cada uno, una variante tras otra: 6 de 20×20 (borde y cuerpo).")]
        [SerializeField] Sprite[] slimeRim = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] slimeBody = System.Array.Empty<Sprite>();
        [Tooltip("Banda fina de la Camada: 5 variantes de 14×14.")]
        [SerializeField] Sprite[] slimeThinRim = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] slimeThinBody = System.Array.Empty<Sprite>();
        [Tooltip("Brillo húmedo (8×8, 4 cuadros en bucle).")]
        [SerializeField] Sprite[] slimeShine = System.Array.Empty<Sprite>();

        [Header("Sanguijuela (Escupitajo con el linaje Sanguijuela, floor3-lineage-art)")]
        [SerializeField] Sprite[] leechBurst = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSplatAppear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSplatIdle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSplatFade = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechRollSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechRollDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechRollUp = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSwellSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSwellDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSwellUp = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechMiniRollSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechMiniRollDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechMiniRollUp = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSlimeRim = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSlimeBody = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSlimeThinRim = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSlimeThinBody = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechSlimeShine = System.Array.Empty<Sprite>();
        [Tooltip("Coágulos de la banda: 3 variantes de 8 cuadros (10×10), los mismos índices que el sello.")]
        [SerializeField] Sprite[] leechClot = System.Array.Empty<Sprite>();
        [Tooltip("Gusanillo que nada por la banda (8 cuadros en bucle, 12×12) y cómo se hunde (3 cuadros).")]
        [SerializeField] Sprite[] leechWorm = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] leechWormSink = System.Array.Empty<Sprite>();

        [Header("Yesquero (Escupitajo con el linaje Yesquero, floor3-lineage-art)")]
        [SerializeField] Sprite[] tinderRollSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderRollDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderRollUp = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderSwellSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderSwellDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderSwellUp = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderMiniRollSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderMiniRollDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] tinderMiniRollUp = System.Array.Empty<Sprite>();
        [Tooltip("Reventón de brasas (64×64, centro, 7 cuadros). El cuadro 1 es el que daña.")]
        [SerializeField] Sprite[] tinderBurst = System.Array.Empty<Sprite>();
        [Tooltip("Marcas chamuscadas bajo cada fuego del rastro: 4 variantes de 8 cuadros (20×20).")]
        [SerializeField] Sprite[] tinderScorch = System.Array.Empty<Sprite>();

        /// <summary>Cuadros de cada sello de la banda de baba.</summary>
        public const int SlimeFrames = 8;

        public static ReactionKit Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        public Sprite[] GroundFire => groundFire ?? System.Array.Empty<Sprite>();
        public float GroundFireFrameSeconds => groundFireFrameSeconds;
        public Sprite[] FireOut => fireOut ?? System.Array.Empty<Sprite>();
        public float FireOutFrameSeconds => fireOutFrameSeconds;
        public GameObject BurningMarker => burningMarker;
        public Sprite[] PuddleAppear => puddleAppear ?? System.Array.Empty<Sprite>();
        public Sprite[] PuddleIdle => puddleIdle ?? System.Array.Empty<Sprite>();
        public Sprite[] PuddleFade => puddleFade ?? System.Array.Empty<Sprite>();
        public Sprite[] PuddleIgnite => puddleIgnite ?? System.Array.Empty<Sprite>();
        public Sprite[] SplatAppear => splatAppear ?? System.Array.Empty<Sprite>();
        public Sprite[] SplatIdle => splatIdle ?? System.Array.Empty<Sprite>();
        public Sprite[] SplatFade => splatFade ?? System.Array.Empty<Sprite>();
        public Sprite[] SlimeShine => slimeShine ?? System.Array.Empty<Sprite>();

        /// <summary>Hay arte de la banda de baba (normal y fina).</summary>
        public bool HasSlime => slimeBody != null && slimeBody.Length >= SlimeFrames && slimeRim != null && slimeRim.Length >= SlimeFrames
            && slimeThinBody != null && slimeThinBody.Length >= SlimeFrames && slimeThinRim != null && slimeThinRim.Length >= SlimeFrames;

        /// <summary>Variantes de sello de la banda normal o de la fina.</summary>
        public int SlimeVariants(bool thin) => (thin ? slimeThinBody : slimeBody).Length / SlimeFrames;

        public Sprite SlimeRimFrame(bool thin, int variant, int frame) => (thin ? slimeThinRim : slimeRim)[variant * SlimeFrames + frame];
        public Sprite SlimeBodyFrame(bool thin, int variant, int frame) => (thin ? slimeThinBody : slimeBody)[variant * SlimeFrames + frame];
        public Sprite[] SpitBurst => spitBurst ?? System.Array.Empty<Sprite>();

        // ---- Yesquero ----
        public Sprite[] TinderRollSide => Safe(tinderRollSide);
        public Sprite[] TinderRollDown => Safe(tinderRollDown);
        public Sprite[] TinderRollUp => Safe(tinderRollUp);
        public Sprite[] TinderSwellSide => Safe(tinderSwellSide);
        public Sprite[] TinderSwellDown => Safe(tinderSwellDown);
        public Sprite[] TinderSwellUp => Safe(tinderSwellUp);
        public Sprite[] TinderMiniRollSide => Safe(tinderMiniRollSide);
        public Sprite[] TinderMiniRollDown => Safe(tinderMiniRollDown);
        public Sprite[] TinderMiniRollUp => Safe(tinderMiniRollUp);
        public Sprite[] TinderBurst => Safe(tinderBurst);
        public Sprite[] TinderScorch => Safe(tinderScorch);

        /// <summary>Cuadros de cada marca chamuscada.</summary>
        public const int ScorchFrames = 8;

        /// <summary>Hay arte de la marca chamuscada.</summary>
        public bool HasTinderScorch => Safe(tinderScorch).Length >= ScorchFrames;

        internal void ConfigureTinderBall(Sprite[] rollSide, Sprite[] rollDown, Sprite[] rollUp, Sprite[] swellSide, Sprite[] swellDown,
            Sprite[] swellUp, Sprite[] miniSide, Sprite[] miniDown, Sprite[] miniUp)
        {
            tinderRollSide = rollSide;
            tinderRollDown = rollDown;
            tinderRollUp = rollUp;
            tinderSwellSide = swellSide;
            tinderSwellDown = swellDown;
            tinderSwellUp = swellUp;
            tinderMiniRollSide = miniSide;
            tinderMiniRollDown = miniDown;
            tinderMiniRollUp = miniUp;
        }

        internal void ConfigureTinderEffects(Sprite[] burst, Sprite[] scorch)
        {
            tinderBurst = burst;
            tinderScorch = scorch;
        }

        // ---- Sanguijuela ----
        static Sprite[] Safe(Sprite[] frames) => frames ?? System.Array.Empty<Sprite>();
        public Sprite[] LeechBurst => Safe(leechBurst);
        public Sprite[] LeechSplatAppear => Safe(leechSplatAppear);
        public Sprite[] LeechSplatIdle => Safe(leechSplatIdle);
        public Sprite[] LeechSplatFade => Safe(leechSplatFade);
        public Sprite[] LeechRollSide => Safe(leechRollSide);
        public Sprite[] LeechRollDown => Safe(leechRollDown);
        public Sprite[] LeechRollUp => Safe(leechRollUp);
        public Sprite[] LeechSwellSide => Safe(leechSwellSide);
        public Sprite[] LeechSwellDown => Safe(leechSwellDown);
        public Sprite[] LeechSwellUp => Safe(leechSwellUp);
        public Sprite[] LeechMiniRollSide => Safe(leechMiniRollSide);
        public Sprite[] LeechMiniRollDown => Safe(leechMiniRollDown);
        public Sprite[] LeechMiniRollUp => Safe(leechMiniRollUp);
        public Sprite[] LeechSlimeShine => Safe(leechSlimeShine);
        public Sprite[] LeechClot => Safe(leechClot);
        public Sprite[] LeechWorm => Safe(leechWorm);
        public Sprite[] LeechWormSink => Safe(leechWormSink);

        /// <summary>Hay arte de la banda de baba de la Sanguijuela (normal y fina).</summary>
        public bool HasLeechSlime => Safe(leechSlimeBody).Length >= SlimeFrames && Safe(leechSlimeRim).Length >= SlimeFrames
            && Safe(leechSlimeThinBody).Length >= SlimeFrames && Safe(leechSlimeThinRim).Length >= SlimeFrames;

        /// <summary>Hay arte del charco salpicado de la Sanguijuela.</summary>
        public bool HasLeechSplat => Safe(leechSplatIdle).Length > 0 && Safe(leechSplatAppear).Length > 0 && Safe(leechSplatFade).Length > 0;

        /// <summary>Variantes de sello de la banda de la Sanguijuela, normal o fina.</summary>
        public int LeechSlimeVariants(bool thin) => (thin ? Safe(leechSlimeThinBody) : Safe(leechSlimeBody)).Length / SlimeFrames;

        public Sprite LeechSlimeRimFrame(bool thin, int variant, int frame) => (thin ? leechSlimeThinRim : leechSlimeRim)[variant * SlimeFrames + frame];
        public Sprite LeechSlimeBodyFrame(bool thin, int variant, int frame) => (thin ? leechSlimeThinBody : leechSlimeBody)[variant * SlimeFrames + frame];

        internal void ConfigureLeechSpit(Sprite[] burst, Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            leechBurst = burst;
            leechSplatAppear = appear;
            leechSplatIdle = idle;
            leechSplatFade = fade;
        }

        internal void ConfigureLeechBall(Sprite[] rollSide, Sprite[] rollDown, Sprite[] rollUp, Sprite[] swellSide, Sprite[] swellDown,
            Sprite[] swellUp, Sprite[] miniSide, Sprite[] miniDown, Sprite[] miniUp)
        {
            leechRollSide = rollSide;
            leechRollDown = rollDown;
            leechRollUp = rollUp;
            leechSwellSide = swellSide;
            leechSwellDown = swellDown;
            leechSwellUp = swellUp;
            leechMiniRollSide = miniSide;
            leechMiniRollDown = miniDown;
            leechMiniRollUp = miniUp;
        }

        internal void ConfigureLeechSlime(Sprite[] rim, Sprite[] body, Sprite[] thinRim, Sprite[] thinBody, Sprite[] shine, Sprite[] clot,
            Sprite[] worm, Sprite[] wormSink)
        {
            leechSlimeRim = rim;
            leechSlimeBody = body;
            leechSlimeThinRim = thinRim;
            leechSlimeThinBody = thinBody;
            leechSlimeShine = shine;
            leechClot = clot;
            leechWorm = worm;
            leechWormSink = wormSink;
        }
        public float SpitBurstFrameSeconds => spitBurstFrameSeconds;
        public Sprite[] CloudForm => cloudForm ?? System.Array.Empty<Sprite>();
        public Sprite[] CloudIdle => cloudIdle ?? System.Array.Empty<Sprite>();
        public Sprite[] CloudFade => cloudFade ?? System.Array.Empty<Sprite>();
        public float CloudFrameSeconds => cloudFrameSeconds;
        public GameObject PoisonedMarker => poisonedMarker;
        public Sprite[] Explosion => explosion ?? System.Array.Empty<Sprite>();
        public float ExplosionFrameSeconds => explosionFrameSeconds;
        public Sprite[] MistIgnite => mistIgnite ?? System.Array.Empty<Sprite>();
        public Sprite[] MistFlare => mistFlare ?? System.Array.Empty<Sprite>();
        public float MistIgniteFrameSeconds => mistIgniteFrameSeconds;

        internal void ConfigurePoison(Sprite[] appear, Sprite[] idle, Sprite[] fade, Sprite[] ignite, Sprite[] form, Sprite[] cloudLoop,
            Sprite[] cloudEnd, GameObject newPoisonedMarker)
        {
            puddleAppear = appear;
            puddleIdle = idle;
            puddleFade = fade;
            puddleIgnite = ignite;
            cloudForm = form;
            cloudIdle = cloudLoop;
            cloudFade = cloudEnd;
            poisonedMarker = newPoisonedMarker;
        }

        internal void ConfigureSpit(Sprite[] burst, Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            spitBurst = burst;
            splatAppear = appear;
            splatIdle = idle;
            splatFade = fade;
        }

        internal void ConfigureSlime(Sprite[] rim, Sprite[] body, Sprite[] thinRim, Sprite[] thinBody, Sprite[] shine)
        {
            slimeRim = rim;
            slimeBody = body;
            slimeThinRim = thinRim;
            slimeThinBody = thinBody;
            slimeShine = shine;
        }

        internal void ConfigureMethane(Sprite[] newExplosion, Sprite[] newMistIgnite, Sprite[] newMistFlare)
        {
            explosion = newExplosion;
            mistIgnite = newMistIgnite;
            mistFlare = newMistFlare;
        }

        internal void ConfigureFire(Sprite[] newGroundFire, Sprite[] newFireOut, GameObject newBurningMarker)
        {
            groundFire = newGroundFire;
            fireOut = newFireOut;
            burningMarker = newBurningMarker;
        }
    }
}

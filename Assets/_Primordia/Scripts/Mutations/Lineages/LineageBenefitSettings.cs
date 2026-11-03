using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Valores y efectos de los beneficios de los linajes (los usa <see cref="PlayerLineage"/>).</summary>
    [Serializable]
    public class LineageBenefitSettings
    {
        [Header("Carnívoro: Rematar")]
        [Tooltip("Un enemigo con esta fracción de su vida o menos cuenta como herido.")]
        [Range(0.05f, 1f)] public float woundedThreshold = 0.5f;
        [Min(1f)] public float finisherMultiplier = 2f;
        [Tooltip("Contra el jefe.")]
        [Min(1f)] public float bossFinisherMultiplier = 1.5f;
        [Tooltip("Sangre en el agua (Tiburón): efecto al recargar la esquiva.")]
        public GameObject finishEffect;

        [Header("Herbívoro: Pastar biomasa")]
        [Min(1)] public int biomassPerHeal = 10;
        public GameObject healEffect;

        [Header("Venenoso: Toxina débil")]
        [Min(1f)] public float toxinMultiplier = 1.5f;

        [Header("Piso 2: clases remodeladas")]
        [Tooltip("Pastar biomasa en el piso 2: biomasa por medio corazón.")]
        [Min(1)] public int biomassPerHealFloor2 = 12;
        [Tooltip("Pastar biomasa en el piso 2: medios corazones verdes que se pueden guardar.")]
        [Min(1)] public int maxBarrierFloor2 = 2;
        [Tooltip("Toxina débil en el piso 2: velocidad de los envenenados (0,8 = un 20 % más lentos).")]
        [Range(0.05f, 1f)] public float toxinSlowFactor = 0.8f;

        [Header("Carroñero: Carroña (piso 2)")]
        [Tooltip("Restos que deja cada enemigo al morir.")]
        public CarrionRemains remainsPrefab;
        [Tooltip("Restos dobles (Doble ración de la Hallucigenia).")]
        public CarrionRemains doubleRemainsPrefab;
        [Min(0.1f)] public float remainsDuration = 4f;
        [Tooltip("Daño de más por cada vez acumulada (0,2 = +20 %).")]
        [Min(0f)] public float carrionBonusPerStack = 0.2f;
        [Min(0.1f)] public float carrionDuration = 5f;
        [Min(1)] public int carrionMaxStacks = 3;
        [Tooltip("Al comer unos restos: los trozos suben hacia el jugador.")]
        public GameObject eatEffect;
        [Tooltip("Marca sobre el jugador con 1, 2 y 3 colmillos.")]
        public GameObject[] carrionMarkers = new GameObject[3];

        [Header("Piso 3: clases remodeladas")]
        [Tooltip("Toxina en el piso 3: segundos que dura el charco que deja el envenenado al morir.")]
        [Min(0.5f)] public float toxinPuddleSeconds = 6f;
        [Tooltip("Colonizar en el piso 3: máximo de matas en la sala cuando brotan otras al morir un enemigo encima.")]
        [Min(1)] public int matSproutLimit = 6;

        [Header("Parásito: Chupar (piso 3)")]
        [Tooltip("Daño a un enemigo para pegarle un parásito.")]
        [Min(1)] public int parasiteDamageNeeded = 15;
        [Tooltip("Vida que quita el parásito cada segundo.")]
        [Min(1)] public int parasiteDamagePerSecond = 1;
        [Tooltip("Vida chupada por cada medio corazón que cura, y con un jefe.")]
        [Min(1)] public int parasiteDrainPerHeal = 15;
        [Min(1)] public int parasiteBossDrainPerHeal = 30;
        [Tooltip("Parásitos vivos a la vez, entre todos los enemigos.")]
        [Min(1)] public int parasiteLimit = 3;
        [Tooltip("Marca sobre el enemigo que lleva un parásito, efecto del salto a otro, gota de vida que sube al jugador con cada tic y efecto de curar.")]
        public GameObject parasiteMarker;
        public GameObject parasiteJumpEffect;
        public GameObject parasiteDrainDrop;
        public GameObject parasiteHealEffect;

        [Header("Hongo: matas del piso 3 (Colonizar)")]
        [Tooltip("Las 4 especies de mata (Campanillas, Bejines, Estrellas de tierra, Mazas) en tamaño mediano y grande.")]
        public MyceliumPatch[] floor3MatsMedium = System.Array.Empty<MyceliumPatch>();
        public MyceliumPatch[] floor3MatsLarge = System.Array.Empty<MyceliumPatch>();
        [Tooltip("Radio de la mata grande respecto a la mediana (27 px / 19 px del dibujo).")]
        [Min(1f)] public float floor3LargeMatScale = 1.42f;

        [System.Serializable]
        public struct FrameSet
        {
            public Sprite[] frames;
        }

        [Header("Cordyceps: Zombi")]
        [Tooltip("Segundos que dura el aliado levantado.")]
        [Min(0.5f)] public float zombieSeconds = 4f;
        [Tooltip("Hongos del lomo: 4 variantes (brota y se mece), la estela de esporas y el levantarse y caer.")]
        public FrameSet[] zombieFungusSprout = System.Array.Empty<FrameSet>();
        public FrameSet[] zombieFungusIdle = System.Array.Empty<FrameSet>();
        public Sprite[] zombieSporeTrail = System.Array.Empty<Sprite>();
        public Sprite[] zombieRise = System.Array.Empty<Sprite>();
        public Sprite[] zombieCrumble = System.Array.Empty<Sprite>();

        [Header("Toques del piso 3")]
        [Tooltip("Charca de sanguijuelas: daño por segundo de más de los charcos y la baba del jugador.")]
        [Min(0)] public int leechPuddleBonus = 1;
        [Tooltip("Cien patas (Ciempiés): segundos que dura el charco de veneno de cada aterrizaje.")]
        [Min(0.5f)] public float centipedePuddleSeconds = 4f;
        [Tooltip("Ventosa giratoria (Lamprea): máximo de parásitos mientras gira, y el parásito grande (marca y salto).")]
        [Min(1)] public int lampreySpinLimit = 6;
        public GameObject parasiteMarkerLamprey;
        public GameObject parasiteLeapLamprey;

        [Tooltip("Aguijón de gas (Pulmonoscorpius): el chorro venenoso (dibujo del chorro, impacto, salida y salpicaduras) y el veneno que pone.")]
        public Primordia.Combat.JetArt poisonJetArt;
        public GameObject poisonJetHit;
        public GameObject poisonJetMuzzle;
        public GameObject[] poisonJetSplashes = System.Array.Empty<GameObject>();
        [Min(1)] public int jetPoisonDamage = 1;
        [Min(0.5f)] public float jetPoisonSeconds = 3f;

        [Header("Toques del piso 2")]
        [Tooltip("Festín (Placodermo): vida que cura rematar con un mordisco (1 = medio corazón) y cada cuánto como mucho.")]
        [Min(1)] public int feastHeal = 1;
        [Min(0f)] public float feastCooldown = 6f;
        [Tooltip("Cinco ojos (Opabinia): segundos de más de los Ojos compuestos, y cuánto los alarga cada remate.")]
        [Min(0f)] public float fiveEyesBonus = 1f;
        [Min(0f)] public float fiveEyesPerFinish = 0.5f;
        [Tooltip("Pólipos urticantes y Lecho urticante: veneno de las larvas y de las púas (daño por segundo y segundos).")]
        [Min(1)] public int touchPoisonDamage = 1;
        [Min(0.1f)] public float touchPoisonDuration = 3f;
        [Tooltip("Cosecha (Wiwaxia): enemigos distintos que tiene que alcanzar una tanda de escleritos.")]
        [Min(1)] public int harvestTargets = 3;

        [Header("Hongo: Colonizar la sala")]
        public MyceliumPatch matPrefab;
        [Tooltip("Colonia grande (Moho): la mata grande.")]
        public MyceliumPatch largeMatPrefab;
        [Min(0)] public int matCount = 3;
        [Min(0)] public int largeMatCount = 5;
        [Min(0.1f)] public float matRadius = 1.2f;
        [Tooltip("Colonia grande: cuánto más grandes (el radio).")]
        [Min(1f)] public float largeMatScale = 1.3f;
        [Tooltip("Velocidad de los enemigos encima (0,6 = un 40 % más lentos).")]
        [Range(0.05f, 1f)] public float matSlowFactor = 0.6f;
        [Min(0)] public int matDamagePerSecond = 1;

        [Header("Tortuga: Retraerse")]
        [Min(0f)] public float retractDuration = 1.5f;
        public GameObject retractEffect;

        [Header("Medusa: Estela urticante")]
        public MyceliumPatch trailPrefab;
        [Min(0.1f)] public float trailRadius = 0.45f;
        [Min(0.1f)] public float trailDuration = 3f;
        [Min(0.1f)] public float trailSpacing = 0.5f;
        [Min(1)] public int trailPoisonDamage = 1;
        [Min(0.1f)] public float trailPoisonDuration = 3f;
        public GameObject poisonEffect;

        [Header("Escorpión: Picadura oculta")]
        [Min(1f)] public float hiddenStingMultiplier = 3f;

        [Header("Cuesco de lobo: Bocanada al golpe")]
        [Min(0.1f)] public float puffRadius = 2f;
        [Min(0f)] public float puffCooldown = 6f;
    }
}

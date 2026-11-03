using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Patas de salto (Extremidades, piso 3): la esquiva es un salto alto de 4 u (5 u en el nivel II, 6 u en el III). En el aire
    /// eres invulnerable y pasas por encima de enemigos, proyectiles, lodo, charcos y obstáculos bajos (las paredes y los árboles
    /// altos te paran); saltar saca del lodo profundo. Al caer esparces los charcos de veneno. Nivel II: el aterrizaje hace 3 de
    /// daño en radio 1,6. Nivel III (Salto cargado): mantén el botón de esquiva al menos 1 s y un marcador muestra dónde
    /// caerás, más lejos cuanto más aguantes y hacia donde apuntes; al soltarlo saltas ahí y el impacto daña, empuja y aturde.
    /// Un toque sigue siendo un salto normal. Ver <see cref="JumpingLegs"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Jumping Legs", fileName = "Mutation_JumpingLegs")]
    public class JumpingLegsMutation : MutationData
    {
        // La esquiva de siempre mide 3 u y dura 0,25 s: las patas suman la diferencia.
        const float BaseDistance = 3f;
        const float BaseDuration = 0.25f;

        [Tooltip("Distancia del salto, por nivel (I, II, III).")]
        [SerializeField] float[] distances = { 4f, 5f, 6f };
        [Tooltip("Daño del aterrizaje, por nivel.")]
        [SerializeField] int[] landDamage = { 0, 3, 3 };
        [Tooltip("Radio del aterrizaje (daño y charcos), por nivel.")]
        [SerializeField] float[] landRadii = { 1.2f, 1.6f, 1.6f };

        [Header("Salto cargado (nivel III)")]
        [Tooltip("Segundos que hay que mantener el botón para empezar a cargar.")]
        [SerializeField, Min(0.1f)] float minHold = 1f;
        [Tooltip("Segundos de carga, desde el mínimo, hasta el alcance máximo.")]
        [SerializeField, Min(0.1f)] float chargeSeconds = 1.5f;
        [SerializeField, Min(0.5f)] float chargedMinDistance = 3f;
        [SerializeField, Min(0.5f)] float chargedMaxDistance = 8f;
        [SerializeField, Min(0)] int chargedDamage = 6;
        [SerializeField, Min(0.1f)] float chargedRadius = 2.2f;
        [SerializeField, Min(0f)] float chargedStun = 1.2f;
        [Tooltip("Velocidad con que el impacto empuja a los enemigos.")]
        [SerializeField, Min(0f)] float chargedPush = 9f;

        [Header("Efectos")]
        [SerializeField] Sprite[] takeoffFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] landFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] quakeFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] stunnedFrames = System.Array.Empty<Sprite>();
        [Tooltip("Marcador del salto cargado: diana (6 cuadros), diana al máximo (6) y las 4 esquinas del radio del impacto.")]
        [SerializeField] Sprite[] markerSpotFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] markerFullFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] markerCornerFrames = System.Array.Empty<Sprite>();

        public float DistanceAt(int level) => distances[Mathf.Clamp(level - 1, 0, distances.Length - 1)];
        public int DamageAt(int level) => landDamage[Mathf.Clamp(level - 1, 0, landDamage.Length - 1)];
        public float RadiusAt(int level) => landRadii[Mathf.Clamp(level - 1, 0, landRadii.Length - 1)];

        /// <summary>Cuánto dura el salto de esa distancia: crece con la raíz, para que los saltos largos no sean un latigazo.</summary>
        public static float DurationFor(float distance) => BaseDuration * Mathf.Sqrt(Mathf.Max(distance, 0.1f) / BaseDistance);

        public override void Apply(PlayerMutations player, int level)
        {
            var controller = player.Controller;
            float distance = DistanceAt(level);
            controller.DodgeDistance += distance - BaseDistance;
            controller.DodgeDuration += DurationFor(distance) - BaseDuration;
            // Saltar saca del lodo profundo.
            if (player.TryGetComponent<Rooms.PlayerMudSink>(out var sink))
                sink.DodgeResetsSink = true;

            bool charged = level >= MaxLevel;
            controller.ManualDodge = charged;
            var settings = new JumpingLegs.Settings
            {
                landDamage = DamageAt(level),
                landRadius = RadiusAt(level),
                charged = charged,
                minHold = minHold,
                chargeSeconds = chargeSeconds,
                chargedMinDistance = chargedMinDistance,
                chargedMaxDistance = chargedMaxDistance,
                chargedDamage = chargedDamage,
                chargedRadius = chargedRadius,
                chargedStun = chargedStun,
                chargedPush = chargedPush,
            };
            player.AddBehaviour<JumpingLegs>(Slot).Configure(controller, settings, takeoffFrames, landFrames, quakeFrames, stunnedFrames,
                markerSpotFrames, markerFullFrames, markerCornerFrames);
        }

        internal void ConfigureMarker(Sprite[] spot, Sprite[] full, Sprite[] corners)
        {
            markerSpotFrames = spot;
            markerFullFrames = full;
            markerCornerFrames = corners;
        }

        public override void Remove(PlayerMutations player, int level)
        {
            var controller = player.Controller;
            float distance = DistanceAt(level);
            controller.DodgeDistance -= distance - BaseDistance;
            controller.DodgeDuration -= DurationFor(distance) - BaseDuration;
            controller.ManualDodge = false;
            if (player.TryGetComponent<Rooms.PlayerMudSink>(out var sink))
                sink.DodgeResetsSink = false;
        }

        internal void ConfigureLook(Sprite[] takeoff, Sprite[] land, Sprite[] quake, Sprite[] stunned)
        {
            takeoffFrames = takeoff;
            landFrames = land;
            quakeFrames = quake;
            stunnedFrames = stunned;
        }
    }
}

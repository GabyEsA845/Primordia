using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Patas hilanderas (Extremidades, piso 3): la esquiva (de 4 u, 5 u en los niveles II y III) tiende un hilo entre donde empiezas
    /// y donde acabas, que frena a los enemigos y daña a cada uno que lo toca. Nivel I: un hilo de 7 s; nivel II: hilos de 9 s con más
    /// daño y hasta 2 a la vez. Nivel III (Red): los hilos ya no desaparecen; al haber 3 en la sala, sus extremos se unen y una
    /// telaraña cubre todo lo de dentro, que recibe el daño del nivel II y queda inmovilizado 3 s. Ver <see cref="SilkThreads"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Spinner Legs", fileName = "Mutation_SpinnerLegs")]
    public class SpinnerLegsMutation : MutationData
    {
        // La esquiva de siempre mide 3 u y dura 0,25 s: las patas suman la diferencia.
        const float BaseDistance = 3f;
        const float BaseDuration = 0.25f;

        [Tooltip("Distancia de la esquiva, por nivel (I, II, III).")]
        [SerializeField] float[] distances = { 4f, 5f, 5f };
        [Tooltip("Hilos que pueden estar tendidos a la vez, por nivel. En el nivel III son los que forman la red.")]
        [SerializeField] int[] threadLimits = { 1, 2, 3 };
        [Tooltip("Segundos que dura cada hilo, por nivel. 0 = no desaparece (nivel III).")]
        [SerializeField] float[] threadLifetimes = { 7f, 9f, 0f };
        [Tooltip("Daño que hace el hilo a cada enemigo que lo toca (una vez), por nivel. En el nivel III es el de la red.")]
        [SerializeField] int[] damages = { 3, 4, 4 };
        [Tooltip("Esquivas más cortas que esto no tienden hilo.")]
        [SerializeField, Min(0f)] float minLength = 0.8f;
        [Header("Red (nivel III)")]
        [Tooltip("Segundos que se queda la red y que inmoviliza a los atrapados.")]
        [SerializeField, Min(0.5f)] float netSeconds = 4f;
        [SerializeField, Min(0f)] float netHoldSeconds = 3f;
        [Header("Efectos")]
        [SerializeField] Sprite[] startFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] threadFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] anchorFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] hitFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] webAppear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] webIdle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] webFade = System.Array.Empty<Sprite>();

        // Una lista vacía (un asset guardado con otra versión) no rompe: da 0 hasta que se rehaga con el menú «Crear…».
        static int At(int[] values, int level) =>
            values == null || values.Length == 0 ? 0 : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];
        static float At(float[] values, int level) =>
            values == null || values.Length == 0 ? 0f : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        public float DistanceAt(int level) => At(distances, level);
        public int MaxThreadsAt(int level) => Mathf.Max(1, At(threadLimits, level));
        public float ThreadSecondsAt(int level) => At(threadLifetimes, level);
        public int DamageAt(int level) => At(damages, level);

        public override void Apply(PlayerMutations player, int level)
        {
            var controller = player.Controller;
            float distance = DistanceAt(level);
            controller.DodgeDistance += distance - BaseDistance;
            controller.DodgeDuration += JumpingLegsMutation.DurationFor(distance) - BaseDuration;
            player.AddBehaviour<SilkThreads>(Slot).Configure(controller, MaxThreadsAt(level), ThreadSecondsAt(level), DamageAt(level),
                minLength, level >= MaxLevel, netSeconds, netHoldSeconds,
                startFrames, threadFrames, anchorFrames, hitFrames, webAppear, webIdle, webFade);
        }

        public override void Remove(PlayerMutations player, int level)
        {
            var controller = player.Controller;
            float distance = DistanceAt(level);
            controller.DodgeDistance -= distance - BaseDistance;
            controller.DodgeDuration -= JumpingLegsMutation.DurationFor(distance) - BaseDuration;
        }

        internal void ConfigureLook(Sprite[] start, Sprite[] thread, Sprite[] anchor, Sprite[] hit, Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            startFrames = start;
            threadFrames = thread;
            anchorFrames = anchor;
            hitFrames = hit;
            webAppear = appear;
            webIdle = idle;
            webFade = fade;
        }
    }
}

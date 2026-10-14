using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Camuflaje (Piel). Nivel I: oculto tras 3 s quieto de ataques y golpes, siguiente ataque ×2. Nivel II: a los
    /// 2 s y ×3. Nivel III (Pánico): además, matar desde el camuflaje asusta a los enemigos de la sala 2 s.
    /// Ver <see cref="Camouflage"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Camouflage", fileName = "Mutation_Camouflage")]
    public class CamouflageMutation : MutationData
    {
        [Tooltip("Segundos sin atacar ni recibir golpes para ocultarse, por nivel (I, II, III).")]
        [SerializeField] float[] delays = { 3f, 2f, 2f };
        [Tooltip("Multiplicador de daño del ataque que sale del camuflaje, por nivel.")]
        [SerializeField] float[] damageMultipliers = { 2f, 3f, 3f };
        [Tooltip("Transparencia del jugador oculto (0 = invisible).")]
        [SerializeField, Range(0f, 1f)] float hiddenAlpha = 0.3f;
        [Tooltip("Distancia a la que un enemigo empieza a sospechar, y segundos que tarda en descubrirlo.")]
        [SerializeField, Min(0f)] float noticeRange = 2.5f;
        [SerializeField, Min(0.1f)] float noticeTime = 1.5f;

        [Header("Nivel III: pánico")]
        [SerializeField, Min(0f)] float panicDuration = 2f;
        [Tooltip("Fracción de su velocidad con la que se alejan los enemigos asustados.")]
        [SerializeField, Range(0f, 1f)] float panicSpeed = 0.35f;
        [Tooltip("Radio en el que se asustan: el de una sala grande.")]
        [SerializeField, Min(0f)] float panicRadius = 14f;

        [Header("Aspecto")]
        [SerializeField] GameObject suspectMarker;
        [SerializeField] GameObject scaredMarker;
        [SerializeField] GameObject hideEffect;

        public float DelayAt(int level) => At(delays, level, 3f);
        public float MultiplierAt(int level) => At(damageMultipliers, level, 2f);

        static float At(float[] values, int level, float fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            var camouflage = player.AddBehaviour<Camouflage>(Slot);
            camouflage.Configure(player, DelayAt(level), MultiplierAt(level), hiddenAlpha, noticeRange, noticeTime,
                suspectMarker, hideEffect);
            if (level >= MaxLevel)
                camouflage.ConfigurePanic(panicDuration, panicSpeed, panicRadius, scaredMarker);
        }

        internal void ConfigureTiming(float[] newDelays, float[] newMultipliers, float newNoticeRange, float newNoticeTime)
        {
            delays = newDelays;
            damageMultipliers = newMultipliers;
            noticeRange = newNoticeRange;
            noticeTime = newNoticeTime;
        }

        internal void ConfigureLook(GameObject newSuspectMarker, GameObject newScaredMarker, GameObject newHideEffect)
        {
            suspectMarker = newSuspectMarker;
            scaredMarker = newScaredMarker;
            hideEffect = newHideEffect;
        }
    }
}

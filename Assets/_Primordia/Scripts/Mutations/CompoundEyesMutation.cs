using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Ojos compuestos (Órgano, piso 2): clic derecho, durante 3 s todo va al 60 % de velocidad salvo tú (cámara lenta de
    /// verdad, ver <see cref="Core.TimeWarp"/>); recarga de 12 s, que empieza al acabar. Nivel II: dura 4 s. Nivel III
    /// (Visión total): mientras dura, tus ataques hacen un 30 % más de daño. Ver <see cref="CompoundEyesEffect"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Compound Eyes", fileName = "Mutation_CompoundEyes")]
    public class CompoundEyesMutation : AbilityMutation
    {
        [Tooltip("Segundos que dura, por nivel (I, II, III).")]
        [SerializeField] float[] durations = { 3f, 4f, 4f };
        [Tooltip("Velocidad del resto del mundo mientras dura (0,6 = al 60 %).")]
        [SerializeField, Range(0.1f, 1f)] float worldSpeed = 0.6f;
        [Tooltip("Visión total (nivel III): multiplicador del daño de tus ataques mientras dura.")]
        [SerializeField, Min(1f)] float totalVisionDamage = 1.3f;
        [SerializeField] Sprite[] flashFrames = System.Array.Empty<Sprite>();
        [Tooltip("Color del marco de facetas de la pantalla; con Visión total, el segundo.")]
        [SerializeField] Color vignetteColor = new(1f, 0.85f, 0.55f, 1f);
        [SerializeField] Color totalVisionColor = new(1f, 0.55f, 0.4f, 1f);

        public float DurationAt(int level) => durations[Mathf.Clamp(level - 1, 0, durations.Length - 1)];

        /// <summary>Mientras dura, la recarga espera (y así no la frena la cámara lenta).</summary>
        public override bool IsRunning(PlayerMutations player) =>
            player.TryGetComponent<CompoundEyesEffect>(out var effect) && effect.IsActive;

        public override void Activate(PlayerMutations player)
        {
            int level = Mathf.Max(1, player.LevelOf(Slot));
            if (!player.TryGetComponent<CompoundEyesEffect>(out var effect))
                effect = player.gameObject.AddComponent<CompoundEyesEffect>();
            bool totalVision = level >= MaxLevel;
            effect.Begin(player, DurationAt(level), worldSpeed, totalVision ? totalVisionDamage : 1f, flashFrames,
                totalVision ? totalVisionColor : vignetteColor);
        }

        public override void Remove(PlayerMutations player, int level)
        {
            base.Remove(player, level);
            if (player.TryGetComponent<CompoundEyesEffect>(out var effect))
                effect.End();
        }

        internal void ConfigureLook(Sprite[] newFlash) => flashFrames = newFlash ?? System.Array.Empty<Sprite>();
    }
}

using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Hifas (Extremidades): al moverte dejas un rastro de micelio que frena y daña a los enemigos que lo pisan.
    /// Nivel II: dura más y daña más. Nivel III (Red micelial): los enemigos que mueren sobre el micelio sueltan una
    /// nube de esporas que envenena a los cercanos. Ver <see cref="HyphaeTrail"/> y <see cref="MyceliumPatch"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Hyphae", fileName = "Mutation_Hyphae")]
    public class HyphaeMutation : MutationData
    {
        [Tooltip("Mancha de micelio (con MyceliumPatch).")]
        [SerializeField] MyceliumPatch patchPrefab;
        [Tooltip("Segundos que dura cada mancha, por nivel (I, II, III).")]
        [SerializeField] float[] durations = { 3f, 4.5f, 4.5f };
        [Tooltip("Daño por segundo a quien está encima, por nivel.")]
        [SerializeField] int[] damagePerSecond = { 1, 2, 2 };
        [Tooltip("Fracción de su velocidad a la que van los enemigos encima (0,6 = un 40 % más lentos).")]
        [SerializeField, Range(0.05f, 1f)] float slowFactor = 0.6f;
        [Tooltip("Distancia recorrida entre una mancha y la siguiente.")]
        [SerializeField, Min(0.1f)] float spacing = 0.6f;
        [Tooltip("Radio de cada mancha.")]
        [SerializeField, Min(0.1f)] float radius = 0.5f;

        [Header("Nivel III: red micelial")]
        [SerializeField, Min(0f)] float burstRadius = 2f;
        [SerializeField, Min(0)] int burstPoisonDamage = 1;
        [SerializeField, Min(0f)] float burstPoisonDuration = 3f;
        [SerializeField] GameObject burstEffect;
        [SerializeField] GameObject poisonEffect;

        public float DurationAt(int level) => durations[Mathf.Clamp(level - 1, 0, durations.Length - 1)];
        public int DamageAt(int level) => damagePerSecond[Mathf.Clamp(level - 1, 0, damagePerSecond.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            var settings = new MyceliumPatch.Settings
            {
                duration = DurationAt(level),
                damagePerSecond = DamageAt(level),
                slowFactor = slowFactor,
                radius = radius,
                burst = level >= MaxLevel,
                burstRadius = burstRadius,
                burstPoisonDamage = burstPoisonDamage,
                burstPoisonDuration = burstPoisonDuration,
                burstEffect = burstEffect,
                poisonEffect = poisonEffect,
            };
            player.AddBehaviour<HyphaeTrail>(Slot).Configure(patchPrefab, settings, spacing);
        }

        internal void ConfigureLook(MyceliumPatch newPatch, GameObject newBurstEffect, GameObject newPoisonEffect)
        {
            patchPrefab = newPatch;
            burstEffect = newBurstEffect;
            poisonEffect = newPoisonEffect;
        }
    }
}

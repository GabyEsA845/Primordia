using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Escleritos de Wiwaxia (Piel, piso 2): cada 9 s sueltas 3 escamas hacia los enemigos cercanos (2 de daño cada una).
    /// Nivel II: cada 7 s, 5 escamas. Nivel III (Órbita): antes de salir, las escamas giran 2 s a tu alrededor y destruyen
    /// los proyectiles enemigos que tocan. Ver <see cref="ScleriteVolley"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Wiwaxia Sclerites", fileName = "Mutation_WiwaxiaSclerites")]
    public class WiwaxiaScleritesMutation : MutationData
    {
        [Tooltip("Segundos entre tandas, por nivel (I, II, III).")]
        [SerializeField] float[] intervals = { 9f, 7f, 7f };
        [Tooltip("Escamas por tanda, por nivel.")]
        [SerializeField] int[] counts = { 3, 5, 5 };
        [Tooltip("La escama disparada (2 de daño).")]
        [SerializeField] AttackData scaleAttack;
        [Tooltip("Distancia a la que busca enemigos; sin ninguno, la tanda espera.")]
        [SerializeField, Min(0f)] float range = 7f;
        [Header("Nivel III: Órbita")]
        [SerializeField, Min(0f)] float orbitDuration = 2f;
        [SerializeField, Min(0.1f)] float orbitRadius = 1.1f;
        [Tooltip("Vueltas por segundo.")]
        [SerializeField] float orbitSpeed = 0.9f;
        [SerializeField] Sprite[] scaleFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] releaseFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] blockFrames = System.Array.Empty<Sprite>();

        public float IntervalAt(int level) => intervals[Mathf.Clamp(level - 1, 0, intervals.Length - 1)];
        public int CountAt(int level) => counts[Mathf.Clamp(level - 1, 0, counts.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            var settings = new ScleriteVolley.Settings
            {
                interval = IntervalAt(level),
                count = CountAt(level),
                attack = scaleAttack,
                range = range,
                orbit = level >= MaxLevel,
                orbitDuration = orbitDuration,
                orbitRadius = orbitRadius,
                orbitSpeed = orbitSpeed,
                scaleFrames = scaleFrames,
                releaseFrames = releaseFrames,
                blockFrames = blockFrames,
            };
            player.AddBehaviour<ScleriteVolley>(Slot).Configure(player.Weapon, settings);
        }

        internal void ConfigureLook(AttackData newAttack, Sprite[] newScale, Sprite[] newRelease, Sprite[] newBlock)
        {
            scaleAttack = newAttack;
            scaleFrames = newScale ?? System.Array.Empty<Sprite>();
            releaseFrames = newRelease ?? System.Array.Empty<Sprite>();
            blockFrames = newBlock ?? System.Array.Empty<Sprite>();
        }
    }
}

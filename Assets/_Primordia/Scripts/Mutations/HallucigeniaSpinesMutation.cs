using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Espinas de Hallucigenia (Extremidades, piso 2): al esquivar dejas púas en el suelo, donde empezaste, que hacen daño
    /// a quien las pisa. Nivel I: 2 púas durante 4 s. Nivel II: 3 púas durante 6 s. Nivel III (Erizo): al desaparecer,
    /// cada púa sale disparada hacia el enemigo más cercano. Ver <see cref="SpineTraps"/> y <see cref="GroundSpine"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Hallucigenia Spines", fileName = "Mutation_HallucigeniaSpines")]
    public class HallucigeniaSpinesMutation : MutationData
    {
        [Tooltip("Púas por esquiva, por nivel (I, II, III).")]
        [SerializeField] int[] counts = { 2, 3, 3 };
        [Tooltip("Segundos que duran, por nivel.")]
        [SerializeField] float[] durations = { 4f, 6f, 6f };
        [Tooltip("Daño a quien pisa una púa.")]
        [SerializeField, Min(0)] int damage = 2;
        [SerializeField] GroundSpine spinePrefab;
        [Tooltip("Erizo (nivel III): el disparo de cada púa al desaparecer.")]
        [SerializeField] AttackData shotAttack;
        [Tooltip("Erizo: distancia a la que busca enemigo.")]
        [SerializeField, Min(0f)] float shotRange = 7f;
        [Tooltip("Separación entre las púas de una misma esquiva.")]
        [SerializeField, Min(0.1f)] float spacing = 0.55f;

        public int CountAt(int level) => counts[Mathf.Clamp(level - 1, 0, counts.Length - 1)];
        public float DurationAt(int level) => durations[Mathf.Clamp(level - 1, 0, durations.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            player.AddBehaviour<SpineTraps>(Slot).Configure(player.Controller, player.Weapon, spinePrefab, CountAt(level),
                DurationAt(level), damage, spacing, level >= MaxLevel ? shotAttack : null, shotRange);
        }

        internal void ConfigureLook(GroundSpine newSpine, AttackData newShot)
        {
            spinePrefab = newSpine;
            shotAttack = newShot;
        }
    }
}

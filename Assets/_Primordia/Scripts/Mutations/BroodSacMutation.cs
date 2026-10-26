using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Saco de crías (Órgano, piso 2): clic derecho, el saco se abre y suelta 2 larvas aliadas que persiguen y muerden a los
    /// enemigos durante 8 s; recarga de 14 s. Nivel II: 3 larvas. Nivel III (Enjambre): la larva que mata a un enemigo se
    /// divide en dos (la nueva también dura 8 s). Ver <see cref="Larva"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Brood Sac", fileName = "Mutation_BroodSac")]
    public class BroodSacMutation : AbilityMutation
    {
        [Tooltip("Larvas por uso, por nivel (I, II, III).")]
        [SerializeField] int[] counts = { 2, 3, 3 };
        [SerializeField] Larva larvaPrefab;
        [SerializeField] Larva.Settings larva = new()
        {
            lifetime = 8f, speed = 5.5f, range = 9f, biteDamage = 2, biteInterval = 0.7f, biteReach = 0.55f,
        };
        [Tooltip("Separación de las larvas al salir del saco.")]
        [SerializeField, Min(0f)] float spread = 0.7f;
        [SerializeField] GameObject openEffect;

        public int CountAt(int level) => counts[Mathf.Clamp(level - 1, 0, counts.Length - 1)];

        public override void Activate(PlayerMutations player)
        {
            int level = Mathf.Max(1, player.LevelOf(Slot));
            Vector2 center = player.transform.position;
            OneShotEffect.Spawn(openEffect, center, 0f, player.transform);
            var settings = larva;
            settings.splits = level >= MaxLevel;
            int count = CountAt(level);
            for (int i = 0; i < count; i++)
            {
                float angle = (90f + 360f * i / count) * Mathf.Deg2Rad;
                Larva.Spawn(larvaPrefab, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread, settings, player);
            }
        }

        internal void ConfigureLook(Larva newLarva, GameObject newOpenEffect)
        {
            larvaPrefab = newLarva;
            openEffect = newOpenEffect;
        }
    }
}

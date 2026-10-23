using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Vacuola digestiva (Órgano especial): clic derecho para digerir y curar. No se recarga con el tiempo sino
    /// matando enemigos. Nivel I: cada 4 muertes, medio corazón. Nivel II: cada 3. Nivel III (Digestión completa):
    /// cada 3, un corazón entero. Con la vida llena no se gasta.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Digestive Vacuole", fileName = "Mutation_DigestiveVacuole")]
    public class DigestiveVacuoleMutation : AbilityMutation
    {
        [Tooltip("Enemigos que hay que matar para recargarla, por nivel (I, II, III).")]
        [SerializeField] int[] killsToCharge = { 4, 3, 3 };
        [Tooltip("Vida que cura, en medios corazones, por nivel.")]
        [SerializeField] int[] heal = { 1, 1, 2 };

        public override int KillsToChargeAt(int level) => killsToCharge[Mathf.Clamp(level - 1, 0, killsToCharge.Length - 1)];

        public int HealAt(int level) => heal[Mathf.Clamp(level - 1, 0, heal.Length - 1)];

        public override bool CanActivate(PlayerMutations player) => player.Health.Current < player.Health.Max;

        public override void Activate(PlayerMutations player)
        {
            player.Health.Heal(HealAt(Mathf.Max(1, player.LevelOf(Slot))));
            Sfx.Play(Sounds.Vacuole);
            if (EffectPrefab != null)
                OneShotEffect.Spawn(EffectPrefab, player.transform.position, parent: player.transform);
        }
    }
}

using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mutación de Boca: cambia el ataque principal (Mandíbulas, Aguijón, Esporas).
    /// El veneno, el abanico o el modo automático van en el <see cref="AttackData"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Attack Mutation", fileName = "Mutation_")]
    public class AttackMutation : MutationData
    {
        [SerializeField] AttackData attack;
        [Tooltip("Cada cuántos enemigos matados con este ataque se cura medio corazón. 0 = nunca.")]
        [SerializeField, Min(0)] int healEveryKills;

        public AttackData Attack => attack;
        public int HealEveryKills => healEveryKills;

        public override void Apply(PlayerMutations player)
        {
            player.Weapon.Attack = attack;
            if (healEveryKills > 0)
                player.AddBehaviour<KillHealer>(Slot).Configure(player.Weapon, player.Health, healEveryKills);
        }

        public override void Remove(PlayerMutations player) => player.Weapon.Attack = player.DefaultAttack;

        internal void Configure(AttackData newAttack, int newHealEveryKills)
        {
            attack = newAttack;
            healEveryKills = Mathf.Max(0, newHealEveryKills);
        }
    }
}

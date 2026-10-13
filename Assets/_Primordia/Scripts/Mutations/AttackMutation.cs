using System;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mutación de Boca: cambia el ataque principal (Mandíbulas, Aguijón, Esporas).
    /// El veneno, el sangrado, el abanico o los proyectiles buscadores van en el <see cref="AttackData"/>;
    /// cada nivel puede usar un ataque distinto.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Attack Mutation", fileName = "Mutation_")]
    public class AttackMutation : MutationData
    {
        [Serializable]
        public class AttackLevel
        {
            [Tooltip("Ataque de este nivel. Vacío = el del nivel anterior.")]
            public AttackData attack;
            [Tooltip("Cada cuántos enemigos matados se cura medio corazón. 0 = nunca.")]
            [Min(0)] public int healEveryKills;
        }

        [Tooltip("Ataque del nivel I.")]
        [SerializeField] AttackData attack;
        [Tooltip("Nivel I: cada cuántos enemigos matados con este ataque se cura medio corazón. 0 = nunca.")]
        [SerializeField, Min(0)] int healEveryKills;
        [Tooltip("Niveles II y III.")]
        [SerializeField] AttackLevel[] upgrades = Array.Empty<AttackLevel>();
        [Tooltip("Efecto sobre el jugador cuando se cura al matar.")]
        [SerializeField] GameObject healEffect;

        public AttackData Attack => attack;
        public int HealEveryKills => healEveryKills;

        public AttackData AttackAt(int level)
        {
            var result = attack;
            for (int i = 0; i <= UpgradeIndex(upgrades, level); i++)
            {
                if (upgrades[i] != null && upgrades[i].attack != null)
                    result = upgrades[i].attack;
            }
            return result;
        }

        public int HealEveryKillsAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index].healEveryKills : healEveryKills;
        }

        public override void Apply(PlayerMutations player, int level)
        {
            player.Weapon.Attack = AttackAt(level);
            int heal = HealEveryKillsAt(level);
            if (heal > 0)
                player.AddBehaviour<KillHealer>(Slot).Configure(player.Weapon, player.Health, heal, healEffect);
        }

        public override void Remove(PlayerMutations player, int level) => player.Weapon.Attack = player.DefaultAttack;

        internal void Configure(AttackData newAttack, int newHealEveryKills)
        {
            attack = newAttack;
            healEveryKills = Mathf.Max(0, newHealEveryKills);
        }

        internal void ConfigureUpgrades(params AttackLevel[] newUpgrades) => upgrades = newUpgrades;

        internal void SetHealEffect(GameObject effect) => healEffect = effect;
    }
}

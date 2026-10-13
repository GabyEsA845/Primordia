using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mutación que cambia estadísticas: velocidad, esquiva y vida máxima (Aletas, Caparazón). Sus evoluciones
    /// (nivel III) son la Emboscada (más daño tras esquivar) y las Placas (anulan el primer golpe de cada sala).
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Stat Mutation", fileName = "Mutation_")]
    public class StatMutation : MutationData
    {
        [Serializable]
        public class StatLevel
        {
            [Tooltip("Multiplica la velocidad. 1,3 = +30 %; 0,9 = −10 %.")]
            [Min(0.1f)] public float moveSpeedMultiplier = 1f;
            [Tooltip("Multiplica la distancia de la esquiva.")]
            [Min(0.1f)] public float dodgeDistanceMultiplier = 1f;
            [Tooltip("Multiplica la espera entre esquivas. 0,75 = −25 %.")]
            [Min(0.1f)] public float dodgeCooldownMultiplier = 1f;
            [Tooltip("Vida máxima extra (2 = un corazón). Al equiparla también cura esa cantidad.")]
            [Min(0)] public int maxHealthBonus;
        }

        [Tooltip("Nivel I: multiplica la velocidad. 1,3 = +30 %; 0,9 = −10 %.")]
        [SerializeField, Min(0.1f)] float moveSpeedMultiplier = 1f;
        [Tooltip("Nivel I: multiplica la distancia de la esquiva.")]
        [SerializeField, Min(0.1f)] float dodgeDistanceMultiplier = 1f;
        [Tooltip("Nivel I: vida máxima extra (2 = un corazón). Al equiparla también cura esa cantidad.")]
        [SerializeField, Min(0)] int maxHealthBonus;
        [Tooltip("Niveles II y III: los valores completos de cada nivel (no se suman a los del I).")]
        [SerializeField] StatLevel[] upgrades = Array.Empty<StatLevel>();

        [Header("Evolución (nivel III): Emboscada")]
        [Tooltip("Daño extra durante un rato después de cada esquiva. 0,5 = +50 %. 0 = sin Emboscada.")]
        [SerializeField, Min(0f)] float ambushDamageBonus;
        [SerializeField, Min(0f)] float ambushDuration = 1.5f;
        [Tooltip("Aura en bucle sobre el jugador mientras dura.")]
        [SerializeField] GameObject ambushEffect;

        [Header("Evolución (nivel III): Placas")]
        [Tooltip("Anula el primer golpe en cada sala.")]
        [SerializeField] bool plates;
        [Tooltip("Brillo en bucle mientras el escudo está cargado.")]
        [SerializeField] GameObject platesEffect;
        [Tooltip("Efecto al gastarse el escudo.")]
        [SerializeField] GameObject platesBreakEffect;

        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float DodgeDistanceMultiplier => dodgeDistanceMultiplier;
        public int MaxHealthBonus => maxHealthBonus;

        public StatLevel StatsAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            if (index >= 0 && upgrades[index] != null)
                return upgrades[index];

            return new StatLevel
            {
                moveSpeedMultiplier = moveSpeedMultiplier,
                dodgeDistanceMultiplier = dodgeDistanceMultiplier,
                maxHealthBonus = maxHealthBonus,
            };
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var stats = StatsAt(level);
            player.Controller.MoveSpeed *= stats.moveSpeedMultiplier;
            player.Controller.DodgeDistance *= stats.dodgeDistanceMultiplier;
            player.Controller.DodgeCooldown *= stats.dodgeCooldownMultiplier;
            if (stats.maxHealthBonus > 0)
                player.Health.SetMax(player.Health.Max + stats.maxHealthBonus, healDifference: true);

            if (level < MaxLevel)
                return;

            if (ambushDamageBonus > 0f)
                player.AddBehaviour<Ambush>(Slot).Configure(player.Controller, player.Weapon, ambushDamageBonus, ambushDuration, ambushEffect);
            if (plates)
                player.AddBehaviour<Plates>(Slot).Configure(player.Health, platesEffect, platesBreakEffect);
        }

        public override void Remove(PlayerMutations player, int level)
        {
            var stats = StatsAt(level);
            player.Controller.MoveSpeed /= stats.moveSpeedMultiplier;
            player.Controller.DodgeDistance /= stats.dodgeDistanceMultiplier;
            player.Controller.DodgeCooldown /= stats.dodgeCooldownMultiplier;
            if (stats.maxHealthBonus > 0)
                player.Health.SetMax(player.Health.Max - stats.maxHealthBonus, healDifference: false);
        }

        internal void Configure(float speedMultiplier, float dodgeMultiplier, int healthBonus)
        {
            moveSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            dodgeDistanceMultiplier = Mathf.Max(0.1f, dodgeMultiplier);
            maxHealthBonus = Mathf.Max(0, healthBonus);
        }

        internal void ConfigureUpgrades(params StatLevel[] newUpgrades) => upgrades = newUpgrades;

        internal void ConfigureAmbush(float damageBonus, float duration, GameObject effect)
        {
            ambushDamageBonus = Mathf.Max(0f, damageBonus);
            ambushDuration = Mathf.Max(0f, duration);
            ambushEffect = effect;
        }

        internal void ConfigurePlates(bool enabled, GameObject effect, GameObject breakEffect)
        {
            plates = enabled;
            platesEffect = effect;
            platesBreakEffect = breakEffect;
        }
    }
}

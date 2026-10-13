using System;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Piel urticante: los enemigos que tocan al jugador reciben daño (y, desde el nivel II, veneno).
    /// Evolución (nivel III): Erizo, que dispara púas en círculo cada vez que el jugador recibe un golpe.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Thorns Mutation", fileName = "Mutation_")]
    public class ThornsMutation : MutationData
    {
        [Serializable]
        public class ThornsLevel
        {
            [Min(1)] public int damage = 2;
            [Tooltip("Veneno por segundo al tocarte. 0 = sin veneno.")]
            [Min(0)] public int poisonDamagePerSecond;
            [Min(0f)] public float poisonDuration;
        }

        [Tooltip("Nivel I: daño al tocarte.")]
        [SerializeField, Min(1)] int damage = 2;
        [Tooltip("Segundos mínimos entre dos daños al mismo enemigo.")]
        [SerializeField, Min(0.05f)] float interval = 0.5f;
        [Tooltip("Niveles II y III.")]
        [SerializeField] ThornsLevel[] upgrades = Array.Empty<ThornsLevel>();
        [Tooltip("Marca del veneno sobre los enemigos.")]
        [SerializeField] GameObject poisonEffect;

        [Header("Evolución (nivel III): Erizo")]
        [Tooltip("Ataque de cada púa (proyectil, daño y veneno). Vacío = sin Erizo.")]
        [SerializeField] AttackData quillAttack;
        [SerializeField, Min(1)] int quillCount = 8;

        public ThornsLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : new ThornsLevel { damage = damage };
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var values = ValuesAt(level);
            player.AddBehaviour<Thorns>(Slot).Configure(values.damage, interval, values.poisonDamagePerSecond, values.poisonDuration, poisonEffect);

            if (level >= MaxLevel && quillAttack != null)
                player.AddBehaviour<QuillBurst>(Slot).Configure(player.Health, quillAttack, quillCount);
        }

        internal void Configure(int newDamage, float newInterval)
        {
            damage = newDamage;
            interval = newInterval;
        }

        internal void ConfigureUpgrades(params ThornsLevel[] newUpgrades) => upgrades = newUpgrades;

        internal void ConfigureQuills(AttackData attack, int count, GameObject newPoisonEffect)
        {
            quillAttack = attack;
            quillCount = Mathf.Max(1, count);
            poisonEffect = newPoisonEffect;
        }
    }
}

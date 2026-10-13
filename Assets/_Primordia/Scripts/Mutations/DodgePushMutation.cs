using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Patas articuladas: durante la esquiva, los enemigos que tocas salen empujados, aturdidos y dañados.
    /// Evolución (nivel III): Pisotón, una onda al terminar cada esquiva que aturde a los que haya alrededor.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Dodge Push Mutation", fileName = "Mutation_")]
    public class DodgePushMutation : MutationData
    {
        [Serializable]
        public class PushLevel
        {
            [Min(0f)] public float stunDuration = 0.5f;
            [Min(0)] public int damage = 1;
        }

        [Tooltip("Velocidad inicial del empujón, en unidades por segundo.")]
        [SerializeField, Min(0f)] float pushSpeed = 12f;
        [Tooltip("Nivel I: segundos de aturdimiento.")]
        [SerializeField, Min(0f)] float stunDuration = 0.5f;
        [Tooltip("Nivel I: daño del empujón.")]
        [SerializeField, Min(0)] int damage = 1;
        [Tooltip("Niveles II y III.")]
        [SerializeField] PushLevel[] upgrades = Array.Empty<PushLevel>();
        [Tooltip("Marca de aturdido sobre los enemigos.")]
        [SerializeField] GameObject stunMarker;

        [Header("Evolución (nivel III): Pisotón")]
        [Tooltip("Radio de la onda al terminar la esquiva. 0 = sin Pisotón.")]
        [SerializeField, Min(0f)] float stompRadius;
        [Tooltip("Efecto del pisotón, dibujado a su radio real.")]
        [SerializeField] GameObject stompEffect;

        public PushLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null
                ? upgrades[index]
                : new PushLevel { stunDuration = stunDuration, damage = damage };
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var values = ValuesAt(level);
            float stomp = level >= MaxLevel ? stompRadius : 0f;
            player.AddBehaviour<DodgePush>(Slot).Configure(player.Controller, pushSpeed, values.stunDuration, values.damage,
                stunMarker, stomp, stompEffect);
        }

        internal void Configure(float newPushSpeed, float newStunDuration, int newDamage)
        {
            pushSpeed = newPushSpeed;
            stunDuration = newStunDuration;
            damage = newDamage;
        }

        internal void ConfigureUpgrades(params PushLevel[] newUpgrades) => upgrades = newUpgrades;

        internal void ConfigureStomp(float radius, GameObject effect, GameObject marker)
        {
            stompRadius = Mathf.Max(0f, radius);
            stompEffect = effect;
            stunMarker = marker;
        }
    }
}

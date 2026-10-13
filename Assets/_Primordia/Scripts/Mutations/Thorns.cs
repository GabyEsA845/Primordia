using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Comportamiento de <see cref="ThornsMutation"/>: daño (y veneno) a los enemigos que tocan al jugador.</summary>
    public class Thorns : MonoBehaviour
    {
        int damage;
        float interval;
        int poisonDamagePerSecond;
        float poisonDuration;
        GameObject poisonEffect;
        int enemyLayer;
        readonly Dictionary<Health, float> nextHitTime = new();

        public void Configure(int newDamage, float newInterval, int newPoisonDamagePerSecond = 0, float newPoisonDuration = 0f,
            GameObject newPoisonEffect = null)
        {
            damage = newDamage;
            interval = newInterval;
            poisonDamagePerSecond = newPoisonDamagePerSecond;
            poisonDuration = newPoisonDuration;
            poisonEffect = newPoisonEffect;
        }

        void Awake() => enemyLayer = LayerMask.NameToLayer(CombatLayers.Enemy);

        void OnCollisionEnter2D(Collision2D collision) => TryHurt(collision);

        void OnCollisionStay2D(Collision2D collision) => TryHurt(collision);

        void TryHurt(Collision2D collision)
        {
            if (collision.gameObject.layer != enemyLayer)
                return;

            var health = collision.gameObject.GetComponentInParent<Health>();
            if (health == null || health.IsDead)
                return;

            if (nextHitTime.TryGetValue(health, out float next) && Time.time < next)
                return;

            nextHitTime[health] = Time.time + interval;
            health.TakeDamage(damage);

            if (poisonDamagePerSecond > 0 && poisonDuration > 0f && !health.IsDead)
                Poison.Apply(health, poisonDamagePerSecond, poisonDuration, poisonEffect);
        }
    }
}

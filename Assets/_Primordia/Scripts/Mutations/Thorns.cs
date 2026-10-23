using System.Collections.Generic;
using Primordia.Audio;
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

            HurtAttacker(collision.gameObject.GetComponentInParent<Health>());
        }

        /// <summary>
        /// Pincha a un enemigo que toca al jugador. Lo llaman los choques y también quien detecta el contacto por su
        /// cuenta (la ameba gigante no choca con el jugador: lo atraviesa y le hace daño al tocarlo).
        /// </summary>
        public void HurtAttacker(Health health)
        {
            if (health == null || health.IsDead)
                return;

            if (nextHitTime.TryGetValue(health, out float next) && Time.time < next)
                return;

            nextHitTime[health] = Time.time + interval;
            health.TakeDamage(damage);
            Sfx.Play(Sounds.Thorns);

            if (poisonDamagePerSecond > 0 && poisonDuration > 0f && !health.IsDead)
                Poison.Apply(health, poisonDamagePerSecond, poisonDuration, poisonEffect);
        }
    }
}

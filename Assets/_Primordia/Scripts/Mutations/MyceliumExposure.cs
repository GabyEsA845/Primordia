using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Lo que le pasa a un enemigo encima del micelio: va más lento y recibe daño a su ritmo (una sola vez aunque
    /// pise varias manchas). Con la red micelial (Hifas nivel III), si muere encima suelta una nube de esporas que
    /// envenena a los enemigos cercanos.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class MyceliumExposure : MonoBehaviour
    {
        // Segundos que sigue contando como "encima" tras el último contacto.
        const float Grace = 0.2f;

        Enemy enemy;
        EnemyAI ai;
        MyceliumPatch.Settings settings;
        float lastTouch = -1f;
        float nextDamage;

        public bool IsOnMycelium => lastTouch >= 0f && Time.time - lastTouch <= Grace;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            ai = GetComponent<EnemyAI>();
            enemy.Health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (enemy != null && enemy.Health != null)
                enemy.Health.Died -= OnDied;
        }

        public void Touch(MyceliumPatch.Settings patch)
        {
            // El primer golpe llega tras un intervalo: rozar el micelio no daña al momento.
            if (!IsOnMycelium)
                nextDamage = Time.time + 1f / Mathf.Max(1, patch.damagePerSecond);

            settings = patch;
            lastTouch = Time.time;
            if (ai != null)
                ai.Slow(patch.slowFactor, Grace);

            if (patch.damagePerSecond > 0 && Time.time >= nextDamage)
            {
                nextDamage = Time.time + 1f / patch.damagePerSecond;
                enemy.Health.TakeDamage(1);
            }
        }

        void OnDied()
        {
            if (!IsOnMycelium || !settings.burst)
                return;

            OneShotEffect.Spawn(settings.burstEffect, transform.position);
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, settings.burstRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var other = hit.GetComponentInParent<Enemy>();
                if (other == null || other == enemy || other.Health.IsDead)
                    continue;
                Poison.Apply(other.Health, settings.burstPoisonDamage, settings.burstPoisonDuration, settings.poisonEffect);
            }
        }
    }
}

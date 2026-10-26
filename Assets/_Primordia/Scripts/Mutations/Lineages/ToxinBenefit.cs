using System;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Toxina débil (Venenoso): los enemigos envenenados reciben más daño de cualquier fuente. En el piso 2 además se
    /// mueven más despacio mientras dura el veneno.
    /// </summary>
    public class ToxinBenefit : MonoBehaviour
    {
        // Cada cuánto busca envenenados para frenarlos, y cuánto dura cada freno (algo más, para que no haya huecos).
        const float SlowInterval = 0.1f;
        const float SlowHold = 0.25f;

        float multiplier = 1.5f;
        float slowFactor = 1f;
        float nextSlow;
        Func<Health, int, int> modifier;

        public float SlowFactor => slowFactor;

        public void Configure(float newMultiplier, float newSlowFactor = 1f)
        {
            multiplier = newMultiplier;
            slowFactor = Mathf.Clamp01(newSlowFactor);
            if (modifier == null)
            {
                modifier = Modify;
                Health.AddDamageModifier(modifier);
            }
        }

        int Modify(Health target, int amount) =>
            target.TryGetComponent<Enemy>(out _) && target.TryGetComponent<Poison>(out _)
                ? Health.ScaleDamage(amount, multiplier)
                : amount;

        void Update()
        {
            if (slowFactor >= 1f || Time.time < nextSlow)
                return;
            nextSlow = Time.time + SlowInterval;
            foreach (var poison in FindObjectsByType<Poison>(FindObjectsSortMode.None))
            {
                if (poison.TryGetComponent<EnemyAI>(out var ai))
                    ai.Slow(slowFactor, SlowHold);
            }
        }

        void OnDestroy()
        {
            if (modifier != null)
                Health.RemoveDamageModifier(modifier);
        }
    }
}

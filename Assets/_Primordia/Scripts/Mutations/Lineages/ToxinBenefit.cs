using System;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Toxina débil (Venenoso): los enemigos envenenados reciben más daño de cualquier fuente.</summary>
    public class ToxinBenefit : MonoBehaviour
    {
        float multiplier = 1.5f;
        Func<Health, int, int> modifier;

        public void Configure(float newMultiplier)
        {
            multiplier = newMultiplier;
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

        void OnDestroy()
        {
            if (modifier != null)
                Health.RemoveDamageModifier(modifier);
        }
    }
}

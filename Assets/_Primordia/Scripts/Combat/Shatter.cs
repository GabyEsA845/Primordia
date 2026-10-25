using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Quebrar (Placas mandibulares): durante unos segundos el enemigo mordido recibe más daño de todo (un 25 %). Un mordisco
    /// nuevo lo renueva. Se suma a los modificadores de daño de <see cref="Health"/> mientras haya algún enemigo quebrado.
    /// </summary>
    public class Shatter : MonoBehaviour
    {
        public const string MarkerKey = "Shatter";

        static readonly HashSet<Health> shattered = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => shattered.Clear();

        Health health;
        float until;
        float multiplier = 1.25f;

        public bool IsActive => Time.time < until;
        public float Multiplier => multiplier;

        /// <summary>Quiebra al objetivo durante <paramref name="duration"/> segundos (o lo renueva).</summary>
        public static Shatter Apply(Health target, float duration, float multiplier, GameObject marker = null)
        {
            if (target == null || target.IsDead || duration <= 0f)
                return null;

            if (!target.TryGetComponent<Shatter>(out var shatter))
            {
                shatter = target.gameObject.AddComponent<Shatter>();
                shatter.health = target;
            }
            shatter.until = Mathf.Max(shatter.until, Time.time + duration);
            shatter.multiplier = Mathf.Max(1f, multiplier);
            shattered.Add(target);
            Health.AddDamageModifier(Modify);
            StatusMarkers.Show(target, MarkerKey, marker, shatter.until - Time.time);
            return shatter;
        }

        static int Modify(Health target, int amount)
        {
            if (!shattered.Contains(target) || !target.TryGetComponent<Shatter>(out var shatter) || !shatter.IsActive)
                return amount;
            return Health.ScaleDamage(amount, shatter.multiplier);
        }

        void Update()
        {
            if (IsActive && health != null && !health.IsDead)
                return;
            Destroy(this);
        }

        void OnDestroy()
        {
            if (health != null)
                StatusMarkers.Hide(health, MarkerKey);
            shattered.Remove(health);
            if (shattered.Count == 0)
                Health.RemoveDamageModifier(Modify);
        }
    }
}

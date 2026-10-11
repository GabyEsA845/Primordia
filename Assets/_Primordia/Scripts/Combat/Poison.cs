using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Daño en el tiempo: quita vida una vez por segundo, tantas veces como segundos dure.
    /// Se añade al objetivo con <see cref="Apply"/>; un veneno nuevo reinicia los golpes que quedan.
    /// </summary>
    public class Poison : MonoBehaviour
    {
        Health health;
        int damagePerTick;
        int ticksLeft;
        float tickTimer;

        public int TicksLeft => ticksLeft;

        public static Poison Apply(Health target, int damagePerSecond, float duration)
        {
            if (!target.TryGetComponent<Poison>(out var poison))
                poison = target.gameObject.AddComponent<Poison>();

            poison.health = target;
            poison.damagePerTick = Mathf.Max(poison.damagePerTick, damagePerSecond);
            // Se cuentan golpes enteros y no segundos con decimales, para no perder el último por redondeo.
            poison.ticksLeft = Mathf.Max(poison.ticksLeft, Mathf.Max(1, Mathf.RoundToInt(duration)));
            return poison;
        }

        void Update()
        {
            if (health == null || health.IsDead)
            {
                Destroy(this);
                return;
            }

            tickTimer += Time.deltaTime;
            if (tickTimer < 1f)
                return;

            tickTimer -= 1f;
            ticksLeft--;
            health.TakeDamage(damagePerTick);

            if (ticksLeft <= 0)
                Destroy(this);
        }
    }
}

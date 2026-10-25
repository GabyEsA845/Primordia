using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Daño en el tiempo (veneno, sangrado): quita vida una vez por segundo, tantas veces como segundos dure.
    /// Cada tipo es un componente distinto, así un objetivo puede sangrar y estar envenenado a la vez. Uno nuevo
    /// del mismo tipo renueva los golpes que quedan. Mientras dura, muestra su marca sobre el objetivo.
    /// </summary>
    public abstract class DamageOverTime : MonoBehaviour
    {
        protected Health health;
        int damagePerTick;
        int ticksLeft;
        float tickTimer;

        public int TicksLeft => ticksLeft;
        public int DamagePerTick => damagePerTick;

        /// <summary>La marca que se ve sobre el objetivo (null si no tiene).</summary>
        public GameObject Effect { get; private set; }

        /// <summary>Nombre de la marca en <see cref="StatusMarkers"/>.</summary>
        protected abstract string MarkerKey { get; }

        protected static T Apply<T>(Health target, int damagePerSecond, float duration, GameObject effectPrefab) where T : DamageOverTime
        {
            if (!target.TryGetComponent<T>(out var dot))
                dot = target.gameObject.AddComponent<T>();

            dot.health = target;
            dot.damagePerTick = Mathf.Max(dot.damagePerTick, damagePerSecond);
            // Se cuentan golpes enteros y no segundos con decimales, para no perder el último por redondeo.
            dot.ticksLeft = Mathf.Max(dot.ticksLeft, Mathf.Max(1, Mathf.RoundToInt(duration)));

            if (dot.Effect == null)
                dot.Effect = StatusMarkers.Show(target, dot.MarkerKey, effectPrefab, 0f);
            return dot;
        }

        protected virtual void Update()
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
            health.TakeDamage(damagePerTick, DamageKind.Tick);

            if (ticksLeft <= 0)
                Destroy(this);
        }

        protected virtual void OnDestroy()
        {
            if (health != null)
                StatusMarkers.Hide(health, MarkerKey);
        }
    }
}

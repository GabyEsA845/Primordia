using Primordia.Core;
using Primordia.Enemies;
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
        float tickSeconds = 1f;

        public int TicksLeft => ticksLeft;
        public int DamagePerTick => damagePerTick;

        /// <summary>Cada cuánto quita vida (1 s; 0,5 s el veneno virulento).</summary>
        public float TickSeconds => tickSeconds;

        /// <summary>La marca que se ve sobre el objetivo (null si no tiene).</summary>
        public GameObject Effect { get; private set; }

        /// <summary>Nombre de la marca en <see cref="StatusMarkers"/>.</summary>
        protected abstract string MarkerKey { get; }

        /// <summary>Lo máximo que quita cada golpe a un jefe (sin tope por defecto); con mejoras el veneno dejaba a los jefes sin vida.</summary>
        protected virtual int BossDamageCap => int.MaxValue;

        /// <summary>Origen que lleva cada tick (para el límite por fuente de un jefe).</summary>
        protected virtual DamageSource Source => DamageSource.Unknown;

        protected static T Apply<T>(Health target, int damagePerSecond, float duration, GameObject effectPrefab, float tickSeconds = 1f) where T : DamageOverTime
        {
            if (!target.TryGetComponent<T>(out var dot))
                dot = target.gameObject.AddComponent<T>();

            dot.health = target;
            dot.damagePerTick = Mathf.Max(dot.damagePerTick, damagePerSecond);
            // Un golpe más rápido (veneno virulento) manda sobre el ritmo normal; los golpes que quedan se cuentan al ritmo nuevo.
            bool faster = tickSeconds < dot.tickSeconds;
            if (faster)
                dot.tickSeconds = Mathf.Max(0.1f, tickSeconds);
            // Se cuentan golpes enteros y no segundos con decimales, para no perder el último por redondeo.
            dot.ticksLeft = Mathf.Max(faster ? 0 : dot.ticksLeft, Mathf.Max(1, Mathf.RoundToInt(duration / dot.tickSeconds)));

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
            if (tickTimer < tickSeconds)
                return;

            tickTimer -= tickSeconds;
            ticksLeft--;
            int damage = damagePerTick;
            if (BossDamageCap < damage && health.GetComponentInParent<IBoss>() != null)
                damage = BossDamageCap;
            health.TakeDamage(damage, DamageKind.Tick, Source);

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

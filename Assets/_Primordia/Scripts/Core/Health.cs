using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Vida, daño y muerte. Sirve para el jugador, los enemigos y el jefe.
    /// En el jugador, 1 punto equivale a medio corazón (6 puntos = 3 corazones).
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Tooltip("Vida máxima. En el jugador, 1 punto = medio corazón.")]
        [SerializeField, Min(1)] int maxHealth = 6;

        [Tooltip("Segundos de invulnerabilidad después de recibir un golpe. 0 en los enemigos.")]
        [SerializeField, Min(0f)] float invulnerabilityAfterHit;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>Invulnerabilidad controlada desde fuera, por ejemplo durante la esquiva.</summary>
        public bool IsInvulnerable { get; set; }

        public float InvulnerabilityAfterHit
        {
            get => invulnerabilityAfterHit;
            set => invulnerabilityAfterHit = Mathf.Max(0f, value);
        }

        public bool CanTakeDamage => !IsDead && !IsInvulnerable && Time.time >= invulnerableUntil;

        /// <summary>
        /// Barrera (el medio corazón verde del Herbívoro): anula un golpe entero antes que los escudos y que la vida.
        /// Se ve en el HUD, así que al cambiar avisa con <see cref="Changed"/>.
        /// </summary>
        public int Barrier
        {
            get => barrier;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (clamped == barrier)
                    return;
                barrier = clamped;
                Changed?.Invoke(Current, maxHealth);
            }
        }

        /// <summary>Golpes que se anulan enteros antes de quitar vida (las Placas del Caparazón).</summary>
        public int Shields
        {
            get => shields;
            set => shields = Mathf.Max(0, value);
        }

        /// <summary>Se lanza al cambiar la vida actual o la máxima: (actual, máxima).</summary>
        public event Action<int, int> Changed;

        /// <summary>Se lanza al recibir daño, con la cantidad recibida.</summary>
        public event Action<int> Damaged;

        public event Action Died;

        /// <summary>Se lanza cuando un escudo anula un golpe.</summary>
        public event Action ShieldBlocked;

        /// <summary>Se lanza cuando la barrera anula un golpe.</summary>
        public event Action BarrierBlocked;

        float invulnerableUntil;
        int shields;
        int barrier;

        /// <summary>
        /// Cambian el daño que recibe cualquier <see cref="Health"/> según quién lo recibe (los beneficios de los
        /// linajes: Rematar, Toxina débil). Reciben el objetivo y el daño, y devuelven el daño nuevo.
        /// </summary>
        static readonly List<Func<Health, int, int>> damageModifiers = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => damageModifiers.Clear();

        public static void AddDamageModifier(Func<Health, int, int> modifier)
        {
            if (modifier != null && !damageModifiers.Contains(modifier))
                damageModifiers.Add(modifier);
        }

        public static void RemoveDamageModifier(Func<Health, int, int> modifier) => damageModifiers.Remove(modifier);

        /// <summary>
        /// Multiplica un daño entero con redondeo al azar: 1 × 1,5 da 1 o 2 con la misma probabilidad, así de media
        /// es exactamente un 50 % más (redondear siempre hacia arriba lo doblaría).
        /// </summary>
        public static int ScaleDamage(int amount, float multiplier)
        {
            float scaled = amount * multiplier;
            int whole = Mathf.FloorToInt(scaled);
            return whole + (UnityEngine.Random.value < scaled - whole ? 1 : 0);
        }

        /// <summary>Daño que haría ese golpe a este objetivo después de los modificadores.</summary>
        public int ModifiedDamage(int amount)
        {
            // Copia: un modificador puede quitarse a sí mismo (o a otro) mientras se recorren.
            if (damageModifiers.Count == 0)
                return amount;
            foreach (var modifier in damageModifiers.ToArray())
                amount = modifier(this, amount);
            return amount;
        }

        void Awake()
        {
            Current = maxHealth;
        }

        /// <summary>Aplica daño. Devuelve false si el golpe no tuvo efecto.</summary>
        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || !CanTakeDamage)
                return false;

            amount = ModifiedDamage(amount);
            if (amount <= 0)
                return false;

            // La barrera y el escudo paran el golpe entero; cuentan como golpe (dan la invulnerabilidad de después).
            if (barrier > 0)
            {
                Barrier = barrier - 1;
                invulnerableUntil = Time.time + invulnerabilityAfterHit;
                BarrierBlocked?.Invoke();
                return true;
            }

            if (shields > 0)
            {
                shields--;
                invulnerableUntil = Time.time + invulnerabilityAfterHit;
                ShieldBlocked?.Invoke();
                return true;
            }

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;

            Damaged?.Invoke(amount);
            Changed?.Invoke(Current, maxHealth);

            if (Current == 0)
            {
                IsDead = true;
                Died?.Invoke();
            }

            return true;
        }

        /// <summary>Invulnerable durante esos segundos (sin tocar <see cref="IsInvulnerable"/>, que lleva la esquiva).</summary>
        public void MakeInvulnerable(float seconds) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
                return;

            int previous = Current;
            Current = Mathf.Min(maxHealth, Current + amount);

            if (Current != previous)
                Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>
        /// Cambia la vida máxima, por ejemplo con el Caparazón (+2).
        /// Si sube y <paramref name="healDifference"/> es true, también cura la diferencia.
        /// </summary>
        public void SetMax(int newMax, bool healDifference = true)
        {
            newMax = Mathf.Max(1, newMax);
            int difference = newMax - maxHealth;
            maxHealth = newMax;

            if (healDifference && difference > 0 && !IsDead)
                Current += difference;

            Current = Mathf.Min(Current, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>Vuelve a la vida máxima y quita el estado de muerte.</summary>
        public void ResetHealth()
        {
            IsDead = false;
            Current = maxHealth;
            invulnerableUntil = 0f;
            Changed?.Invoke(Current, maxHealth);
        }

        [ContextMenu("Prueba: recibir 1 de daño")]
        void DebugTakeDamage()
        {
            bool applied = TakeDamage(1);
            Debug.Log($"{name}: {(applied ? "recibe 1 de daño" : "el golpe no tuvo efecto")} → vida {Current}/{maxHealth}{(IsDead ? " (muerto)" : "")}", this);
        }

        [ContextMenu("Prueba: curar 1")]
        void DebugHeal()
        {
            Heal(1);
            Debug.Log($"{name}: cura 1 → vida {Current}/{maxHealth}", this);
        }
    }
}

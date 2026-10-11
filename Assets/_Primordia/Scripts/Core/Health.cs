using System;
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

        /// <summary>Se lanza al cambiar la vida actual o la máxima: (actual, máxima).</summary>
        public event Action<int, int> Changed;

        /// <summary>Se lanza al recibir daño, con la cantidad recibida.</summary>
        public event Action<int> Damaged;

        public event Action Died;

        float invulnerableUntil;

        void Awake()
        {
            Current = maxHealth;
        }

        /// <summary>Aplica daño. Devuelve false si el golpe no tuvo efecto.</summary>
        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || !CanTakeDamage)
                return false;

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

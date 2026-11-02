using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>De dónde viene un daño: el precio Inestable de las mutaciones especiales solo se suma a los golpes.</summary>
    public enum DamageKind
    {
        /// <summary>Golpe de un enemigo, un proyectil o un jefe.</summary>
        Hit,
        /// <summary>Cada tick de un daño continuo (veneno, sangrado).</summary>
        Tick,
        /// <summary>Caída a una fosa.</summary>
        Fall
    }

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

        /// <summary>
        /// Corazones morados (el del coral de la sala del Corazón morado): cada uno anula un golpe entero, antes que la barrera,
        /// los escudos y la vida, y se gasta con él. El jugador los conserva entre pisos. Se ven en el HUD.
        /// </summary>
        public int PurpleHearts
        {
            get => purpleHearts;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (clamped == purpleHearts)
                    return;
                purpleHearts = clamped;
                // Con un corazón morado, el jugador lleva su aura y su efecto al bloquear.
                if (clamped > 0 && !TryGetComponent<PurpleHeartFx>(out _))
                    gameObject.AddComponent<PurpleHeartFx>();
                Changed?.Invoke(Current, maxHealth);
            }
        }

        /// <summary>True si alguna vez cogió un corazón morado (aunque ya lo haya gastado): abrirá una sala secreta del piso 3.</summary>
        public bool PurpleHeartFound { get; private set; }

        /// <summary>Suma un corazón morado y recuerda que lo encontró.</summary>
        public void AddPurpleHeart()
        {
            PurpleHeartFound = true;
            PurpleHearts++;
        }

        /// <summary>
        /// Daño que se suma a cada golpe (<see cref="DamageKind.Hit"/>): el precio Inestable, medio corazón por cada mutación
        /// especial que lleva el jugador. Lo que para un golpe entero para también este extra.
        /// </summary>
        public int ExtraHitDamage
        {
            get => extraHitDamage;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (clamped == extraHitDamage)
                    return;
                extraHitDamage = clamped;
                Changed?.Invoke(Current, maxHealth);
            }
        }

        /// <summary>Se lanza cuando un golpe quita vida de más por <see cref="ExtraHitDamage"/>, con lo que quitó de más.</summary>
        public event Action<int> ExtraDamageTaken;

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

        /// <summary>Un corazón morado ha parado un golpe.</summary>
        public event Action PurpleBlocked;

        /// <summary>Se lanza cuando un golpe no hace nada porque es invulnerable (la ameba gigante en su escena final).</summary>
        public event Action Deflected;

        float invulnerableUntil;
        int shields;
        int barrier;
        int purpleHearts;
        int extraHitDamage;

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
        /// <param name="kind">De dónde viene: solo los golpes llevan el extra de <see cref="ExtraHitDamage"/>.</param>
        public bool TakeDamage(int amount, DamageKind kind = DamageKind.Hit)
        {
            if (amount <= 0 || !CanTakeDamage)
            {
                if (amount > 0 && !IsDead && IsInvulnerable)
                    Deflected?.Invoke();
                return false;
            }

            amount = ModifiedDamage(amount);
            if (amount <= 0)
                return false;

            // El corazón morado, la barrera y el escudo paran el golpe entero; cuentan como golpe (dan la invulnerabilidad de después).
            if (purpleHearts > 0)
            {
                PurpleHearts = purpleHearts - 1;
                invulnerableUntil = Time.time + invulnerabilityAfterHit;
                PurpleBlocked?.Invoke();
                return true;
            }

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

            int extra = kind == DamageKind.Hit ? extraHitDamage : 0;
            int before = Current;
            Current = Mathf.Max(0, Current - amount - extra);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;

            // Lo que quitó de más el precio Inestable (el HUD rompe ese medio corazón con un destello dorado).
            int extraTaken = Mathf.Clamp(before - amount, 0, extra);
            if (extraTaken > 0)
                ExtraDamageTaken?.Invoke(extraTaken);
            Damaged?.Invoke(amount + extra);
            Changed?.Invoke(Current, maxHealth);

            if (Current == 0)
            {
                IsDead = true;
                Died?.Invoke();
            }

            return true;
        }

        /// <summary>Muerte inmediata aunque sea invulnerable, sin barrera ni escudos (caer en una fosa).</summary>
        public void Kill()
        {
            if (IsDead)
                return;
            int lost = Current;
            Current = 0;
            Damaged?.Invoke(lost);
            Changed?.Invoke(Current, maxHealth);
            IsDead = true;
            Died?.Invoke();
        }

        /// <summary>
        /// Pierde vida sin que cuente como golpe (no avisa con <see cref="Damaged"/>) y sin morir: se queda en 1 como mínimo.
        /// La barra del último aliento del Euriptérido, que se vacía sola.
        /// </summary>
        public void Drain(int amount)
        {
            if (amount <= 0 || IsDead)
                return;
            int previous = Current;
            Current = Mathf.Max(1, Current - amount);
            if (Current != previous)
                Changed?.Invoke(Current, maxHealth);
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

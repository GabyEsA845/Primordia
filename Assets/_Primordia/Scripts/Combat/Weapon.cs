using System;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Dispara (o golpea, si es cuerpo a cuerpo) el ataque definido en un <see cref="AttackData"/>. Lo usan el jugador
    /// (cambia de ataque con las mutaciones de Boca) y los enemigos que disparan.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField] AttackData attack;
        [Tooltip("Equipo del que dispara. Decide a quién pueden dañar sus proyectiles.")]
        [SerializeField] Team team;
        [Tooltip("Punto de salida de los proyectiles. Si está vacío, salen del centro.")]
        [SerializeField] Transform muzzle;

        /// <summary>Ataque actual. Cambiarlo no reinicia la recarga.</summary>
        public AttackData Attack
        {
            get => attack;
            set => attack = value;
        }

        public Team Team => team;

        /// <summary>Punto de salida de los proyectiles (puede ser null).</summary>
        public Transform Muzzle => muzzle;
        public bool CanFire => attack != null && attack.IsUsable && Time.time >= nextFireTime;

        /// <summary>Momento (Time.time) a partir del cual termina la recarga.</summary>
        public float NextFireTime => nextFireTime;

        /// <summary>Se lanza en cada disparo, con la dirección central.</summary>
        public event Action<Vector2> Fired;

        /// <summary>
        /// Multiplica el daño de cada disparo o mordisco (la Emboscada de las Aletas). 1 = normal. Al leerlo incluye
        /// también los bonus de <see cref="SetDamageBonus"/>; asignarlo solo cambia el multiplicador base.
        /// </summary>
        public float DamageMultiplier
        {
            get
            {
                float total = baseMultiplier;
                foreach (var bonus in damageBonuses.Values)
                    total *= bonus;
                return total;
            }
            set => baseMultiplier = value;
        }

        /// <summary>Multiplicador de daño de una fuente (el Camuflaje); se combina con los demás.</summary>
        public void SetDamageBonus(object source, float multiplier) => damageBonuses[source] = multiplier;

        public void ClearDamageBonus(object source) => damageBonuses.Remove(source);

        float baseMultiplier = 1f;
        readonly Dictionary<object, float> damageBonuses = new();

        /// <summary>Daño de ese ataque con el multiplicador actual (redondeado hacia arriba).</summary>
        public int DamageOf(AttackData attack) =>
            Mathf.Max(0, Mathf.CeilToInt(attack.Damage * DamageMultiplier - 0.001f));

        /// <summary>Se lanza cuando un proyectil de esta arma hace daño. Si el objetivo murió, su IsDead es true.</summary>
        public event Action<Health> HitTarget;

        float nextFireTime;
        int projectileLayer;
        IMeleeLunger lunger;

        // Mordisco con avance que aún no ha golpeado: golpea al acabar el avance o al tener a alguien a tiro.
        AttackData pendingAttack;
        Vector2 pendingDirection;
        float pendingStrikeTime;

        /// <summary>True mientras se lanza hacia delante antes de morder.</summary>
        public bool IsLunging => pendingAttack != null;

        void Awake() => projectileLayer = CombatLayers.ProjectileLayer(team);

        void OnDisable() => pendingAttack = null;

        void Update()
        {
            if (pendingAttack == null)
                return;

            if (Time.time < pendingStrikeTime && !AnyTargetInReach(pendingDirection, pendingAttack))
                return;

            var attack = pendingAttack;
            pendingAttack = null;
            lunger?.StopLunge();
            Strike(pendingDirection, MuzzlePosition, attack);
        }

        Vector2 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;

        /// <summary>Dispara si la recarga lo permite. Devuelve false si no disparó.</summary>
        public bool TryFire(Vector2 direction)
        {
            if (!CanFire || direction.sqrMagnitude < 0.0001f)
                return false;

            nextFireTime = Time.time + attack.Cooldown;
            Fire(direction);
            return true;
        }

        /// <summary>Dispara ya, sin mirar la recarga.</summary>
        public void Fire(Vector2 direction) => Fire(direction, attack);

        /// <summary>
        /// Dispara otro ataque desde esta arma, sin mirar la recarga ni cambiar el ataque actual.
        /// Útil para patrones de jefe con varios tipos de proyectil.
        /// </summary>
        public void Fire(Vector2 direction, AttackData attack)
        {
            if (attack == null || !attack.IsUsable || direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            Vector2 origin = muzzle != null ? muzzle.position : transform.position;

            if (attack.IsMelee)
            {
                if (!TryLunge(direction, attack))
                    Strike(direction, origin, attack);
                Fired?.Invoke(direction);
                return;
            }

            int count = attack.ProjectileCount;
            float step = count > 1 ? attack.SpreadAngle / (count - 1) : 0f;
            float firstAngle = count > 1 ? -attack.SpreadAngle / 2f : 0f;

            for (int i = 0; i < count; i++)
            {
                var projectileDirection = Rotate(direction, firstAngle + step * i);
                ProjectilePool.Get(attack.ProjectilePrefab).Launch(origin, projectileDirection, attack, projectileLayer, this);
            }

            Fired?.Invoke(direction);
        }

        internal void ReportHit(Health target) => HitTarget?.Invoke(target);

        internal void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;

        /// <summary>
        /// Mordisco con avance: si nadie está a tiro, se lanza hacia delante y deja el golpe para el final del
        /// avance (o para cuando alguien entre en el alcance). Devuelve false si golpea ya.
        /// </summary>
        bool TryLunge(Vector2 direction, AttackData attack)
        {
            // Se busca al usarlo: quien se lanza puede añadirse después del arma (pruebas).
            lunger ??= GetComponentInParent<IMeleeLunger>();
            if (attack.MeleeLunge <= 0f || lunger == null || AnyTargetInReach(direction, attack))
                return false;

            float duration = lunger.Lunge(direction, attack.MeleeLunge, attack.MeleeLungeDuration);
            if (duration <= 0f)
                return false;

            pendingAttack = attack;
            pendingDirection = direction;
            pendingStrikeTime = Time.time + duration;
            return true;
        }

        bool AnyTargetInReach(Vector2 direction, AttackData attack)
        {
            foreach (var _ in TargetsInReach(direction, attack))
                return true;
            return false;
        }

        /// <summary>
        /// Objetivos del otro equipo dentro del alcance (medido desde el centro del atacante) y del arco delante
        /// de él, una vez cada uno.
        /// </summary>
        IEnumerable<(Health health, Collider2D collider)> TargetsInReach(Vector2 direction, AttackData attack)
        {
            Vector2 center = transform.position;
            var ownHealth = GetComponentInParent<Health>();
            int targets = LayerMask.GetMask(team == Team.Player ? CombatLayers.Enemy : CombatLayers.Player);
            var seen = new HashSet<Health>();

            foreach (var hit in Physics2D.OverlapCircleAll(center, attack.Range, targets))
            {
                if (hit.isTrigger)
                    continue;

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health == ownHealth || health.IsDead || !seen.Add(health))
                    continue;

                // El punto del colisionador más cercano: así cuenta aunque solo asome dentro del arco.
                Vector2 toTarget = hit.ClosestPoint(center) - center;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(direction, toTarget) > attack.MeleeArc / 2f)
                    continue;

                yield return (health, hit);
            }
        }

        /// <summary>
        /// Golpe cuerpo a cuerpo: daña una vez a cada objetivo del otro equipo que esté dentro del alcance
        /// (medido desde el centro del atacante) y del arco delante de él.
        /// </summary>
        void Strike(Vector2 direction, Vector2 effectPosition, AttackData attack)
        {
            // Se recogen antes de dañar: una muerte puede cambiar la escena mientras se recorren.
            var hits = new List<(Health health, Collider2D collider)>(TargetsInReach(direction, attack));
            foreach (var (health, hit) in hits)
            {
                if (health == null)
                    continue;

                if (!health.TakeDamage(DamageOf(attack)))
                    continue;

                OneShotEffect.Spawn(attack.MeleeHitEffect, hit.bounds.center);
                attack.ApplyStatus(health);
                ReportHit(health);
            }

            if (attack.MeleeEffect != null)
            {
                // Cuelga del mismo padre que la boca, así sigue al jugador mientras dura.
                var parent = muzzle != null ? muzzle.parent : transform;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var effect = Instantiate(attack.MeleeEffect, effectPosition, Quaternion.Euler(0f, 0f, angle), parent);
                Destroy(effect, attack.Lifetime);
            }
        }

        static Vector2 Rotate(Vector2 vector, float degrees) => Quaternion.Euler(0f, 0f, degrees) * vector;
    }
}

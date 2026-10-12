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
        public bool CanFire => attack != null && attack.IsUsable && Time.time >= nextFireTime;

        /// <summary>Momento (Time.time) a partir del cual termina la recarga.</summary>
        public float NextFireTime => nextFireTime;

        /// <summary>Se lanza en cada disparo, con la dirección central.</summary>
        public event Action<Vector2> Fired;

        /// <summary>Se lanza cuando un proyectil de esta arma hace daño. Si el objetivo murió, su IsDead es true.</summary>
        public event Action<Health> HitTarget;

        float nextFireTime;
        int projectileLayer;
        readonly HashSet<Health> hitThisStrike = new();

        void Awake() => projectileLayer = CombatLayers.ProjectileLayer(team);

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

        /// <summary>
        /// Golpe cuerpo a cuerpo: daña una vez a cada objetivo del otro equipo que esté dentro del alcance
        /// (medido desde el centro del atacante) y del arco delante de él.
        /// </summary>
        void Strike(Vector2 direction, Vector2 effectPosition, AttackData attack)
        {
            Vector2 center = transform.position;
            var ownHealth = GetComponentInParent<Health>();
            int targets = LayerMask.GetMask(team == Team.Player ? CombatLayers.Enemy : CombatLayers.Player);

            hitThisStrike.Clear();
            foreach (var hit in Physics2D.OverlapCircleAll(center, attack.Range, targets))
            {
                if (hit.isTrigger)
                    continue;

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health == ownHealth || !hitThisStrike.Add(health))
                    continue;

                // El punto del colisionador más cercano: así cuenta aunque solo asome dentro del arco.
                Vector2 toTarget = hit.ClosestPoint(center) - center;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(direction, toTarget) > attack.MeleeArc / 2f)
                    continue;

                if (!health.TakeDamage(attack.Damage))
                    continue;

                OneShotEffect.Spawn(attack.MeleeHitEffect, hit.bounds.center);
                if (attack.AppliesPoison && !health.IsDead)
                    Poison.Apply(health, attack.PoisonDamagePerSecond, attack.PoisonDuration, attack.PoisonEffect);
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

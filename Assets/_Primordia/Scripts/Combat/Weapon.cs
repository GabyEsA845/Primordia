using System;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Dispara el ataque definido en un <see cref="AttackData"/>. Lo usan el jugador
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
        public bool CanFire => attack != null && attack.ProjectilePrefab != null && Time.time >= nextFireTime;

        /// <summary>Se lanza en cada disparo, con la dirección central.</summary>
        public event Action<Vector2> Fired;

        /// <summary>Se lanza cuando un proyectil de esta arma hace daño. Si el objetivo murió, su IsDead es true.</summary>
        public event Action<Health> HitTarget;

        float nextFireTime;
        int projectileLayer;

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

        /// <summary>Dispara ya, sin mirar la recarga. Útil para patrones de jefe.</summary>
        public void Fire(Vector2 direction)
        {
            if (attack == null || attack.ProjectilePrefab == null || direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            Vector2 origin = muzzle != null ? muzzle.position : transform.position;

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

        static Vector2 Rotate(Vector2 vector, float degrees) => Quaternion.Euler(0f, 0f, degrees) * vector;
    }
}

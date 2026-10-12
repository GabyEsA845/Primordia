using Primordia.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Primordia.Combat
{
    /// <summary>
    /// Proyectil en línea recta. Daña al primer cuerpo con <see cref="Health"/> que toca y desaparece;
    /// también desaparece al chocar con una pared o al acabarse su tiempo de vida.
    /// Si el objetivo no recibe daño (por ejemplo, porque está esquivando), lo atraviesa.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Efecto al golpear un objetivo o una pared (no al acabarse su tiempo de vida).")]
        [SerializeField] GameObject impactEffect;

        public int Damage { get; private set; }

        Rigidbody2D rb;
        ObjectPool<Projectile> pool;
        AttackData attack;
        Weapon source;
        float expireTime;
        bool launched;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            GetComponent<Collider2D>().isTrigger = true;
        }

        internal void SetPool(ObjectPool<Projectile> owner) => pool = owner;

        internal void SetImpactEffect(GameObject effect) => impactEffect = effect;

        /// <param name="source">Arma que disparó; recibe el aviso de cada impacto. Puede ser null.</param>
        public void Launch(Vector2 position, Vector2 direction, AttackData attack, int layer, Weapon source)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            gameObject.layer = layer;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = direction.normalized * attack.ProjectileSpeed;

            this.attack = attack;
            this.source = source;
            Damage = attack.Damage;
            expireTime = Time.time + attack.Lifetime;
            launched = true;
        }

        void FixedUpdate()
        {
            if (launched && Time.time >= expireTime)
                Despawn();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Las zonas trigger (detectores de sala, etc.) no detienen proyectiles.
            if (!launched || other.isTrigger)
                return;

            var health = other.GetComponentInParent<Health>();
            if (health == null)
            {
                OneShotEffect.Spawn(impactEffect, transform.position);
                Despawn();
                return;
            }

            if (!health.TakeDamage(Damage))
                return;

            OneShotEffect.Spawn(impactEffect, transform.position);
            if (attack.AppliesPoison && !health.IsDead)
                Poison.Apply(health, attack.PoisonDamagePerSecond, attack.PoisonDuration, attack.PoisonEffect);

            // El arma puede haberse destruido (enemigo muerto) mientras el proyectil volaba.
            if (source != null)
                source.ReportHit(health);

            Despawn();
        }

        public void Despawn()
        {
            if (!launched)
                return;

            launched = false;
            source = null;
            rb.linearVelocity = Vector2.zero;

            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}

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
        public int Damage { get; private set; }

        Rigidbody2D rb;
        ObjectPool<Projectile> pool;
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

        public void Launch(Vector2 position, Vector2 direction, float speed, int damage, float lifetime, int layer)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            gameObject.layer = layer;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = direction.normalized * speed;

            Damage = damage;
            expireTime = Time.time + lifetime;
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
                Despawn();
                return;
            }

            if (health.TakeDamage(Damage))
                Despawn();
        }

        public void Despawn()
        {
            if (!launched)
                return;

            launched = false;
            rb.linearVelocity = Vector2.zero;

            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}

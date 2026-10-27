using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Púa venenosa del caparazón del Euriptérido: vuela en línea recta y, si toca al jugador, le quita medio corazón y lo
    /// ralentiza (<see cref="Venom"/>). Se rompe contra las paredes, las rocas y la muda vacía (todo lo de la capa Default
    /// que no es trigger). No se puede disparar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EurypteridSpine : MonoBehaviour
    {
        static readonly List<EurypteridSpine> all = new();

        [SerializeField, Min(0)] int damage = 1;
        [SerializeField, Min(0.05f)] float touchRadius = 0.25f;
        [SerializeField, Min(0.1f)] float lifetime = 2.5f;
        [Tooltip("Velocidad del jugador mientras dura el veneno (0,7 = un 30 % más lento) y cuánto dura.")]
        [SerializeField, Range(0f, 1f)] float slowFactor = 0.7f;
        [SerializeField, Min(0f)] float slowDuration = 1.5f;

        Rigidbody2D rb;
        Vector2 velocity;
        Health owner;
        float age;
        bool done;
        readonly Collider2D[] touching = new Collider2D[4];
        readonly RaycastHit2D[] hits = new RaycastHit2D[4];

        public static IReadOnlyList<EurypteridSpine> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public Vector2 Velocity => velocity;

        internal void Configure(int newDamage, float newLifetime, float newSlowFactor, float newSlowDuration)
        {
            damage = newDamage;
            lifetime = newLifetime;
            slowFactor = newSlowFactor;
            slowDuration = newSlowDuration;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        /// <summary>Echa a volar la púa; <paramref name="newOwner"/> es el jefe (la esquiva justa de la Cutícula se la devuelve).</summary>
        public void Launch(Vector2 newVelocity, Health newOwner = null)
        {
            velocity = newVelocity;
            owner = newOwner;
            age = 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        void FixedUpdate()
        {
            if (done)
                return;
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (age >= lifetime)
            {
                Break();
                return;
            }

            var step = velocity * dt;
            float distance = step.magnitude;
            if (distance > 0f)
            {
                var filter = new ContactFilter2D { useTriggers = false };
                filter.SetLayerMask(LayerMask.GetMask("Default"));
                if (Physics2D.CircleCast(rb.position, touchRadius * 0.5f, step / distance, filter, hits, distance) > 0)
                {
                    Break();
                    return;
                }
                rb.MovePosition(rb.position + step);
            }
            TouchPlayer();
        }

        void TouchPlayer()
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Player));
            int count = Physics2D.OverlapCircle(rb.position, touchRadius, filter, touching);
            for (int i = 0; i < count; i++)
            {
                var target = touching[i].GetComponentInParent<Health>();
                if (target == null)
                    continue;
                // La esquiva justa de la Cutícula la rompe sin daño.
                var reflector = touching[i].GetComponentInParent<IHitReflector>();
                if (reflector == null || !reflector.TryReflect(owner))
                {
                    if (target.TakeDamage(damage))
                        Venom.Apply(target, slowFactor, slowDuration);
                }
                Break();
                return;
            }
        }

        void Break()
        {
            done = true;
            Destroy(gameObject);
        }

        /// <summary>Rompe todas (al morir el jefe o al empezar la muda).</summary>
        public static void BreakAll()
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] != null)
                    all[i].Break();
            }
        }
    }
}

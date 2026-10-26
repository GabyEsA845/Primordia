using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Burbuja del Anomalocaris (estela del acecho y muro del cerco): deriva despacio unos segundos y, si toca al jugador,
    /// le hace medio corazón y revienta. Tiene 1 de vida en la capa Enemy, así que los disparos y mordiscos del jugador la
    /// revientan; no choca con el jugador (lo atraviesa) ni cuenta como enemigo de la sala.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class AnomalocarisBubble : MonoBehaviour
    {
        static readonly List<AnomalocarisBubble> all = new();

        [SerializeField, Min(0)] int damage = 1;
        [Tooltip("Radio con el que toca al jugador (unidades).")]
        [SerializeField, Min(0.05f)] float touchRadius = 0.3f;
        [Tooltip("Segundos que dura; el último medio segundo se desvanece.")]
        [SerializeField, Min(0.1f)] float lifetime = 4f;
        [SerializeField] GameObject popEffect;

        Rigidbody2D rb;
        Health health;
        SpriteRenderer sprite;
        Vector2 velocity;
        float age;
        float phase;
        bool popped;
        readonly Collider2D[] touching = new Collider2D[4];

        /// <summary>Las burbujas que hay ahora en la escena.</summary>
        public static IReadOnlyList<AnomalocarisBubble> All => all;

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public float Lifetime => lifetime;
        public bool Popped => popped;

        internal void Configure(int newDamage, float newLifetime, GameObject newPopEffect)
        {
            damage = newDamage;
            lifetime = newLifetime;
            popEffect = newPopEffect;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            // Atraviesa al jugador: el daño lo hace al tocarlo, sin empujarlo.
            rb.excludeLayers = LayerMask.GetMask(CombatLayers.Player);
            health = GetComponent<Health>();
            health.SetMax(1, healDifference: false);
            health.ResetHealth();
            sprite = GetComponentInChildren<SpriteRenderer>();
            phase = Random.value * Mathf.PI * 2f;
        }

        void OnEnable()
        {
            all.Add(this);
            health.Died += OnShot;
        }

        void OnDisable()
        {
            all.Remove(this);
            health.Died -= OnShot;
        }

        /// <summary>Echa a andar la burbuja con esa velocidad (unidades por segundo) y, si se da, otra duración.</summary>
        public void Launch(Vector2 newVelocity, float newLifetime = 0f)
        {
            velocity = newVelocity;
            if (newLifetime > 0f)
                lifetime = newLifetime;
            age = 0f;
        }

        void FixedUpdate()
        {
            if (popped)
                return;

            age += Time.fixedDeltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Frena poco a poco y sube y baja un poco, como una burbuja de verdad.
            velocity *= 1f - 0.6f * Time.fixedDeltaTime;
            var wobble = new Vector2(0f, Mathf.Sin(age * 3f + phase) * 0.15f);
            rb.MovePosition(rb.position + (velocity + wobble) * Time.fixedDeltaTime);

            if (sprite != null)
            {
                var color = sprite.color;
                color.a = Mathf.Clamp01((lifetime - age) / 0.5f);
                sprite.color = color;
            }

            TouchPlayer();
        }

        void TouchPlayer()
        {
            if (damage <= 0)
                return;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Player));
            int count = Physics2D.OverlapCircle(rb.position, touchRadius, filter, touching);
            for (int i = 0; i < count; i++)
            {
                var target = touching[i].GetComponentInParent<Health>();
                if (target == null || target == health)
                    continue;
                // La esquiva justa de la Cutícula la revienta sin daño.
                var reflector = touching[i].GetComponentInParent<IHitReflector>();
                if (reflector == null || !reflector.TryReflect(health))
                    target.TakeDamage(damage);
                Pop();
                return;
            }
        }

        void OnShot() => Pop();

        /// <summary>Revienta con su efecto.</summary>
        public void Pop()
        {
            if (popped)
                return;
            popped = true;
            OneShotEffect.Spawn(popEffect, transform.position);
            Destroy(gameObject);
        }

        /// <summary>Revienta todas (al morir el jefe o al empezar una escena).</summary>
        public static void PopAll()
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] != null)
                    all[i].Pop();
            }
        }
    }
}

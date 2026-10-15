using Primordia.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Primordia.Combat
{
    /// <summary>
    /// Proyectil en línea recta. Daña al primer cuerpo con <see cref="Health"/> que toca y desaparece;
    /// también desaparece al chocar con una pared o al acabarse su tiempo de vida.
    /// Si el objetivo no recibe daño (por ejemplo, porque está esquivando), lo atraviesa.
    /// Con <see cref="AttackData.BrakeTime"/> frena y flota como una mina que se dispersa despacio (las esporas).
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
        Transform homingTarget;
        float nextTargetSearch;
        Vector2 direction;
        float speed;
        float launchTime;
        float wobbleSeed;
        SpriteRenderer[] renderers;
        Color[] baseColors;

        // Cada cuánto vuelve a buscar el enemigo más cercano un proyectil buscador.
        const float TargetSearchInterval = 0.15f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            GetComponent<Collider2D>().isTrigger = true;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].color;
        }

        internal void SetPool(ObjectPool<Projectile> owner) => pool = owner;

        internal void SetImpactEffect(GameObject effect) => impactEffect = effect;

        /// <param name="source">Arma que disparó; recibe el aviso de cada impacto. Puede ser null.</param>
        public void Launch(Vector2 position, Vector2 direction, AttackData attack, int layer, Weapon source)
        {
            float speed = attack.ProjectileSpeed;
            if (attack.SpeedJitter > 0f)
                speed *= Random.Range(1f - attack.SpeedJitter, 1f + attack.SpeedJitter);
            if (attack.AngleJitter > 0f)
                direction = Quaternion.Euler(0f, 0f, Random.Range(-attack.AngleJitter, attack.AngleJitter)) * direction;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            gameObject.layer = layer;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = direction.normalized * speed;
            this.direction = direction.normalized;
            this.speed = speed;
            launchTime = Time.time;
            wobbleSeed = Random.value * 100f;
            SetAlpha(1f);

            this.attack = attack;
            this.source = source;
            Damage = source != null ? source.DamageOf(attack) : attack.Damage;
            nextTargetSearch = 0f;
            homingTarget = null;
            expireTime = Time.time + attack.Lifetime;
            launched = true;
        }

        void FixedUpdate()
        {
            if (launched && Time.time >= expireTime)
            {
                Despawn();
                return;
            }

            if (!launched)
                return;

            if (attack.BrakeTime > 0f)
                Float();
            else if (attack.HomingTurnRate > 0f)
                Steer();

            if (attack.FadeTime > 0f)
                SetAlpha(Mathf.Clamp01((expireTime - Time.time) / attack.FadeTime));
        }

        /// <summary>
        /// Frena en <see cref="AttackData.BrakeTime"/> hasta la velocidad de dispersión y después sigue alejándose
        /// despacio, con un leve vaivén; con deriva, se acerca al enemigo más cercano si lo hay (Esporas nivel III).
        /// </summary>
        void Float()
        {
            float scatter = Mathf.Min(attack.ScatterSpeed, speed);
            float t = (Time.time - launchTime) / attack.BrakeTime;
            if (t < 1f)
            {
                rb.linearVelocity = direction * Mathf.Lerp(speed, scatter, t);
                return;
            }

            if (attack.DriftSpeed > 0f)
            {
                if (Time.time >= nextTargetSearch)
                {
                    nextTargetSearch = Time.time + TargetSearchInterval;
                    homingTarget = FindTarget();
                }

                if (homingTarget != null)
                {
                    rb.linearVelocity = ((Vector2)homingTarget.position - rb.position).normalized * attack.DriftSpeed;
                    return;
                }
            }

            // Vaivén de hasta ±40° alrededor de su dirección, distinto en cada espora.
            float wobble = (Mathf.PerlinNoise(wobbleSeed, Time.time * WobbleFrequency) - 0.5f) * 80f;
            rb.linearVelocity = (Vector2)(Quaternion.Euler(0f, 0f, wobble) * direction) * scatter;
        }

        const float WobbleFrequency = 0.8f;

        void SetAlpha(float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var color = baseColors[i];
                color.a *= alpha;
                renderers[i].color = color;
            }
        }

        /// <summary>Gira poco a poco hacia el objetivo más cercano del otro equipo (Esporas nivel III).</summary>
        void Steer()
        {
            if (Time.time >= nextTargetSearch)
            {
                nextTargetSearch = Time.time + TargetSearchInterval;
                homingTarget = FindTarget();
            }

            if (homingTarget == null)
                return;

            var velocity = rb.linearVelocity;
            float current = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            Vector2 toTarget = (Vector2)homingTarget.position - rb.position;
            float wanted = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(current, wanted, attack.HomingTurnRate * Time.fixedDeltaTime);

            rb.rotation = angle;
            rb.linearVelocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * velocity.magnitude;
        }

        Transform FindTarget()
        {
            // Los proyectiles del jugador buscan enemigos, y al revés.
            string targetLayer = gameObject.layer == CombatLayers.ProjectileLayer(Team.Player) ? CombatLayers.Enemy : CombatLayers.Player;
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var hit in Physics2D.OverlapCircleAll(rb.position, attack.HomingRange, LayerMask.GetMask(targetLayer)))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;

                float distance = ((Vector2)hit.transform.position - rb.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = hit.transform;
                }
            }
            return best;
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
            attack.ApplyStatus(health);

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

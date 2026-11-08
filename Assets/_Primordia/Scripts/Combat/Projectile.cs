using System.Collections.Generic;
using Primordia.Audio;
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

        /// <summary>Ataque con el que se lanzó (null antes del primer lanzamiento).</summary>
        public AttackData Attack => attack;

        /// <summary>Por qué acaba un proyectil; lo reciben los que reaccionan al final (el Escupitajo venenoso).</summary>
        public enum EndReason
        {
            /// <summary>Quitado sin más: escena, jefe, rebote…</summary>
            Cleared,
            /// <summary>Se acabó su tiempo de vida.</summary>
            Expired,
            /// <summary>Chocó con una pared u obstáculo.</summary>
            Wall,
            /// <summary>Dio a alguien y no atraviesa más.</summary>
            Hit,
            /// <summary>Lo hizo estallar quien lo disparó.</summary>
            Detonated
        }

        /// <summary>Se lanza al terminar <see cref="Launch"/>.</summary>
        public event System.Action<Projectile> Launched;

        /// <summary>Se lanza justo antes de quitarlo, con el motivo. Aún conserva su arma y su posición.</summary>
        public event System.Action<Projectile, EndReason> Ending;

        /// <summary>Arma que lo disparó (puede ser null o haberse destruido).</summary>
        public Weapon Source => source;

        /// <summary>Velocidad actual.</summary>
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        /// <summary>Multiplicador del veneno del arma al lanzarlo (como el daño, se guarda al salir).</summary>
        public float PoisonMultiplier { get; private set; } = 1f;

        Rigidbody2D rb;
        ObjectPool<Projectile> pool;
        AttackData attack;
        Weapon source;
        // Lo que ha vivido, en su tiempo: los del jugador no se frenan en la cámara lenta (ver TimeWarp).
        float age;
        float warpAtLaunch = 1f;
        bool playerShot;
        bool launched;
        Transform homingTarget;
        // Atravesar: enemigos que aún puede atravesar y los ya alcanzados (no les vuelve a dar).
        int piercesLeft;
        readonly HashSet<Health> struck = new();
        // Objetivo fijo de un proyectil buscador (el reflejado de la Cutícula vuelve a quien lo lanzó).
        Transform fixedTarget;
        float nextTargetSearch;
        Vector2 direction;
        float speed;
        float wobbleSeed;
        SpriteRenderer[] renderers;
        Color[] baseColors;

        // Cada cuánto vuelve a buscar el enemigo más cercano un proyectil buscador.
        const float TargetSearchInterval = 0.15f;

        // Proyectiles en vuelo, para poder quitarlos todos de golpe (escenas de los jefes).
        static readonly List<Projectile> active = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            AnyHit = null;
        }

        /// <summary>Se lanza cuando cualquier proyectil hace daño, con el proyectil y la vida (la Cosecha de la Wiwaxia).</summary>
        public static event System.Action<Projectile, Health> AnyHit;

        /// <summary>Proyectiles en vuelo ahora mismo, de los dos equipos.</summary>
        public static int ActiveCount => active.Count;

        /// <summary>Los proyectiles en vuelo, de los dos equipos (para buscar los de un equipo por su capa).</summary>
        public static IReadOnlyList<Projectile> Active => active;

        /// <summary>Quita todos los proyectiles en vuelo, de los dos equipos, sin efecto de impacto.</summary>
        public static void DespawnAll()
        {
            foreach (var projectile in active.ToArray())
            {
                if (projectile != null)
                    projectile.Despawn();
            }
            active.Clear();
        }

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

        // Al descargar la escena se destruyen sin pasar por Despawn.
        void OnDestroy() => active.Remove(this);

        internal void SetPool(ObjectPool<Projectile> owner) => pool = owner;

        internal void SetImpactEffect(GameObject effect) => impactEffect = effect;

        /// <summary>
        /// Después de <see cref="Launch"/>: otro daño y, si el ataque es buscador, ese objetivo en lugar del más cercano
        /// (el proyectil reflejado de la Cutícula, que vuelve a quien lo lanzó).
        /// </summary>
        public void Redirect(int damage, Transform target)
        {
            Damage = Mathf.Max(0, damage);
            fixedTarget = target;
        }

        /// <summary>Cambia su velocidad en ese vector (el viento de un jefe): sigue su camino con la nueva dirección.</summary>
        public void Nudge(Vector2 velocityDelta)
        {
            if (!launched || rb == null)
                return;
            rb.linearVelocity += velocityDelta;
            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                direction = rb.linearVelocity.normalized;
                rb.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            }
        }

        /// <param name="source">Arma que disparó; recibe el aviso de cada impacto. Puede ser null.</param>
        public void Launch(Vector2 position, Vector2 direction, AttackData attack, int layer, Weapon source)
        {
            float speed = attack.ProjectileSpeed;
            playerShot = layer == CombatLayers.ProjectileLayer(Team.Player);
            warpAtLaunch = playerShot ? TimeWarp.Scale : 1f;
            speed /= warpAtLaunch;
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
            age = 0f;
            wobbleSeed = Random.value * 100f;
            SetAlpha(1f);

            this.attack = attack;
            this.source = source;
            Damage = source != null ? source.DamageOf(attack) : attack.Damage;
            PoisonMultiplier = source != null ? source.PoisonMultiplier : 1f;
            nextTargetSearch = 0f;
            homingTarget = null;
            fixedTarget = null;
            piercesLeft = attack.Pierce;
            struck.Clear();

            launched = true;
            active.Add(this);
            Launched?.Invoke(this);
        }

        void FixedUpdate()
        {
            if (!launched)
                return;

            // Si la cámara lenta empieza o acaba en pleno vuelo, el del jugador corrige su velocidad.
            if (playerShot && !Mathf.Approximately(warpAtLaunch, TimeWarp.Scale))
            {
                float correction = warpAtLaunch / TimeWarp.Scale;
                rb.linearVelocity *= correction;
                speed *= correction;
                warpAtLaunch = TimeWarp.Scale;
            }

            age += Time.fixedDeltaTime * (playerShot ? TimeWarp.PlayerSpeed : 1f);
            if (age >= attack.Lifetime)
            {
                Despawn(EndReason.Expired);
                return;
            }

            if (attack.BrakeTime > 0f)
                Float();
            else if (attack.HomingTurnRate > 0f)
                Steer();

            if (attack.FadeTime > 0f)
                SetAlpha(Mathf.Clamp01((attack.Lifetime - age) / attack.FadeTime));
        }

        /// <summary>
        /// Frena en <see cref="AttackData.BrakeTime"/> hasta la velocidad de dispersión y después sigue alejándose
        /// despacio, con un leve vaivén; con deriva, se acerca al enemigo más cercano si lo hay (Esporas nivel III).
        /// </summary>
        void Float()
        {
            float scatter = Mathf.Min(attack.ScatterSpeed, speed);
            float t = age / attack.BrakeTime;
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
            if (fixedTarget != null)
            {
                homingTarget = fixedTarget;
            }
            else if (Time.time >= nextTargetSearch)
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
                Sfx.Play(attack.ImpactSound);
                Despawn(EndReason.Wall);
                return;
            }

            // La esquiva justa de la Cutícula lo devuelve antes de que haga daño.
            if (gameObject.layer != CombatLayers.ProjectileLayer(Team.Player))
            {
                bool reflected = false;
                foreach (var reflector in other.GetComponentsInParent<IHitReflector>())
                    reflected |= reflector.TryReflect(this);
                if (reflected)
                {
                    Despawn();
                    return;
                }
            }

            // Al atravesar, no vuelve a dar al mismo.
            if (struck.Contains(health))
                return;

            if (!health.TakeDamage(Damage))
                return;

            struck.Add(health);
            OneShotEffect.Spawn(impactEffect, transform.position);
            Sfx.Play(attack.ImpactSound);
            attack.ApplyStatus(health, PoisonMultiplier);

            // El arma puede haberse destruido (enemigo muerto) mientras el proyectil volaba.
            if (source != null)
                source.ReportHit(health);
            AnyHit?.Invoke(this, health);

            if (piercesLeft > 0)
            {
                piercesLeft--;
                return;
            }
            Despawn(EndReason.Hit);
        }

        public void Despawn() => Despawn(EndReason.Cleared);

        public void Despawn(EndReason reason)
        {
            if (!launched)
                return;

            launched = false;
            Ending?.Invoke(this, reason);
            active.Remove(this);
            source = null;
            rb.linearVelocity = Vector2.zero;

            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}

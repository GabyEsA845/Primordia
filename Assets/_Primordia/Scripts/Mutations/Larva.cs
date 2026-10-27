using System;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Larva aliada del Saco de crías: nada (por encima de las fosas, rodeando paredes) hacia el enemigo vivo más cercano y lo
    /// muerde cada poco. Cada larva ataca desde su propio lado (un ángulo distinto alrededor del objetivo) y se aparta de las
    /// demás, para que se vea que son varias y lo rodean. Sin enemigos, se queda cerca del jugador, también cada una en su
    /// sitio. Al acabar su tiempo se deshace. Con el Enjambre, la que mata a un enemigo se divide en dos.
    /// </summary>
    public class Larva : MonoBehaviour
    {
        [Serializable]
        public struct Settings
        {
            public float lifetime;
            public float speed;
            public float range;
            public int biteDamage;
            public float biteInterval;
            public float biteReach;
            [NonSerialized] public bool splits;
        }

        const float RetargetInterval = 0.25f;
        const float BodyRadius = 0.25f;
        const float FollowDistance = 1.2f;
        // Distancia a la que se apartan unas de otras, y cuánto pesa apartarse frente a ir a su sitio.
        const float SeparationRadius = 0.7f;
        const float SeparationWeight = 1.2f;
        // Ángulo áureo: así las larvas que salen seguidas quedan repartidas alrededor sin coincidir nunca.
        const float GoldenAngle = 137.5f;

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] swim = Array.Empty<Sprite>();
        [SerializeField] Sprite[] bite = Array.Empty<Sprite>();
        [SerializeField] Sprite[] death = Array.Empty<Sprite>();
        [SerializeField] GameObject splitEffect;
        [SerializeField, Min(0.01f)] float swimFrame = 0.11f;
        [SerializeField, Min(0.01f)] float biteFrame = 0.07f;
        [SerializeField, Min(0.01f)] float deathFrame = 0.09f;

        Settings settings;
        Larva prefab;
        PlayerMutations owner;
        Health target;
        float until;
        float nextBite;
        float nextSearch;
        float biteStart = float.NegativeInfinity;
        bool dying;
        Vector2 side = Vector2.right;

        static readonly List<Larva> all = new();
        static int spawned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            spawned = 0;
        }

        /// <summary>Lado desde el que ataca (y se coloca junto al jugador): uno distinto para cada larva.</summary>
        public Vector2 Side => side;

        /// <summary>Larvas vivas del jugador.</summary>
        public static int CountOf(PlayerMutations player) => all.FindAll(l => l != null && l.owner == player && !l.dying).Count;

        public Health Target => target;
        public bool IsDying => dying;

        /// <summary>Se lanza con cada mordisco, con la vida mordida (los Pólipos urticantes envenenan aquí).</summary>
        public static event Action<Larva, Health> AnyBit;

        public static Larva Spawn(Larva prefab, Vector2 position, Settings settings, PlayerMutations owner)
        {
            var larva = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity)
                : new GameObject("Larva").AddComponent<Larva>();
            larva.transform.position = position;
            larva.prefab = prefab;
            larva.settings = settings;
            larva.owner = owner;
            larva.until = Time.time + settings.lifetime;
            larva.nextBite = Time.time + 0.2f;
            float angle = spawned++ * GoldenAngle * Mathf.Deg2Rad;
            larva.side = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return larva;
        }

        internal void Configure(SpriteRenderer newBody, Sprite[] newSwim, Sprite[] newBite, Sprite[] newDeath, GameObject newSplit)
        {
            body = newBody;
            swim = newSwim ?? Array.Empty<Sprite>();
            bite = newBite ?? Array.Empty<Sprite>();
            death = newDeath ?? Array.Empty<Sprite>();
            splitEffect = newSplit;
        }

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (dying)
                return;
            if (Time.time >= until)
            {
                StartCoroutine(Die());
                return;
            }

            if (Time.time >= nextSearch || target == null || target.IsDead)
            {
                nextSearch = Time.time + RetargetInterval;
                target = Nearest();
            }

            // Su sitio: a distancia de mordisco del objetivo, por su lado (o junto al jugador, también por su lado).
            Vector2 position = transform.position;
            Vector2 anchor = target != null ? (Vector2)target.transform.position
                : owner != null ? (Vector2)owner.transform.position : position;
            float reach = target != null ? settings.biteReach * 0.75f : FollowDistance;
            Vector2 goal = anchor + side * reach;
            Vector2 to = goal - position;
            var move = to.sqrMagnitude > 0.01f ? Direction(position, goal) : Vector2.zero;
            move += Separation(position) * SeparationWeight;
            if (move.sqrMagnitude > 0.0001f)
            {
                // Más despacio al llegar a su sitio, para no temblar encima.
                float speed = settings.speed * Mathf.Clamp01(to.magnitude / 0.4f + 0.2f);
                transform.position = position + Vector2.ClampMagnitude(move, 1f) * speed * Time.deltaTime;
                // Mira a lo que muerde; si no, hacia donde va.
                float facing = target != null ? anchor.x - position.x : move.x;
                if (body != null && Mathf.Abs(facing) > 0.05f)
                    body.flipX = facing < 0f;
            }

            if (target != null && Vector2.Distance(anchor, position) <= settings.biteReach && Time.time >= nextBite)
                Bite();

            Animate();
        }

        /// <summary>Por el camino si hay rejilla (nada: por encima de las fosas); si no, derecho.</summary>
        static Vector2 Direction(Vector2 from, Vector2 to)
        {
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            var direction = grid != null ? grid.DirectionTo(from, to, BodyRadius) : (to - from).normalized;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : (to - from).normalized;
        }

        /// <summary>Empujón suave para apartarse de las otras larvas que tiene demasiado cerca.</summary>
        Vector2 Separation(Vector2 position)
        {
            Vector2 push = Vector2.zero;
            foreach (var other in all)
            {
                if (other == this || other == null || other.dying)
                    continue;
                Vector2 away = position - (Vector2)other.transform.position;
                float distance = away.magnitude;
                if (distance >= SeparationRadius)
                    continue;
                push += distance > 0.001f ? away / distance * (1f - distance / SeparationRadius) : side;
            }
            return Vector2.ClampMagnitude(push, 1f);
        }

        Health Nearest()
        {
            Health best = null;
            float bestDistance = settings.range * settings.range;
            Vector2 position = transform.position;
            foreach (var hit in Physics2D.OverlapCircleAll(position, settings.range, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !health.CanTakeDamage)
                    continue;
                float distance = ((Vector2)health.transform.position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = health;
                }
            }
            return best;
        }

        void Bite()
        {
            nextBite = Time.time + settings.biteInterval;
            biteStart = Time.time;
            var bitten = target;
            bitten.TakeDamage(settings.biteDamage);
            Sfx.Play(Sounds.BroodSacLarvaBite);
            AnyBit?.Invoke(this, bitten);
            if (settings.splits && bitten.IsDead)
                Split();
        }

        /// <summary>Enjambre: la larva que mata se divide en dos; la nueva dura su tiempo entero.</summary>
        void Split()
        {
            OneShotEffect.Spawn(splitEffect, transform.position);
            Spawn(prefab, (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 0.3f, settings, owner);
        }

        void Animate()
        {
            if (body == null)
                return;
            float sinceBite = Time.time - biteStart;
            if (bite.Length > 0 && sinceBite < bite.Length * biteFrame)
                body.sprite = bite[Mathf.Min(bite.Length - 1, (int)(sinceBite / biteFrame))];
            else if (swim.Length > 0)
                body.sprite = swim[Mathf.FloorToInt(Time.time / swimFrame) % swim.Length];
        }

        System.Collections.IEnumerator Die()
        {
            dying = true;
            foreach (var frame in death)
            {
                if (body != null)
                    body.sprite = frame;
                yield return new WaitForSeconds(deathFrame);
            }
            Destroy(gameObject);
        }
    }
}

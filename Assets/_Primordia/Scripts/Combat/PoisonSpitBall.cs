using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// La bola del Escupitajo venenoso (<see cref="AttackData.IsSpit"/>), sobre un <see cref="Projectile"/>: rueda por el suelo
    /// atravesando a los enemigos y dejando una banda de baba continua (<see cref="SlimeTrail"/>). No encoge: mantiene su tamaño y en
    /// los últimos 0,6 s se hincha y parpadea. Estalla (daño en un radio y charco salpicado) al acabar su tiempo, al chocar con una
    /// pared o al disparar otra con la misma arma. En la Camada, el estallido suelta bolitas en cruz que dejan una banda fina.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    public class PoisonSpitBall : MonoBehaviour
    {
        // Los últimos segundos de la bola normal: sigue rodando con la hoja Swell (6 cuadros de 0,1 s).
        const float SwellSeconds = 0.6f;
        const float SwellFrameSeconds = 0.1f;

        // Yesquero: la bola arde. Cada cuánto quema lo que toca y a qué distancia.
        const float BurnEvery = 0.1f;
        const float BurnRadius = 0.6f;
        const int BurnDamage = 1;
        const float BurnSeconds = 3f;

        // De lado, la boca queda más abajo que el punto de disparo (la cabeza): la bola sale al suelo desde ahí.
        const float SideMouthDrop = 0.35f;

        // La última bola viva de cada arma: al disparar otra, esta estalla.
        static readonly Dictionary<Weapon, PoisonSpitBall> latest = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => latest.Clear();

        [Header("Dibujo (el proyectil gira hacia donde va; el dibujo no)")]
        [Tooltip("Hijo con el SpriteRenderer de la bola. El pivote de los cuadros es el punto de apoyo en el suelo.")]
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Sprite[] rollSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] rollDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] rollUp = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] swellSide = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] swellDown = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] swellUp = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float rollFrameSeconds = 0.135f;

        Projectile projectile;
        Rigidbody2D body;
        AttackData attack;
        Weapon weapon;
        SlimeTrail trail;
        float age;
        float launchedAt;
        bool leech;
        bool tinder;
        float nextBurn;
        Vector2 lastFire;
        Vector2 heading = Vector2.right;
        bool alive;

        internal void ConfigureArt(SpriteRenderer newVisual, Sprite[] side, Sprite[] down, Sprite[] up, Sprite[] swellS, Sprite[] swellD,
            Sprite[] swellU, float newRollFrameSeconds)
        {
            visual = newVisual;
            rollSide = side;
            rollDown = down;
            rollUp = up;
            swellSide = swellS;
            swellDown = swellD;
            swellUp = swellU;
            rollFrameSeconds = newRollFrameSeconds;
        }

        void Awake()
        {
            projectile = GetComponent<Projectile>();
            body = GetComponent<Rigidbody2D>();
            projectile.Launched += OnLaunched;
            projectile.Ending += OnEnding;
        }

        void OnDestroy()
        {
            if (projectile == null)
                return;
            projectile.Launched -= OnLaunched;
            projectile.Ending -= OnEnding;
        }

        void OnLaunched(Projectile launched)
        {
            attack = launched.Attack;
            alive = attack != null && attack.IsSpit;
            if (alive)
                launched.DamageOrigin = DamageSource.Spit;
            if (!alive)
                return;

            age = 0f;
            launchedAt = Time.time;
            weapon = launched.Source;
            heading = body != null && body.linearVelocity.sqrMagnitude > 0f ? body.linearVelocity.normalized : Vector2.right;
            if (!attack.SpitMini && Mathf.Abs(heading.x) >= Mathf.Abs(heading.y))
            {
                var mouth = (Vector2)transform.position + Vector2.down * SideMouthDrop;
                transform.position = mouth;
                if (body != null)
                    body.position = mouth;
            }
            var touch = IsPlayerShot ? LineageTouches.Of(weapon) : LineageTouch.None;
            tinder = touch == LineageTouch.Tinder;
            leech = touch == LineageTouch.LeechPool && ReactionKit.Current != null && ReactionKit.Current.LeechRollSide.Length > 0;
            nextBurn = 0f;
            if (tinder)
            {
                // Yesquero: sin baba; la bola va dejando fuego por el camino.
                trail = null;
                lastFire = transform.position;
                TinderScorch.Attach(GroundFire.Spawn(lastFire, attack.SpitTrailSeconds, harmsPlayer: false));
            }
            else
            {
                trail = SlimeTrail.Begin(transform.position, heading, attack.SpitMini, attack.SpitTrailEvery, attack.SpitTrailSeconds,
                    harmsPlayer: !IsPlayerShot, leech: leech);
            }
            Draw();
            if (weapon == null || attack.SpitMini)
                return;

            // Disparar otra hace estallar la anterior.
            if (latest.TryGetValue(weapon, out var previous) && previous != null && previous != this && previous.alive)
                previous.projectile.Despawn(Projectile.EndReason.Detonated);
            latest[weapon] = this;
        }

        void FixedUpdate()
        {
            if (!alive)
                return;

            age += Time.fixedDeltaTime * (IsPlayerShot ? Core.TimeWarp.PlayerSpeed : 1f);
            if (trail != null)
                trail.Extend(transform.position);
            if (tinder)
                Smolder();
        }

        // Yesquero: va ardiendo. Deja fuego cada trecho y quema a los enemigos y lo inflamable que roza.
        void Smolder()
        {
            Vector2 position = transform.position;
            if (Vector2.Distance(position, lastFire) >= attack.SpitTrailEvery)
            {
                lastFire = position;
                TinderScorch.Attach(GroundFire.Spawn(position, attack.SpitTrailSeconds, harmsPlayer: false));
            }

            if (Time.time < nextBurn)
                return;
            nextBurn = Time.time + BurnEvery;
            Flammables.IgniteNear(position, BurnRadius);
            var marker = ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null;
            foreach (var hit in Physics2D.OverlapCircleAll(position, BurnRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health != null && !health.IsDead && health.TryGetComponent<Enemy>(out _))
                    Burn.Apply(health, BurnDamage, BurnSeconds, marker);
            }
        }

        void Update()
        {
            if (alive)
                Draw();
        }

        // El proyectil gira hacia donde va; el dibujo no: mira a la derecha (side, con Flip X), arriba o abajo según el eje dominante.
        void LateUpdate()
        {
            if (visual != null)
                visual.transform.rotation = Quaternion.identity;
        }

        void Draw()
        {
            if (visual == null)
                return;

            if (body != null && body.linearVelocity.sqrMagnitude > 0.0001f)
                heading = body.linearVelocity.normalized;
            bool horizontal = Mathf.Abs(heading.x) >= Mathf.Abs(heading.y);
            visual.flipX = horizontal && heading.x < 0f;

            float swellStart = attack.Lifetime - SwellSeconds;
            bool swelling = !attack.SpitMini && age >= swellStart && SwellFor(horizontal).Length > 0;
            Sprite[] frames;
            float sinceStart;
            float frameSeconds;
            if (swelling)
            {
                frames = SwellFor(horizontal);
                sinceStart = age - swellStart;
                frameSeconds = SwellFrameSeconds;
            }
            else
            {
                frames = RollFor(horizontal);
                sinceStart = Time.time - launchedAt;
                frameSeconds = rollFrameSeconds;
            }

            if (frames.Length == 0)
                return;
            int frame = Mathf.FloorToInt(sinceStart / frameSeconds);
            visual.sprite = swelling ? frames[Mathf.Min(frame, frames.Length - 1)] : frames[frame % frames.Length];
        }

        // Sanguijuela y Yesquero: las hojas propias salen del ReactionKit; si falta alguna, la normal.
        Sprite[] RollFor(bool horizontal)
        {
            var kit = ReactionKit.Current;
            if (kit != null && (leech || tinder))
            {
                bool up = heading.y > 0f;
                Sprite[] own;
                if (leech)
                    own = attack.SpitMini
                        ? (horizontal ? kit.LeechMiniRollSide : up ? kit.LeechMiniRollUp : kit.LeechMiniRollDown)
                        : (horizontal ? kit.LeechRollSide : up ? kit.LeechRollUp : kit.LeechRollDown);
                else
                    own = attack.SpitMini
                        ? (horizontal ? kit.TinderMiniRollSide : up ? kit.TinderMiniRollUp : kit.TinderMiniRollDown)
                        : (horizontal ? kit.TinderRollSide : up ? kit.TinderRollUp : kit.TinderRollDown);
                if (own.Length > 0)
                    return own;
            }
            return horizontal ? rollSide : heading.y > 0f ? rollUp : rollDown;
        }

        Sprite[] SwellFor(bool horizontal)
        {
            var kit = ReactionKit.Current;
            if (kit != null && (leech || tinder))
            {
                bool up = heading.y > 0f;
                var own = leech
                    ? (horizontal ? kit.LeechSwellSide : up ? kit.LeechSwellUp : kit.LeechSwellDown)
                    : (horizontal ? kit.TinderSwellSide : up ? kit.TinderSwellUp : kit.TinderSwellDown);
                if (own.Length > 0)
                    return own;
            }
            return horizontal ? swellSide : heading.y > 0f ? swellUp : swellDown;
        }

        bool IsPlayerShot => gameObject.layer == CombatLayers.ProjectileLayer(Team.Player);

        void OnEnding(Projectile ending, Projectile.EndReason reason)
        {
            if (!alive)
                return;
            alive = false;
            if (weapon != null && latest.TryGetValue(weapon, out var current) && current == this)
                latest.Remove(weapon);

            if (trail != null)
            {
                if (reason == Projectile.EndReason.Cleared)
                    trail.Clear();
                else
                    trail.Finish();
                trail = null;
            }
            if (reason == Projectile.EndReason.Cleared)
                return;

            Explode(ending.transform.position);
        }

        void Explode(Vector2 position)
        {
            var victims = IsPlayerShot ? Team.Enemy : Team.Player;
            int damage = weapon != null
                ? Mathf.Max(0, Mathf.CeilToInt(attack.SpitBurstDamage * weapon.DamageMultiplier - 0.001f))
                : attack.SpitBurstDamage;
            var look = tinder ? PoisonSpitBurst.Look.Fire : leech ? PoisonSpitBurst.Look.Leech : PoisonSpitBurst.Look.Normal;
            PoisonSpitBurst.Spawn(position, damage, attack.SpitBurstRadius, attack.SpitBurstPuddleSeconds, weapon, victims, look);

            // Camada: bolitas en cruz.
            if (attack.SpitSplitCount > 0 && attack.SpitSplitAttack != null && attack.SpitSplitAttack.ProjectilePrefab != null)
            {
                float step = 360f / attack.SpitSplitCount;
                for (int i = 0; i < attack.SpitSplitCount; i++)
                {
                    var direction = Quaternion.Euler(0f, 0f, step * i) * Vector2.right;
                    ProjectilePool.Get(attack.SpitSplitAttack.ProjectilePrefab)
                        .Launch(position, direction, attack.SpitSplitAttack, gameObject.layer, weapon);
                }
            }
        }
    }
}

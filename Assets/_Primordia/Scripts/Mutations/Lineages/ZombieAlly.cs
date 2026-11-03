using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Enemigo levantado por el Zombi del Cordyceps: un enemigo confundido que iba a morir se queda con 1 de vida y durante unos segundos
    /// es un aliado. No se le puede hacer daño, deja de herir al jugador y persigue y ataca a los demás enemigos con su ataque de siempre
    /// (lo hace <see cref="Confusion"/>, que no lo toma a él por objetivo). Lleva el aspecto de zombi: tinte, hongos en el lomo y una
    /// estela de esporas. Al acabar el tiempo, o si no queda a quién atacar, cae de verdad (su muerte normal, sin cadáver): lo que
    /// cuenta la sala y suelta la biomasa es esa muerte.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class ZombieAlly : MonoBehaviour
    {
        /// <summary>Dibujos del zombi (docs/multimedia-temp/floor3-lineage-art/Zombie).</summary>
        public class Art
        {
            public Sprite[][] FungusSprout = System.Array.Empty<Sprite[]>();
            public Sprite[][] FungusIdle = System.Array.Empty<Sprite[]>();
            public Sprite[] SporeTrail = System.Array.Empty<Sprite>();
            public Sprite[] Rise = System.Array.Empty<Sprite>();
            public Sprite[] Crumble = System.Array.Empty<Sprite>();
        }

        const float RiseSeconds = 0.5f;
        const float TrailEvery = 0.15f;
        const float SproutFrameSeconds = 0.09f;
        const float IdleFrameSeconds = 0.15f;
        const float TrailFrameSeconds = 0.09f;
        const float EffectFrameSeconds = 0.065f;
        const float CrumbleFrameSeconds = 0.08f;
        // Cuánto cae del borde de arriba el pie de cada hongo.
        const float FungusDrop = 0.08f;
        // Tinte provisional (sin shader): gris verdoso que apaga el color del enemigo.
        static readonly Color Tint = new(0.80f, 0.84f, 0.76f);

        static readonly List<ZombieAlly> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        /// <summary>Los aliados que hay ahora.</summary>
        public static IReadOnlyList<ZombieAlly> All => all;

        Enemy enemy;
        Health health;
        EnemyAI ai;
        Art art;
        float seconds;
        float endTime;
        float riseEnd;
        float nextTrail;
        Vector2 lastPosition;
        bool ending;
        readonly List<Fungus> fungi = new();

        sealed class Fungus
        {
            public SpriteFramePlayer Player;
            public SpriteRenderer Renderer;
            public Sprite[] Idle;
            public float IdleAt;
            public bool Switched;
        }

        public bool IsRising => Time.time < riseEnd;

        /// <summary>Segundos que le quedan de aliado.</summary>
        public float TimeLeft => Mathf.Max(0f, endTime - Time.time);

        /// <summary>
        /// Levanta a ese enemigo como aliado durante <paramref name="allySeconds"/>. Null si no se puede: ya es un aliado, ya murió,
        /// es un jefe, no se mueve (plantas y trampas) o no tiene IA.
        /// </summary>
        public static ZombieAlly Raise(Health target, float allySeconds, Art newArt)
        {
            if (target == null || target.IsDead || !IsRaisable(target) || target.TryGetComponent<ZombieAlly>(out _))
                return null;

            var ally = target.gameObject.AddComponent<ZombieAlly>();
            ally.art = newArt ?? new Art();
            ally.seconds = Mathf.Max(0.5f, allySeconds);
            ally.Begin();
            return ally;
        }

        /// <summary>Los jefes, lo que no se mueve (plantas y trampas) y lo que no tiene IA no se levantan.</summary>
        public static bool IsRaisable(Health target) =>
            target != null && target.TryGetComponent<Enemy>(out var e) && target.TryGetComponent<EnemyAI>(out _)
            && target.GetComponentInParent<IBoss>() == null && e.Data != null && e.Data.MoveSpeed > 0f;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            health = GetComponent<Health>();
            ai = GetComponent<EnemyAI>();
        }

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        void Begin()
        {
            // Se queda con 1 de vida y es intocable mientras dura (se renueva en Update: TakeDamage pisa la del golpe que lo salvó).
            endTime = Time.time + seconds;
            riseEnd = Time.time + RiseSeconds;
            lastPosition = transform.position;

            // No hiere al jugador al tocarlo: solo a los demás enemigos (lo hace Confusion con su daño de contacto).
            if (TryGetComponent<ContactDamage>(out var contact))
                contact.enabled = false;
            Confusion.Apply(health, seconds + 0.5f, false, null, null, null);

            // Los proyectiles del jugador lo atraviesan (si no, se perderían contra un aliado al que no se puede dañar).
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.excludeLayers |= LayerMask.GetMask(CombatLayers.PlayerProjectile);

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                renderer.color = Tint;

            SpriteFramePlayer.PlayOnce(art.Rise, EffectFrameSeconds, FeetPosition(), BodyOrder() + 1);
            AddFungi();
        }

        void Update()
        {
            if (ending)
                return;
            if (health == null || health.IsDead)
                return;

            health.MakeInvulnerable(0.3f);
            float now = Time.time;
            if (now >= endTime || (now > riseEnd && !AnyoneToFight()))
            {
                Fall();
                return;
            }

            // Mientras se levanta no ataca: la IA espera a que acabe.
            if (ai != null)
                ai.enabled = now >= riseEnd;

            foreach (var fungus in fungi)
            {
                if (!fungus.Switched && now >= fungus.IdleAt && fungus.Renderer != null)
                {
                    fungus.Switched = true;
                    fungus.Player.Play(fungus.Renderer, fungus.Idle, IdleFrameSeconds, newLoop: true);
                }
            }

            // Estela de esporas a sus pies mientras se mueve.
            Vector2 position = transform.position;
            if (now >= nextTrail && (position - lastPosition).sqrMagnitude > 0.0004f)
            {
                nextTrail = now + TrailEvery;
                SpriteFramePlayer.PlayOnce(art.SporeTrail, TrailFrameSeconds, FeetPosition(), BodyOrder() - 1);
            }
            lastPosition = position;
        }

        // Queda algún enemigo (no aliado) al que atacar.
        static bool AnyoneToFight()
        {
            foreach (var other in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (other.Health != null && !other.Health.IsDead && !other.TryGetComponent<ZombieAlly>(out _))
                    return true;
            }
            return false;
        }

        /// <summary>Cae de verdad: su muerte normal, sin cadáver, con el soplo de esporas pálidas.</summary>
        void Fall()
        {
            ending = true;
            SpriteFramePlayer.PlayOnce(art.Crumble, CrumbleFrameSeconds, FeetPosition(), BodyOrder() + 1);
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                renderer.enabled = false;
            if (enemy != null)
                enemy.SetAnimationTimings(0f, 0f);
            if (health != null)
                health.Kill();
        }

        void AddFungi()
        {
            if (art.FungusSprout.Length == 0 || art.FungusSprout.Length != art.FungusIdle.Length)
                return;

            var collider = GetComponentInChildren<Collider2D>();
            Bounds bounds = collider != null ? collider.bounds : new Bounds(transform.position, Vector3.one * 0.8f);
            int count = Random.Range(2, 4);
            var used = new HashSet<int>();
            for (int i = 0; i < count && used.Count < art.FungusSprout.Length; i++)
            {
                // Cada hongo es de una variante distinta, repartidos por la parte de arriba del cuerpo.
                int variant;
                do
                {
                    variant = Random.Range(0, art.FungusSprout.Length);
                } while (!used.Add(variant));

                var obj = new GameObject($"ZombieFungus_{variant}");
                obj.transform.SetParent(transform, true);
                float x = Mathf.Lerp(bounds.min.x + bounds.size.x * 0.2f, bounds.max.x - bounds.size.x * 0.2f, count == 1 ? 0.5f : i / (float)(count - 1));
                obj.transform.position = new Vector3(x, bounds.max.y - FungusDrop, transform.position.z);
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = BodyOrder() + 1;
                var player = obj.AddComponent<SpriteFramePlayer>();
                player.Play(renderer, art.FungusSprout[variant], SproutFrameSeconds, newLoop: false);
                fungi.Add(new Fungus
                {
                    Player = player,
                    Renderer = renderer,
                    Idle = art.FungusIdle[variant],
                    IdleAt = Time.time + art.FungusSprout[variant].Length * SproutFrameSeconds,
                });
            }
        }

        Vector2 FeetPosition()
        {
            var collider = GetComponentInChildren<Collider2D>();
            return collider != null ? new Vector2(collider.bounds.center.x, collider.bounds.min.y) : (Vector2)transform.position;
        }

        int BodyOrder()
        {
            var renderer = GetComponentInChildren<SpriteRenderer>();
            return renderer != null ? renderer.sortingOrder : 10;
        }

        void OnDestroy()
        {
            foreach (var fungus in fungi)
            {
                if (fungus.Renderer != null)
                    Destroy(fungus.Renderer.gameObject);
            }
        }
    }
}

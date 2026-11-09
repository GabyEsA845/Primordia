using System.Collections;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Charco de veneno del escenario (piso 3, el que deja el bulbo al reventar): cae, burbujea ~12 s y se seca. Quien lo pisa va
    /// un 30 % más lento; a los enemigos los envenena (1 por segundo, 3 s) y el jugador recibe 1 de daño al empezar cada ventana
    /// de 3 s (<see cref="PlayerToxin.TouchPuddle"/>). La esquiva pasa por encima. Arde: el fuego se come el charco y sale una
    /// nube tóxica (<see cref="ToxicCloud"/>).
    /// </summary>
    public class PoisonPuddle : MonoBehaviour, IFlammable
    {
        public const float DefaultSeconds = 12f;

        // Lo que cubre el charco (el dibujo grande mide ~1,2 u de radio) y cada cuánto se comprueba quién lo pisa.
        const float Radius = 1f;
        // Radio del cuerpo del charco salpicado del Escupitajo (14 px de 64): se escala para que coincida con el radio que daña.
        const float SplatBodyRadius = 14f / 16f;
        public const float CheckEvery = 0.1f;
        const int EnemyDamagePerSecond = 1;
        const float EnemyPoisonSeconds = 3f;
        const float AppearFrameSeconds = 0.09f;
        const float IgniteFrameSeconds = 0.08f;
        // Cuadro del fuego en el que sale la nube tóxica.
        const int CloudFrame = 3;

        enum State
        {
            Idle,
            Igniting,
            Drying
        }

        static readonly List<PoisonPuddle> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            PlayerBonus = 0;
            EnemyAffected = null;
        }

        /// <summary>Daño por segundo de más de los charcos y la baba que deja el jugador (la Sanguijuela: sangrientos).</summary>
        public static int PlayerBonus { get; set; }

        /// <summary>Un enemigo ha pisado un charco o una banda del jugador (no los del escenario); se lanza cada comprobación.</summary>
        public static event System.Action<Health> EnemyAffected;

        /// <summary>Los charcos que hay ahora.</summary>
        public static IReadOnlyList<PoisonPuddle> All => all;

        SpriteRenderer body;
        State state;
        bool harmsPlayer = true;
        bool useSplat;
        bool leech;
        /// <summary>Charco del Escupitajo: envenena con veneno virulento (más rápido y fuerte) en vez del normal.</summary>
        bool venom;
        float radius = Radius;
        float endTime;
        float nextCheck;

        public Vector2 Position => transform.position;

        // La sangre no arde: el charco de la Sanguijuela no es inflamable.
        public bool CanIgnite => state == State.Idle && !leech;

        /// <summary>True si ese punto está dentro de algún charco de veneno que no se esté quemando ni secando.</summary>
        public static bool IsOver(Vector2 point)
        {
            foreach (var puddle in all)
            {
                if (puddle.state == State.Idle && (point - puddle.Position).sqrMagnitude <= puddle.radius * puddle.radius)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Hace un charco en ese punto (el centro del dibujo). Sin <paramref name="harmsPlayer"/>, el jugador lo pisa sin frenarse ni
        /// envenenarse (los charcos que deja él mismo). Null si no hay arte del veneno.
        /// </summary>
        public static PoisonPuddle Spawn(Vector2 position, float seconds = DefaultSeconds, bool harmsPlayer = true)
        {
            var kit = ReactionKit.Current;
            if (kit == null || kit.PuddleIdle.Length == 0)
                return null;
            return Create(kit, position, seconds, Radius, harmsPlayer, splat: false);
        }

        /// <summary>
        /// El charco salpicado que deja el reventón del Escupitajo: su cuerpo coincide con <paramref name="effectRadius"/>, el radio
        /// que frena y envenena. Sin dibujo propio, el charco grande reducido. Null si no hay arte del veneno.
        /// </summary>
        public static PoisonPuddle SpawnSplat(Vector2 position, float seconds, float effectRadius, bool harmsPlayer = false, bool leech = false,
            bool venom = false)
        {
            var kit = ReactionKit.Current;
            if (kit == null)
                return null;
            bool splat = kit.SplatIdle.Length > 0;
            if (!splat && kit.PuddleIdle.Length == 0)
                return null;
            // Sanguijuela: charco rojizo con gusanillos (si hay su arte).
            var puddle = Create(kit, position, seconds, effectRadius, harmsPlayer, splat, leech && splat && kit.HasLeechSplat);
            puddle.venom = venom;
            return puddle;
        }

        static PoisonPuddle Create(ReactionKit kit, Vector2 position, float seconds, float effectRadius, bool harmsPlayer, bool splat,
            bool leech = false)
        {
            var obj = new GameObject("PoisonPuddle");
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            // Pegado al suelo, por encima de la niebla pero por debajo de todo lo que se mueve.
            renderer.sortingOrder = 3;
            var puddle = obj.AddComponent<PoisonPuddle>();
            puddle.body = renderer;
            puddle.harmsPlayer = harmsPlayer;
            puddle.radius = effectRadius;
            puddle.useSplat = splat;
            puddle.leech = leech;
            obj.transform.localScale = Vector3.one * (splat ? effectRadius / SplatBodyRadius : effectRadius / Radius);
            puddle.endTime = Time.time + seconds;
            puddle.StartCoroutine(puddle.Run(kit));
            return puddle;
        }

        void OnEnable()
        {
            all.Add(this);
            Flammables.Register(this);
        }

        void OnDisable()
        {
            all.Remove(this);
            Flammables.Unregister(this);
        }

        IEnumerator Run(ReactionKit kit)
        {
            yield return Play(!useSplat ? kit.PuddleAppear : leech ? kit.LeechSplatAppear : kit.SplatAppear, AppearFrameSeconds);
            var player = gameObject.AddComponent<SpriteFramePlayer>();
            player.Play(body, !useSplat ? kit.PuddleIdle : leech ? kit.LeechSplatIdle : kit.SplatIdle, 0.09f, newLoop: true);
            while (Time.time < endTime && state == State.Idle)
                yield return null;
            if (state != State.Idle)
                yield break;

            // Se seca: el último cuadro de la animación y fuera.
            state = State.Drying;
            Destroy(player);
            yield return Play(!useSplat ? kit.PuddleFade : leech ? kit.LeechSplatFade : kit.SplatFade, AppearFrameSeconds);
            Destroy(gameObject);
        }

        IEnumerator Play(Sprite[] frames, float seconds)
        {
            foreach (var frame in frames)
            {
                if (body != null)
                    body.sprite = frame;
                yield return new WaitForSeconds(seconds);
            }
        }

        public void Ignite()
        {
            if (!CanIgnite)
                return;
            state = State.Igniting;
            Flammables.Unregister(this);
            StopAllCoroutines();
            var player = GetComponent<SpriteFramePlayer>();
            if (player != null)
                Destroy(player);
            // El fuego es del tamaño del charco grande: en uno más pequeño se reduce.
            transform.localScale = Vector3.one * (radius / Radius);
            StartCoroutine(Burn());
        }

        // El fuego corre por el charco; en el cuadro 3 sale la nube tóxica y el charco se gasta.
        IEnumerator Burn()
        {
            var kit = ReactionKit.Current;
            var frames = kit != null ? kit.PuddleIgnite : System.Array.Empty<Sprite>();
            for (int i = 0; i < frames.Length; i++)
            {
                body.sprite = frames[i];
                if (i == CloudFrame)
                    ToxicCloud.Spawn(transform.position);
                yield return new WaitForSeconds(IgniteFrameSeconds);
            }
            if (frames.Length <= CloudFrame)
                ToxicCloud.Spawn(transform.position);
            Destroy(gameObject);
        }

        void Update()
        {
            if (state != State.Idle || Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;

            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius))
                Affect(hit, harmsPlayer, seen, venom);
        }

        /// <summary>
        /// Lo que hace un charco (o un tramo de la banda de baba) a lo que toca, cada <see cref="CheckEvery"/>: el jugador recibe su
        /// ventana de veneno (si <paramref name="harmsPlayer"/> y no esquiva); a los enemigos los frena y los envenena.
        /// <paramref name="seen"/> evita contar dos veces al mismo en una comprobación.
        /// </summary>
        public static void Affect(Collider2D hit, bool harmsPlayer, HashSet<Health> seen, bool venom = false)
        {
            if (hit.isTrigger)
                return;

            if (hit.GetComponentInParent<PlayerToxin>() is { } player)
            {
                if (harmsPlayer && !player.IsDodging)
                    player.TouchPuddle();
                return;
            }

            var health = hit.GetComponentInParent<Health>();
            if (health == null || health.IsDead || !seen.Add(health)
                || (health.GetComponentInParent<Enemy>() == null && health.GetComponentInParent<MeganeuraEgg>() == null))
                return;
            Toxins.SlowEnemy(health, CheckEvery * 3f);
            // Los del jugador (sin daño al jugador) hacen más daño con la Sanguijuela.
            if (venom)
                Toxins.VenomEnemy(health);
            else
                Toxins.PoisonEnemy(health, EnemyDamagePerSecond + (harmsPlayer ? 0 : PlayerBonus), EnemyPoisonSeconds);
            if (!harmsPlayer)
                EnemyAffected?.Invoke(health);
        }
    }
}

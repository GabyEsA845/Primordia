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
        const float SmallRadius = 0.45f;
        const float SmallScale = 0.4f;
        const float CheckEvery = 0.1f;
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
        static void ResetStatics() => all.Clear();

        /// <summary>Los charcos que hay ahora.</summary>
        public static IReadOnlyList<PoisonPuddle> All => all;

        SpriteRenderer body;
        State state;
        bool harmsPlayer = true;
        bool useSmallArt;
        float radius = Radius;
        float endTime;
        float nextCheck;

        public Vector2 Position => transform.position;

        public bool CanIgnite => state == State.Idle;

        /// <summary>
        /// Hace un charco en ese punto (el centro del dibujo). Pequeño: el del rastro del Escupitajo venenoso. Sin <paramref name="harmsPlayer"/>,
        /// el jugador lo pisa sin frenarse ni envenenarse (los charcos que deja él mismo). Null si no hay arte del veneno.
        /// </summary>
        public static PoisonPuddle Spawn(Vector2 position, float seconds = DefaultSeconds, bool small = false, bool harmsPlayer = true)
        {
            var kit = ReactionKit.Current;
            if (kit == null || kit.PuddleIdle.Length == 0)
                return null;

            var obj = new GameObject("PoisonPuddle");
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            // Pegado al suelo, por encima de la niebla pero por debajo de todo lo que se mueve.
            renderer.sortingOrder = 3;
            var puddle = obj.AddComponent<PoisonPuddle>();
            puddle.body = renderer;
            puddle.harmsPlayer = harmsPlayer;
            puddle.radius = small ? SmallRadius : Radius;
            // Sin dibujo pequeño, el grande reducido.
            puddle.useSmallArt = small && kit.SmallPuddleIdle.Length > 0;
            if (small && !puddle.useSmallArt)
                obj.transform.localScale = Vector3.one * SmallScale;
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
            yield return Play(useSmallArt ? kit.SmallPuddleAppear : kit.PuddleAppear, AppearFrameSeconds);
            var player = gameObject.AddComponent<SpriteFramePlayer>();
            player.Play(body, useSmallArt ? kit.SmallPuddleIdle : kit.PuddleIdle, 0.09f, newLoop: true);
            while (Time.time < endTime && state == State.Idle)
                yield return null;
            if (state != State.Idle)
                yield break;

            // Se seca: el último cuadro de la animación y fuera.
            state = State.Drying;
            Destroy(player);
            yield return Play(useSmallArt ? kit.SmallPuddleFade : kit.PuddleFade, AppearFrameSeconds);
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
            // El fuego es del tamaño del charco grande: en uno pequeño se reduce.
            if (useSmallArt)
                transform.localScale = Vector3.one * SmallScale;
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
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerToxin>() is { } player)
                {
                    if (harmsPlayer && !player.IsDodging)
                        player.TouchPuddle();
                    continue;
                }

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !seen.Add(health) || health.GetComponentInParent<Enemy>() == null)
                    continue;
                Toxins.SlowEnemy(health, CheckEvery * 3f);
                Toxins.PoisonEnemy(health, EnemyDamagePerSecond, EnemyPoisonSeconds);
            }
        }
    }
}

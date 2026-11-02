using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Fuego en el suelo (piso 3): arde unos segundos, quema a quien lo pisa (al jugador le llena la barra de
    /// <see cref="PlayerBurn"/>; a los enemigos los prende con <see cref="Burn"/>) y salta a lo inflamable que tenga cerca
    /// (helechos, maleza). La esquiva pasa por encima sin quemarse. Lo crean los helechos que arden, las explosiones y los
    /// ataques de fuego con <see cref="Spawn"/>; al acabar, se apaga con su animación.
    /// </summary>
    public class GroundFire : MonoBehaviour
    {
        public const float DefaultSeconds = 4f;

        // Radio de lo que quema, cada cuánto salta a lo inflamable y hasta dónde llega (2 tiles).
        const float BurnRadius = 0.55f;
        const float SpreadEvery = 0.6f;
        const float SpreadRadius = 2f;
        const float CheckEvery = 0.1f;
        // Daño a los enemigos: ½ corazón por segundo mientras ardan (3 s tras salir del fuego).
        const int EnemyDamagePerSecond = 1;
        const float EnemyBurnSeconds = 3f;

        static readonly List<GroundFire> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        /// <summary>Los fuegos que arden ahora.</summary>
        public static IReadOnlyList<GroundFire> All => all;

        float endTime;
        float nextSpread;
        float nextCheck;
        bool dying;
        SpriteRenderer body;

        /// <summary>
        /// Enciende un fuego en esa posición (el pie de las llamas). Si ya hay otro casi en el mismo sitio, lo alarga en vez de
        /// poner otro encima. Null si no hay arte del fuego (<see cref="ReactionKit"/>).
        /// </summary>
        public static GroundFire Spawn(Vector2 position, float seconds = DefaultSeconds)
        {
            var kit = ReactionKit.Current;
            if (kit == null || kit.GroundFire.Length == 0)
                return null;

            foreach (var fire in all)
            {
                if (!fire.dying && (fire.transform.position - (Vector3)position).sqrMagnitude < 0.25f)
                {
                    fire.endTime = Mathf.Max(fire.endTime, Time.time + seconds);
                    return fire;
                }
            }

            var obj = new GameObject("GroundFire");
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 6;
            var fireComponent = obj.AddComponent<GroundFire>();
            fireComponent.body = renderer;
            fireComponent.endTime = Time.time + seconds;
            fireComponent.nextSpread = Time.time + SpreadEvery;
            obj.AddComponent<SpriteFramePlayer>().Play(renderer, kit.GroundFire, kit.GroundFireFrameSeconds, newLoop: true);
            return fireComponent;
        }

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (dying)
                return;

            if (Time.time >= endTime)
            {
                StartCoroutine(GoOut());
                return;
            }

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + CheckEvery;
                BurnWhoStands(CheckEvery);
            }

            if (Time.time >= nextSpread)
            {
                nextSpread = Time.time + SpreadEvery;
                Flammables.IgniteNear(transform.position, SpreadRadius);
            }
        }

        void BurnWhoStands(float seconds)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, BurnRadius);
            var burned = new HashSet<Health>();
            foreach (var hit in hits)
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerBurn>() is { } player)
                {
                    // La esquiva pasa por encima del fuego.
                    if (!player.IsDodging)
                        player.Feed(seconds);
                    continue;
                }

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !burned.Add(health) || health.GetComponentInParent<Enemy>() == null)
                    continue;
                Burn.Apply(health, EnemyDamagePerSecond, EnemyBurnSeconds, ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null);
            }
        }

        System.Collections.IEnumerator GoOut()
        {
            dying = true;
            var kit = ReactionKit.Current;
            if (kit != null && kit.FireOut.Length > 0 && body != null && TryGetComponent<SpriteFramePlayer>(out var player))
            {
                player.Play(body, kit.FireOut, kit.FireOutFrameSeconds, newLoop: false);
                // SpriteFramePlayer se destruye al acabar una pasada sin bucle.
                yield break;
            }
            Destroy(gameObject);
        }
    }
}

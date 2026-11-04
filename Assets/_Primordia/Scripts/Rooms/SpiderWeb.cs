using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Telaraña de la Araña de las frondas (piso 3): pegada al suelo, frena a quien la pisa (jugador y enemigos, menos las
    /// arañas; la esquiva pasa por encima) y arde: al prenderla (el cono del escarabajo, el fuego del suelo, un helecho que arde
    /// al lado) se consume y deja un fuego corto. Desaparece sola pasado un rato.
    /// </summary>
    public class SpiderWeb : MonoBehaviour, IFlammable
    {
        /// <summary>La telaraña deja al 50 % de la velocidad.</summary>
        public const float WebSpeed = 0.5f;

        const float CheckEvery = 0.1f;
        const float SlowHold = 0.25f;
        const float Radius = 0.8f;
        const float AppearFrameSeconds = 0.06f;
        const float BurnFrameSeconds = 0.1f;
        const float FireSeconds = 2f;

        enum State { Appear, Idle, Burning }

        static readonly List<SpiderWeb> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public static IReadOnlyList<SpiderWeb> All => all;

        SpriteRenderer body;
        Sprite[] appear;
        Sprite[] idle;
        Sprite[] burn;
        State state;
        float stateStart;
        float endTime;
        float nextCheck;

        public Vector2 Position => transform.position;

        public bool CanIgnite => state != State.Burning;

        public bool IsBurning => state == State.Burning;

        /// <summary>True si ese punto está dentro de alguna telaraña.</summary>
        public static bool IsOver(Vector2 point)
        {
            foreach (var web in all)
            {
                if (((Vector2)web.transform.position - point).sqrMagnitude <= Radius * Radius)
                    return true;
            }
            return false;
        }

        /// <summary>Echa una telaraña en ese punto, que dura <paramref name="seconds"/> (sin dibujo, solo con su efecto).</summary>
        public static SpiderWeb Create(Vector2 position, Sprite[] appearFrames, Sprite[] idleFrames, Sprite[] burnFrames, float seconds)
        {
            var obj = new GameObject("SpiderWeb");
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            // Pegada al suelo, por encima de los charcos.
            renderer.sortingOrder = -4;
            var web = obj.AddComponent<SpiderWeb>();
            web.body = renderer;
            web.appear = appearFrames ?? System.Array.Empty<Sprite>();
            web.idle = idleFrames ?? System.Array.Empty<Sprite>();
            web.burn = burnFrames ?? System.Array.Empty<Sprite>();
            web.endTime = Time.time + seconds;
            web.state = web.appear.Length > 0 ? State.Appear : State.Idle;
            web.stateStart = Time.time;
            web.Show();
            return web;
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

        public void Ignite()
        {
            if (!CanIgnite)
                return;
            state = State.Burning;
            stateStart = Time.time;
            Show();
        }

        void Update()
        {
            float elapsed = Time.time - stateStart;
            switch (state)
            {
                case State.Appear when elapsed >= appear.Length * AppearFrameSeconds:
                    state = State.Idle;
                    stateStart = Time.time;
                    break;
                case State.Burning when elapsed >= Mathf.Max(burn.Length, 1) * BurnFrameSeconds:
                    // Consumida: deja un fuego corto donde estaba.
                    GroundFire.Spawn(transform.position, FireSeconds);
                    Destroy(gameObject);
                    return;
                case State.Idle when Time.time >= endTime:
                    Destroy(gameObject);
                    return;
            }
            Show();

            if (state != State.Burning && Time.time >= nextCheck)
            {
                nextCheck = Time.time + CheckEvery;
                SlowWhoStands();
            }
        }

        void Show()
        {
            if (body == null)
                return;
            Sprite[] frames = state switch
            {
                State.Appear => appear,
                State.Burning => burn,
                _ => idle,
            };
            if (frames.Length == 0)
                return;
            float seconds = state == State.Burning ? BurnFrameSeconds : AppearFrameSeconds;
            int frame = state == State.Idle ? 0 : Mathf.Clamp(Mathf.FloorToInt((Time.time - stateStart) / seconds), 0, frames.Length - 1);
            body.sprite = frames[frame];
        }

        void SlowWhoStands()
        {
            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, Radius))
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerController>() is { } player)
                {
                    if (!player.IsDodging && !player.IsLunging)
                        player.Slow(WebSpeed, SlowHold);
                    continue;
                }

                // Las arañas andan por sus telarañas sin frenarse.
                var health = hit.GetComponentInParent<Health>();
                if (health != null && !health.IsDead && seen.Add(health) && !health.TryGetComponent<SpiderBrain>(out _)
                    && health.TryGetComponent<EnemyAI>(out var ai))
                    ai.Slow(WebSpeed, SlowHold);
            }
        }
    }
}

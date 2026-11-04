using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Planta carnívora (trampa del piso 3): no es un enemigo (no cuenta para limpiar la sala, no se la puede matar) y se marchita
    /// al limpiarla. Espera con la boca abierta; lo que se le acerca (el jugador o cualquier enemigo que no sea un jefe ni esté a
    /// salvo) recibe un mordisco que lo atrapa 1 s: al jugador lo deja sin poder moverse ni esquivar y le hace daño al soltarlo; a un
    /// enemigo se lo come (muere). Después vuelve a abrirse y descansa antes de poder morder otra vez.
    /// </summary>
    public class CarnivorousPlant : MonoBehaviour
    {
        public enum State { Idle, Snap, Chew, Open, Wilted }

        [Header("Mordisco")]
        [Tooltip("Radio desde la boca en el que muerde.")]
        [SerializeField, Min(0.1f)] float biteRadius = 1.3f;
        [Tooltip("Dónde está la boca respecto a sus pies.")]
        [SerializeField] Vector2 mouthOffset = new(0.5f, 0.7f);
        [Tooltip("Cuadro de Snap en el que atrapa (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int catchFrame = 2;
        [SerializeField, Min(0.1f)] float holdSeconds = 1f;
        [SerializeField, Min(0)] int playerDamage = 1;
        [Tooltip("Segundos de descanso tras volver a abrirse.")]
        [SerializeField, Min(0f)] float restSeconds = 1.5f;

        [Header("Dibujo (cuadros de 40×40, pivote al pie)")]
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] snap = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] chew = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] open = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] wilt = System.Array.Empty<Sprite>();

        const float IdleFrameSeconds = 0.18f;
        const float SnapFrameSeconds = 0.05f;
        const float ChewFrameSeconds = 0.13f;
        const float OpenFrameSeconds = 0.11f;
        const float WiltFrameSeconds = 0.16f;
        const float ScanEvery = 0.1f;

        public State Current { get; private set; } = State.Idle;

        /// <summary>Se lanza al atrapar a alguien (null si ya no existe al medir): el jugador o un enemigo.</summary>
        public event System.Action<GameObject> Caught;

        /// <summary>Se lanza al comerse a un enemigo.</summary>
        public event System.Action<Enemy> Ate;

        /// <summary>Se lanza al soltar al jugador, con lo que le hizo (daño causado).</summary>
        public event System.Action<Health> Released;

        SpriteRenderer body;
        Room room;
        float stateStart;
        float nextScan;
        float restUntil;
        Health victim;
        PlayerController heldPlayer;
        bool victimIsPlayer;
        Enemy eatenEnemy;
        bool caughtDone;

        public Vector2 Mouth => (Vector2)transform.position + mouthOffset;

        public void Configure(Sprite[] idleFrames, Sprite[] snapFrames, Sprite[] chewFrames, Sprite[] openFrames, Sprite[] wiltFrames)
        {
            idle = idleFrames;
            snap = snapFrames;
            chew = chewFrames;
            open = openFrames;
            wilt = wiltFrames;
        }

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (body == null)
                body = gameObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = 5;
        }

        void Start()
        {
            // Se marchita al limpiarse su sala.
            room = GetComponentInParent<Room>();
            if (room != null)
                room.Cleared += OnRoomCleared;
            nextScan = Time.time + Random.Range(0f, ScanEvery);
            Show();
        }

        void OnDestroy()
        {
            if (room != null)
                room.Cleared -= OnRoomCleared;
            Release();
        }

        void OnDisable() => Release();

        void OnRoomCleared(Room _) => Wither();

        /// <summary>Se marchita: suelta lo que tenga y no vuelve a morder.</summary>
        public void Wither()
        {
            if (Current == State.Wilted)
                return;
            Release();
            Enter(State.Wilted);
        }

        void Enter(State state)
        {
            Current = state;
            stateStart = Time.time;
            Show();
        }

        void Update()
        {
            float elapsed = Time.time - stateStart;
            switch (Current)
            {
                case State.Idle:
                    if (Time.time >= nextScan && Time.time >= restUntil)
                    {
                        nextScan = Time.time + ScanEvery;
                        var found = FindVictim();
                        if (found != null)
                        {
                            victim = found;
                            caughtDone = false;
                            Enter(State.Snap);
                        }
                    }
                    break;

                case State.Snap:
                    if (!caughtDone && elapsed >= SnapFrameSeconds * catchFrame)
                    {
                        caughtDone = true;
                        if (!TryCatch())
                        {
                            // Falló: vuelve a abrirse con las fauces vacías.
                            victim = null;
                            Enter(State.Open);
                        }
                    }
                    break;

                case State.Chew:
                    // Si lo atrapado desaparece o muere por otro lado, lo suelta.
                    if (victim == null || victim.IsDead)
                    {
                        Release();
                        Enter(State.Open);
                    }
                    else if (elapsed >= holdSeconds)
                    {
                        Finish();
                        Enter(State.Open);
                    }
                    break;

                case State.Open:
                    if (elapsed >= open.Length * OpenFrameSeconds)
                    {
                        restUntil = Time.time + restSeconds;
                        Enter(State.Idle);
                    }
                    break;
            }
            Show();
        }

        // El jugador (si puede ser mordido) o, si no, el enemigo comestible más cercano dentro del radio.
        Health FindVictim()
        {
            Health best = null;
            float bestDistance = float.MaxValue;
            foreach (var hit in Physics2D.OverlapCircleAll(Mouth, biteRadius))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;

                bool isPlayer = hit.GetComponentInParent<PlayerController>() != null || hit.gameObject.layer == LayerMask.NameToLayer(CombatLayers.Player);
                if (isPlayer)
                {
                    // La esquiva pasa por encima de la boca.
                    if (hit.GetComponentInParent<PlayerController>() is { IsDodging: true })
                        continue;
                    return health;
                }
                if (!IsEdible(health))
                    continue;
                float distance = Vector2.Distance(Mouth, hit.bounds.center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = health;
                }
            }
            return best;
        }

        // Comestible: un enemigo normal. Ni jefes, ni lo que está a salvo (escondido, en el aire), ni otras trampas.
        static bool IsEdible(Health health) =>
            health.TryGetComponent<Enemy>(out _) && !health.TryGetComponent<IBoss>(out _) && !health.IsInvulnerable;

        // Al cerrarse las fauces: si la víctima sigue dentro, la atrapa.
        bool TryCatch()
        {
            if (victim == null || victim.IsDead)
                return false;
            var collider = victim.GetComponentInChildren<Collider2D>();
            Vector2 at = collider != null ? (Vector2)collider.bounds.center : (Vector2)victim.transform.position;
            if (Vector2.Distance(Mouth, at) > biteRadius * 1.25f)
                return false;

            victimIsPlayer = victim.gameObject.layer == LayerMask.NameToLayer(CombatLayers.Player) || victim.TryGetComponent<PlayerController>(out _);
            if (victimIsPlayer)
            {
                if (victim.TryGetComponent<PlayerController>(out var player))
                {
                    if (player.IsDodging)
                        return false;
                    heldPlayer = player;
                    player.MovementLocked = true;
                }
            }
            else if (victim.TryGetComponent<Enemy>(out var enemy))
            {
                // Se lo come: ya no se ve ni choca, y no actúa.
                eatenEnemy = enemy;
                foreach (var renderer in enemy.GetComponentsInChildren<SpriteRenderer>())
                    renderer.enabled = false;
                if (collider != null)
                    collider.enabled = false;
                if (enemy.TryGetComponent<EnemyAI>(out var ai))
                    ai.Knockback(Vector2.zero, holdSeconds + 0.5f);
            }
            Caught?.Invoke(victim.gameObject);
            Enter(State.Chew);
            return true;
        }

        // Pasado el segundo: al jugador lo suelta herido; al enemigo se lo come del todo.
        void Finish()
        {
            var finished = victim;
            if (victimIsPlayer)
            {
                if (heldPlayer != null)
                    heldPlayer.MovementLocked = false;
                heldPlayer = null;
                victimIsPlayer = false;
                if (finished != null && !finished.IsDead)
                    finished.TakeDamage(playerDamage);
                Released?.Invoke(finished);
            }
            else if (eatenEnemy != null)
            {
                var eaten = eatenEnemy;
                eatenEnemy = null;
                if (finished != null && !finished.IsDead)
                    finished.Kill();
                Ate?.Invoke(eaten);
            }
            victim = null;
        }

        // Suelta sin hacer daño (la sala se limpió, la planta desapareció…).
        void Release()
        {
            if (heldPlayer != null)
            {
                heldPlayer.MovementLocked = false;
                heldPlayer = null;
            }
            victimIsPlayer = false;
            if (eatenEnemy != null)
            {
                // Lo comido no puede quedar invisible e inmortal: se le deja morir.
                var eaten = eatenEnemy;
                eatenEnemy = null;
                if (eaten != null && eaten.TryGetComponent<Health>(out var health) && !health.IsDead)
                    health.Kill();
            }
            victim = null;
        }

        void Show()
        {
            if (body == null)
                return;
            Sprite[] frames;
            float seconds;
            bool loop = false;
            switch (Current)
            {
                case State.Snap:
                    frames = snap;
                    seconds = SnapFrameSeconds;
                    break;
                case State.Chew:
                    frames = chew;
                    seconds = ChewFrameSeconds;
                    loop = true;
                    break;
                case State.Open:
                    frames = open;
                    seconds = OpenFrameSeconds;
                    break;
                case State.Wilted:
                    frames = wilt;
                    seconds = WiltFrameSeconds;
                    break;
                default:
                    frames = idle;
                    seconds = IdleFrameSeconds;
                    loop = true;
                    break;
            }
            if (frames == null || frames.Length == 0)
                return;
            int frame = Mathf.FloorToInt((Time.time - stateStart) / seconds);
            frame = loop ? frame % frames.Length : Mathf.Clamp(frame, 0, frames.Length - 1);
            body.sprite = frames[frame];
        }
    }
}

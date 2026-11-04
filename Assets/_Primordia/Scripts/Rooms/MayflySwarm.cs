using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Enjambre de efímeras (trampa del piso 3): nube de una pieza que flota hacia el jugador, despacio y por encima de todo (las
    /// fosas y el lodo no la paran; las paredes sí), y le hace daño mientras la atraviesa. No es un enemigo (no cuenta para limpiar la
    /// sala) y no tiene cuerpo: los proyectiles y los golpes la atraviesan. Solo muere con fuego o explosiones: prende (es
    /// inflamable) y cae como ascuas, y no vuelve. Al limpiar la sala se disipa.
    /// </summary>
    public class MayflySwarm : MonoBehaviour, IFlammable
    {
        [SerializeField, Min(0.1f)] float speed = 1.4f;
        [Tooltip("A qué distancia empieza a ir hacia el jugador.")]
        [SerializeField, Min(1f)] float followRange = 12f;
        [Tooltip("Radio de la nube dentro del que hiere.")]
        [SerializeField, Min(0.1f)] float damageRadius = 0.9f;
        [SerializeField, Min(0)] int damage = 1;
        [Tooltip("Segundos entre una picadura y la siguiente a quien siga dentro.")]
        [SerializeField, Min(0.1f)] float damageEvery = 1f;
        [SerializeField, Min(0f)] float fadeSeconds = 0.6f;
        [SerializeField, Min(0f)] float emberSeconds = 1.2f;

        [Header("Dibujo (cuadros de 40×40, pivote al centro)")]
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] burn = System.Array.Empty<Sprite>();

        const float IdleFrameSeconds = 0.18f;
        const float BurnFrameSeconds = 0.08f;
        const float ProbeRadius = 0.3f;

        enum State { Swarm, Burning, Fading }

        SpriteRenderer body;
        Room room;
        Transform player;
        State state;
        float stateStart;
        float nextDamageAt;
        Vector2 wanderDirection;
        float nextWanderAt;

        public Vector2 Position => transform.position;

        public bool CanIgnite => state == State.Swarm;

        public bool IsBurning => state == State.Burning;

        public void Configure(Sprite[] idleFrames, Sprite[] burnFrames)
        {
            idle = idleFrames;
            burn = burnFrames;
        }

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (body == null)
                body = gameObject.AddComponent<SpriteRenderer>();
            // Por encima de todo lo que anda.
            body.sortingOrder = 15;
        }

        void Start()
        {
            room = GetComponentInParent<Room>();
            if (room != null)
                room.Cleared += OnRoomCleared;
            Show();
        }

        void OnEnable() => Flammables.Register(this);

        void OnDisable() => Flammables.Unregister(this);

        void OnDestroy()
        {
            if (room != null)
                room.Cleared -= OnRoomCleared;
        }

        void OnRoomCleared(Room _) => Disperse();

        /// <summary>Se disipa (la sala se limpió). Si ya ardía, sigue ardiendo.</summary>
        public void Disperse()
        {
            if (state != State.Swarm)
                return;
            state = State.Fading;
            stateStart = Time.time;
        }

        public void Ignite()
        {
            if (!CanIgnite)
                return;
            state = State.Burning;
            stateStart = Time.time;
        }

        void Update()
        {
            float elapsed = Time.time - stateStart;
            switch (state)
            {
                case State.Swarm:
                    Drift();
                    Sting();
                    break;

                case State.Burning when elapsed >= Mathf.Max(burn.Length, 1) * BurnFrameSeconds:
                    // Cae como ascuas y no vuelve.
                    GroundFire.Spawn(transform.position, emberSeconds);
                    Destroy(gameObject);
                    return;

                case State.Fading:
                    if (body != null)
                        body.color = new Color(1f, 1f, 1f, fadeSeconds > 0f ? 1f - Mathf.Clamp01(elapsed / fadeSeconds) : 0f);
                    if (elapsed >= fadeSeconds)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    break;
            }
            Show();
        }

        void Drift()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                player = found != null ? found.transform : null;
            }

            Vector2 position = transform.position;
            Vector2 direction;
            if (player != null && Vector2.Distance(position, player.position) <= followRange)
            {
                direction = ((Vector2)player.position - position).normalized;
            }
            else
            {
                if (Time.time >= nextWanderAt)
                {
                    nextWanderAt = Time.time + Random.Range(1.5f, 3f);
                    wanderDirection = Random.insideUnitCircle.normalized;
                }
                direction = wanderDirection * 0.5f;
            }

            Vector2 next = position + direction * speed * Time.deltaTime;
            // Las paredes la paran (las fosas y el lodo, no).
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            if (grid != null && !grid.HasLineOfSight(position, next, ProbeRadius))
            {
                // Se desliza por cada eje por separado antes de rendirse.
                Vector2 alongX = position + new Vector2(direction.x, 0f) * speed * Time.deltaTime;
                Vector2 alongY = position + new Vector2(0f, direction.y) * speed * Time.deltaTime;
                if (Mathf.Abs(direction.x) > 0.01f && grid.HasLineOfSight(position, alongX, ProbeRadius))
                    next = alongX;
                else if (Mathf.Abs(direction.y) > 0.01f && grid.HasLineOfSight(position, alongY, ProbeRadius))
                    next = alongY;
                else
                {
                    nextWanderAt = 0f;
                    return;
                }
            }
            transform.position = next;
        }

        // Pica al jugador que esté dentro de la nube (la esquiva pasa por encima).
        void Sting()
        {
            if (Time.time < nextDamageAt)
                return;
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, damageRadius, LayerMask.GetMask(CombatLayers.Player)))
            {
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                if (hit.GetComponentInParent<PlayerController>() is { IsDodging: true })
                    continue;
                nextDamageAt = Time.time + damageEvery;
                health.TakeDamage(damage);
                return;
            }
        }

        void Show()
        {
            if (body == null)
                return;
            bool burning = state == State.Burning;
            var frames = burning ? burn : idle;
            if (frames == null || frames.Length == 0)
                return;
            float seconds = burning ? BurnFrameSeconds : IdleFrameSeconds;
            int frame = Mathf.FloorToInt((Time.time - stateStart) / seconds);
            frame = burning ? Mathf.Clamp(frame, 0, frames.Length - 1) : frame % frames.Length;
            body.sprite = frames[frame];
        }
    }
}

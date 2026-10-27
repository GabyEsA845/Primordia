using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Euriptérido, jefe del piso 2 (GDD, "Euriptérido"): escorpión marino gigante en su cueva con rocas de cobertura.
    /// Acorazado (100–50 %): anda hacia el jugador girando despacio (rodearlo es la forma de pegarle sin coraza); su coraza
    /// para la mitad del daño que viene de delante, como la del Trilobite. Si el jugador está cerca de la cabeza, abre las
    /// pinzas (aviso) y golpea un cono por delante. Cada pocos segundos, si lo ve, eriza las púas del caparazón y suelta un
    /// abanico de 3 púas venenosas (las paran las rocas). La cueva cruje mientras tanto (<see cref="CaveCollapse"/>).
    /// La muda (50 %) y la fase blanda llegan después.
    /// Visto desde arriba: el cuerpo ("Body") gira hacia donde mira. Es sólido (el jugador no lo atraviesa) pero tocarlo no
    /// hace daño; anda, pero nunca cae en las fosas (las rodea).
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Rigidbody2D))]
    public class EurypteridBoss : MonoBehaviour, IBoss, IPhasedBoss, IFallingRockTarget
    {
        public const string WalkState = "Walk";
        public const string PincerWarnState = "PincerWarn";
        public const string PincerStrikeState = "PincerStrike";
        public const string SpineWarnState = "SpineWarn";
        public const string SpineShootState = "SpineShoot";
        public const string HurtState = "Hurt";
        public const string DeathState = "Death";
        public const string IntroState = "Intro";

        /// <summary>Fase de la pelea, según la vida.</summary>
        public enum Stage
        {
            /// <summary>100–50 %: acorazado, pinzas y púas.</summary>
            Armored,
            /// <summary>50–0 %: blando y furioso (cola y doble pinza).</summary>
            Soft
        }

        /// <summary>Lo que está haciendo ahora.</summary>
        public enum Mode
        {
            /// <summary>Antes de empezar (presentación).</summary>
            Idle,
            /// <summary>Anda hacia el jugador.</summary>
            Walk,
            /// <summary>Aviso de las pinzas: las abre y se echa atrás.</summary>
            PincerWarn,
            /// <summary>Cierra las pinzas: golpea un cono por delante.</summary>
            PincerStrike,
            /// <summary>Eriza y carga de veneno las púas del caparazón.</summary>
            SpineWarn,
            /// <summary>Suelta el abanico de púas.</summary>
            SpineShoot,
            /// <summary>Aturdido (le cayó una piedra del techo).</summary>
            Stunned,
            Dead
        }

        [SerializeField] string displayName = "Euriptérido";

        [Header("Piezas")]
        [Tooltip("Cuerpo que gira hacia donde mira (con el sprite, el Animator y la colisión).")]
        [SerializeField] Transform body;
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] EurypteridSpine spinePrefab;

        [Header("Andar")]
        [SerializeField, Min(0f)] float walkSpeed = 2.5f;
        [Tooltip("Grados por segundo que gira: despacio, para que se le pueda rodear.")]
        [SerializeField, Min(1f)] float turnRate = 90f;
        [Tooltip("No se acerca más que esto (de la cabeza al jugador): ahí ya usa las pinzas.")]
        [SerializeField, Min(0f)] float stopDistance = 1.2f;
        [Tooltip("Segundos quieto tras la presentación antes de empezar.")]
        [SerializeField, Min(0f)] float startDelay = 0.6f;

        [Header("Coraza")]
        [Tooltip("Ángulo a cada lado de la cabeza en el que la coraza para parte del daño.")]
        [SerializeField, Range(0f, 180f)] float armorAngle = 60f;
        [SerializeField, Range(0f, 1f)] float armorFactor = 0.5f;

        [Header("Pinzas")]
        [Tooltip("A esta distancia de la cabeza al jugador (y por delante) empieza el aviso.")]
        [SerializeField, Min(0.5f)] float pincerReach = 2.5f;
        [SerializeField, Min(0f)] float pincerWarnTime = 0.6f;
        [Tooltip("Durante esta primera parte del aviso aún gira hacia el jugador; luego queda fijo.")]
        [SerializeField, Min(0f)] float pincerAimTime = 0.3f;
        [Tooltip("Segundo del golpe en que hace daño (cuadro 1 de PincerStrike, a 60 ms).")]
        [SerializeField, Min(0f)] float pincerHitDelay = 0.06f;
        [SerializeField, Min(0)] int pincerDamage = 2;
        [Tooltip("Alcance del cono de las pinzas desde la cabeza (unidades) y su medio ángulo (grados).")]
        [SerializeField, Min(0.1f)] float pincerRadius = 2.4f;
        [SerializeField, Range(5f, 90f)] float pincerHalfAngle = 55f;
        [Tooltip("Segundos que se queda tras el golpe antes de volver a andar, y entre golpes.")]
        [SerializeField, Min(0f)] float pincerRecover = 0.4f;
        [SerializeField, Min(0f)] float pincerCooldown = 1.2f;
        [Tooltip("Cuánto se echa atrás durante el aviso (unidades).")]
        [SerializeField, Min(0f)] float recoil = 0.3f;

        [Header("Púas")]
        [Tooltip("Segundos entre abanicos (mínimo y máximo); solo dispara si ve al jugador.")]
        [SerializeField] Vector2 spineInterval = new(4f, 5f);
        [SerializeField, Min(0f)] float spineWarnTime = 0.6f;
        [SerializeField, Min(1)] int spineCount = 3;
        [Tooltip("Grados entre púa y púa del abanico.")]
        [SerializeField, Range(0f, 45f)] float spineSpread = 18f;
        [SerializeField, Min(0.1f)] float spineSpeed = 7f;
        [Tooltip("Segundos que dura la animación de soltar las púas.")]
        [SerializeField, Min(0f)] float spineShootTime = 0.24f;

        [Header("Cuerpo")]
        [Tooltip("De su centro a la cabeza (unidades).")]
        [SerializeField, Min(0f)] float headOffset = 2.3f;
        [Tooltip("Radio con el que choca con paredes y rocas al andar, y distancia que deja con las fosas.")]
        [SerializeField, Min(0.1f)] float bodyRadius = 0.9f;
        [SerializeField, Min(0f)] float pitMargin = 1.2f;

        public string DisplayName => displayName;
        public Health Health { get; private set; }
        public BossBarStyle BarStyle => CurrentStage == Stage.Soft ? BossBarStyle.Enraged : BossBarStyle.Normal;
        public Stage CurrentStage { get; private set; } = Stage.Armored;
        public Mode CurrentMode { get; private set; } = Mode.Idle;
        public Vector2 ArenaCenter => arenaCenter != null ? (Vector2)arenaCenter.position : home;

        /// <summary>Hacia dónde mira, normalizado.</summary>
        public Vector2 Heading { get; private set; } = Vector2.right;

        /// <summary>Punta de la cabeza, de donde salen las pinzas.</summary>
        public Vector2 Head => (Vector2)transform.position + Heading * headOffset;

        public int PhaseIndex => CurrentStage == Stage.Armored ? 0 : 1;
        public float[] PhaseThresholds => phaseThresholds ??= new[] { MoltThreshold };
        public const float MoltThreshold = 0.5f;

        /// <summary>Se lanza cada vez que cambia lo que está haciendo.</summary>
        public event System.Action<Mode> ModeChanged;

        Enemy enemy;
        Rigidbody2D rb;
        AnimatorStates states;
        Transform arenaCenter;
        Vector2 home;
        Transform player;
        CaveCollapse cave;
        bool waitingIntro;
        float fightStartsAt;
        float actionStart;
        float nextPincer;
        float nextSpines;
        float stunUntil;
        float hurtUntil;
        float flashUntil;
        bool struck;
        bool rockHit;
        float[] phaseThresholds;
        readonly RaycastHit2D[] castHits = new RaycastHit2D[6];

        const float FlashTime = 0.12f;
        const float WallSkin = 0.1f;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            Health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            // Cinemático: no lo empujan, pero el jugador choca con él (es sólido) y los disparos le dan.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;
            states = animator != null ? new AnimatorStates(animator) : null;
        }

        internal void Configure(Transform newBody, Animator newAnimator, SpriteRenderer newSprite, EurypteridSpine newSpine)
        {
            body = newBody;
            animator = newAnimator;
            sprite = newSprite;
            spinePrefab = newSpine;
            states = animator != null ? new AnimatorStates(animator) : null;
        }

        /// <summary>Ritmos (pruebas): segundos entre abanicos de púas y espera al empezar.</summary>
        internal void SetTimings(Vector2 newSpineInterval, float newStartDelay)
        {
            spineInterval = newSpineInterval;
            startDelay = newStartDelay;
            fightStartsAt = Time.time + startDelay;
            nextSpines = fightStartsAt + Random.Range(spineInterval.x, spineInterval.y);
        }

        /// <summary>Lo pone mirando hacia ahí de golpe (pruebas).</summary>
        internal void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                Heading = direction.normalized;
        }

        public void SetArena(Transform center, Vector2 size) => arenaCenter = center;

        public void PlayIntroPose(float hold) => states?.Play(IntroState, restart: true);

        public void BeginFight()
        {
            waitingIntro = false;
            fightStartsAt = Time.time + startDelay;
            nextSpines = fightStartsAt + Random.Range(spineInterval.x, spineInterval.y);
            states?.Play(WalkState);
        }

        void OnEnable()
        {
            // Se activa cuando el jugador entra en su sala (la sala oculta a sus enemigos hasta entonces).
            ActiveBoss.Set(this);
            home = transform.position;
            FindPlayer();
            cave = GetComponentInParent<CaveCollapse>();
            Health.Damaged += OnDamaged;
            Health.AddDamageModifier(ModifyDamage);
            enemy.Died += OnDied;

            CurrentMode = Mode.Idle;
            waitingIntro = TryGetComponent<BossIntro>(out var intro) && intro.Data != null && !intro.Played;
            fightStartsAt = Time.time + startDelay;
            nextSpines = fightStartsAt + Random.Range(spineInterval.x, spineInterval.y);
        }

        void OnDisable()
        {
            ActiveBoss.Clear(this);
            Health.Damaged -= OnDamaged;
            Health.RemoveDamageModifier(ModifyDamage);
            enemy.Died -= OnDied;
        }

        void FindPlayer()
        {
            if (player != null)
                return;
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        // ---------- Comportamiento ----------

        void FixedUpdate()
        {
            if (CurrentMode == Mode.Dead || !CanAct())
                return;
            FindPlayer();
            float dt = Time.fixedDeltaTime;
            float t = Time.time - actionStart;

            switch (CurrentMode)
            {
                case Mode.Idle:
                    if (!waitingIntro && Time.time >= fightStartsAt)
                    {
                        // Empieza la pelea: la cueva cruje durante la fase acorazada.
                        if (cave != null && cave.Current == CaveCollapse.Mode.Calm)
                            cave.SetMode(CaveCollapse.Mode.Creaking);
                        SetMode(Mode.Walk);
                    }
                    break;

                case Mode.Walk:
                    Steer(ToPlayer(), turnRate, dt);
                    if (player != null && Vector2.Distance(Head, player.position) > stopDistance)
                        Move(Heading * walkSpeed * dt);
                    if (player != null && Time.time >= nextPincer && InPincerReach(player.position))
                        SetMode(Mode.PincerWarn);
                    else if (player != null && Time.time >= nextSpines && Sees(player.position))
                        SetMode(Mode.SpineWarn);
                    break;

                case Mode.PincerWarn:
                    // Al principio sigue apuntando; luego queda fijo para que se pueda esquivar.
                    if (t < pincerAimTime)
                        Steer(ToPlayer(), turnRate, dt);
                    Move(-Heading * (recoil / Mathf.Max(0.01f, pincerWarnTime)) * dt);
                    if (t >= pincerWarnTime)
                        SetMode(Mode.PincerStrike);
                    break;

                case Mode.PincerStrike:
                    if (!struck && t >= pincerHitDelay)
                    {
                        struck = true;
                        TryPincer();
                    }
                    if (t >= PincerStrikeLength + pincerRecover)
                    {
                        nextPincer = Time.time + pincerCooldown;
                        SetMode(Mode.Walk);
                    }
                    break;

                case Mode.SpineWarn:
                    if (t >= spineWarnTime)
                    {
                        SetMode(Mode.SpineShoot);
                        ShootSpines();
                    }
                    break;

                case Mode.SpineShoot:
                    if (t >= spineShootTime)
                    {
                        nextSpines = Time.time + Random.Range(spineInterval.x, spineInterval.y);
                        SetMode(Mode.Walk);
                    }
                    break;

                case Mode.Stunned:
                    if (Time.time >= stunUntil)
                        SetMode(Mode.Walk);
                    break;
            }
        }

        float PincerStrikeLength => states != null && states.Has(PincerStrikeState)
            ? Mathf.Max(pincerHitDelay, states.Length(PincerStrikeState)) : 0.24f;

        void SetMode(Mode mode)
        {
            CurrentMode = mode;
            actionStart = Time.time;
            if (mode == Mode.PincerStrike)
                struck = false;
            // El aviso y los ataques se ven siempre: un golpe recibido no los tapa.
            bool restart = mode is Mode.PincerWarn or Mode.PincerStrike or Mode.SpineWarn or Mode.SpineShoot;
            if (restart)
                hurtUntil = 0f;
            PlayState(StateFor(mode), restart);
            ModeChanged?.Invoke(mode);
        }

        static string StateFor(Mode mode) => mode switch
        {
            Mode.PincerWarn => PincerWarnState,
            Mode.PincerStrike => PincerStrikeState,
            Mode.SpineWarn => SpineWarnState,
            Mode.SpineShoot => SpineShootState,
            Mode.Stunned => HurtState,
            Mode.Dead => DeathState,
            _ => WalkState
        };

        void PlayState(string state, bool restart = false)
        {
            if (states != null && states.Has(state))
                states.Play(state, restart);
        }

        Vector2 ToPlayer()
        {
            if (player == null)
                return Heading;
            var direction = (Vector2)player.position - rb.position;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Heading;
        }

        void Steer(Vector2 desired, float degreesPerSecond, float dt)
        {
            Heading = ((Vector2)Vector3.RotateTowards(Heading, desired, degreesPerSecond * Mathf.Deg2Rad * dt, 0f)).normalized;
        }

        /// <summary>Anda sin meterse en paredes ni rocas, y sin acercarse a las fosas. Devuelve cuánto ha avanzado.</summary>
        float Move(Vector2 step)
        {
            float distance = step.magnitude;
            if (distance <= 0f)
                return 0f;
            var direction = step / distance;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask("Default"));
            int count = Physics2D.CircleCast(rb.position, bodyRadius, direction, filter, castHits, distance + WallSkin);
            for (int i = 0; i < count; i++)
            {
                if (castHits[i].collider.attachedRigidbody == rb)
                    continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, castHits[i].distance - WallSkin));
            }
            var target = rb.position + direction * distance;
            // Los jefes no caen en las fosas: se para antes del borde (también delante, donde va la cabeza).
            if (!Pits.IsSafe(target, pitMargin) || !Pits.IsSafe(target + direction * headOffset * 0.6f, 0.3f))
                return 0f;
            rb.MovePosition(target);
            return distance;
        }

        /// <summary>True si el jugador está lo bastante cerca de la cabeza, y por delante, para el aviso de las pinzas.</summary>
        bool InPincerReach(Vector2 point)
        {
            var toPoint = point - Head;
            return toPoint.magnitude <= pincerReach && (toPoint.magnitude <= 1f || Vector2.Angle(Heading, toPoint) <= 75f);
        }

        /// <summary>True si <paramref name="point"/> está en el cono de las pinzas (o pegado a la cabeza).</summary>
        public bool InPincers(Vector2 point)
        {
            var toPoint = point - Head;
            float distance = toPoint.magnitude;
            if (distance <= 1f)
                return true;
            return distance <= pincerRadius && Vector2.Angle(Heading, toPoint) <= pincerHalfAngle;
        }

        void TryPincer()
        {
            if (player == null || !InPincers(player.position))
                return;
            var health = player.GetComponent<Health>();
            if (health == null)
                return;
            var reflector = player.GetComponentInParent<IHitReflector>();
            if (reflector != null && reflector.TryReflect(Health))
                return;
            health.TakeDamage(pincerDamage);
        }

        /// <summary>True si ve ese punto: ni paredes ni rocas en medio (las púas no las atraviesan).</summary>
        bool Sees(Vector2 point)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask("Default"));
            var from = rb.position;
            var toPoint = point - from;
            int count = Physics2D.Raycast(from, toPoint.normalized, filter, castHits, toPoint.magnitude);
            for (int i = 0; i < count; i++)
            {
                if (castHits[i].collider.attachedRigidbody != rb)
                    return false;
            }
            return true;
        }

        /// <summary>Abanico de púas desde el caparazón hacia el jugador.</summary>
        void ShootSpines()
        {
            if (spinePrefab == null)
                return;
            var aim = player != null ? ToPlayer() : Heading;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < spineCount; i++)
            {
                float angle = (baseAngle + (i - (spineCount - 1) / 2f) * spineSpread) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var spine = Instantiate(spinePrefab, rb.position + direction * 0.6f, Quaternion.identity, transform.parent);
                spine.gameObject.SetActive(true);
                spine.Launch(direction * spineSpeed, Health);
            }
        }

        // ---------- Coraza, piedras y daño ----------

        /// <summary>True si la coraza para parte del daño que viene de ese punto (está delante de la cabeza).</summary>
        public bool IsArmoredAgainst(Vector2 point)
        {
            if (CurrentStage != Stage.Armored)
                return false;
            var to = point - rb.position;
            return to.sqrMagnitude > 0.0001f && Vector2.Angle(Heading, to) <= armorAngle;
        }

        int ModifyDamage(Health target, int amount)
        {
            if (target != Health || rockHit || player == null)
                return amount;
            return IsArmoredAgainst(player.position) ? Health.ScaleDamage(amount, armorFactor) : amount;
        }

        /// <summary>Una piedra del techo le cae encima: daño entero (la coraza no la para) y aturdido.</summary>
        public void HitByFallingRock(int damage, float stunDuration)
        {
            if (CurrentMode == Mode.Dead)
                return;
            rockHit = true;
            Health.TakeDamage(damage);
            rockHit = false;
            if (Health.IsDead || CurrentMode is Mode.Idle)
                return;
            stunUntil = Mathf.Max(stunUntil, Time.time + stunDuration);
            SetMode(Mode.Stunned);
        }

        void OnDamaged(int amount)
        {
            if (CurrentMode is Mode.Walk && states != null && states.Has(HurtState))
            {
                states.Play(HurtState, restart: true);
                hurtUntil = Time.time + states.Length(HurtState);
            }
            else
            {
                flashUntil = Time.time + FlashTime;
            }
        }

        void OnDied(Enemy _)
        {
            CurrentMode = Mode.Dead;
            EurypteridSpine.BreakAll();
            if (cave != null)
                cave.SetMode(CaveCollapse.Mode.Calm);
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            hurtUntil = 0f;
            PlayState(DeathState, restart: true);
        }

        void Update()
        {
            if (CurrentMode == Mode.Dead || states == null)
                return;
            if (hurtUntil > 0f && Time.time >= hurtUntil)
            {
                hurtUntil = 0f;
                states.Play(StateFor(CurrentMode));
            }
        }

        void LateUpdate()
        {
            float angle = Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg;
            if (body != null)
                body.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (sprite != null)
                sprite.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : Color.white;
        }

        bool CanAct() =>
            !Health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);
    }
}

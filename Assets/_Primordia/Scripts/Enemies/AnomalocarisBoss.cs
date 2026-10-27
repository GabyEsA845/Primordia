using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Anomalocaris, jefe del piso 2 (GDD, "Anomalocaris"): el gran depredador del Cámbrico, en su sala en penumbra.
    /// Acecho (100–60 %): nada deprisa en círculos junto a las paredes dejando una estela de burbujas lentas y, cada 3–4,5 s,
    /// va a por el jugador: aviso de 0,7 s (se echa atrás con los ojos rojos y los apéndices abiertos) apuntando adonde irá
    /// el jugador, dash en línea recta a toda velocidad y mordisco en un cono por delante en cuanto lo alcanza, y 1 s quieto y
    /// vulnerable. El dash obliga a esquivar: seguir andando igual no basta.
    /// Cerco (60–25 %), escena de los ojos (al 25 %) y frenesí (25–0 %): en AnomalocarisBoss.Phases.cs.
    /// Visto desde arriba: el cuerpo ("Body") gira libre hacia donde nada o ataca. No choca con el jugador: le hace medio
    /// corazón al tocarlo, como la ameba gigante.
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Rigidbody2D))]
    public partial class AnomalocarisBoss : MonoBehaviour, IBoss, IPhasedBoss
    {
        public const string SwimState = "Swim";
        public const string SwimFastState = "SwimFast";
        public const string WarnState = "ClawWarn";
        public const string StrikeState = "ClawStrike";
        public const string VulnerableState = "Vulnerable";
        public const string HurtState = "Hurt";
        public const string DeathState = "Death";
        public const string IntroState = "Intro";
        public const string ChargeState = "Charge";
        public const string EyesState = "EyesDark";

        /// <summary>Fase de la pelea, según la vida.</summary>
        public enum Stage
        {
            /// <summary>100–60 %: acecho junto a las paredes.</summary>
            Stalk,
            /// <summary>60–25 %: alterna el acecho con cercos en espiral.</summary>
            Encircle,
            /// <summary>Al 25 %: escena de los ojos (inmune).</summary>
            Eyes,
            /// <summary>25–0 %: frenesí con los zarpazos marcados.</summary>
            Frenzy
        }

        /// <summary>Lo que está haciendo ahora.</summary>
        public enum Mode
        {
            /// <summary>Antes de empezar (presentación).</summary>
            Idle,
            /// <summary>Acecho: nada en círculos junto a las paredes.</summary>
            Swim,
            /// <summary>Va a por el jugador para el zarpazo.</summary>
            Hunt,
            /// <summary>Aviso del zarpazo: se echa atrás y abre los apéndices.</summary>
            Warn,
            /// <summary>Se lanza en línea recta adonde apuntó y muerde si alcanza al jugador por el camino.</summary>
            Dash,
            Strike,
            /// <summary>Quieto tras el zarpazo: el momento de castigarle.</summary>
            Vulnerable,
            /// <summary>Cerco: nada en espiral alrededor del jugador, cerrándose, con un muro de burbujas.</summary>
            Circle,
            /// <summary>Escena de los ojos: solo se ven sus ojos cruzando la sala a oscuras.</summary>
            Eyes,
            /// <summary>Frenesí: quieto, cargando, con su dash marcado en el suelo.</summary>
            Charge,
            /// <summary>Frenesí: dash largo y rapidísimo por la línea marcada.</summary>
            Frenzy,
            /// <summary>Frenesí: al final del primer dash salta hacia el jugador a morderle.</summary>
            Leap,
            /// <summary>Frenesí: aturdido un instante tras el salto (el momento bueno para pegarle).</summary>
            Stun,
            /// <summary>Frenesí: persigue al jugador a toda velocidad y le muerde si le alcanza.</summary>
            Chase,
            /// <summary>Frenesí: silueta de ojos brillantes, invulnerable, rondando la sala.</summary>
            Silhouette,
            Dead
        }

        [SerializeField] string displayName = "Anomalocaris";

        [Header("Piezas")]
        [Tooltip("Cuerpo que gira hacia donde nada (con el sprite, el Animator y la colisión).")]
        [SerializeField] Transform body;
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] AnomalocarisBubble bubblePrefab;
        [Tooltip("Tajo del zarpazo, dibujado hacia la derecha con el origen en su borde izquierdo.")]
        [SerializeField] GameObject swipeEffect;

        [Header("Nado")]
        [SerializeField, Min(0f)] float swimSpeed = 5f;
        [SerializeField, Min(0f)] float huntSpeed = 6.5f;
        [Tooltip("Grados por segundo que gira nadando.")]
        [SerializeField, Min(1f)] float turnRate = 220f;
        [Tooltip("Distancia del círculo de acecho a las paredes de la sala (horizontal y vertical, unidades).")]
        [SerializeField] Vector2 wallMargin = new(4.5f, 4f);
        [Tooltip("Segundos quieto tras la presentación antes de empezar a nadar.")]
        [SerializeField, Min(0f)] float startDelay = 0.6f;

        [Header("Burbujas")]
        [SerializeField, Min(0.05f)] float bubbleInterval = 0.3f;
        [SerializeField, Min(0f)] float bubbleSpeed = 0.4f;
        [SerializeField, Min(0.1f)] float bubbleLifetime = 4f;

        [Header("Zarpazo")]
        [Tooltip("Segundos entre zarpazos (mínimo y máximo).")]
        [SerializeField] Vector2 strikeInterval = new(3f, 4.5f);
        [Tooltip("A esta distancia de la cabeza al jugador empieza el aviso (luego se lanza hasta él).")]
        [SerializeField, Min(0.5f)] float strikeReach = 7f;
        [Tooltip("Segundos que puede pasar persiguiendo antes de rendirse y volver a acechar.")]
        [SerializeField, Min(0.1f)] float huntTimeout = 2.5f;
        [SerializeField, Min(0f)] float warnTime = 0.7f;
        [Tooltip("Durante esta primera parte del aviso aún gira hacia el jugador; luego la dirección queda fija.")]
        [SerializeField, Min(0f)] float aimTime = 0.6f;
        [Tooltip("Apunta adonde estará el jugador dentro de estos segundos (si sigue moviéndose igual).")]
        [SerializeField, Min(0f)] float aimLead = 0.25f;
        [Tooltip("Cuánto se echa atrás durante el aviso (unidades).")]
        [SerializeField, Min(0f)] float recoil = 0.4f;
        [Header("Dash")]
        [Tooltip("Velocidad del dash tras el aviso (unidades por segundo).")]
        [SerializeField, Min(1f)] float dashSpeed = 22f;
        [Tooltip("Cuánto sigue más allá del punto al que apuntó, y el largo máximo del dash (unidades).")]
        [SerializeField, Min(0f)] float dashOvershoot = 2.5f;
        [SerializeField, Min(0.5f)] float dashMaxDistance = 10f;

        [Header("Mordisco")]
        [Tooltip("Pequeño avance al morder, tras el dash (unidades).")]
        [SerializeField, Min(0f)] float lungeDistance = 0.6f;
        [SerializeField, Min(0.01f)] float lungeTime = 0.15f;
        [Tooltip("Segundo del golpe en que empieza a hacer daño (cuadro 1 de ClawStrike, a 60 ms).")]
        [SerializeField, Min(0f)] float hitDelay = 0.06f;
        [SerializeField, Min(0)] int clawDamage = 2;
        [Tooltip("Alcance del cono del zarpazo desde la cabeza (unidades) y su medio ángulo (grados).")]
        [SerializeField, Min(0.1f)] float clawRadius = 2.2f;
        [SerializeField, Range(5f, 90f)] float clawHalfAngle = 50f;
        [SerializeField, Min(0f)] float vulnerableTime = 1f;

        [Header("Cuerpo")]
        [Tooltip("De su centro a la cabeza y a la cola (unidades).")]
        [SerializeField, Min(0f)] float headOffset = 1.9f;
        [SerializeField, Min(0f)] float tailOffset = 2.2f;
        [Tooltip("Largo y ancho del cuerpo que hace daño al tocar (unidades).")]
        [SerializeField] Vector2 bodySize = new(4.2f, 1.2f);
        [SerializeField, Min(0)] int contactDamage = 1;

        public string DisplayName => displayName;
        public Health Health { get; private set; }
        public BossBarStyle BarStyle => CurrentMode == Mode.Silhouette ? BossBarStyle.Immune : CurrentStage switch
        {
            Stage.Eyes => BossBarStyle.Immune,
            Stage.Frenzy => BossBarStyle.Enraged,
            _ => BossBarStyle.Normal
        };

        public Stage CurrentStage { get; private set; } = Stage.Stalk;
        public Vector2 ArenaCenter => arenaCenter != null ? (Vector2)arenaCenter.position : home;
        public Mode CurrentMode { get; private set; } = Mode.Idle;

        /// <summary>Hacia dónde mira (y nada), normalizado.</summary>
        public Vector2 Heading { get; private set; } = Vector2.right;

        /// <summary>Punta de la cabeza, de donde sale el zarpazo.</summary>
        public Vector2 Head => (Vector2)transform.position + Heading * headOffset;

        /// <summary>Se lanza cada vez que cambia lo que está haciendo.</summary>
        public event System.Action<Mode> ModeChanged;

        Enemy enemy;
        Rigidbody2D rb;
        AnimatorStates states;
        Transform arenaCenter;
        Vector2 arenaSize = new(40f, 31f);
        Vector2 home;
        Transform player;
        Transform shadow;
        bool waitingIntro;
        float fightStartsAt;
        float actionStart;
        float nextStrike;
        float nextBubble;
        float hurtUntil;
        float flashUntil;
        int orbitSign = 1;
        bool struck;
        Vector2 lockedAim;
        float dashLeft;
        readonly Collider2D[] touching = new Collider2D[4];
        readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

        const float FlashTime = 0.12f;
        const float WallSkin = 0.1f;
        const float CastRadius = 0.7f;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            Health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            // Cinemático y sin choque con el jugador; los proyectiles y las mordidas sí le dan.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.excludeLayers = LayerMask.GetMask(CombatLayers.Player);
            states = animator != null ? new AnimatorStates(animator) : null;
        }

        internal void Configure(Transform newBody, Animator newAnimator, SpriteRenderer newSprite, AnomalocarisBubble newBubble,
            GameObject newSwipe)
        {
            body = newBody;
            animator = newAnimator;
            sprite = newSprite;
            bubblePrefab = newBubble;
            swipeEffect = newSwipe;
            states = animator != null ? new AnimatorStates(animator) : null;
        }

        /// <summary>Ritmos del zarpazo (pruebas): segundos entre zarpazos, alcance para avisar y espera al empezar.</summary>
        internal void SetTimings(Vector2 newStrikeInterval, float newStrikeReach, float newStartDelay)
        {
            strikeInterval = newStrikeInterval;
            strikeReach = newStrikeReach;
            startDelay = newStartDelay;
            fightStartsAt = Time.time + startDelay;
            nextStrike = fightStartsAt + UnityEngine.Random.Range(strikeInterval.x, strikeInterval.y);
        }

        public void SetArena(Transform center, Vector2 size)
        {
            arenaCenter = center;
            arenaSize = size;
        }

        public void PlayIntroPose(float hold)
        {
            // Durante la presentación no actúa, así que nadie le cambia la animación.
            states?.Play(IntroState, restart: true);
        }

        public void BeginFight()
        {
            waitingIntro = false;
            fightStartsAt = Time.time + startDelay;
            nextStrike = fightStartsAt + UnityEngine.Random.Range(strikeInterval.x, strikeInterval.y);
            states?.Play(SwimState);
        }

        void OnEnable()
        {
            // Se activa cuando el jugador entra en su sala (la sala oculta a sus enemigos hasta entonces).
            ActiveBoss.Set(this);
            home = transform.position;
            FindPlayer();
            Health.Damaged += OnDamaged;
            Health.AddDamageModifier(ModifyDamage);
            enemy.Died += OnDied;

            CurrentMode = Mode.Idle;
            // Con presentación espera a que acabe (BeginFight); sin ella (pruebas), empieza tras la espera.
            waitingIntro = TryGetComponent<BossIntro>(out var intro) && intro.Data != null && !intro.Played;
            fightStartsAt = Time.time + startDelay;
            nextStrike = fightStartsAt + UnityEngine.Random.Range(strikeInterval.x, strikeInterval.y);
        }

        void OnDisable()
        {
            ActiveBoss.Clear(this);
            Health.Damaged -= OnDamaged;
            Health.RemoveDamageModifier(ModifyDamage);
            enemy.Died -= OnDied;
            // Si se va a mitad de una escena o de un frenesí (otra escena, reinicio), todo vuelve a la normalidad.
            EndEyesScene();
            ClearMarkers();
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
                        SetMode(CurrentStage == Stage.Frenzy ? Mode.Charge : Mode.Swim);
                    break;

                case Mode.Swim:
                    Steer(OrbitDirection(), turnRate, dt);
                    Move(Heading * swimSpeed * dt);
                    DropBubbles();
                    // En el cerco, el ataque es una espiral alrededor del jugador en lugar de ir directo a por él.
                    if (player != null && Time.time >= nextStrike)
                        SetMode(CurrentStage == Stage.Encircle ? Mode.Circle : Mode.Hunt);
                    break;

                case Mode.Hunt:
                    if (player == null || t >= huntTimeout)
                    {
                        // No llega: vuelve a acechar y lo intenta un poco después.
                        nextStrike = Time.time + 1f;
                        SetMode(Mode.Swim);
                        break;
                    }
                    Steer(ToPlayer(), turnRate * 1.5f, dt);
                    Move(Heading * huntSpeed * dt);
                    DropBubbles();
                    if (Vector2.Distance(Head, player.position) <= strikeReach)
                        SetMode(Mode.Warn);
                    break;

                case Mode.Warn:
                    // Al principio sigue apuntando; luego la dirección queda fija para que se pueda esquivar.
                    if (t < aimTime)
                    {
                        Steer(ToAim(), 720f, dt);
                    }
                    else if (lockedAim == Vector2.zero)
                    {
                        // Se fija la dirección y el largo del dash: hasta donde apuntó y un poco más.
                        lockedAim = Heading;
                        float reach = player != null ? Vector2.Distance(Head, AimPoint()) : strikeReach;
                        dashLeft = Mathf.Min(reach + dashOvershoot, dashMaxDistance);
                    }
                    if (lockedAim != Vector2.zero)
                        Heading = lockedAim;
                    Move(-Heading * (recoil / Mathf.Max(0.01f, warnTime)) * dt);
                    if (t >= warnTime)
                        SetMode(Mode.Dash);
                    break;

                case Mode.Dash:
                {
                    float step = Mathf.Min(dashSpeed * dt, dashLeft);
                    float moved = Move(Heading * step);
                    dashLeft -= step;
                    // Muerde en cuanto alcanza al jugador; si no, al final del dash (o al chocar con la pared).
                    if (player != null && InClaw(player.position))
                    {
                        SetMode(Mode.Strike);
                        TryClaw();
                    }
                    else if (dashLeft <= 0f || moved < step * 0.5f)
                    {
                        SetMode(Mode.Strike);
                    }
                    break;
                }

                case Mode.Strike:
                    if (t < lungeTime)
                        Move(Heading * (lungeDistance / lungeTime) * dt);
                    if (!struck && t >= hitDelay && t <= lungeTime + hitDelay)
                        TryClaw();
                    if (t >= StrikeLength)
                        SetMode(Mode.Vulnerable);
                    break;

                case Mode.Vulnerable:
                    if (t >= vulnerableTime)
                    {
                        nextStrike = Time.time + UnityEngine.Random.Range(strikeInterval.x, strikeInterval.y);
                        // A veces cambia el sentido en que da vueltas.
                        if (UnityEngine.Random.value < 0.4f)
                            orbitSign = -orbitSign;
                        SetMode(CurrentStage == Stage.Frenzy ? Mode.Charge : Mode.Swim);
                    }
                    break;

                default:
                    UpdatePhaseMode(dt, t);
                    break;
            }

            if (CurrentMode is not (Mode.Vulnerable or Mode.Idle or Mode.Eyes))
                TouchPlayer();
        }

        float StrikeLength => states != null && states.Has(StrikeState) ? Mathf.Max(lungeTime + hitDelay, states.Length(StrikeState)) : 0.3f;

        void SetMode(Mode mode)
        {
            CurrentMode = mode;
            actionStart = Time.time;
            switch (mode)
            {
                case Mode.Warn:
                    lockedAim = Vector2.zero;
                    PlayState(WarnState, restart: true);
                    break;
                case Mode.Strike:
                    struck = false;
                    PlayState(StrikeState, restart: true);
                    float angle = Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg;
                    OneShotEffect.Spawn(swipeEffect, Head, angle);
                    break;
                case Mode.Vulnerable:
                    PlayState(VulnerableState, restart: true);
                    break;
                default:
                    PlayState(StateFor(mode));
                    BeginPhaseMode(mode);
                    break;
            }
            ModeChanged?.Invoke(mode);
        }

        static string StateFor(Mode mode) => mode switch
        {
            Mode.Hunt or Mode.Dash or Mode.Circle or Mode.Frenzy or Mode.Chase => SwimFastState,
            Mode.Charge => ChargeState,
            Mode.Leap => StrikeState,
            Mode.Stun => VulnerableState,
            Mode.Eyes or Mode.Silhouette => EyesState,
            Mode.Warn => WarnState,
            Mode.Strike => StrikeState,
            Mode.Vulnerable => VulnerableState,
            Mode.Dead => DeathState,
            _ => SwimState
        };

        void PlayState(string state, bool restart = false)
        {
            // Un golpe recibido no tapa el aviso ni el zarpazo: esos se ven siempre.
            if (restart)
                hurtUntil = 0f;
            if (states != null && states.Has(state))
                states.Play(state, restart);
        }

        /// <summary>Dirección para seguir el óvalo de acecho, a unas unidades de las paredes.</summary>
        Vector2 OrbitDirection()
        {
            Vector2 center = ArenaCenter;
            float rx = Mathf.Max(1f, arenaSize.x / 2f - wallMargin.x);
            float ry = Mathf.Max(1f, arenaSize.y / 2f - wallMargin.y);
            Vector2 offset = rb.position - center;
            float angle = Mathf.Atan2(offset.y / ry, offset.x / rx);
            float ahead = angle + orbitSign * 0.4f;
            var target = center + new Vector2(Mathf.Cos(ahead) * rx, Mathf.Sin(ahead) * ry);
            var direction = target - rb.position;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Heading;
        }

        /// <summary>Adonde estará el jugador dentro de <see cref="aimLead"/> segundos si sigue igual.</summary>
        Vector2 AimPoint()
        {
            if (player == null)
                return rb.position + Heading;
            var target = (Vector2)player.position;
            if (player.TryGetComponent<Rigidbody2D>(out var playerBody))
                target += playerBody.linearVelocity * aimLead;
            return target;
        }

        Vector2 ToAim()
        {
            var direction = AimPoint() - rb.position;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Heading;
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

        /// <summary>
        /// Se mueve sin meterse en las paredes (las fosas no le afectan: nada por encima). Devuelve cuánto ha avanzado.
        /// </summary>
        float Move(Vector2 step)
        {
            float distance = step.magnitude;
            if (distance <= 0f)
                return 0f;
            var direction = step / distance;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask("Default"));
            int count = Physics2D.CircleCast(rb.position, CastRadius, direction, filter, castHits, distance + WallSkin);
            for (int i = 0; i < count; i++)
            {
                if (castHits[i].collider.attachedRigidbody == rb)
                    continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, castHits[i].distance - WallSkin));
            }
            rb.MovePosition(rb.position + direction * distance);
            return distance;
        }

        void DropBubbles() => DropBubbles(bubbleInterval, bubbleLifetime, bubbleSpeed);

        /// <summary>Suelta una burbuja por la cola cada <paramref name="interval"/> segundos.</summary>
        void DropBubbles(float interval, float lifetime, float speed)
        {
            if (bubblePrefab == null || Time.time < nextBubble)
                return;
            nextBubble = Time.time + interval;
            var tail = rb.position - Heading * tailOffset;
            var side = new Vector2(-Heading.y, Heading.x) * UnityEngine.Random.Range(-0.3f, 0.3f);
            var bubble = Instantiate(bubblePrefab, tail + side * 0.5f, Quaternion.identity, transform.parent);
            bubble.Launch(-Heading * speed + side, lifetime);
        }

        void TryClaw()
        {
            if (player == null)
                return;
            if (!InClaw(player.position))
                return;
            struck = true;
            var health = player.GetComponent<Health>();
            if (health == null)
                return;
            // La esquiva justa de la Cutícula le devuelve el golpe.
            var reflector = player.GetComponentInParent<IHitReflector>();
            if (reflector != null && reflector.TryReflect(Health))
                return;
            health.TakeDamage(clawDamage);
        }

        /// <summary>True si <paramref name="point"/> está en el cono del zarpazo (o pegado a la cabeza).</summary>
        public bool InClaw(Vector2 point)
        {
            var toPoint = point - Head;
            float distance = toPoint.magnitude;
            if (distance <= 1f)
                return true;
            return distance <= clawRadius && Vector2.Angle(Heading, toPoint) <= clawHalfAngle;
        }

        void TouchPlayer()
        {
            if (contactDamage <= 0)
                return;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Player));
            float angle = Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg;
            var center = rb.position + Heading * ((headOffset - tailOffset) / 2f);
            int count = Physics2D.OverlapCapsule(center, bodySize, CapsuleDirection2D.Horizontal, angle, filter, touching);
            for (int i = 0; i < count; i++)
            {
                var target = touching[i].GetComponentInParent<Health>();
                if (target == null || target == Health)
                    continue;
                var reflector = touching[i].GetComponentInParent<IHitReflector>();
                if (reflector != null && reflector.TryReflect(Health))
                    continue;
                target.TakeDamage(contactDamage);
                // Las Espinas pinchan a quien toca al jugador, aunque no haya choque.
                var thorns = touching[i].GetComponentInParent<Mutations.Thorns>();
                if (thorns != null)
                    thorns.HurtAttacker(Health);
            }
        }

        // ---------- Vida y aspecto ----------

        void OnDamaged(int amount)
        {
            if ((CurrentMode is Mode.Swim or Mode.Hunt or Mode.Vulnerable or Mode.Circle or Mode.Chase or Mode.Stun)
                && states != null && states.Has(HurtState))
            {
                states.Play(HurtState, restart: true);
                hurtUntil = Time.time + states.Length(HurtState);
            }
            else
            {
                // Avisando o golpeando, la animación de daño taparía el ataque: solo un destello.
                flashUntil = Time.time + FlashTime;
            }
        }

        void OnDied(Enemy _)
        {
            CurrentMode = Mode.Dead;
            AnomalocarisBubble.PopAll();
            ClearMarkers();
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            hurtUntil = 0f;
            PlayState(DeathState, restart: true);
        }

        void Update()
        {
            if (CurrentMode == Mode.Dead)
                return;
            UpdateStage();
            if (states == null)
                return;
            // Vuelve a su animación cuando acaba la de daño.
            if (hurtUntil > 0f && Time.time >= hurtUntil)
            {
                hurtUntil = 0f;
                states.Play(StateFor(CurrentMode), restart: CurrentMode is Mode.Vulnerable);
            }
        }

        void LateUpdate()
        {
            float angle = Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg;
            if (body != null)
                body.localRotation = Quaternion.Euler(0f, 0f, angle);
            // La sombra de los que nadan (SwimmerLook) aparece en su Start: se busca hasta que existe.
            if (shadow == null && TryGetComponent<SwimmerLook>(out var look))
                shadow = look.Shadow;
            if (shadow != null)
            {
                // La sombra (una elipse de 12×4 px) toma el largo y el ancho del cuerpo y gira con él.
                shadow.localRotation = Quaternion.Euler(0f, 0f, angle);
                shadow.localScale = new Vector3(bodySize.x / 0.75f, bodySize.y / 0.25f, 1f);
            }
            if (sprite != null)
                sprite.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : Color.white;
        }

        bool CanAct() =>
            !Health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);
    }
}

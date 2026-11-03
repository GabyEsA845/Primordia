using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Player
{
    /// <summary>
    /// Movimiento en 8 direcciones, apuntado hacia el ratón y esquiva con invulnerabilidad.
    /// La orientación del sprite la lleva <see cref="PlayerAnimator"/>.
    /// La esquiva va en la dirección del movimiento; si el jugador está quieto, hacia donde apunta.
    /// Solo responde mientras el GameManager está en <see cref="GameState.Playing"/>.
    /// También hace el pequeño avance del mordisco (<see cref="IMeleeLunger"/>), sin invulnerabilidad.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour, IMeleeLunger
    {
        [Header("Movimiento")]
        [Tooltip("Unidades por segundo (1 unidad = 16 px).")]
        [SerializeField, Min(0f)] float moveSpeed = 6f;
        [Tooltip("Segundos que tarda en alcanzar la velocidad máxima al empezar a andar.")]
        [SerializeField, Min(0f)] float accelerationTime = 0.07f;
        [Tooltip("Segundos que tarda en pararse al soltar el movimiento.")]
        [SerializeField, Min(0f)] float decelerationTime = 0.09f;

        [Header("Esquiva")]
        [Tooltip("Distancia total que recorre la esquiva, en unidades.")]
        [SerializeField, Min(0.1f)] float dodgeDistance = 3f;
        [Tooltip("Duración de la esquiva en segundos. El jugador es invulnerable durante ella.")]
        [SerializeField, Min(0.05f)] float dodgeDuration = 0.25f;
        [Tooltip("Segundos de espera desde que termina una esquiva hasta poder hacer otra.")]
        [SerializeField, Min(0f)] float dodgeCooldown = 0.4f;

        [Header("Apuntado")]
        [Tooltip("Gira hacia el cursor. Aquí cuelgan la boca y el punto de disparo.")]
        [SerializeField] Transform aimPivot;

        public Vector2 MoveInput { get; private set; }
        public Vector2 AimDirection { get; private set; } = Vector2.right;

        /// <summary>Del jugador al cursor, en unidades (sin normalizar). Lo usa la cámara para adelantarse.</summary>
        public Vector2 AimOffset { get; private set; } = Vector2.right;
        public bool IsDodging { get; private set; }
        public bool IsLunging => lungeEndTime > 0f;
        public Vector2 DodgeDirection { get; private set; }
        public bool CanDodge => CanAct && !MovementLocked && !IsDodging && (Time.time >= nextDodgeTime || DodgeInChain <= ExtraDodges);

        /// <summary>
        /// Esquivas que se pueden encadenar después de la primera sin esperar la recarga (la Cola propulsora da 1). Pasada la
        /// recarga, la cadena empieza de nuevo.
        /// </summary>
        public int ExtraDodges
        {
            get => extraDodges;
            set => extraDodges = Mathf.Max(0, value);
        }

        /// <summary>Qué esquiva de la cadena es la actual (o la última): 1 la primera, 2 la segunda encadenada…</summary>
        public int DodgeInChain { get; private set; }

        /// <summary>Velocidad de movimiento; las mutaciones de Extremidades la modifican.</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Velocidad que manda sobre <see cref="MoveSpeed"/> mientras tenga valor (sin tocarla): la sala Parkour del piso 2
        /// la fija a la velocidad base para que las mejoras de velocidad no estropeen los saltos.
        /// </summary>
        public float? SpeedOverride { get; set; }

        /// <summary>
        /// Multiplica los tiempos de arranque y frenada: más de 1 pesa más y resbala, menos de 1 es más ágil. 0 = sin inercia.
        /// Lo pone el cuerpo de la criatura.
        /// </summary>
        public float Inertia
        {
            get => inertia;
            set => inertia = Mathf.Max(0f, value);
        }

        /// <summary>Distancia de la esquiva; las mutaciones de Extremidades la modifican.</summary>
        public float DodgeDistance
        {
            get => dodgeDistance;
            set => dodgeDistance = Mathf.Max(0.1f, value);
        }

        /// <summary>Segundos entre el final de una esquiva y la siguiente; las Aletas lo acortan.</summary>
        public float DodgeCooldown
        {
            get => dodgeCooldown;
            set => dodgeCooldown = Mathf.Max(0f, value);
        }

        /// <summary>Multiplicador de la velocidad al andar por el terreno (el lodo profundo del piso 3, 1 sin él). Lo fija quien lo causa cada paso.</summary>
        public float TerrainSpeedFactor { get; set; } = 1f;

        /// <summary>
        /// True mientras no puede andar ni esquivar (el mareo de la Peonza): se frena como al soltar el movimiento. Lo fija quien lo causa.
        /// </summary>
        public bool MovementLocked { get; set; }

        /// <summary>
        /// True mientras no se puede andar pero sí apuntar, atacar y esquivar: se frena como al soltar el movimiento (las Patas de salto
        /// del nivel III mientras cargan el salto). A diferencia de <see cref="MovementLocked"/>, no impide esquivar.
        /// </summary>
        public bool MoveFrozen { get; set; }

        /// <summary>
        /// Multiplicador de la velocidad al andar mientras mantiene un ataque (la Peonza gira un 35 % más lento). 1 = normal. No tiñe ni
        /// cuenta como ralentización.
        /// </summary>
        public float ChannelSpeedFactor
        {
            get => channelSpeedFactor;
            set => channelSpeedFactor = Mathf.Clamp(value, 0.05f, 1f);
        }

        /// <summary>True mientras va ralentizado (veneno de las púas del Euriptérido).</summary>
        public bool IsSlowed => Time.time < slowUntil;

        /// <summary>Multiplicador de la velocidad al andar por la ralentización (1 sin ella). No afecta a la esquiva.</summary>
        public float SlowFactor => IsSlowed ? slowFactor : 1f;

        /// <summary>
        /// Lo ralentiza a <paramref name=factor/> de su velocidad durante <paramref name=duration/> s. Si ya iba ralentizado,
        /// se queda la más fuerte y dura hasta el final más tardío.
        /// </summary>
        public void Slow(float factor, float duration)
        {
            factor = Mathf.Clamp01(factor);
            slowFactor = IsSlowed ? Mathf.Min(slowFactor, factor) : factor;
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        public event Action DodgeStarted;
        public event Action DodgeEnded;

        /// <summary>Se lanza al pulsar y al soltar el botón de esquiva (también si no se puede esquivar).</summary>
        public event Action DodgePressed;
        public event Action DodgeReleased;

        /// <summary>True mientras el botón de esquiva está pulsado.</summary>
        public bool DodgeHeld { get; private set; }

        /// <summary>
        /// Si es true, pulsar el botón no esquiva: quien lo pone (las Patas de salto del nivel III, que cargan el salto) decide cuándo
        /// llamar a <see cref="TryDodge"/> al soltarlo.
        /// </summary>
        public bool ManualDodge { get; set; }

        /// <summary>Duración de la esquiva; las Patas de salto la alargan con la distancia.</summary>
        public float DodgeDuration
        {
            get => dodgeDuration;
            set => dodgeDuration = Mathf.Max(0.05f, value);
        }

        float dodgeSpeed;
        float dodgeStartTime;
        float dodgeLength;

        /// <summary>Avance de la esquiva en curso, de 0 a 1 (las Patas de salto lo usan para la pose y el arco). 0 si no esquiva.</summary>
        public float DodgeProgress =>
            !IsDodging ? 0f : Mathf.Clamp01((Time.time - dodgeStartTime) / Mathf.Max(0.01f, dodgeEndTime - dodgeStartTime));

        /// <summary>Distancia de la esquiva en curso (o de la última), en unidades.</summary>
        public float DodgeLength => dodgeLength;

        int extraDodges;
        float inertia = 1f;
        float channelSpeedFactor = 1f;
        Vector2 walkVelocity;

        // Las pruebas lo desactivan para dar la entrada con SetInput.
        internal bool readInput = true;

        Rigidbody2D rb;
        Health health;
        Camera mainCamera;
        InputAction moveAction;
        InputAction aimAction;
        InputAction dodgeAction;
        float dodgeEndTime;
        float nextDodgeTime;
        Vector2 lungeVelocity;
        float lungeEndTime;
        Vector2 shoveVelocity;
        float shoveEndTime;
        float slowFactor = 1f;
        float slowUntil;
        readonly List<RaycastHit2D> castHits = new();

        // Hueco que se deja al chocar al final del avance.
        const float LungeSkin = 0.05f;

        /// <summary>True si el jugador puede actuar: está vivo y el juego está en curso.</summary>
        public bool CanAct => !health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);

        void Reset() => ConfigureRigidbody(GetComponent<Rigidbody2D>());

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            ConfigureRigidbody(rb);
            if (GetComponent<PlayerShadow>() == null)
                gameObject.AddComponent<PlayerShadow>();

            var actions = InputSystem.actions;
            if (actions != null)
            {
                moveAction = actions.FindAction("Player/Move");
                aimAction = actions.FindAction("Player/Aim");
                dodgeAction = actions.FindAction("Player/Dodge");
            }
        }

        void OnEnable() => health.Died += OnDied;

        void OnDisable()
        {
            health.Died -= OnDied;
            StopLunge();
            if (IsDodging)
                EndDodge();
            rb.linearVelocity = Vector2.zero;
        }

        void Update()
        {
            if (readInput)
                ReadInput();
            if (readInput && dodgeAction != null)
                SetDodgeHeld(dodgeAction.IsPressed());

            if (!CanAct)
                return;

            UpdateAimPivot();
        }

        void FixedUpdate()
        {
            if (IsDodging)
            {
                if (Time.time < dodgeEndTime)
                {
                    // En cámara lenta, más deprisa para que vaya a su ritmo de siempre (ver TimeWarp).
                    rb.linearVelocity = DodgeDirection * dodgeSpeed * TimeWarp.PlayerSpeed;
                    walkVelocity = Vector2.zero;
                    return;
                }

                EndDodge();
            }

            if (IsLunging)
            {
                if (Time.time < lungeEndTime && CanAct)
                {
                    rb.linearVelocity = lungeVelocity * TimeWarp.PlayerSpeed;
                    walkVelocity = Vector2.zero;
                    return;
                }

                StopLunge();
            }

            if (Time.time < shoveEndTime && CanAct)
            {
                rb.linearVelocity = shoveVelocity * TimeWarp.PlayerSpeed;
                walkVelocity = Vector2.zero;
                return;
            }

            if (CanAct)
            {
                rb.linearVelocity = Walk();
            }
            else
            {
                walkVelocity = Vector2.zero;
                rb.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Velocidad de andar de este paso: se acerca a la que pide el movimiento con un arranque y una frenada cortos
        /// (<see cref="accelerationTime"/> y <see cref="decelerationTime"/>), para que el cuerpo tenga algo de peso.
        /// </summary>
        Vector2 Walk()
        {
            float top = (SpeedOverride ?? moveSpeed) * SlowFactor * ChannelSpeedFactor * TerrainSpeedFactor * TimeWarp.PlayerSpeed;
            Vector2 target = MovementLocked || MoveFrozen ? Vector2.zero : MoveInput * top;
            float time = (target.sqrMagnitude > 0.0001f ? accelerationTime : decelerationTime) * inertia;
            // Cambio de velocidad por paso para ir de 0 a la máxima en ese tiempo; la ralentización no alarga el arranque.
            float maxChange = time > 0.0001f ? Mathf.Max(top, 0.01f) / time * Time.fixedDeltaTime : float.MaxValue;
            walkVelocity = Vector2.MoveTowards(walkVelocity, target, maxChange);
            return walkVelocity;
        }

        /// <summary>
        /// Cambia el estado del botón de esquiva: al pulsarlo esquiva (salvo con <see cref="ManualDodge"/>) y lanza
        /// <see cref="DodgePressed"/>; al soltarlo, <see cref="DodgeReleased"/>. Lo llama el teclado y las pruebas.
        /// </summary>
        internal void SetDodgeHeld(bool held)
        {
            if (held == DodgeHeld)
                return;
            DodgeHeld = held;
            if (held)
            {
                DodgePressed?.Invoke();
                if (!ManualDodge)
                    TryDodge();
            }
            else
                DodgeReleased?.Invoke();
        }

        /// <summary>
        /// Empieza una esquiva si se puede. Devuelve false si está en recarga o el juego no está en curso. Sin parámetros usa la
        /// dirección del movimiento (o del apuntado), la distancia y la duración de siempre; el salto cargado fija las suyas.
        /// </summary>
        public bool TryDodge(Vector2? direction = null, float? distance = null, float? duration = null)
        {
            if (!CanDodge)
                return false;

            // Pasada la recarga, empieza una cadena nueva; si no, es una esquiva encadenada.
            DodgeInChain = Time.time >= nextDodgeTime ? 1 : DodgeInChain + 1;
            DodgeDirection = direction.HasValue && direction.Value.sqrMagnitude > 0.0001f
                ? direction.Value.normalized
                : ChooseDodgeDirection(MoveInput, AimDirection);
            float length = distance ?? dodgeDistance;
            float seconds = duration ?? dodgeDuration;
            dodgeSpeed = length / seconds;
            dodgeLength = length;
            dodgeStartTime = Time.time;
            StopLunge();
            shoveEndTime = 0f;
            IsDodging = true;
            // Los tiempos van en tiempo de juego: en cámara lenta duran lo mismo para el jugador.
            dodgeEndTime = Time.time + seconds * TimeWarp.Scale;
            nextDodgeTime = dodgeEndTime + dodgeCooldown * TimeWarp.Scale;
            health.IsInvulnerable = true;

            DodgeStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// Avance corto al morder: recorre la distancia en ese tiempo, o menos si delante hay una pared o un
        /// cuerpo con el que choca. Devuelve la duración real (0 si no avanza).
        /// </summary>
        public float Lunge(Vector2 direction, float distance, float duration)
        {
            if (!CanAct || IsDodging || direction.sqrMagnitude < 0.0001f || distance <= 0f || duration <= 0f)
                return 0f;

            direction.Normalize();
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            float free = distance;
            if (rb.Cast(direction, filter, castHits, distance + LungeSkin) > 0)
            {
                foreach (var hit in castHits)
                    free = Mathf.Min(free, hit.distance - LungeSkin);
            }

            if (free <= LungeSkin)
                return 0f;

            float time = duration * free / distance * TimeWarp.Scale;
            lungeVelocity = direction * (distance / duration);
            lungeEndTime = Time.time + time;
            return time;
        }

        public void StopLunge() => lungeEndTime = 0f;

        /// <summary>True mientras un empujón externo (la fumarola) lo lleva.</summary>
        public bool IsShoved => Time.time < shoveEndTime;

        /// <summary>
        /// Lo empuja con esa velocidad durante ese tiempo (en tiempo de juego), sin dejarle moverse por su cuenta. Una esquiva
        /// empezada durante el empujón lo cancela. No hace nada si no puede actuar.
        /// </summary>
        public void Shove(Vector2 velocity, float duration)
        {
            if (!CanAct || duration <= 0f)
                return;
            shoveVelocity = velocity;
            shoveEndTime = Time.time + duration * TimeWarp.Scale;
        }

        /// <summary>La esquiva queda lista ya (Sangre en el agua del Tiburón).</summary>
        public void ResetDodgeCooldown() => nextDodgeTime = 0f;

        /// <summary>Dirección del movimiento si el jugador se mueve; si no, la del apuntado.</summary>
        public static Vector2 ChooseDodgeDirection(Vector2 move, Vector2 aim)
        {
            if (move.sqrMagnitude > 0.01f)
                return move.normalized;

            return aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
        }

        internal void SetInput(Vector2 move, Vector2 aim)
        {
            MoveInput = Vector2.ClampMagnitude(move, 1f);
            if (aim.sqrMagnitude > 0.0001f)
            {
                AimDirection = aim.normalized;
                AimOffset = aim;
            }
        }

        void ReadInput()
        {
            MoveInput = moveAction != null ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

            if (aimAction == null)
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            Vector2 screenPosition = aimAction.ReadValue<Vector2>();
            Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            var toCursor = worldPosition - rb.position;

            // Con el cursor justo encima del jugador se conserva la última dirección.
            if (toCursor.sqrMagnitude > 0.0001f)
            {
                AimDirection = toCursor.normalized;
                AimOffset = toCursor;
            }
        }

        void UpdateAimPivot()
        {
            if (aimPivot != null)
                aimPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg);
        }

        void EndDodge()
        {
            IsDodging = false;
            health.IsInvulnerable = false;
            DodgeEnded?.Invoke();
        }

        void OnDied()
        {
            if (IsDodging)
                EndDodge();
            rb.linearVelocity = Vector2.zero;
            walkVelocity = Vector2.zero;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Defeat);
        }

        static void ConfigureRigidbody(Rigidbody2D body)
        {
            // Vista cenital: sin gravedad ni rotación por choques.
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
}

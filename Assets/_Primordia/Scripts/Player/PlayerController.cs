using System;
using Primordia.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Player
{
    /// <summary>
    /// Movimiento en 8 direcciones, apuntado hacia el ratón y esquiva con invulnerabilidad.
    /// La esquiva va en la dirección del movimiento; si el jugador está quieto, hacia donde apunta.
    /// Solo responde mientras el GameManager está en <see cref="GameState.Playing"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [Tooltip("Unidades por segundo (1 unidad = 16 px).")]
        [SerializeField, Min(0f)] float moveSpeed = 6f;

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
        [Tooltip("Sprite del cuerpo; se voltea según el lado al que se apunta.")]
        [SerializeField] SpriteRenderer body;

        public Vector2 MoveInput { get; private set; }
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public bool IsDodging { get; private set; }
        public Vector2 DodgeDirection { get; private set; }
        public bool CanDodge => CanAct && !IsDodging && Time.time >= nextDodgeTime;

        /// <summary>Velocidad de movimiento; las mutaciones de Extremidades la modifican.</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>Distancia de la esquiva; las mutaciones de Extremidades la modifican.</summary>
        public float DodgeDistance
        {
            get => dodgeDistance;
            set => dodgeDistance = Mathf.Max(0.1f, value);
        }

        public event Action DodgeStarted;
        public event Action DodgeEnded;

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

        bool CanAct => !health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);

        void Reset() => ConfigureRigidbody(GetComponent<Rigidbody2D>());

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            ConfigureRigidbody(rb);

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
            if (IsDodging)
                EndDodge();
            rb.linearVelocity = Vector2.zero;
        }

        void Update()
        {
            if (readInput)
                ReadInput();

            if (!CanAct)
                return;

            if (readInput && dodgeAction != null && dodgeAction.WasPressedThisFrame())
                TryDodge();

            UpdateFacing();
        }

        void FixedUpdate()
        {
            if (IsDodging)
            {
                if (Time.time < dodgeEndTime)
                {
                    rb.linearVelocity = DodgeDirection * (dodgeDistance / dodgeDuration);
                    return;
                }

                EndDodge();
            }

            rb.linearVelocity = CanAct ? MoveInput * moveSpeed : Vector2.zero;
        }

        /// <summary>Empieza una esquiva si se puede. Devuelve false si está en recarga o el juego no está en curso.</summary>
        public bool TryDodge()
        {
            if (!CanDodge)
                return false;

            DodgeDirection = ChooseDodgeDirection(MoveInput, AimDirection);
            IsDodging = true;
            dodgeEndTime = Time.time + dodgeDuration;
            nextDodgeTime = dodgeEndTime + dodgeCooldown;
            health.IsInvulnerable = true;

            DodgeStarted?.Invoke();
            return true;
        }

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
                AimDirection = aim.normalized;
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
                AimDirection = toCursor.normalized;
        }

        void UpdateFacing()
        {
            if (aimPivot != null)
                aimPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg);

            if (body != null)
                body.flipX = AimDirection.x < 0f;
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

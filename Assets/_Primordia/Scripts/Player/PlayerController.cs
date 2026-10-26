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
        public bool CanDodge => CanAct && !IsDodging && (Time.time >= nextDodgeTime || DodgeInChain <= ExtraDodges);

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

        public event Action DodgeStarted;
        public event Action DodgeEnded;

        int extraDodges;

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

            if (!CanAct)
                return;

            if (readInput && dodgeAction != null && dodgeAction.WasPressedThisFrame())
                TryDodge();

            UpdateAimPivot();
        }

        void FixedUpdate()
        {
            if (IsDodging)
            {
                if (Time.time < dodgeEndTime)
                {
                    // En cámara lenta, más deprisa para que vaya a su ritmo de siempre (ver TimeWarp).
                    rb.linearVelocity = DodgeDirection * (dodgeDistance / dodgeDuration) * TimeWarp.PlayerSpeed;
                    return;
                }

                EndDodge();
            }

            if (IsLunging)
            {
                if (Time.time < lungeEndTime && CanAct)
                {
                    rb.linearVelocity = lungeVelocity * TimeWarp.PlayerSpeed;
                    return;
                }

                StopLunge();
            }

            rb.linearVelocity = CanAct ? MoveInput * moveSpeed * TimeWarp.PlayerSpeed : Vector2.zero;
        }

        /// <summary>Empieza una esquiva si se puede. Devuelve false si está en recarga o el juego no está en curso.</summary>
        public bool TryDodge()
        {
            if (!CanDodge)
                return false;

            // Pasada la recarga, empieza una cadena nueva; si no, es una esquiva encadenada.
            DodgeInChain = Time.time >= nextDodgeTime ? 1 : DodgeInChain + 1;
            DodgeDirection = ChooseDodgeDirection(MoveInput, AimDirection);
            StopLunge();
            IsDodging = true;
            // Los tiempos van en tiempo de juego: en cámara lenta duran lo mismo para el jugador.
            dodgeEndTime = Time.time + dodgeDuration * TimeWarp.Scale;
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

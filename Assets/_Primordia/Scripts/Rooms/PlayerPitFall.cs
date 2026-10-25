using System.Collections;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El jugador en las fosas: andando cae (se encoge y se oscurece, vuelve al último suelo firme y pierde medio corazón,
    /// con un momento de invulnerabilidad); esquivando las cruza, así que salta las de hasta 2 tiles. Lo añade
    /// <see cref="FloorGenerator"/> al montar el piso.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPitFall : MonoBehaviour
    {
        /// <summary>Medio corazón.</summary>
        public const int FallDamage = 1;

        // Para guardar el último suelo firme, los pies tienen que estar al menos a esta distancia de cualquier fosa.
        const float SafeMargin = 0.45f;
        const float InvulnerabilityAfterFall = 1f;

        [Tooltip("Dónde están los pies respecto al centro del cuerpo: la caída se mide ahí, no en el centro.")]
        [SerializeField] Vector2 footOffset = new(0f, -0.3f);
        [Tooltip("Cuánto tienen que entrar los pies en la fosa (por todos los lados) para caer: asomarse al borde no hace caer.")]
        [SerializeField, Min(0f)] float edgeGrace = 0.25f;
        [Tooltip("Segundos sobre la fosa antes de caer (perdona pasar rozando).")]
        [SerializeField, Min(0f)] float coyoteTime = 0.08f;

        float overPitSince = -1f;

        PlayerController controller;
        Rigidbody2D body;
        Health health;
        Vector2 lastSafe;
        bool hasSafe;

        public bool IsFalling { get; private set; }

        /// <summary>Último sitio firme, al que vuelve tras caer.</summary>
        public Vector2 LastSafePosition => lastSafe;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
        }

        void OnEnable() => hasSafe = false;

        void FixedUpdate()
        {
            if (IsFalling || Pits.Map == null)
                return;

            var position = body != null ? body.position : (Vector2)transform.position;
            var feet = position + footOffset;
            // Esquivando (y en las embestidas) se pasa por encima.
            if (controller.IsDodging || controller.IsLunging)
            {
                overPitSince = -1f;
                return;
            }

            if (Pits.IsDeepInPit(feet, edgeGrace))
            {
                if (overPitSince < 0f)
                    overPitSince = Time.time;
                if (Time.time - overPitSince >= coyoteTime && (health == null || !health.IsDead))
                {
                    overPitSince = -1f;
                    StartCoroutine(Fall());
                }
                return;
            }
            overPitSince = -1f;

            if (Pits.IsSafe(feet, SafeMargin))
            {
                lastSafe = position;
                hasSafe = true;
            }
        }

        IEnumerator Fall()
        {
            IsFalling = true;
            controller.enabled = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            System.Action restore = null;
            yield return PitFallEffect.Play(transform, action => restore = action);

            var back = hasSafe ? lastSafe : (Vector2)transform.position;
            if (body != null)
            {
                body.position = back;
                body.simulated = true;
            }
            transform.position = back;
            restore?.Invoke();
            controller.enabled = true;
            IsFalling = false;

            if (health != null)
            {
                health.TakeDamage(FallDamage);
                health.MakeInvulnerable(InvulnerabilityAfterFall);
            }
        }
    }
}

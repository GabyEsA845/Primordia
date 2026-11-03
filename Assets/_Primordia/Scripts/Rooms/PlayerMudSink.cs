using System.Collections;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El jugador en el lodo profundo (piso 3): mientras lo pisa se hunde poco a poco (la barra de hundimiento sube) y va cada
    /// vez más lento; en tierra firme la barra baja. Esquivar (o embestir) cruza el lodo sin hundirse y deja la barra en pausa (no
    /// se vacía: al acabar sigue desde donde estaba; solo la vacía un salto, ver <see cref="DodgeResetsSink"/>). Con la barra
    /// llena, vuelve al último suelo firme y pierde medio corazón, con un momento de invulnerabilidad, como en la fosa. Lo añade
    /// <see cref="FloorGenerator"/> al montar el piso. El lodo se ve tapando al jugador de abajo arriba (<see cref="MudSinkVisual"/>).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMudSink : MonoBehaviour
    {
        /// <summary>Medio corazón.</summary>
        public const int SinkDamage = 1;

        // Para guardar el último suelo firme, los pies tienen que estar al menos a esta distancia de cualquier fosa o lodo.
        const float SafeMargin = 0.45f;
        const float InvulnerabilityAfterSink = 1f;

        // Lo que baja la barra de quemado por segundo mientras se pisa lodo.
        const float BurnDouseSpeed = 3f;

        [Tooltip("Dónde están los pies respecto al centro del cuerpo: el lodo se mide ahí.")]
        [SerializeField] Vector2 footOffset = new(0f, -0.3f);
        [Tooltip("Segundos en el lodo hasta hundirse del todo.")]
        [SerializeField, Min(0.1f)] float fillSeconds = 1.6f;
        [Tooltip("Segundos en tierra firme para vaciar la barra desde llena.")]
        [SerializeField, Min(0.1f)] float emptySeconds = 1f;
        [Tooltip("Fracción de la velocidad al andar con la barra llena (con la barra vacía, 1).")]
        [SerializeField, Range(0.1f, 1f)] float slowestSpeed = 0.45f;

        PlayerController controller;
        Rigidbody2D body;
        Health health;
        Vector2 lastFirm;
        bool hasFirm;

        /// <summary>Hundimiento actual, de 0 a 1.</summary>
        public float Sink { get; private set; }

        public bool IsSinking { get; private set; }

        /// <summary>
        /// Si la esquiva vacía la barra de hundimiento. Por defecto no: la esquiva la deja en pausa y sigue desde ahí. La mutación
        /// que cambia la esquiva por un salto lo pondrá a true (saltar sí saca del lodo).
        /// </summary>
        public bool DodgeResetsSink { get; set; }

        /// <summary>
        /// Cruza el lodo profundo sin hundirse (el Eryops): solo va a <see cref="MudWalkSpeed"/> y no pierde vida. El dibujo del barro lo
        /// pone quien lo activa.
        /// </summary>
        public bool SinkImmune { get; set; }

        /// <summary>Fracción de la velocidad en el lodo profundo cuando <see cref="SinkImmune"/> (1 = sin frenar).</summary>
        public float MudWalkSpeed { get; set; } = 0.6f;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
        }

        void OnEnable() => hasFirm = false;

        void OnDisable() => Clear();

        void FixedUpdate()
        {
            var fall = GetComponent<PlayerPitFall>();
            if (IsSinking || Pits.Map == null || (fall != null && fall.IsFalling) || (health != null && health.IsDead))
                return;

            var position = body != null ? body.position : (Vector2)transform.position;
            var feet = position + footOffset;

            // Esquivando (y en las embestidas) se pasa por encima: el hundimiento se queda en pausa (ni sube ni baja) y sigue
            // desde ahí al acabar. Así no se cruza el lodo a base de esquivas. Un salto sí sale limpio (ver DodgeResetsSink).
            if (controller.IsDodging || controller.IsLunging)
            {
                if (DodgeResetsSink && Sink > 0f)
                    Clear();
                return;
            }

            if (SinkImmune)
            {
                bool wading = Pits.IsMud(feet);
                if (wading && TryGetComponent<PlayerBurn>(out var wet))
                    wet.Douse(BurnDouseSpeed * Time.fixedDeltaTime);
                if (Sink > 0f)
                    Clear();
                controller.TerrainSpeedFactor = wading ? MudWalkSpeed : 1f;
                if (!wading && Pits.IsFirm(feet, SafeMargin))
                {
                    lastFirm = position;
                    hasFirm = true;
                }
                return;
            }

            bool inMud = Pits.IsMud(feet);
            // El lodo apaga el quemado casi de golpe (la barra entera en ~⅓ de segundo).
            if (inMud && TryGetComponent<PlayerBurn>(out var burn))
                burn.Douse(BurnDouseSpeed * Time.fixedDeltaTime);
            float before = Sink;
            Sink = Mathf.Clamp01(Sink + (inMud ? 1f / fillSeconds : -1f / emptySeconds) * Time.fixedDeltaTime);

            if (Sink > 0f)
            {
                controller.TerrainSpeedFactor = Mathf.Lerp(1f, slowestSpeed, Sink);
                MudSinkVisual.SetLevel(transform, Sink);
            }
            else if (before > 0f)
                Clear();

            if (Sink >= 1f)
            {
                StartCoroutine(SinkFully());
                return;
            }

            if (!inMud && Pits.IsFirm(feet, SafeMargin))
            {
                lastFirm = position;
                hasFirm = true;
            }
        }

        void Clear()
        {
            Sink = 0f;
            controller.TerrainSpeedFactor = 1f;
            MudSinkVisual.SetLevel(transform, 0f);
        }

        IEnumerator SinkFully()
        {
            IsSinking = true;
            controller.enabled = false;
            Sfx.Play(Sounds.PlayerPitFall);
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            // Un último instante con el lodo tapándolo antes de volver a la orilla.
            yield return new WaitForSeconds(0.25f);

            var back = hasFirm ? lastFirm : (Vector2)transform.position;
            if (body != null)
            {
                body.position = back;
                body.simulated = true;
            }
            transform.position = back;
            Clear();
            controller.enabled = true;
            IsSinking = false;

            if (health != null)
            {
                health.TakeDamage(SinkDamage, DamageKind.Fall);
                health.MakeInvulnerable(InvulnerabilityAfterSink);
            }
        }
    }
}

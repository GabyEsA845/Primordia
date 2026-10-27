using System.Collections;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Enemies
{
    /// <summary>
    /// Escena de la muda y fase blanda del Euriptérido (GDD, "Euriptérido").
    /// Muda (al 50 %, unos 3 s, se salta con E): franjas de cine, se queda quieto, se agrieta y sale el cuerpo nuevo, rosado
    /// y blando. Es inmune y antes de ella no baja del 50 %. La cueva tiembla (terremoto: se derrumban las rocas agrietadas)
    /// y la muda vacía (<see cref="EurypteridExuvia"/>) se queda como cobertura que se rompe a golpes.
    /// Blando y furioso (50–0 %): recibe un 25 % más de daño, anda el doble de rápido y gira más deprisa; ya no tiene púas.
    /// Si el jugador está detrás o a los lados, barre con la cola (aviso: la punta se enciende en verde) y deja una estela
    /// venenosa unos segundos. Sus pinzas encadenan dos golpes. La cueva se derrumba sin parar hasta el final.
    /// </summary>
    public partial class EurypteridBoss
    {
        public const string MoltState = "Molt";
        public const string WalkSoftState = "Walk_Soft";
        public const string HurtSoftState = "Hurt_Soft";
        public const string DoublePincerState = "DoublePincer";
        public const string TailWarnState = "TailWarn";
        public const string TailSweepState = "TailSweep";

        [Header("Muda")]
        [Tooltip("Lo que dura la escena (la animación Molt: 12 cuadros a 250 ms).")]
        [SerializeField, Min(0.1f)] float moltTime = 3f;
        [Tooltip("Al final de la muda el cuerpo nuevo está esto más adelante (unidades); la muda vacía queda atrás.")]
        [SerializeField, Min(0f)] float moltAdvance = 1f;
        [SerializeField] EurypteridExuvia exuviaPrefab;
        [SerializeField] string skipText = "[E] Saltar";

        [Header("Fase blanda")]
        [SerializeField, Min(1f)] float softSpeedFactor = 2f;
        [SerializeField, Min(1f)] float softTurnRate = 150f;
        [Tooltip("Daño que recibe sin la coraza (1,25 = un 25 % más).")]
        [SerializeField, Min(1f)] float softDamageFactor = 1.25f;
        [SerializeField, Min(0f)] float softPincerWarnTime = 0.45f;
        [Tooltip("Segundos de la doble pinza en que golpea cada pinza (cuadros 1 y 3 de DoublePincer, a 70 ms).")]
        [SerializeField] float[] doublePincerHits = { 0.07f, 0.21f };

        [Header("Cola")]
        [Tooltip("Si el jugador está a menos de esto del centro, detrás o a los lados, barre con la cola.")]
        [SerializeField, Min(0.5f)] float tailReach = 3.5f;
        [Tooltip("Grados desde la cabeza a partir de los que el jugador está 'a los lados o detrás'.")]
        [SerializeField, Range(0f, 180f)] float tailSideAngle = 70f;
        [SerializeField, Min(0f)] float tailWarnTime = 0.6f;
        [Tooltip("Segundo del barrido en que golpea, y lo que dura con la pausa de después.")]
        [SerializeField, Min(0f)] float tailHitDelay = 0.1f;
        [SerializeField, Min(0f)] float tailSweepTime = 0.55f;
        [SerializeField, Min(0)] int tailDamage = 2;
        [Tooltip("Radio de la media luna del barrido (unidades).")]
        [SerializeField, Min(0.5f)] float tailRadius = 3.2f;
        [SerializeField, Min(0f)] float tailCooldown = 2.5f;
        [Tooltip("Cuadros del barrido venenoso (media luna hacia la izquierda del dibujo, centrada en él).")]
        [SerializeField] Sprite[] tailSweepFrames = System.Array.Empty<Sprite>();
        [Tooltip("Segundos que queda la estela venenosa en el suelo.")]
        [SerializeField, Min(0f)] float trailTime = 2f;

        Coroutine moltScene;
        bool startedCinematic;
        bool skipRequested;
        bool tailHit;
        float nextTail;
        CinematicOverlay overlay;
        CameraLookAhead cameraTarget;
        InputAction interactAction;

        int MoltHealth => Mathf.CeilToInt(Health.Max * MoltThreshold);

        /// <summary>La muda vacía que dejó (null si aún no ha mudado o ya se rompió).</summary>
        public EurypteridExuvia Exuvia { get; private set; }

        internal void ConfigurePhases(EurypteridExuvia newExuvia, Sprite[] newTailSweep)
        {
            exuviaPrefab = newExuvia;
            tailSweepFrames = newTailSweep ?? System.Array.Empty<Sprite>();
        }

        /// <summary>Duración de la muda (pruebas).</summary>
        internal void SetMoltTime(float seconds) => moltTime = Mathf.Max(0.1f, seconds);

        /// <summary>Deja listos ya las pinzas y el barrido de cola (pruebas).</summary>
        internal void ReadyAttacks() => nextPincer = nextTail = 0f;

        /// <summary>Aviso de la doble pinza: el cuerpo destella mientras espera con las pinzas abiertas.</summary>
        bool WarnFlash => CurrentStage == Stage.Soft && CurrentMode == Mode.PincerWarn && Mathf.FloorToInt(Time.time * 10f) % 2 == 0;

        void UpdateStage()
        {
            if (Health.IsDead || CurrentMode == Mode.Idle || CurrentStage != Stage.Armored)
                return;
            // Por si otro modificador de daño (los beneficios de los linajes) lo dejó por debajo antes de tiempo.
            if (Health.Current < MoltHealth)
                Health.Heal(MoltHealth - Health.Current);
            if (Health.Current <= MoltHealth)
                moltScene = StartCoroutine(MoltScene());
        }

        // ---------- Escena de la muda ----------

        IEnumerator MoltScene()
        {
            CurrentStage = Stage.Molt;
            SetMode(Mode.Molt);
            EurypteridSpine.BreakAll();
            Projectile.DespawnAll();
            skipRequested = false;
            if (cave != null)
                cave.SetMode(CaveCollapse.Mode.Quake);

            var manager = GameManager.Instance;
            if (manager != null)
            {
                manager.SetState(GameState.Cinematic);
                startedCinematic = true;
            }
            if (player != null && player.TryGetComponent<Health>(out var playerHealth))
                playerHealth.MakeInvulnerable(moltTime + 0.5f);
            cameraTarget = FindAnyObjectByType<CameraLookAhead>();
            if (cameraTarget != null)
                cameraTarget.SetFocus(rb.position, 0.6f);
            overlay = FindAnyObjectByType<CinematicOverlay>();
            if (overlay != null)
                overlay.SetSkipHint(true, skipText);
            interactAction ??= InputSystem.actions?.FindAction("Player/Interact");

            for (float t = 0f; t < moltTime && !skipRequested; t += Time.deltaTime)
            {
                // Como en la presentación: no se salta en los primeros instantes, para no hacerlo sin querer.
                if (t > 0.2f && interactAction != null && interactAction.WasPressedThisFrame())
                    skipRequested = true;
                yield return null;
            }

            // El cuerpo nuevo sale hacia delante; la muda vacía se queda donde estaba, como cobertura.
            Vector2 shell = rb.position;
            float angle = Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg;
            var advanced = shell + Heading * Clearance(Heading, moltAdvance);
            rb.position = advanced;
            transform.position = advanced;
            if (exuviaPrefab != null)
            {
                Exuvia = Instantiate(exuviaPrefab, shell, Quaternion.Euler(0f, 0f, angle), transform.parent);
                Exuvia.gameObject.SetActive(true);
            }

            moltScene = null;
            EndMoltScene();
            CurrentStage = Stage.Soft;
            if (cave != null)
                cave.SetMode(CaveCollapse.Mode.Collapse);
            nextTail = Time.time + 1f;
            SetMode(Mode.Walk);
        }

        /// <summary>Deja todo como antes de la escena (también si se interrumpe).</summary>
        void EndMoltScene()
        {
            if (moltScene != null)
                StopCoroutine(moltScene);
            moltScene = null;
            if (cameraTarget != null)
                cameraTarget.ClearFocus();
            if (overlay != null)
                overlay.SetSkipHint(false);
            if (startedCinematic && GameManager.Instance != null && GameManager.Instance.State == GameState.Cinematic)
                GameManager.Instance.SetState(GameState.Playing);
            startedCinematic = false;
        }

        // ---------- Fase blanda ----------

        /// <summary>True si el jugador está lo bastante cerca, detrás o a los lados, para el barrido de cola.</summary>
        public bool InTailReach(Vector2 point)
        {
            var toPoint = point - rb.position;
            return toPoint.magnitude <= tailReach && Vector2.Angle(Heading, toPoint) >= tailSideAngle;
        }

        /// <summary>True si <paramref name="point"/> está en la media luna del barrido (detrás y a los lados).</summary>
        public bool InTailSweep(Vector2 point)
        {
            var toPoint = point - rb.position;
            return toPoint.magnitude <= tailRadius && Vector2.Angle(Heading, toPoint) >= tailSideAngle;
        }

        void BeginSoftMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.PincerWarn when CurrentStage == Stage.Soft:
                    // Sin aviso propio en rosa: la doble pinza quieta en su primer cuadro (ya abiertas).
                    if (animator != null)
                        animator.speed = 0f;
                    break;
                case Mode.TailSweep:
                    tailHit = false;
                    float angle = Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg;
                    var effect = FrameEffect.Play("TailSweep", tailSweepFrames, 0.05f, 6, null, rb.position);
                    if (effect != null)
                        effect.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    break;
            }
        }

        void UpdateSoftMode(float t)
        {
            switch (CurrentMode)
            {
                case Mode.TailWarn:
                    if (t >= tailWarnTime)
                        SetMode(Mode.TailSweep);
                    break;

                case Mode.TailSweep:
                    if (!tailHit && t >= tailHitDelay)
                    {
                        tailHit = true;
                        TryTail();
                        // La estela venenosa se queda donde barrió, aunque él siga andando.
                        if (trailTime > 0f && tailSweepFrames.Length > 0)
                            EurypteridVenomTrail.Spawn(transform.parent, rb.position, -Heading,
                                tailSweepFrames[Mathf.Min(2, tailSweepFrames.Length - 1)], tailRadius, trailTime);
                    }
                    if (t >= tailSweepTime)
                    {
                        nextTail = Time.time + tailCooldown;
                        SetMode(Mode.Walk);
                    }
                    break;
            }
        }

        void TryTail()
        {
            if (player == null || !InTailSweep(player.position))
                return;
            var health = player.GetComponent<Health>();
            if (health == null)
                return;
            var reflector = player.GetComponentInParent<IHitReflector>();
            if (reflector != null && reflector.TryReflect(Health))
                return;
            health.TakeDamage(tailDamage);
        }
    }
}

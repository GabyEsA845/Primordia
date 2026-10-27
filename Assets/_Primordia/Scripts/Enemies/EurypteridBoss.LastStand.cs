using System.Collections;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Último aliento del Euriptérido: cuando su barra llega a 0 en la fase blanda no muere. Escena (franjas de cine, se salta
    /// con E): va tambaleándose al centro de la sala, cae rendido y jadea; se levanta rugiendo, la barra se rellena y la
    /// cámara se aleja hasta ver la sala entera. Luego, inmune y quieto en el centro, suelta un bullet hell de unos 25 s en
    /// tres tramos mientras su barra se vacía sola; al llegar a 0 muere de verdad. Las piedras del techo siguen cayendo.
    /// <list type="bullet">
    /// <item>Lluvia de veneno: anillos de gotas con un hueco por donde pasar, que gira de un anillo al siguiente.</item>
    /// <item>Remolino de cola: gira sobre sí mismo y lanza brazos de gotas en los dos sentidos.</item>
    /// <item>Derrumbe final: abanicos apuntados al jugador, anillos más espaciados y más piedras del techo.</item>
    /// </list>
    /// Las gotas (<see cref="EurypteridSpine"/> con otro dibujo) quitan medio corazón y ralentizan; las paran las rocas y la
    /// muda vacía, que se va gastando.
    /// </summary>
    public partial class EurypteridBoss
    {
        public const string DownedState = "Downed";
        public const string DownedIdleState = "Downed_Idle";
        public const string RiseState = "Rise";
        public const string FuryState = "Fury";

        [System.Serializable]
        public struct BarrageSegment
        {
            [Min(0.1f)] public float duration;
            [Tooltip("Segundos entre anillos (0 = sin anillos), gotas por anillo, las que faltan seguidas (el hueco) y cuánto gira el hueco.")]
            [Min(0f)] public float ringInterval;
            [Min(1)] public int ringCount;
            [Min(0)] public int ringGap;
            public float ringGapStep;
            [Tooltip("Brazos del remolino en cada sentido (0 = sin remolino), segundos entre disparos y grados que giran.")]
            [Min(0)] public int spiralArms;
            [Min(0.02f)] public float spiralInterval;
            public float spiralStep;
            [Tooltip("Segundos entre abanicos apuntados (0 = sin abanicos), gotas por abanico y grados entre la primera y la última.")]
            [Min(0f)] public float fanInterval;
            [Min(1)] public int fanCount;
            [Min(0f)] public float fanSpread;
            [Tooltip("Segundos entre tandas de piedras extra (0 = solo las del derrumbe).")]
            [Min(0f)] public float rockInterval;
            [Min(0.1f)] public float speed;
        }

        [Header("Último aliento")]
        [SerializeField] EurypteridSpine venomPrefab;
        [Tooltip("Segundos como mucho que tarda en llegar tambaleándose al centro.")]
        [SerializeField, Min(0f)] float staggerTime = 2.5f;
        [Tooltip("Segundos rendido en el suelo, jadeando.")]
        [SerializeField, Min(0f)] float downedTime = 2f;
        [Tooltip("Segundos en que la barra se rellena al levantarse.")]
        [SerializeField, Min(0.01f)] float refillTime = 0.8f;
        [Tooltip("Segundos que tarda la cámara en alejarse hasta ver la sala, y unidades de margen alrededor.")]
        [SerializeField, Min(0.01f)] float zoomTime = 1f;
        [SerializeField, Min(0f)] float zoomMargin = 1f;
        [SerializeField] BarrageSegment[] barrage =
        {
            new() { duration = 8f, ringInterval = 0.95f, ringCount = 26, ringGap = 4, ringGapStep = 37f, spiralInterval = 0.2f,
                    fanCount = 5, speed = 4.6f },
            new() { duration = 8f, spiralArms = 3, spiralInterval = 0.17f, spiralStep = 11f, ringCount = 1, fanInterval = 2.5f,
                    fanCount = 3, fanSpread = 24f, speed = 5f },
            new() { duration = 9f, ringInterval = 1.7f, ringCount = 24, ringGap = 4, ringGapStep = -47f, spiralInterval = 0.2f,
                    fanInterval = 1.1f, fanCount = 7, fanSpread = 50f, rockInterval = 1.2f, speed = 5.5f },
        };

        Coroutine lastStand;
        Vector2 arenaSize = new(30f, 22f);
        CameraZoom zoom;

        /// <summary>Tramo del bullet hell en curso (desde 0); -1 si no está disparando.</summary>
        public int BarrageSegmentIndex { get; private set; } = -1;

        /// <summary>Lo que dura el bullet hell entero (la suma de los tramos).</summary>
        public float BarrageDuration
        {
            get
            {
                float total = 0f;
                foreach (var segment in barrage)
                    total += segment.duration;
                return total;
            }
        }

        internal void ConfigureLastStand(EurypteridSpine newVenom) => venomPrefab = newVenom;

        /// <summary>Ritmos del último aliento (pruebas): tambaleo, suelo, relleno y tramos del bullet hell.</summary>
        internal void SetLastStandTimings(float newStagger, float newDowned, float newRefill, BarrageSegment[] newBarrage)
        {
            staggerTime = newStagger;
            downedTime = newDowned;
            refillTime = Mathf.Max(0.01f, newRefill);
            if (newBarrage != null)
                barrage = newBarrage;
        }

        void BeginLastStand()
        {
            if (lastStand != null)
                return;
            lastStand = StartCoroutine(LastStandScene());
        }

        IEnumerator LastStandScene()
        {
            CurrentStage = Stage.LastStand;
            EurypteridSpine.BreakAll();
            Combat.Projectile.DespawnAll();
            skipRequested = false;

            var manager = GameManager.Instance;
            if (manager != null)
            {
                manager.SetState(GameState.Cinematic);
                startedCinematic = true;
            }
            float sceneLength = staggerTime + downedTime + 1f + refillTime;
            if (player != null && player.TryGetComponent<Health>(out var playerHealth))
                playerHealth.MakeInvulnerable(sceneLength + 0.5f);
            cameraTarget = FindAnyObjectByType<CameraLookAhead>();
            overlay = FindAnyObjectByType<UI.CinematicOverlay>();
            if (overlay != null)
                overlay.SetSkipHint(true, skipText);
            interactAction ??= UnityEngine.InputSystem.InputSystem.actions?.FindAction("Player/Interact");

            // 1. Va tambaleándose al centro de la sala (más despacio, a trompicones).
            SetMode(Mode.Downed);
            PlayState(WalkSoftState, restart: true);
            if (animator != null)
                animator.speed = 0.5f;
            Vector2 center = ArenaCenter;
            for (float t = 0f; t < staggerTime && !skipRequested && Vector2.Distance(rb.position, center) > 0.3f; t += Time.deltaTime)
            {
                SkipCheck(t);
                var toCenter = center - rb.position;
                Heading = ((Vector2)Vector3.RotateTowards(Heading, toCenter.normalized, 4f * Time.deltaTime, 0f)).normalized;
                float wobble = 0.6f + 0.4f * Mathf.Sin(t * 9f);
                var step = Vector2.ClampMagnitude(toCenter, walkSpeed * 0.7f * wobble * Time.deltaTime);
                var target = rb.position + step.normalized * Clearance(step.normalized, step.magnitude);
                rb.position = target;
                transform.position = target;
                if (cameraTarget != null)
                    cameraTarget.SetFocus(target, 0.6f);
                yield return null;
            }
            if (skipRequested && Pits.IsSafe(center, pitMargin))
            {
                rb.position = center;
                transform.position = center;
            }
            if (animator != null)
                animator.speed = 1f;

            // 2. Cae rendido y jadea.
            PlayState(DownedState, restart: true);
            ScreenShake.Pulse(2f, 0.4f);
            yield return Wait(StateLength(DownedState, 0.96f));
            PlayState(DownedIdleState, restart: true);
            yield return Wait(downedTime);

            // 3. Se levanta rugiendo: la cueva retumba y la barra se rellena entera.
            SetMode(Mode.Rise);
            ScreenShake.Pulse(4f, 1f);
            float rise = StateLength(RiseState, 0.88f);
            int missing = Health.Max - Health.Current;
            int healed = 0;
            for (float t = 0f; t < Mathf.Max(rise, refillTime); t += Time.deltaTime)
            {
                int target = Mathf.RoundToInt(missing * Mathf.Clamp01(t / refillTime));
                if (target > healed)
                {
                    Health.Heal(target - healed);
                    healed = target;
                }
                if (skipRequested)
                    break;
                SkipCheck(t + 1f);
                yield return null;
            }
            Health.Heal(Health.Max);

            // 4. Vuelve el juego; la cámara se aleja hasta ver la sala entera y empieza el bullet hell.
            if (overlay != null)
                overlay.SetSkipHint(false);
            if (startedCinematic && GameManager.Instance != null && GameManager.Instance.State == GameState.Cinematic)
                GameManager.Instance.SetState(GameState.Playing);
            startedCinematic = false;
            SetMode(Mode.Fury);
            yield return Barrage();

            // 5. La barra llega a 0: muere de verdad.
            BarrageSegmentIndex = -1;
            ReleaseLastStandCamera();
            lastStand = null;
            Health.Kill();
        }

        IEnumerator Barrage()
        {
            zoom = new CameraZoom(1f);
            zoom.Zoom = FitZoom(zoom.BaseSize);

            float total = BarrageDuration;
            float start = Time.time;
            float ringAngle = 0f, spiralAngle = 0f;
            int drained = 0;
            int toDrain = Health.Max - 1;
            for (int index = 0; index < barrage.Length; index++)
            {
                var segment = barrage[index];
                BarrageSegmentIndex = index;
                float segmentStart = Time.time;
                float nextRing = segmentStart, nextSpiral = segmentStart, nextFan = segmentStart + 0.5f, nextRocks = segmentStart;
                while (Time.time - segmentStart < segment.duration)
                {
                    float now = Time.time;
                    float elapsed = now - start;
                    // La cámara se aleja al empezar y se acerca en el último segundo.
                    float zoomIn = Mathf.Clamp01(elapsed / zoomTime);
                    float zoomOut = Mathf.Clamp01((total - elapsed) / zoomTime);
                    float amount = Mathf.SmoothStep(0f, 1f, Mathf.Min(zoomIn, zoomOut));
                    zoom?.Set(amount);
                    if (cameraTarget != null)
                        cameraTarget.SetFocus(ArenaCenter, amount);

                    // La barra se vacía sola hasta 1 (el último punto se lo quita al morir).
                    int shouldDrain = Mathf.RoundToInt(toDrain * Mathf.Clamp01(elapsed / total));
                    if (shouldDrain > drained)
                    {
                        Health.Drain(shouldDrain - drained);
                        drained = shouldDrain;
                    }

                    if (segment.ringInterval > 0f && now >= nextRing)
                    {
                        nextRing = now + segment.ringInterval;
                        Ring(segment.ringCount, segment.ringGap, ringAngle, segment.speed);
                        ringAngle += segment.ringGapStep;
                    }
                    if (segment.spiralArms > 0 && now >= nextSpiral)
                    {
                        nextSpiral = now + segment.spiralInterval;
                        for (int arm = 0; arm < segment.spiralArms; arm++)
                        {
                            float offset = arm * 360f / segment.spiralArms;
                            Drop(spiralAngle + offset, segment.speed);
                            Drop(-spiralAngle + offset + 180f / segment.spiralArms, segment.speed);
                        }
                        spiralAngle += segment.spiralStep;
                        // Gira sobre sí mismo con el remolino: la cola barre la sala.
                        float facing = spiralAngle * Mathf.Deg2Rad;
                        Heading = new Vector2(Mathf.Cos(facing), Mathf.Sin(facing));
                    }
                    if (segment.fanInterval > 0f && now >= nextFan)
                    {
                        nextFan = now + segment.fanInterval;
                        var aim = ToPlayer();
                        Heading = aim;
                        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                        for (int i = 0; i < segment.fanCount; i++)
                        {
                            float spread = segment.fanCount > 1 ? segment.fanSpread / (segment.fanCount - 1) : 0f;
                            Drop(baseAngle + (i - (segment.fanCount - 1) / 2f) * spread, segment.speed * 1.3f);
                        }
                    }
                    if (segment.rockInterval > 0f && now >= nextRocks && cave != null)
                    {
                        nextRocks = now + segment.rockInterval;
                        cave.DropVolley();
                    }
                    yield return null;
                }
            }
            zoom?.Set(0f);
        }

        /// <summary>Anillo de gotas con un hueco de <paramref name="gap"/> seguidas que empieza en <paramref name="gapAngle"/>.</summary>
        void Ring(int count, int gap, float gapAngle, float speed)
        {
            float step = 360f / Mathf.Max(1, count);
            for (int i = gap; i < count; i++)
                Drop(gapAngle + i * step, speed);
        }

        void Drop(float angleDegrees, float speed)
        {
            if (venomPrefab == null)
                return;
            float angle = angleDegrees * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var drop = Instantiate(venomPrefab, rb.position + direction * 1.2f, Quaternion.identity, transform.parent);
            drop.gameObject.SetActive(true);
            drop.Launch(direction * speed, Health);
        }

        float FitZoom(float baseSize)
        {
            var camera = Camera.main;
            float aspect = camera != null ? camera.aspect : 16f / 9f;
            float needed = Mathf.Max(arenaSize.y / 2f, arenaSize.x / 2f / aspect) + zoomMargin;
            return baseSize > 0f ? Mathf.Max(1f, needed / baseSize) : 1f;
        }

        float StateLength(string state, float fallback) => states != null && states.Has(state) ? states.Length(state) : fallback;

        IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !skipRequested; t += Time.deltaTime)
            {
                SkipCheck(t + 1f);
                yield return null;
            }
        }

        // Como en la presentación: no se salta en los primeros instantes, para no hacerlo sin querer.
        void SkipCheck(float elapsed)
        {
            if (elapsed > 0.2f && interactAction != null && interactAction.WasPressedThisFrame())
                skipRequested = true;
        }

        void ReleaseLastStandCamera()
        {
            zoom?.Restore();
            zoom = null;
            if (cameraTarget != null)
                cameraTarget.ClearFocus();
        }

        /// <summary>Deja todo como antes del último aliento (si se interrumpe: otra escena, reinicio).</summary>
        void EndLastStand()
        {
            if (lastStand != null)
                StopCoroutine(lastStand);
            lastStand = null;
            ReleaseLastStandCamera();
        }
    }
}

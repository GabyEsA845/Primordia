using System.Collections;
using System.Collections.Generic;
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
    /// Fases del Anomalocaris tras el acecho (GDD):
    /// - Cerco (60–25 %): alterna el acecho con espirales alrededor del jugador, de unas 8 u a 3 u en 3 s, soltando un muro
    ///   de burbujas, y acaba con el aviso y el dash.
    /// - Escena de los ojos (al 25 %, unos 3,5 s, se salta con E): franjas de cine, las plantas se apagan y solo se ven sus
    ///   ojos cruzando la sala; ruge y vuelve la luz. Es inmune y antes no baja del 25 %.
    /// - Frenesí (25–0 %), un ciclo que se repite: marca en el suelo un dash largo hacia el jugador (~1 s, menos con poca
    ///   vida), lo recorre rapidísimo y al final salta a morderle; se queda aturdido medio segundo; le persigue a toda
    ///   velocidad unos segundos mordiéndole si le alcanza; marca otro dash (más corto de ver) y lo hace; y se vuelve una
    ///   silueta de ojos brillantes, invulnerable, que ronda la sala con las plantas atenuadas. Se le puede pegar todo el
    ///   rato salvo en silueta.
    /// </summary>
    public partial class AnomalocarisBoss
    {
        [Header("Cerco (60–25 %)")]
        [Tooltip("Fracción de vida a la que empieza el cerco.")]
        [SerializeField, Range(0f, 1f)] float encircleThreshold = 0.6f;
        [Tooltip("Radio de la espiral al empezar y al acabar (unidades).")]
        [SerializeField] Vector2 circleRadius = new(8f, 3f);
        [SerializeField, Min(0.1f)] float circleTime = 3f;
        [SerializeField, Min(0f)] float circleSpeed = 10f;
        [Tooltip("Burbujas del muro: cada cuánto y cuánto duran.")]
        [SerializeField, Min(0.03f)] float wallBubbleInterval = 0.12f;
        [SerializeField, Min(0.1f)] float wallBubbleLifetime = 6f;

        [Header("Escena de los ojos (25 %)")]
        [SerializeField, Range(0f, 1f)] float eyesThreshold = 0.25f;
        [Tooltip("Segundos en que se apagan (y se vuelven a encender) las plantas.")]
        [SerializeField, Min(0.01f)] float lightsFadeTime = 0.4f;
        [Tooltip("Segundos que tardan los ojos en cruzar la sala.")]
        [SerializeField, Min(0.1f)] float eyesCrossTime = 2f;
        [SerializeField, Min(0f)] float roarTime = 0.6f;
        [Tooltip("Material sin luz: los ojos de la escena y de la silueta y las marcas del frenesí se ven en la oscuridad.")]
        [SerializeField] Material unlitMaterial;
        [SerializeField] string skipText = "[E] Saltar";

        [Header("Frenesí (25–0 %)")]
        [Tooltip("Segundos que se ve marcado el primer dash: al empezar la fase y con muy poca vida.")]
        [SerializeField] Vector2 markTime = new(1f, 0.6f);
        [Tooltip("Lo mismo para el segundo dash, tras la persecución.")]
        [SerializeField] Vector2 secondMarkTime = new(0.5f, 0.35f);
        [Tooltip("Cuánto sigue el dash más allá del jugador, y su largo máximo (unidades).")]
        [SerializeField, Min(0f)] float frenzyOvershoot = 6f;
        [SerializeField, Min(1f)] float frenzyMaxDistance = 18f;
        [SerializeField, Min(1f)] float frenzySpeed = 30f;
        [Tooltip("Salto a morder al final del primer dash: distancia (unidades) y segundos.")]
        [SerializeField, Min(0f)] float leapDistance = 3.5f;
        [SerializeField, Min(0.05f)] float leapTime = 0.22f;
        [Tooltip("Segundos aturdido tras el salto.")]
        [SerializeField, Min(0f)] float stunTime = 0.5f;
        [Tooltip("Persecución: segundos, velocidad (el jugador va a 6) y giro (grados por segundo).")]
        [SerializeField, Min(0f)] float chaseTime = 2.5f;
        [SerializeField, Min(0f)] float chaseSpeed = 7.5f;
        [SerializeField, Min(1f)] float chaseTurnRate = 300f;
        [Tooltip("Tras morder persiguiendo, segundos que se aparta antes de volver a por el jugador.")]
        [SerializeField, Min(0f)] float chaseBackoff = 0.35f;
        [Tooltip("Silueta invulnerable: segundos rondando la sala y su velocidad.")]
        [SerializeField, Min(0f)] float silhouetteTime = 3f;
        [SerializeField, Min(0f)] float silhouetteSpeed = 6f;
        [Tooltip("Radio del área marcada al final del dash (unidades).")]
        [SerializeField, Min(0.5f)] float biteAreaRadius = 2f;
        [SerializeField] Sprite[] clawPathFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] clawAreaFrames = System.Array.Empty<Sprite>();

        // Cerco
        float circleAngle;
        float circleSign = 1f;

        // Escena de los ojos y silueta
        Coroutine eyesScene;
        bool startedCinematic;
        internal bool skipRequested;
        Material normalMaterial;
        CameraLookAhead cameraTarget;
        CinematicOverlay overlay;
        RoomDarkness darkness;
        InputAction interactAction;

        // Frenesí
        AnomalocarisMarkers markers;
        readonly List<Vector2> frenzyPath = new();
        // 0 el primer dash del ciclo (acaba en salto), 1 el segundo (acaba en silueta).
        int frenzyDash;
        float markDuration;
        bool frenzyHit;
        float backoffUntil;

        // Margen con las paredes de los puntos del frenesí, del cerco y de la escena, respecto al óvalo de la plantilla
        // (que incluye las paredes): unas 2 u de suelo libre.
        const float PathMargin = 3.5f;
        const float PlantsDimTime = 0.4f;

        /// <summary>Vida a la que llega la escena de los ojos (antes no baja de ahí).</summary>
        public int EyesHealth => Mathf.Max(1, Mathf.CeilToInt(Health.Max * eyesThreshold));

        /// <summary>Dash marcado ahora mismo: de dónde sale y adónde llega (vacío si no hay marcas).</summary>
        public IReadOnlyList<Vector2> FrenzyPath => frenzyPath;

        /// <summary>Qué dash del ciclo del frenesí toca: 0 el primero, 1 el segundo.</summary>
        public int FrenzyDash => frenzyDash;

        /// <summary>Segundos que se ve marcado el primer dash del frenesí con la vida que le queda.</summary>
        public float MarkDuration => ByHealth(markTime);

        /// <summary>Segundos que se ve marcado el segundo dash.</summary>
        public float SecondMarkDuration => ByHealth(secondMarkTime);

        public AnomalocarisMarkers Markers => markers;

        /// <summary>Fase para la barra de vida: 0 acecho, 1 cerco, 2 desde la escena de los ojos (con el frenesí).</summary>
        public int PhaseIndex => CurrentStage switch
        {
            Stage.Stalk => 0,
            Stage.Encircle => 1,
            _ => 2
        };

        /// <summary>Umbrales de la barra: el cerco y la escena de los ojos.</summary>
        public float[] PhaseThresholds => phaseThresholds ??= new[] { encircleThreshold, eyesThreshold };

        float[] phaseThresholds;

        // Del primer valor (al empezar el frenesí) al segundo (casi muerto).
        float ByHealth(Vector2 range)
        {
            float ratio = Mathf.Clamp01((float)Health.Current / EyesHealth);
            return Mathf.Lerp(range.y, range.x, ratio);
        }

        internal void ConfigurePhases(Material newUnlit, Sprite[] newPathFrames, Sprite[] newAreaFrames)
        {
            unlitMaterial = newUnlit;
            clawPathFrames = newPathFrames ?? System.Array.Empty<Sprite>();
            clawAreaFrames = newAreaFrames ?? System.Array.Empty<Sprite>();
        }

        /// <summary>Pasa directamente a una fase (pruebas).</summary>
        internal void ForceStage(Stage stage) => CurrentStage = stage;

        // ---------- Vida y fases ----------

        int ModifyDamage(Health target, int amount)
        {
            if (target != Health)
                return amount;
            // Antes de la escena de los ojos no baja del 25 %: la escena siempre se ve.
            if (CurrentStage < Stage.Eyes)
                return Mathf.Min(amount, Mathf.Max(0, Health.Current - EyesHealth));
            return amount;
        }

        void UpdateStage()
        {
            if (Health.IsDead || CurrentMode == Mode.Idle)
                return;
            // Por si otro modificador de daño (los beneficios de los linajes) lo dejó por debajo antes de tiempo.
            if (CurrentStage < Stage.Eyes && Health.Current < EyesHealth)
                Health.Heal(EyesHealth - Health.Current);

            if (CurrentStage < Stage.Eyes && Health.Current <= EyesHealth)
                eyesScene = StartCoroutine(EyesScene());
            else if (CurrentStage == Stage.Stalk && Health.Current <= Health.Max * encircleThreshold)
                CurrentStage = Stage.Encircle;
        }

        void BeginPhaseMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.Circle:
                    if (player != null)
                    {
                        var offset = rb.position - (Vector2)player.position;
                        circleAngle = Mathf.Atan2(offset.y, offset.x);
                    }
                    circleSign = Random.value < 0.5f ? -1f : 1f;
                    break;
                case Mode.Charge:
                    MarkDash();
                    break;
                case Mode.Frenzy:
                    ClearMarkers();
                    frenzyHit = false;
                    break;
                case Mode.Leap:
                    Heading = ToPlayer();
                    frenzyHit = false;
                    StartClaw();
                    break;
                case Mode.Chase:
                    backoffUntil = 0f;
                    break;
                case Mode.Silhouette:
                    SetShadowForm(true);
                    if (Darkness != null)
                        Darkness.SetPlantsDimmed(true, PlantsDimTime);
                    break;
            }
        }

        void UpdatePhaseMode(float dt, float t)
        {
            switch (CurrentMode)
            {
                case Mode.Circle:
                    UpdateCircle(dt, t);
                    break;
                case Mode.Charge:
                    // Quieto, mirando hacia donde se lanzará, mientras se ve la marca.
                    if (t >= markDuration)
                        SetMode(Mode.Frenzy);
                    break;
                case Mode.Frenzy:
                    UpdateFrenzyDash(dt);
                    break;
                case Mode.Leap:
                    Move(Heading * (leapDistance / leapTime) * dt);
                    TryFrenzyBite();
                    if (t >= leapTime)
                        SetMode(Mode.Stun);
                    break;
                case Mode.Stun:
                    if (t >= stunTime)
                        SetMode(Mode.Chase);
                    break;
                case Mode.Chase:
                    UpdateChase(dt, t);
                    break;
                case Mode.Silhouette:
                    Steer(OrbitDirection(), turnRate, dt);
                    Move(Heading * silhouetteSpeed * dt);
                    DropBubbles();
                    if (t >= silhouetteTime)
                    {
                        SetShadowForm(false);
                        if (Darkness != null)
                            Darkness.SetPlantsDimmed(false, PlantsDimTime);
                        frenzyDash = 0;
                        SetMode(Mode.Charge);
                    }
                    break;
            }
        }

        RoomDarkness Darkness => darkness != null ? darkness : darkness = GetComponentInParent<RoomDarkness>();

        // ---------- Cerco ----------

        void UpdateCircle(float dt, float t)
        {
            if (player == null)
            {
                SetMode(Mode.Swim);
                return;
            }
            float k = Mathf.Clamp01(t / circleTime);
            float radius = Mathf.Lerp(circleRadius.x, circleRadius.y, k);
            circleAngle += circleSign * circleSpeed / Mathf.Max(1f, radius) * dt;
            var goal = InsideArena((Vector2)player.position + new Vector2(Mathf.Cos(circleAngle), Mathf.Sin(circleAngle)) * radius, PathMargin);
            var toGoal = goal - rb.position;
            if (toGoal.sqrMagnitude > 0.0001f)
            {
                Steer(toGoal.normalized, 540f, dt);
                Move(toGoal.normalized * Mathf.Min(toGoal.magnitude, circleSpeed * 1.3f * dt));
            }
            DropBubbles(wallBubbleInterval, wallBubbleLifetime, 0.1f);
            // Termina el cerco con el zarpazo de siempre.
            if (t >= circleTime)
                SetMode(Mode.Warn);
        }

        // ---------- Escena de los ojos ----------

        IEnumerator EyesScene()
        {
            CurrentStage = Stage.Eyes;
            SetMode(Mode.Eyes);
            ClearMarkers();
            AnomalocarisBubble.PopAll();
            Projectile.DespawnAll();
            skipRequested = false;

            float length = lightsFadeTime * 2f + eyesCrossTime + roarTime;
            var manager = GameManager.Instance;
            if (manager != null)
            {
                manager.SetState(GameState.Cinematic);
                startedCinematic = true;
            }
            if (player != null && player.TryGetComponent<Health>(out var playerHealth))
                playerHealth.MakeInvulnerable(length + 0.5f);

            cameraTarget = FindAnyObjectByType<CameraLookAhead>();
            overlay = FindAnyObjectByType<CinematicOverlay>();
            if (overlay != null)
                overlay.SetSkipHint(true, skipText);
            interactAction ??= InputSystem.actions?.FindAction("Player/Interact");

            // 1. Se apagan las plantas; de él solo quedan los ojos (se ven sin luz). Es inmune.
            if (Darkness != null)
                Darkness.SetPlantsLit(false, lightsFadeTime);
            SetShadowForm(true);
            yield return Wait(lightsFadeTime);

            // 2. Los ojos cruzan la sala por el centro.
            Vector2 from = rb.position;
            Vector2 center = ArenaCenter;
            var across = center - from;
            across = across.sqrMagnitude > 0.01f ? across.normalized : Heading;
            Vector2 to = InsideArena(center + across * Mathf.Min(arenaSize.x, arenaSize.y) * 0.3f, PathMargin);
            Heading = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : Heading;
            for (float t = 0f; t < eyesCrossTime && !skipRequested; t += Time.deltaTime)
            {
                SkipCheck(t);
                var position = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / eyesCrossTime));
                rb.position = position;
                transform.position = position;
                if (cameraTarget != null)
                    cameraTarget.SetFocus(position, 0.5f);
                yield return null;
            }
            rb.position = to;
            transform.position = to;

            // 3. Ruge temblando (tiembla entero: el cuerpo ya lo mueve el vaivén de nado).
            for (float t = 0f; t < roarTime && !skipRequested; t += Time.deltaTime)
            {
                SkipCheck(t + 1f);
                var shake = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f));
                rb.position = to + shake;
                transform.position = to + shake;
                yield return null;
            }
            rb.position = to;
            transform.position = to;

            // 4. Vuelve la luz y empieza el frenesí, con el primer dash marcado.
            if (Darkness != null)
                Darkness.SetPlantsLit(true, skipRequested ? 0f : lightsFadeTime);
            if (!skipRequested)
                yield return Wait(lightsFadeTime);
            eyesScene = null;
            EndEyesScene();
            CurrentStage = Stage.Frenzy;
            frenzyDash = 0;
            SetMode(Mode.Charge);
        }

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

        /// <summary>Deja todo como antes de la escena o de la silueta (también si se interrumpen).</summary>
        void EndEyesScene()
        {
            if (eyesScene != null)
                StopCoroutine(eyesScene);
            eyesScene = null;
            SetShadowForm(false);
            if (darkness != null)
            {
                darkness.SetPlantsLit(true, 0f);
                darkness.SetPlantsDimmed(false, 0f);
            }
            if (cameraTarget != null)
                cameraTarget.ClearFocus();
            if (overlay != null)
                overlay.SetSkipHint(false);
            if (startedCinematic && GameManager.Instance != null && GameManager.Instance.State == GameState.Cinematic)
                GameManager.Instance.SetState(GameState.Playing);
            startedCinematic = false;
        }

        /// <summary>Silueta de ojos brillantes (escena y frenesí): material sin luz e inmune; false lo deja como estaba.</summary>
        void SetShadowForm(bool on)
        {
            Health.IsInvulnerable = on;
            if (sprite == null)
                return;
            if (on && unlitMaterial != null && normalMaterial == null)
            {
                normalMaterial = sprite.sharedMaterial;
                sprite.sharedMaterial = unlitMaterial;
            }
            else if (!on && normalMaterial != null)
            {
                sprite.sharedMaterial = normalMaterial;
                normalMaterial = null;
            }
        }

        // ---------- Frenesí ----------

        /// <summary>Marca en el suelo el dash que toca: desde donde está, pasando por el jugador y bastante más allá.</summary>
        void MarkDash()
        {
            Vector2 start = rb.position;
            Vector2 target = player != null ? (Vector2)player.position : start + Heading * frenzyOvershoot;
            var toTarget = target - start;
            var direction = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Heading;
            Heading = direction;
            float length = Mathf.Min(toTarget.magnitude + frenzyOvershoot, frenzyMaxDistance);
            var end = InsideArena(start + direction * length, PathMargin);

            frenzyPath.Clear();
            frenzyPath.Add(start);
            frenzyPath.Add(end);
            markDuration = frenzyDash == 0 ? MarkDuration : SecondMarkDuration;
            ClearMarkers();
            markers = AnomalocarisMarkers.Create(transform.parent, frenzyPath, end, biteAreaRadius, clawPathFrames,
                clawAreaFrames, unlitMaterial);
        }

        void UpdateFrenzyDash(float dt)
        {
            if (frenzyPath.Count < 2)
            {
                SetMode(Mode.Chase);
                return;
            }
            var end = frenzyPath[1];
            var toEnd = end - rb.position;
            float step = frenzySpeed * dt;
            if (toEnd.magnitude > 0.0001f)
                Heading = toEnd.normalized;
            bool arrived = toEnd.magnitude <= step;
            rb.MovePosition(arrived ? end : rb.position + Heading * step);
            TryFrenzyBite();
            if (!arrived)
                return;

            frenzyPath.Clear();
            // El primero acaba saltando a morder; el segundo, convertido en silueta.
            SetMode(frenzyDash == 0 ? Mode.Leap : Mode.Silhouette);
        }

        void UpdateChase(float dt, float t)
        {
            if (t >= chaseTime || player == null)
            {
                // Se para en seco y marca el segundo dash.
                frenzyDash = 1;
                SetMode(Mode.Charge);
                return;
            }
            if (Time.time < backoffUntil)
            {
                // Tras morder se aparta un instante.
                Move(-Heading * chaseSpeed * 0.5f * dt);
                return;
            }
            Steer(ToAim(), chaseTurnRate, dt);
            Move(Heading * chaseSpeed * dt);
            if (InClaw(player.position))
            {
                Bite();
                StartClaw();
                backoffUntil = Time.time + chaseBackoff;
            }
        }

        // Muerde una vez por dash o por salto.
        void TryFrenzyBite()
        {
            if (frenzyHit || player == null || !InClaw(player.position))
                return;
            frenzyHit = true;
            Bite();
        }

        void Bite()
        {
            var health = player.GetComponent<Health>();
            var reflector = player.GetComponentInParent<IHitReflector>();
            if (health != null && (reflector == null || !reflector.TryReflect(Health)))
                health.TakeDamage(clawDamage);
        }

        void StartClaw()
        {
            PlayState(StrikeState, restart: true);
            // Al acabar el mordisco vuelve a la animación de lo que esté haciendo (como tras la de daño).
            if (states != null && states.Has(StrikeState))
                hurtUntil = Time.time + states.Length(StrikeState);
            OneShotEffect.Spawn(swipeEffect, Head, Mathf.Atan2(Heading.y, Heading.x) * Mathf.Rad2Deg);
        }

        void ClearMarkers()
        {
            if (markers != null)
                markers.Clear();
            markers = null;
        }

        /// <summary>El punto, dentro del óvalo de la sala con ese margen a las paredes.</summary>
        Vector2 InsideArena(Vector2 point, float margin)
        {
            Vector2 center = ArenaCenter;
            float rx = Mathf.Max(1f, arenaSize.x / 2f - margin);
            float ry = Mathf.Max(1f, arenaSize.y / 2f - margin);
            var offset = point - center;
            float k = (offset.x / rx) * (offset.x / rx) + (offset.y / ry) * (offset.y / ry);
            return k <= 1f ? point : center + offset / Mathf.Sqrt(k);
        }
    }
}

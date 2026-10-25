using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Ameba gigante, segundo jefe del piso 1. Lleva la vida compartida; sus cuerpos (<see cref="AmoebaBody"/>) son
    /// hijos y se mueven solos, embistiendo en línea recta hasta chocar con una pared, donde se quedan quietos y
    /// tiemblan antes de volver a salir. No dispara hasta el final.
    /// <list type="number">
    /// <item>Calma (100–80 %): sale hacia cualquier lado, despacio. Parece inofensiva.</item>
    /// <item>Enojo (80–50 %): se enfada (animación) y desde entonces sale hacia el jugador, más rápido.</item>
    /// <item>
    /// División (50–10 %): se parte en dos amebas medianas, más rápidas, que comparten la barra de vida. Nada más
    /// nacer salen disparadas hacia lados opuestos; después una va directa al jugador y la otra le anticipa (apunta
    /// adonde estará cuando llegue).
    /// </item>
    /// <item>
    /// Final (10 %): escena (el jugador no puede hacer nada): la cámara va al centro de la sala, las medianas se
    /// juntan allí en una sola y esta se enfurece. Luego, inmune, llena la sala de proyectiles
    /// (<see cref="AmoebaBarrage"/>) con la cámara alejada para verla entera. Al acabar queda agotada en el centro y
    /// cae de uno o dos golpes.
    /// </item>
    /// </list>
    /// No le afectan los empujones ni la ceguera: no tiene <see cref="EnemyAI"/>.
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(AmoebaBarrage))]
    public class AmoebaBoss : MonoBehaviour, IBoss, IStatusAnchor
    {
        public enum Stage
        {
            Calm,
            Enraging,
            Angry,
            Splitting,
            Split,
            /// <summary>Escena: la cámara va al centro, las medianas se juntan y se enfurece.</summary>
            FinalScene,
            Barrage,
            Exhausted
        }

        // Estados de las animaciones del cuerpo grande y de las medianas.
        public const string CalmState = "Idle";
        public const string EnrageState = "Angry_Transition";
        public const string AngryState = "Angry";
        public const string HurtCalmState = "Hurt_Calm";
        public const string HurtAngryState = "Hurt_Angry";
        public const string SplitState = "Split";
        public const string MergeState = "Merge";
        public const string RageState = "Rage";
        public const string CastStatePrefix = "Cast_";
        public const string ExhaustState = "Exhaust";
        public const string TiredState = "Tired";
        public const string HurtTiredState = "Hurt_Tired";
        public const string DeathState = "Death";
        public const string IntroState = "Intro";
        public const string HalfMoveState = "Move";
        public const string HalfHurtState = "Hurt";
        /// <summary>Mirando hacia donde va a salir: Aim_0 (derecha) a Aim_7, en sentido antihorario cada 45°.</summary>
        public const string AimStatePrefix = "Aim_";
        public const string AimCalmStatePrefix = "AimCalm_";

        [SerializeField] string displayName = "Ameba gigante";

        [Header("Cuerpos")]
        [SerializeField] AmoebaBody mainBody;
        [Tooltip("Las dos amebas medianas en que se divide. Empiezan desactivadas.")]
        [SerializeField] AmoebaBody[] halves = Array.Empty<AmoebaBody>();

        [Header("Fases (fracción de vida)")]
        [SerializeField, Range(0f, 1f)] float angryThreshold = 0.8f;
        [SerializeField, Range(0f, 1f)] float splitThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] float finalThreshold = 0.1f;

        [Header("Embestidas")]
        [SerializeField, Min(0f)] float calmSpeed = 1.2f;
        [Tooltip("Enfadada: el triple que en calma, tan rápida como el jugador.")]
        [SerializeField, Min(0f)] float angrySpeed = 6f;
        [SerializeField, Min(0f)] float halfSpeed = 10f;
        [Tooltip("Segundos quieta tras chocar con la pared.")]
        [SerializeField, Min(0f)] float calmRest = 1.5f;
        [SerializeField, Min(0f)] float angryRest = 1f;
        [SerializeField, Min(0f)] float halfRest = 1f;
        [Tooltip("Últimos segundos de cada parada en los que avisa de que va a salir: tiembla y mira hacia donde irá. "
            + "La dirección queda fijada al final del aviso.")]
        [SerializeField, Min(0f)] float shakeTime = 0.6f;
        [Tooltip("Últimos segundos del aviso en los que se encoge para coger impulso.")]
        [SerializeField, Min(0f)] float windupTime = 0.15f;
        [Tooltip("Cuánto se encoge al coger impulso (0,2 = un 20 %).")]
        [SerializeField, Range(0f, 0.5f)] float windupSquash = 0.2f;
        [Tooltip("Cuánto se estira mientras embiste.")]
        [SerializeField, Range(0f, 0.5f)] float chargeStretch = 0.12f;
        [Tooltip("Cuánto tiembla, en unidades.")]
        [SerializeField, Min(0f)] float shakeAmount = 0.12f;
        [Tooltip("En calma no sale hacia una pared más cercana que esto, si hay otra salida.")]
        [SerializeField, Min(0f)] float minCalmRun = 4f;
        [Tooltip("Segundos hasta la primera embestida al entrar el jugador (el aviso va incluido).")]
        [SerializeField, Min(0f)] float startDelay = 0.6f;
        [Tooltip("Si roza una pared con menos de este ángulo (en grados), resbala a lo largo de ella en lugar de pararse.")]
        [SerializeField, Range(0f, 80f)] float slideAngle = 35f;
        [Tooltip("Veces que puede resbalar en una misma embestida; después se para en la siguiente pared.")]
        [SerializeField, Min(0)] int maxSlides = 3;

        [Header("División")]
        [Tooltip("Distancia de cada mediana al centro de la grande al aparecer.")]
        [SerializeField, Min(0f)] float halfSpacing = 1.4f;
        [Tooltip("Segundos de adelanto máximo con que la segunda mediana calcula dónde estará el jugador.")]
        [SerializeField, Min(0f)] float maxLead = 1.2f;
        [Tooltip("Duración del enfado y de la división si les falta la animación.")]
        [SerializeField, Min(0f)] float fallbackTransition = 1f;

        [Header("Escena final")]
        [SerializeField, Min(0.01f)] float cameraPanTime = 2f;
        [Tooltip("Segundos que tardan las medianas en llegar al centro.")]
        [SerializeField, Min(0.01f)] float mergeGlideTime = 2f;
        [Tooltip("Segundos de la fusión en el centro (animación Merge).")]
        [SerializeField, Min(0f)] float mergeTime = 1f;
        [Tooltip("Segundos de la furia antes de la lluvia de proyectiles (animación Rage).")]
        [SerializeField, Min(0f)] float rageTime = 2f;
        [Tooltip("Segundos que tarda la cámara en alejarse y en volver.")]
        [SerializeField, Min(0.01f)] float zoomTime = 1f;
        [Tooltip("Margen alrededor de la sala con la cámara alejada, en unidades.")]
        [SerializeField, Min(0f)] float zoomMargin = 1f;
        [Tooltip("Segundos entre el final de la escena y el primer proyectil.")]
        [SerializeField, Min(0f)] float barrageGrace = 1f;

        [Tooltip("Escribe en la consola cada choque de las embestidas (para depurar).")]
        [SerializeField] bool logCharges;

        [Header("Agotada")]
        [Tooltip("Golpes que aguanta agotada, sea cual sea el daño de cada uno.")]
        [SerializeField, Min(1)] int exhaustedHits = 2;

        public string DisplayName => displayName;
        public Health Health { get; private set; }
        public Stage CurrentStage { get; private set; } = Stage.Calm;
        public AmoebaBody MainBody => mainBody;
        public AmoebaBody[] Halves => halves;
        public AmoebaBarrage Barrage => barrage;

        public BossBarStyle BarStyle => CurrentStage switch
        {
            Stage.FinalScene or Stage.Barrage => BossBarStyle.Immune,
            Stage.Exhausted => BossBarStyle.Weak,
            Stage.Calm or Stage.Enraging => BossBarStyle.Normal,
            _ => BossBarStyle.Enraged
        };

        public Vector2 ArenaCenter => arenaCenter != null ? (Vector2)arenaCenter.position : home;

        /// <summary>Vida a la que empieza la escena final (no baja de ahí antes).</summary>
        public int FinalHealth => Mathf.Max(1, Mathf.CeilToInt(Health.Max * finalThreshold));

        /// <summary>Se lanza al cambiar de fase.</summary>
        public event Action<Stage> StageChanged;

        // Lo que hace cada cuerpo entre embestidas.
        sealed class Charger
        {
            public AmoebaBody Body;
            public Vector2 Direction;
            public bool Charging;
            /// <summary>Veces que ha resbalado por una pared en esta embestida.</summary>
            public int Slides;
            /// <summary>Apunta adonde estará el jugador cuando llegue, en lugar de adonde está.</summary>
            public bool Predicts;
            /// <summary>Espera de más en la próxima parada (para que las medianas no vayan a la par).</summary>
            public float ExtraRest;
            /// <summary>True durante el aviso antes de salir.</summary>
            public bool Aiming;
            public float RestUntil;
        }

        Enemy enemy;
        AmoebaBarrage barrage;
        Transform arenaCenter;
        Vector2 arenaSize = new(30f, 30f);
        Vector2 home;
        Transform player;
        Rigidbody2D playerBody;
        Health playerHealth;
        Charger main;
        Charger[] halfChargers = Array.Empty<Charger>();
        Coroutine transition;
        readonly Dictionary<AmoebaBody, float> hurtUntil = new();
        CameraLookAhead cameraTarget;
        CameraZoom zoom;
        int exhaustedHitDamage = 1;
        bool startedCinematic;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            barrage = GetComponent<AmoebaBarrage>();
            Health = GetComponent<Health>();

            // El objeto de la vida se queda quieto en su sitio; los que se mueven son los cuerpos.
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            BuildChargers();
        }

        void BuildChargers()
        {
            main = mainBody != null ? new Charger { Body = mainBody } : null;
            halfChargers = new Charger[halves.Length];
            for (int i = 0; i < halves.Length; i++)
                halfChargers[i] = new Charger { Body = halves[i], Predicts = i % 2 == 1 };
        }

        internal void Configure(AmoebaBody newMain, AmoebaBody[] newHalves)
        {
            mainBody = newMain;
            halves = newHalves ?? Array.Empty<AmoebaBody>();
            BuildChargers();
        }

        internal void SetTimings(float newStartDelay, float newPanTime, float newGlideTime, float newMergeTime, float newRageTime,
            float newZoomTime, float newGrace)
        {
            startDelay = newStartDelay;
            cameraPanTime = Mathf.Max(0.01f, newPanTime);
            mergeGlideTime = Mathf.Max(0.01f, newGlideTime);
            mergeTime = newMergeTime;
            rageTime = newRageTime;
            zoomTime = Mathf.Max(0.01f, newZoomTime);
            barrageGrace = newGrace;
            // La primera salida se vuelve a programar con la espera nueva.
            if (main != null && !main.Charging && CurrentStage == Stage.Calm)
                main.RestUntil = Time.time + startDelay;
        }

        internal bool LogCharges
        {
            get => logCharges;
            set => logCharges = value;
        }

        internal void SetFallbackTransition(float seconds) => fallbackTransition = Mathf.Max(0f, seconds);

        internal void SetRests(float newCalmRest, float newAngryRest, float newHalfRest)
        {
            calmRest = newCalmRest;
            angryRest = newAngryRest;
            halfRest = newHalfRest;
        }

        public void SetArena(Transform center, Vector2 size)
        {
            arenaCenter = center;
            arenaSize = size;
        }

        public void PlayIntroPose(float hold)
        {
            // Durante la presentación la ameba no actúa (no es su escena), así que nadie le cambia la animación.
            if (mainBody != null && mainBody.States.Has(IntroState))
                mainBody.Play(IntroState, restart: true);
        }

        public void BeginFight()
        {
            if (mainBody != null)
                mainBody.Play(IdleStateFor(CurrentStage));
            if (main != null)
                main.RestUntil = Time.time + startDelay;
        }

        void OnEnable()
        {
            // Se activa cuando el jugador entra en su sala (la sala oculta a sus enemigos hasta entonces).
            ActiveBoss.Set(this);
            home = transform.position;
            FindPlayer();
            Health.AddDamageModifier(ModifyDamage);
            Health.Damaged += OnDamaged;
            Health.Deflected += OnDeflected;
            enemy.Died += OnDied;
            barrage.SegmentStarted += OnSegmentStarted;
            barrage.Finished += OnBarrageFinished;

            if (main != null)
            {
                main.RestUntil = Time.time + startDelay;
                main.Charging = false;
            }
        }

        void OnDisable()
        {
            ActiveBoss.Clear(this);
            Health.RemoveDamageModifier(ModifyDamage);
            Health.Damaged -= OnDamaged;
            Health.Deflected -= OnDeflected;
            enemy.Died -= OnDied;
            Sfx.StopLoop(Sounds.AmoebaExhaustedLoop);
            barrage.SegmentStarted -= OnSegmentStarted;
            barrage.Finished -= OnBarrageFinished;
            // Si se va a mitad de la escena (otra escena, reinicio), la cámara y el juego vuelven a la normalidad.
            ReleaseCamera();
            if (startedCinematic && GameManager.Instance != null && GameManager.Instance.State == GameState.Cinematic)
                GameManager.Instance.SetState(GameState.Playing);
            startedCinematic = false;
        }

        void FindPlayer()
        {
            if (player != null)
                return;
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
                return;
            player = playerObject.transform;
            playerBody = playerObject.GetComponent<Rigidbody2D>();
            playerHealth = playerObject.GetComponent<Health>();
        }

        // ---------- Vida ----------

        int ModifyDamage(Health target, int amount)
        {
            if (target != Health)
                return amount;

            // Antes de la escena final no baja del 10 %: la escena siempre se ve.
            if (CurrentStage < Stage.FinalScene)
                return Mathf.Min(amount, Mathf.Max(0, Health.Current - FinalHealth));

            // Agotada cae de pocos golpes, aunque sean flojos.
            if (CurrentStage == Stage.Exhausted)
                return Mathf.Max(amount, exhaustedHitDamage);

            return amount;
        }

        void OnDamaged(int amount)
        {
            string state = CurrentStage switch
            {
                Stage.Calm => HurtCalmState,
                Stage.Angry => HurtAngryState,
                Stage.Exhausted => HurtTiredState,
                _ => null
            };

            // Con la vida compartida, las dos medianas acusan el golpe a la vez.
            if (CurrentStage == Stage.Split)
            {
                foreach (var half in halves)
                    PlayHurt(half, HalfHurtState);
            }
            else if (state != null && mainBody != null)
            {
                PlayHurt(mainBody, state);
            }
        }

        // Inmune (escena final y lluvia de proyectiles): el golpe rebota.
        void OnDeflected() => Sfx.Play(Sounds.AmoebaImmune);

        void PlayHurt(AmoebaBody body, string hurt)
        {
            if (body == null || !body.gameObject.activeInHierarchy)
                return;

            // Avisando o embistiendo, la animación de daño taparía hacia dónde va: solo un destello.
            var charger = ChargerOf(body);
            if (charger != null && (charger.Aiming || charger.Charging))
            {
                body.Flash();
                return;
            }

            if (!body.States.Has(hurt))
                return;
            body.Play(hurt, restart: true);
            hurtUntil[body] = Time.time + body.States.Length(hurt);
        }

        static string IdleStateFor(Stage stage) => stage switch
        {
            Stage.Calm => CalmState,
            Stage.Exhausted => TiredState,
            _ => AngryState
        };

        void OnDied(Enemy _)
        {
            StopTransition();
            barrage.Stop();
            Sfx.StopLoop(Sounds.AmoebaExhaustedLoop);
            ReleaseCamera();
            foreach (var body in AllBodies())
            {
                body.Shake(0f);
                body.GetComponent<Collider2D>().enabled = false;
            }
            if (mainBody != null)
            {
                mainBody.gameObject.SetActive(true);
                mainBody.Play(DeathState, restart: true);
            }
        }

        // ---------- Fases ----------

        void Update()
        {
            if (Health.IsDead || !CanAct())
                return;

            // Por si otro modificador de daño (los beneficios de los linajes) la dejó por debajo del 10 % antes de tiempo.
            if (CurrentStage < Stage.FinalScene && Health.Current < FinalHealth)
                Health.Heal(FinalHealth - Health.Current);

            float ratio = (float)Health.Current / Health.Max;
            if (CurrentStage < Stage.FinalScene && Health.Current <= FinalHealth)
                Begin(Stage.FinalScene, FinalScene());
            else if (CurrentStage < Stage.Splitting && ratio <= splitThreshold)
                Begin(Stage.Splitting, Split());
            else if (CurrentStage == Stage.Calm && ratio <= angryThreshold)
                Begin(Stage.Enraging, Enrage());

            ReturnFromHurt();
        }

        void ReturnFromHurt()
        {
            foreach (var body in AllBodies())
            {
                if (!hurtUntil.TryGetValue(body, out float until) || Time.time < until)
                    continue;
                hurtUntil.Remove(body);
                // Si ya está avisando, el aviso vuelve a poner su dibujo en el siguiente paso.
                var charger = ChargerOf(body);
                if (charger != null && charger.Aiming)
                    continue;
                if (body == mainBody && CurrentStage is Stage.Calm or Stage.Angry or Stage.Exhausted)
                    body.Play(IdleStateFor(CurrentStage));
                else if (CurrentStage == Stage.Split)
                    body.Play(HalfMoveState);
            }
        }

        void Begin(Stage stage, IEnumerator routine)
        {
            StopTransition();
            SetStage(stage);
            transition = StartCoroutine(routine);
        }

        void StopTransition()
        {
            if (transition != null)
                StopCoroutine(transition);
            transition = null;
        }

        void SetStage(Stage stage)
        {
            CurrentStage = stage;
            StageChanged?.Invoke(stage);
        }

        IEnumerator Enrage()
        {
            Stop(main);
            mainBody.Play(EnrageState, restart: true);
            Sfx.Play(Sounds.AmoebaEnrage);
            yield return new WaitForSeconds(Length(mainBody, EnrageState));

            SetStage(Stage.Angry);
            mainBody.Play(AngryState);
            // Una pausa corta, temblando, antes de la primera embestida contra el jugador.
            main.RestUntil = Time.time + shakeTime;
            transition = null;
        }

        IEnumerator Split()
        {
            Stop(main);
            mainBody.Play(SplitState, restart: true);
            Sfx.Play(Sounds.AmoebaSplit);
            yield return new WaitForSeconds(Length(mainBody, SplitState));

            // Las medianas aparecen a los lados de la grande, sin meterse en la pared.
            Vector2 center = mainBody.Position;
            mainBody.gameObject.SetActive(false);
            for (int i = 0; i < halfChargers.Length; i++)
            {
                var charger = halfChargers[i];
                Vector2 side = i % 2 == 0 ? Vector2.left : Vector2.right;
                charger.Body.gameObject.SetActive(true);
                charger.Body.Teleport(center);
                float free = charger.Body.FreeDistance(side, halfSpacing);
                charger.Body.Teleport(center + side * Mathf.Max(0f, free - 0.05f));
                charger.Body.Play(HalfMoveState, restart: true);
                // Nada más nacer salen disparadas hacia lados opuestos, sin aviso: empiezan a cazar lejos una de otra.
                charger.Aiming = false;
                charger.Charging = true;
                charger.Slides = 0;
                charger.Direction = side;
                // Llegan a la pared a la vez: la segunda espera un poco más, y así no van a la par.
                charger.ExtraRest = i * 0.45f;
            }

            SetStage(Stage.Split);
            transition = null;
        }

        IEnumerator FinalScene()
        {
            startedCinematic = true;
            var manager = GameManager.Instance;
            if (manager != null)
                manager.SetState(GameState.Cinematic);

            Projectile.DespawnAll();
            Health.IsInvulnerable = true;
            float sceneLength = cameraPanTime + mergeGlideTime + mergeTime + rageTime;
            FindPlayer();
            if (playerHealth != null)
                playerHealth.MakeInvulnerable(sceneLength + barrageGrace);

            foreach (var charger in Chargers())
                Stop(charger);

            // 1. La cámara va despacio al centro de la sala.
            cameraTarget = FindAnyObjectByType<CameraLookAhead>();
            Vector2 center = ArenaCenter;
            for (float t = 0f; t < cameraPanTime; t += Time.deltaTime)
            {
                if (cameraTarget != null)
                    cameraTarget.SetFocus(center, Mathf.SmoothStep(0f, 1f, t / cameraPanTime));
                yield return null;
            }
            if (cameraTarget != null)
                cameraTarget.SetFocus(center, 1f);

            // 2. Las medianas (o la grande, si no llegó a dividirse) van al centro y se funden.
            var moving = new List<(AmoebaBody body, Vector2 from)>();
            foreach (var body in AllBodies())
            {
                if (body.gameObject.activeSelf)
                {
                    body.Shake(0f);
                    moving.Add((body, body.Position));
                }
            }
            bool wasSplit = moving.Exists(m => m.body != mainBody);

            for (float t = 0f; t < mergeGlideTime; t += Time.fixedDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / mergeGlideTime);
                for (int i = 0; i < moving.Count; i++)
                {
                    // Cada mediana se queda a un lado del centro: así la fusión empieza con las dos tocándose.
                    Vector2 goal = center;
                    if (wasSplit && moving.Count > 1)
                        goal += (i % 2 == 0 ? Vector2.left : Vector2.right) * moving[i].body.Radius * 0.8f;
                    moving[i].body.MoveTo(Vector2.Lerp(moving[i].from, goal, k));
                }
                yield return new WaitForFixedUpdate();
            }

            foreach (var half in halves)
                half.gameObject.SetActive(false);
            mainBody.gameObject.SetActive(true);
            mainBody.Teleport(center);
            if (wasSplit)
            {
                mainBody.Play(MergeState, restart: true);
                Sfx.Play(Sounds.AmoebaMerge);
                yield return new WaitForSeconds(mergeTime);
            }

            // 3. Se enfurece.
            mainBody.Play(RageState, restart: true);
            mainBody.Shake(shakeAmount * 0.6f);
            Sfx.Play(Sounds.AmoebaFury);
            yield return new WaitForSeconds(rageTime);
            mainBody.Shake(0f);

            // 4. Vuelve el juego, la cámara se aleja hasta ver la sala entera y empieza la lluvia de proyectiles.
            SetStage(Stage.Barrage);
            mainBody.Play(CastStatePrefix + 1);
            if (manager != null && manager.State == GameState.Cinematic)
                manager.SetState(GameState.Playing);
            startedCinematic = false;

            zoom = new CameraZoom(1f);
            zoom.Zoom = FitZoom(zoom.BaseSize);
            for (float t = 0f; t < zoomTime; t += Time.deltaTime)
            {
                zoom.Set(Mathf.SmoothStep(0f, 1f, t / zoomTime));
                yield return null;
            }
            zoom.Set(1f);

            yield return new WaitForSeconds(Mathf.Max(0f, barrageGrace - zoomTime));
            // Su pista dura lo mismo que la lluvia (30 s) y acaba en seco cuando queda agotada.
            Music.Play(Sounds.MusicBossAmoebaFinal, 0.1f);
            barrage.Begin(player);
            transition = null;
        }

        /// <summary>Zoom con el que cabe la sala entera (más el margen) en pantalla.</summary>
        float FitZoom(float baseSize)
        {
            var camera = Camera.main;
            float aspect = camera != null ? camera.aspect : 16f / 9f;
            float needed = Mathf.Max(arenaSize.y / 2f, arenaSize.x / 2f / aspect) + zoomMargin;
            return baseSize > 0f ? Mathf.Max(1f, needed / baseSize) : 1f;
        }

        void OnSegmentStarted(int index)
        {
            string state = CastStatePrefix + (index + 1);
            if (mainBody != null && mainBody.States.Has(state))
                mainBody.Play(state, restart: true);
        }

        void OnBarrageFinished()
        {
            if (Health.IsDead)
                return;
            Begin(Stage.Exhausted, Exhaust());
        }

        IEnumerator Exhaust()
        {
            Health.IsInvulnerable = false;
            exhaustedHitDamage = Mathf.Max(1, Mathf.CeilToInt(Health.Current / (float)exhaustedHits));
            mainBody.ContactDamage = 0;

            mainBody.Play(ExhaustState, restart: true);
            Sfx.StartLoop(Sounds.AmoebaExhaustedLoop);

            // La cámara vuelve al jugador y a su tamaño.
            for (float t = 0f; t < zoomTime; t += Time.deltaTime)
            {
                float k = 1f - Mathf.SmoothStep(0f, 1f, t / zoomTime);
                zoom?.Set(k);
                if (cameraTarget != null)
                    cameraTarget.SetFocus(ArenaCenter, k);
                yield return null;
            }
            ReleaseCamera();

            float rest = Length(mainBody, ExhaustState) - zoomTime;
            if (rest > 0f)
                yield return new WaitForSeconds(rest);
            mainBody.Play(TiredState);
            transition = null;
        }

        void ReleaseCamera()
        {
            zoom?.Restore();
            zoom = null;
            if (cameraTarget != null)
                cameraTarget.ClearFocus();
        }

        // ---------- Embestidas ----------

        void FixedUpdate()
        {
            if (Health.IsDead || !CanAct())
                return;

            switch (CurrentStage)
            {
                case Stage.Calm:
                    Step(main, calmSpeed, calmRest, towardsPlayer: false);
                    break;
                case Stage.Angry:
                    Step(main, angrySpeed, angryRest, towardsPlayer: true);
                    break;
                case Stage.Split:
                    foreach (var charger in halfChargers)
                        Step(charger, halfSpeed, halfRest, towardsPlayer: true);
                    BounceHalves();
                    break;
            }
        }

        void Step(Charger charger, float speed, float rest, bool towardsPlayer)
        {
            if (charger == null || charger.Body == null || !charger.Body.gameObject.activeInHierarchy)
                return;

            var body = charger.Body;
            if (!charger.Charging)
            {
                float left = charger.RestUntil - Time.time;
                if (left > shakeTime)
                    return;

                if (left > 0f)
                {
                    // Aviso: tiembla y mira hacia donde va a salir (al jugador, que puede moverse mientras tanto).
                    if (!charger.Aiming || towardsPlayer)
                        charger.Direction = towardsPlayer ? DirectionToPlayer(body, charger.Predicts, speed) : RandomDirection(body);
                    if (!charger.Aiming)
                        Sfx.Play(Sounds.AmoebaWarning);
                    charger.Aiming = true;
                    body.Shake(shakeAmount);
                    PlayAim(body, charger.Direction, calm: !towardsPlayer);
                    // Al final se encoge para coger impulso.
                    float windup = windupTime > 0f ? 1f - Mathf.Clamp01(left / windupTime) : 0f;
                    body.SetStretch(charger.Direction, -windupSquash * windup);
                    return;
                }

                // Sale disparada, ya a toda velocidad, en la dirección que miraba (o la más parecida que esté libre).
                body.ResolveOverlap();
                // Sin aviso (por ejemplo, si una espera lo saltó) no hay dirección: se elige ahora.
                if (charger.Direction.sqrMagnitude < 0.0001f)
                    charger.Direction = towardsPlayer ? DirectionToPlayer(body, charger.Predicts, speed) : RandomDirection(body);
                charger.Direction = FreeDirectionNear(body, charger.Direction);
                charger.Aiming = false;
                charger.Charging = true;
                charger.Slides = 0;
                body.Shake(0f);
                PlayMove(body);
                Sfx.Play(Sounds.AmoebaLaunch);
            }

            body.SetStretch(charger.Direction, chargeStretch);
            if (body.Move(charger.Direction * speed * Time.fixedDeltaTime, out var normal))
            {
                if (logCharges)
                    Debug.Log($"AmoebaBoss: {body.name} choca en {body.Position} yendo hacia {charger.Direction} con "
                        + $"{(body.LastHit != null ? body.LastHit.name : "nada")} (normal {normal}).", this);
                // Rozando la pared: resbala a lo largo de ella y sigue. De frente: se para y se aplasta.
                float towardsWall = -Vector2.Dot(charger.Direction, normal);
                if (charger.Slides < maxSlides && towardsWall < Mathf.Sin(slideAngle * Mathf.Deg2Rad))
                {
                    var along = charger.Direction + normal * towardsWall;
                    if (along.sqrMagnitude > 0.0001f)
                    {
                        charger.Direction = along.normalized;
                        charger.Slides++;
                        return;
                    }
                }

                charger.Charging = false;
                charger.RestUntil = Time.time + rest + charger.ExtraRest;
                charger.ExtraRest = 0f;
                body.SetStretch(charger.Direction, 0f);
                body.Impact(normal);
                Sfx.Play(Sounds.AmoebaWallSlam);
            }
        }

        /// <summary>Mira hacia esa dirección (con el dibujo de la calma o del enfado), salvo mientras acusa un golpe.</summary>
        void PlayAim(AmoebaBody body, Vector2 direction, bool calm)
        {
            if (hurtUntil.TryGetValue(body, out float until) && Time.time < until)
                return;
            string state = (calm && body == mainBody ? AimCalmStatePrefix : AimStatePrefix) + AimSector(direction);
            if (body.States.Has(state))
                body.Play(state);
        }

        void PlayMove(AmoebaBody body)
        {
            if (hurtUntil.TryGetValue(body, out float until) && Time.time < until)
                return;
            body.Play(body == mainBody ? IdleStateFor(CurrentStage) : HalfMoveState);
        }

        /// <summary>Sector de 45° de una dirección: 0 = derecha, 2 = arriba, 4 = izquierda, 6 = abajo.</summary>
        public static int AimSector(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return (Mathf.RoundToInt(angle / 45f) % 8 + 8) % 8;
        }

        /// <summary>Las medianas no se atraviesan: si se tocan, rebotan.</summary>
        void BounceHalves()
        {
            for (int i = 0; i < halfChargers.Length; i++)
            {
                for (int j = i + 1; j < halfChargers.Length; j++)
                {
                    var a = halfChargers[i];
                    var b = halfChargers[j];
                    Vector2 delta = b.Body.Position - a.Body.Position;
                    float minDistance = a.Body.Radius + b.Body.Radius;
                    if (delta.sqrMagnitude >= minDistance * minDistance)
                        continue;

                    Vector2 normal = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
                    // Solo rebota la que va hacia la otra; la que está parada sigue esperando.
                    bool bounced = false;
                    if (a.Charging && Vector2.Dot(a.Direction, normal) > 0f)
                    {
                        a.Direction = Vector2.Reflect(a.Direction, normal);
                        bounced = true;
                    }
                    if (b.Charging && Vector2.Dot(b.Direction, -normal) > 0f)
                    {
                        b.Direction = Vector2.Reflect(b.Direction, -normal);
                        bounced = true;
                    }
                    if (bounced)
                        Sfx.Play(Sounds.AmoebaBounce);
                }
            }
        }

        static void Stop(Charger charger)
        {
            if (charger == null)
                return;
            charger.Charging = false;
            charger.Aiming = false;
            charger.RestUntil = float.PositiveInfinity;
            if (charger.Body != null)
            {
                charger.Body.Shake(0f);
                charger.Body.SetStretch(Vector2.right, 0f);
            }
        }

        Vector2 DirectionToPlayer(AmoebaBody body, bool predicts = false, float speed = 0f)
        {
            FindPlayer();
            if (player == null)
                return RandomDirection(body);
            Vector2 target = player.position;
            if (predicts && playerBody != null)
                target = PredictTarget(body.Position, target, playerBody.linearVelocity, speed, maxLead);
            Vector2 toPlayer = target - body.Position;
            return toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : RandomDirection(body);
        }

        /// <summary>
        /// Dónde estará el objetivo cuando llegue alguien que sale de <paramref name="from"/> a esa velocidad, si sigue
        /// moviéndose igual; como mucho <paramref name="maxLead"/> segundos por delante.
        /// </summary>
        public static Vector2 PredictTarget(Vector2 from, Vector2 target, Vector2 velocity, float speed, float maxLead)
        {
            if (speed <= 0f || velocity.sqrMagnitude < 0.0001f)
                return target;
            // Dos pasadas: el tiempo de llegada depende de adónde se apunte.
            float lead = Mathf.Min(maxLead, Vector2.Distance(from, target) / speed);
            var predicted = target + velocity * lead;
            lead = Mathf.Min(maxLead, Vector2.Distance(from, predicted) / speed);
            return target + velocity * lead;
        }

        /// <summary>
        /// La dirección pedida si hay sitio para avanzar; si no (pegada a una pared que la tapa), la más cercana a ella
        /// que esté libre, girando poco a poco hacia los dos lados. Así no se queda atascada en las esquinas.
        /// </summary>
        static Vector2 FreeDirectionNear(AmoebaBody body, Vector2 direction)
        {
            const float needed = 0.5f;
            if (body.FreeDistance(direction, needed) >= needed)
                return direction;

            for (int step = 1; step <= 12; step++)
            {
                foreach (int side in new[] { 1, -1 })
                {
                    var candidate = (Vector2)(Quaternion.Euler(0f, 0f, side * step * 15f) * direction);
                    if (body.FreeDistance(candidate, needed) >= needed)
                        return candidate;
                }
            }
            return -direction;
        }

        /// <summary>Una dirección al azar con sitio para avanzar; si no hay ninguna larga, la más larga.</summary>
        Vector2 RandomDirection(AmoebaBody body)
        {
            Vector2 best = Vector2.right;
            float bestFree = -1f;
            for (int i = 0; i < 12; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float free = body.FreeDistance(direction, minCalmRun);
                if (free >= minCalmRun)
                    return direction;
                if (free > bestFree)
                {
                    bestFree = free;
                    best = direction;
                }
            }
            return best;
        }

        // ---------- Utilidades ----------

        Charger ChargerOf(AmoebaBody body)
        {
            foreach (var charger in Chargers())
            {
                if (charger.Body == body)
                    return charger;
            }
            return null;
        }

        bool CanAct()
        {
            if (GameManager.Instance == null)
                return true;
            var state = GameManager.Instance.State;
            return state == GameState.Playing || (state == GameState.Cinematic && startedCinematic);
        }

        float Length(AmoebaBody body, string state)
        {
            float length = body != null ? body.States.Length(state) : 0f;
            return length > 0f ? length : fallbackTransition;
        }

        IEnumerable<Charger> Chargers()
        {
            if (main != null)
                yield return main;
            foreach (var charger in halfChargers)
                yield return charger;
        }

        IEnumerable<AmoebaBody> AllBodies()
        {
            if (mainBody != null)
                yield return mainBody;
            foreach (var half in halves)
            {
                if (half != null)
                    yield return half;
            }
        }

        // Las marcas de estado (veneno…) van sobre el cuerpo que se ve: la grande o la primera mediana.
        AmoebaBody MarkerBody
        {
            get
            {
                if (mainBody != null && mainBody.gameObject.activeSelf)
                    return mainBody;
                foreach (var half in halves)
                {
                    if (half != null && half.gameObject.activeSelf)
                        return half;
                }
                return mainBody;
            }
        }

        public Vector3 MarkerOrigin => MarkerBody != null ? MarkerBody.transform.position : transform.position;
        public float MarkerTop => MarkerBody != null ? MarkerBody.Radius : 0.5f;
    }
}

using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Virus gigante, jefe del piso 1. Flota despacio alrededor del centro de la sala y dispara:
    /// una espiral continua y, cada pocos segundos, una ráfaga en círculo.
    /// Fase 2 (al bajar de cierta vida): espiral con más brazos y más rápida, ráfaga más densa
    /// e invoca enemigos. Al morir, sus invocaciones mueren con él.
    /// No le afectan los empujones ni la ceguera: no tiene <see cref="EnemyAI"/>.
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Weapon))]
    public class BossAI : MonoBehaviour
    {
        /// <summary>Jefe activo ahora mismo, para la barra de vida. Null si no hay ninguno.</summary>
        public static BossAI Current { get; private set; }

        [SerializeField] string displayName = "Virus gigante";

        [Header("Movimiento")]
        [Tooltip("Centro de la sala. Si está vacío, usa la posición inicial.")]
        [SerializeField] Transform arenaCenter;
        [SerializeField, Min(0f)] float moveSpeed = 1.5f;
        [Tooltip("Radio del círculo que recorre alrededor del centro.")]
        [SerializeField, Min(0f)] float orbitRadius = 2.5f;
        [Tooltip("Segundos que tarda en dar una vuelta.")]
        [SerializeField, Min(0.1f)] float orbitPeriod = 10f;
        [Tooltip("Segundos de calma al empezar, para que el jugador entre en la sala.")]
        [SerializeField, Min(0f)] float startDelay = 1.5f;

        [Header("Espiral")]
        [SerializeField] AttackData spiralAttack;
        [SerializeField, Min(0.02f)] float spiralInterval = 0.15f;
        [Tooltip("Grados que gira la espiral entre un disparo y el siguiente.")]
        [SerializeField] float spiralStep = 14f;

        [Header("Ráfaga")]
        [SerializeField] AttackData burstAttack;
        [SerializeField, Min(0.1f)] float burstInterval = 4f;
        [SerializeField, Min(1)] int burstCount = 12;

        [Header("Fase 2")]
        [Tooltip("Fracción de vida a la que empieza la fase 2.")]
        [SerializeField, Range(0f, 1f)] float phase2Threshold = 0.5f;
        [SerializeField, Min(1)] int phase2SpiralArms = 2;
        [SerializeField, Min(0.02f)] float phase2SpiralInterval = 0.1f;
        [SerializeField] float phase2SpiralStep = 17f;
        [SerializeField, Min(1)] int phase2BurstCount = 16;
        [Tooltip("Enemigo que invoca en la fase 2.")]
        [SerializeField] Enemy minionPrefab;
        [SerializeField, Min(0.1f)] float summonInterval = 8f;
        [SerializeField, Min(0)] int summonCount = 2;
        [SerializeField, Min(0)] int maxMinions = 4;

        [Header("Animación")]
        [Tooltip("Segundos de aviso antes de cada ráfaga (animación Burst). 0 = sin aviso.")]
        [SerializeField, Min(0f)] float burstWindup;
        [Tooltip("Segundos de aviso antes de cada invocación (animación Summon). 0 = sin aviso.")]
        [SerializeField, Min(0f)] float summonWindup;
        [Tooltip("Segundos sin atacar al pasar a la fase 2 (animación Phase2_Transition).")]
        [SerializeField, Min(0f)] float phase2TransitionDuration;

        public string DisplayName => displayName;
        public int Phase { get; private set; } = 1;
        public Health Health { get; private set; }
        public IReadOnlyList<Enemy> Minions => minions;

        /// <summary>True durante la pausa del cambio a la fase 2.</summary>
        public bool IsTransitioning => Time.time < transitionUntil;

        public event Action<int> PhaseChanged;

        /// <summary>Se lanza al empezar el aviso de una ráfaga; los proyectiles salen al terminar.</summary>
        public event Action BurstStarting;

        /// <summary>Se lanza al empezar el aviso de una invocación; los enemigos aparecen al terminar.</summary>
        public event Action SummonStarting;

        Enemy enemy;
        Weapon weapon;
        Rigidbody2D rb;
        Vector2 home;
        float activeSince;
        float spiralAngle;
        float nextSpiral;
        float nextBurst;
        float nextSummon;
        float transitionUntil;
        bool burstAnnounced;
        bool summonAnnounced;
        readonly List<Enemy> minions = new();

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            weapon = GetComponent<Weapon>();
            Health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();

            // Cinemático: el jugador no lo empuja, pero sigue chocando con él y recibiendo daño por contacto.
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        void OnEnable()
        {
            // Se activa cuando el jugador entra en su sala (la sala oculta a sus enemigos hasta entonces).
            Current = this;
            home = arenaCenter != null ? (Vector2)arenaCenter.position : (Vector2)transform.position;
            activeSince = Time.time;
            nextSpiral = Time.time + startDelay;
            // La primera ráfaga espera también su aviso, para que se vea entero.
            nextBurst = nextSpiral + burstWindup;
            enemy.Died += OnDied;
        }

        void OnDisable()
        {
            if (Current == this)
                Current = null;
            enemy.Died -= OnDied;
        }

        internal void Configure(AttackData spiral, AttackData burst, Enemy minion, float delay = 0f,
            float newSummonInterval = 8f, int newSummonCount = 2, int newMaxMinions = 4)
        {
            spiralAttack = spiral;
            burstAttack = burst;
            minionPrefab = minion;
            startDelay = delay;
            summonInterval = newSummonInterval;
            summonCount = newSummonCount;
            maxMinions = newMaxMinions;
            nextSpiral = Time.time + startDelay;
            // La primera ráfaga espera también su aviso, para que se vea entero.
            nextBurst = nextSpiral + burstWindup;
        }

        internal void SetAnimationTimings(float newBurstWindup, float newSummonWindup, float newTransitionDuration)
        {
            burstWindup = newBurstWindup;
            summonWindup = newSummonWindup;
            phase2TransitionDuration = newTransitionDuration;
        }

        internal void SetArenaCenter(Transform center) => arenaCenter = center;

        /// <summary>Centro de su sala (o donde apareció, si no tiene): ahí deja caer su recompensa.</summary>
        public Vector2 ArenaCenter => arenaCenter != null ? (Vector2)arenaCenter.position : home;

        void Update()
        {
            if (!CanAct() || Time.time < activeSince + startDelay)
                return;

            if (Phase == 1 && Health.Current <= Health.Max * phase2Threshold)
                EnterPhase2();

            if (IsTransitioning)
                return;

            UpdateSpiral();
            UpdateBurst();
            if (Phase == 2)
                UpdateSummons();
        }

        void FixedUpdate()
        {
            if (!CanAct())
                return;

            // Recorre despacio un círculo alrededor del centro de la sala.
            float angle = (Time.time - activeSince) / orbitPeriod * Mathf.PI * 2f;
            var goal = home + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitRadius;
            rb.MovePosition(Vector2.MoveTowards(rb.position, goal, moveSpeed * Time.fixedDeltaTime));
        }

        void EnterPhase2()
        {
            Phase = 2;
            transitionUntil = Time.time + phase2TransitionDuration;
            nextSummon = transitionUntil + 1f;

            // La ráfaga anunciada se cancela; la siguiente vuelve a avisar tras la transición.
            burstAnnounced = false;
            nextBurst = Mathf.Max(nextBurst, transitionUntil + burstWindup);
            PhaseChanged?.Invoke(Phase);
        }

        void UpdateSpiral()
        {
            if (spiralAttack == null || Time.time < nextSpiral)
                return;

            int arms = Phase == 2 ? phase2SpiralArms : 1;
            for (int arm = 0; arm < arms; arm++)
                weapon.Fire(Direction(spiralAngle + arm * 360f / arms), spiralAttack);

            spiralAngle += Phase == 2 ? phase2SpiralStep : spiralStep;
            nextSpiral = Time.time + (Phase == 2 ? phase2SpiralInterval : spiralInterval);
        }

        void UpdateBurst()
        {
            if (burstAttack == null)
                return;

            if (!burstAnnounced && Time.time >= nextBurst - burstWindup)
            {
                burstAnnounced = true;
                BurstStarting?.Invoke();
            }

            if (Time.time < nextBurst)
                return;

            burstAnnounced = false;
            int count = Phase == 2 ? phase2BurstCount : burstCount;
            // Desfase de media separación para que no coincida con la espiral.
            float offset = spiralAngle + 180f / count;
            for (int i = 0; i < count; i++)
                weapon.Fire(Direction(offset + i * 360f / count), burstAttack);

            nextBurst = Time.time + burstInterval;
        }

        void UpdateSummons()
        {
            if (minionPrefab == null)
                return;

            if (!summonAnnounced && Time.time >= nextSummon - summonWindup)
            {
                summonAnnounced = true;
                minions.RemoveAll(m => m == null);
                // Sin hueco para más invocaciones no hay aviso.
                if (summonCount > 0 && minions.Count < maxMinions)
                    SummonStarting?.Invoke();
            }

            if (Time.time < nextSummon)
                return;

            summonAnnounced = false;
            nextSummon = Time.time + summonInterval;
            minions.RemoveAll(m => m == null);

            int toSpawn = Mathf.Min(summonCount, maxMinions - minions.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                Vector2 offset = Direction(i * 360f / Mathf.Max(1, toSpawn) + spiralAngle) * 2f;
                var minion = Instantiate(minionPrefab, rb.position + offset, Quaternion.identity, transform.parent);
                if (minion.TryGetComponent<EnemyAI>(out var ai))
                    ai.Alert();

                // Si soltaran biomasa, un combate largo daría demasiada.
                minion.DropsBiomass = false;
                minions.Add(minion);
                enemy.RegisterChild(minion);
            }
        }

        void OnDied(Enemy _)
        {
            // Sus invocaciones mueren con él, para que la sala quede limpia.
            foreach (var minion in minions)
            {
                if (minion != null && !minion.Health.IsDead)
                    minion.Health.TakeDamage(minion.Health.Max);
            }
            minions.Clear();
        }

        bool CanAct() =>
            !Health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);

        static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}

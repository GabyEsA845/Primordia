using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Animaciones del virus gigante. Cada fase tiene su reposo, ráfaga y daño (_P1 / _P2);
    /// además, Phase2_Transition al cambiar de fase, Summon_P2 al invocar y Death al morir.
    /// Ráfaga, invocación y transición no se interrumpen con los golpes.
    /// </summary>
    [RequireComponent(typeof(BossAI))]
    public class BossAnimator : MonoBehaviour
    {
        public const string TransitionState = "Phase2_Transition";
        public const string SummonState = "Summon_P2";
        public const string DeathState = "Death";

        // Índice 0 para la fase 1 y 1 para la fase 2.
        static readonly string[] IdleStates = { "Idle_P1", "Idle_P2" };
        static readonly string[] BurstStates = { "Burst_P1", "Burst_P2" };
        static readonly string[] HurtStates = { "Hurt_P1", "Hurt_P2" };

        [Tooltip("Animator del cuerpo, con un estado por clip (Idle_P1, Burst_P1…).")]
        [SerializeField] Animator animator;

        BossAI boss;
        Health health;
        AnimatorStates states;
        float lockedUntil;
        float actionUntil;

        public string CurrentState => states?.Current;

        void Awake()
        {
            boss = GetComponent<BossAI>();
            // La vida se pide aquí: el Awake de BossAI puede ejecutarse después que este.
            health = GetComponent<Health>();
            states = new AnimatorStates(animator);
        }

        void OnEnable()
        {
            boss.BurstStarting += OnBurstStarting;
            boss.SummonStarting += OnSummonStarting;
            boss.PhaseChanged += OnPhaseChanged;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            boss.BurstStarting -= OnBurstStarting;
            boss.SummonStarting -= OnSummonStarting;
            boss.PhaseChanged -= OnPhaseChanged;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        internal void Configure(Animator newAnimator)
        {
            animator = newAnimator;
            states = new AnimatorStates(animator);
        }

        void LateUpdate()
        {
            if (health.IsDead || Time.time < lockedUntil)
                return;

            states.Play(IdleStates[PhaseIndex]);
        }

        void OnBurstStarting() => ShowAction(BurstStates[PhaseIndex]);

        void OnSummonStarting() => ShowAction(SummonState);

        void OnPhaseChanged(int _) => ShowAction(TransitionState);

        void OnDamaged(int _)
        {
            if (health.IsDead || Time.time < actionUntil)
                return;

            var hurt = HurtStates[PhaseIndex];
            states.Play(hurt, restart: true);
            lockedUntil = Time.time + states.Length(hurt);
        }

        void OnDied() => states.Play(DeathState, restart: true);

        void ShowAction(string state)
        {
            states.Play(state, restart: true);
            lockedUntil = actionUntil = Time.time + states.Length(state);
        }

        int PhaseIndex => boss.Phase == 2 ? 1 : 0;
    }
}

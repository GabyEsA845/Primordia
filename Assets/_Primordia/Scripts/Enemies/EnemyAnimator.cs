using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Animaciones de un enemigo normal: Move en bucle, Hurt al recibir daño, Shoot al apuntar
    /// (si dispara) y Split o Death al morir. Los sprites miran a la derecha: se voltean para mirar
    /// al jugador cuando está alerta y, si no, hacia donde se mueve.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyAnimator : MonoBehaviour
    {
        public const string MoveState = "Move";
        public const string HurtState = "Hurt";
        public const string ShootState = "Shoot";
        public const string SplitState = "Split";
        public const string DeathState = "Death";

        [Tooltip("Animator del cuerpo, con un estado por clip (Move, Hurt, Death…).")]
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer body;

        Enemy enemy;
        Health health;
        EnemyAI ai;
        Rigidbody2D rb;
        AnimatorStates states;
        float lockedUntil;
        float actionUntil;

        /// <summary>Estado que se está mostrando: Move, Hurt, Shoot, Split o Death.</summary>
        public string CurrentState => states?.Current;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            TryGetComponent(out ai);
            states = new AnimatorStates(animator);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            if (ai != null)
                ai.AimStarted += OnAimStarted;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            if (ai != null)
                ai.AimStarted -= OnAimStarted;
        }

        internal void Configure(Animator newAnimator, SpriteRenderer newBody)
        {
            animator = newAnimator;
            body = newBody;
            states = new AnimatorStates(animator);
        }

        void LateUpdate()
        {
            if (health.IsDead)
                return;

            UpdateFlip();

            if (Time.time >= lockedUntil)
                states.Play(MoveState);
        }

        void UpdateFlip()
        {
            if (body == null)
                return;

            Vector2 direction = ai != null && ai.IsAlert && ai.Target != null
                ? ai.Target.position - transform.position
                : rb.linearVelocity;

            if (Mathf.Abs(direction.x) > 0.05f)
                body.flipX = direction.x < 0f;
        }

        void OnAimStarted()
        {
            Show(ShootState);
            actionUntil = lockedUntil;
        }

        void OnDamaged(int _)
        {
            // El golpe no interrumpe el disparo.
            if (health.IsDead || Time.time < actionUntil)
                return;

            Show(HurtState);
        }

        void OnDied()
        {
            bool split = enemy.WillSplit && (animator == null || states.Has(SplitState));
            states.Play(split ? SplitState : DeathState, restart: true);
        }

        void Show(string state)
        {
            states.Play(state, restart: true);
            lockedUntil = Time.time + states.Length(state);
        }
    }
}

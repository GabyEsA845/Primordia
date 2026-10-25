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
        [Tooltip("Efecto encima de la muerte (burbujas). No aparece al dividirse: Split ya cuenta lo que pasa.")]
        [SerializeField] GameObject deathEffect;

        Enemy enemy;
        Health health;
        EnemyAI ai;
        EnemyBrain brain;
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

        void Start() => TryGetComponent(out brain);

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

        internal void Configure(Animator newAnimator, SpriteRenderer newBody, GameObject newDeathEffect = null)
        {
            animator = newAnimator;
            body = newBody;
            deathEffect = newDeathEffect;
            states = new AnimatorStates(animator);
        }

        void LateUpdate()
        {
            if (health.IsDead)
                return;

            UpdateFlip();

            if (Time.time >= lockedUntil)
                states.Play(brain != null && brain.AnimationState != null ? brain.AnimationState : MoveState);
        }

        /// <summary>Muestra una vez esa animación (la del cerebro: aviso, ataque…) y devuelve lo que dura; 0 si no la tiene.</summary>
        public float PlayOnce(string state)
        {
            if (!states.Has(state))
                return 0f;
            Show(state);
            actionUntil = lockedUntil;
            return states.Length(state);
        }

        /// <summary>Cuánto dura esa animación (0 si no la tiene).</summary>
        public float LengthOf(string state) => states.Has(state) ? states.Length(state) : 0f;

        /// <summary>Corta la animación de una vez que se esté viendo (la del cerebro manda enseguida).</summary>
        public void Unlock()
        {
            lockedUntil = 0f;
            actionUntil = 0f;
        }

        void UpdateFlip()
        {
            if (body == null)
                return;

            Vector2 direction = brain != null && brain.Facing.HasValue ? brain.Facing.Value
                : ai != null && ai.IsAlert && ai.Target != null ? ai.Target.position - transform.position
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
            // El golpe no interrumpe el disparo ni lo que haga el cerebro (enterrado, embistiendo…).
            if (health.IsDead || Time.time < actionUntil || (brain != null && !brain.ShowsHurt))
                return;

            Show(HurtState);
        }

        void OnDied()
        {
            bool split = enemy.WillSplit && (animator == null || states.Has(SplitState));
            states.Play(split ? SplitState : DeathState, restart: true);

            if (!enemy.WillSplit)
                OneShotEffect.Spawn(deathEffect, transform.position);
        }

        void Show(string state)
        {
            states.Play(state, restart: true);
            lockedUntil = Time.time + states.Length(state);
        }
    }
}

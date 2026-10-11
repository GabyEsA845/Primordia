using Primordia.Core;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// Elige la animación del cuerpo según lo que hace el jugador: reposo, moverse, esquivar,
    /// recibir daño o morir. Reposo y moverse miran hacia el cursor; la esquiva, hacia donde rueda.
    /// Los estados del Animator se llaman como los clips: <c>Idle_side</c>, <c>Move_up</c>, <c>Hurt</c>…
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health))]
    public class PlayerAnimator : MonoBehaviour
    {
        public enum Facing { Side, Down, Up }
        public enum Action { Idle, Move, Dodge }

        [Tooltip("Animator del cuerpo, con un estado por clip.")]
        [SerializeField] Animator animator;
        [Tooltip("Sprite del cuerpo. La vista lateral mira a la derecha y se voltea para la izquierda.")]
        [SerializeField] SpriteRenderer body;
        [Tooltip("Segundos que se muestra la animación Hurt tras un golpe.")]
        [SerializeField, Min(0f)] float hurtDuration = 0.2f;

        static readonly int HurtState = Animator.StringToHash("Hurt");
        static readonly int DeathState = Animator.StringToHash("Death");
        static readonly int[,] States = BuildStates();

        PlayerController controller;
        Health health;
        float hurtUntil;
        int currentState;

        public Facing CurrentFacing { get; private set; } = Facing.Down;
        public Action CurrentAction { get; private set; } = Action.Idle;
        public bool IsHurt => !health.IsDead && Time.time < hurtUntil;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void LateUpdate()
        {
            if (health.IsDead)
                return;

            Vector2 direction;
            if (controller.IsDodging)
            {
                CurrentAction = Action.Dodge;
                direction = controller.DodgeDirection;
            }
            else
            {
                bool moving = controller.CanAct && controller.MoveInput.sqrMagnitude > 0.01f;
                CurrentAction = moving ? Action.Move : Action.Idle;
                direction = controller.AimDirection;
            }

            CurrentFacing = ChooseFacing(direction);

            // La hoja Hurt es de perfil: se voltea igual que la vista lateral.
            if (body != null)
                body.flipX = (CurrentFacing == Facing.Side || IsHurt) && direction.x < 0f;

            if (CurrentAction != Action.Dodge && IsHurt)
                return;

            Play(States[(int)CurrentAction, (int)CurrentFacing]);
        }

        /// <summary>Lateral si la dirección es más horizontal que vertical; si no, arriba o abajo.</summary>
        public static Facing ChooseFacing(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
                return direction.y > 0f ? Facing.Up : Facing.Down;

            return Facing.Side;
        }

        /// <summary>Nombre del estado del Animator (y del clip) para una acción y orientación.</summary>
        public static string StateName(Action action, Facing facing) => $"{action}_{facing.ToString().ToLowerInvariant()}";

        internal void Configure(Animator animator, SpriteRenderer body)
        {
            this.animator = animator;
            this.body = body;
        }

        void OnDamaged(int _)
        {
            if (health.IsDead || controller.IsDodging)
                return;

            hurtUntil = Time.time + hurtDuration;
            Play(HurtState, restart: true);
        }

        void OnDied()
        {
            if (body != null)
                body.flipX = false;

            // Al reventar solo queda la célula: se ocultan la boca y los aspectos de mutación.
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
            {
                if (sprite != body)
                    sprite.enabled = false;
            }

            Play(DeathState, restart: true);
        }

        void Play(int state, bool restart = false)
        {
            if (animator == null || (state == currentState && !restart))
                return;

            animator.Play(state, 0, 0f);
            currentState = state;
        }

        static int[,] BuildStates()
        {
            var states = new int[3, 3];
            for (int a = 0; a < 3; a++)
            {
                for (int f = 0; f < 3; f++)
                    states[a, f] = Animator.StringToHash(StateName((Action)a, (Facing)f));
            }
            return states;
        }
    }
}

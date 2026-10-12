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
        public Animator BodyAnimator => animator;
        public SpriteRenderer Body => body;

        /// <summary>
        /// Se lanza cuando el cuerpo empieza (o vuelve a empezar) una animación de reposo, moverse o esquiva.
        /// Las piezas de mutación lo usan para ir al mismo cuadro que el cuerpo.
        /// </summary>
        public event System.Action<Action, Facing> StateChanged;

        RuntimeAnimatorController cellController;
        bool hasCellController;

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

            if (Play(States[(int)CurrentAction, (int)CurrentFacing]))
                StateChanged?.Invoke(CurrentAction, CurrentFacing);
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

        /// <summary>
        /// Cambia las animaciones del cuerpo (la Piel sustituye a la célula); null vuelve a las de la célula.
        /// El controlador nuevo debe tener los mismos estados que el del jugador.
        /// </summary>
        public void SetBodyController(RuntimeAnimatorController controller)
        {
            if (animator == null)
                return;

            if (!hasCellController)
            {
                cellController = animator.runtimeAnimatorController;
                hasCellController = true;
            }

            animator.runtimeAnimatorController = controller != null ? controller : cellController;

            // El controlador nuevo empieza en su estado por defecto: se vuelve a elegir el que toca.
            currentState = 0;
            if (health.IsDead)
                Play(DeathState, restart: true);
            else if (IsHurt)
                Play(HurtState, restart: true);
        }

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

            // Al reventar solo queda el cuerpo: se ocultan la boca y las piezas de mutación.
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
            {
                if (sprite != body)
                    sprite.enabled = false;
            }

            Play(DeathState, restart: true);
        }

        /// <summary>Pone el estado desde el principio. Devuelve false si ya estaba en él (y no se reinicia).</summary>
        bool Play(int state, bool restart = false)
        {
            if (state == currentState && !restart)
                return false;

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Play(state, 0, 0f);
            currentState = state;
            return true;
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

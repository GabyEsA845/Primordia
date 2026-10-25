using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// Elige la animación del cuerpo según lo que hace el jugador: reposo, moverse, esquivar, atacar,
    /// recibir daño o morir. Reposo y moverse miran hacia el cursor; la esquiva, hacia donde rueda.
    /// Los estados del Animator se llaman como los clips: <c>Idle_side</c>, <c>Move_up</c>, <c>Hurt</c>…
    /// Los cuerpos de las criaturas (desde el piso 2) tienen además <c>Attack_side/down/up</c>, que suenan al disparar.
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
        static readonly int[] AttackStates =
        {
            Animator.StringToHash("Attack_side"), Animator.StringToHash("Attack_down"), Animator.StringToHash("Attack_up"),
        };
        static readonly int DeathState = Animator.StringToHash("Death");
        static readonly int[,] States = BuildStates();

        PlayerController controller;
        Health health;
        Weapon weapon;
        float hurtUntil;
        float attackUntil;
        int currentState;
        readonly Dictionary<(RuntimeAnimatorController, Facing), float> attackLengths = new();

        public Facing CurrentFacing { get; private set; } = Facing.Down;
        public Action CurrentAction { get; private set; } = Action.Idle;
        public bool IsHurt => !health.IsDead && Time.time < hurtUntil;
        public Animator BodyAnimator => animator;
        public SpriteRenderer Body => body;

        /// <summary>True mientras el cuerpo hace su animación de ataque.</summary>
        public bool IsAttacking => attackUntil > Time.time;

        /// <summary>Animaciones de la criatura que sustituyen a la célula y a la Piel (null = ninguna).</summary>
        public RuntimeAnimatorController CreatureController => creatureController;

        /// <summary>
        /// Orientación fija (escenas): el cuerpo mira hacia ahí aunque se mueva el cursor. Null = la de siempre.
        /// </summary>
        public Facing? FacingOverride { get; set; }

        /// <summary>
        /// Se lanza cuando el cuerpo empieza (o vuelve a empezar) una animación de reposo, moverse o esquiva.
        /// Las piezas de mutación lo usan para ir al mismo cuadro que el cuerpo.
        /// </summary>
        public event System.Action<Action, Facing> StateChanged;

        RuntimeAnimatorController cellController;
        RuntimeAnimatorController skinController;
        RuntimeAnimatorController creatureController;
        bool hasCellController;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            weapon = GetComponent<Weapon>();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            if (weapon != null)
                weapon.Started += OnAttackStarted;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            if (weapon != null)
                weapon.Started -= OnAttackStarted;
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

            CurrentFacing = FacingOverride ?? ChooseFacing(direction);

            // La hoja Hurt es de perfil: se voltea igual que la vista lateral.
            if (body != null)
                body.flipX = FacingOverride == null && (CurrentFacing == Facing.Side || IsHurt) && direction.x < 0f;

            if (CurrentAction != Action.Dodge && (IsHurt || IsAttacking))
                return;
            attackUntil = 0f;

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
        /// El controlador nuevo debe tener los mismos estados que el del jugador. Con una criatura puesta
        /// (<see cref="SetCreatureController"/>) se guarda para cuando se quite.
        /// </summary>
        public void SetBodyController(RuntimeAnimatorController controller)
        {
            skinController = controller;
            ApplyController();
        }

        /// <summary>
        /// Pone las animaciones de la criatura de la Boca (desde el piso 2), que mandan sobre la célula y la Piel;
        /// null las quita.
        /// </summary>
        public void SetCreatureController(RuntimeAnimatorController controller)
        {
            creatureController = controller;
            ApplyController();
        }

        /// <summary>El cuerpo hace una vez su ataque en esa orientación (si tiene), como pose de presentación.</summary>
        public bool PlayAttack(Facing facing)
        {
            int state = AttackStates[(int)facing];
            if (animator == null || animator.runtimeAnimatorController == null || !animator.HasState(0, state))
                return false;

            attackUntil = Time.time + AttackLength(facing);
            Play(state, restart: true);
            return true;
        }

        void OnAttackStarted(Vector2 direction)
        {
            if (health.IsDead || controller.IsDodging)
                return;
            PlayAttack(FacingOverride ?? ChooseFacing(direction));
        }

        float AttackLength(Facing facing)
        {
            var current = animator.runtimeAnimatorController;
            var key = (current, facing);
            if (attackLengths.TryGetValue(key, out float length))
                return length;

            length = 0.4f;
            string name = $"Attack_{facing.ToString().ToLowerInvariant()}";
            foreach (var clip in current.animationClips)
            {
                if (clip != null && clip.name.EndsWith(name))
                    length = clip.length;
            }
            attackLengths[key] = length;
            return length;
        }

        void ApplyController()
        {
            if (animator == null)
                return;

            if (!hasCellController)
            {
                cellController = animator.runtimeAnimatorController;
                hasCellController = true;
            }

            animator.runtimeAnimatorController = creatureController != null ? creatureController
                : skinController != null ? skinController
                : cellController;
            attackUntil = 0f;

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

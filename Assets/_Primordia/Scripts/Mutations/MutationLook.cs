using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Dibuja las mutaciones equipadas por capas, de atrás hacia delante: Extremidades, cuerpo, Órgano y Boca
    /// (la Boca pasa detrás del cuerpo cuando el jugador mira hacia arriba). La Piel no es una pieza: cambia
    /// las animaciones del cuerpo y aleja la Boca hasta su borde.
    /// Las Extremidades van al mismo cuadro que el cuerpo; la Boca gira con el apuntado y ataca en cada
    /// disparo; el Órgano se activa al usar la habilidad. En la esquiva y al morir solo se ve el cuerpo.
    /// </summary>
    // Después de PlayerAnimator, que elige la animación del cuerpo en su LateUpdate.
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(PlayerMutations), typeof(PlayerAnimator))]
    public class MutationLook : MonoBehaviour
    {
        [Tooltip("Pieza de Extremidades: hijo del jugador, centrado, detrás del cuerpo.")]
        [SerializeField] Animator limbs;
        [Tooltip("Pieza de Órgano: hijo del jugador, centrado, delante del cuerpo.")]
        [SerializeField] Animator organ;
        [Tooltip("Pieza de Boca: hijo de AimPivot, con el pivote en su punto de unión (borde izquierdo).")]
        [SerializeField] Animator mouth;
        [Tooltip("Distancia del centro al borde de la célula, donde va la Boca sin Piel (6 px).")]
        [SerializeField, Min(0f)] float cellMouthDistance = 0.375f;
        [Tooltip("Orden de dibujo de la Boca; mirando hacia arriba, el de detrás del cuerpo.")]
        [SerializeField] int mouthFrontOrder = 12;
        [SerializeField] int mouthBehindOrder = 9;

        static readonly int IdleState = Animator.StringToHash("Idle");
        static readonly int AttackState = Animator.StringToHash("Attack");
        static readonly int ActivateState = Animator.StringToHash("Activate");

        PlayerMutations mutations;
        PlayerAnimator playerAnimator;
        PlayerController controller;
        Health health;
        Weapon weapon;
        PlayerAbility ability;
        SpriteRenderer limbsRenderer;
        SpriteRenderer organRenderer;
        SpriteRenderer mouthRenderer;
        float mouthBusyUntil;
        float organBusyUntil;

        public bool LimbsVisible => IsShown(limbsRenderer);
        public bool OrganVisible => IsShown(organRenderer);
        public bool MouthVisible => IsShown(mouthRenderer);
        public bool MouthBehindBody => mouthRenderer != null && mouthRenderer.sortingOrder == mouthBehindOrder;
        public float MouthDistance => mouth != null ? mouth.transform.localPosition.x : 0f;

        void Awake()
        {
            mutations = GetComponent<PlayerMutations>();
            playerAnimator = GetComponent<PlayerAnimator>();
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            weapon = GetComponent<Weapon>();
            CacheRenderers();
        }

        void OnEnable()
        {
            mutations.Changed += OnMutationChanged;
            playerAnimator.StateChanged += OnBodyStateChanged;
            weapon.Fired += OnFired;
            SubscribeAbility();
        }

        void OnDisable()
        {
            mutations.Changed -= OnMutationChanged;
            playerAnimator.StateChanged -= OnBodyStateChanged;
            weapon.Fired -= OnFired;
            if (ability != null)
                ability.Used -= OnAbilityUsed;
        }

        void Start()
        {
            // Por si se equipó algo antes de que este componente escuchara.
            foreach (MutationSlot slot in System.Enum.GetValues(typeof(MutationSlot)))
                OnMutationChanged(slot, mutations.Get(slot));
        }

        void LateUpdate()
        {
            // Al morir, PlayerAnimator ya ocultó las piezas.
            if (health.IsDead)
                return;

            if (mouthBusyUntil > 0f && Time.time >= mouthBusyUntil)
            {
                mouthBusyUntil = 0f;
                Play(mouth, IdleState);
            }

            if (organBusyUntil > 0f && Time.time >= organBusyUntil)
            {
                organBusyUntil = 0f;
                Play(organ, IdleState);
            }

            bool shown = !controller.IsDodging;
            Show(limbsRenderer, limbs, shown);
            Show(organRenderer, organ, shown);
            Show(mouthRenderer, mouth, shown);

            // Extremidades y Órgano se voltean con el cuerpo; la Boca gira con AimPivot y, apuntando a la
            // izquierda, se voltea en vertical para no quedar boca abajo.
            bool flip = playerAnimator.Body != null && playerAnimator.Body.flipX;
            if (limbsRenderer != null)
                limbsRenderer.flipX = flip;
            if (organRenderer != null)
                organRenderer.flipX = flip;
            if (mouthRenderer != null)
            {
                mouthRenderer.flipY = controller.AimDirection.x < 0f;
                mouthRenderer.sortingOrder = playerAnimator.CurrentFacing == PlayerAnimator.Facing.Up
                    ? mouthBehindOrder
                    : mouthFrontOrder;
            }
        }

        internal void Configure(Animator newLimbs, Animator newOrgan, Animator newMouth)
        {
            limbs = newLimbs;
            organ = newOrgan;
            mouth = newMouth;
            CacheRenderers();
        }

        void CacheRenderers()
        {
            limbsRenderer = limbs != null ? limbs.GetComponent<SpriteRenderer>() : null;
            organRenderer = organ != null ? organ.GetComponent<SpriteRenderer>() : null;
            mouthRenderer = mouth != null ? mouth.GetComponent<SpriteRenderer>() : null;
        }

        void SubscribeAbility()
        {
            // PlayerAbility puede añadirse después (escenas de prueba); se busca hasta encontrarla.
            if (ability != null || !TryGetComponent(out ability))
                return;

            ability.Used += OnAbilityUsed;
        }

        void OnMutationChanged(MutationSlot slot, MutationData mutation)
        {
            var look = mutation != null ? mutation.Look : null;
            switch (slot)
            {
                case MutationSlot.Skin:
                    playerAnimator.SetBodyController(look);
                    if (mouth != null)
                    {
                        float distance = mutation != null && mutation.MouthDistance > 0f ? mutation.MouthDistance : cellMouthDistance;
                        mouth.transform.localPosition = new Vector3(distance, 0f, 0f);
                    }
                    break;

                case MutationSlot.Limbs:
                    if (SetLook(limbs, look))
                        SyncLimbs();
                    break;

                case MutationSlot.Organ:
                    organBusyUntil = 0f;
                    if (SetLook(organ, look))
                        Play(organ, IdleState);
                    SubscribeAbility();
                    break;

                case MutationSlot.Mouth:
                    mouthBusyUntil = 0f;
                    if (SetLook(mouth, look))
                        Play(mouth, IdleState);
                    break;
            }
        }

        /// <summary>Pone las animaciones a la pieza. Devuelve false si se quedó vacía.</summary>
        static bool SetLook(Animator piece, RuntimeAnimatorController look)
        {
            if (piece == null)
                return false;

            piece.runtimeAnimatorController = look;
            var renderer = piece.GetComponent<SpriteRenderer>();
            if (look == null && renderer != null)
            {
                renderer.enabled = false;
                renderer.sprite = null;
            }
            return look != null;
        }

        /// <summary>Al equiparlas a mitad de animación, las Extremidades van al mismo punto que el cuerpo.</summary>
        void SyncLimbs()
        {
            var action = playerAnimator.CurrentAction == PlayerAnimator.Action.Move ? PlayerAnimator.Action.Move : PlayerAnimator.Action.Idle;
            float time = 0f;
            var body = playerAnimator.BodyAnimator;
            if (body != null && body.runtimeAnimatorController != null)
                time = Mathf.Repeat(body.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

            Play(limbs, Animator.StringToHash(PlayerAnimator.StateName(action, playerAnimator.CurrentFacing)), time);
        }

        // Las Extremidades no tienen esquiva: durante la esquiva están ocultas.
        void OnBodyStateChanged(PlayerAnimator.Action action, PlayerAnimator.Facing facing)
        {
            if (action != PlayerAnimator.Action.Dodge)
                Play(limbs, Animator.StringToHash(PlayerAnimator.StateName(action, facing)));
        }

        void OnFired(Vector2 _)
        {
            if (Play(mouth, AttackState))
                mouthBusyUntil = Time.time + ClipLength(mouth, "Attack");
        }

        void OnAbilityUsed()
        {
            if (Play(organ, ActivateState))
                organBusyUntil = Time.time + ClipLength(organ, "Activate");
        }

        static bool Play(Animator piece, int state, float normalizedTime = 0f)
        {
            if (piece == null || piece.runtimeAnimatorController == null || !piece.HasState(0, state))
                return false;

            piece.Play(state, 0, normalizedTime);
            return true;
        }

        static float ClipLength(Animator piece, string clipName)
        {
            foreach (var clip in piece.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                    return clip.length;
            }
            return 0f;
        }

        static void Show(SpriteRenderer renderer, Animator piece, bool shown)
        {
            if (renderer != null)
                renderer.enabled = shown && piece.runtimeAnimatorController != null;
        }

        static bool IsShown(SpriteRenderer renderer) => renderer != null && renderer.enabled;
    }
}

using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Dibuja las mutaciones equipadas por capas, de atrás hacia delante: Extremidades, cuerpo, Órgano, Boca y
    /// gorrito (la Boca pasa detrás del cuerpo cuando el jugador mira hacia arriba). La Piel no es una pieza:
    /// cambia las animaciones del cuerpo y aleja la Boca hasta su borde.
    /// Las Extremidades van al mismo cuadro que el cuerpo; la Boca gira con el apuntado y ataca en cada
    /// disparo; el Órgano se activa al usar la habilidad. Una Boca que se lleva en la cabeza (el gorrito de las
    /// Esporas) va como las Extremidades, sobre la cabeza de la Piel, y al atacar el cuerpo se adelanta y el
    /// gorrito se aplasta y se hincha. En la esquiva y al morir solo se ve el cuerpo.
    /// Con un linaje puesto (<see cref="SetLineageLook"/>), sus piezas sustituyen a las de la Boca y las
    /// Extremidades de sus mutaciones (la cola del Escorpión, las patas de la Tortuga…).
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
        [Tooltip("Boca que se lleva en la cabeza (gorrito): hijo del jugador, centrado, delante de todo.")]
        [SerializeField] Animator cap;
        [Tooltip("Distancia del centro al borde de la célula, donde va la Boca sin Piel (6 px).")]
        [SerializeField, Min(0f)] float cellMouthDistance = 0.375f;
        [Tooltip("Orden de dibujo de la Boca; mirando hacia arriba, el de detrás del cuerpo.")]
        [SerializeField] int mouthFrontOrder = 12;
        [SerializeField] int mouthBehindOrder = 9;

        [Header("Ataque desde la cabeza")]
        [Tooltip("Segundos que dura el adelanto del cuerpo al atacar.")]
        [SerializeField, Min(0.05f)] float leanDuration = 0.2f;
        [Tooltip("Altura de la base del gorrito sobre su pivote: desde ahí se aplasta y se hincha (4 px).")]
        [SerializeField] float capBase = 0.25f;
        [Tooltip("De dónde salen los proyectiles de una Boca en la cabeza: altura sobre el pivote del gorrito " +
                 "y distancia hacia el cursor.")]
        [SerializeField] Vector2 capMuzzle = new(0.25f, 0.45f);

        static readonly int IdleState = Animator.StringToHash("Idle");
        static readonly int AttackState = Animator.StringToHash("Attack");
        static readonly int ActivateState = Animator.StringToHash("Activate");

        // Fracción del adelanto en la que el cuerpo llega al punto más adelantado (y el gorrito, al más aplastado).
        const float LeanPeak = 0.3f;
        const float PixelsPerUnit = 16f;

        PlayerMutations mutations;
        PlayerAnimator playerAnimator;
        PlayerController controller;
        Health health;
        Weapon weapon;
        PlayerAbility ability;
        SpriteRenderer limbsRenderer;
        SpriteRenderer organRenderer;
        SpriteRenderer mouthRenderer;
        SpriteRenderer capRenderer;
        float mouthBusyUntil;
        float organBusyUntil;

        MutationData mouthMutation;
        LineageData lineageLook;
        float headHeight;
        Vector3 defaultMuzzle;
        bool hasDefaultMuzzle;
        Vector2 leanDirection;
        float leanAmount;
        float leanStart = -1f;

        // Lo que se adelanta al atacar; el gorrito va aparte porque además se aplasta.
        Transform[] leaners;
        Vector3[] leanerBases;

        public bool LimbsVisible => IsShown(limbsRenderer);
        public bool OrganVisible => IsShown(organRenderer);
        public bool MouthVisible => IsShown(mouthRenderer);
        public bool CapVisible => IsShown(capRenderer);
        public bool MouthBehindBody => mouthRenderer != null && mouthRenderer.sortingOrder == mouthBehindOrder;
        public float MouthDistance => mouth != null ? mouth.transform.localPosition.x : 0f;

        /// <summary>Altura de la cabeza de la Piel actual respecto a la de la célula (donde va el gorrito).</summary>
        public float HeadHeight => headHeight;

        /// <summary>True si la Boca equipada se lleva en la cabeza.</summary>
        public bool MouthOnHead => mouthMutation != null && mouthMutation.WornOnHead;

        /// <summary>Linaje cuyas piezas se ven ahora, o null.</summary>
        public LineageData LineageLook => lineageLook;

        /// <summary>Oculta las piezas (mientras se ve la animación de la evolución).</summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// True con el cuerpo de una criatura (Boca del piso 2 en adelante): manda sobre la célula y la Piel, y las piezas
        /// de las demás ranuras no se dibujan hasta que tengan sus capas para ese cuerpo.
        /// </summary>
        public bool CreatureForm { get; private set; }

        /// <summary>
        /// Mientras es true, cambiar de Boca no cambia todavía la forma: la metamorfosis la retiene hasta revelarla
        /// (<see cref="ReleaseForm"/>).
        /// </summary>
        public bool FormHeld { get; private set; }

        /// <summary>Retiene la forma actual aunque cambie la Boca (ver <see cref="FormHeld"/>).</summary>
        public void HoldForm() => FormHeld = true;

        /// <summary>Deja de retener la forma y pone la de la Boca que se lleva ahora.</summary>
        public void ReleaseForm()
        {
            FormHeld = false;
            ApplyCreatureForm(mutations.Get(MutationSlot.Mouth));
        }

        void ApplyCreatureForm(MutationData newMouth)
        {
            var creature = newMouth != null ? newMouth.CreatureBody : null;
            CreatureForm = creature != null;
            playerAnimator.SetCreatureController(creature);
        }

        /// <summary>Animaciones de la pieza de esa ranura que se ven ahora (las del linaje, si las sustituye).</summary>
        public RuntimeAnimatorController LookOf(MutationSlot slot) => LookFor(slot, mutations.Get(slot));

        /// <summary>
        /// Pone (o quita, con null) las piezas de un linaje en lugar de las de sus mutaciones. Solo cambian las
        /// ranuras que el linaje sustituye y que llevan su mutación.
        /// </summary>
        public void SetLineageLook(LineageData lineage)
        {
            lineageLook = lineage;
            OnMutationChanged(MutationSlot.Mouth, mutations.Get(MutationSlot.Mouth));
            OnMutationChanged(MutationSlot.Limbs, mutations.Get(MutationSlot.Limbs));
        }

        RuntimeAnimatorController LookFor(MutationSlot slot, MutationData mutation)
        {
            if (mutation == null)
                return null;
            var lineage = LineageOverrides(slot, mutation) ? lineageLook.LookFor(slot) : null;
            return lineage != null ? lineage : mutation.Look;
        }

        bool LineageOverrides(MutationSlot slot, MutationData mutation) =>
            lineageLook != null && mutation != null && lineageLook.MutationFor(slot) == mutation && lineageLook.LookFor(slot) != null;

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
            EndLean();
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

            bool shown = !controller.IsDodging && !Hidden && !CreatureForm;
            Show(limbsRenderer, limbs, shown);
            Show(organRenderer, organ, shown);
            Show(mouthRenderer, mouth, shown);
            Show(capRenderer, cap, shown);

            // Extremidades, Órgano y gorrito se voltean con el cuerpo; la Boca gira con AimPivot y, apuntando a la
            // izquierda, se voltea en vertical para no quedar boca abajo.
            bool flip = playerAnimator.Body != null && playerAnimator.Body.flipX;
            if (limbsRenderer != null)
                limbsRenderer.flipX = flip;
            if (organRenderer != null)
                organRenderer.flipX = flip;
            if (capRenderer != null)
                capRenderer.flipX = flip;
            if (mouthRenderer != null)
            {
                mouthRenderer.flipY = controller.AimDirection.x < 0f;
                mouthRenderer.sortingOrder = playerAnimator.CurrentFacing == PlayerAnimator.Facing.Up
                    ? mouthBehindOrder
                    : mouthFrontOrder;
            }

            UpdateLean();

            // Con la Boca en la cabeza, los proyectiles salen del gorrito, hacia el cursor.
            if (MouthOnHead && weapon.Muzzle != null && cap != null)
                weapon.Muzzle.position = cap.transform.position + Vector3.up * capMuzzle.y + (Vector3)(controller.AimDirection * capMuzzle.x);
        }

        internal void Configure(Animator newLimbs, Animator newOrgan, Animator newMouth, Animator newCap = null)
        {
            limbs = newLimbs;
            organ = newOrgan;
            mouth = newMouth;
            cap = newCap;
            leaners = null;
            CacheRenderers();
        }

        void CacheRenderers()
        {
            limbsRenderer = limbs != null ? limbs.GetComponent<SpriteRenderer>() : null;
            organRenderer = organ != null ? organ.GetComponent<SpriteRenderer>() : null;
            mouthRenderer = mouth != null ? mouth.GetComponent<SpriteRenderer>() : null;
            capRenderer = cap != null ? cap.GetComponent<SpriteRenderer>() : null;
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
            var look = LookFor(slot, mutation);
            switch (slot)
            {
                case MutationSlot.Skin:
                    playerAnimator.SetBodyController(look);
                    if (mouth != null)
                    {
                        float distance = mutation != null && mutation.MouthDistance > 0f ? mutation.MouthDistance : cellMouthDistance;
                        mouth.transform.localPosition = new Vector3(distance, 0f, 0f);
                    }
                    headHeight = mutation != null ? mutation.HeadHeight : 0f;
                    PlaceCap(Vector2.zero, 1f, 1f);
                    UpdateMuzzle();
                    break;

                case MutationSlot.Limbs:
                    if (SetLook(limbs, look))
                        SyncWithBody(limbs);
                    break;

                case MutationSlot.Organ:
                    organBusyUntil = 0f;
                    if (SetLook(organ, look))
                        Play(organ, IdleState);
                    SubscribeAbility();
                    break;

                case MutationSlot.Mouth:
                    mouthBusyUntil = 0f;
                    mouthMutation = mutation;
                    if (!FormHeld)
                        ApplyCreatureForm(mutation);
                    bool onHead = MouthOnHead;
                    if (SetLook(mouth, onHead ? null : look))
                        Play(mouth, IdleState);
                    if (SetLook(cap, onHead ? look : null))
                        SyncWithBody(cap);
                    UpdateMuzzle();
                    break;
            }
        }

        /// <summary>
        /// Pone el punto de disparo en la punta de la Boca (si la Boca dice dónde está); si no, donde estaba.
        /// Con la Boca en la cabeza lo coloca <see cref="LateUpdate"/>.
        /// </summary>
        void UpdateMuzzle()
        {
            var muzzle = weapon != null ? weapon.Muzzle : null;
            if (muzzle == null)
                return;

            if (!hasDefaultMuzzle)
            {
                defaultMuzzle = muzzle.localPosition;
                hasDefaultMuzzle = true;
            }

            float tip = mouthMutation != null ? mouthMutation.MuzzleDistance : 0f;
            if (LineageOverrides(MutationSlot.Mouth, mouthMutation) && lineageLook.MouthMuzzleDistance > 0f)
                tip = lineageLook.MouthMuzzleDistance;

            if (mouthMutation != null && !mouthMutation.WornOnHead && tip > 0f)
                muzzle.localPosition = new Vector3(MouthDistance + tip, 0f, 0f);
            else
                muzzle.localPosition = defaultMuzzle;
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

        /// <summary>Al equiparla a mitad de animación, la pieza va al mismo punto que el cuerpo.</summary>
        void SyncWithBody(Animator piece)
        {
            var action = playerAnimator.CurrentAction == PlayerAnimator.Action.Move ? PlayerAnimator.Action.Move : PlayerAnimator.Action.Idle;
            float time = 0f;
            var body = playerAnimator.BodyAnimator;
            if (body != null && body.runtimeAnimatorController != null)
                time = Mathf.Repeat(body.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

            Play(piece, Animator.StringToHash(PlayerAnimator.StateName(action, playerAnimator.CurrentFacing)), time);
        }

        // Extremidades y gorrito no tienen esquiva: durante la esquiva están ocultos.
        void OnBodyStateChanged(PlayerAnimator.Action action, PlayerAnimator.Facing facing)
        {
            if (action == PlayerAnimator.Action.Dodge)
                return;

            int state = Animator.StringToHash(PlayerAnimator.StateName(action, facing));
            Play(limbs, state);
            Play(cap, state);
        }

        void OnFired(Vector2 direction)
        {
            if (!MouthOnHead && Play(mouth, AttackState))
                mouthBusyUntil = Time.time + ClipLength(mouth, "Attack");

            if (mouthMutation != null && mouthMutation.AttackLean > 0f)
            {
                leanDirection = direction.normalized;
                leanAmount = mouthMutation.AttackLean;
                leanStart = Time.time;
            }
        }

        /// <summary>
        /// Adelanto al atacar: el cuerpo y sus piezas van hacia el cursor y vuelven; el gorrito se aplasta al
        /// adelantarse y se hincha al soltar las esporas.
        /// </summary>
        void UpdateLean()
        {
            if (leanStart < 0f)
                return;

            float t = (Time.time - leanStart) / leanDuration;
            if (t >= 1f)
            {
                EndLean();
                return;
            }

            float reach = t < LeanPeak ? t / LeanPeak : 1f - (t - LeanPeak) / (1f - LeanPeak);
            Vector2 offset = leanDirection * (leanAmount * reach);
            // En píxeles enteros del arte, para que no tiemble.
            offset = new Vector2(Mathf.Round(offset.x * PixelsPerUnit), Mathf.Round(offset.y * PixelsPerUnit)) / PixelsPerUnit;

            CacheLeaners();
            for (int i = 0; i < leaners.Length; i++)
            {
                if (leaners[i] != null)
                    leaners[i].localPosition = leanerBases[i] + (Vector3)offset;
            }

            // Se aplasta hasta 0,75 en el punto más adelantado, se hincha hasta 1,2 y vuelve.
            float stretch = t < LeanPeak
                ? Mathf.Lerp(1f, 0.75f, t / LeanPeak)
                : t < 0.6f
                    ? Mathf.Lerp(0.75f, 1.2f, (t - LeanPeak) / (0.6f - LeanPeak))
                    : Mathf.Lerp(1.2f, 1f, (t - 0.6f) / 0.4f);
            PlaceCap(offset, 1f + (1f - stretch) * 0.6f, stretch);
        }

        void EndLean()
        {
            if (leanStart < 0f)
                return;

            leanStart = -1f;
            if (leaners != null)
            {
                for (int i = 0; i < leaners.Length; i++)
                {
                    if (leaners[i] != null)
                        leaners[i].localPosition = leanerBases[i];
                }
            }
            PlaceCap(Vector2.zero, 1f, 1f);
        }

        /// <summary>Gorrito sobre la cabeza de la Piel, escalado desde su base.</summary>
        void PlaceCap(Vector2 offset, float scaleX, float scaleY)
        {
            if (cap == null)
                return;

            cap.transform.localPosition = new Vector3(offset.x, headHeight + offset.y + capBase * (1f - scaleY), 0f);
            cap.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        void CacheLeaners()
        {
            if (leaners != null)
                return;

            leaners = new[]
            {
                playerAnimator.Body != null ? playerAnimator.Body.transform : null,
                limbs != null ? limbs.transform : null,
                organ != null ? organ.transform : null,
                mouth != null ? mouth.transform.parent : null,
            };
            leanerBases = new Vector3[leaners.Length];
            for (int i = 0; i < leaners.Length; i++)
            {
                // AimPivot es el propio jugador si la Boca cuelga de él directamente: no se mueve.
                if (leaners[i] == transform)
                    leaners[i] = null;
                leanerBases[i] = leaners[i] != null ? leaners[i].localPosition : Vector3.zero;
            }
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

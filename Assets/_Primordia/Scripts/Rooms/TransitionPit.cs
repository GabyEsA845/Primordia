using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Fosa de la sala de paso: un agujero sólido (no se cae en él andando ni esquivando) con una luz que late y
    /// burbujas que suben. Desde cualquier punto del borde se pulsa Interactuar (E): la tecla aparece en el punto del
    /// borde más cercano al jugador, que salta hacia el centro. En el aire, sobre la fosa, se queda congelado y hay que
    /// elegir una de las Bocas del piso siguiente (<see cref="MouthChoiceUI"/>); luego cae y, si la fosa tiene
    /// <see cref="Metamorphosis"/>, se transforma en la criatura de esa Boca. Mientras no exista el piso siguiente, se
    /// acaba en la victoria.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TransitionPit : Interactable
    {
        const float PixelSize = 1f / 16f;

        [Header("Aspecto")]
        [SerializeField] SpriteRenderer pitRenderer;
        [Tooltip("Latido de la luz del fondo, en bucle.")]
        [SerializeField] Sprite[] pitFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] float pitFramesPerSecond = 6f;
        [Tooltip("Semiejes del borde de la fosa, en unidades: ahí se pone la tecla.")]
        [SerializeField] Vector2 edgeRadii = new(3.1f, 2.1f);

        [Header("Burbujas")]
        [SerializeField] Sprite[] bubbleFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.05f)] float bubbleInterval = 0.35f;
        [SerializeField, Min(0.1f)] float bubbleLifetime = 1.6f;
        [SerializeField, Min(0f)] float bubbleSpeed = 0.9f;
        [Tooltip("Los últimos cuadros de la hoja son el reventón (la burbuja no se desvanece: sube y revienta). 0 = solo se desvanece.")]
        [SerializeField, Min(0)] int bubblePopFrames;
        [SerializeField, Min(0.05f)] float bubblePopSeconds = 0.25f;

        [Header("Tecla")]
        [SerializeField] SpriteRenderer keyPrompt;
        [Tooltip("Tecla normal y pulsada.")]
        [SerializeField] Sprite[] keyFrames = new Sprite[2];
        [SerializeField] Color tooltipColor = new(0.45f, 0.75f, 0.95f);

        [Header("Salto")]
        [SerializeField, Min(0.05f)] float jumpDuration = 0.55f;
        [SerializeField, Min(0f)] float jumpHeight = 1.4f;
        [SerializeField, Min(0.05f)] float fallDuration = 0.7f;
        [Tooltip("Momento del salto (0 a 1) en que se congela en el aire para elegir la Boca.")]
        [SerializeField, Range(0f, 1f)] float choiceAt = 0.75f;

        public const string TooltipTitle = "Saltar al piso siguiente";
        public const string TooltipWarning = "Te dejas mutaciones del jefe";

        public bool Used { get; private set; }

        /// <summary>Bocas del piso siguiente, entre las que hay que elegir al saltar (las pone el generador del piso).</summary>
        public IReadOnlyList<MutationData> NextFloorMouths => nextFloorMouths;

        MutationData[] nextFloorMouths = System.Array.Empty<MutationData>();
        string nextFloorName = "";
        MutationData chosenMouth;

        /// <summary>La Boca elegida al saltar (null hasta elegir).</summary>
        public MutationData ChosenMouth => chosenMouth;
        public Vector2 EdgeRadii => edgeRadii;

        /// <summary>El jugador ha saltado a la fosa (al empezar el salto).</summary>
        public static event System.Action<TransitionPit> Jumped;

        /// <summary>La fosa del piso actual (null si no hay): la música se apaga al acercarse a ella.</summary>
        public static TransitionPit Current { get; private set; }

        readonly List<(SpriteRenderer renderer, float born)> bubbles = new();
        readonly Stack<SpriteRenderer> bubblePool = new();
        Transform player;
        PlayerInteractor interactor;
        ShopTooltip tooltip;
        float nextBubble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Jumped = null;
            Current = null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Current == this)
                Current = null;
        }

        internal void Configure(SpriteRenderer newPitRenderer, Sprite[] newPitFrames, Sprite[] newBubbleFrames,
            SpriteRenderer newKeyPrompt, Sprite[] newKeyFrames, Vector2 newEdgeRadii)
        {
            pitRenderer = newPitRenderer;
            pitFrames = newPitFrames ?? System.Array.Empty<Sprite>();
            bubbleFrames = newBubbleFrames ?? System.Array.Empty<Sprite>();
            keyPrompt = newKeyPrompt;
            keyFrames = newKeyFrames ?? System.Array.Empty<Sprite>();
            edgeRadii = newEdgeRadii;
        }

        /// <summary>Los últimos <paramref name="frames"/> cuadros de la hoja de burbujas son el reventón del final de su vida.</summary>
        internal void SetBubblePop(int frames) => bubblePopFrames = Mathf.Clamp(frames, 0, Mathf.Max(0, bubbleFrames.Length - 1));

        internal void SetNextFloor(MutationData[] mouths, string floorName)
        {
            nextFloorMouths = mouths ?? System.Array.Empty<MutationData>();
            nextFloorName = floorName ?? "";
        }

        internal void ConfigureJump(float newJumpDuration, float newFallDuration)
        {
            jumpDuration = Mathf.Max(0.05f, newJumpDuration);
            fallDuration = Mathf.Max(0.05f, newFallDuration);
        }

        public override bool IsAvailable => !Used;

        /// <summary>El punto del borde más cercano al jugador: la tecla sale ahí y desde ahí se mide la distancia.</summary>
        public override Vector2 Position => EdgePoint(PlayerPosition());

        /// <summary>Punto del borde (elipse de <see cref="edgeRadii"/>) en la dirección de <paramref name="from"/>.</summary>
        public Vector2 EdgePoint(Vector2 from)
        {
            Vector2 center = transform.position;
            var direction = from - center;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.down;
            // Dirección en el espacio de la elipse, para que el punto caiga justo en el borde.
            var scaled = new Vector2(direction.x / edgeRadii.x, direction.y / edgeRadii.y).normalized;
            return center + new Vector2(scaled.x * edgeRadii.x, scaled.y * edgeRadii.y);
        }

        void Awake()
        {
            if (keyPrompt != null)
                keyPrompt.enabled = false;
        }

        void Start() => tooltip = FindAnyObjectByType<ShopTooltip>(FindObjectsInactive.Include);

        Vector2 PlayerPosition()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                player = found != null ? found.transform : null;
            }
            return player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down * 10f;
        }

        void Update()
        {
            if (pitRenderer != null && pitFrames.Length > 0)
                pitRenderer.sprite = pitFrames[(int)(Time.time * pitFramesPerSecond) % pitFrames.Length];

            UpdateBubbles();

            if (keyPrompt != null && keyPrompt.enabled)
            {
                bool holding = interactor != null && interactor.IsHolding;
                if (keyFrames.Length > 1 && keyFrames[0] != null)
                    keyPrompt.sprite = holding && keyFrames[1] != null ? keyFrames[1] : keyFrames[0];
                float bob = Mathf.Sin(Time.time * 4f) > 0f ? PixelSize : 0f;
                keyPrompt.transform.position = Snap(Position + new Vector2(0f, 1.1f + bob));
            }
        }

        static Vector3 Snap(Vector2 position) =>
            new(Mathf.Round(position.x / PixelSize) * PixelSize, Mathf.Round(position.y / PixelSize) * PixelSize, 0f);

        void UpdateBubbles()
        {
            if (bubbleFrames.Length == 0)
                return;

            if (Time.time >= nextBubble)
            {
                nextBubble = Time.time + bubbleInterval * Random.Range(0.6f, 1.4f);
                var bubble = bubblePool.Count > 0 ? bubblePool.Pop() : CreateBubble();
                // Salen del fondo iluminado, algo por debajo del centro (la fosa se ve en 3/4).
                bubble.transform.position = (Vector2)transform.position
                    + new Vector2(Random.Range(-edgeRadii.x * 0.6f, edgeRadii.x * 0.6f), Random.Range(-1.1f, 0f));
                bubble.gameObject.SetActive(true);
                bubbles.Add((bubble, Time.time));
            }

            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var (renderer, born) = bubbles[i];
                float age = Time.time - born;
                if (age >= bubbleLifetime)
                {
                    renderer.gameObject.SetActive(false);
                    bubblePool.Push(renderer);
                    bubbles.RemoveAt(i);
                    continue;
                }

                float untilPop = bubbleLifetime - age;
                bool popping = bubblePopFrames > 0 && untilPop <= bubblePopSeconds;
                if (popping)
                {
                    // Revienta donde está: se queda quieta y pasa los cuadros del reventón.
                    int step = Mathf.Min(bubblePopFrames - 1, Mathf.FloorToInt((1f - untilPop / bubblePopSeconds) * bubblePopFrames));
                    renderer.sprite = bubbleFrames[bubbleFrames.Length - bubblePopFrames + step];
                    renderer.color = Color.white;
                    continue;
                }

                renderer.transform.position += new Vector3(Mathf.Sin((age + born) * 5f) * 0.004f, bubbleSpeed * Time.deltaTime, 0f);
                int rising = bubbleFrames.Length - bubblePopFrames;
                renderer.sprite = bubbleFrames[(int)(age * 4f) % rising];
                renderer.color = bubblePopFrames > 0 ? Color.white : new Color(1f, 1f, 1f, 1f - age / bubbleLifetime);
            }
        }

        SpriteRenderer CreateBubble()
        {
            var bubble = new GameObject("Bubble").AddComponent<SpriteRenderer>();
            bubble.transform.SetParent(transform, true);
            bubble.sortingOrder = pitRenderer != null ? pitRenderer.sortingOrder + 1 : -11;
            return bubble;
        }

        protected override void OnFocusChanged(bool focused, GameObject focusPlayer)
        {
            interactor = focused && focusPlayer != null ? focusPlayer.GetComponent<PlayerInteractor>() : null;
            if (keyPrompt != null)
                keyPrompt.enabled = focused;

            if (tooltip == null)
                return;
            if (focused)
                tooltip.Show(TooltipTitle, "", "", LeavesMutations() ? TooltipWarning : "", tooltipColor);
            else
                tooltip.Hide();
        }

        /// <summary>True si quedan burbujas de mutación en el suelo (las que deja el jefe).</summary>
        static bool LeavesMutations() => FindAnyObjectByType<MutationPickup>() != null;

        public override void Interact(GameObject jumper)
        {
            if (Used)
                return;

            Used = true;
            SetFocused(false, jumper);
            // La forma nueva no se ve hasta la metamorfosis, aunque la Boca se cambie al elegirla.
            if (jumper.TryGetComponent<MutationLook>(out var look))
                look.HoldForm();
            Jumped?.Invoke(this);
            StartCoroutine(Jump(jumper));
        }

        IEnumerator Jump(GameObject jumper)
        {
            var manager = GameManager.Instance;
            // La Boca con la que saltas: de su cuerpo parte la silueta de la metamorfosis (la elección la cambia).
            var previousMouth = jumper.TryGetComponent<PlayerMutations>(out var jumperMutations) ? jumperMutations.Get(MutationSlot.Mouth) : null;
            if (manager != null)
                manager.SetState(GameState.Cinematic);
            Sfx.Play(Sounds.TransitionJump);

            // Sin física mientras salta: así cruza el borde sólido de la fosa.
            var body = jumper.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            var visual = jumper.transform;
            Vector2 start = visual.position;
            Vector2 center = transform.position;
            var baseScale = visual.localScale;

            // Salto en arco hasta el centro, algo estirado en el aire. Casi al final, sobre la fosa, se congela para
            // elegir la Boca del piso siguiente.
            bool chose = false;
            for (float t = 0f; t < jumpDuration; t += Time.deltaTime)
            {
                float k = t / jumpDuration;
                visual.position = Vector2.Lerp(start, center, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * jumpHeight);
                float stretch = 1f + Mathf.Sin(k * Mathf.PI) * 0.15f;
                visual.localScale = new Vector3(baseScale.x / stretch, baseScale.y * stretch, baseScale.z);

                if (!chose && k >= choiceAt)
                {
                    chose = true;
                    yield return ChooseMouth(jumper);
                    if (manager != null)
                        manager.SetState(GameState.Cinematic);
                }
                yield return null;
            }
            if (!chose)
                yield return ChooseMouth(jumper);

            var metamorphosis = GetComponent<Metamorphosis>();
            if (metamorphosis != null && chosenMouth != null)
            {
                visual.localScale = baseScale;
                yield return metamorphosis.Play(jumper, chosenMouth, previousMouth);
                LeaveFloor(manager);
                yield break;
            }

            if (jumper.TryGetComponent<MutationLook>(out var look))
                look.ReleaseForm();

            // Cae: se hunde y se hace pequeño hacia el fondo de la fosa.
            Sfx.Play(Sounds.TransitionFall);
            for (float t = 0f; t < fallDuration; t += Time.deltaTime)
            {
                float k = t / fallDuration;
                visual.position = center + Vector2.down * (k * k * 1.2f);
                visual.localScale = baseScale * Mathf.Lerp(1f, 0.05f, k * k);
                yield return null;
            }
            visual.localScale = Vector3.zero;

            LeaveFloor(manager);
        }

        /// <summary>
        /// Al piso siguiente, que el generador monta en esta misma escena (esta fosa se borra con el piso); si no hay
        /// piso siguiente, la victoria.
        /// </summary>
        static void LeaveFloor(GameManager manager)
        {
            var generator = FindAnyObjectByType<FloorGenerator>();
            if (generator != null && generator.HasNextFloor)
                generator.GoToNextFloor();
            else if (manager != null)
                manager.SetState(GameState.Victory);
        }

        /// <summary>Abre la ventana de Bocas y espera a que acabe el reparto. Sin Bocas o sin ventana, no hace nada.</summary>
        IEnumerator ChooseMouth(GameObject jumper)
        {
            var mutations = jumper.GetComponent<PlayerMutations>();
            var window = FindAnyObjectByType<MouthChoiceUI>(FindObjectsInactive.Include);
            if (nextFloorMouths.Length == 0 || mutations == null || window == null)
                yield break;

            bool done = false;
            window.Show(nextFloorMouths, mutations, nextFloorName, () => done = true);
            while (!done)
                yield return null;
            chosenMouth = window.Chosen;
        }
    }
}

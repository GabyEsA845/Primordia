using System.Collections;
using System.Linq;
using Primordia.Audio;
using Primordia.Biomass;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>
    /// Almeja gigante (el cofre del piso 2): respira con un brillo suave y, con el jugador cerca, enseña "E para abrir". Al
    /// abrirla suelta su biomasa (que vuela hacia el jugador) y, la grande, una mutación especial si el piso tiene alguna.
    /// Se queda abierta y bloquea el paso como antes.
    /// </summary>
    public class Clam : PromptInteractable
    {
        const float IdleFrameDuration = 0.18f;
        const float OpenFrameDuration = 0.08f;
        static readonly Color GlowColor = new(1f, 0.85f, 0.55f);

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] open = System.Array.Empty<Sprite>();
        [SerializeField] Sprite opened;
        [SerializeField] bool big;
        [SerializeField] Vector2Int biomass = new(5, 15);
        [SerializeField] MutationData mutation;
        [SerializeField] MutationPickup mutationPickup;
        [SerializeField] BiomassPickup smallBiomass;
        [SerializeField] BiomassPickup bigBiomass;

        Light2D glow;
        float phase;

        public bool IsOpen { get; private set; }
        public bool IsBig => big;
        public MutationData Mutation => mutation;

        public override bool IsAvailable => !IsOpen;

        protected override string TooltipTitle => big ? "Almeja gigante" : "Almeja";
        protected override string TooltipDetails => "E para abrir";
        protected override Color TooltipColor => GlowColor;

        internal void Configure(SpriteRenderer newBody, Sprite[] newIdle, Sprite[] newOpen, Sprite newOpened, bool newBig,
            Vector2Int newBiomass, MutationData newMutation, MutationPickup newPickup, BiomassPickup newSmallPickup, BiomassPickup newBigPickup)
        {
            body = newBody;
            idle = newIdle ?? System.Array.Empty<Sprite>();
            open = newOpen ?? System.Array.Empty<Sprite>();
            opened = newOpened;
            big = newBig;
            biomass = newBiomass;
            mutation = newMutation;
            mutationPickup = newPickup;
            smallBiomass = newSmallPickup;
            bigBiomass = newBigPickup;
        }

        internal void SetGlow(Light2D light) => glow = light;

        void Awake() => phase = Random.value * 10f;

        void Update()
        {
            if (IsOpen || body == null || idle.Length == 0)
                return;
            int frame = Mathf.FloorToInt((Time.time + phase) / IdleFrameDuration) % idle.Length;
            body.sprite = idle[frame];
        }

        public override void Interact(GameObject player)
        {
            if (IsOpen)
                return;
            IsOpen = true;
            HidePrompt();
            StartCoroutine(Open(player));
        }

        IEnumerator Open(GameObject player)
        {
            Sfx.Play(Sounds.MutationBubbleSpawn);
            foreach (var frame in open)
            {
                if (body != null)
                    body.sprite = frame;
                yield return new WaitForSeconds(OpenFrameDuration);
            }
            if (body != null && opened != null)
                body.sprite = opened;
            if (glow != null)
                glow.enabled = false;

            Release(player);
        }

        void Release(GameObject player)
        {
            var origin = Position + Vector2.up * 0.6f;
            int value = Random.Range(Mathf.Min(biomass.x, biomass.y), Mathf.Max(biomass.x, biomass.y) + 1);
            var (bigCount, smallCount) = RoomBiomass.Split(value);
            for (int i = 0; i < bigCount + smallCount; i++)
            {
                var prefab = i < bigCount ? bigBiomass : smallBiomass;
                if (prefab == null)
                    continue;
                var pickup = Instantiate(prefab, origin, Quaternion.identity, transform.parent);
                pickup.Pop();
                pickup.Attract();
            }

            if (mutation != null && mutationPickup != null)
            {
                var bubble = Instantiate(mutationPickup, origin, Quaternion.identity, transform.parent);
                bubble.SetMutation(mutation);
                StartCoroutine(Drop(bubble, DropTarget(player)));
            }
        }

        const float DropDistance = 1.2f;
        const float DropDuration = 0.45f;
        const float DropArc = 0.9f;

        /// <summary>
        /// Dónde cae la burbuja: delante de la almeja, hacia el jugador que la abrió (normalmente abajo). Ese lado es suelo que
        /// pisa (en el Anillo de la sima, la isla), y como queda casi encima de él, la coge sin buscarla. Sin jugador, abajo.
        /// </summary>
        Vector2 DropTarget(GameObject player)
        {
            var toPlayer = player != null ? (Vector2)player.transform.position - Position : Vector2.down;
            var direction = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector2.down;
            return Position + direction * Mathf.Min(DropDistance, Mathf.Max(0.6f, toPlayer.magnitude));
        }

        /// <summary>La burbuja salta de la almeja en un arco corto y no se puede coger hasta que cae.</summary>
        IEnumerator Drop(MutationPickup bubble, Vector2 target)
        {
            var trigger = bubble.GetComponent<Collider2D>();
            if (trigger != null)
                trigger.enabled = false;

            Vector2 start = bubble.transform.position;
            for (float t = 0f; t < 1f && bubble != null; t += Time.deltaTime / DropDuration)
            {
                var position = Vector2.Lerp(start, target, t) + Vector2.up * (4f * DropArc * t * (1f - t));
                bubble.transform.position = position;
                yield return null;
            }

            if (bubble == null)
                yield break;
            bubble.transform.position = target;
            if (trigger != null)
                trigger.enabled = true;
        }

        /// <summary>
        /// Mutación especial para una almeja grande: una al azar de la lista que el jugador no lleve y que no guarde ya otra
        /// almeja del piso (<paramref name="assigned"/>); si ya están todas repartidas, puede repetir. Null si no hay.
        /// </summary>
        public static MutationData PickMutation(MutationData[] options, PlayerMutations player, System.Random random,
            System.Collections.Generic.ICollection<MutationData> assigned = null)
        {
            var free = (options ?? System.Array.Empty<MutationData>()).Where(m => m != null && (player == null || !player.IsEquipped(m))).ToList();
            var fresh = assigned == null ? free : free.Where(m => !assigned.Contains(m)).ToList();
            var pool = fresh.Count > 0 ? fresh : free;
            return pool.Count > 0 ? pool[random.Next(pool.Count)] : null;
        }

        /// <summary>Mutaciones especiales que guardan las almejas que hay ya en la escena (las del piso que se está montando).</summary>
        public static System.Collections.Generic.HashSet<MutationData> AssignedMutations() =>
            new(FindObjectsByType<Clam>(FindObjectsSortMode.None).Select(c => c.Mutation).Where(m => m != null));
    }
}

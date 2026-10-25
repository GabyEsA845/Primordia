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
                var bubble = Instantiate(mutationPickup, origin + Vector2.up * 1.2f, Quaternion.identity, transform.parent);
                bubble.SetMutation(mutation);
            }
        }

        /// <summary>Mutación especial para una almeja grande: una al azar de la lista que el jugador no lleve. Null si no hay.</summary>
        public static MutationData PickMutation(MutationData[] options, PlayerMutations player, System.Random random)
        {
            var free = (options ?? System.Array.Empty<MutationData>()).Where(m => m != null && (player == null || !player.IsEquipped(m))).ToList();
            return free.Count > 0 ? free[random.Next(free.Count)] : null;
        }
    }
}

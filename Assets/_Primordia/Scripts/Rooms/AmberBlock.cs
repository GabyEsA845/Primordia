using System.Collections;
using Primordia.Audio;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>
    /// Bloque de ámbar con una cucaracha dentro (sala del ámbar del piso 3): brilla y, con el jugador cerca, enseña "E para romper".
    /// Al romperlo se agrieta y salta en esquirlas, y en el cuadro en que aparecen suelta la mutación especial que guarda (una
    /// burbuja que salta hacia el jugador, como la almeja grande). Una vez roto ya no estorba.
    /// </summary>
    public class AmberBlock : PromptInteractable
    {
        const float IdleFrameDuration = 0.2f;
        const float BreakFrameDuration = 0.09f;
        // Cuadro de la rotura (empezando en 0) en el que aparece la mutación: "el cuadro que cuenta: 3".
        const int ReleaseFrame = 2;
        static readonly Color GlowColor = new(1f, 0.72f, 0.25f);

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] shatter = System.Array.Empty<Sprite>();
        [SerializeField] MutationData mutation;
        [SerializeField] MutationPickup mutationPickup;

        Light2D glow;
        float phase;

        public bool IsBroken { get; private set; }
        public MutationData Mutation => mutation;

        public override bool IsAvailable => !IsBroken;

        protected override string TooltipTitle => "Bloque de ámbar";
        protected override string TooltipDetails => "E para romperlo";
        protected override Color TooltipColor => GlowColor;

        internal void Configure(SpriteRenderer newBody, Sprite[] newIdle, Sprite[] newShatter, MutationData newMutation, MutationPickup newPickup)
        {
            body = newBody;
            idle = newIdle ?? System.Array.Empty<Sprite>();
            shatter = newShatter ?? System.Array.Empty<Sprite>();
            mutation = newMutation;
            mutationPickup = newPickup;
        }

        /// <summary>Lo que guarda el bloque (el prefab ya lleva el cuerpo y los cuadros).</summary>
        internal void SetContents(MutationData newMutation, MutationPickup newPickup)
        {
            mutation = newMutation;
            mutationPickup = newPickup;
        }

        internal void SetGlow(Light2D light) => glow = light;

        void Awake() => phase = Random.value * 10f;

        void Update()
        {
            if (IsBroken || body == null || idle.Length == 0)
                return;
            body.sprite = idle[Mathf.FloorToInt((Time.time + phase) / IdleFrameDuration) % idle.Length];
        }

        public override void Interact(GameObject player)
        {
            if (IsBroken)
                return;
            IsBroken = true;
            HidePrompt();
            StartCoroutine(Break(player));
        }

        IEnumerator Break(GameObject player)
        {
            Sfx.Play(Sounds.ClamOpenBig);
            if (glow != null)
                glow.enabled = false;
            for (int i = 0; i < shatter.Length; i++)
            {
                if (body != null)
                    body.sprite = shatter[i];
                if (i == ReleaseFrame)
                    Release(player);
                yield return new WaitForSeconds(BreakFrameDuration);
            }
            if (shatter.Length <= ReleaseFrame)
                Release(player);

            // Roto, ya no bloquea el paso.
            foreach (var collider in GetComponents<Collider2D>())
                collider.enabled = false;
        }

        void Release(GameObject player)
        {
            if (mutation == null || mutationPickup == null)
                return;
            var origin = Position + Vector2.up * 0.4f;
            var bubble = Instantiate(mutationPickup, origin, Quaternion.identity, transform.parent);
            bubble.SetMutation(mutation);
            StartCoroutine(Clam.DropBubble(bubble, Clam.DropTarget(Position, player)));
        }
    }
}

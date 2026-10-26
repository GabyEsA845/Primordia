using Primordia.Audio;
using Primordia.Mutations;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// La cápsula del laboratorio de Darwin, en el centro de la sala de cambio (desde el piso 2, detrás de la tienda): con
    /// el jugador cerca enseña "E"; al usarla se abre su ventana (<see cref="ExchangeUI"/>) para entregar la mutación de una
    /// ranura y recibir otra del piso (<see cref="MutationExchange"/>). Gratis y una sola vez por piso: después el líquido
    /// baja y se apaga.
    /// </summary>
    public class ExchangeCapsule : PromptInteractable
    {
        static readonly Color GlowColor = new(0.45f, 0.95f, 0.65f);

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] used = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float idleFrameDuration = 0.14f;
        [SerializeField, Min(0.01f)] float usedFrameDuration = 0.26f;
        [Tooltip("Las mutaciones que ofrece: las del piso (el generador pone la reserva de su piso).")]
        [SerializeField] MutationPool pool;
        [SerializeField, Min(1)] int floor = 2;

        public bool Used { get; private set; }
        public MutationPool Pool => pool;
        public int Floor => floor;

        /// <summary>Se lanza tras un cambio, con la mutación recibida.</summary>
        public event System.Action<MutationData> Exchanged;

        public override bool IsAvailable => !Used;

        protected override string TooltipTitle => "Cápsula de Darwin";
        protected override string TooltipDetails => "E para cambiar una mutación (gratis, una vez por piso)";
        protected override Color TooltipColor => GlowColor;

        internal void Configure(SpriteRenderer newBody, Sprite[] newIdle, Sprite[] newUsed)
        {
            body = newBody;
            idle = newIdle ?? System.Array.Empty<Sprite>();
            used = newUsed ?? System.Array.Empty<Sprite>();
        }

        /// <summary>Lo que ofrece: la reserva y el número de su piso.</summary>
        internal void SetFloor(MutationPool newPool, int newFloor)
        {
            if (newPool != null)
                pool = newPool;
            floor = Mathf.Max(1, newFloor);
        }

        void Update()
        {
            var frames = Used ? used : idle;
            if (body == null || frames.Length == 0)
                return;
            float duration = Used ? usedFrameDuration : idleFrameDuration;
            body.sprite = frames[Mathf.FloorToInt(Time.time / duration) % frames.Length];
        }

        public override void Interact(GameObject player)
        {
            if (Used || player == null)
                return;
            var mutations = player.GetComponent<PlayerMutations>();
            var window = FindAnyObjectByType<ExchangeUI>(FindObjectsInactive.Include);
            if (mutations == null || window == null)
                return;
            Sfx.Play(Sounds.MutationChoiceOpen);
            window.Show(mutations, pool, floor, received => Exchange(mutations, received), null);
        }

        /// <summary>Hace el cambio (sin ventana: también lo usan las pruebas) y deja la cápsula gastada.</summary>
        public void Exchange(PlayerMutations player, MutationData received)
        {
            if (Used || player == null || received == null)
                return;
            var look = player.GetComponent<MutationLook>();
            bool newBody = MutationExchange.ChangesBody(player, received);
            if (newBody && look != null)
                look.HoldForm();
            if (MutationExchange.Apply(player, received, floor) == 0)
            {
                if (newBody && look != null)
                    look.ReleaseForm();
                return;
            }
            if (newBody && look != null)
                look.ReleaseForm();
            Used = true;
            HidePrompt();
            Sfx.Play(Sounds.MutationGain);
            Exchanged?.Invoke(received);
        }
    }
}

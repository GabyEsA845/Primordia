using Primordia.Audio;
using Primordia.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>
    /// Coral de la sala del Corazón morado: el corazón flota y late encima con un brillo morado. Con E se coge: para un golpe
    /// entero (antes que nada), se gasta con él y el jugador lo conserva entre pisos (<see cref="Health.PurpleHearts"/>).
    /// </summary>
    public class HeartCoral : PromptInteractable
    {
        const float FrameDuration = 0.12f;
        static readonly Color Purple = new(0.75f, 0.45f, 1f);

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite taken;

        Light2D glow;

        public bool Taken { get; private set; }

        public override bool IsAvailable => !Taken;

        protected override string TooltipTitle => "Corazón morado";
        protected override string TooltipDetails => "Para un golpe entero y se conserva entre pisos. E para coger";
        protected override Color TooltipColor => Purple;

        internal void Configure(SpriteRenderer newBody, Sprite[] newIdle, Sprite newTaken)
        {
            body = newBody;
            idle = newIdle ?? System.Array.Empty<Sprite>();
            taken = newTaken;
        }

        internal void SetGlow(Light2D light) => glow = light;

        void Update()
        {
            if (Taken || body == null || idle.Length == 0)
                return;
            body.sprite = idle[Mathf.FloorToInt(Time.time / FrameDuration) % idle.Length];
        }

        public override void Interact(GameObject player)
        {
            if (Taken || player == null || !player.TryGetComponent<Health>(out var health))
                return;
            Taken = true;
            HidePrompt();
            health.AddPurpleHeart();
            Sfx.Play(Sounds.MutationBubbleSpawn);
            if (body != null && taken != null)
                body.sprite = taken;
            if (glow != null)
                glow.enabled = false;
        }
    }
}

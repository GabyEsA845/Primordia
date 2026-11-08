using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Recompensa PROVISIONAL de la cámara de la sala secreta (piso 3): un corazón morado que flota; con E da +1 corazón de vida máxima
    /// (curando ese corazón). Hasta que exista el jefe secreto (paso 7), que dará el corazón, la Espora morada y la reliquia en su cámara.
    /// </summary>
    public class SecretReward : PromptInteractable
    {
        const float FrameSeconds = 0.12f;
        static readonly Color Purple = new(0.75f, 0.45f, 1f);

        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();

        public bool Taken { get; private set; }

        public override bool IsAvailable => !Taken;

        protected override string TooltipTitle => "Corazón de la raíz ancestral";

        protected override string TooltipDetails => "+1 corazón de vida máxima. E para coger";

        protected override Color TooltipColor => Purple;

        internal static SecretReward Create(Transform parent, Vector2 position, Sprite[] frames, Sprite[] keyFrames)
        {
            var go = new GameObject("SecretReward");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var reward = go.AddComponent<SecretReward>();
            reward.idle = frames ?? System.Array.Empty<Sprite>();
            reward.body = go.AddComponent<SpriteRenderer>();
            reward.body.sortingOrder = 5;
            reward.body.sprite = reward.idle.Length > 0 ? reward.idle[0] : null;
            reward.ConfigurePrompt(keyFrames, 1.4f);
            reward.SetRadius(1.8f);
            return reward;
        }

        void Update()
        {
            if (!Taken && idle.Length > 0)
                body.sprite = idle[Mathf.FloorToInt(Time.time / FrameSeconds) % idle.Length];
        }

        public override void Interact(GameObject player)
        {
            if (Taken || player == null || !player.TryGetComponent<Health>(out var health))
                return;
            Taken = true;
            HidePrompt();
            // Un corazón entero son dos medios; SetMax cura la diferencia.
            health.SetMax(health.Max + 2);
            Sfx.Play(Sounds.HeartCoral);
            body.enabled = false;
        }
    }
}

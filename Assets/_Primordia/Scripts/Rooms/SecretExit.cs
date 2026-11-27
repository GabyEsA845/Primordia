using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Salida de la arena del jefe secreto: con E se vuelve a la antesala (<see cref="SecretPassage"/>).</summary>
    public class SecretExit : PromptInteractable
    {
        const float FrameSeconds = 0.13f;

        SecretPassage passage;
        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();

        public override bool IsAvailable => passage != null && !passage.Travelling;

        protected override string TooltipTitle => "Salida";

        protected override string TooltipDetails => "E para volver a la antesala";

        internal static SecretExit Create(Transform parent, SecretPassage passage, Vector2 position, Sprite[] frames, Sprite[] keyFrames)
        {
            var go = new GameObject("SecretExit");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            // Apagada hasta que la arena queda limpia.
            go.SetActive(false);
            var exit = go.AddComponent<SecretExit>();
            exit.passage = passage;
            exit.idle = frames ?? System.Array.Empty<Sprite>();
            exit.body = go.AddComponent<SpriteRenderer>();
            exit.body.sortingOrder = 4;
            exit.body.sprite = exit.idle.Length > 0 ? exit.idle[0] : null;
            exit.ConfigurePrompt(keyFrames, 1.8f);
            exit.SetRadius(1.6f);
            return exit;
        }

        void Update()
        {
            if (idle.Length > 0)
                body.sprite = idle[Mathf.FloorToInt(Time.time / FrameSeconds) % idle.Length];
        }

        public override void Interact(GameObject player) => passage?.Return(player);
    }
}

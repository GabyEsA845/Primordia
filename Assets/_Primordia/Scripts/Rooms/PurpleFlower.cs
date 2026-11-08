using System.Collections;
using Primordia.Audio;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Flor morada del camino secreto (piso 3): una de las tres que hay repartidas por el piso cuando el corazón morado se rompió. Se
    /// mece con un halo que late; con E se coge y estalla en chispas moradas. Con las tres, el jugador tiene la semilla morada.
    /// </summary>
    public class PurpleFlower : PromptInteractable
    {
        const float IdleFrameSeconds = 0.22f;
        const float PickFrameSeconds = 0.08f;
        static readonly Color Purple = new(0.75f, 0.45f, 1f);

        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();
        Sprite[] pick = System.Array.Empty<Sprite>();
        PurpleTrail trail;
        float phase;
        bool taken;

        public override bool IsAvailable => !taken;

        protected override string TooltipTitle => "Flor morada";

        protected override string TooltipDetails => trail != null ? $"{trail.Taken}/{PurpleTrail.FlowerCount} · E para coger" : "E para coger";

        protected override Color TooltipColor => Purple;

        internal static PurpleFlower Create(Transform parent, Vector2 foot, SpecialRoomArt art, PurpleTrail trail, Sprite[] keyFrames)
        {
            var go = new GameObject("PurpleFlower");
            go.transform.SetParent(parent, false);
            go.transform.position = foot;
            var flower = go.AddComponent<PurpleFlower>();
            flower.idle = art.purpleFlower;
            flower.pick = art.purpleFlowerPick;
            flower.trail = trail;
            flower.phase = Random.value * 5f;
            flower.body = go.AddComponent<SpriteRenderer>();
            flower.body.sortingOrder = 2;
            flower.body.sprite = flower.idle.Length > 0 ? flower.idle[0] : null;
            flower.ConfigurePrompt(keyFrames, 1.4f);
            flower.SetRadius(1.3f);
            return flower;
        }

        void Update()
        {
            if (!taken && idle.Length > 0)
                body.sprite = idle[Mathf.FloorToInt((Time.time + phase) / IdleFrameSeconds) % idle.Length];
        }

        public override void Interact(GameObject player)
        {
            if (taken)
                return;
            taken = true;
            HidePrompt();
            Sfx.Play(Sounds.HeartCoral);
            trail?.OnTaken(player);
            StartCoroutine(Burst());
        }

        IEnumerator Burst()
        {
            foreach (var frame in pick)
            {
                body.sprite = frame;
                yield return new WaitForSeconds(PickFrameSeconds);
            }
            Destroy(gameObject);
        }
    }
}

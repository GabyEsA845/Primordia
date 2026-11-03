using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Marca chamuscada bajo cada fuego del Yesquero (el rastro de la bola y el reventón): aparece (cuadros 0-1), titila mientras arde su
    /// fuego (2-3, en bucle) y, cuando el fuego se apaga, las ascuas se apagan y el hollín se desvanece (4-7), tras lo cual se destruye.
    /// Va por debajo del fuego (orden menor que el suyo, 6) y por encima del suelo. Un fuego que se alarga alarga también su marca porque
    /// la sigue mientras <see cref="GroundFire.IsBurning"/>. Una sola marca por fuego.
    /// </summary>
    public class TinderScorch : MonoBehaviour
    {
        const float AppearFrameSeconds = 0.09f;
        const float EmberFrameSeconds = 0.2f;
        const float FadeFrameSeconds = 0.35f;
        const int Order = 5;

        enum Phase { Appear, Ember, Fade }

        static readonly Dictionary<GroundFire, TinderScorch> marks = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => marks.Clear();

        GroundFire fire;
        SpriteRenderer body;
        Sprite[] frames;
        Phase phase;
        float phaseStart;

        public bool IsFading => phase == Phase.Fade;

        /// <summary>Pone la marca bajo ese fuego (si ya la tiene, la misma). Null si no hay fuego o no hay arte de la marca.</summary>
        public static TinderScorch Attach(GroundFire fire)
        {
            if (fire == null)
                return null;
            if (marks.TryGetValue(fire, out var existing) && existing != null)
                return existing;

            var kit = ReactionKit.Current;
            if (kit == null || !kit.HasTinderScorch)
                return null;

            // Una de las variantes (8 cuadros cada una), al azar.
            int variants = kit.TinderScorch.Length / ReactionKit.ScorchFrames;
            int first = Random.Range(0, variants) * ReactionKit.ScorchFrames;
            var obj = new GameObject("TinderScorch");
            obj.transform.position = fire.transform.position;
            var scorch = obj.AddComponent<TinderScorch>();
            scorch.fire = fire;
            scorch.frames = new Sprite[ReactionKit.ScorchFrames];
            System.Array.Copy(kit.TinderScorch, first, scorch.frames, 0, ReactionKit.ScorchFrames);
            scorch.body = obj.AddComponent<SpriteRenderer>();
            scorch.body.sortingOrder = Order;
            scorch.phaseStart = Time.time;
            scorch.Show();
            marks[fire] = scorch;
            return scorch;
        }

        void Update()
        {
            float elapsed = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Appear:
                    if (!Burning())
                        Enter(Phase.Fade);
                    else if (elapsed >= 2f * AppearFrameSeconds)
                        Enter(Phase.Ember);
                    break;

                case Phase.Ember:
                    if (!Burning())
                        Enter(Phase.Fade);
                    break;

                case Phase.Fade:
                    if (elapsed >= 4f * FadeFrameSeconds)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    break;
            }
            Show();
        }

        bool Burning() => fire != null && fire.IsBurning;

        void Enter(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
        }

        void Show()
        {
            float elapsed = Time.time - phaseStart;
            int index = phase switch
            {
                Phase.Appear => Mathf.Min(1, Mathf.FloorToInt(elapsed / AppearFrameSeconds)),
                Phase.Ember => 2 + Mathf.FloorToInt(elapsed / EmberFrameSeconds) % 2,
                _ => 4 + Mathf.Min(3, Mathf.FloorToInt(elapsed / FadeFrameSeconds)),
            };
            body.sprite = frames[index];
        }

        void OnDestroy()
        {
            if (fire != null)
                marks.Remove(fire);
        }
    }
}

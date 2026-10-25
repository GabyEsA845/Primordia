using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Repite unos cuadros de sprite en bucle (rayos de luz).</summary>
    public class SpriteLoop : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.01f)] float frameDuration = 0.25f;
        [SerializeField] float phase;

        public Sprite[] Frames => frames;

        internal void Configure(SpriteRenderer newTarget, Sprite[] newFrames, float newFrameDuration, float newPhase)
        {
            target = newTarget;
            frames = newFrames;
            frameDuration = Mathf.Max(0.01f, newFrameDuration);
            phase = newPhase;
        }

        void Update()
        {
            if (target == null || frames == null || frames.Length == 0)
                return;
            int frame = Mathf.FloorToInt((Time.time + phase) / frameDuration) % frames.Length;
            target.sprite = frames[(frame + frames.Length) % frames.Length];
        }
    }
}

using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Efecto de una hoja de cuadros que se reproduce una vez en su SpriteRenderer y se destruye al terminar.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OneShotSprite : MonoBehaviour
    {
        Sprite[] frames;
        float step, started;

        public void Play(Sprite[] newFrames, float newStep)
        {
            frames = newFrames;
            step = Mathf.Max(0.01f, newStep);
            started = Time.time;
            GetComponent<SpriteRenderer>().sprite = frames[0];
        }

        void Update()
        {
            if (frames == null)
                return;
            int frame = Mathf.FloorToInt((Time.time - started) / step);
            if (frame >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            GetComponent<SpriteRenderer>().sprite = frames[frame];
        }
    }
}

using UnityEngine;

namespace Primordia.Core
{
    /// <summary>Reproduce unos cuadros de sprite en un SpriteRenderer, en bucle o una sola vez (y se destruye al acabar).</summary>
    public class SpriteFramePlayer : MonoBehaviour
    {
        Sprite[] frames = System.Array.Empty<Sprite>();
        float frameSeconds = 0.1f;
        bool loop;
        float start;
        SpriteRenderer target;

        /// <summary>Crea un objeto suelto que reproduce los cuadros una vez en esa posición y se destruye.</summary>
        public static SpriteFramePlayer PlayOnce(Sprite[] frames, float frameSeconds, Vector2 position, int sortingOrder, float scale = 1f)
        {
            if (frames == null || frames.Length == 0)
                return null;

            var go = new GameObject("FramePlayer");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            var player = go.AddComponent<SpriteFramePlayer>();
            player.Play(renderer, frames, frameSeconds, newLoop: false);
            return player;
        }

        public void Play(SpriteRenderer renderer, Sprite[] newFrames, float newFrameSeconds, bool newLoop)
        {
            target = renderer;
            frames = newFrames ?? System.Array.Empty<Sprite>();
            frameSeconds = Mathf.Max(0.01f, newFrameSeconds);
            loop = newLoop;
            start = Time.time;
            Apply();
        }

        void Update()
        {
            if (!Apply() && !loop)
                Destroy(gameObject);
        }

        /// <summary>Pone el cuadro que toca; false cuando una pasada sin bucle ya acabó.</summary>
        bool Apply()
        {
            if (target == null || frames.Length == 0)
                return false;

            int index = Mathf.FloorToInt((Time.time - start) / frameSeconds);
            if (index >= frames.Length && !loop)
                return false;
            target.sprite = frames[index % frames.Length];
            return true;
        }
    }
}

using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Sombra de una libélula gigante que cruza el suelo de la sala del Cielo de libélulas cada cierto tiempo, de un lado a otro
    /// y a una altura al azar. Solo decorado. El dibujo mira a la derecha (se voltea al ir hacia la izquierda).
    /// </summary>
    public class DragonflyShadow : MonoBehaviour
    {
        const float Speed = 5f;
        const float MinWait = 5f;
        const float MaxWait = 11f;

        SpriteRenderer renderer2D;
        Rect area;
        System.Random random;
        float nextFlight;
        float direction;
        bool flying;

        internal void Configure(SpriteRenderer newRenderer, Sprite[] frames, float frameSeconds, Rect newArea, int seed)
        {
            renderer2D = newRenderer;
            area = newArea;
            random = new System.Random(seed);
            gameObject.AddComponent<SpriteFramePlayer>().Play(renderer2D, frames, frameSeconds, newLoop: true);
            nextFlight = Time.time + Wait();
        }

        float Wait() => MinWait + (float)random.NextDouble() * (MaxWait - MinWait);

        void Update()
        {
            if (!flying)
            {
                if (Time.time < nextFlight)
                    return;
                flying = true;
                direction = random.NextDouble() < 0.5 ? 1f : -1f;
                float y = area.yMin + 2f + (float)random.NextDouble() * Mathf.Max(0.1f, area.height - 4f);
                float margin = renderer2D.sprite != null ? renderer2D.sprite.bounds.extents.x : 2f;
                transform.position = new Vector3(direction > 0f ? area.xMin - margin : area.xMax + margin, y, 0f);
                renderer2D.flipX = direction < 0f;
                renderer2D.enabled = true;
                return;
            }

            transform.position += Vector3.right * (direction * Speed * Time.deltaTime);
            float edge = renderer2D.sprite != null ? renderer2D.sprite.bounds.extents.x : 2f;
            bool done = direction > 0f ? transform.position.x > area.xMax + edge : transform.position.x < area.xMin - edge;
            if (done)
            {
                flying = false;
                renderer2D.enabled = false;
                nextFlight = Time.time + Wait();
            }
        }
    }
}

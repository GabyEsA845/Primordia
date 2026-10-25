using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Burbuja que sube y se desvanece (caída a una fosa).</summary>
    public class PitBubble : MonoBehaviour
    {
        float rise, life, delay, age;
        SpriteRenderer sprite;
        Vector3 start;

        internal void Configure(float newRise, float newLife, float newDelay)
        {
            rise = newRise;
            life = Mathf.Max(0.05f, newLife);
            delay = newDelay;
            sprite = GetComponent<SpriteRenderer>();
            start = transform.position;
            sprite.enabled = delay <= 0f;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = (age - delay) / life;
            if (t < 0f)
                return;
            sprite.enabled = true;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = start + Vector3.up * rise * t + Vector3.right * Mathf.Sin(t * 9f) * 0.06f;
            var color = sprite.color;
            color.a = 0.8f * (1f - t);
            sprite.color = color;
        }
    }
}

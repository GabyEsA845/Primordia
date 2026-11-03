using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Lleva un efecto de un punto a otro (en arco si se pide) y lo destruye al llegar: el salto del parásito de un enemigo
    /// a otro y la gota de vida que sube hacia el jugador. Si el destino es un Transform, lo sigue mientras se mueve.
    /// </summary>
    public class ArcFlight : MonoBehaviour
    {
        Vector2 from;
        Vector2 to;
        Transform follow;
        float duration;
        float arcHeight;
        float elapsed;
        bool flying;

        public bool Flying => flying;

        /// <summary>Echa a volar hacia un punto fijo.</summary>
        public void Launch(Vector2 start, Vector2 end, float seconds, float arc = 0f)
        {
            from = start;
            to = end;
            follow = null;
            Begin(seconds, arc);
        }

        /// <summary>Echa a volar hacia algo que se mueve.</summary>
        public void Launch(Vector2 start, Transform target, float seconds, float arc = 0f)
        {
            from = start;
            follow = target;
            to = target != null ? (Vector2)target.position : start;
            Begin(seconds, arc);
        }

        void Begin(float seconds, float arc)
        {
            duration = Mathf.Max(0.01f, seconds);
            arcHeight = arc;
            elapsed = 0f;
            flying = true;
            transform.position = from;
        }

        void Update()
        {
            if (!flying)
                return;

            elapsed += Time.deltaTime;
            if (follow != null)
                to = follow.position;

            float k = Mathf.Clamp01(elapsed / duration);
            Vector2 position = Vector2.Lerp(from, to, k) + Vector2.up * (arcHeight * 4f * k * (1f - k));
            transform.position = position;

            if (k >= 1f)
            {
                flying = false;
                Destroy(gameObject);
            }
        }
    }
}

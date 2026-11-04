using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Bola de seda de la Araña de las frondas (piso 3): sale en arco hacia donde estaba el jugador, con su sombra en el suelo, y al
    /// caer deja una telaraña (<see cref="SpiderWeb"/>). No hace daño por sí misma: frena, y arde.
    /// </summary>
    public class SpiderSilk : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform shadow;
        [SerializeField] Sprite[] webAppear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] webIdle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] webBurn = System.Array.Empty<Sprite>();

        Vector2 from;
        Vector2 to;
        Vector2 mouthShift;
        float duration;
        float arcHeight;
        float elapsed;
        float webSeconds;
        bool flying;

        public bool Flying => flying;

        public void Configure(Transform newBody, Transform newShadow, Sprite[] appear, Sprite[] idle, Sprite[] burn)
        {
            body = newBody;
            shadow = newShadow;
            webAppear = appear;
            webIdle = idle;
            webBurn = burn;
        }

        /// <summary>Lanza la bola de <paramref name="start"/> (los pies de la araña) a <paramref name="end"/>, saliendo de su boca.</summary>
        public void Launch(Vector2 start, Vector2 end, float seconds, float arc, float webLife, Vector2 mouthFromFeet = default)
        {
            from = start;
            to = end;
            duration = Mathf.Max(0.05f, seconds);
            arcHeight = arc;
            webSeconds = webLife;
            mouthShift = mouthFromFeet;
            elapsed = 0f;
            flying = true;
            transform.position = start;
        }

        void Update()
        {
            if (!flying)
                return;

            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector2.Lerp(from, to, k);
            float height = arcHeight * 4f * k * (1f - k);
            if (body != null)
                body.localPosition = (Vector3)(mouthShift * (1f - k)) + new Vector3(0f, height, 0f);
            if (shadow != null)
                shadow.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, arcHeight > 0f ? height / arcHeight : 0f);

            if (k >= 1f)
            {
                flying = false;
                SpiderWeb.Create(to, webAppear, webIdle, webBurn, webSeconds);
                Destroy(gameObject);
            }
        }
    }
}

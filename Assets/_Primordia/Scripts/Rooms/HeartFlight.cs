using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El medio corazón que sale volando hacia la vaina de la tienda cuando el jugador lo envía con E: sube a toda prisa, girado hacia
    /// donde vuela y con su estela, y se pierde de vista. Solo se ve; el corazón ya está guardado.
    /// </summary>
    public class HeartFlight : MonoBehaviour
    {
        const float Speed = 11f;
        const float Seconds = 1.1f;
        const float FadeSeconds = 0.3f;
        const float FrameSeconds = 0.07f;

        Vector2 direction;
        float start;
        SpriteRenderer body;

        public static void Launch(Vector2 from, Vector2 direction)
        {
            var art = UiExtrasArt.Current;
            if (art == null || art.roomHeartFly.Length == 0)
                return;

            var go = new GameObject("HeartFlight");
            go.transform.position = from;
            var flight = go.AddComponent<HeartFlight>();
            flight.direction = direction.sqrMagnitude < 0.0001f ? Vector2.up : direction.normalized;
            flight.start = Time.time;
            flight.body = go.AddComponent<SpriteRenderer>();
            flight.body.sprite = art.roomHeartFly[0];
            flight.body.sortingOrder = 25;
            go.transform.right = flight.direction;
            go.AddComponent<SpriteFramePlayer>().Play(flight.body, art.roomHeartFly, FrameSeconds, newLoop: true);
        }

        void Update()
        {
            float age = Time.time - start;
            if (age >= Seconds)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(direction * Speed * Time.deltaTime);
            float fade = Mathf.Clamp01((Seconds - age) / FadeSeconds);
            body.color = new Color(1f, 1f, 1f, fade);
        }
    }
}

using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Estela venenosa que deja el barrido de cola del Euriptérido: una media luna verde en el suelo, por detrás de donde
    /// barrió, que se desvanece en unos segundos. Pisarla quita medio corazón y ralentiza (<see cref="Venom"/>), como mucho
    /// una vez por segundo.
    /// </summary>
    public class EurypteridVenomTrail : MonoBehaviour
    {
        const int SortingOrder = -11;
        const float HitInterval = 1f;

        SpriteRenderer sprite;
        Vector2 back;
        float radius;
        float lifetime;
        float age;
        float nextHit;
        Transform player;

        [SerializeField, Min(0)] int damage = 1;
        [SerializeField, Range(0f, 1f)] float slowFactor = 0.7f;
        [SerializeField, Min(0f)] float slowDuration = 1.5f;

        public Vector2 Back => back;

        /// <summary>Crea la estela en <paramref name="center"/>, abierta hacia <paramref name="backDirection"/>.</summary>
        public static EurypteridVenomTrail Spawn(Transform parent, Vector2 center, Vector2 backDirection, Sprite sprite, float radius,
            float lifetime)
        {
            var go = new GameObject("VenomTrail");
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            // La media luna está dibujada hacia la izquierda: se gira para que mire hacia atrás del jefe.
            float angle = Mathf.Atan2(-backDirection.y, -backDirection.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var trail = go.AddComponent<EurypteridVenomTrail>();
            trail.sprite = go.AddComponent<SpriteRenderer>();
            trail.sprite.sprite = sprite;
            trail.sprite.sortingOrder = SortingOrder;
            trail.back = backDirection.sqrMagnitude > 0.0001f ? backDirection.normalized : Vector2.left;
            trail.radius = radius;
            trail.lifetime = Mathf.Max(0.1f, lifetime);
            return trail;
        }

        /// <summary>True si ese punto está sobre la estela (media luna detrás del barrido).</summary>
        public bool Covers(Vector2 point)
        {
            var to = point - (Vector2)transform.position;
            return to.magnitude <= radius && Vector2.Angle(back, to) <= 90f;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            if (sprite != null)
                sprite.color = new Color(1f, 1f, 1f, 0.75f * Mathf.Clamp01((lifetime - age) / 0.5f));

            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                player = found != null ? found.transform : null;
            }
            if (player == null || Time.time < nextHit || !Covers(player.position))
                return;
            var health = player.GetComponent<Health>();
            if (health == null)
                return;
            nextHit = Time.time + HitInterval;
            if (health.TakeDamage(damage))
                Venom.Apply(health, slowFactor, slowDuration);
        }
    }
}

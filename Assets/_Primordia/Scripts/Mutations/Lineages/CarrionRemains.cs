using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Restos de Carroña (Carroñero): los deja un enemigo al morir y duran unos segundos (parpadean al final). Se comen
    /// al pasar por encima: el jugador está a menos de <see cref="eatRadius"/>. Avisan a su <see cref="CarrionBenefit"/>,
    /// que da el daño de más. Los dobles (Doble ración) cuentan como dos.
    /// </summary>
    public class CarrionRemains : MonoBehaviour
    {
        [Tooltip("Cuántas veces cuenta al comerlos (los dobles, 2).")]
        [SerializeField, Min(1)] int value = 1;
        [Tooltip("Distancia al centro del jugador a la que se comen.")]
        [SerializeField, Min(0.05f)] float eatRadius = 0.55f;
        [Tooltip("Segundos del final en los que parpadea.")]
        [SerializeField, Min(0f)] float blinkTime = 1f;

        CarrionBenefit owner;
        Transform player;
        SpriteRenderer spriteRenderer;
        float until;

        public int Value => value;
        public bool Eaten { get; private set; }

        /// <summary>Crea unos restos de ese prefab en esa posición, que duran <paramref name="duration"/> segundos.</summary>
        public static CarrionRemains Spawn(CarrionRemains prefab, Vector2 position, float duration, CarrionBenefit owner, Transform player)
        {
            if (prefab == null)
                return null;
            var remains = Instantiate(prefab, position, Quaternion.identity);
            remains.gameObject.SetActive(true);
            remains.owner = owner;
            remains.player = player;
            remains.until = Time.time + duration;
            return remains;
        }

        internal void Configure(int newValue, float newEatRadius)
        {
            value = Mathf.Max(1, newValue);
            eatRadius = Mathf.Max(0.05f, newEatRadius);
        }

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (Eaten)
                return;

            float left = until - Time.time;
            if (left <= 0f || owner == null)
            {
                Destroy(gameObject);
                return;
            }

            if (spriteRenderer != null)
                spriteRenderer.enabled = left > blinkTime || Mathf.Repeat(left, 0.2f) > 0.1f;

            if (player != null && Vector2.Distance(transform.position, player.position) <= eatRadius)
                Eat();
        }

        /// <summary>Se los come el jugador: suma al bonus y desaparecen.</summary>
        public void Eat()
        {
            if (Eaten)
                return;
            Eaten = true;
            if (owner != null)
                owner.OnEaten(this);
            Destroy(gameObject);
        }
    }
}

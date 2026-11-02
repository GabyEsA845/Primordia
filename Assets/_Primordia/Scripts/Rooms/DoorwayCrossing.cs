using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El jugador cruza el arco de una puerta de arriba pasando «por detrás» de él: mientras tiene los pies dentro del hueco, el
    /// marco del arco (la capa delantera) se dibuja encima del jugador y la sombra del hueco lo oscurece más cuanto más entra.
    /// Con los pies fuera del hueco (de pie delante de la puerta, o ya al otro lado) no hace nada, y el arco se ve como siempre.
    /// Cuelga del objeto Doorway; sin el arte del pulido no se crea.
    /// </summary>
    public class DoorwayCrossing : MonoBehaviour
    {
        // Encima de todo lo que lleva el jugador (cuerpo, capas, velo y llamas), debajo de la interfaz.
        const int ShadeOrder = 39;
        const int FrontOrder = 40;
        // Los pies del jugador respecto al centro de su cuerpo.
        const float FootOffset = -0.3f;
        // El arco ocupa 2×2 casillas: la pared empieza a una casilla por debajo del centro y acaba una por encima.
        const float HalfHeight = 1f;
        const float HalfWidth = 1.5f;
        // Pasado el arco por arriba, el jugador ya no se solapa con él.
        const float ExitMargin = 0.25f;
        const float FadeSpeed = 8f;

        SpriteRenderer front;
        SpriteRenderer shade;
        Transform player;
        float shadeAmount;

        /// <summary>True mientras el jugador está dentro del hueco (para las pruebas).</summary>
        public bool Crossing { get; private set; }

        public static DoorwayCrossing Create(Transform doorway, Sprite frontSprite, Sprite shadeSprite)
        {
            var crossing = doorway.gameObject.AddComponent<DoorwayCrossing>();
            crossing.front = NewLayer(doorway, "DoorwayFront", frontSprite, FrontOrder);
            crossing.shade = NewLayer(doorway, "DoorwayShade", shadeSprite, ShadeOrder);
            crossing.front.enabled = false;
            crossing.shade.enabled = false;
            return crossing;
        }

        static SpriteRenderer NewLayer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>True si unos pies en <paramref name="feet"/> están dentro del hueco de un arco centrado en <paramref name="center"/>.</summary>
        public static bool IsInside(Vector2 center, Vector2 feet) =>
            Mathf.Abs(feet.x - center.x) < HalfWidth && feet.y >= center.y - HalfHeight && feet.y <= center.y + HalfHeight + ExitMargin;

        /// <summary>Lo oscuro que va el jugador (0..1): nada en la boca del hueco, lo máximo arriba del todo.</summary>
        public static float Depth(Vector2 center, Vector2 feet) => Mathf.Clamp01((feet.y - (center.y - HalfHeight)) / (2f * HalfHeight));

        void Update()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                    return;
                player = found.transform;
            }

            var feet = (Vector2)player.position + Vector2.up * FootOffset;
            var center = (Vector2)transform.position;
            Crossing = IsInside(center, feet);

            front.enabled = Crossing;
            float target = Crossing ? 1f : 0f;
            shadeAmount = Mathf.MoveTowards(shadeAmount, target, FadeSpeed * Time.deltaTime);
            shade.enabled = shadeAmount > 0.001f;
            // Más oscuro cuanto más ha entrado (la sombra ya es más fuerte arriba; esto la afina con la profundidad).
            shade.color = new Color(1f, 1f, 1f, shadeAmount * Mathf.Lerp(0.6f, 1f, Depth(center, feet)));
        }
    }
}

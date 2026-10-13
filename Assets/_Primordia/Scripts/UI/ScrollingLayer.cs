using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Capa de fondo que se repite sin cortes y se desplaza a velocidad constante (en píxeles de la interfaz
    /// por segundo, tiempo real). La textura debe importarse con Wrap Mode = Repeat.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ScrollingLayer : MonoBehaviour
    {
        [Tooltip("Píxeles por segundo; x positiva = hacia la derecha, y positiva = hacia arriba.")]
        [SerializeField] Vector2 velocity;

        RawImage image;
        RectTransform rect;
        Vector2 offset;

        void Awake()
        {
            image = GetComponent<RawImage>();
            rect = (RectTransform)transform;
        }

        internal void Configure(Vector2 newVelocity) => velocity = newVelocity;

        void Update()
        {
            var texture = image.texture;
            if (texture == null)
                return;

            var size = new Vector2(texture.width, texture.height);
            // Mover el contenido a un lado es mover la ventana de la textura al otro.
            offset = new Vector2(Mathf.Repeat(offset.x - velocity.x * Time.unscaledDeltaTime / size.x, 1f),
                Mathf.Repeat(offset.y - velocity.y * Time.unscaledDeltaTime / size.y, 1f));

            // Un píxel de textura por píxel de interfaz, aunque la pantalla sea más ancha que 480.
            var tiles = new Vector2(rect.rect.width / size.x, rect.rect.height / size.y);
            image.uvRect = new Rect(offset, tiles);
        }
    }
}

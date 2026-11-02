using Primordia.Core;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// Sombra elíptica bajo los pies del jugador: lo apoya en el suelo. Se encoge y se aclara durante la esquiva, como si el
    /// cuerpo se despegara, y desaparece al morir. Sigue el sprite del cuerpo: si este se oculta, la sombra también.
    /// <see cref="PlayerController"/> la añade sola.
    /// </summary>
    public class PlayerShadow : MonoBehaviour
    {
        const int Width = 16;
        const int Height = 6;
        const int PixelsPerUnit = 16;

        [Tooltip("Distancia de la sombra al centro del jugador, hacia abajo (unidades).")]
        [SerializeField] float feetOffset = 0.34f;
        [Tooltip("Orden de dibujo: por debajo del cuerpo.")]
        [SerializeField] int sortingOrder = 8;
        [SerializeField, Range(0f, 1f)] float opacity = 0.4f;
        [Tooltip("Tamaño y opacidad (relativos) de la sombra en la esquiva.")]
        [SerializeField, Range(0.3f, 1f)] float dodgeScale = 0.7f;
        [SerializeField, Range(0f, 1f)] float dodgeOpacity = 0.6f;
        [Tooltip("Velocidad con que la sombra pasa de un estado al otro (por segundo).")]
        [SerializeField, Min(1f)] float blendSpeed = 14f;

        static Sprite sprite;

        SpriteRenderer shadow;
        SpriteRenderer body;
        PlayerController controller;
        PlayerAnimator animator;
        Health health;
        float blend;

        public SpriteRenderer Renderer => shadow;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = GetComponent<PlayerAnimator>();
            health = GetComponent<Health>();

            var go = new GameObject("PlayerShadow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -feetOffset, 0f);
            shadow = go.AddComponent<SpriteRenderer>();
            shadow.sprite = ShadowSprite();
            shadow.sortingOrder = sortingOrder;
        }

        void LateUpdate()
        {
            if (body == null && animator != null)
                body = animator.Body;
            if (body != null)
                shadow.sortingLayerID = body.sortingLayerID;

            bool visible = (body == null || body.enabled) && !(health != null && health.IsDead);
            shadow.enabled = visible;
            if (!visible)
                return;

            float target = controller != null && controller.IsDodging ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, blendSpeed * Time.unscaledDeltaTime);

            float scale = Mathf.Lerp(1f, dodgeScale, blend);
            shadow.transform.localScale = new Vector3(scale, scale, 1f);
            float bodyAlpha = body != null ? body.color.a : 1f;
            shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(opacity, opacity * dodgeOpacity, blend) * bodyAlpha);
        }

        // Elipse de 16×6 px: borde un poco más claro que el centro, para que no parezca un agujero.
        static Sprite ShadowSprite()
        {
            if (sprite != null)
                return sprite;

            var texture = new Texture2D(Width, Height) { filterMode = FilterMode.Point };
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float nx = (x + 0.5f - Width / 2f) / (Width / 2f);
                    float ny = (y + 0.5f - Height / 2f) / (Height / 2f);
                    float d = nx * nx + ny * ny;
                    float alpha = d > 1f ? 0f : d > 0.55f ? 0.6f : 1f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return sprite;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Marco que cubre los bordes de la pantalla mientras dura un efecto (las facetas de los Ojos compuestos): aparece y se
    /// va con un fundido corto en tiempo real, teñido del color que se le pida.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ScreenVignette : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float fade = 0.2f;

        Image image;
        Color color = Color.white;
        float alpha;
        float target;

        public bool IsShown => target > 0f;

        void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            Apply();
        }

        public void Show(Color newColor)
        {
            color = newColor;
            target = 1f;
            gameObject.SetActive(true);
            enabled = true;
        }

        public void Hide() => target = 0f;

        void Update()
        {
            alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime / fade);
            Apply();
        }

        void Apply()
        {
            if (image == null)
                return;
            image.color = new Color(color.r, color.g, color.b, color.a * alpha);
            image.enabled = alpha > 0f;
        }
    }
}

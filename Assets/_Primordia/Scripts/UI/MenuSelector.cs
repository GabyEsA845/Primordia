using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Marcador animado a la izquierda del botón que tiene el ratón encima (o el seleccionado con teclado).
    /// Empieza en el primero. Al pulsar un botón suelta una burbuja que revienta.
    /// </summary>
    public class MenuSelector : MonoBehaviour
    {
        [SerializeField] Button[] buttons = System.Array.Empty<Button>();
        [Tooltip("Separación en píxeles entre el marcador y el borde izquierdo del botón.")]
        [SerializeField] float gap = 3f;
        [Tooltip("Cuadros de la burbuja al pulsar. Vacío = sin burbuja.")]
        [SerializeField] Sprite[] bubbleFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float bubbleFrameDuration = 0.09f;

        RectTransform rect;

        public Button Current { get; private set; }

        void Awake()
        {
            rect = (RectTransform)transform;
            foreach (var button in buttons)
            {
                if (button != null && !button.TryGetComponent<MenuButtonFeedback>(out _))
                    button.gameObject.AddComponent<MenuButtonFeedback>().Selector = this;
            }
        }

        void OnEnable()
        {
            if (buttons.Length > 0)
                MoveTo(buttons[0]);
        }

        internal void Configure(Button[] newButtons, Sprite[] newBubbleFrames, float newBubbleFrameDuration)
        {
            buttons = newButtons;
            bubbleFrames = newBubbleFrames ?? System.Array.Empty<Sprite>();
            bubbleFrameDuration = newBubbleFrameDuration;
        }

        public void MoveTo(Button button)
        {
            if (button == null)
                return;

            Current = button;
            var target = (RectTransform)button.transform;
            // A media altura del botón, pegado a su borde izquierdo.
            Vector3 left = target.TransformPoint(new Vector3(target.rect.xMin, target.rect.center.y, 0f));
            var parent = (RectTransform)rect.parent;
            Vector2 local = parent.InverseTransformPoint(left);
            rect.localPosition = new Vector3(Mathf.Round(local.x - gap - rect.rect.width / 2f), Mathf.Round(local.y), 0f);
        }

        public void Burst(Button button)
        {
            if (bubbleFrames.Length == 0 || button == null)
                return;

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(rect.parent, false);
            var bubbleRect = (RectTransform)bubble.transform;
            bubbleRect.sizeDelta = bubbleFrames[0].rect.size;
            bubbleRect.position = rect.position;
            bubble.GetComponent<Image>().raycastTarget = false;
            bubble.AddComponent<UISpriteAnimation>().Configure(bubbleFrames, bubbleFrameDuration, newLoop: false, newDestroyWhenDone: true);
        }
    }
}

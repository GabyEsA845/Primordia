using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Enemies
{
    /// <summary>Arte de las flechas del borde de la Arthropleura: insignias de 24×24 y punteros de 12×12 en 16 direcciones.</summary>
    [System.Serializable]
    public class EdgeArrowArt
    {
        public Sprite[] badgeHead = System.Array.Empty<Sprite>(), badgeHeadWarn = System.Array.Empty<Sprite>();
        public Sprite[] badgeTail = System.Array.Empty<Sprite>(), badgeWeak = System.Array.Empty<Sprite>();
        public Sprite[] pointerHead = System.Array.Empty<Sprite>(), pointerTail = System.Array.Empty<Sprite>(), pointerWeak = System.Array.Empty<Sprite>();
    }

    /// <summary>
    /// Flechas en el borde de la pantalla que señalan lo que el jugador no ve de la Arthropleura (más grande que la pantalla): la cabeza
    /// (roja, parpadea cuando va a embestir), la cola (dorada) y el punto débil abierto (rosa que late). Cada insignia lleva su puntero
    /// a ~21 px hacia el objetivo. Solo se muestran las que apuntan a algo fuera de cámara.
    /// </summary>
    internal sealed class ArthropleuraEdgeArrows : MonoBehaviour
    {
        // Provisionales. Medidas en píxeles de la interfaz (referencia 480×270).
        const float ScreenWidth = 480f, ScreenHeight = 270f, Margin = 18f, PointerDistance = 21f;
        const float BadgeFrameSeconds = 0.18f;
        const int PointerDirections = 16;

        sealed class Arrow
        {
            public RectTransform root;
            public Image badge, pointer;
        }

        ArthropleuraBoss boss;
        EdgeArrowArt art;
        Camera view;
        Arrow head, tail, weak;

        internal bool HeadShown => head != null && head.root.gameObject.activeSelf;
        internal bool TailShown => tail != null && tail.root.gameObject.activeSelf;
        internal bool WeakShown => weak != null && weak.root.gameObject.activeSelf;

        internal static ArthropleuraEdgeArrows Create(ArthropleuraBoss owner, EdgeArrowArt newArt)
        {
            var go = new GameObject("ArthropleuraEdgeArrows");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var arrows = go.AddComponent<ArthropleuraEdgeArrows>();
            arrows.boss = owner;
            arrows.art = newArt;
            arrows.head = arrows.NewArrow("Head");
            arrows.tail = arrows.NewArrow("Tail");
            arrows.weak = arrows.NewArrow("Weak");
            return arrows;
        }

        Arrow NewArrow(string label)
        {
            var arrow = new Arrow();
            var rootObject = new GameObject(label, typeof(RectTransform));
            arrow.root = rootObject.GetComponent<RectTransform>();
            arrow.root.SetParent(transform, false);
            arrow.root.anchorMin = arrow.root.anchorMax = Vector2.zero;
            arrow.root.sizeDelta = new Vector2(24f, 24f);
            arrow.badge = NewImage("Badge", arrow.root, new Vector2(24f, 24f));
            arrow.pointer = NewImage("Pointer", arrow.root, new Vector2(12f, 12f));
            rootObject.SetActive(false);
            return arrow;
        }

        static Image NewImage(string label, RectTransform parent, Vector2 size)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(Image));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            var image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        void LateUpdate()
        {
            if (boss == null || !boss.isActiveAndEnabled)
            {
                Destroy(gameObject);
                return;
            }
            if (view == null)
                view = Camera.main;
            if (view == null)
                return;
            Show(head, boss.HeadWorld, boss.IsAboutToAttack ? art.badgeHeadWarn : art.badgeHead, art.pointerHead);
            // Con la rueda no hay cola ni punto débil: todo el cuerpo es la rueda.
            if (boss.WheelActive)
            {
                tail.root.gameObject.SetActive(false);
                weak.root.gameObject.SetActive(false);
                return;
            }
            Show(tail, boss.TailWorld, art.badgeTail, art.pointerTail);
            if (boss.TryGetWeakPoint(out Vector2 weakWorld))
                Show(weak, weakWorld, art.badgeWeak, art.pointerWeak);
            else
                weak.root.gameObject.SetActive(false);
        }

        void Show(Arrow arrow, Vector2 world, Sprite[] badges, Sprite[] pointers)
        {
            Vector3 viewport = view.WorldToViewportPoint(world);
            Vector2 target = new(viewport.x * ScreenWidth, viewport.y * ScreenHeight);
            bool inside = viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
            bool visible = !inside && badges.Length > 0;
            arrow.root.gameObject.SetActive(visible);
            if (!visible)
                return;
            Vector2 center = new(ScreenWidth * 0.5f, ScreenHeight * 0.5f);
            Vector2 direction = target - center;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.up;
            // La insignia va en el borde, en la línea del centro de la pantalla al objetivo.
            float halfX = ScreenWidth * 0.5f - Margin, halfY = ScreenHeight * 0.5f - Margin;
            float scale = Mathf.Min(Mathf.Abs(direction.x) > 0.001f ? halfX / Mathf.Abs(direction.x) : float.MaxValue,
                Mathf.Abs(direction.y) > 0.001f ? halfY / Mathf.Abs(direction.y) : float.MaxValue);
            arrow.root.anchoredPosition = center + direction * scale;
            arrow.badge.sprite = badges[Mathf.FloorToInt(Time.unscaledTime / BadgeFrameSeconds) % badges.Length];
            Vector2 unit = direction.normalized;
            float angle = Mathf.Atan2(unit.y, unit.x) * Mathf.Rad2Deg;
            int frame = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / (360f / PointerDirections)) % PointerDirections;
            arrow.pointer.enabled = pointers.Length > 0;
            if (pointers.Length > 0)
                arrow.pointer.sprite = pointers[Mathf.Clamp(frame, 0, pointers.Length - 1)];
            arrow.pointer.rectTransform.anchoredPosition = unit * PointerDistance;
        }
    }
}

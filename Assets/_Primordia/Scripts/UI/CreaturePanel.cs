using System.Collections.Generic;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Zona derecha de la pausa: la criatura en la lente y, de cada ranura, una línea en codo desde su pieza
    /// hasta su recuadro. Boca y Órgano bajan o suben por un pasillo a cada lado de la lente; Piel y
    /// Extremidades van en vertical y luego hacia su recuadro. Las líneas pasan por detrás de la criatura.
    /// Con el ratón sobre un recuadro se abre su cuadro de información. Se actualiza cada vez que se muestra.
    /// </summary>
    public class CreaturePanel : MonoBehaviour
    {
        [SerializeField] CreaturePortrait portrait;
        [Tooltip("Recuadros de las 4 ranuras, colocados respecto al centro del panel (que es el de la lente).")]
        [SerializeField] SlotCallout[] callouts = new SlotCallout[4];
        [SerializeField] MutationTooltip tooltip;
        [Tooltip("Contenedor de las líneas, detrás de la criatura.")]
        [SerializeField] RectTransform linesRoot;
        [Tooltip("Contenedor de los puntos, delante de la criatura.")]
        [SerializeField] RectTransform dotsRoot;
        [Tooltip("Trazo 9-slice que se estira en horizontal y en vertical (3 px de grueso).")]
        [SerializeField] Sprite lineSprite;
        [SerializeField] Sprite[] dotFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float dotFrameDuration = 0.18f;
        [Tooltip("Distancia del centro a los pasillos por los que van las líneas de la Boca y el Órgano.")]
        [SerializeField] float corridor = 61f;
        [SerializeField] Color placeholderLineColor = new(0.7f, 0.95f, 0.9f, 1f);

        readonly List<Image> lines = new();
        readonly List<RectTransform> dots = new();
        PlayerMutations player;
        bool dirty;

        public CreaturePortrait Portrait => portrait;
        public MutationTooltip Tooltip => tooltip;
        public IReadOnlyList<Image> Lines => lines;
        public SlotCallout CalloutOf(MutationSlot slot) => System.Array.Find(callouts, c => c != null && c.Slot == slot);

        void Awake()
        {
            foreach (var callout in callouts)
            {
                if (callout != null)
                    callout.Panel = this;
            }
        }

        void OnEnable()
        {
            var playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.GetComponent<PlayerMutations>() : null;
            if (tooltip != null)
                tooltip.Hide();
            // En el primer LateUpdate: así ya están activos todos los hijos (y los Animator del retrato).
            dirty = true;
        }

        void LateUpdate()
        {
            if (dirty)
                Refresh(player);
        }

        internal void Configure(CreaturePortrait newPortrait, SlotCallout[] newCallouts, MutationTooltip newTooltip,
            RectTransform newLinesRoot, RectTransform newDotsRoot)
        {
            portrait = newPortrait;
            callouts = newCallouts;
            tooltip = newTooltip;
            linesRoot = newLinesRoot;
            dotsRoot = newDotsRoot;
            Awake();
        }

        internal void ConfigureLook(Sprite line, Sprite[] dots, float frameDuration)
        {
            lineSprite = line;
            dotFrames = dots ?? System.Array.Empty<Sprite>();
            dotFrameDuration = frameDuration;
        }

        internal void Refresh(PlayerMutations newPlayer)
        {
            dirty = false;
            player = newPlayer;

            foreach (var callout in callouts)
            {
                if (callout != null)
                    callout.Show(player != null ? player.Get(callout.Slot) : null);
            }

            if (portrait != null)
                portrait.Show(player);
            DrawLines();
        }

        internal void Hover(SlotCallout callout)
        {
            foreach (var other in callouts)
            {
                if (other != null)
                    other.SetHighlighted(other == callout);
            }
            if (tooltip != null)
                tooltip.Show(callout);
        }

        internal void Unhover(SlotCallout callout)
        {
            callout.SetHighlighted(false);
            if (tooltip != null)
                tooltip.Hide();
        }

        void DrawLines()
        {
            int lineCount = 0;
            int dotCount = 0;
            Vector2 origin = portrait != null ? ((RectTransform)portrait.transform).anchoredPosition : Vector2.zero;

            foreach (var callout in callouts)
            {
                if (callout == null || portrait == null)
                    continue;

                Vector2 start = origin + portrait.AnchorOf(callout.Slot);
                Vector2 box = callout.Rect.anchoredPosition;
                float side = Mathf.Sign(box.x);
                // Borde del recuadro que mira a la lente.
                float edge = box.x - side * callout.Rect.rect.width / 2f;

                if (callout.Slot == MutationSlot.Mouth || callout.Slot == MutationSlot.Organ)
                {
                    float lane = side * corridor;
                    Segment(ref lineCount, start, new Vector2(lane, start.y));
                    Segment(ref lineCount, new Vector2(lane, start.y), new Vector2(lane, box.y));
                    Segment(ref lineCount, new Vector2(lane, box.y), new Vector2(edge, box.y));
                }
                else
                {
                    Segment(ref lineCount, start, new Vector2(start.x, box.y));
                    Segment(ref lineCount, new Vector2(start.x, box.y), new Vector2(edge, box.y));
                }

                Dot(ref dotCount, start);
            }

            for (int i = lineCount; i < lines.Count; i++)
                lines[i].gameObject.SetActive(false);
            for (int i = dotCount; i < dots.Count; i++)
                dots[i].gameObject.SetActive(false);
        }

        /// <summary>Tramo horizontal o vertical entre dos puntos de la rejilla de píxeles, de 3 px de grueso.</summary>
        void Segment(ref int count, Vector2 from, Vector2 to)
        {
            if (linesRoot == null)
                return;

            if (count == lines.Count)
                lines.Add(CreateImage("Line", linesRoot));
            var line = lines[count++];
            line.gameObject.SetActive(true);
            line.sprite = lineSprite;
            line.type = Image.Type.Sliced;
            line.color = lineSprite != null ? Color.white : placeholderLineColor;

            Vector2 min = Vector2.Min(from, to);
            Vector2 max = Vector2.Max(from, to);
            var rect = line.rectTransform;
            // El píxel del punto queda en el centro del trazo.
            rect.anchoredPosition = min - Vector2.one;
            rect.sizeDelta = max - min + new Vector2(3f, 3f);
        }

        void Dot(ref int count, Vector2 position)
        {
            if (dotsRoot == null || dotFrames.Length == 0)
                return;

            if (count == dots.Count)
            {
                var image = CreateImage("Dot", dotsRoot);
                image.gameObject.AddComponent<UISpriteAnimation>().Configure(dotFrames, dotFrameDuration, newLoop: true);
                image.rectTransform.sizeDelta = dotFrames[0].rect.size;
                dots.Add(image.rectTransform);
            }

            var dot = dots[count++];
            dot.gameObject.SetActive(true);
            // Centro del punto (2, 2) sobre el píxel del punto.
            dot.anchoredPosition = position - new Vector2(Mathf.Floor(dot.sizeDelta.x / 2f), Mathf.Floor(dot.sizeDelta.y / 2f));
        }

        static Image CreateImage(string name, RectTransform parent)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            var image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}

using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Muestra la vida del jugador en corazones: cada corazón son 2 puntos de vida. Detrás, un medio corazón verde
    /// por cada golpe que para la barrera (<see cref="Health.Barrier"/>, el Pastar biomasa del Herbívoro).
    /// Crea sus propios iconos como hijos; conviene ponerlo en un objeto con HorizontalLayoutGroup.
    /// </summary>
    public class HeartsDisplay : MonoBehaviour
    {
        public enum Fill
        {
            Empty,
            Half,
            Full
        }

        [Tooltip("Vida a mostrar. Si está vacío, usa la del objeto con la etiqueta Player.")]
        [SerializeField] Health health;
        [SerializeField] Sprite fullHeart;
        [SerializeField] Sprite halfHeart;
        [SerializeField] Sprite emptyHeart;
        [Tooltip("Medio corazón verde de la barrera.")]
        [SerializeField] Sprite barrierHeart;
        [Tooltip("Corazón morado (para un golpe entero y se conserva entre pisos).")]
        [SerializeField] Sprite purpleHeart;
        [Tooltip("Capas del corazón morado sobre cada corazón: contorno morado y grietas (variantes).")]
        [SerializeField] Sprite[] purpleInlays;
        [Tooltip("Tamaño de cada corazón en píxeles de la interfaz.")]
        [SerializeField] Vector2 heartSize = new(16f, 16f);

        readonly List<Image> hearts = new();
        readonly List<Image> barrierHearts = new();
        readonly List<Image> purpleHearts = new();

        public IReadOnlyList<Image> Hearts => hearts;

        /// <summary>Medios corazones verdes que se ven ahora.</summary>
        public int BarrierShown
        {
            get
            {
                int shown = 0;
                foreach (var heart in barrierHearts)
                {
                    if (heart.gameObject.activeSelf)
                        shown++;
                }
                return shown;
            }
        }

        void Start()
        {
            if (health == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    health = player.GetComponent<Health>();
            }

            if (health == null)
                return;

            health.Changed += OnHealthChanged;
            Refresh();
        }

        void OnDestroy()
        {
            if (health != null)
                health.Changed -= OnHealthChanged;
        }

        internal void SetBarrierSprite(Sprite sprite) => barrierHeart = sprite;

        internal void Configure(Health newHealth, Sprite full, Sprite half, Sprite empty)
        {
            health = newHealth;
            fullHeart = full;
            halfHeart = half;
            emptyHeart = empty;
        }

        /// <summary>Cómo se ve el corazón número <paramref name="index"/> (desde 0) con esa vida.</summary>
        public static Fill HeartFill(int currentHealth, int index)
        {
            int remaining = currentHealth - index * 2;
            return remaining >= 2 ? Fill.Full : remaining == 1 ? Fill.Half : Fill.Empty;
        }

        void OnHealthChanged(int current, int max) => Refresh();

        void Refresh()
        {
            int count = Mathf.CeilToInt(health.Max / 2f);

            while (hearts.Count < count)
                hearts.Add(CreateHeart());

            for (int i = 0; i < hearts.Count; i++)
            {
                bool visible = i < count;
                hearts[i].gameObject.SetActive(visible);
                if (visible)
                    hearts[i].sprite = SpriteFor(HeartFill(health.Current, i));
            }

            // Los verdes van detrás de los rojos (los hijos se crean en orden).
            while (barrierHearts.Count < health.Barrier)
            {
                var heart = CreateHeart();
                heart.name = "BarrierHeart";
                barrierHearts.Add(heart);
            }
            for (int i = 0; i < barrierHearts.Count; i++)
            {
                barrierHearts[i].sprite = barrierHeart != null ? barrierHeart : halfHeart;
                barrierHearts[i].transform.SetAsLastSibling();
                barrierHearts[i].gameObject.SetActive(i < health.Barrier);
            }

            // Con el corazón morado, el morado se incrusta en los corazones: contorno morado y grietas (una variante por corazón).
            bool inlaid = health.PurpleHearts > 0 && purpleInlays != null && purpleInlays.Length > 0;
            for (int i = 0; i < hearts.Count; i++)
            {
                var overlay = InlayOf(hearts[i]);
                overlay.gameObject.SetActive(inlaid && hearts[i].gameObject.activeSelf);
                if (inlaid)
                    overlay.sprite = purpleInlays[i % purpleInlays.Length];
            }

            // Sin el arte de las grietas, el corazón morado se ve como un corazón más al final.
            int icons = inlaid ? 0 : health.PurpleHearts;
            while (purpleHearts.Count < icons)
            {
                var heart = CreateHeart();
                heart.name = "PurpleHeart";
                purpleHearts.Add(heart);
            }
            for (int i = 0; i < purpleHearts.Count; i++)
            {
                purpleHearts[i].sprite = purpleHeart != null ? purpleHeart : fullHeart;
                purpleHearts[i].color = purpleHeart != null ? Color.white : new Color(0.75f, 0.45f, 1f);
                purpleHearts[i].transform.SetAsLastSibling();
                purpleHearts[i].gameObject.SetActive(i < icons);
            }
        }

        /// <summary>Capa morada de un corazón (se crea la primera vez).</summary>
        Image InlayOf(Image heart)
        {
            var existing = heart.transform.Find("PurpleInlay");
            if (existing != null)
                return existing.GetComponent<Image>();

            var inlay = new GameObject("PurpleInlay", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)inlay.transform;
            rect.SetParent(heart.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = inlay.GetComponent<Image>();
            image.raycastTarget = false;
            inlay.SetActive(false);
            return image;
        }

        /// <summary>Corazones morados que se ven ahora como icono suelto (sin el arte de las grietas).</summary>
        public int PurpleShown => purpleHearts.FindAll(heart => heart.gameObject.activeSelf).Count;

        /// <summary>Corazones que llevan ahora la capa morada.</summary>
        public int InlaidShown => hearts.FindAll(heart => heart.gameObject.activeSelf && InlayOf(heart).gameObject.activeSelf).Count;

        internal void SetPurpleSprite(Sprite sprite) => purpleHeart = sprite;

        internal void SetPurpleInlays(Sprite[] sprites) => purpleInlays = sprites;

        Image CreateHeart()
        {
            var heart = new GameObject("Heart", typeof(RectTransform), typeof(Image));
            heart.transform.SetParent(transform, false);
            ((RectTransform)heart.transform).sizeDelta = heartSize;
            return heart.GetComponent<Image>();
        }

        Sprite SpriteFor(Fill fill) => fill switch
        {
            Fill.Full => fullHeart,
            Fill.Half => halfHeart,
            _ => emptyHeart
        };
    }
}

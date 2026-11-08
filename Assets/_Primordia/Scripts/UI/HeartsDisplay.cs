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
        [Tooltip("Precio Inestable (mutaciones especiales): borde dorado agrietado sobre cada corazón (variantes).")]
        [SerializeField] Sprite[] unstableInlays;
        [Tooltip("Inestable con el corazón morado a la vez: solo las grietas doradas (el contorno se queda morado).")]
        [SerializeField] Sprite[] unstableCracks;
        [Tooltip("El medio corazón extra de Inestable se rompe: esquirlas doradas (24×24, centrado en el corazón).")]
        [SerializeField] Sprite[] unstableBreak;
        [SerializeField, Min(0.01f)] float unstableBreakFrameDuration = 0.06f;
        [Tooltip("Tamaño de cada corazón en píxeles de la interfaz.")]
        [SerializeField] Vector2 heartSize = new(16f, 16f);

        readonly List<Image> hearts = new();
        readonly List<Image> barrierHearts = new();
        readonly List<Image> purpleHearts = new();
        Image purpleSeed;

        public IReadOnlyList<Image> Hearts => hearts;

        /// <summary>True si ahora se ve el icono de la semilla morada (la que abre el muro de raíces de la sala secreta).</summary>
        public bool SeedShown => purpleSeed != null && purpleSeed.gameObject.activeSelf;

        /// <summary>Medios corazones verdes que se ven ahora (un corazón verde entero cuenta como dos).</summary>
        public int BarrierShown
        {
            get
            {
                int shown = 0;
                for (int i = 0; i < barrierHearts.Count; i++)
                {
                    if (barrierHearts[i].gameObject.activeSelf)
                        shown += i < health.Barrier / 2 ? 2 : 1;
                }
                return shown;
            }
        }

        Sprite greenFull;

        /// <summary>
        /// Corazón verde entero PROVISIONAL: el corazón lleno pasado a verde por código (el arte definitivo, más adelante).
        /// Null si no se puede leer la textura; entonces se tiñe la imagen.
        /// </summary>
        Sprite GreenFullSprite()
        {
            if (greenFull != null)
                return greenFull;
            var art = UiExtrasArt.Current;
            if (art != null && art.barrierFull != null)
                return greenFull = art.barrierFull;
            if (fullHeart == null)
                return null;

            try
            {
                var texture = fullHeart.texture;
                var rect = fullHeart.rect;
                var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, target);
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                copy.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);

                var pixels = copy.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a == 0)
                        continue;
                    Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
                    if (s > 0.12f)
                        h = 0.30f;
                    var color = (Color32)Color.HSVToRGB(h, s, v);
                    color.a = pixels[i].a;
                    pixels[i] = color;
                }
                copy.SetPixels32(pixels);
                copy.Apply();
                greenFull = Sprite.Create(copy, new Rect(0f, 0f, copy.width, copy.height), fullHeart.pivot / rect.size, fullHeart.pixelsPerUnit);
                return greenFull;
            }
            catch (System.Exception)
            {
                return null;
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
            health.ExtraDamageTaken += OnExtraDamage;
            Refresh();
        }

        void OnDestroy()
        {
            if (health != null)
            {
                health.Changed -= OnHealthChanged;
                health.ExtraDamageTaken -= OnExtraDamage;
            }
        }

        /// <summary>Esquirlas doradas en el corazón que perdió el medio corazón de más (el último que se vació).</summary>
        void OnExtraDamage(int extra)
        {
            if (unstableBreak == null || unstableBreak.Length == 0 || hearts.Count == 0)
                return;

            int index = Mathf.Clamp(health.Current / 2, 0, hearts.Count - 1);
            var shards = new GameObject("UnstableBreak", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)shards.transform;
            rect.SetParent(hearts[index].transform, false);
            rect.sizeDelta = unstableBreak[0].rect.size;
            shards.GetComponent<Image>().raycastTarget = false;
            shards.AddComponent<UISpriteAnimation>().Configure(unstableBreak, unstableBreakFrameDuration, false, newDestroyWhenDone: true);
        }

        /// <summary>Corazones que llevan ahora la capa dorada de Inestable.</summary>
        public int UnstableShown => hearts.FindAll(heart => heart.gameObject.activeSelf && LayerOf(heart, "UnstableInlay").gameObject.activeSelf).Count;

        internal void SetUnstableArt(Sprite[] inlays, Sprite[] cracks, Sprite[] breakFrames)
        {
            unstableInlays = inlays;
            unstableCracks = cracks;
            unstableBreak = breakFrames;
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

            // Los verdes van detrás de los rojos (los hijos se crean en orden). Cada dos medios forman un corazón verde
            // entero; si sobra uno, va al final como medio.
            int greenFullCount = health.Barrier / 2;
            int greenIcons = greenFullCount + health.Barrier % 2;
            while (barrierHearts.Count < greenIcons)
            {
                var heart = CreateHeart();
                heart.name = "BarrierHeart";
                barrierHearts.Add(heart);
            }
            var greenFullSprite = greenFullCount > 0 ? GreenFullSprite() : null;
            for (int i = 0; i < barrierHearts.Count; i++)
            {
                bool isFull = i < greenFullCount;
                barrierHearts[i].sprite = isFull ? greenFullSprite != null ? greenFullSprite : fullHeart
                    : barrierHeart != null ? barrierHeart : halfHeart;
                barrierHearts[i].color = isFull && greenFullSprite == null ? new Color(0.55f, 1f, 0.55f) : Color.white;
                barrierHearts[i].transform.SetAsLastSibling();
                barrierHearts[i].gameObject.SetActive(i < greenIcons);
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

            // Con mutaciones especiales (Inestable), borde dorado agrietado; si también está el morado, el contorno se queda
            // morado y el dorado va solo en las grietas. Encima de la capa morada.
            bool unstable = health.ExtraHitDamage > 0 && unstableInlays != null && unstableInlays.Length > 0;
            var goldArt = inlaid && unstableCracks != null && unstableCracks.Length > 0 ? unstableCracks : unstableInlays;
            for (int i = 0; i < hearts.Count; i++)
            {
                var overlay = LayerOf(hearts[i], "UnstableInlay");
                overlay.transform.SetSiblingIndex(InlayOf(hearts[i]).transform.GetSiblingIndex() + 1);
                overlay.gameObject.SetActive(unstable && hearts[i].gameObject.activeSelf);
                if (unstable)
                    overlay.sprite = goldArt[i % goldArt.Length];
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

            // La semilla morada, al final de los corazones. Icono PROVISIONAL hecho por código (falta el dibujo del HUD).
            if (health.PurpleSeed && purpleSeed == null)
            {
                purpleSeed = CreateHeart();
                purpleSeed.name = "PurpleSeed";
                purpleSeed.sprite = PurpleSeedIcon.Sprite;
            }
            if (purpleSeed != null)
            {
                purpleSeed.transform.SetAsLastSibling();
                purpleSeed.gameObject.SetActive(health.PurpleSeed);
            }
        }

        /// <summary>Capa morada de un corazón (se crea la primera vez).</summary>
        Image InlayOf(Image heart) => LayerOf(heart, "PurpleInlay");

        /// <summary>Capa con ese nombre sobre un corazón, del mismo tamaño (se crea oculta la primera vez).</summary>
        static Image LayerOf(Image heart, string name)
        {
            var existing = heart.transform.Find(name);
            if (existing != null)
                return existing.GetComponent<Image>();

            var inlay = new GameObject(name, typeof(RectTransform), typeof(Image));
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

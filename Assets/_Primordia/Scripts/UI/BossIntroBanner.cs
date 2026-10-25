using Primordia.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Rótulo de la presentación de un jefe, sobre la pantalla de 480×270: una banda diagonal oscura de lado a lado con
    /// un filo de color arriba y abajo, el nombre del jefe a la izquierda, su título debajo y su retrato a la derecha,
    /// por encima de la banda. Entra deslizándose (la banda y el nombre por la izquierda, el retrato por la derecha)
    /// y sale igual. Se crea solo en el lienzo del HUD la primera vez que hace falta.
    /// </summary>
    public class BossIntroBanner : MonoBehaviour
    {
        /// <summary>Segundos que tarda en salir.</summary>
        public const float OutTime = 0.3f;
        const float InTime = 0.35f;
        // La banda sube hacia la derecha: de 164 a 136 px (desde arriba) en el borde de arriba, sobre 480 de ancho.
        const float BandAngle = 3.34f;
        const float BandHeight = 64f;

        RectTransform band;
        Image bandImage;
        Image edgeTop;
        Image edgeBottom;
        Image nameImage;
        Text title;
        Image portrait;
        CanvasGroup group;
        float time;
        float hideAt = float.PositiveInfinity;
        float outTime = OutTime;

        /// <summary>True mientras se ve (entrando, quieto o saliendo).</summary>
        public bool Showing => group != null && group.alpha > 0f;

        /// <summary>El rótulo del lienzo <paramref name="canvasRoot"/>, que se crea si no existe.</summary>
        public static BossIntroBanner For(Transform canvasRoot)
        {
            var banner = canvasRoot.GetComponentInChildren<BossIntroBanner>(true);
            if (banner == null)
            {
                var go = new GameObject("BossIntroBanner", typeof(RectTransform));
                go.transform.SetParent(canvasRoot, false);
                banner = go.AddComponent<BossIntroBanner>();
            }
            banner.transform.SetAsLastSibling();
            return banner;
        }

        void Awake() => Build();

        void Build()
        {
            if (group != null)
                return;
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // Banda: centrada en (240, 88) contando desde abajo, más ancha que la pantalla para que no se vean sus esquinas.
            band = NewRect("Band", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 88f), new Vector2(560f, BandHeight));
            band.localRotation = Quaternion.Euler(0f, 0f, BandAngle);
            bandImage = band.gameObject.AddComponent<Image>();
            edgeTop = NewImage("EdgeTop", band, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(560f, 2f));
            edgeBottom = NewImage("EdgeBottom", band, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(560f, 2f));

            nameImage = NewImage("Name", root, Vector2.zero, Vector2.zero, new Vector2(36f, 71f), Vector2.one);
            title = NewRect("Title", root, Vector2.zero, new Vector2(0f, 1f), new Vector2(42f, 72f), new Vector2(240f, 12f))
                .gameObject.AddComponent<Text>();
            title.fontSize = 8;
            title.alignment = TextAnchor.UpperLeft;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.raycastTarget = false;
            // La misma fuente pixel que el resto de la interfaz.
            foreach (var sample in GetComponentInParent<Canvas>().GetComponentsInChildren<Text>(true))
            {
                if (sample != title && sample.font != null)
                {
                    title.font = sample.font;
                    break;
                }
            }
            portrait = NewImage("Portrait", root, Vector2.zero, new Vector2(0f, 1f), new Vector2(278f, 222f), new Vector2(176f, 176f));
        }

        /// <summary>Entra con los datos de <paramref name="data"/>.</summary>
        public void Show(BossIntroData data)
        {
            Build();
            bandImage.color = new Color(data.BandColor.r, data.BandColor.g, data.BandColor.b, 0.94f);
            edgeTop.color = edgeBottom.color = data.AccentColor;
            nameImage.sprite = data.NameSprite;
            nameImage.enabled = data.NameSprite != null;
            if (data.NameSprite != null)
                nameImage.rectTransform.sizeDelta = data.NameSprite.rect.size;
            title.text = (data.Title ?? "").ToUpperInvariant();
            title.color = data.AccentColor;
            portrait.sprite = data.Portrait;
            portrait.enabled = data.Portrait != null;
            if (data.Portrait != null)
                portrait.rectTransform.sizeDelta = data.Portrait.rect.size;
            time = 0f;
            hideAt = float.PositiveInfinity;
            group.alpha = 1f;
            Apply();
        }

        /// <summary>Sale deslizándose (más deprisa con <paramref name="fast"/>).</summary>
        public void Hide(bool fast = false)
        {
            if (!Showing)
                return;
            outTime = fast ? OutTime * 0.5f : OutTime;
            hideAt = Mathf.Min(hideAt, time);
        }

        public void HideImmediate()
        {
            if (group != null)
                group.alpha = 0f;
            hideAt = float.PositiveInfinity;
        }

        void Update()
        {
            if (!Showing)
                return;
            time += Time.unscaledDeltaTime;
            Apply();
        }

        void Apply()
        {
            // Entrada: la banda, luego el retrato y el nombre, y al final el título. Salida: todo hacia fuera a la vez.
            float bandIn = Ease(time / InTime);
            float portraitIn = Ease((time - 0.15f) / InTime);
            float nameIn = Ease((time - 0.25f) / InTime);
            float titleIn = Mathf.Clamp01((time - 0.55f) / 0.25f);
            float outK = float.IsPositiveInfinity(hideAt) ? 0f : Mathf.Clamp01((time - hideAt) / outTime);
            float gone = outK * outK;

            band.anchoredPosition = Snap(new Vector2((bandIn - 1f) * 560f - gone * 600f, 88f));
            nameImage.rectTransform.anchoredPosition = Snap(new Vector2(36f + (nameIn - 1f) * 300f - gone * 500f, 71f));
            title.rectTransform.anchoredPosition = Snap(new Vector2(42f - gone * 500f, 72f));
            SetAlpha(title, titleIn * (1f - outK));
            portrait.rectTransform.anchoredPosition = Snap(new Vector2(278f + (1f - portraitIn) * 120f + gone * 260f, 222f));
            SetAlpha(portrait, portraitIn * (1f - outK));

            if (outK >= 1f)
                HideImmediate();
        }

        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        // A píxeles enteros, para que el arte no se emborrone al deslizarse.
        static Vector2 Snap(Vector2 v) => new(Mathf.Round(v.x), Mathf.Round(v.y));

        static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image NewImage(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var image = NewRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}

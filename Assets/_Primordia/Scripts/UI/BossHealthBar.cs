using System.Collections.Generic;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Barra de vida del jefe, arriba en el centro. Aparece mientras hay un jefe activo y cambia según su estado
    /// (<see cref="IBoss.BarStyle"/>): otro color en la fase agresiva, gris con candado mientras es inmune, y roja,
    /// palpitando y temblando cuando está agotado.
    /// Todas tienen barra fantasma: al recibir daño el relleno baja en un instante y un trazo claro marca lo perdido, espera
    /// un poco y se vacía. Un jefe con <see cref="BossBarAppearance"/> usa su propio dibujo (<see cref="BossBarSkin"/>):
    /// marco, relleno y ojos por fase, muescas en los umbrales de <see cref="IPhasedBoss"/> que se rompen al pasarlos y, mientras
    /// es inmune, el marco en silueta con sus ojos brillando en vez del candado.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Relleno de la barra: se estira de izquierda a derecha cambiando su ancla derecha.")]
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;
        [SerializeField] Text title;
        [Tooltip("Candado que se ve mientras el jefe es inmune. Sin sprite no se muestra.")]
        [SerializeField] Image lockIcon;
        [SerializeField] Color phase1Color = new(0.85f, 0.25f, 0.45f);
        [SerializeField] Color phase2Color = new(1f, 0.55f, 0.15f);
        [SerializeField] Color immuneColor = new(0.55f, 0.55f, 0.6f);
        [SerializeField] Color weakColor = new(0.9f, 0.1f, 0.12f);
        [Tooltip("Latidos por segundo de la barra cuando el jefe está agotado.")]
        [SerializeField, Min(0f)] float weakPulseRate = 2.5f;
        [Tooltip("Cuánto crece la barra en cada latido (0,08 = un 8 %).")]
        [SerializeField, Min(0f)] float weakPulseScale = 0.08f;
        [Tooltip("Píxeles que tiembla la barra cuando el jefe está agotado.")]
        [SerializeField, Min(0f)] float weakShake = 1f;

        [Header("Barra fantasma")]
        [Tooltip("Segundos que tarda el relleno en bajar hasta la vida nueva.")]
        [SerializeField, Min(0.01f)] float dropTime = 0.12f;
        [Tooltip("Segundos que espera el trazo claro de la vida perdida antes de vaciarse (cada golpe lo reinicia).")]
        [SerializeField, Min(0f)] float trailHold = 0.4f;
        [Tooltip("Lo que se vacía el trazo claro por segundo (fracción de la barra).")]
        [SerializeField, Min(0.01f)] float trailDrainRate = 0.8f;
        [Tooltip("Color del trazo claro en la barra lisa.")]
        [SerializeField] Color trailColor = new(1f, 0.93f, 0.88f);

        // El aspecto propio queda bajo el nombre, a esta distancia del borde de arriba de la barra.
        const float SkinTop = 12f;
        const float SkinBottomMargin = 24f; // Provisional.
        const float LowHealthFraction = 0.10f;
        const float LowHealthShake = 1f;

        RectTransform rootRect;
        Vector2 rootPosition;
        RectTransform plainTrail;
        IBoss shownBoss;
        SkinView skinView;
        float shown;
        float trail;
        float lastRatio;
        float dropFrom;
        float dropStart = float.NegativeInfinity;
        float trailWaitUntil;

        public BossBarStyle Style { get; private set; }

        /// <summary>Objeto de la barra (se queda visible durante las escenas de los jefes).</summary>
        public GameObject Root => root;

        /// <summary>Vida que muestra el relleno ahora (0–1): va detrás de la real mientras baja.</summary>
        public float ShownRatio => shown;

        /// <summary>Hasta dónde llega el trazo claro de la vida perdida (0–1).</summary>
        public float TrailRatio => trail;

        /// <summary>Aspecto propio en uso (null con la barra lisa).</summary>
        public BossBarSkin ActiveSkin => skinView?.Skin;

        internal Sprite SkinFrameSprite => skinView?.FrameSprite;
        internal Sprite SkinFillSprite => skinView?.FillSprite;
        internal Sprite SkinEyesSprite => skinView?.EyesSprite;
        internal int NotchCount => skinView?.NotchCount ?? 0;
        internal bool IsNotchBroken(int index) => skinView != null && skinView.IsBroken(index);
        internal bool IsNotchBreaking(int index) => skinView != null && skinView.IsBreaking(index);
        internal bool ShowsPhaseChange => skinView != null && skinView.ShowsPhaseChange;

        void Awake()
        {
            root.SetActive(false);
            rootRect = root.transform as RectTransform;
            if (rootRect != null)
                rootPosition = rootRect.anchoredPosition;
            CreatePlainTrail();
        }

        void LateUpdate()
        {
            var boss = ActiveBoss.Current;
            // Un jefe destruido sigue pareciendo no nulo a través de la interfaz.
            bool alive = boss is Object unityObject && unityObject != null && boss.Health != null && !boss.Health.IsDead;
            if (root.activeSelf != alive)
                root.SetActive(alive);
            if (!alive)
            {
                shownBoss = null;
                skinView?.SetVisible(false);
                return;
            }

            float ratio = (float)boss.Health.Current / boss.Health.Max;
            if (!ReferenceEquals(boss, shownBoss))
                ShowBoss(boss, ratio);
            UpdateGhost(ratio);

            title.text = boss.DisplayName;
            ApplyStyle(boss.BarStyle);

            if (skinView != null)
            {
                var phased = boss as IPhasedBoss;
                skinView.Apply(phased?.PhaseIndex ?? 0, Style == BossBarStyle.Immune, shown, trail, Time.unscaledTime, ratio <= LowHealthFraction);
            }
            else
            {
                fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
                if (plainTrail != null)
                    plainTrail.anchorMax = new Vector2(trail, plainTrail.anchorMax.y);
            }
        }

        // Un jefe nuevo: la barra empieza llena hasta su vida, sin trazo, con su aspecto si lo tiene.
        void ShowBoss(IBoss boss, float ratio)
        {
            shownBoss = boss;
            shown = trail = lastRatio = dropFrom = ratio;
            dropStart = float.NegativeInfinity;
            trailWaitUntil = 0f;

            var skin = boss is Component component && component.TryGetComponent<BossBarAppearance>(out var appearance)
                ? appearance.Skin
                : null;
            if (skinView != null)
            {
                skinView.Destroy();
                skinView = null;
            }
            if (skin != null)
                skinView = new SkinView(skin, root.transform, (boss as IPhasedBoss)?.PhaseThresholds, SkinTop);

            // La barra lisa (fondo, relleno y trazo) queda oculta bajo un aspecto propio.
            if (fill != null && fill.parent != null)
                fill.parent.gameObject.SetActive(skinView == null);
        }

        void OnDisable() => skinView?.SetVisible(false);

        void OnDestroy() => skinView?.Destroy();

        void UpdateGhost(float ratio)
        {
            float now = Time.unscaledTime;
            if (ratio < lastRatio)
            {
                // Golpe: el relleno baja desde donde estaba y el trazo vuelve a esperar.
                dropFrom = shown;
                dropStart = now;
                trailWaitUntil = now + trailHold;
            }
            else if (ratio > lastRatio)
            {
                dropFrom = ratio;
                dropStart = float.NegativeInfinity;
            }
            lastRatio = ratio;

            shown = Mathf.Lerp(dropFrom, ratio, Mathf.Clamp01((now - dropStart) / dropTime));
            if (trail < shown)
                trail = shown;
            else if (now >= trailWaitUntil)
                trail = Mathf.Max(shown, trail - trailDrainRate * Time.unscaledDeltaTime);
        }

        void ApplyStyle(BossBarStyle style)
        {
            Style = style;
            fillImage.color = style switch
            {
                BossBarStyle.Enraged => phase2Color,
                BossBarStyle.Immune => immuneColor,
                BossBarStyle.Weak => weakColor,
                _ => phase1Color
            };

            // Con aspecto propio, la inmunidad se ve en el marco a oscuras: sin candado.
            if (lockIcon != null)
                lockIcon.enabled = style == BossBarStyle.Immune && lockIcon.sprite != null && skinView == null;

            if (rootRect == null)
                return;

            if (style == BossBarStyle.Weak)
            {
                // Latido: crece de golpe y vuelve despacio, como un pulso débil.
                float beat = Mathf.Repeat(Time.unscaledTime * weakPulseRate, 1f);
                float pulse = Mathf.Pow(1f - beat, 3f);
                rootRect.localScale = Vector3.one * (1f + weakPulseScale * pulse);
                var shake = new Vector2(Random.Range(-1, 2), Random.Range(-1, 2)) * weakShake * pulse;
                rootRect.anchoredPosition = rootPosition + new Vector2(Mathf.Round(shake.x), Mathf.Round(shake.y));
            }
            else
            {
                rootRect.localScale = Vector3.one;
                rootRect.anchoredPosition = rootPosition;
            }
        }

        // El trazo de la barra lisa va detrás del relleno, con sus mismos márgenes. Se crea al vuelo para no tener que
        // rehacer el prefab de la interfaz.
        void CreatePlainTrail()
        {
            if (fill == null || fill.parent == null || plainTrail != null)
                return;

            plainTrail = new GameObject("Trail", typeof(RectTransform)).GetComponent<RectTransform>();
            plainTrail.SetParent(fill.parent, false);
            plainTrail.SetSiblingIndex(fill.GetSiblingIndex());
            plainTrail.anchorMin = fill.anchorMin;
            plainTrail.anchorMax = fill.anchorMax;
            plainTrail.offsetMin = fill.offsetMin;
            plainTrail.offsetMax = fill.offsetMax;
            var image = plainTrail.gameObject.AddComponent<Image>();
            image.color = trailColor;
            image.raycastTarget = false;
        }

        internal void Configure(GameObject newRoot, RectTransform newFill, Image newFillImage, Text newTitle)
        {
            root = newRoot;
            fill = newFill;
            fillImage = newFillImage;
            title = newTitle;
            CreatePlainTrail();
        }

        internal void ConfigureLock(Image newLockIcon) => lockIcon = newLockIcon;

        /// <summary>Las piezas de un aspecto propio, creadas bajo la barra mientras se ve ese jefe.</summary>
        sealed class SkinView
        {
            sealed class Notch
            {
                public Image Plate;
                public Image Effect;
                public bool Broken;
                public float BreakStart = float.NegativeInfinity;
            }

            public readonly BossBarSkin Skin;
            readonly RectTransform root;
            readonly Image fill;
            readonly Image trail;
            readonly Image frame;
            readonly Image eyes;
            readonly Image phaseChange;
            readonly Image lowSparks;
            readonly Vector2 position;
            readonly List<Notch> notches = new();
            bool first = true;
            int lastPhase = -1;
            float phaseChangeStart = float.NegativeInfinity;

            public Sprite FrameSprite => frame.sprite;
            public Sprite FillSprite => fill.sprite;
            public Sprite EyesSprite => eyes.enabled ? eyes.sprite : null;
            public int NotchCount => notches.Count;
            public bool IsBroken(int index) => index >= 0 && index < notches.Count && notches[index].Broken;
            public bool IsBreaking(int index) => IsBroken(index) && notches[index].Effect.enabled;
            public bool ShowsPhaseChange => phaseChange.enabled;

            public SkinView(BossBarSkin skin, Transform parent, float[] thresholds, float top)
            {
                Skin = skin;
                var size = skin.Frame != null ? skin.Frame.rect.size : new Vector2(skin.Window.xMax, skin.Window.yMax);
                root = new GameObject("BossBarSkin", typeof(RectTransform)).GetComponent<RectTransform>();
                // La raíz de la barra está arriba; el aspecto inferior se ancla al contenedor de pantalla.
                root.SetParent(skin.BottomAnchored ? parent.parent : parent, false);
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, skin.BottomAnchored ? 0f : 1f);
                position = new Vector2(0f, skin.BottomAnchored ? SkinBottomMargin : -top);
                root.anchoredPosition = position;
                root.sizeDelta = size;

                var window = skin.Window;
                var windowPosition = new Vector2(window.x, window.y);
                var windowSize = new Vector2(window.width, window.height);
                Piece("Back", skin.Back, windowPosition, windowSize, Vector2.zero);
                trail = Bar(Piece("Trail", skin.Trail, windowPosition, windowSize, Vector2.zero));
                fill = Bar(Piece("Fill", null, windowPosition, windowSize, Vector2.zero));
                frame = Piece("Frame", skin.Frame, Vector2.zero, size, Vector2.zero);

                // Cada muesca centrada en su umbral; la rotura, centrada en el mismo punto.
                if (thresholds != null && skin.Notch != null)
                {
                    foreach (float threshold in thresholds)
                    {
                        float x = window.x + Mathf.Round(window.width * threshold);
                        var plateSize = skin.Notch.rect.size;
                        var effectSize = skin.NotchBreak.Length > 0 ? skin.NotchBreak[0].rect.size : plateSize;
                        notches.Add(new Notch
                        {
                            Plate = Piece("Notch", skin.Notch, new Vector2(x, 0f), plateSize, new Vector2(0.5f, 0f)),
                            Effect = Piece("NotchBreak", null, new Vector2(x, Mathf.Round(size.y * 0.5f)), effectSize, new Vector2(0.5f, 0.5f)),
                        });
                    }
                }

                eyes = Piece("Eyes", null, Vector2.zero, size, Vector2.zero);
                phaseChange = Piece("PhaseChange", null, Vector2.zero, size, Vector2.zero);
                phaseChange.enabled = false;
                lowSparks = Piece("LowSparks", null, Vector2.zero, size, Vector2.zero);
            }

            public void Apply(int phase, bool dark, float shown, float trailRatio, float now, bool lowHealth)
            {
                root.gameObject.SetActive(true);
                var look = dark ? Skin.Dark : Skin.Phase(phase);
                var frameLook = dark && Skin.ImmuneUsesPhaseFrame ? Skin.Phase(phase) : look;
                frame.sprite = frameLook.frame != null && frameLook.frame.Length > 0 ? Skin.Loop(frameLook.frame, now) : dark ? Skin.FrameDark : Skin.Frame;
                fill.sprite = Skin.Loop(look.fill, now, look.fillFrameTime);
                fill.enabled = fill.sprite != null;
                eyes.sprite = Skin.Loop(frameLook.eyes, now, frameLook.eyesFrameTime);
                eyes.enabled = eyes.sprite != null;
                fill.fillAmount = Pixels(shown);
                trail.fillAmount = Pixels(trailRatio);

                for (int i = 0; i < notches.Count; i++)
                {
                    var notch = notches[i];
                    bool broken = phase > i;
                    if (broken != notch.Broken)
                    {
                        notch.Broken = broken;
                        // Al aparecer la barra ya rota, sin animación; si se rompe a la vista, se ve romperse.
                        notch.BreakStart = broken && !first ? now : float.NegativeInfinity;
                    }

                    var effect = notch.Broken ? Skin.NotchBreakFrame(now - notch.BreakStart) : null;
                    notch.Effect.sprite = effect;
                    notch.Effect.enabled = effect != null;
                    notch.Plate.sprite = notch.Broken ? Skin.NotchBroken : Skin.Notch;
                    notch.Plate.enabled = effect == null && notch.Plate.sprite != null;
                }

                // Al pasar a la fase siguiente a la vista (no al aparecer la barra), el efecto de cambio se ve una vez.
                if (phase > lastPhase && !first)
                    phaseChangeStart = now;
                lastPhase = phase;
                var change = Skin.PhaseChangeFrame(now - phaseChangeStart, lastPhase);
                phaseChange.sprite = change;
                phaseChange.enabled = change != null;
                lowSparks.sprite = lowHealth ? Skin.LowSparksFrame(now) : null;
                lowSparks.enabled = lowSparks.sprite != null;
                root.anchoredPosition = position + (lowHealth && Skin.HasLowSparks
                    ? new Vector2(Random.Range(-1, 2), Random.Range(-1, 2)) * LowHealthShake : Vector2.zero);
                first = false;
            }

            public void SetVisible(bool visible)
            {
                if (root != null)
                    root.gameObject.SetActive(visible);
            }

            public void Destroy()
            {
                if (root == null)
                    return;
                root.gameObject.SetActive(false);
                Object.Destroy(root.gameObject);
            }

            // Redondeado a píxeles enteros de la ventana; con algo de vida, al menos un píxel.
            float Pixels(float ratio)
            {
                int width = Mathf.Max(1, Skin.Window.width);
                float pixels = Mathf.Round(ratio * width);
                if (ratio > 0f && pixels < 1f)
                    pixels = 1f;
                return pixels / width;
            }

            Image Piece(string name, Sprite sprite, Vector2 position, Vector2 size, Vector2 pivot)
            {
                var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(root, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = pivot;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.enabled = sprite != null;
                image.raycastTarget = false;
                return image;
            }

            // Se recorta de izquierda a derecha.
            static Image Bar(Image image)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.enabled = image.sprite != null;
                return image;
            }
        }
    }
}

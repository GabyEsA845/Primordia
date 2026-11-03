using System.Collections.Generic;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Recuadro del Órgano especial, abajo a la izquierda del HUD (docs/multimedia-temp/organ-hud). Sustituye a la barra de recarga bajo el
    /// jugador. Muestra el icono del órgano y un tubo de savia que sube al recargar: el icono va apagado y le vuelve el color de abajo
    /// arriba al ritmo del tubo, con una línea lima en el borde que sube; si se recarga matando, el tubo lleva una muesca por enemigo.
    /// Al quedar listo, destello una vez y luego un pulso suave hasta que se usa. Sin órgano, el recuadro vacío.
    /// Todas las piezas de dibujo se miden en píxeles desde la esquina de arriba a la izquierda del recuadro (46×36).
    /// </summary>
    public class OrganHud : MonoBehaviour
    {
        // Posición de cada pieza dentro del recuadro (x, y desde arriba) y tamaños.
        const float IconX = 2f, IconY = 2f, IconSize = 32f;
        const float SapX = 40f, SapY = 2f, SapWidth = 4f, SapHeight = 32f;
        const float PanelWidth = 46f, PanelHeight = 36f;
        const float BubbleFrameSeconds = 0.09f;
        const float SapTopFrameSeconds = 0.1f;
        const float ReadyFrameSeconds = 0.06f;
        const float PulseFrameSeconds = 0.12f;

        // Tonos del icono apagado (README): luminancia en 4 escalones; el contorno se conserva con algo de transparencia.
        static readonly Color32[] DimTones =
        {
            new(0x28, 0x46, 0x3e, 235), new(0x36, 0x59, 0x4f, 235), new(0x4a, 0x70, 0x62, 235), new(0x62, 0x8a, 0x7a, 235),
        };
        static readonly Color32 Outline = new(24, 20, 37, 255);
        static readonly Color32 EdgeLine = new(0x8c, 0xe6, 0x59, 255);
        static readonly Dictionary<Sprite, Sprite> dimmed = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => dimmed.Clear();

        [Header("Dibujo")]
        [SerializeField] Sprite panel;
        [SerializeField] Sprite panelEmpty;
        [SerializeField] Sprite emptyIcon;
        [SerializeField] Sprite sapFill;
        [SerializeField] Sprite notch;
        [SerializeField] Sprite[] sapBubbles = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] sapTop = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] readyFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] pulseFrames = System.Array.Empty<Sprite>();
        [SerializeField] PlayerAbility ability;

        Image panelImage;
        Image emptyIconImage;
        Image dimIcon;
        Image colorIcon;
        Image edgeLine;
        Image fill;
        Image bubbles;
        Image top;
        Image overlay;
        readonly List<Image> notches = new();
        bool built;
        bool wasCharging;
        bool pulsing;
        float readyStart = -1f;
        AbilityMutation shownAbility;

        /// <summary>Progreso que se dibuja ahora, de 0 a 1 (para las pruebas).</summary>
        public float ShownProgress { get; private set; }

        /// <summary>True mientras suena el destello de «listo» (para las pruebas).</summary>
        public bool IsFlashing => readyStart >= 0f;

        /// <summary>True mientras el órgano está listo y pulsa (para las pruebas).</summary>
        public bool IsPulsing => pulsing;

        /// <summary>True si se ve el recuadro vacío (sin órgano).</summary>
        public bool ShowsEmpty => emptyIconImage != null && emptyIconImage.enabled;

        internal void ConfigureArt(Sprite newPanel, Sprite newPanelEmpty, Sprite newEmptyIcon, Sprite newSapFill, Sprite newNotch,
            Sprite[] newBubbles, Sprite[] newTop, Sprite[] newReady, Sprite[] newPulse)
        {
            panel = newPanel;
            panelEmpty = newPanelEmpty;
            emptyIcon = newEmptyIcon;
            sapFill = newSapFill;
            notch = newNotch;
            sapBubbles = newBubbles;
            sapTop = newTop;
            readyFrames = newReady;
            pulseFrames = newPulse;
        }

        internal void SetAbility(PlayerAbility newAbility) => ability = newAbility;

        void Awake() => Build();

        // Las piezas se crean una vez, en el orden de dibujo: recuadro, icono apagado, icono de color (con la línea del borde dentro),
        // savia, burbujas, superficie, muescas y, encima de todo, el destello y el pulso.
        void Build()
        {
            if (built)
                return;
            built = true;

            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(8f, 8f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            panelImage = Piece("Panel", rect, 0f, 0f, PanelWidth, PanelHeight);
            emptyIconImage = Piece("EmptyIcon", rect, IconX, IconY, IconSize, IconSize);
            emptyIconImage.sprite = emptyIcon;
            dimIcon = Piece("IconDim", rect, IconX, IconY, IconSize, IconSize);
            colorIcon = Piece("IconColor", rect, IconX, IconY, IconSize, IconSize);
            colorIcon.type = Image.Type.Filled;
            colorIcon.fillMethod = Image.FillMethod.Vertical;
            colorIcon.fillOrigin = (int)Image.OriginVertical.Bottom;
            // La línea del borde solo se ve sobre el icono de color: lo que no cubre el icono la recorta.
            colorIcon.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            edgeLine = Piece("EdgeLine", colorIcon.rectTransform, 0f, 0f, IconSize, 1f);
            edgeLine.color = EdgeLine;
            edgeLine.sprite = null;

            fill = Piece("SapFill", rect, SapX, SapY, SapWidth, SapHeight);
            fill.sprite = sapFill;
            Filled(fill);
            bubbles = Piece("SapBubbles", rect, SapX, SapY, SapWidth, SapHeight);
            Filled(bubbles);
            top = Piece("SapTop", rect, SapX, SapY, SapWidth, 2f);
            overlay = Piece("Overlay", rect, 0f, 0f, PanelWidth, PanelHeight);
            SetAll(false);
        }

        static void Filled(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        // Una imagen anclada arriba a la izquierda del recuadro, en píxeles desde esa esquina.
        static Image Piece(string name, RectTransform parent, float x, float y, float width, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            var image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = false;
            return image;
        }

        void SetAll(bool visible)
        {
            foreach (var image in new[] { emptyIconImage, dimIcon, colorIcon, edgeLine, fill, bubbles, top, overlay })
                image.enabled = visible;
            foreach (var mark in notches)
                mark.enabled = visible;
        }

        void LateUpdate()
        {
            Build();
            if (ability == null)
            {
                var player = GameObject.FindWithTag("Player");
                ability = player != null ? player.GetComponent<PlayerAbility>() : null;
            }

            var current = ability != null ? ability.Ability : null;
            if (current == null)
            {
                ShowEmpty();
                return;
            }

            if (current != shownAbility)
            {
                // Otro órgano: empieza listo, sin destello ni sonido de recarga.
                shownAbility = current;
                wasCharging = false;
                pulsing = false;
                readyStart = -1f;
            }

            panelImage.sprite = panel;
            emptyIconImage.enabled = false;
            var icon = current.LargeIcon;
            bool charging = ability.IsCharging;

            // Al terminar de recargarse: el destello de «listo» (una vez) y después el pulso.
            if (wasCharging && !charging)
            {
                readyStart = Time.time;
                pulsing = false;
            }
            wasCharging = charging;

            ShownProgress = charging ? ability.CooldownProgress : 1f;
            ShowIcon(icon, charging);
            ShowSap(charging);
            ShowNotches(charging && ability.UsesKills ? ability.KillsRequired : 0);
            ShowOverlay(charging);
        }

        void ShowEmpty()
        {
            shownAbility = null;
            wasCharging = false;
            pulsing = false;
            readyStart = -1f;
            ShownProgress = 0f;
            SetAll(false);
            panelImage.sprite = panelEmpty;
            emptyIconImage.enabled = emptyIcon != null;
        }

        // Recargando: el icono apagado y el de color subiendo de abajo arriba; listo: el de color entero.
        void ShowIcon(Sprite icon, bool charging)
        {
            colorIcon.sprite = icon;
            colorIcon.enabled = icon != null;
            if (!charging)
            {
                dimIcon.enabled = false;
                edgeLine.enabled = false;
                colorIcon.fillAmount = 1f;
                return;
            }

            // El color sube de 1 en 1 px: el corte cae en una fila del icono.
            int cut = Mathf.RoundToInt(IconSize * (1f - ShownProgress));
            colorIcon.fillAmount = (IconSize - cut) / IconSize;
            dimIcon.sprite = Dimmed(icon);
            dimIcon.enabled = dimIcon.sprite != null;
            edgeLine.enabled = cut > 0 && cut < IconSize;
            // La línea ocupa la fila de arriba de lo que ya tiene color (a `cut` filas del borde de arriba del icono).
            edgeLine.rectTransform.anchoredPosition = new Vector2(0f, -cut);
        }

        // El tubo: la savia sube con el progreso, con burbujas encima y la superficie temblando.
        void ShowSap(bool charging)
        {
            int height = charging ? Mathf.RoundToInt(SapHeight * ShownProgress) : 0;
            // Listo: el tubo queda vacío (el recuadro se ilumina con el pulso).
            fill.enabled = bubbles.enabled = height > 0;
            fill.fillAmount = bubbles.fillAmount = height / SapHeight;
            if (sapBubbles.Length > 0)
                bubbles.sprite = sapBubbles[Mathf.FloorToInt(Time.time / BubbleFrameSeconds) % sapBubbles.Length];

            top.enabled = height > 0 && sapTop.Length > 0;
            if (top.enabled)
            {
                top.sprite = sapTop[Mathf.FloorToInt(Time.time / SapTopFrameSeconds) % sapTop.Length];
                // Su fila de abajo va 1 px por encima de la altura del relleno.
                top.rectTransform.anchoredPosition = new Vector2(SapX, -(SapY + SapHeight - height - 1f));
            }
        }

        // Una muesca por enemigo que falta menos uno, repartidas por el tubo.
        void ShowNotches(int needed)
        {
            int count = Mathf.Max(0, needed - 1);
            while (notches.Count < count)
            {
                var mark = Piece("Notch", (RectTransform)transform, SapX, SapY, SapWidth, 1f);
                mark.sprite = notch;
                notches.Add(mark);
            }

            for (int i = 0; i < notches.Count; i++)
            {
                bool used = i < count;
                notches[i].enabled = used;
                if (used)
                    notches[i].rectTransform.anchoredPosition = new Vector2(SapX, -(SapY + SapHeight - Mathf.Round(SapHeight * (i + 1) / needed)));
            }
        }

        // Encima de todo: el destello de listo (una vez) y, después, el pulso en bucle.
        void ShowOverlay(bool charging)
        {
            overlay.enabled = false;
            if (charging)
                return;

            if (readyStart >= 0f)
            {
                int frame = Mathf.FloorToInt((Time.time - readyStart) / ReadyFrameSeconds);
                if (frame < readyFrames.Length)
                {
                    overlay.sprite = readyFrames[frame];
                    overlay.enabled = true;
                    return;
                }
                readyStart = -1f;
                pulsing = true;
            }

            if (pulsing && pulseFrames.Length > 0)
            {
                overlay.sprite = pulseFrames[Mathf.FloorToInt(Time.time / PulseFrameSeconds) % pulseFrames.Length];
                overlay.enabled = true;
            }
        }

        /// <summary>
        /// El icono apagado: la luminancia pasa a 4 tonos verdosos y azulados del HUD, conservando el contorno. Se hace una vez por icono
        /// (se copia por una textura temporal, así no hace falta que el icono sea legible).
        /// </summary>
        static Sprite Dimmed(Sprite source)
        {
            if (source == null)
                return null;
            if (dimmed.TryGetValue(source, out var cached) && cached != null)
                return cached;

            var rect = source.textureRect;
            int width = Mathf.RoundToInt(rect.width), height = Mathf.RoundToInt(rect.height);
            var texture = source.texture;
            var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Color32[] pixels;
            try
            {
                Graphics.Blit(texture, target);
                RenderTexture.active = target;
                var read = new Texture2D(width, height, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                pixels = read.GetPixels32();
                Object.Destroy(read);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.a == 0)
                    continue;
                if (Mathf.Abs(pixel.r - Outline.r) <= 3 && Mathf.Abs(pixel.g - Outline.g) <= 3 && Mathf.Abs(pixel.b - Outline.b) <= 3)
                {
                    pixels[i] = new Color32(Outline.r, Outline.g, Outline.b, 180);
                    continue;
                }
                float luminance = (pixel.r * 3f + pixel.g * 6f + pixel.b) / 10f;
                pixels[i] = DimTones[Mathf.Min(3, Mathf.FloorToInt(luminance / 56f))];
            }

            var dimTexture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            dimTexture.SetPixels32(pixels);
            dimTexture.Apply();
            var result = Sprite.Create(dimTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
            dimmed[source] = result;
            return result;
        }
    }
}

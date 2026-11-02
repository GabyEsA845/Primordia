using Primordia.Mutations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Tarjeta de una mutación en la pantalla de elección. Si es una mutación que ya se lleva, es una mejora:
    /// dice "Nivel I > II" con una flechita y describe lo que añade el siguiente nivel.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MutationCard : MonoBehaviour
    {
        [SerializeField] Image colorStrip;
        [SerializeField] Text title;
        [SerializeField] Text details;
        [SerializeField] Text description;
        [SerializeField] Text replaces;
        [Tooltip("Icono grande de la mutación (32×32).")]
        [SerializeField] Image icon;
        [Tooltip("Icono de la ranura, junto a su nombre.")]
        [SerializeField, FormerlySerializedAs("lineageIcon")] Image slotIcon;
        [Tooltip("Iconos de ranura en el orden Boca, Extremidades, Piel, Órgano.")]
        [SerializeField, FormerlySerializedAs("lineageIcons")] Sprite[] slotIcons = new Sprite[4];
        [Tooltip("Insignias de nivel para el icono grande, de nivel 1 a 3.")]
        [SerializeField] Sprite[] levelBadges = new Sprite[3];
        [Tooltip("Gema dorada de mutación especial (esquina del icono).")]
        [SerializeField] Sprite specialBadge;
        [Tooltip("Flechita junto a \"Nivel I > II\" en las mejoras.")]
        [SerializeField] Sprite upgradeArrow;

        [Header("Pista de linaje")]
        [Tooltip("Color de los bordes cuando la mutación acerca un linaje que el jugador ya ha empezado.")]
        [SerializeField] Color lineageGlowColor = new(1f, 0.85f, 0.35f, 1f);
        [Tooltip("Color con el que se tiñe todo el fondo de la tarjeta recomendada (dorado cálido).")]
        [SerializeField] Color lineageFillColor = new(1f, 0.74f, 0.15f, 1f);
        [Tooltip("Cuánto se tiñe el fondo (0 a 1); no late.")]
        [SerializeField, Range(0f, 1f)] float lineageFillAlpha = 0.6f;
        [Tooltip("Transparencia mínima y máxima de los bordes; late entre las dos.")]
        [SerializeField] Vector2 lineageBorderAlpha = new(0.4f, 1f);
        [Tooltip("Segundos de cada latido (tiempo real: la elección pausa el juego).")]
        [SerializeField, Min(0.1f)] float lineageGlowPeriod = 1.2f;

        Image badge;
        Image arrow;
        Image fill;
        Image edgeLayer;

        /// <summary>True si la tarjeta brilla porque la mutación ayuda a completar un linaje empezado.</summary>
        public bool HasLineageHint => fill != null && fill.enabled;

        /// <summary>True si la última mutación mostrada era una mejora de una que ya se lleva.</summary>
        public bool IsUpgrade { get; private set; }

        public Button Button => GetComponent<Button>();

        /// <param name="current">Mutación que hay ahora en esa ranura, o null si está libre.</param>
        /// <param name="currentLevel">Nivel de <paramref name="current"/>.</param>
        /// <param name="lineageHint">Brilla suavemente: la mutación acerca un linaje (ver <see cref="LineageData.Advances"/>).</param>
        public void Show(MutationData mutation, MutationData current, int currentLevel = 1, bool lineageHint = false)
        {
            IsUpgrade = current == mutation;
            int level = IsUpgrade ? Mathf.Min(currentLevel + 1, MutationData.MaxLevel) : 1;

            if (colorStrip != null)
                colorStrip.color = mutation.Color;

            title.text = mutation.DisplayName;
            details.text = MutationText.SlotName(mutation.Slot);

            if (IsUpgrade)
            {
                var extra = mutation.LevelDescription(level);
                description.text = MutationText.WithLine(string.IsNullOrEmpty(extra) ? mutation.Description : extra,
                    MutationText.LineageList(mutation));
                replaces.text = MutationText.LevelChange(currentLevel, level);
            }
            else
            {
                description.text = MutationText.WithLine(mutation.Description, MutationText.LineageList(mutation));
                replaces.text = current != null ? $"Reemplaza: {current.DisplayName}" : "Ranura libre";
            }

            SetSprite(icon, mutation.LargeIcon);
            int slot = (int)mutation.Slot;
            SetSprite(slotIcon, slotIcons != null && slot < slotIcons.Length ? slotIcons[slot] : null);

            if (icon != null && badge == null)
                badge = LevelBadge.Create(icon, new Vector2(18f, 26f));
            LevelBadge.Show(badge, levelBadges, icon != null && icon.enabled ? level : 0);
            LevelBadge.ShowSpecial(icon, specialBadge, mutation.IsSpecial);
            ShowArrow(IsUpgrade);
            ShowGlow(lineageHint && !IsUpgrade);
        }

        /// <summary>Tamaño mínimo de letra al que se encoge la descripción de las tarjetas de la sala de cambio.</summary>
        const int ExchangeMinFontSize = 6;

        /// <summary>
        /// Tarjeta de la sala de cambio: la mutación que recibirías con lo que hace en el nivel con el que entra (la
        /// descripción y solo la mejora de ese nivel, como las tarjetas de mejora), ese nivel en la insignia y, abajo,
        /// <paramref name="note"/> (por cuál la cambias). Si aun así no cabe, la descripción encoge la letra.
        /// </summary>
        public void ShowExchange(MutationData mutation, int level, string note)
        {
            IsUpgrade = false;
            if (colorStrip != null)
                colorStrip.color = mutation.Color;
            title.text = mutation.DisplayName;
            details.text = MutationText.SlotName(mutation.Slot);
            description.text = MutationText.WithLine(MutationText.CurrentDescription(mutation, level), MutationText.LineageList(mutation));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            description.resizeTextMaxSize = description.fontSize;
            description.resizeTextMinSize = Mathf.Min(ExchangeMinFontSize, description.fontSize);
            description.resizeTextForBestFit = true;
            // Ocupa solo el hueco que dejan el título, el icono y la nota (si pidiera su alto entero, la tarjeta aplastaría
            // a los demás); dentro de ese hueco, la letra encoge lo que haga falta.
            if (description.TryGetComponent<LayoutElement>(out var element))
            {
                element.preferredHeight = 1f;
                element.flexibleHeight = 1f;
            }
            replaces.text = note ?? "";

            SetSprite(icon, mutation.LargeIcon);
            int slot = (int)mutation.Slot;
            SetSprite(slotIcon, slotIcons != null && slot < slotIcons.Length ? slotIcons[slot] : null);
            if (icon != null && badge == null)
                badge = LevelBadge.Create(icon, new Vector2(18f, 26f));
            LevelBadge.Show(badge, levelBadges, icon != null && icon.enabled ? level : 0);
            LevelBadge.ShowSpecial(icon, specialBadge, false);
            ShowArrow(false);
            ShowGlow(false);
        }

        /// <summary>
        /// Pista de la tarjeta que acerca el linaje más avanzado: dos capas por encima del fondo y por debajo del contenido,
        /// con la silueta exacta de la tarjeta (se calculan a partir de su sprite): una tiñe todo el fondo de dorado
        /// (fija) y la otra dibuja solo los bordes de la tarjeta en dorado, y late.
        /// </summary>
        void ShowGlow(bool visible)
        {
            if (fill == null && visible)
            {
                var background = GetComponent<Image>();
                BuildMasks(background != null ? background.sprite : null);
                fill = CreateLayer("LineageFill", background, silhouette);
                fill.transform.SetAsFirstSibling();
                edgeLayer = CreateLayer("LineageEdge", background, edge);
                edgeLayer.transform.SetSiblingIndex(1);
            }

            if (fill != null)
            {
                fill.enabled = visible && fill.sprite != null;
                edgeLayer.enabled = visible && edgeLayer.sprite != null;
                UpdateGlow();
            }
        }

        // Ancho de los bordes dorados, en píxeles del sprite de la tarjeta.
        const int EdgeWidth = 2;

        static Sprite maskSource;
        static Sprite silhouette;
        static Sprite edge;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            maskSource = null;
            silhouette = null;
            edge = null;
        }

        /// <summary>
        /// Dos copias blancas del sprite del fondo, con sus mismos bordes de 9 trozos: su silueta entera (la forma de la
        /// tarjeta, redondeces incluidas) y solo sus píxeles de borde (los que tocan fuera de la tarjeta). Teñidas de
        /// dorado dan un color vivo y no el oscuro que saldría de teñir el dibujo.
        /// </summary>
        static void BuildMasks(Sprite source)
        {
            if (source == null || maskSource == source)
                return;
            maskSource = source;
            silhouette = null;
            edge = null;

            try
            {
                var texture = source.texture;
                var rect = source.rect;
                var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, target);
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                int w = (int)rect.width;
                int h = (int)rect.height;
                var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                var pixels = copy.GetPixels32();
                Object.Destroy(copy);

                bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && pixels[y * w + x].a > 127;

                var silhouettePixels = new Color32[w * h];
                var edgePixels = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!Solid(x, y))
                            continue;
                        silhouettePixels[y * w + x] = new Color32(255, 255, 255, 255);
                        bool onEdge = false;
                        for (int dy = -EdgeWidth; dy <= EdgeWidth && !onEdge; dy++)
                        {
                            for (int dx = -EdgeWidth; dx <= EdgeWidth; dx++)
                            {
                                if (!Solid(x + dx, y + dy))
                                {
                                    onEdge = true;
                                    break;
                                }
                            }
                        }
                        if (onEdge)
                            edgePixels[y * w + x] = new Color32(255, 255, 255, 255);
                    }
                }

                silhouette = MakeSprite(source, w, h, silhouettePixels);
                edge = MakeSprite(source, w, h, edgePixels);
            }
            catch (System.Exception)
            {
                // Sin poder leer el dibujo no hay pista de color: la tarjeta se queda como estaba.
                silhouette = null;
                edge = null;
            }
        }

        static Sprite MakeSprite(Sprite source, int w, int h, Color32[] pixels)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, w, h), source.pivot / source.rect.size, source.pixelsPerUnit, 0,
                SpriteMeshType.FullRect, source.border);
        }

        Image CreateLayer(string layerName, Image background, Sprite sprite)
        {
            var layer = new GameObject(layerName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            layer.transform.SetParent(transform, false);
            layer.raycastTarget = false;
            layer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = layer.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            layer.sprite = sprite;
            if (background != null)
            {
                layer.type = background.type;
                layer.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
            }
            return layer;
        }

        void Update()
        {
            if (fill != null && fill.enabled)
                UpdateGlow();
        }

        void UpdateGlow()
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / lineageGlowPeriod);
            var color = lineageGlowColor;
            color.a = Mathf.Lerp(lineageBorderAlpha.x, lineageBorderAlpha.y, t);
            edgeLayer.color = color;
            var tint = lineageFillColor;
            tint.a = lineageFillAlpha;
            fill.color = tint;
        }

        /// <summary>La flechita, a la izquierda del texto "Nivel I > II".</summary>
        void ShowArrow(bool visible)
        {
            if (replaces == null)
                return;

            if (arrow == null && upgradeArrow != null)
            {
                arrow = new GameObject("UpgradeArrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                arrow.transform.SetParent(replaces.transform, false);
                arrow.raycastTarget = false;
                arrow.sprite = upgradeArrow;
                arrow.rectTransform.sizeDelta = upgradeArrow.rect.size;
                arrow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }

            if (arrow == null)
                return;

            arrow.enabled = visible;
            float textWidth = Mathf.Min(replaces.preferredWidth, replaces.rectTransform.rect.width);
            arrow.rectTransform.anchoredPosition = new Vector2(Mathf.Round(-textWidth / 2f - 6f), 0f);
        }

        /// <summary>Sin sprite, la imagen se oculta (pero sigue ocupando su sitio en la tarjeta).</summary>
        static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        internal Text Details => details;

        internal void ConfigureIcons(Image newIcon, Image newSlotIcon)
        {
            icon = newIcon;
            slotIcon = newSlotIcon;
        }

        internal void SetSlotIcons(Sprite[] sprites) => slotIcons = sprites;

        internal void SetSpecialBadge(Sprite sprite) => specialBadge = sprite;

        internal void SetLevelSprites(Sprite[] badges, Sprite arrowSprite)
        {
            levelBadges = badges;
            upgradeArrow = arrowSprite;
        }

        internal void Configure(Image strip, Text newTitle, Text newDetails, Text newDescription, Text newReplaces)
        {
            colorStrip = strip;
            title = newTitle;
            details = newDetails;
            description = newDescription;
            replaces = newReplaces;
        }
    }
}

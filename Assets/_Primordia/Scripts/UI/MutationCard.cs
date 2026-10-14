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
        [Tooltip("Flechita junto a \"Nivel I > II\" en las mejoras.")]
        [SerializeField] Sprite upgradeArrow;

        [Header("Pista de linaje")]
        [Tooltip("Color del brillo cuando la mutación acerca un linaje que el jugador ya ha empezado.")]
        [SerializeField] Color lineageGlowColor = new(1f, 0.85f, 0.35f, 1f);
        [Tooltip("Transparencia mínima y máxima del brillo; late entre las dos.")]
        [SerializeField] Vector2 lineageGlowAlpha = new(0.08f, 0.32f);
        [Tooltip("Segundos de cada latido (tiempo real: la elección pausa el juego).")]
        [SerializeField, Min(0.1f)] float lineageGlowPeriod = 1.2f;

        Image badge;
        Image arrow;
        Image glow;

        /// <summary>True si la tarjeta brilla porque la mutación ayuda a completar un linaje empezado.</summary>
        public bool HasLineageHint => glow != null && glow.enabled;

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
            ShowArrow(IsUpgrade);
            ShowGlow(lineageHint && !IsUpgrade);
        }

        /// <summary>
        /// Capa del mismo sprite que el fondo de la tarjeta, teñida de dorado y un poco más grande, que late por
        /// encima del fondo y por debajo del contenido.
        /// </summary>
        void ShowGlow(bool visible)
        {
            if (glow == null && visible)
            {
                glow = new GameObject("LineageGlow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                glow.transform.SetParent(transform, false);
                glow.transform.SetAsFirstSibling();
                glow.raycastTarget = false;
                glow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var background = GetComponent<Image>();
                if (background != null && background.sprite != null)
                {
                    glow.sprite = background.sprite;
                    glow.type = background.type;
                    glow.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
                }
                var rect = glow.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-2f, -2f);
                rect.offsetMax = new Vector2(2f, 2f);
            }

            if (glow != null)
            {
                glow.enabled = visible;
                UpdateGlow();
            }
        }

        void Update()
        {
            if (glow != null && glow.enabled)
                UpdateGlow();
        }

        void UpdateGlow()
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / lineageGlowPeriod);
            var color = lineageGlowColor;
            color.a = Mathf.Lerp(lineageGlowAlpha.x, lineageGlowAlpha.y, t);
            glow.color = color;
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

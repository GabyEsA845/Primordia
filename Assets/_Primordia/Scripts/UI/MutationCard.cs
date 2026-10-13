using Primordia.Mutations;
using UnityEngine;
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
        [Tooltip("Icono del linaje, junto a la ranura y el linaje.")]
        [SerializeField] Image lineageIcon;
        [Tooltip("Iconos de linaje en el orden Depredador, Acorazado, Tóxico.")]
        [SerializeField] Sprite[] lineageIcons = new Sprite[3];
        [Tooltip("Insignias de nivel para el icono grande, de nivel 1 a 3.")]
        [SerializeField] Sprite[] levelBadges = new Sprite[3];
        [Tooltip("Flechita junto a \"Nivel I > II\" en las mejoras.")]
        [SerializeField] Sprite upgradeArrow;

        Image badge;
        Image arrow;

        /// <summary>True si la última mutación mostrada era una mejora de una que ya se lleva.</summary>
        public bool IsUpgrade { get; private set; }

        public Button Button => GetComponent<Button>();

        /// <param name="current">Mutación que hay ahora en esa ranura, o null si está libre.</param>
        /// <param name="currentLevel">Nivel de <paramref name="current"/>.</param>
        public void Show(MutationData mutation, MutationData current, int currentLevel = 1)
        {
            IsUpgrade = current == mutation;
            int level = IsUpgrade ? Mathf.Min(currentLevel + 1, MutationData.MaxLevel) : 1;

            if (colorStrip != null)
                colorStrip.color = mutation.Color;

            title.text = mutation.DisplayName;
            details.text = $"{MutationText.SlotName(mutation.Slot)} · {MutationText.LineageName(mutation.Lineage)}";

            if (IsUpgrade)
            {
                var extra = mutation.LevelDescription(level);
                description.text = string.IsNullOrEmpty(extra) ? mutation.Description : extra;
                replaces.text = MutationText.LevelChange(currentLevel, level);
            }
            else
            {
                description.text = mutation.Description;
                replaces.text = current != null ? $"Reemplaza: {current.DisplayName}" : "Ranura libre";
            }

            SetSprite(icon, mutation.LargeIcon);
            int lineage = (int)mutation.Lineage;
            SetSprite(lineageIcon, lineageIcons != null && lineage < lineageIcons.Length ? lineageIcons[lineage] : null);

            if (icon != null && badge == null)
                badge = LevelBadge.Create(icon, new Vector2(18f, 26f));
            LevelBadge.Show(badge, levelBadges, icon != null && icon.enabled ? level : 0);
            ShowArrow(IsUpgrade);
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

        internal void ConfigureIcons(Image newIcon, Image newLineageIcon)
        {
            icon = newIcon;
            lineageIcon = newLineageIcon;
        }

        internal void SetLineageIcons(Sprite[] sprites) => lineageIcons = sprites;

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

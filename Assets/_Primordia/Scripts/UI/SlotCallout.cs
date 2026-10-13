using Primordia.Mutations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Recuadro de una ranura en el panel de la criatura (pausa): marco, icono de la mutación o silueta de la
    /// ranura libre. Con el ratón encima se resalta y pide a <see cref="CreaturePanel"/> su cuadro de información.
    /// </summary>
    public class SlotCallout : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] MutationSlot slot;
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] Sprite normalFrame;
        [SerializeField] Sprite highlightedFrame;
        [Tooltip("Silueta apagada de la ranura libre.")]
        [SerializeField] Sprite emptyIcon;
        [Tooltip("Insignias de nivel para los iconos de 16, de nivel 1 a 3.")]
        [SerializeField] Sprite[] levelBadges = new Sprite[3];

        Image badge;

        internal CreaturePanel Panel;

        public MutationSlot Slot => slot;
        public MutationData Mutation { get; private set; }
        public int Level { get; private set; }
        public bool IsHighlighted { get; private set; }
        public Image Icon => icon;
        public RectTransform Rect => (RectTransform)transform;

        internal void Configure(MutationSlot newSlot, Image newFrame, Image newIcon)
        {
            slot = newSlot;
            frame = newFrame;
            icon = newIcon;
        }

        internal void ConfigureLook(Sprite normal, Sprite highlighted, Sprite empty)
        {
            normalFrame = normal;
            highlightedFrame = highlighted;
            emptyIcon = empty;
            SetHighlighted(IsHighlighted);
        }

        public void Show(MutationData mutation, int level = 1)
        {
            Mutation = mutation;
            Level = mutation != null ? level : 0;
            var sprite = mutation != null && mutation.Icon != null ? mutation.Icon : emptyIcon;
            icon.sprite = sprite;
            icon.enabled = sprite != null;

            if (badge == null)
                badge = LevelBadge.Create(icon, new Vector2(8f, 12f));
            LevelBadge.Show(badge, levelBadges, Level);
        }

        internal void SetLevelBadges(Sprite[] badges) => levelBadges = badges;

        public void SetHighlighted(bool highlighted)
        {
            IsHighlighted = highlighted;
            if (frame == null)
                return;

            var sprite = highlighted && highlightedFrame != null ? highlightedFrame : normalFrame;
            if (sprite != null)
                frame.sprite = sprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Panel != null)
                Panel.Hover(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Panel != null)
                Panel.Unhover(this);
        }

        void OnDisable() => SetHighlighted(false);
    }
}

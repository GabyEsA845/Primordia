using Primordia.Mutations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Carta de una Boca en la ventana de la sala de paso: la criatura en que te convertirás, en reposo, y al pasar el
    /// ratón (o seleccionarla) crece un poco y la criatura enseña su ataque en bucle. Debajo, icono, nombre, familia y lo
    /// que hace en el nivel I.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MouthCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Image creature;
        [SerializeField] Image icon;
        [SerializeField] Image colorStrip;
        [SerializeField] Text title;
        [SerializeField] Text family;
        [SerializeField] Text description;
        [Tooltip("Escala de la carta con el ratón encima.")]
        [SerializeField, Min(1f)] float hoverScale = 1.06f;
        [SerializeField, Min(0.01f)] float idleFrameDuration = 0.18f;
        [SerializeField, Min(0.01f)] float attackFrameDuration = 0.09f;
        [Tooltip("Segundos entre dos ataques de la criatura mientras la carta está señalada.")]
        [SerializeField, Min(0f)] float attackPause = 0.35f;

        MutationData mutation;
        UISpriteAnimation creatureAnimation;
        bool hovered;
        float scale = 1f;

        public Button Button => GetComponent<Button>();
        public MutationData Mutation => mutation;

        /// <summary>True mientras la criatura enseña su ataque (ratón encima o seleccionada).</summary>
        public bool ShowsAttack => hovered;

        public void Show(MutationData newMutation)
        {
            mutation = newMutation;
            hovered = false;
            scale = 1f;
            transform.localScale = Vector3.one;

            if (title != null)
                title.text = mutation.DisplayName;
            if (family != null)
                family.text = MutationText.FamilyName(mutation.Family);
            if (description != null)
                description.text = mutation.Description;
            if (colorStrip != null)
                colorStrip.color = mutation.Color;
            if (icon != null)
            {
                icon.sprite = mutation.Icon;
                icon.enabled = mutation.Icon != null;
            }

            PlayCreature();
        }

        void PlayCreature()
        {
            if (creature == null || mutation == null)
                return;
            if (creatureAnimation == null && !creature.TryGetComponent(out creatureAnimation))
                creatureAnimation = creature.gameObject.AddComponent<UISpriteAnimation>();

            var attack = mutation.CreatureAttackFrames;
            var idle = mutation.CreatureIdleFrames;
            if (hovered && attack.Length > 0)
            {
                // Entre dos ataques se queda quieta en el primer cuadro del reposo (la espera de la animación la ocultaría).
                int rest = idle.Length > 0 ? Mathf.RoundToInt(attackPause / attackFrameDuration) : 0;
                var frames = new Sprite[attack.Length + rest];
                attack.CopyTo(frames, 0);
                for (int i = attack.Length; i < frames.Length; i++)
                    frames[i] = idle[0];
                creatureAnimation.Configure(frames, attackFrameDuration, newLoop: true);
            }
            else
            {
                creatureAnimation.Configure(idle, idleFrameDuration, newLoop: true);
            }
        }

        void Update()
        {
            float target = hovered && Button.interactable ? hoverScale : 1f;
            scale = Mathf.MoveTowards(scale, target, Time.unscaledDeltaTime * 1.5f);
            transform.localScale = Vector3.one * scale;
        }

        void SetHovered(bool value)
        {
            if (hovered == value)
                return;
            hovered = value;
            PlayCreature();
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);
        public void OnSelect(BaseEventData eventData) => SetHovered(true);
        public void OnDeselect(BaseEventData eventData) => SetHovered(false);

        internal void Configure(Image newCreature, Image newIcon, Image newStrip, Text newTitle, Text newFamily, Text newDescription)
        {
            creature = newCreature;
            icon = newIcon;
            colorStrip = newStrip;
            title = newTitle;
            family = newFamily;
            description = newDescription;
        }
    }
}

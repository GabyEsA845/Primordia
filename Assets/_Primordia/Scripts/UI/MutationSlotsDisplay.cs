using System.Collections.Generic;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Fila de 4 ranuras del cuerpo en el HUD (Boca, Extremidades, Piel y Órgano): icono de la mutación que lleva
    /// cada una o la silueta apagada si está libre. Al cambiar una ranura da un pequeño salto, y la del Órgano
    /// se ve oscurecida mientras se recarga la habilidad, aclarándose de abajo arriba.
    /// Crea sus propias ranuras como hijos; conviene ponerlo en un objeto con HorizontalLayoutGroup.
    /// </summary>
    public class MutationSlotsDisplay : MonoBehaviour
    {
        static readonly MutationSlot[] Order = { MutationSlot.Mouth, MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ };

        [Tooltip("Mutaciones a mostrar. Si está vacío, usa las del objeto con la etiqueta Player.")]
        [SerializeField] PlayerMutations player;
        [Tooltip("Marco de cada ranura (20×20).")]
        [SerializeField] Sprite frameSprite;
        [Tooltip("Silueta de cada ranura libre, en el orden Boca, Extremidades, Piel, Órgano. Vacía = sin icono.")]
        [SerializeField] Sprite[] emptySprites = new Sprite[4];
        [Tooltip("Insignias de nivel para los iconos de 16, de nivel 1 a 3.")]
        [SerializeField] Sprite[] levelBadges = new Sprite[3];
        [SerializeField] Vector2 slotSize = new(20f, 20f);
        [SerializeField] Vector2 iconSize = new(16f, 16f);
        [Tooltip("Color que oscurece el icono del Órgano mientras se recarga.")]
        [SerializeField] Color cooldownShade = new(0f, 0f, 0f, 0.7f);
        [Tooltip("Tamaño al empezar el salto; vuelve a 1 en popDuration segundos (tiempo real).")]
        [SerializeField, Min(1f)] float popScale = 1.25f;
        [SerializeField, Min(0.01f)] float popDuration = 0.2f;

        class Slot
        {
            public RectTransform Root;
            public Image Icon;
            public Image Shade;
            public Image Badge;
            public float PopStart = -1f;
        }

        readonly Dictionary<MutationSlot, Slot> slots = new();
        int startFrame;

        void Start()
        {
            startFrame = Time.frameCount;

            if (player == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                    player = playerObject.GetComponent<PlayerMutations>();
            }

            foreach (var slot in Order)
                slots[slot] = CreateSlot(slot);

            if (player == null)
                return;

            player.Changed += OnChanged;
            foreach (var slot in Order)
                Refresh(slot);
        }

        void OnDestroy()
        {
            if (player != null)
                player.Changed -= OnChanged;
        }

        internal void Configure(PlayerMutations newPlayer) => player = newPlayer;

        internal void ConfigureLook(Sprite frame, Sprite[] empty)
        {
            frameSprite = frame;
            emptySprites = empty;
        }

        public Image IconOf(MutationSlot slot) => slots.TryGetValue(slot, out var s) ? s.Icon : null;
        public Image ShadeOf(MutationSlot slot) => slots.TryGetValue(slot, out var s) ? s.Shade : null;
        public RectTransform SlotOf(MutationSlot slot) => slots.TryGetValue(slot, out var s) ? s.Root : null;
        public Image BadgeOf(MutationSlot slot) => slots.TryGetValue(slot, out var s) ? s.Badge : null;

        internal void SetLevelBadges(Sprite[] badges) => levelBadges = badges;

        void OnChanged(MutationSlot slot, MutationData mutation)
        {
            Refresh(slot);
            // Las mutaciones iniciales se equipan en el primer cuadro: sin salto.
            if (Time.frameCount != startFrame && slots.TryGetValue(slot, out var s))
                s.PopStart = Time.unscaledTime;
        }

        void Refresh(MutationSlot slot)
        {
            if (!slots.TryGetValue(slot, out var s))
                return;

            var mutation = player != null ? player.Get(slot) : null;
            int index = System.Array.IndexOf(Order, slot);
            var sprite = mutation != null ? mutation.Icon
                : emptySprites != null && index < emptySprites.Length ? emptySprites[index] : null;

            s.Icon.sprite = sprite;
            s.Icon.enabled = sprite != null;
            s.Shade.sprite = sprite;
            LevelBadge.Show(s.Badge, levelBadges, mutation != null ? player.LevelOf(slot) : 0);
        }

        void LateUpdate()
        {
            foreach (var s in slots.Values)
            {
                if (s.PopStart < 0f)
                    continue;

                float t = (Time.unscaledTime - s.PopStart) / popDuration;
                if (t >= 1f)
                {
                    s.PopStart = -1f;
                    s.Root.localScale = Vector3.one;
                }
                else
                {
                    s.Root.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, t);
                }
            }

            UpdateCooldown();
        }

        void UpdateCooldown()
        {
            if (!slots.TryGetValue(MutationSlot.Organ, out var organ))
                return;

            var ability = player != null && player.Get(MutationSlot.Organ) != null ? player.Ability : null;
            bool charging = ability != null && ability.IsCharging && organ.Shade.sprite != null;
            organ.Shade.enabled = charging;
            if (charging)
                organ.Shade.fillAmount = 1f - ability.CooldownProgress;
        }

        Slot CreateSlot(MutationSlot slot)
        {
            var root = new GameObject($"Slot_{slot}", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = slotSize;

            var frame = root.GetComponent<Image>();
            frame.raycastTarget = false;
            frame.sprite = frameSprite;
            // Sin arte, un recuadro oscuro provisional.
            if (frameSprite == null)
                frame.color = new Color(0.06f, 0.1f, 0.08f, 0.8f);

            var icon = CreateImage("Icon", rect);
            // Oscurece la parte del icono que falta por recargar: se vacía desde abajo.
            var shade = CreateImage("CooldownShade", rect);
            shade.color = cooldownShade;
            shade.type = Image.Type.Filled;
            shade.fillMethod = Image.FillMethod.Vertical;
            shade.fillOrigin = (int)Image.OriginVertical.Top;
            shade.enabled = false;

            // Encima del icono y de la sombra de recarga.
            var badge = LevelBadge.Create(icon, new Vector2(8f, 12f));
            badge.transform.SetParent(rect, true);

            return new Slot { Root = rect, Icon = icon, Shade = shade, Badge = badge };
        }

        Image CreateImage(string name, Transform parent)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            ((RectTransform)imageObject.transform).sizeDelta = iconSize;
            var image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}

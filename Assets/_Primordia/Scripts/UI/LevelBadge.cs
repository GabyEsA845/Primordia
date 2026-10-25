using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Insignia de nivel sobre el icono de una mutación (puntos o gemas). Se crea como hija del icono, con su esquina
    /// superior izquierda en ese punto del icono, y se oculta sin nivel o sin sprite.
    /// </summary>
    public static class LevelBadge
    {
        /// <param name="topLeft">Píxeles desde la esquina superior izquierda del icono: (8, 12) en los de 16 y (18, 26) en los de 32.</param>
        public static Image Create(Image icon, Vector2 topLeft)
        {
            var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            badge.transform.SetParent(icon.transform, false);
            badge.raycastTarget = false;
            badge.enabled = false;
            var rect = badge.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            // Fuera de cualquier distribución automática del icono.
            badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return badge;
        }

        /// <summary>
        /// Gema dorada de mutación especial en la esquina de arriba a la derecha del icono (sobresale un poco). Se crea la
        /// primera vez como hija del icono; sin sprite o si no es especial, se oculta.
        /// </summary>
        public static void ShowSpecial(Image icon, Sprite sprite, bool special)
        {
            if (icon == null)
                return;

            var existing = icon.transform.Find("SpecialBadge");
            Image badge;
            if (existing != null)
                badge = existing.GetComponent<Image>();
            else
            {
                if (!special || sprite == null)
                    return;
                badge = Create(icon, Vector2.zero);
                badge.name = "SpecialBadge";
                var rect = badge.rectTransform;
                // Esquina de arriba a la derecha: sobresale 2 px hacia fuera.
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(2f, 2f);
            }

            badge.sprite = sprite;
            badge.enabled = special && sprite != null && icon.enabled;
            if (sprite != null)
                badge.rectTransform.sizeDelta = sprite.rect.size;
        }

        /// <summary>Pone el sprite de ese nivel (1 a 3) de la lista; nivel 0 o sin sprite, la oculta.</summary>
        public static void Show(Image badge, Sprite[] sprites, int level)
        {
            if (badge == null)
                return;

            var sprite = sprites != null && level >= 1 && level <= sprites.Length ? sprites[level - 1] : null;
            badge.sprite = sprite;
            badge.enabled = sprite != null;
            if (sprite != null)
                badge.rectTransform.sizeDelta = sprite.rect.size;
        }
    }
}

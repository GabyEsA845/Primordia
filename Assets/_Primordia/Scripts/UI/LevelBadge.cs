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

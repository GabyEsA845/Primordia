using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Muestra la vida del jugador en corazones: cada corazón son 2 puntos de vida.
    /// Crea sus propios iconos como hijos; conviene ponerlo en un objeto con HorizontalLayoutGroup.
    /// </summary>
    public class HeartsDisplay : MonoBehaviour
    {
        public enum Fill
        {
            Empty,
            Half,
            Full
        }

        [Tooltip("Vida a mostrar. Si está vacío, usa la del objeto con la etiqueta Player.")]
        [SerializeField] Health health;
        [SerializeField] Sprite fullHeart;
        [SerializeField] Sprite halfHeart;
        [SerializeField] Sprite emptyHeart;
        [Tooltip("Tamaño de cada corazón en píxeles de la interfaz.")]
        [SerializeField] Vector2 heartSize = new(16f, 16f);

        readonly List<Image> hearts = new();

        public IReadOnlyList<Image> Hearts => hearts;

        void Start()
        {
            if (health == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    health = player.GetComponent<Health>();
            }

            if (health == null)
                return;

            health.Changed += OnHealthChanged;
            Refresh();
        }

        void OnDestroy()
        {
            if (health != null)
                health.Changed -= OnHealthChanged;
        }

        internal void Configure(Health newHealth, Sprite full, Sprite half, Sprite empty)
        {
            health = newHealth;
            fullHeart = full;
            halfHeart = half;
            emptyHeart = empty;
        }

        /// <summary>Cómo se ve el corazón número <paramref name="index"/> (desde 0) con esa vida.</summary>
        public static Fill HeartFill(int currentHealth, int index)
        {
            int remaining = currentHealth - index * 2;
            return remaining >= 2 ? Fill.Full : remaining == 1 ? Fill.Half : Fill.Empty;
        }

        void OnHealthChanged(int current, int max) => Refresh();

        void Refresh()
        {
            int count = Mathf.CeilToInt(health.Max / 2f);

            while (hearts.Count < count)
                hearts.Add(CreateHeart());

            for (int i = 0; i < hearts.Count; i++)
            {
                bool visible = i < count;
                hearts[i].gameObject.SetActive(visible);
                if (visible)
                    hearts[i].sprite = SpriteFor(HeartFill(health.Current, i));
            }
        }

        Image CreateHeart()
        {
            var heart = new GameObject("Heart", typeof(RectTransform), typeof(Image));
            heart.transform.SetParent(transform, false);
            ((RectTransform)heart.transform).sizeDelta = heartSize;
            return heart.GetComponent<Image>();
        }

        Sprite SpriteFor(Fill fill) => fill switch
        {
            Fill.Full => fullHeart,
            Fill.Half => halfHeart,
            _ => emptyHeart
        };
    }
}

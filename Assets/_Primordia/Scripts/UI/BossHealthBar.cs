using Primordia.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Barra de vida del jefe, arriba en el centro. Aparece mientras hay un jefe activo y cambia según su estado
    /// (<see cref="IBoss.BarStyle"/>): otro color en la fase agresiva, gris con candado mientras es inmune, y roja,
    /// palpitando y temblando cuando está agotado.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Relleno de la barra: se estira de izquierda a derecha cambiando su ancla derecha.")]
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;
        [SerializeField] Text title;
        [Tooltip("Candado que se ve mientras el jefe es inmune. Sin sprite no se muestra.")]
        [SerializeField] Image lockIcon;
        [SerializeField] Color phase1Color = new(0.85f, 0.25f, 0.45f);
        [SerializeField] Color phase2Color = new(1f, 0.55f, 0.15f);
        [SerializeField] Color immuneColor = new(0.55f, 0.55f, 0.6f);
        [SerializeField] Color weakColor = new(0.9f, 0.1f, 0.12f);
        [Tooltip("Latidos por segundo de la barra cuando el jefe está agotado.")]
        [SerializeField, Min(0f)] float weakPulseRate = 2.5f;
        [Tooltip("Cuánto crece la barra en cada latido (0,08 = un 8 %).")]
        [SerializeField, Min(0f)] float weakPulseScale = 0.08f;
        [Tooltip("Píxeles que tiembla la barra cuando el jefe está agotado.")]
        [SerializeField, Min(0f)] float weakShake = 1f;

        RectTransform rootRect;
        Vector2 rootPosition;

        public BossBarStyle Style { get; private set; }

        /// <summary>Objeto de la barra (se queda visible durante las escenas de los jefes).</summary>
        public GameObject Root => root;

        void Awake()
        {
            root.SetActive(false);
            rootRect = root.transform as RectTransform;
            if (rootRect != null)
                rootPosition = rootRect.anchoredPosition;
        }

        void LateUpdate()
        {
            var boss = ActiveBoss.Current;
            // Un jefe destruido sigue pareciendo no nulo a través de la interfaz.
            bool alive = boss is Object unityObject && unityObject != null && boss.Health != null && !boss.Health.IsDead;
            if (root.activeSelf != alive)
                root.SetActive(alive);
            if (!alive)
                return;

            float ratio = (float)boss.Health.Current / boss.Health.Max;
            fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
            title.text = boss.DisplayName;
            ApplyStyle(boss.BarStyle);
        }

        void ApplyStyle(BossBarStyle style)
        {
            Style = style;
            fillImage.color = style switch
            {
                BossBarStyle.Enraged => phase2Color,
                BossBarStyle.Immune => immuneColor,
                BossBarStyle.Weak => weakColor,
                _ => phase1Color
            };

            if (lockIcon != null)
                lockIcon.enabled = style == BossBarStyle.Immune && lockIcon.sprite != null;

            if (rootRect == null)
                return;

            if (style == BossBarStyle.Weak)
            {
                // Latido: crece de golpe y vuelve despacio, como un pulso débil.
                float beat = Mathf.Repeat(Time.unscaledTime * weakPulseRate, 1f);
                float pulse = Mathf.Pow(1f - beat, 3f);
                rootRect.localScale = Vector3.one * (1f + weakPulseScale * pulse);
                var shake = new Vector2(Random.Range(-1, 2), Random.Range(-1, 2)) * weakShake * pulse;
                rootRect.anchoredPosition = rootPosition + new Vector2(Mathf.Round(shake.x), Mathf.Round(shake.y));
            }
            else
            {
                rootRect.localScale = Vector3.one;
                rootRect.anchoredPosition = rootPosition;
            }
        }

        internal void Configure(GameObject newRoot, RectTransform newFill, Image newFillImage, Text newTitle)
        {
            root = newRoot;
            fill = newFill;
            fillImage = newFillImage;
            title = newTitle;
        }

        internal void ConfigureLock(Image newLockIcon) => lockIcon = newLockIcon;
    }
}

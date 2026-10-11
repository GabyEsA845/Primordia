using Primordia.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Barra de vida del jefe, arriba en el centro. Aparece mientras hay un jefe activo
    /// y cambia de color en la fase 2.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Relleno de la barra: se estira de izquierda a derecha cambiando su ancla derecha.")]
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;
        [SerializeField] Text title;
        [SerializeField] Color phase1Color = new(0.85f, 0.25f, 0.45f);
        [SerializeField] Color phase2Color = new(1f, 0.55f, 0.15f);

        void Awake() => root.SetActive(false);

        void LateUpdate()
        {
            var boss = BossAI.Current;
            bool visible = boss != null && !boss.Health.IsDead;
            if (root.activeSelf != visible)
                root.SetActive(visible);
            if (!visible)
                return;

            float ratio = (float)boss.Health.Current / boss.Health.Max;
            fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
            fillImage.color = boss.Phase == 2 ? phase2Color : phase1Color;
            title.text = boss.DisplayName;
        }

        internal void Configure(GameObject newRoot, RectTransform newFill, Image newFillImage, Text newTitle)
        {
            root = newRoot;
            fill = newFill;
            fillImage = newFillImage;
            title = newTitle;
        }
    }
}

using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Durante una escena (<see cref="GameState.Cinematic"/>) bajan dos franjas negras de cine, arriba y abajo, y se
    /// desvanece el HUD del jugador: todo lo que cuelga del lienzo salvo la barra del jefe. Al acabar, todo vuelve.
    /// Las franjas las crea el propio componente, detrás de lo demás.
    /// </summary>
    public class CinematicOverlay : MonoBehaviour
    {
        [Tooltip("Alto de cada franja, en píxeles de la interfaz (270 de alto en total).")]
        [SerializeField, Min(0f)] float barHeight = 28f;
        [Tooltip("Segundos que tardan en entrar y salir las franjas y el HUD.")]
        [SerializeField, Min(0.01f)] float fadeTime = 0.4f;
        [SerializeField] Color barColor = Color.black;
        [Tooltip("Aviso de que la escena se puede saltar, abajo a la derecha sobre la franja.")]
        [SerializeField] string skipHintText = "E: saltar escena";

        RectTransform topBar;
        RectTransform bottomBar;
        Text skipHint;
        GameManager manager;
        // Transparencia que tenía cada elemento oculto: algunos llevan la suya (el cartel del linaje está a 0 cuando no se ve).
        readonly Dictionary<CanvasGroup, float> hidden = new();
        readonly List<GameObject> keep = new();
        float amount;

        /// <summary>0 = juego normal, 1 = escena del todo (franjas dentro, HUD oculto).</summary>
        public float Amount => amount;

        public bool Showing { get; private set; }

        /// <summary>True si se ve el aviso de saltar la escena.</summary>
        public bool SkipHintShown => skipHint != null && skipHint.gameObject.activeSelf;

        /// <summary>Enseña u oculta el aviso de que la escena se puede saltar (la metamorfosis, desde la segunda vez).</summary>
        public void SetSkipHint(bool show)
        {
            if (show && skipHint == null)
                skipHint = CreateSkipHint();
            if (skipHint != null)
                skipHint.gameObject.SetActive(show);
        }

        void Awake()
        {
            topBar = CreateBar("CinematicBarTop", 1f);
            bottomBar = CreateBar("CinematicBarBottom", 0f);
            Apply(0f);
        }

        void Start()
        {
            var bossBar = GetComponentInChildren<BossHealthBar>(true);
            if (bossBar != null && bossBar.Root != null)
                keep.Add(bossBar.Root);

            manager = GameManager.Instance;
            if (manager == null)
                return;
            manager.StateChanged += OnStateChanged;
            if (manager.State == GameState.Cinematic)
                Show(true);
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(GameState previous, GameState current) => Show(current == GameState.Cinematic);

        void Show(bool show)
        {
            if (show == Showing)
                return;
            Showing = show;

            if (!show)
                return;

            foreach (Transform child in transform)
            {
                if (child == topBar || child == bottomBar || (skipHint != null && child == skipHint.transform)
                    || !child.gameObject.activeSelf || keep.Contains(child.gameObject))
                    continue;
                if (!child.TryGetComponent<CanvasGroup>(out var group))
                    group = child.gameObject.AddComponent<CanvasGroup>();
                // Si la escena anterior aún se estaba desvaneciendo, se conserva la transparencia de antes de ella.
                if (!hidden.ContainsKey(group))
                    hidden[group] = group.alpha;
            }
        }

        void Update()
        {
            float target = Showing ? 1f : 0f;
            if (Mathf.Approximately(amount, target))
                return;
            amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime / fadeTime);
            Apply(amount);
            if (!Showing && amount <= 0f)
                hidden.Clear();
        }

        void Apply(float value)
        {
            float eased = Mathf.SmoothStep(0f, 1f, value);
            topBar.sizeDelta = new Vector2(0f, barHeight * eased);
            bottomBar.sizeDelta = new Vector2(0f, barHeight * eased);
            foreach (var (group, alpha) in hidden)
            {
                if (group != null)
                    group.alpha = alpha * (1f - eased);
            }
        }

        Text CreateSkipHint()
        {
            var text = new GameObject("SkipHint", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var rect = text.rectTransform;
            rect.SetParent(transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-8f, 7f);
            rect.sizeDelta = new Vector2(160f, 14f);
            // La misma fuente pixel que el resto de la interfaz.
            var sample = GetComponentInChildren<Text>(true);
            if (sample != null && sample != text)
                text.font = sample.font;
            text.fontSize = 9;
            text.alignment = TextAnchor.LowerRight;
            text.color = new Color(1f, 1f, 1f, 0.85f);
            text.raycastTarget = false;
            text.text = skipHintText;
            return text;
        }

        RectTransform CreateBar(string barName, float anchorY)
        {
            var bar = new GameObject(barName, typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(transform, false);
            // Detrás de todo lo demás: la barra del jefe se ve encima.
            bar.SetAsFirstSibling();
            bar.anchorMin = new Vector2(0f, anchorY);
            bar.anchorMax = new Vector2(1f, anchorY);
            bar.pivot = new Vector2(0.5f, anchorY);
            bar.anchoredPosition = Vector2.zero;
            var image = bar.gameObject.AddComponent<Image>();
            image.color = barColor;
            image.raycastTarget = false;
            return bar;
        }
    }
}

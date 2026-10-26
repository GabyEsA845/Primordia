using System.Collections.Generic;
using Primordia.Core;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Cartel "elige tu linaje": cuando una mutación especial (comodín) completa dos linajes a la vez, pausa el juego y
    /// enseña los dos, cada uno con su clase y su beneficio. No se puede saltar. Se abre un cuadro después de que lo pida
    /// <see cref="PlayerLineage.ChoiceNeeded"/>, para no pisarse con la elección de mutación que lo provocó al cerrarse.
    /// </summary>
    public class LineageChoiceUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button[] buttons = System.Array.Empty<Button>();
        [SerializeField] Text[] names = System.Array.Empty<Text>();
        [SerializeField] Text[] benefits = System.Array.Empty<Text>();
        [Tooltip("Colores por clase, en el orden de LineageClass (los del rótulo del HUD).")]
        [SerializeField] Color[] classColors =
        {
            new(0.95f, 0.45f, 0.4f), new(0.55f, 0.9f, 0.45f), new(0.8f, 0.6f, 0.95f), new(0.95f, 0.75f, 0.4f), new(0.78f, 0.64f, 0.42f),
            new(0.78f, 0.64f, 0.42f),
        };
        [Tooltip("Segundos (reales) en que los botones no aceptan clics al abrirse, para no elegir por error.")]
        [SerializeField, Min(0f)] float inputLockDuration = 0.5f;

        readonly List<LineageData> options = new();
        PlayerLineage lineage;
        bool pending;
        float unlockTime;

        public bool IsOpen => panel != null && panel.activeSelf;
        public IReadOnlyList<LineageData> Options => options;

        void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                if (buttons[i] != null)
                    buttons[i].onClick.AddListener(() => Choose(index));
            }
            if (panel != null)
                panel.SetActive(false);
        }

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                Bind(player.GetComponent<PlayerLineage>());
        }

        void OnDestroy()
        {
            if (lineage != null)
                lineage.ChoiceNeeded -= OnChoiceNeeded;
        }

        internal void Bind(PlayerLineage target)
        {
            if (lineage != null)
                lineage.ChoiceNeeded -= OnChoiceNeeded;
            lineage = target;
            if (lineage == null)
                return;
            lineage.ChoiceNeeded += OnChoiceNeeded;
            pending = lineage.AwaitingChoice;
        }

        void OnChoiceNeeded(IReadOnlyList<LineageData> candidates) => pending = true;

        void Update()
        {
            if (!pending || IsOpen || lineage == null)
                return;
            // Espera a volver al juego (la elección de mutación que lo provocó se cierra en ese mismo cuadro).
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                return;
            pending = false;
            if (lineage.AwaitingChoice)
                Open();
        }

        void Open()
        {
            options.Clear();
            options.AddRange(lineage.Candidates);
            for (int i = 0; i < buttons.Length; i++)
            {
                bool used = i < options.Count;
                if (buttons[i] != null)
                    buttons[i].gameObject.SetActive(used);
                if (!used)
                    continue;
                var option = options[i];
                if (i < names.Length && names[i] != null)
                {
                    names[i].text = $"{option.DisplayName} · {MutationText.ClassName(option.Class)}";
                    names[i].color = MutationText.ClassColor(option.Class, classColors);
                }
                if (i < benefits.Length && benefits[i] != null)
                    benefits[i].text = LineagePanel.Describe(option);
            }

            panel.SetActive(true);
            unlockTime = Time.unscaledTime + inputLockDuration;
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MutationChoice);
        }

        public void Choose(int index)
        {
            if (!IsOpen || Time.unscaledTime < unlockTime || index < 0 || index >= options.Count)
                return;

            var chosen = options[index];
            panel.SetActive(false);
            options.Clear();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.MutationChoice)
                GameManager.Instance.SetState(GameState.Playing);
            // Después de volver al juego: la evolución del linaje espera a que se juegue.
            lineage.Choose(chosen);
        }

        internal void Configure(GameObject newPanel, Button[] newButtons, Text[] newNames, Text[] newBenefits)
        {
            panel = newPanel;
            buttons = newButtons ?? System.Array.Empty<Button>();
            names = newNames ?? System.Array.Empty<Text>();
            benefits = newBenefits ?? System.Array.Empty<Text>();
        }
    }
}

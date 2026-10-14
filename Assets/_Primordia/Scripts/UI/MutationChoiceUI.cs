using System.Collections.Generic;
using Primordia.Core;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Pantalla "elige 1 de 3": pausa el juego, muestra una tarjeta por mutación ofrecida
    /// (con su ranura, linaje y lo que reemplazaría) y un botón Saltar.
    /// </summary>
    public class MutationChoiceUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] MutationCard[] cards;
        [SerializeField] Button skipButton;
        [Tooltip("Segundos (reales: el juego está en pausa) en que los botones no aceptan clics al abrirse, " +
                 "para no elegir por error si se seguía disparando.")]
        [SerializeField, Min(0f)] float inputLockDuration = 0.5f;

        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>True mientras los botones aún no aceptan clics.</summary>
        public bool IsLocked => IsOpen && Time.unscaledTime < unlockTime;

        readonly List<MutationData> options = new();
        PlayerMutations player;
        float unlockTime;
        bool buttonsEnabled = true;

        void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].Button.onClick.AddListener(() => Choose(index));
            }

            if (skipButton != null)
                skipButton.onClick.AddListener(Skip);

            panel.SetActive(false);
        }

        public void Show(IReadOnlyList<MutationData> offered, PlayerMutations target)
        {
            player = target;
            options.Clear();
            options.AddRange(offered);

            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < options.Count;
                cards[i].gameObject.SetActive(used);
                if (used)
                    cards[i].Show(options[i], player.Get(options[i].Slot), player.LevelOf(options[i].Slot),
                        LineageData.Advances(options[i], player));
            }

            panel.SetActive(true);
            unlockTime = Time.unscaledTime + inputLockDuration;
            SetButtonsEnabled(inputLockDuration <= 0f);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MutationChoice);
        }

        void Update()
        {
            // Los botones desactivados se ven atenuados; se activan al acabar el bloqueo.
            if (IsOpen && !buttonsEnabled && !IsLocked)
                SetButtonsEnabled(true);
        }

        public void Choose(int index)
        {
            if (!IsOpen || IsLocked || index < 0 || index >= options.Count)
                return;

            player.Equip(options[index]);
            Close();
        }

        public void Skip()
        {
            if (IsOpen && !IsLocked)
                Close();
        }

        void SetButtonsEnabled(bool enabled)
        {
            buttonsEnabled = enabled;
            foreach (var card in cards)
                card.Button.interactable = enabled;
            if (skipButton != null)
                skipButton.interactable = enabled;
        }

        void Close()
        {
            panel.SetActive(false);
            options.Clear();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.MutationChoice)
                GameManager.Instance.SetState(GameState.Playing);
        }

        internal void Configure(GameObject newPanel, MutationCard[] newCards, Button newSkipButton, float newInputLockDuration = 0.5f)
        {
            panel = newPanel;
            cards = newCards;
            skipButton = newSkipButton;
            inputLockDuration = newInputLockDuration;
        }
    }
}

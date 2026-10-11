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

        public bool IsOpen => panel != null && panel.activeSelf;

        readonly List<MutationData> options = new();
        PlayerMutations player;

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
                    cards[i].Show(options[i], player.Get(options[i].Slot));
            }

            panel.SetActive(true);
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MutationChoice);
        }

        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= options.Count)
                return;

            player.Equip(options[index]);
            Close();
        }

        public void Skip()
        {
            if (IsOpen)
                Close();
        }

        void Close()
        {
            panel.SetActive(false);
            options.Clear();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.MutationChoice)
                GameManager.Instance.SetState(GameState.Playing);
        }

        internal void Configure(GameObject newPanel, MutationCard[] newCards, Button newSkipButton)
        {
            panel = newPanel;
            cards = newCards;
            skipButton = newSkipButton;
        }
    }
}

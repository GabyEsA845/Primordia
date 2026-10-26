using System;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Ventana del laboratorio de Darwin (la cápsula de la sala de cambio). A la izquierda, la criatura con sus ranuras,
    /// como en la pausa: se elige la ranura cuya mutación se entrega (las especiales no se pueden). A la derecha, las
    /// mutaciones del piso de esa ranura por las que se puede cambiar, al nivel con el que entrarían. Aceptar hace el
    /// cambio; Salir (o Esc) cierra sin cambiar. Mientras está abierta el juego espera.
    /// </summary>
    public class ExchangeUI : MonoBehaviour
    {
        static readonly MutationSlot[] SlotOrder = { MutationSlot.Mouth, MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ };

        [SerializeField] GameObject panel;
        [SerializeField] CreaturePanel creature;
        [Tooltip("Botón de cada ranura, en el orden Boca, Extremidades, Piel, Órgano.")]
        [SerializeField] Button[] slotButtons = Array.Empty<Button>();
        [Tooltip("Marca de la ranura elegida, en el mismo orden.")]
        [SerializeField] Image[] slotMarks = Array.Empty<Image>();
        [SerializeField] Text heading;
        [SerializeField] Text note;
        [SerializeField] MutationCard[] cards = Array.Empty<MutationCard>();
        [SerializeField] Button acceptButton;
        [SerializeField] Button exitButton;
        [SerializeField, Min(0f)] float inputLockDuration = 0.3f;
        [Tooltip("Escala de la tarjeta elegida.")]
        [SerializeField, Min(1f)] float chosenScale = 1.06f;

        PlayerMutations player;
        MutationPool pool;
        int floor;
        Action<MutationData> onAccept;
        Action onClosed;
        float unlockTime;
        readonly List<MutationData> options = new();

        public bool IsOpen => panel != null && panel.activeSelf;
        public MutationSlot? SelectedSlot { get; private set; }
        public MutationData Selected { get; private set; }
        public IReadOnlyList<MutationData> Options => options;
        public bool CanAccept => IsOpen && Selected != null;

        void Awake()
        {
            for (int i = 0; i < slotButtons.Length && i < SlotOrder.Length; i++)
            {
                var slot = SlotOrder[i];
                if (slotButtons[i] != null)
                    slotButtons[i].onClick.AddListener(() => SelectSlot(slot));
            }
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                if (cards[i] != null)
                    cards[i].Button.onClick.AddListener(() => Choose(index));
            }
            if (acceptButton != null)
                acceptButton.onClick.AddListener(Accept);
            if (exitButton != null)
                exitButton.onClick.AddListener(Close);
            if (panel != null)
                panel.SetActive(false);
        }

        internal void Configure(GameObject newPanel, CreaturePanel newCreature, Button[] newSlotButtons, Image[] newSlotMarks,
            Text newHeading, Text newNote, MutationCard[] newCards, Button newAccept, Button newExit)
        {
            panel = newPanel;
            creature = newCreature;
            slotButtons = newSlotButtons ?? Array.Empty<Button>();
            slotMarks = newSlotMarks ?? Array.Empty<Image>();
            heading = newHeading;
            note = newNote;
            cards = newCards ?? Array.Empty<MutationCard>();
            acceptButton = newAccept;
            exitButton = newExit;
        }

        internal void SetInputLock(float seconds) => inputLockDuration = Mathf.Max(0f, seconds);

        /// <param name="accepted">Se llama al aceptar, con la mutación elegida (la ventana ya se ha cerrado).</param>
        /// <param name="closed">Se llama al cerrar sin cambiar.</param>
        public void Show(PlayerMutations target, MutationPool offers, int floorNumber, Action<MutationData> accepted, Action closed)
        {
            player = target;
            pool = offers;
            floor = floorNumber;
            onAccept = accepted;
            onClosed = closed;
            unlockTime = Time.unscaledTime + inputLockDuration;

            panel.SetActive(true);
            if (creature != null)
                creature.Refresh(player);

            // Empieza en la primera ranura que se pueda entregar (normalmente la Boca).
            SelectedSlot = null;
            foreach (var slot in SlotOrder)
            {
                if (MutationExchange.CanGive(player, slot))
                {
                    SelectSlot(slot, force: true);
                    break;
                }
            }
            if (SelectedSlot == null)
                ShowSlot(null);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MutationChoice);
        }

        void Update()
        {
            if (!IsOpen)
                return;
            if (acceptButton != null)
                acceptButton.interactable = CanAccept && Time.unscaledTime >= unlockTime;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        /// <summary>Elige la ranura cuya mutación se entrega y enseña por cuáles se puede cambiar.</summary>
        public void SelectSlot(MutationSlot slot) => SelectSlot(slot, force: false);

        void SelectSlot(MutationSlot slot, bool force)
        {
            if (!IsOpen || (!force && SelectedSlot == slot))
                return;
            if (!force)
                Sfx.Play(Sounds.UIClick);
            SelectedSlot = slot;
            ShowSlot(slot);
        }

        void ShowSlot(MutationSlot? slot)
        {
            Selected = null;
            options.Clear();
            for (int i = 0; i < slotMarks.Length && i < SlotOrder.Length; i++)
            {
                if (slotMarks[i] != null)
                    slotMarks[i].enabled = slot == SlotOrder[i];
            }

            var given = slot is MutationSlot s ? player.Get(s) : null;
            if (slot is MutationSlot chosen)
                options.AddRange(MutationExchange.Options(pool, player, chosen, floor));

            if (heading != null)
                heading.text = given != null ? $"Entregas: {given.DisplayName} {MutationText.LevelName(player.LevelOf(given.Slot))}" : "Elige una ranura";
            if (note != null)
                note.text = NoteFor(slot, given);

            int level = slot is MutationSlot from ? MutationExchange.LevelFor(player, from, floor) : 1;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                    continue;
                bool shown = i < options.Count;
                cards[i].gameObject.SetActive(shown);
                cards[i].transform.localScale = Vector3.one;
                if (shown)
                    cards[i].ShowExchange(options[i], level, CardNote(options[i], given));
            }
        }

        string NoteFor(MutationSlot? slot, MutationData given)
        {
            if (slot == null)
                return "No llevas nada que se pueda cambiar.";
            if (given == null)
                return "Esa ranura está vacía: no hay nada que entregar.";
            if (given.IsSpecial)
                return "Las mutaciones especiales de las almejas no se cambian.";
            if (options.Count == 0)
                return "Aún no hay otras mutaciones de esta ranura en este piso.";
            if (given.Floor < floor)
                return "Viene de un piso anterior: la nueva entra un nivel por debajo.";
            return "La nueva entra con el mismo nivel. Solo un cambio por piso: piénsalo bien.";
        }

        string CardNote(MutationData option, MutationData given)
        {
            var text = given != null ? $"Por: {given.DisplayName}" : "";
            if (MutationExchange.ChangesBody(player, option))
                text += " · cambia tu cuerpo";
            return text;
        }

        public void Choose(int index)
        {
            if (!IsOpen || Time.unscaledTime < unlockTime || index < 0 || index >= options.Count)
                return;
            Selected = options[index];
            Sfx.Play(Sounds.UICardSelect);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                    cards[i].transform.localScale = Vector3.one * (i == index ? chosenScale : 1f);
            }
        }

        /// <summary>Hace el cambio elegido: cierra la ventana y avisa a la cápsula.</summary>
        public void Accept()
        {
            if (!CanAccept || Time.unscaledTime < unlockTime)
                return;
            var chosen = Selected;
            var callback = onAccept;
            Hide();
            callback?.Invoke(chosen);
        }

        /// <summary>Cierra sin cambiar nada.</summary>
        public void Close()
        {
            if (!IsOpen)
                return;
            Sfx.Play(Sounds.UIBack);
            var callback = onClosed;
            Hide();
            callback?.Invoke();
        }

        void Hide()
        {
            panel.SetActive(false);
            onAccept = null;
            onClosed = null;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.MutationChoice)
                GameManager.Instance.SetState(GameState.Playing);
        }
    }
}

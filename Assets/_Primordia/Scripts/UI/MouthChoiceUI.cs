using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Ventana de la sala de paso: con el jugador congelado en el aire sobre la fosa, hay que elegir una de las Bocas del
    /// piso siguiente (no se puede cerrar ni saltar). Al elegir, la otra carta se desvanece y una fila con las ranuras
    /// enseña el reparto de los niveles extra de la Boca anterior: cada "+1" vuela a la mutación que lo recibe (que sube
    /// con un destello) o se deshace con "Perdido". Todo en tiempo real: el juego está detenido.
    /// </summary>
    public class MouthChoiceUI : MonoBehaviour
    {
        static readonly MutationSlot[] SlotOrder = { MutationSlot.Mouth, MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ };

        [SerializeField] GameObject panel;
        [SerializeField] Text subtitle;
        [SerializeField] Text note;
        [SerializeField] MouthCard[] cards = Array.Empty<MouthCard>();

        [Header("Reparto")]
        [Tooltip("Fila de las ranuras, oculta hasta elegir.")]
        [SerializeField] RectTransform slotsRow;
        [Tooltip("Icono de cada ranura, en el orden Boca, Extremidades, Piel, Órgano.")]
        [SerializeField] Image[] slotIcons = new Image[4];
        [Tooltip("Insignias pequeñas de nivel, de 1 a 3.")]
        [SerializeField] Sprite[] levelBadges = new Sprite[3];
        [SerializeField] Color transferColor = new(1f, 0.9f, 0.45f);
        [SerializeField] Color lostColor = new(1f, 0.45f, 0.45f);

        [Header("Ritmo (segundos reales)")]
        [Tooltip("Los botones no aceptan clics al abrirse, para no elegir por error si se seguía disparando.")]
        [SerializeField, Min(0f)] float inputLockDuration = 0.6f;
        [SerializeField, Min(0f)] float fadeDuration = 0.35f;
        [SerializeField, Min(0.05f)] float flightDuration = 0.45f;
        [SerializeField, Min(0f)] float transferInterval = 0.55f;
        [SerializeField, Min(0f)] float endPause = 0.9f;

        readonly List<MutationData> options = new();
        readonly Image[] badges = new Image[4];
        PlayerMutations player;
        Action onFinished;
        float unlockTime;
        bool buttonsEnabled = true;

        public bool IsOpen => panel != null && panel.activeSelf;
        public bool IsLocked => IsOpen && Time.unscaledTime < unlockTime;

        /// <summary>True desde que se elige hasta que acaba la animación del reparto.</summary>
        public bool IsDistributing { get; private set; }

        /// <summary>La Boca elegida la última vez (null hasta elegir).</summary>
        public MutationData Chosen { get; private set; }

        /// <summary>Reparto de la última elección, en orden.</summary>
        public IReadOnlyList<MouthChange.Transfer> LastPlan { get; private set; } = Array.Empty<MouthChange.Transfer>();

        void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].Button.onClick.AddListener(() => Choose(index));
            }
            if (panel != null)
                panel.SetActive(false);
        }

        /// <param name="nextFloorName">Para el subtítulo: "Así llegarás al …".</param>
        /// <param name="finished">Se llama al acabar el reparto, con la Boca ya equipada.</param>
        public void Show(IReadOnlyList<MutationData> mouths, PlayerMutations target, string nextFloorName, Action finished)
        {
            player = target;
            onFinished = finished;
            Chosen = null;
            LastPlan = Array.Empty<MouthChange.Transfer>();
            options.Clear();
            options.AddRange(mouths);

            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < options.Count;
                cards[i].gameObject.SetActive(used);
                SetAlpha(cards[i], 1f);
                if (used)
                    cards[i].Show(options[i]);
            }

            if (subtitle != null)
                subtitle.text = string.IsNullOrEmpty(nextFloorName) ? "" : $"Así llegarás al {nextFloorName}";
            if (note != null)
            {
                note.text = MutationText.MouthChangeNote(player);
                note.gameObject.SetActive(!string.IsNullOrEmpty(note.text));
            }
            if (slotsRow != null)
                slotsRow.gameObject.SetActive(false);

            panel.SetActive(true);
            IsDistributing = false;
            unlockTime = Time.unscaledTime + inputLockDuration;
            SetButtonsEnabled(inputLockDuration <= 0f);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MutationChoice);
        }

        void Update()
        {
            if (IsOpen && !IsDistributing && !buttonsEnabled && !IsLocked)
                SetButtonsEnabled(true);
        }

        public void Choose(int index)
        {
            if (!IsOpen || IsLocked || IsDistributing || index < 0 || index >= options.Count)
                return;

            Chosen = options[index];
            IsDistributing = true;
            SetButtonsEnabled(false);

            // Lo que había antes del cambio, para enseñarlo y subirlo poco a poco.
            var before = new Dictionary<MutationSlot, (MutationData mutation, int level)>();
            foreach (var slot in SlotOrder)
                before[slot] = (player.Get(slot), player.LevelOf(slot));

            var plan = MouthChange.Plan(player, new System.Random(UnityEngine.Random.Range(1, int.MaxValue)));
            LastPlan = plan;
            MouthChange.Apply(player, Chosen, plan);

            StartCoroutine(Distribute(index, before, plan));
        }

        IEnumerator Distribute(int chosenIndex, Dictionary<MutationSlot, (MutationData mutation, int level)> before,
            List<MouthChange.Transfer> plan)
        {
            // La carta elegida se queda; las demás se desvanecen y se apartan.
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i != chosenIndex && cards[i].gameObject.activeSelf)
                        SetAlpha(cards[i], 1f - t / fadeDuration);
                }
                yield return null;
            }
            for (int i = 0; i < cards.Length; i++)
            {
                if (i != chosenIndex)
                    cards[i].gameObject.SetActive(false);
            }
            if (note != null)
                note.gameObject.SetActive(false);

            if (slotsRow != null)
            {
                slotsRow.gameObject.SetActive(true);
                foreach (var slot in SlotOrder)
                    ShowSlot(slot, before[slot].mutation, before[slot].level);
                yield return Wait(0.35f);

                // La Boca vieja deja su sitio a la nueva, en el nivel I.
                ShowSlot(MutationSlot.Mouth, Chosen, 1);
                yield return Pulse(MutationSlot.Mouth);

                var levels = new Dictionary<MutationSlot, int>();
                foreach (var slot in SlotOrder)
                    levels[slot] = before[slot].level;

                foreach (var transfer in plan)
                {
                    yield return Wait(transferInterval - flightDuration);
                    if (transfer.Slot is MutationSlot target)
                    {
                        yield return Fly(MutationSlot.Mouth, target);
                        levels[target]++;
                        ShowSlot(target, before[target].mutation, levels[target]);
                        yield return Pulse(target);
                    }
                    else
                    {
                        yield return Lose();
                    }
                }
            }

            yield return Wait(endPause);
            Finish();
        }

        void Finish()
        {
            IsDistributing = false;
            panel.SetActive(false);
            options.Clear();
            var finished = onFinished;
            onFinished = null;
            finished?.Invoke();
        }

        void ShowSlot(MutationSlot slot, MutationData mutation, int level)
        {
            var icon = IconOf(slot);
            if (icon == null)
                return;
            icon.sprite = mutation != null ? mutation.Icon : null;
            icon.enabled = icon.sprite != null;
            int index = Array.IndexOf(SlotOrder, slot);
            if (badges[index] == null)
                badges[index] = LevelBadge.Create(icon, new Vector2(8f, 12f));
            LevelBadge.Show(badges[index], levelBadges, mutation != null ? level : 0);
        }

        Image IconOf(MutationSlot slot)
        {
            int index = Array.IndexOf(SlotOrder, slot);
            return slotIcons != null && index < slotIcons.Length ? slotIcons[index] : null;
        }

        /// <summary>El icono crece y vuelve.</summary>
        IEnumerator Pulse(MutationSlot slot)
        {
            var icon = IconOf(slot);
            if (icon == null)
                yield break;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                icon.transform.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(t / 0.3f * Mathf.PI));
                yield return null;
            }
            icon.transform.localScale = Vector3.one;
        }

        /// <summary>Un "+1" vuela de una ranura a otra.</summary>
        IEnumerator Fly(MutationSlot from, MutationSlot to)
        {
            var start = IconOf(from);
            var end = IconOf(to);
            if (start == null || end == null)
                yield break;

            var label = CreateLabel("+1", transferColor);
            Vector3 a = start.rectTransform.position, b = end.rectTransform.position;
            for (float t = 0f; t < flightDuration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / flightDuration);
                // Un pequeño arco por encima de la fila.
                label.rectTransform.position = Vector3.Lerp(a, b, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 18f * Scale());
                yield return null;
            }
            Destroy(label.gameObject);
        }

        /// <summary>Un "+1" que no cabe en ninguna mutación sube, se vuelve "Perdido" y se desvanece.</summary>
        IEnumerator Lose()
        {
            var start = IconOf(MutationSlot.Mouth);
            if (start == null)
                yield break;

            var label = CreateLabel("+1", transferColor);
            Vector3 a = start.rectTransform.position;
            for (float t = 0f; t < 0.7f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.7f;
                if (k > 0.35f && label.text != "Perdido")
                {
                    label.text = "Perdido";
                    label.color = lostColor;
                }
                var color = label.color;
                color.a = 1f - Mathf.Max(0f, k - 0.5f) * 2f;
                label.color = color;
                label.rectTransform.position = a + Vector3.up * (k * 22f * Scale());
                yield return null;
            }
            Destroy(label.gameObject);
        }

        float Scale() => slotsRow != null ? slotsRow.lossyScale.y : 1f;

        Text CreateLabel(string content, Color color)
        {
            var label = new GameObject("Transfer", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(panel.transform, false);
            label.raycastTarget = false;
            if (note != null)
            {
                label.font = note.font;
                label.fontSize = note.fontSize + 2;
            }
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = content;
            label.color = color;
            label.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return label;
        }

        static IEnumerator Wait(float seconds)
        {
            if (seconds > 0f)
                yield return new WaitForSecondsRealtime(seconds);
        }

        static void SetAlpha(MouthCard card, float alpha)
        {
            var group = card.GetComponent<CanvasGroup>();
            if (group == null)
                group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
        }

        void SetButtonsEnabled(bool enabled)
        {
            buttonsEnabled = enabled;
            foreach (var card in cards)
                card.Button.interactable = enabled;
        }

        internal void Configure(GameObject newPanel, Text newSubtitle, Text newNote, MouthCard[] newCards, RectTransform newSlotsRow,
            Image[] newSlotIcons)
        {
            panel = newPanel;
            subtitle = newSubtitle;
            note = newNote;
            cards = newCards;
            slotsRow = newSlotsRow;
            slotIcons = newSlotIcons;
        }

        internal void SetLevelBadges(Sprite[] newBadges) => levelBadges = newBadges;

        internal void SetTiming(float newInputLock, float newFade, float newFlight, float newInterval, float newEndPause)
        {
            inputLockDuration = newInputLock;
            fadeDuration = newFade;
            flightDuration = Mathf.Max(0.05f, newFlight);
            transferInterval = newInterval;
            endPause = newEndPause;
        }
    }
}

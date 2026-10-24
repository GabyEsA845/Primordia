using System.Collections;
using Primordia.Core;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Muestra el panel que toca según el estado del juego: pausa, derrota o victoria.
    /// La derrota espera un momento para que se vea el golpe final. En la victoria, si la Boca que se lleva tiene
    /// criatura (desde el piso 2), se ve la criatura en reposo.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject defeatPanel;
        [SerializeField] GameObject victoryPanel;
        [Tooltip("Segundos (tiempo real) entre la muerte del jugador y la pantalla de derrota.")]
        [SerializeField, Min(0f)] float defeatDelay = 1f;
        [Tooltip("Criatura del jugador en el panel de victoria (oculta si su Boca no tiene).")]
        [SerializeField] Image victoryCreature;

        GameManager manager;
        Coroutine defeatRoutine;

        void Start()
        {
            manager = GameManager.Instance;
            if (manager == null)
                return;

            manager.StateChanged += OnStateChanged;
            Show(manager.State);
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.StateChanged -= OnStateChanged;
        }

        internal void Configure(GameObject pause, GameObject defeat, GameObject victory, float delay)
        {
            pausePanel = pause;
            defeatPanel = defeat;
            victoryPanel = victory;
            defeatDelay = delay;
        }

        internal void SetVictoryCreature(Image image) => victoryCreature = image;

        void OnStateChanged(GameState previous, GameState current) => Show(current);

        void Show(GameState state)
        {
            SetActive(pausePanel, state == GameState.Paused);
            SetActive(victoryPanel, state == GameState.Victory);
            if (state == GameState.Victory)
                ShowVictoryCreature();

            if (defeatRoutine != null)
            {
                StopCoroutine(defeatRoutine);
                defeatRoutine = null;
            }

            if (state == GameState.Defeat && defeatDelay > 0f)
                defeatRoutine = StartCoroutine(ShowDefeatAfterDelay());
            else
                SetActive(defeatPanel, state == GameState.Defeat);
        }

        void ShowVictoryCreature()
        {
            if (victoryCreature == null)
                return;

            var player = GameObject.FindWithTag("Player");
            var mouth = player != null && player.TryGetComponent<PlayerMutations>(out var mutations)
                ? mutations.Get(MutationSlot.Mouth)
                : null;
            var frames = mouth != null ? mouth.CreatureIdleFrames : System.Array.Empty<Sprite>();
            victoryCreature.gameObject.SetActive(frames.Length > 0);
            if (frames.Length == 0)
                return;

            if (!victoryCreature.TryGetComponent<UISpriteAnimation>(out var animation))
                animation = victoryCreature.gameObject.AddComponent<UISpriteAnimation>();
            animation.Configure(frames, 0.18f, newLoop: true);
        }

        IEnumerator ShowDefeatAfterDelay()
        {
            SetActive(defeatPanel, false);
            yield return new WaitForSecondsRealtime(defeatDelay);
            SetActive(defeatPanel, true);
            defeatRoutine = null;
        }

        static void SetActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}

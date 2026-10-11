using System.Collections;
using Primordia.Core;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Muestra el panel que toca según el estado del juego: pausa, derrota o victoria.
    /// La derrota espera un momento para que se vea el golpe final.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject defeatPanel;
        [SerializeField] GameObject victoryPanel;
        [Tooltip("Segundos (tiempo real) entre la muerte del jugador y la pantalla de derrota.")]
        [SerializeField, Min(0f)] float defeatDelay = 1f;

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

        void OnStateChanged(GameState previous, GameState current) => Show(current);

        void Show(GameState state)
        {
            SetActive(pausePanel, state == GameState.Paused);
            SetActive(victoryPanel, state == GameState.Victory);

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

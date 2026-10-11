using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Primordia.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        MutationChoice,
        Victory,
        Defeat
    }

    /// <summary>
    /// Estado global de la partida. Hay una sola instancia y sobrevive a los cambios de escena.
    /// Pausa el tiempo en <see cref="GameState.Paused"/> y <see cref="GameState.MutationChoice"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Estado al cargar la escena. Usa Playing en escenas de prueba y MainMenu en el menú.")]
        [SerializeField] GameState initialState = GameState.Playing;

        [Tooltip("Escribe en la Console cada cambio de estado. Útil durante el desarrollo.")]
        [SerializeField] bool logStateChanges = true;

        public GameState State { get; private set; }

        /// <summary>Se lanza al cambiar de estado: (anterior, nuevo).</summary>
        public event Action<GameState, GameState> StateChanged;

        InputAction pauseAction;

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            State = initialState;
            ApplyTimeScale(State);
            pauseAction = InputSystem.actions?.FindAction("Player/Pause");
        }

        void OnEnable()
        {
            if (pauseAction != null)
                pauseAction.performed += OnPausePerformed;
        }

        void OnDisable()
        {
            if (pauseAction != null)
                pauseAction.performed -= OnPausePerformed;
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            Time.timeScale = 1f;
        }

        public void SetState(GameState newState)
        {
            if (newState == State)
                return;

            var previous = State;
            State = newState;
            ApplyTimeScale(newState);

            if (logStateChanges)
                Debug.Log($"GameManager: {previous} → {newState} (Time.timeScale = {Time.timeScale})", this);

            StateChanged?.Invoke(previous, newState);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
                SetState(GameState.Paused);
            else if (State == GameState.Paused)
                SetState(GameState.Playing);
        }

        public void Resume()
        {
            if (State == GameState.Paused)
                SetState(GameState.Playing);
        }

        public void StartGame() => LoadScene(SceneNames.Game, GameState.Playing);

        /// <summary>Vuelve a cargar la escena actual desde el principio.</summary>
        public void RestartLevel() => LoadScene(SceneManager.GetActiveScene().name, GameState.Playing);

        public void GoToMainMenu() => LoadScene(SceneNames.MainMenu, GameState.MainMenu);

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // El estado se fija antes de cargar: el GameManager sobrevive al cambio de escena
        // y el de la escena nueva se destruye por ser un duplicado.
        void LoadScene(string sceneName, GameState stateAfterLoad)
        {
            SetState(stateAfterLoad);
            SceneManager.LoadScene(sceneName);
        }

        void OnPausePerformed(InputAction.CallbackContext _) => TogglePause();

        static void ApplyTimeScale(GameState state)
        {
            Time.timeScale = state is GameState.Paused or GameState.MutationChoice ? 0f : 1f;
        }
    }
}

using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    public enum FlowAction
    {
        Play,
        Resume,
        Restart,
        MainMenu,
        Quit
    }

    /// <summary>Botón de menú que llama a una acción del <see cref="GameManager"/>.</summary>
    [RequireComponent(typeof(Button))]
    public class GameFlowButton : MonoBehaviour
    {
        [SerializeField] FlowAction action;

        public FlowAction Action => action;

        void Awake()
        {
            // En el navegador no se puede cerrar la pestaña desde el juego: el botón Salir sobra.
            if (action == FlowAction.Quit && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                gameObject.SetActive(false);
                return;
            }

            GetComponent<Button>().onClick.AddListener(Run);
        }

        void Run()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("No hay GameManager en la escena.", this);
                return;
            }

            switch (action)
            {
                case FlowAction.Play:
                    manager.StartGame();
                    break;
                case FlowAction.Resume:
                    manager.Resume();
                    break;
                case FlowAction.Restart:
                    manager.RestartLevel();
                    break;
                case FlowAction.MainMenu:
                    manager.GoToMainMenu();
                    break;
                case FlowAction.Quit:
                    manager.QuitGame();
                    break;
            }
        }
    }
}

using Primordia.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>El bestiario desde el menú principal: sustituye al menú y se cierra con Esc.</summary>
    public class MainMenuBestiary : MemScreen
    {
        [SerializeField] Button openButton;
        [SerializeField] GameObject mainPanel;

        BestiaryView view;

        public bool IsOpen => view != null;

        internal void Configure(Button button, GameObject panel)
        {
            openButton = button;
            mainPanel = panel;
        }

        void Awake()
        {
            if (openButton != null)
                openButton.onClick.AddListener(Open);
        }

        public void Open()
        {
            Art = PauseMenuArt.Current;
            if (Art == null || view != null)
                return;
            Sfx.Play(Sounds.UIClick);
            if (mainPanel != null)
                mainPanel.SetActive(false);
            view = new BestiaryView(this, Close);
            view.Show(0);
        }

        public void Close()
        {
            ClearPage();
            view = null;
            if (mainPanel != null)
                mainPanel.SetActive(true);
        }

        protected override void ChangeTab(int step) => view?.ChangeTab(step);

        protected override void Update()
        {
            if (view == null)
                return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
            else
                base.Update();
        }
    }
}

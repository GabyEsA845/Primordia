using Primordia.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Primordia.Audio
{
    /// <summary>
    /// Sonidos de un botón: al pasar el ratón por encima y al pulsarlo. El <see cref="AudioManager"/> se lo pone a
    /// todos los botones de cada escena al cargarla (<see cref="BindAll"/>), con el sonido que toca a cada uno, así
    /// sirve también para los prefabs de interfaz que se regeneran.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSounds : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] string hover = Sounds.UIHover;
        [SerializeField] string click = Sounds.UIClick;

        Button button;

        public string Hover => hover;
        public string Click => click;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && button.IsInteractable())
                Sfx.Play(hover);
        }

        void OnClick() => Sfx.Play(click);

        internal void Configure(string newHover, string newClick)
        {
            hover = newHover;
            click = newClick;
        }

        /// <summary>Pone sus sonidos a los botones de la escena que aún no los tienen.</summary>
        public static void BindAll()
        {
            foreach (var target in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (target.TryGetComponent<ButtonSounds>(out _))
                    continue;
                target.gameObject.AddComponent<ButtonSounds>().Configure(Sounds.UIHover, ClickFor(target));
            }
        }

        /// <summary>Sonido al pulsar según lo que hace el botón.</summary>
        internal static string ClickFor(Button target)
        {
            if (target.TryGetComponent<GameFlowButton>(out var flow))
            {
                return flow.Action switch
                {
                    FlowAction.Play => Sounds.UIStartGame,
                    // Al cerrar la pausa ya suena su cierre.
                    FlowAction.Resume => null,
                    _ => Sounds.UIClick
                };
            }
            if (target.GetComponentInParent<MutationCard>(true) != null)
                return Sounds.UICardSelect;
            // El otro botón de la elección es Saltar.
            if (target.GetComponentInParent<MutationChoiceUI>(true) != null)
                return Sounds.UIBack;
            return Sounds.UIClick;
        }
    }
}

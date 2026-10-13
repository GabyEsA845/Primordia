using Primordia.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Player
{
    /// <summary>
    /// Elige el <see cref="Interactable"/> más cercano a su alcance (solo mientras se juega) y lo usa al pulsar
    /// Interactuar (E en teclado, botón oeste en mando).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        // Las pruebas lo desactivan para controlar interactPressed a mano.
        internal bool readInput = true;
        internal bool interactPressed;

        PlayerController controller;
        InputAction interactAction;

        /// <summary>Lo que tiene delante; null si nada.</summary>
        public Interactable Focused { get; private set; }

        /// <summary>True mientras se mantiene pulsado Interactuar (para dibujar la tecla hundida).</summary>
        public bool IsHolding => readInput && interactAction != null && interactAction.IsPressed();

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            interactAction = InputSystem.actions?.FindAction("Player/Interact");
        }

        void OnDisable() => SetFocus(null);

        void Update()
        {
            if (readInput)
                interactPressed = interactAction != null && interactAction.WasPressedThisFrame();

            SetFocus(controller.CanAct ? Nearest() : null);

            if (interactPressed && Focused != null)
                Focused.Interact(gameObject);

            interactPressed = false;
        }

        Interactable Nearest()
        {
            Vector2 position = transform.position;
            Interactable best = null;
            float bestDistance = float.MaxValue;
            foreach (var candidate in Interactable.Active)
            {
                if (candidate == null || !candidate.IsAvailable)
                    continue;

                float distance = Vector2.Distance(position, candidate.Position);
                if (distance <= candidate.Radius && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        void SetFocus(Interactable target)
        {
            if (target == Focused)
                return;

            if (Focused != null)
                Focused.SetFocused(false, gameObject);
            Focused = target;
            if (Focused != null)
                Focused.SetFocused(true, gameObject);
        }
    }
}

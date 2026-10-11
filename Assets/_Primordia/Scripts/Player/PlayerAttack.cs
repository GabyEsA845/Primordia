using Primordia.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Player
{
    /// <summary>
    /// Ataque principal del jugador: dispara hacia el ratón con el clic izquierdo.
    /// Con un ataque automático basta mantener el botón; si no, es un disparo por clic.
    /// No dispara durante la esquiva ni con el juego en pausa.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Weapon))]
    public class PlayerAttack : MonoBehaviour
    {
        // Las pruebas lo desactivan para controlar attackHeld y attackPressed a mano.
        internal bool readInput = true;
        internal bool attackHeld;
        internal bool attackPressed;

        PlayerController controller;
        Weapon weapon;
        InputAction attackAction;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            weapon = GetComponent<Weapon>();
            attackAction = InputSystem.actions?.FindAction("Player/Attack");
        }

        void Update()
        {
            if (readInput)
            {
                attackHeld = attackAction != null && attackAction.IsPressed();
                attackPressed = attackAction != null && attackAction.WasPressedThisFrame();
            }

            bool automatic = weapon.Attack != null && weapon.Attack.Automatic;
            bool wantsToFire = automatic ? attackHeld : attackPressed;

            if (wantsToFire && controller.CanAct && !controller.IsDodging)
                weapon.TryFire(controller.AimDirection);

            // El clic solo cuenta en el frame en que ocurre.
            attackPressed = false;
        }
    }
}

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
        SpinAttack spin;
        JetAttack jet;
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

            // La Peonza se mantiene: la maneja SpinAttack, no el arma.
            if (weapon.Attack != null && weapon.Attack.IsSpin)
            {
                if (spin == null && !TryGetComponent(out spin))
                    spin = gameObject.AddComponent<SpinAttack>();
                if (jet != null && jet.IsActive)
                    jet.Cancel();
                spin.Tick(attackHeld);
                attackPressed = false;
                return;
            }
            // El Chorro de vinagre también se mantiene: lo maneja JetAttack.
            if (weapon.Attack != null && weapon.Attack.IsJet)
            {
                if (jet == null && !TryGetComponent(out jet))
                    jet = gameObject.AddComponent<JetAttack>();
                if (spin != null && spin.IsActive)
                    spin.Cancel();
                jet.Tick(attackHeld);
                attackPressed = false;
                return;
            }
            if (jet != null && jet.IsActive)
                jet.Cancel();
            if (spin != null && spin.IsActive)
                spin.Cancel();

            bool automatic = weapon.Attack != null && weapon.Attack.Automatic;
            bool wantsToFire = automatic ? attackHeld : attackPressed;

            // El remolino de la Corona de brazos aparece en el cursor.
            weapon.AimPoint = (Vector2)transform.position + controller.AimOffset;
            if (wantsToFire && controller.CanAct && !controller.IsDodging)
                weapon.TryFire(controller.AimDirection);

            // El clic solo cuenta en el frame en que ocurre.
            attackPressed = false;
        }
    }
}

using System;
using Primordia.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Mutations
{
    /// <summary>
    /// Habilidad del Órgano especial: se usa con el clic derecho y luego se recarga.
    /// Sin mutación de Órgano, el clic derecho no hace nada.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerMutations))]
    public class PlayerAbility : MonoBehaviour
    {
        // Las pruebas lo desactivan para controlar specialPressed a mano.
        internal bool readInput = true;
        internal bool specialPressed;

        public AbilityMutation Ability { get; private set; }
        public bool IsReady => Ability != null && Time.time >= readyTime;

        /// <summary>True mientras la habilidad se está recargando.</summary>
        public bool IsCharging => Ability != null && Time.time < readyTime;

        /// <summary>De 0 (recién usada) a 1 (lista).</summary>
        public float CooldownProgress =>
            !IsCharging ? 1f : Mathf.Clamp01(1f - (readyTime - Time.time) / Mathf.Max(0.01f, currentCooldown));

        public event Action Used;

        PlayerController controller;
        PlayerMutations mutations;
        InputAction specialAction;
        float readyTime;
        // Recarga de la última vez que se usó (depende del nivel del Órgano).
        float currentCooldown = 1f;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            mutations = GetComponent<PlayerMutations>();
            specialAction = InputSystem.actions?.FindAction("Player/Special");
        }

        void Update()
        {
            if (readInput)
                specialPressed = specialAction != null && specialAction.WasPressedThisFrame();

            if (specialPressed)
                TryUse();

            specialPressed = false;
        }

        /// <summary>Usa la habilidad si está lista y el jugador puede actuar.</summary>
        public bool TryUse()
        {
            if (!IsReady || !controller.CanAct)
                return false;

            currentCooldown = Ability.CooldownAt(Mathf.Max(1, mutations.LevelOf(Ability.Slot)));
            readyTime = Time.time + currentCooldown;
            Ability.Activate(mutations);
            Used?.Invoke();
            return true;
        }

        /// <summary>Cambia la habilidad. La nueva empieza lista para usarse.</summary>
        internal void SetAbility(AbilityMutation ability)
        {
            Ability = ability;
            readyTime = 0f;
        }
    }
}

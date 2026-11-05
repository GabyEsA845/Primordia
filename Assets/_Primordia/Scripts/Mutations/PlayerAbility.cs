using System;
using Primordia.Audio;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Mutations
{
    /// <summary>
    /// Habilidad del Órgano especial: se usa con el clic derecho y luego se recarga, con el tiempo o, si la
    /// habilidad lo pide (<see cref="AbilityMutation.KillsToChargeAt"/>), matando enemigos.
    /// Sin mutación de Órgano, el clic derecho no hace nada.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerMutations))]
    public class PlayerAbility : MonoBehaviour
    {
        // Las pruebas lo desactivan para controlar specialPressed a mano.
        internal bool readInput = true;
        internal bool specialPressed;
        // Para las pruebas: el clic se mantiene (habilidades de mantener y soltar).
        internal bool specialHeld;
        bool holding;

        public AbilityMutation Ability { get; private set; }
        public bool IsReady => Ability != null && (UsesKills ? kills >= KillsNeeded : Time.time >= readyTime);

        /// <summary>True mientras la habilidad se está recargando.</summary>
        public bool IsCharging => Ability != null && !IsReady;

        /// <summary>De 0 (recién usada) a 1 (lista).</summary>
        public float CooldownProgress =>
            !IsCharging ? 1f
            : UsesKills ? Mathf.Clamp01((float)kills / KillsNeeded)
            : Mathf.Clamp01(1f - (readyTime - Time.time) / Mathf.Max(0.01f, currentCooldown));

        /// <summary>True si se recarga matando enemigos en lugar de con el tiempo.</summary>
        public bool UsesKills => KillsNeeded > 0;

        /// <summary>Enemigos que hay que matar para recargarla entera (0 si va por tiempo).</summary>
        public int KillsRequired => KillsNeeded;

        /// <summary>Enemigos que faltan por matar para recargarla (0 si está lista o va por tiempo).</summary>
        public int KillsLeft => UsesKills ? Mathf.Max(0, KillsNeeded - kills) : 0;

        public event Action Used;

        /// <summary>Se lanza al intentar usarla mientras aún se recarga.</summary>
        public event Action Refused;

        PlayerController controller;
        PlayerMutations mutations;
        InputAction specialAction;
        float readyTime;
        // Recarga de la última vez que se usó (depende del nivel del Órgano).
        float currentCooldown = 1f;
        int kills;
        bool running;

        int KillsNeeded => Ability != null ? Ability.KillsToChargeAt(Mathf.Max(1, mutations.LevelOf(Ability.Slot))) : 0;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            mutations = GetComponent<PlayerMutations>();
            specialAction = InputSystem.actions?.FindAction("Player/Special");
        }

        void OnEnable() => Enemy.AnyDied += OnEnemyDied;

        void OnDisable() => Enemy.AnyDied -= OnEnemyDied;

        void OnEnemyDied(Enemy enemy)
        {
            if (UsesKills && kills < KillsNeeded)
            {
                kills++;
                Sfx.Play(Sounds.VacuoleCharge);
            }
        }

        void Update()
        {
            // Mientras dura su efecto, la recarga espera (empieza cuando acaba).
            if (running && Ability != null && !UsesKills)
            {
                if (Ability.IsRunning(mutations))
                    readyTime = Time.time + currentCooldown;
                else
                    running = false;
            }

            if (readInput)
            {
                specialPressed = specialAction != null && specialAction.WasPressedThisFrame();
                specialHeld = specialAction != null && specialAction.IsPressed();
            }

            if (Ability != null && Ability.HoldToCharge)
            {
                UpdateHold();
                specialPressed = false;
                return;
            }

            if (specialPressed)
                TryUse();

            specialPressed = false;
        }

        // Habilidad de mantener y soltar: al pulsar empieza a cargar; al soltar sale y empieza la recarga.
        void UpdateHold()
        {
            if (!holding)
            {
                if (!specialPressed)
                    return;
                if (!IsReady || !controller.CanAct || !Ability.CanActivate(mutations))
                {
                    if (!IsReady && controller.CanAct)
                        Refused?.Invoke();
                    return;
                }
                holding = true;
                Ability.Activate(mutations);
                return;
            }

            if (specialHeld && controller.CanAct)
                return;
            holding = false;
            if (!Ability.Release(mutations))
                return;
            currentCooldown = Ability.CooldownAt(Mathf.Max(1, mutations.LevelOf(Ability.Slot)));
            readyTime = Time.time + currentCooldown;
            kills = 0;
            Used?.Invoke();
        }

        /// <summary>Usa la habilidad si está lista, sirve de algo y el jugador puede actuar.</summary>
        public bool TryUse()
        {
            if (!IsReady || !controller.CanAct || !Ability.CanActivate(mutations))
            {
                if (Ability != null && !IsReady && controller.CanAct)
                    Refused?.Invoke();
                return false;
            }

            currentCooldown = Ability.CooldownAt(Mathf.Max(1, mutations.LevelOf(Ability.Slot)));
            readyTime = Time.time + currentCooldown;
            kills = 0;
            Ability.Activate(mutations);
            running = Ability.IsRunning(mutations);
            Used?.Invoke();
            return true;
        }

        /// <summary>Cambia la habilidad. La nueva empieza lista para usarse.</summary>
        internal void SetAbility(AbilityMutation ability)
        {
            Ability = ability;
            running = false;
            holding = false;
            readyTime = 0f;
            kills = int.MaxValue / 2;
        }

        /// <summary>Cuenta una muerte como si hubiera ocurrido (pruebas).</summary>
        internal void CountKill() => OnEnemyDied(null);
    }
}

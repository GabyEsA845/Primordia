using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Sonidos del jugador que salen de sus eventos: daño, muerte, curación, latido con poca vida, esquiva (la de
    /// las Extremidades que tengan la suya), barrera rota, mutación nueva, habilidad lista o aún recargando y linaje
    /// completado. El <see cref="AudioManager"/> se lo pone al jugador de cada escena (<see cref="Attach"/>).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerAudio : MonoBehaviour
    {
        // Al cargar la escena el jugador se equipa y se cura: no debe sonar.
        const float QuietTime = 0.5f;
        // Con esta vida o menos (1 = medio corazón) late el corazón.
        const int LowHealth = 1;

        Health health;
        PlayerController controller;
        PlayerMutations mutations;
        PlayerAbility ability;
        LineageEvolution evolution;
        int lastHealth;
        int lastMax;
        bool levelingUp;
        bool wasCharging;
        float quietUntil;

        bool Quiet => Time.unscaledTime < quietUntil;

        /// <summary>Se lo pone al jugador de la escena si no lo tiene.</summary>
        public static void Attach()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null && player.TryGetComponent<Health>(out _) && !player.TryGetComponent<PlayerAudio>(out _))
                player.AddComponent<PlayerAudio>();
        }

        void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            mutations = GetComponent<PlayerMutations>();
            ability = GetComponent<PlayerAbility>();
            evolution = GetComponent<LineageEvolution>();
        }

        void OnEnable()
        {
            quietUntil = Time.unscaledTime + QuietTime;
            lastHealth = health.Current;
            lastMax = health.Max;
            wasCharging = ability != null && ability.IsCharging;

            health.Damaged += OnDamaged;
            health.Died += OnDied;
            health.Changed += OnHealthChanged;
            health.BarrierBlocked += OnBarrierBlocked;
            if (controller != null)
                controller.DodgeStarted += OnDodgeStarted;
            if (mutations != null)
            {
                mutations.Changed += OnMutationChanged;
                mutations.LevelChanged += OnLevelChanged;
            }
            if (ability != null)
                ability.Refused += OnAbilityRefused;
            if (evolution != null)
                evolution.Evolved += OnEvolved;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            health.Changed -= OnHealthChanged;
            health.BarrierBlocked -= OnBarrierBlocked;
            if (controller != null)
                controller.DodgeStarted -= OnDodgeStarted;
            if (mutations != null)
            {
                mutations.Changed -= OnMutationChanged;
                mutations.LevelChanged -= OnLevelChanged;
            }
            if (ability != null)
                ability.Refused -= OnAbilityRefused;
            if (evolution != null)
                evolution.Evolved -= OnEvolved;
            Sfx.StopLoop(Sounds.PlayerLowHealthLoop);
        }

        void Update()
        {
            if (ability == null)
                return;
            bool charging = ability.IsCharging;
            if (wasCharging && !charging && ability.Ability != null)
                Sfx.Play(Sounds.PlayerAbilityReady);
            wasCharging = charging;
        }

        void OnDamaged(int _) => Sfx.Play(Sounds.PlayerHurt);

        void OnDied()
        {
            Sfx.Play(Sounds.PlayerDeath);
            Sfx.StopLoop(Sounds.PlayerLowHealthLoop);
        }

        void OnHealthChanged(int current, int max)
        {
            // Curación: sube la vida sin que cambie la máxima (el Caparazón y el corazón extra la suben a la vez).
            if (current > lastHealth && max == lastMax && !health.IsDead && !Quiet)
                Sfx.Play(Sounds.PlayerHeal);
            lastHealth = current;
            lastMax = max;

            if (current <= LowHealth && !health.IsDead)
                Sfx.StartLoop(Sounds.PlayerLowHealthLoop);
            else
                Sfx.StopLoop(Sounds.PlayerLowHealthLoop);
        }

        void OnBarrierBlocked() => Sfx.Play(Sounds.LineageBarrierBlock);

        void OnDodgeStarted()
        {
            var limbs = mutations != null ? mutations.Get(MutationSlot.Limbs) : null;
            Sfx.Play(limbs != null && !string.IsNullOrEmpty(limbs.DodgeSound) ? limbs.DodgeSound : Sounds.PlayerDodge);
        }

        // Al subir de nivel llega LevelChanged y luego Changed: esa no es una mutación nueva (la subida ya suena
        // con su efecto, Effect_LevelUp).
        void OnLevelChanged(MutationSlot slot, int level) => levelingUp = true;

        void OnMutationChanged(MutationSlot slot, MutationData mutation)
        {
            if (mutation != null && !levelingUp && !Quiet)
                Sfx.Play(Sounds.MutationGain);
            levelingUp = false;
        }

        void OnAbilityRefused() => Sfx.Play(Sounds.PlayerAbilityNotReady);

        void OnEvolved(LineageData lineage) => Sfx.Play(Sounds.LineageComplete);
    }
}

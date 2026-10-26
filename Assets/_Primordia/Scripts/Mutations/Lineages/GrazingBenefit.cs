using Primordia.Audio;
using Primordia.Biomass;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Pastar biomasa (Herbívoro): cada cierta biomasa recogida cura medio corazón; con la vida llena da un medio
    /// corazón verde (<see cref="Health.Barrier"/>) que para un golpe, como mucho uno a la vez (dos en el piso 2).
    /// </summary>
    public class GrazingBenefit : MonoBehaviour
    {
        PlayerBiomass biomass;
        Health health;
        int biomassPerHeal = 10;
        GameObject healEffect;
        int grazed;
        int maxBarrier = 1;
        int granted;

        /// <summary>Biomasa recogida que aún no ha curado.</summary>
        public int Grazed => grazed;

        public void Configure(PlayerBiomass newBiomass, Health newHealth, int newBiomassPerHeal, GameObject newHealEffect,
            int newMaxBarrier = 1)
        {
            maxBarrier = Mathf.Max(1, newMaxBarrier);
            Unsubscribe();
            biomass = newBiomass;
            health = newHealth;
            biomassPerHeal = Mathf.Max(1, newBiomassPerHeal);
            healEffect = newHealEffect;
            if (biomass != null)
                biomass.Changed += OnBiomassChanged;
            health.BarrierBlocked += OnBarrierBlocked;
        }

        void OnBiomassChanged(int amount, int change)
        {
            // Solo lo que se recoge; lo que se gasta en la tienda no cuenta.
            if (change <= 0 || health.IsDead)
                return;

            grazed += change;
            while (grazed >= biomassPerHeal)
            {
                grazed -= biomassPerHeal;
                Graze();
            }
        }

        void Graze()
        {
            if (health.Current < health.Max)
                health.Heal(1);
            else if (health.Barrier < maxBarrier)
            {
                health.Barrier++;
                granted++;
                Sfx.Play(Sounds.LineageBarrier);
            }
            else
                return;

            OneShotEffect.Spawn(healEffect, transform.position, parent: transform);
        }

        void OnBarrierBlocked() => granted = Mathf.Min(granted, health.Barrier);

        void Unsubscribe()
        {
            if (biomass != null)
                biomass.Changed -= OnBiomassChanged;
            if (health != null)
                health.BarrierBlocked -= OnBarrierBlocked;
        }

        void OnDestroy()
        {
            Unsubscribe();
            // Al perder el linaje se pierden también los corazones verdes que dio.
            if (granted > 0 && health != null)
                health.Barrier = Mathf.Max(0, health.Barrier - granted);
        }
    }
}

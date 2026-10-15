using Primordia.Biomass;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Pastar biomasa (Herbívoro): cada cierta biomasa recogida cura medio corazón; con la vida llena da un medio
    /// corazón verde (<see cref="Health.Barrier"/>) que para un golpe, como mucho uno a la vez.
    /// </summary>
    public class GrazingBenefit : MonoBehaviour
    {
        PlayerBiomass biomass;
        Health health;
        int biomassPerHeal = 10;
        GameObject healEffect;
        int grazed;
        bool grantedBarrier;

        /// <summary>Biomasa recogida que aún no ha curado.</summary>
        public int Grazed => grazed;

        public void Configure(PlayerBiomass newBiomass, Health newHealth, int newBiomassPerHeal, GameObject newHealEffect)
        {
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
            else if (health.Barrier == 0)
            {
                health.Barrier = 1;
                grantedBarrier = true;
            }
            else
                return;

            OneShotEffect.Spawn(healEffect, transform.position, parent: transform);
        }

        void OnBarrierBlocked()
        {
            if (health.Barrier == 0)
                grantedBarrier = false;
        }

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
            // Al perder el linaje se pierde también el corazón verde que dio.
            if (grantedBarrier && health != null)
                health.Barrier = 0;
        }
    }
}

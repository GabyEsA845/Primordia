using Primordia.Biomass;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Corriente nutritiva (Lirio de mar): los remolinos del jugador atraen también la biomasa del suelo que cae dentro;
    /// la biomasa vuela hacia él como con el imán.
    /// </summary>
    public class NutrientCurrentTouch : MonoBehaviour
    {
        const float Interval = 0.1f;

        Weapon weapon;
        float next;

        /// <summary>Biomasa que ha atraído (para las pruebas).</summary>
        public int Attracted { get; private set; }

        public void Configure(Weapon newWeapon) => weapon = newWeapon;

        void Update()
        {
            if (weapon == null || Time.time < next || Whirlpool.Active.Count == 0)
                return;
            next = Time.time + Interval;

            BiomassPickup[] pickups = null;
            foreach (var whirlpool in Whirlpool.Active)
            {
                if (whirlpool == null || whirlpool.Source != weapon)
                    continue;
                pickups ??= FindObjectsByType<BiomassPickup>(FindObjectsSortMode.None);
                Vector2 center = whirlpool.transform.position;
                foreach (var pickup in pickups)
                {
                    if (pickup == null || pickup.IsAttracted)
                        continue;
                    if (Vector2.Distance(pickup.transform.position, center) <= whirlpool.Radius)
                    {
                        pickup.Attract();
                        Attracted++;
                    }
                }
            }
        }
    }
}

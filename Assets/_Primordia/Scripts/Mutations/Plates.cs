using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Placas (Caparazón nivel III): un escudo que anula el primer golpe y se recarga al entrar en una sala nueva.
    /// Mientras está cargado se ve un brillo sobre la concha; al gastarse, las placas saltan en pedazos.
    /// </summary>
    public class Plates : MonoBehaviour
    {
        Health health;
        GameObject effectPrefab;
        GameObject breakEffect;
        GameObject effect;
        Room[] rooms = System.Array.Empty<Room>();

        public bool IsCharged => health != null && health.Shields > 0;

        public void Configure(Health newHealth, GameObject newEffect, GameObject newBreakEffect)
        {
            Unsubscribe();
            health = newHealth;
            effectPrefab = newEffect;
            breakEffect = newBreakEffect;
            health.ShieldBlocked += OnShieldBlocked;

            rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            foreach (var room in rooms)
                room.Entered += OnRoomEntered;

            Recharge();
        }

        /// <summary>Vuelve a cargar el escudo (al entrar en una sala nueva).</summary>
        public void Recharge()
        {
            health.Shields = 1;
            if (effect == null && effectPrefab != null)
                effect = Instantiate(effectPrefab, transform);
        }

        void OnRoomEntered(Room _) => Recharge();

        void OnShieldBlocked()
        {
            if (health.Shields > 0)
                return;

            if (effect != null)
                Destroy(effect);
            effect = null;
            OneShotEffect.Spawn(breakEffect, transform.position, parent: transform);
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (health != null)
                health.Shields = 0;
            if (effect != null)
                Destroy(effect);
        }

        void Unsubscribe()
        {
            if (health != null)
                health.ShieldBlocked -= OnShieldBlocked;
            foreach (var room in rooms)
            {
                if (room != null)
                    room.Entered -= OnRoomEntered;
            }
        }
    }
}

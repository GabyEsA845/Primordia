using Primordia.Combat;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Emboscada (Aletas nivel III): al terminar cada esquiva, el daño del arma sube durante un rato y se ve un aura
    /// sobre el jugador. Una esquiva nueva vuelve a empezar la cuenta.
    /// </summary>
    public class Ambush : MonoBehaviour
    {
        PlayerController controller;
        Weapon weapon;
        float damageBonus;
        float duration;
        GameObject effectPrefab;
        GameObject effect;
        float activeUntil = -1f;

        public bool IsActive => activeUntil >= 0f;

        public void Configure(PlayerController newController, Weapon newWeapon, float newDamageBonus, float newDuration, GameObject newEffect)
        {
            if (controller != null)
                controller.DodgeEnded -= OnDodgeEnded;

            controller = newController;
            weapon = newWeapon;
            damageBonus = newDamageBonus;
            duration = newDuration;
            effectPrefab = newEffect;
            controller.DodgeEnded += OnDodgeEnded;
        }

        void OnDodgeEnded()
        {
            activeUntil = Time.time + duration;
            weapon.DamageMultiplier = 1f + damageBonus;
            if (effect == null && effectPrefab != null)
                effect = Instantiate(effectPrefab, transform);
        }

        void Update()
        {
            if (IsActive && Time.time >= activeUntil)
                End();
        }

        void End()
        {
            activeUntil = -1f;
            if (weapon != null)
                weapon.DamageMultiplier = 1f;
            if (effect != null)
                Destroy(effect);
            effect = null;
        }

        void OnDestroy()
        {
            if (controller != null)
                controller.DodgeEnded -= OnDodgeEnded;
            End();
        }
    }
}

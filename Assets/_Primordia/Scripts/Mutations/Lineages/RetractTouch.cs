using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Retraerse (Tortuga): al usar la habilidad (la Vacuola digestiva) eres invulnerable un momento.</summary>
    public class RetractTouch : MonoBehaviour
    {
        PlayerAbility ability;
        Health health;
        float duration = 1.5f;
        GameObject effectPrefab;
        GameObject effect;

        public void Configure(PlayerAbility newAbility, Health newHealth, float newDuration, GameObject newEffect)
        {
            Unsubscribe();
            ability = newAbility;
            health = newHealth;
            duration = newDuration;
            effectPrefab = newEffect;
            if (ability != null)
                ability.Used += OnUsed;
        }

        void OnUsed()
        {
            health.MakeInvulnerable(duration);
            if (effect != null)
                Destroy(effect);
            if (effectPrefab != null)
            {
                effect = Instantiate(effectPrefab, transform);
                Destroy(effect, duration);
            }
        }

        void Unsubscribe()
        {
            if (ability != null)
                ability.Used -= OnUsed;
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (effect != null)
                Destroy(effect);
        }
    }
}

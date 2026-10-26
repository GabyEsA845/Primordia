using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Festín (Placodermo): rematar a un enemigo con un mordisco cura medio corazón, como mucho uno cada pocos segundos.
    /// Escucha los remates de <see cref="FinisherBenefit"/> y mira si el golpe era un mordisco (<see cref="Weapon.Striking"/>).
    /// </summary>
    public class FeastTouch : MonoBehaviour
    {
        FinisherBenefit finisher;
        Weapon weapon;
        Health health;
        int heal = 1;
        float cooldown = 6f;
        GameObject healEffect;
        float readyAt;

        /// <summary>Veces que ha curado (para las pruebas).</summary>
        public int Feasts { get; private set; }

        public void Configure(FinisherBenefit newFinisher, Weapon newWeapon, Health newHealth, int newHeal, float newCooldown, GameObject newHealEffect)
        {
            if (finisher != null)
                finisher.Finished -= OnFinished;
            finisher = newFinisher;
            weapon = newWeapon;
            health = newHealth;
            heal = Mathf.Max(1, newHeal);
            cooldown = Mathf.Max(0f, newCooldown);
            healEffect = newHealEffect;
            if (finisher != null)
                finisher.Finished += OnFinished;
        }

        void OnFinished(Health target)
        {
            if (weapon == null || Weapon.Striking != weapon || health == null || health.IsDead || Time.time < readyAt)
                return;
            readyAt = Time.time + cooldown;
            health.Heal(heal);
            Feasts++;
            Sfx.Play(Sounds.LineageBarrier);
            OneShotEffect.Spawn(healEffect, transform.position, parent: transform);
        }

        void OnDestroy()
        {
            if (finisher != null)
                finisher.Finished -= OnFinished;
        }
    }
}

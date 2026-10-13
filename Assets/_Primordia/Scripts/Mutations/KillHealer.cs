using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cura medio corazón cada cierto número de enemigos matados con el arma (Mandíbulas), con un efecto sobre
    /// el jugador si de verdad se curó.
    /// </summary>
    public class KillHealer : MonoBehaviour
    {
        Weapon weapon;
        Health health;
        int healEveryKills;
        GameObject healEffect;

        public int Kills { get; private set; }

        public void Configure(Weapon newWeapon, Health newHealth, int everyKills, GameObject effect = null)
        {
            if (weapon != null)
                weapon.HitTarget -= OnHitTarget;

            weapon = newWeapon;
            health = newHealth;
            healEveryKills = Mathf.Max(1, everyKills);
            healEffect = effect;
            weapon.HitTarget += OnHitTarget;
        }

        void OnDestroy()
        {
            if (weapon != null)
                weapon.HitTarget -= OnHitTarget;
        }

        void OnHitTarget(Health target)
        {
            if (!target.IsDead)
                return;

            Kills++;
            if (Kills % healEveryKills != 0)
                return;

            int before = health.Current;
            health.Heal(1);
            if (health.Current > before)
                OneShotEffect.Spawn(healEffect, health.transform.position, parent: health.transform);
        }
    }
}

using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Cura medio corazón cada cierto número de enemigos matados con el arma (Mandíbulas).</summary>
    public class KillHealer : MonoBehaviour
    {
        Weapon weapon;
        Health health;
        int healEveryKills;

        public int Kills { get; private set; }

        public void Configure(Weapon newWeapon, Health newHealth, int everyKills)
        {
            if (weapon != null)
                weapon.HitTarget -= OnHitTarget;

            weapon = newWeapon;
            health = newHealth;
            healEveryKills = Mathf.Max(1, everyKills);
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
            if (Kills % healEveryKills == 0)
                health.Heal(1);
        }
    }
}

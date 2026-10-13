using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Erizo (Piel urticante nivel III): cada golpe que recibe el jugador dispara púas en círculo.</summary>
    public class QuillBurst : MonoBehaviour
    {
        Health health;
        AttackData attack;
        int count;

        /// <summary>Veces que ha disparado (para las pruebas).</summary>
        public int Bursts { get; private set; }

        public void Configure(Health newHealth, AttackData newAttack, int newCount)
        {
            if (health != null)
                health.Damaged -= OnDamaged;

            health = newHealth;
            attack = newAttack;
            count = Mathf.Max(1, newCount);
            health.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        void OnDamaged(int _)
        {
            if (attack == null || attack.ProjectilePrefab == null || health.IsDead)
                return;

            Bursts++;
            int layer = CombatLayers.ProjectileLayer(Team.Player);
            Vector2 origin = transform.position;
            for (int i = 0; i < count; i++)
            {
                float angle = 360f / count * i * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                ProjectilePool.Get(attack.ProjectilePrefab).Launch(origin, direction, attack, layer, null);
            }
        }
    }
}

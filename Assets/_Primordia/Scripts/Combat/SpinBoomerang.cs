using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Peonza lanzada del nivel III de la Peonza (Bumerán): sale hacia el cursor, da la vuelta al llegar a su alcance o chocar con una
    /// pared y vuelve al lanzador. Daña y aturde a cada enemigo una vez a la ida y otra a la vuelta.
    /// </summary>
    public class SpinBoomerang : MonoBehaviour
    {
        const float Speed = 14f;
        const float HitRadius = 0.7f;
        const float WallRadius = 0.3f;
        const float CatchDistance = 0.5f;
        // Por si el lanzador desaparece o se queda atascado.
        const float MaxSeconds = 4f;

        Transform owner;
        Weapon weapon;
        Vector2 direction;
        float range;
        int damage;
        float stun;
        float traveled;
        float age;
        bool returning;
        readonly HashSet<Health> struck = new();

        public bool IsReturning => returning;

        public void Launch(Transform thrower, Vector2 aim, float maxRange, int hitDamage, float stunSeconds, Weapon source)
        {
            owner = thrower;
            weapon = source;
            direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            range = maxRange;
            damage = hitDamage;
            stun = stunSeconds;
        }

        void Update()
        {
            float step = Speed * Time.deltaTime * TimeWarp.PlayerSpeed;
            age += Time.deltaTime;
            Vector2 position = transform.position;

            if (!returning)
            {
                position += direction * step;
                traveled += step;
                if (traveled >= range || HitsWall(position))
                    Turn();
            }
            else
            {
                if (owner == null || age > MaxSeconds)
                {
                    Destroy(gameObject);
                    return;
                }
                Vector2 toOwner = (Vector2)owner.position - position;
                if (toOwner.magnitude <= step + CatchDistance)
                {
                    Destroy(gameObject);
                    return;
                }
                position += toOwner.normalized * step;
            }

            transform.position = position;
            StrikeNearby(position);
        }

        void Turn()
        {
            returning = true;
            struck.Clear();
        }

        static bool HitsWall(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position, WallRadius))
            {
                if (!hit.isTrigger && hit.GetComponentInParent<Health>() == null && hit.GetComponentInParent<Projectile>() == null)
                    return true;
            }
            return false;
        }

        void StrikeNearby(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position, HitRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                if (hit.isTrigger)
                    continue;
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !struck.Add(target))
                    continue;

                if (target.TakeDamage(damage) && weapon != null)
                    weapon.ReportHit(target);
                if (!target.IsDead && stun > 0f && hit.GetComponentInParent<IPushable>() is { } pushable)
                    pushable.Knockback(Vector2.zero, stun);
            }
        }
    }
}

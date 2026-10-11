using Primordia.Combat;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Glándula de choque: onda que destruye los proyectiles enemigos cercanos y empuja a los enemigos.</summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Shockwave Mutation", fileName = "Mutation_")]
    public class ShockwaveMutation : AbilityMutation
    {
        [SerializeField, Min(0.1f)] float radius = 4f;
        [Tooltip("Velocidad inicial del empujón a los enemigos.")]
        [SerializeField, Min(0f)] float pushSpeed = 14f;
        [SerializeField, Min(0f)] float stunDuration = 0.4f;

        public float Radius => radius;

        public override void Activate(PlayerMutations player)
        {
            Vector2 center = player.transform.position;
            SpawnEffect(center, radius);

            int mask = LayerMask.GetMask(CombatLayers.Enemy, CombatLayers.EnemyProjectile);
            foreach (var hit in Physics2D.OverlapCircleAll(center, radius, mask))
            {
                if (hit.TryGetComponent<Projectile>(out var projectile))
                {
                    projectile.Despawn();
                    continue;
                }

                var body = hit.attachedRigidbody;
                if (body != null && body.TryGetComponent<EnemyAI>(out var enemy))
                {
                    Vector2 away = (body.position - center).normalized;
                    enemy.Knockback(away * pushSpeed, stunDuration);
                }
            }
        }

        internal void Configure(float newRadius, float newPushSpeed, float newStunDuration)
        {
            radius = newRadius;
            pushSpeed = newPushSpeed;
            stunDuration = newStunDuration;
        }
    }
}

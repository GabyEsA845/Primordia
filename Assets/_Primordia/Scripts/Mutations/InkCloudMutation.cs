using Primordia.Combat;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Vejiga de tinta: nube alrededor del jugador que ciega a los enemigos que alcanza.</summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Ink Cloud Mutation", fileName = "Mutation_")]
    public class InkCloudMutation : AbilityMutation
    {
        [SerializeField, Min(0.1f)] float radius = 3f;
        [SerializeField, Min(0.1f)] float blindDuration = 3f;

        public float Radius => radius;
        public float BlindDuration => blindDuration;

        public override void Activate(PlayerMutations player)
        {
            Vector2 center = player.transform.position;
            SpawnEffect(center, radius);

            foreach (var hit in Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var body = hit.attachedRigidbody;
                if (body != null && body.TryGetComponent<EnemyAI>(out var enemy))
                    enemy.Blind(blindDuration);
            }
        }

        internal void Configure(float newRadius, float newBlindDuration)
        {
            radius = newRadius;
            blindDuration = newBlindDuration;
        }
    }
}

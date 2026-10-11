using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Comportamiento de <see cref="DodgePushMutation"/>: cada enemigo se empuja una vez por esquiva.</summary>
    public class DodgePush : MonoBehaviour
    {
        PlayerController controller;
        float pushSpeed;
        float stunDuration;
        int damage;
        readonly HashSet<EnemyAI> pushedThisDodge = new();

        public void Configure(PlayerController newController, float newPushSpeed, float newStunDuration, int newDamage)
        {
            if (controller != null)
                controller.DodgeStarted -= OnDodgeStarted;

            controller = newController;
            pushSpeed = newPushSpeed;
            stunDuration = newStunDuration;
            damage = newDamage;
            controller.DodgeStarted += OnDodgeStarted;
        }

        void OnDestroy()
        {
            if (controller != null)
                controller.DodgeStarted -= OnDodgeStarted;
        }

        void OnDodgeStarted() => pushedThisDodge.Clear();

        void OnCollisionEnter2D(Collision2D collision) => TryPush(collision);

        void OnCollisionStay2D(Collision2D collision) => TryPush(collision);

        void TryPush(Collision2D collision)
        {
            if (controller == null || !controller.IsDodging)
                return;

            var body = collision.rigidbody;
            if (body == null || !body.TryGetComponent<EnemyAI>(out var enemy) || !pushedThisDodge.Add(enemy))
                return;

            Vector2 away = (body.position - (Vector2)transform.position).normalized;
            enemy.Knockback(away * pushSpeed, stunDuration);

            if (damage > 0 && body.TryGetComponent<Health>(out var health))
                health.TakeDamage(damage);
        }
    }
}

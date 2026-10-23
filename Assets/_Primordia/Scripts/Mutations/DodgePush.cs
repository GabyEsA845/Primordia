using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="DodgePushMutation"/>: cada enemigo se empuja una vez por esquiva. Con Pisotón,
    /// al terminar la esquiva aturde a todos los enemigos que haya en su radio.
    /// </summary>
    public class DodgePush : MonoBehaviour
    {
        // Empujón suave del pisotón, para que se note que salen despedidos.
        const float StompPushSpeed = 5f;

        PlayerController controller;
        float pushSpeed;
        float stunDuration;
        int damage;
        GameObject stunMarker;
        float stompRadius;
        GameObject stompEffect;
        readonly HashSet<EnemyAI> pushedThisDodge = new();

        public void Configure(PlayerController newController, float newPushSpeed, float newStunDuration, int newDamage,
            GameObject newStunMarker = null, float newStompRadius = 0f, GameObject newStompEffect = null)
        {
            Unsubscribe();

            controller = newController;
            pushSpeed = newPushSpeed;
            stunDuration = newStunDuration;
            damage = newDamage;
            stunMarker = newStunMarker;
            stompRadius = newStompRadius;
            stompEffect = newStompEffect;
            controller.DodgeStarted += OnDodgeStarted;
            controller.DodgeEnded += OnDodgeEnded;
        }

        void OnDestroy() => Unsubscribe();

        void Unsubscribe()
        {
            if (controller == null)
                return;
            controller.DodgeStarted -= OnDodgeStarted;
            controller.DodgeEnded -= OnDodgeEnded;
        }

        void OnDodgeStarted() => pushedThisDodge.Clear();

        /// <summary>Pisotón: onda alrededor del jugador que aturde a los enemigos que alcanza.</summary>
        void OnDodgeEnded()
        {
            if (stompRadius <= 0f)
                return;

            Vector2 center = transform.position;
            OneShotEffect.Spawn(stompEffect, center);
            foreach (var hit in Physics2D.OverlapCircleAll(center, stompRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var body = hit.attachedRigidbody;
                if (body == null || !body.TryGetComponent<EnemyAI>(out var enemy))
                    continue;

                Vector2 away = (body.position - center).normalized;
                enemy.Knockback(away * StompPushSpeed, stunDuration, stunMarker);
            }
        }

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
            enemy.Knockback(away * pushSpeed, stunDuration, stunMarker);
            Sfx.Play(Sounds.LegsPush);

            if (damage > 0 && body.TryGetComponent<Health>(out var health))
                health.TakeDamage(damage);
        }
    }
}

using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class MegarachneBrood : MonoBehaviour
    {
        // Provisionales.
        public const int MaxHealth = 2, BiteDamage = 1;
        public const float Speed = 4f, Radius = 0.4f, BiteRange = 0.8f, BiteCooldown = 1.2f;
        const int BiteFrame = 1, BiteFrames = 4, HurtFrames = 2, DeathFrames = 5;
        const float AvoidanceDistance = 0.6f, EscapeAngle = 90f;

        enum State { Move, Bite, Hurt, Death }
        State state;
        float age, nextBite, avoidanceSide;
        MegarachneBoss boss;
        SpriteRenderer sprite;
        Rigidbody2D body;
        CircleCollider2D hit;
        bool bit;
        internal Health Health { get; private set; }
        internal float CurrentSpeed => Speed * (boss != null && boss.IsRage ? MegarachneBoss.RageBroodSpeedMultiplier : 1f);

        internal void Configure(MegarachneBoss owner)
        {
            boss = owner;
            sprite = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            hit = GetComponent<CircleCollider2D>();
            hit.radius = Radius;
            Health = GetComponent<Health>();
            Health.SetMax(MaxHealth, false);
            Health.ResetHealth();
            Health.InvulnerabilityAfterHit = 0f;
            Health.Damaged += Hurt;
            Health.Died += Die;
            Show();
        }

        void OnDestroy()
        {
            if (Health == null) return;
            Health.Damaged -= Hurt;
            Health.Died -= Die;
        }

        void Hurt(int damage) => Play(State.Hurt);

        void Die()
        {
            hit.enabled = false;
            Play(State.Death);
        }

        internal void Remove()
        {
            Health.Kill();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void Play(State animation)
        {
            state = animation;
            age = 0f;
            body.linearVelocity = Vector2.zero;
            Show();
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            float frame = MegarachneBoss.FxFrameSeconds;
            if (state == State.Death)
            {
                if (age >= DeathFrames * frame) Destroy(gameObject);
                else Show();
                return;
            }

            var target = boss.Target;
            Vector2 delta = target != null ? (Vector2)target.position - body.position : Vector2.zero;
            bool close = target != null && delta.sqrMagnitude < BiteRange * BiteRange
                && !MegarachneBoss.SolidAt(body.position, delta.normalized, delta.magnitude);
            if (state == State.Bite)
            {
                if (!bit && age >= BiteFrame * frame)
                {
                    bit = true;
                    if (close) target.GetComponent<Health>()?.TakeDamage(BiteDamage);
                }
                if (age >= BiteFrames * frame) Play(State.Move);
            }
            else if (state == State.Hurt)
            {
                if (age >= HurtFrames * frame) Play(State.Move);
            }
            else if (target != null)
            {
                if (Mathf.Abs(delta.x) > Mathf.Epsilon) sprite.flipX = delta.x < 0f;
                body.linearVelocity = close ? Vector2.zero : Steer(delta.normalized) * CurrentSpeed;
                if (close && Time.fixedTime >= nextBite)
                {
                    nextBite = Time.fixedTime + BiteCooldown;
                    bit = false;
                    Play(State.Bite);
                }
            }
            else body.linearVelocity = Vector2.zero;
            Show();
        }

        Vector2 Steer(Vector2 forward)
        {
            if (!Blocked(forward))
            {
                avoidanceSide = 0f;
                return forward;
            }
            if (avoidanceSide == 0f)
            {
                var left = Turn(forward, EscapeAngle);
                avoidanceSide = Blocked(left) ? -1f : 1f;
            }
            var side = Turn(forward, avoidanceSide * EscapeAngle);
            if (!Blocked(side)) return side;
            var otherSide = -side;
            return Blocked(otherSide) ? Vector2.zero : otherSide;
        }

        bool Blocked(Vector2 direction) => MegarachneBoss.SolidAt(body.position, direction, AvoidanceDistance, Radius);
        static Vector2 Turn(Vector2 direction, float angle) => Quaternion.Euler(0f, 0f, angle) * direction;
        void Show() => boss.Frame(sprite, "Megarachne_Brood_" + state, age, state == State.Move);
    }
}

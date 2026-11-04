using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Retoño de Calamites (piso 3): brota de la tierra (Emerge, sin moverse) y persigue al jugador a saltitos haciendo daño por
    /// contacto. Si su madre muere, se seca (Wither) y se deshace. No suelta biomasa.
    /// </summary>
    public class CalamitesSproutBrain : EnemyBrain
    {
        public enum Phase { Emerge, Chase, Wither }

        [SerializeField, Min(0f)] float witherTime = 0.56f;

        const float DefaultEmerge = 0.36f;

        public Phase Current { get; private set; } = Phase.Emerge;

        /// <summary>La madre que lo echó (null si nació suelto).</summary>
        public CalamitesBrain Mother { get; private set; }

        float phaseUntil;
        bool moving;

        public override string AnimationState => Current switch
        {
            Phase.Emerge => "Emerge",
            Phase.Wither => "Wither",
            _ => moving ? null : "Idle",
        };

        public override bool ShowsHurt => Current == Phase.Chase;

        void Start() => phaseUntil = Time.time + (Animator != null && Animator.LengthOf("Emerge") > 0f ? Animator.LengthOf("Emerge") : DefaultEmerge);

        public void Bind(CalamitesBrain mother) => Mother = mother;

        /// <summary>Se seca y muere (la madre ha muerto). La animación de muerte se ve después de secarse.</summary>
        public void Wither()
        {
            if (Current == Phase.Wither || Health == null || Health.IsDead)
                return;
            Current = Phase.Wither;
            phaseUntil = Time.time + (Animator != null && Animator.LengthOf("Wither") > 0f ? Animator.LengthOf("Wither") : witherTime);
            Health.IsInvulnerable = true;
            // Seco, ya no hace daño al tocarlo.
            if (TryGetComponent<Combat.ContactDamage>(out var contact))
                contact.enabled = false;
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            moving = false;
            switch (Current)
            {
                case Phase.Emerge:
                    if (Time.time >= phaseUntil)
                        Current = Phase.Chase;
                    return Vector2.zero;

                case Phase.Wither:
                    if (Time.time >= phaseUntil)
                    {
                        Health.IsInvulnerable = false;
                        Health.Kill();
                    }
                    return Vector2.zero;
            }

            if (target == null)
                return Vector2.zero;
            AI.Alert();
            float speed = AI.Speed(data);
            moving = true;
            return AI.Separated(AI.DirectionTo(target.position) * speed, speed);
        }
    }
}

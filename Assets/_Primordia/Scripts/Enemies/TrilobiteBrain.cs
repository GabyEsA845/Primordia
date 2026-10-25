using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Trilobite (piso 2, embestida): ronda despacio por la sala. Al verte avisa 0,6 s rascando el suelo con los ojos rojos y
    /// embiste en línea recta muy rápido. Si choca con una pared se da la vuelta y queda aturdido 1 s panza arriba; luego se
    /// endereza. Su cabeza blindada reduce a la mitad el daño mientras el jugador está delante de él (no cuando está panza
    /// arriba).
    /// </summary>
    public class TrilobiteBrain : EnemyBrain
    {
        public enum Phase { Roam, Warn, Charge, Flip, Stunned, Recover, Rest }

        [SerializeField, Min(0f)] float warnTime = 0.6f;
        [SerializeField, Min(0f)] float chargeSpeed = 10f;
        [Tooltip("Segundos como mucho de embestida (si no choca, para).")]
        [SerializeField, Min(0.1f)] float maxChargeTime = 1.2f;
        [SerializeField, Min(0f)] float stunTime = 1f;
        [Tooltip("Pausa después de embestir sin chocar o de enderezarse.")]
        [SerializeField, Min(0f)] float restTime = 0.7f;
        [Tooltip("Velocidad al rondar, respecto a la normal.")]
        [SerializeField, Range(0f, 1f)] float roamFactor = 0.5f;
        [Tooltip("Ángulo a cada lado de la cabeza en el que el blindaje para la mitad del daño.")]
        [SerializeField, Range(0f, 180f)] float armorAngle = 60f;
        [SerializeField, Range(0f, 1f)] float armorFactor = 0.5f;

        // Se considera que chocó si a mitad de embestida apenas avanza (pared, obstáculo, puerta, orilla de una fosa).
        const float StuckSpeed = 1.5f;
        const float StuckGrace = 0.15f;
        const float WallProbe = 0.2f;

        public Phase Current { get; private set; } = Phase.Roam;
        public Vector2 Heading { get; private set; } = Vector2.right;

        float phaseUntil;
        float phaseStart;
        Vector2 roamPoint;
        float roamUntil;
        Transform player;

        public override string AnimationState => Current switch
        {
            Phase.Warn => "Warn",
            Phase.Charge => "Charge",
            Phase.Flip => "Flip",
            Phase.Stunned => "Stunned",
            Phase.Recover => "Recover",
            _ => null,
        };

        public override Vector2? Facing => Current is Phase.Roam or Phase.Rest ? null : Heading;

        public override bool ShowsHurt => Current is Phase.Roam or Phase.Rest;

        /// <summary>True si ahora el blindaje de la cabeza para la mitad del daño que viene de ese punto.</summary>
        public bool Shields(Vector2 from)
        {
            if (Current is Phase.Flip or Phase.Stunned or Phase.Recover)
                return false;
            Vector2 to = from - (Vector2)transform.position;
            return to.sqrMagnitude > 0.0001f && Vector2.Angle(Heading, to) <= armorAngle;
        }

        void OnEnable() => Core.Health.AddDamageModifier(Armor);

        /// <summary>El blindaje: la mitad del daño si el jugador está delante de la cabeza.</summary>
        static int Armor(Core.Health target, int amount)
        {
            if (!target.TryGetComponent<TrilobiteBrain>(out var trilobite))
                return amount;
            if (trilobite.player == null)
            {
                var found = GameObject.FindWithTag("Player");
                trilobite.player = found != null ? found.transform : null;
            }
            return trilobite.player != null && trilobite.Shields(trilobite.player.position)
                ? Core.Health.ScaleDamage(amount, trilobite.armorFactor)
                : amount;
        }

        void Enter(Phase phase, float duration)
        {
            Current = phase;
            phaseStart = Time.time;
            phaseUntil = Time.time + duration;
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            Vector2 position = transform.position;
            switch (Current)
            {
                case Phase.Warn:
                    // Mira al jugador mientras avisa; la dirección se fija al salir.
                    if (target != null)
                        Heading = ((Vector2)target.position - position).normalized;
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Charge, maxChargeTime);
                    return Vector2.zero;

                case Phase.Charge:
                    if (HitsWall(position) || (Time.time - phaseStart > StuckGrace && Body.linearVelocity.magnitude < StuckSpeed))
                    {
                        Enter(Phase.Flip, Mathf.Max(0.05f, Animator != null ? Animator.LengthOf("Flip") : 0.2f));
                        return Vector2.zero;
                    }
                    if (Time.time >= phaseUntil)
                    {
                        Enter(Phase.Rest, restTime);
                        return Vector2.zero;
                    }
                    return Heading * chargeSpeed;

                case Phase.Flip:
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Stunned, stunTime);
                    return Vector2.zero;

                case Phase.Stunned:
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Recover, Mathf.Max(0.05f, Animator != null ? Animator.LengthOf("Recover") : 0.3f));
                    return Vector2.zero;

                case Phase.Recover:
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Rest, restTime);
                    return Vector2.zero;

                case Phase.Rest:
                    if (Time.time >= phaseUntil)
                        Current = Phase.Roam;
                    return Vector2.zero;
            }

            // Rondando: si te ve, avisa; si te conoce pero no te ve, se acerca por el camino; si no, pasea.
            float speed = AI.Speed(data);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.position - position;
                if (toTarget.magnitude <= data.DetectionRange && AI.Sees(target.position))
                {
                    AI.Alert();
                    Heading = toTarget.normalized;
                    Enter(Phase.Warn, warnTime);
                    return Vector2.zero;
                }
                if (AI.IsAlert)
                    return AI.Separated(AI.DirectionTo(target.position) * speed * roamFactor, speed);
            }
            return Wander(position, speed * roamFactor);
        }

        Vector2 Wander(Vector2 position, float speed)
        {
            if (Time.time >= roamUntil || (roamPoint - position).sqrMagnitude < 0.2f)
            {
                roamPoint = position + Random.insideUnitCircle * 3f;
                var grid = NavigationGrid.Current;
                if (grid != null && !grid.HasLineOfSight(position, roamPoint, AI.BodyRadius))
                    roamPoint = position;
                roamUntil = Time.time + Random.Range(1.5f, 3f);
            }
            Vector2 to = roamPoint - position;
            return to.sqrMagnitude > 0.04f ? AI.Separated(to.normalized * speed, speed) : Vector2.zero;
        }

        /// <summary>True si justo delante hay pared u obstáculo (las fosas no cuentan: anda, y las rodea).</summary>
        bool HitsWall(Vector2 position)
        {
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            return grid != null && !grid.HasLineOfSight(position, position + Heading * (AI.BodyRadius + WallProbe), AI.BodyRadius * 0.8f);
        }
    }
}

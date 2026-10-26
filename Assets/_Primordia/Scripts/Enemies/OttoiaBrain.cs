using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Ottoia (piso 2, emboscada): va enterrada, como una onda en la arena que se acerca a ti, y así no recibe daño ni
    /// choca con nada. Al llegar debajo de ti avisa 0,8 s con un círculo rojo, sale, muerde a lo que siga dentro del
    /// círculo y se queda un momento asomada, vulnerable; luego se hunde y descansa antes de volver a acercarse.
    /// Solo hace daño con el mordisco: sus datos no llevan daño por contacto, para que acercarse a golpearla cuando
    /// asoma (también con un mordisco) no se castigue.
    /// </summary>
    public class OttoiaBrain : EnemyBrain
    {
        public enum Phase { Burrowed, Warn, Attack, Exposed, Sink }

        [SerializeField, Min(0f)] float warnTime = 0.8f;
        [Tooltip("Distancia al objetivo a la que deja de acercarse y avisa.")]
        [SerializeField, Min(0f)] float warnDistance = 0.5f;
        [Tooltip("Radio del mordisco: el del círculo del aviso.")]
        [SerializeField, Min(0.1f)] float biteRadius = 0.8f;
        [SerializeField, Min(0)] int biteDamage = 2;
        [Tooltip("Cuadro de la animación de ataque en el que muerde (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int biteFrame = 3;
        [SerializeField, Min(1)] int attackFrames = 6;
        [Tooltip("Segundos que se queda asomada después de morder.")]
        [SerializeField, Min(0f)] float exposedTime = 1f;
        [Tooltip("Segundos enterrada, después de hundirse, antes de poder avisar otra vez.")]
        [SerializeField, Min(0f)] float restTime = 1.2f;

        // Duraciones de las animaciones si no las hay (pruebas): las del README.
        const float DefaultAttack = 0.42f;
        const float DefaultSink = 0.36f;

        public Phase Current { get; private set; } = Phase.Burrowed;

        /// <summary>True mientras está bajo la arena: inmune y sin cuerpo físico.</summary>
        public bool IsBurrowed => Current is Phase.Burrowed or Phase.Warn;

        /// <summary>Se lanza al morder, con lo que mordió (null si no alcanzó a nadie).</summary>
        public event System.Action<Health> Bit;

        Collider2D bodyCollider;
        float phaseStart;
        float phaseUntil;
        float canWarnAt;
        bool bitten;

        public override string AnimationState => Current switch
        {
            Phase.Burrowed => "Burrowed",
            Phase.Warn => "Warn",
            Phase.Attack or Phase.Exposed => "Attack",
            Phase.Sink => "Sink",
            _ => null,
        };

        public override bool ShowsHurt => Current == Phase.Exposed;

        public override bool HoldsAnimationEnd => Current == Phase.Exposed;

        protected override void Awake()
        {
            base.Awake();
            bodyCollider = GetComponent<Collider2D>();
        }

        void Start() => SetBurrowed(true);

        void OnDisable()
        {
            if (Health != null)
                Health.IsInvulnerable = false;
        }

        /// <summary>Enterrada: inmune y sin colisiones (no la tocan proyectiles ni remolinos).</summary>
        void SetBurrowed(bool burrowed)
        {
            if (Health != null)
                Health.IsInvulnerable = burrowed;
            if (bodyCollider != null)
                bodyCollider.enabled = !burrowed;
        }

        void Enter(Phase phase, float duration)
        {
            Current = phase;
            phaseStart = Time.time;
            phaseUntil = Time.time + duration;
        }

        float Length(string state, float fallback)
        {
            float length = Animator != null ? Animator.LengthOf(state) : 0f;
            return length > 0f ? length : fallback;
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            switch (Current)
            {
                case Phase.Warn:
                    if (Time.time >= phaseUntil)
                    {
                        bitten = false;
                        Enter(Phase.Attack, Length("Attack", DefaultAttack));
                    }
                    return Vector2.zero;

                case Phase.Attack:
                    // Sale de la arena y muerde en su cuadro; desde ahí ya se le puede golpear.
                    if (!bitten && Time.time - phaseStart >= (phaseUntil - phaseStart) * biteFrame / attackFrames)
                    {
                        bitten = true;
                        Bite();
                        SetBurrowed(false);
                    }
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Exposed, exposedTime);
                    return Vector2.zero;

                case Phase.Exposed:
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Sink, Length("Sink", DefaultSink));
                    return Vector2.zero;

                case Phase.Sink:
                    if (Time.time >= phaseUntil)
                    {
                        SetBurrowed(true);
                        Current = Phase.Burrowed;
                        canWarnAt = Time.time + restTime;
                    }
                    return Vector2.zero;
            }

            // Enterrada: si te ve (o ya te conoce) va hacia ti por el camino; al llegar debajo, avisa.
            Vector2 position = transform.position;
            float speed = AI.Speed(data);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.position - position;
                if (toTarget.magnitude <= data.DetectionRange && AI.Sees(target.position))
                    AI.Alert();
                if (AI.IsAlert)
                {
                    if (toTarget.magnitude <= warnDistance && Time.time >= canWarnAt)
                    {
                        Enter(Phase.Warn, warnTime);
                        return Vector2.zero;
                    }
                    // Pegada debajo de ti mientras descansa, no te rebasa.
                    return toTarget.magnitude <= warnDistance ? Vector2.zero : AI.DirectionTo(target.position) * speed;
                }
            }
            return Wander(position, speed * 0.5f);
        }

        /// <summary>Muerde a quien siga dentro del círculo (jugador o señuelos).</summary>
        void Bite()
        {
            Health hit = null;
            var mask = LayerMask.GetMask(CombatLayers.Player);
            foreach (var other in Physics2D.OverlapCircleAll(transform.position, biteRadius, mask))
            {
                var health = other.GetComponentInParent<Health>();
                if (health == null || health == Health || health == hit)
                    continue;
                var reflector = other.GetComponentInParent<IHitReflector>();
                if (reflector != null && reflector.TryReflect(Health))
                    continue;
                if (health.TakeDamage(biteDamage))
                    hit = health;
            }
            Bit?.Invoke(hit);
        }
    }
}

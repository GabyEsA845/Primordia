using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Escarabajo bombardero (piso 3, cono de fuego): se acerca despacio y, al tenerte a tiro, se da la vuelta para apuntarte con
    /// el abdomen (Turn), avisa con la punta al rojo (Warn, te sigue casi hasta el final) y suelta por detrás un cono ardiente de
    /// 3 u (Spray) que quema al jugador y a los enemigos que pille y prende gas, charcos y helechos. Después descansa y vuelve a
    /// acercarse. Es inmune al fuego. Su espalda (el lado contrario a su cabeza) es débil: +50 % de daño.
    /// </summary>
    public class BeetleBrain : EnemyBrain
    {
        public enum Phase { Roam, Turn, Warn, Spray, Rest }

        [Tooltip("Se detiene y ataca a esta distancia (algo menos que el largo del cono).")]
        [SerializeField, Min(0.5f)] float sprayRange = 2.8f;
        [SerializeField, Min(0.1f)] float warnTime = 0.6f;
        [Tooltip("Al final del aviso deja de seguirte: la dirección del cono queda fijada estos segundos antes.")]
        [SerializeField, Min(0f)] float lockTime = 0.25f;
        [SerializeField, Min(0.1f)] float sprayTime = 1.2f;
        [SerializeField, Min(0f)] float restTime = 1f;
        [Tooltip("Segundos entre el inicio de un ataque y la posibilidad del siguiente.")]
        [SerializeField, Min(0f)] float cooldown = 4f;
        [SerializeField, Min(0.5f)] float coneLength = 3f;
        [Tooltip("Mitad del ancho del cono en la boca y en el extremo.")]
        [SerializeField, Min(0f)] float coneStartHalfWidth = 0.2f;
        [SerializeField, Min(0f)] float coneEndHalfWidth = 0.8f;
        [Tooltip("Cada cuánto quema y prende lo que toca el cono.")]
        [SerializeField, Min(0.02f)] float tickSeconds = 0.1f;
        [Tooltip("Segundos que arde el fuego que deja en el suelo a lo largo del cono (0 = no deja).")]
        [SerializeField, Min(0f)] float groundFireSeconds = 2f;
        [Tooltip("Punta del abdomen respecto al pie, mirando a la derecha (el dibujo: píxel (3, 14) de 32×32).")]
        [SerializeField] Vector2 tipOffset = new(-0.8125f, 0.44f);
        [Tooltip("Cono ardiente (se crea al soltar el chorro, con el pivote en su borde izquierdo). Opcional.")]
        [SerializeField] GameObject sprayEffect;
        [Tooltip("Ángulo a cada lado de la espalda en el que se hace más daño.")]
        [SerializeField, Range(0f, 180f)] float backAngle = 70f;
        [SerializeField, Min(1f)] float backFactor = 1.5f;

        const float DefaultTurn = 0.21f;
        const float IgniteRadius = 1.3f;
        const float EnemyBurnSeconds = 3f;

        public Phase Current { get; private set; } = Phase.Roam;

        /// <summary>Hacia dónde mira la cabeza.</summary>
        public Vector2 Heading { get; private set; } = Vector2.right;

        /// <summary>Hacia dónde sale el cono (de la punta del abdomen al objetivo).</summary>
        public Vector2 SprayDirection { get; private set; } = Vector2.left;

        public float ConeLength => coneLength;

        float phaseStart;
        float phaseUntil;
        float canSprayAt;
        float nextTick;
        Transform player;
        GameObject cone;

        public void Configure(GameObject effect) => sprayEffect = effect;

        public override string AnimationState => Current switch
        {
            Phase.Turn => "Turn",
            Phase.Warn => "Warn",
            Phase.Spray => "Spray",
            _ => null,
        };

        public override Vector2? Facing => Current == Phase.Roam && !AI.IsAlert ? null : Heading;

        public override bool ShowsHurt => Current is Phase.Roam or Phase.Rest;

        /// <summary>De dónde sale el cono ahora (la punta del abdomen, según hacia dónde mira).</summary>
        public Vector2 Tip => (Vector2)transform.position + new Vector2(Heading.x < 0f ? -tipOffset.x : tipOffset.x, tipOffset.y);

        /// <summary>True si ese punto queda del lado de su espalda (donde pega más fuerte).</summary>
        public bool IsBehind(Vector2 from)
        {
            Vector2 to = from - (Vector2)transform.position;
            return to.sqrMagnitude > 0.0001f && Vector2.Angle(-Heading, to) <= backAngle;
        }

        void OnEnable() => Core.Health.AddDamageModifier(Armor);

        void OnDisable()
        {
            Core.Health.RemoveDamageModifier(Armor);
            EndCone();
        }

        protected override void Awake()
        {
            base.Awake();
            // Arde sin dañarse a sí mismo ni a los suyos.
            if (GetComponent<FireImmune>() == null)
                gameObject.AddComponent<FireImmune>();
        }

        void Start()
        {
            if (Health != null)
                Health.Died += EndCone;
        }

        void OnDestroy()
        {
            if (Health != null)
                Health.Died -= EndCone;
            EndCone();
        }

        static int Armor(Core.Health target, int amount)
        {
            if (!target.TryGetComponent<BeetleBrain>(out var beetle))
                return amount;
            if (beetle.player == null)
            {
                var found = GameObject.FindWithTag("Player");
                beetle.player = found != null ? found.transform : null;
            }
            return beetle.player != null && beetle.IsBehind(beetle.player.position)
                ? Core.Health.ScaleDamage(amount, beetle.backFactor)
                : amount;
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

        void Update()
        {
            // Si lo empujan o lo ciegan a mitad del chorro, se corta.
            if (Current == Phase.Spray && AI.Current is EnemyAI.State.Stunned or EnemyAI.State.Blind)
            {
                EndCone();
                Enter(Phase.Rest, restTime);
            }
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            Vector2 position = transform.position;
            switch (Current)
            {
                case Phase.Turn:
                    if (Time.time >= phaseUntil)
                    {
                        Heading = -SprayDirection;
                        Enter(Phase.Warn, warnTime);
                    }
                    return Vector2.zero;

                case Phase.Warn:
                    // Te sigue con el abdomen hasta casi el final del aviso; entonces fija el cono.
                    if (target != null && Time.time < phaseUntil - lockTime)
                        Aim(target);
                    if (Time.time >= phaseUntil)
                    {
                        StartCone();
                        Enter(Phase.Spray, sprayTime);
                    }
                    return Vector2.zero;

                case Phase.Spray:
                    if (Time.time >= nextTick)
                    {
                        nextTick = Time.time + tickSeconds;
                        BurnTick();
                    }
                    if (Time.time >= phaseUntil)
                    {
                        EndCone();
                        Enter(Phase.Rest, restTime);
                    }
                    return Vector2.zero;

                case Phase.Rest:
                    if (Time.time >= phaseUntil)
                        Current = Phase.Roam;
                    return Vector2.zero;
            }

            // Rondando: si te ve y es hora, se da la vuelta; si te conoce, se acerca hasta el alcance; si no, pasea.
            float speed = AI.Speed(data);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.position - position;
                float distance = toTarget.magnitude;
                bool sees = distance <= data.DetectionRange && AI.Sees(target.position);
                if (sees)
                    AI.Alert();
                if (AI.IsAlert)
                {
                    Heading = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Heading;
                    if (sees && distance <= sprayRange && Time.time >= canSprayAt)
                    {
                        BeginAttack(target);
                        return Vector2.zero;
                    }
                    if (distance <= sprayRange)
                        return Vector2.zero;
                    return AI.Separated(AI.DirectionTo(target.position) * speed, speed);
                }
            }
            return Wander(position, speed * 0.5f);
        }

        /// <summary>Apunta el abdomen al objetivo: el cono sale hacia él y la cabeza mira al lado contrario.</summary>
        void Aim(Transform target)
        {
            Vector2 to = (Vector2)target.position - Tip;
            if (to.sqrMagnitude < 0.0001f)
                return;
            SprayDirection = to.normalized;
            Heading = -SprayDirection;
        }

        void BeginAttack(Transform target)
        {
            canSprayAt = Time.time + cooldown;
            Vector2 oldHeading = Heading;
            Vector2 to = (Vector2)target.position - (Vector2)transform.position;
            SprayDirection = to.normalized;
            // Si ya tiene el abdomen hacia ti (cabeza al otro lado en horizontal), no hace falta girar.
            bool alreadyFacingAway = Mathf.Sign(oldHeading.x) != Mathf.Sign(SprayDirection.x);
            if (alreadyFacingAway)
            {
                Heading = -SprayDirection;
                Enter(Phase.Warn, warnTime);
                return;
            }
            Heading = oldHeading;
            Enter(Phase.Turn, Length("Turn", DefaultTurn));
        }

        void StartCone()
        {
            nextTick = Time.time;
            if (sprayEffect == null)
                return;
            cone = Instantiate(sprayEffect, Tip, Quaternion.Euler(0f, 0f, Mathf.Atan2(SprayDirection.y, SprayDirection.x) * Mathf.Rad2Deg));
        }

        void EndCone()
        {
            if (cone != null)
                Destroy(cone);
            cone = null;
        }

        /// <summary>True si ese punto queda dentro del cono ahora.</summary>
        public bool InCone(Vector2 point)
        {
            Vector2 tip = Tip;
            Vector2 to = point - tip;
            float along = Vector2.Dot(to, SprayDirection);
            if (along < -0.2f || along > coneLength + 0.2f)
                return false;
            float lateral = Mathf.Abs(to.x * SprayDirection.y - to.y * SprayDirection.x);
            float half = Mathf.Lerp(coneStartHalfWidth, coneEndHalfWidth, Mathf.Clamp01(along / coneLength));
            return lateral <= half + 0.25f;
        }

        // Quema a quien está en el cono (jugador por barra, enemigos por quemadura) y prende lo inflamable a lo largo de él.
        void BurnTick()
        {
            Vector2 tip = Tip;
            var hits = Physics2D.OverlapCircleAll(tip + SprayDirection * (coneLength * 0.5f), coneLength * 0.5f + coneEndHalfWidth + 0.3f);
            var done = new System.Collections.Generic.HashSet<Core.Health>();
            foreach (var hit in hits)
            {
                if (hit.isTrigger || !InCone(hit.bounds.center))
                    continue;

                if (hit.GetComponentInParent<PlayerBurn>() is { } burn)
                {
                    if (!burn.IsDodging)
                        burn.Feed(tickSeconds);
                    continue;
                }

                var health = hit.GetComponentInParent<Core.Health>();
                if (health == null || health.IsDead || health == Health || !done.Add(health) || health.GetComponentInParent<Enemy>() == null)
                    continue;
                Combat.Burn.Apply(health, 1, EnemyBurnSeconds, ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null);
            }

            // Prende lo inflamable a lo largo del cono (con margen: un helecho al lado también arde) y deja fuego en el
            // suelo, que se propaga solo a lo que tenga cerca (GroundFire).
            for (float d = 0.3f; d <= coneLength + 0.3f; d += 0.5f)
                Flammables.IgniteNear(tip + SprayDirection * d, IgniteRadius);
            for (float d = 1f; groundFireSeconds > 0f && d <= coneLength; d += 1f)
                GroundFire.Spawn(tip + SprayDirection * d, groundFireSeconds);
        }
    }
}

using System.Collections.Generic;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Libélula gigante (piso 3, voladora): vuela por encima de todo (fosas, lodo, charcos; los proyectiles y el jugador la
    /// atraviesan y es inmune) dando vueltas a tu alrededor. Cada pocos segundos fija una línea de ataque 0,7 s (morro arriba, el ojo
    /// brilla, una línea en el suelo la delata) y cae en picado por ella, rápida, hiriendo al jugador que alcance. Al acabar el
    /// picado se posa 0,8 s: ahí, y solo ahí, se le puede hacer daño. Después despega y vuelve a volar. Si el picado acaba sobre una
    /// fosa o lodo no se posa y sigue volando.
    /// </summary>
    public class DragonflyBrain : EnemyBrain
    {
        public enum Phase { Fly, Warn, Dive, Land, Perch, Takeoff }

        [Header("Vuelo")]
        [Tooltip("A qué distancia da vueltas al jugador.")]
        [SerializeField, Min(1f)] float orbitDistance = 5.5f;
        [SerializeField, Min(0.1f)] float orbitTurnSeconds = 3f;

        [Header("Picado")]
        [SerializeField, Min(0.5f)] float diveRange = 10f;
        [Tooltip("Segundos entre el inicio de un picado y el siguiente.")]
        [SerializeField, Min(0.1f)] float diveCooldown = 4f;
        [SerializeField, Min(0.1f)] float warnTime = 0.7f;
        [Tooltip("Al final del aviso deja de seguirte: la línea queda fijada estos segundos antes.")]
        [SerializeField, Min(0f)] float lockTime = 0.3f;
        [SerializeField, Min(1f)] float diveSpeed = 14f;
        [Tooltip("Cuánto sigue más allá del punto donde estabas.")]
        [SerializeField, Min(0f)] float diveOvershoot = 3f;
        [SerializeField, Min(0.1f)] float diveHitRadius = 0.6f;
        [SerializeField, Min(0)] int diveDamage = 2;
        [SerializeField, Min(0f)] float perchTime = 0.8f;
        [Tooltip("Altura del cuerpo sobre sus pies a la que impacta (el pecho).")]
        [SerializeField] Vector2 chestOffset = new(0f, 0.5f);

        [Header("Efectos")]
        [Tooltip("Tramo de la línea de aviso (8×4 px, bucle): se repite a lo largo del recorrido del picado.")]
        [SerializeField] GameObject aimSegment;

        const float DefaultLand = 0.27f;
        const float DefaultTakeoff = 0.27f;
        const float SegmentLength = 0.5f;
        const float WallProbeStep = 0.25f;

        public Phase Current { get; private set; } = Phase.Fly;

        /// <summary>True mientras está en el aire: inmune y sin chocar con el jugador ni con sus proyectiles.</summary>
        public bool IsAirborne => Current is Phase.Fly or Phase.Warn or Phase.Dive or Phase.Takeoff;

        /// <summary>Dirección del picado (la de la línea de aviso).</summary>
        public Vector2 DiveDirection { get; private set; } = Vector2.right;

        /// <summary>Se lanza al fijar la línea (dirección del picado).</summary>
        public event System.Action<Vector2> Warned;

        /// <summary>Se lanza al salir del picado, con quien recibió el golpe (null si no alcanzó a nadie).</summary>
        public event System.Action<Health> Dived;

        /// <summary>Se lanza al posarse.</summary>
        public event System.Action Landed;

        readonly List<Transform> segments = new();
        Collider2D bodyCollider;
        SwimmerLook look;
        float phaseStart;
        float phaseUntil;
        float nextDiveAt;
        float orbitSign = 1f;
        float nextTurnAt;
        float diveLength;
        float diveTravelled;
        Vector2 diveFrom;
        bool hitDone;
        Health diveVictim;

        public void Configure(GameObject segment) => aimSegment = segment;

        public override string AnimationState => Current switch
        {
            Phase.Warn => "Warn",
            Phase.Dive => "Dive",
            Phase.Land => "Land",
            Phase.Perch => "Perch",
            Phase.Takeoff => "Takeoff",
            _ => "Fly",
        };

        public override Vector2? Facing => Current is Phase.Warn or Phase.Dive ? DiveDirection : null;

        // En el aire es inmune: los golpes (la Peonza, empujes) no la empujan ni la aturden, y así no rompen su picado.
        public override bool CanBePushed => !IsAirborne;

        public override bool ShowsHurt => Current is Phase.Perch or Phase.Land;

        protected override void Awake()
        {
            base.Awake();
            bodyCollider = GetComponent<Collider2D>();
        }

        void Start()
        {
            look = GetComponent<SwimmerLook>();
            nextDiveAt = Time.time + Random.Range(1f, 2.5f);
            nextTurnAt = Time.time + orbitTurnSeconds;
            SetAirborne(true);
        }

        void OnDisable()
        {
            ClearLine();
            if (Health != null)
                Health.IsInvulnerable = false;
        }

        /// <summary>En el aire: inmune y sin chocar con el jugador ni con sus proyectiles; las paredes sí la paran.</summary>
        void SetAirborne(bool airborne)
        {
            if (Health != null)
                Health.IsInvulnerable = airborne;
            if (Body != null)
                Body.excludeLayers = airborne ? LayerMask.GetMask(Combat.CombatLayers.Player, Combat.CombatLayers.PlayerProjectile) : 0;
            if (look != null)
                look.Grounded = !airborne;
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
            Vector2 position = transform.position;
            float speed = AI.Speed(data);

            switch (Current)
            {
                case Phase.Warn:
                    // Te sigue con la línea hasta casi el final del aviso; entonces la fija.
                    if (target != null && Time.time < phaseUntil - lockTime)
                        Aim(target);
                    UpdateLine(position);
                    if (Time.time >= phaseUntil)
                        BeginDive(position);
                    return Vector2.zero;

                case Phase.Dive:
                    return DiveStep(position);

                case Phase.Land:
                    if (Time.time >= phaseUntil)
                        Enter(Phase.Perch, perchTime);
                    return Vector2.zero;

                case Phase.Perch:
                    if (Time.time >= phaseUntil)
                    {
                        // Despega: ya en el aire, inmune, aunque aún subiendo.
                        SetAirborne(true);
                        Enter(Phase.Takeoff, Length("Takeoff", DefaultTakeoff));
                    }
                    return Vector2.zero;

                case Phase.Takeoff:
                    if (Time.time >= phaseUntil)
                    {
                        Current = Phase.Fly;
                        nextDiveAt = Time.time + Mathf.Max(0f, diveCooldown - warnTime);
                    }
                    return Vector2.zero;
            }

            // Volando: da vueltas a tu alrededor; si te ve y es hora, fija la línea.
            if (target == null)
                return Wander(position, speed * 0.5f);

            Vector2 toTarget = (Vector2)target.position - position;
            float distance = toTarget.magnitude;
            bool sees = distance <= data.DetectionRange && AI.Sees(target.position);
            if (sees)
                AI.Alert();
            if (!AI.IsAlert)
                return Wander(position, speed * 0.5f);

            if (sees && Time.time >= nextDiveAt && distance <= diveRange)
            {
                nextDiveAt = Time.time + diveCooldown;
                Aim(target);
                Enter(Phase.Warn, warnTime);
                Warned?.Invoke(DiveDirection);
                return Vector2.zero;
            }

            if (Time.time >= nextTurnAt)
            {
                nextTurnAt = Time.time + orbitTurnSeconds;
                orbitSign = -orbitSign;
            }
            Vector2 radial = toTarget.normalized * Mathf.Clamp(distance - orbitDistance, -1f, 1f);
            Vector2 tangent = new Vector2(-toTarget.y, toTarget.x).normalized * orbitSign;
            Vector2 desired = (radial + tangent * 0.8f).normalized * speed;
            return AI.Separated(desired, speed);
        }

        void Aim(Transform target)
        {
            Vector2 to = (Vector2)target.position - ((Vector2)transform.position);
            if (to.sqrMagnitude < 0.0001f)
                return;
            DiveDirection = to.normalized;
        }

        // Hasta dónde llega el picado: al punto de la línea más lejano sin pared en medio.
        float ClearLength(Vector2 from, Vector2 direction, float wanted)
        {
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            if (grid == null)
                return wanted;
            float length = 0f;
            while (length + WallProbeStep <= wanted)
            {
                if (!grid.HasLineOfSight(from, from + direction * (length + WallProbeStep), AI.BodyRadius * 0.8f))
                    break;
                length += WallProbeStep;
            }
            return length;
        }

        float WantedLength(Vector2 position)
        {
            var player = GameObject.FindWithTag("Player");
            float toPlayer = player != null ? Vector2.Distance(position, player.transform.position) : 4f;
            return toPlayer + diveOvershoot;
        }

        void UpdateLine(Vector2 position)
        {
            if (aimSegment == null)
                return;
            float length = ClearLength(position, DiveDirection, WantedLength(position));
            int count = Mathf.Max(0, Mathf.FloorToInt(length / SegmentLength));
            while (segments.Count < count)
                segments.Add(Instantiate(aimSegment).transform);
            while (segments.Count > count)
            {
                Destroy(segments[^1].gameObject);
                segments.RemoveAt(segments.Count - 1);
            }
            float angle = Mathf.Atan2(DiveDirection.y, DiveDirection.x) * Mathf.Rad2Deg;
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].position = position + DiveDirection * (SegmentLength * (i + 0.5f));
                segments[i].rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        void ClearLine()
        {
            foreach (var segment in segments)
            {
                if (segment != null)
                    Destroy(segment.gameObject);
            }
            segments.Clear();
        }

        void BeginDive(Vector2 position)
        {
            ClearLine();
            diveFrom = position;
            diveLength = ClearLength(position, DiveDirection, WantedLength(position));
            diveTravelled = 0f;
            hitDone = false;
            diveVictim = null;
            Enter(Phase.Dive, diveLength / diveSpeed + 0.5f);
        }

        Vector2 DiveStep(Vector2 position)
        {
            diveTravelled = Vector2.Distance(diveFrom, position);
            if (!hitDone)
                TryHit(position);

            // Termina al recorrer la línea o si algo la frena antes (la pared) pasado un momento.
            bool arrived = diveTravelled >= diveLength - 0.1f || Time.time >= phaseUntil;
            if (!arrived)
                return DiveDirection * diveSpeed;

            Dived?.Invoke(diveVictim);
            if (Pits.IsHole(position))
            {
                // Sobre una fosa o lodo no puede posarse: sigue volando.
                SetAirborne(true);
                Current = Phase.Fly;
                return Vector2.zero;
            }

            SetAirborne(false);
            Enter(Phase.Land, Length("Land", DefaultLand));
            Landed?.Invoke();
            return Vector2.zero;
        }

        // Hiere al jugador que alcance con el pecho (una vez por picado; esquivar lo evita).
        void TryHit(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position + chestOffset, diveHitRadius, LayerMask.GetMask(Combat.CombatLayers.Player)))
            {
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                hitDone = true;
                if (hit.GetComponentInParent<Primordia.Player.PlayerController>() is { IsDodging: true })
                    return;
                diveVictim = health;
                health.TakeDamage(diveDamage);
                return;
            }
        }
    }
}

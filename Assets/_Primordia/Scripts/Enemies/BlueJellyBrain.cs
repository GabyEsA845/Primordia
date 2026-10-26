using Primordia.Combat;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Medusa azul (piso 2, anillos): nada despacio a impulsos, latiendo, sin acercarse demasiado ni alejarse mucho del
    /// jugador. Cada 3 s, si te ve, se llena (aviso con las 8 cargas encendidas) y al contraerse suelta un anillo de 8
    /// proyectiles lentos con un hueco por donde pasar, que gira de un anillo al siguiente. Pasa por encima de las fosas.
    /// </summary>
    [RequireComponent(typeof(Weapon))]
    public class BlueJellyBrain : EnemyBrain
    {
        public enum Phase { Drift, Warn, Shoot }

        [SerializeField, Min(0.5f)] float shotInterval = 3f;
        [Tooltip("Proyectiles de cada anillo.")]
        [SerializeField, Min(1)] int ringCount = 8;
        [Tooltip("Grados del hueco por donde pasar; los proyectiles se reparten por el resto.")]
        [SerializeField, Range(0f, 180f)] float gapAngle = 72f;
        [Tooltip("Grados que gira el hueco de un anillo al siguiente.")]
        [SerializeField] float gapStep = 50f;
        [Tooltip("Segundos entre dos impulsos al nadar.")]
        [SerializeField, Min(0.2f)] float pulseInterval = 0.96f;
        [Tooltip("Distancia al jugador que intenta mantener: más lejos se acerca, más cerca se aparta.")]
        [SerializeField, Min(0f)] float nearDistance = 3.5f;
        [SerializeField, Min(0f)] float farDistance = 6f;

        // Cada impulso arranca rápido (como mucho al doble de la velocidad normal) y se frena hasta pararse.
        const float PulseBoost = 2f;

        public Phase Current { get; private set; } = Phase.Drift;

        /// <summary>Ángulo (grados) del centro del hueco del próximo anillo.</summary>
        public float GapAngle => ringAngle;

        /// <summary>Se lanza al soltar cada anillo.</summary>
        public event System.Action RingFired;

        Weapon weapon;
        float phaseUntil;
        float nextShot;
        float ringAngle;
        float pulseStart = float.NegativeInfinity;
        Vector2 pulseDirection;

        public override string AnimationState => Current switch
        {
            Phase.Warn => "Warn",
            Phase.Shoot => "Shoot",
            _ => null,
        };

        public override bool ShowsHurt => Current == Phase.Drift;

        protected override void Awake()
        {
            base.Awake();
            weapon = GetComponent<Weapon>();
        }

        void Start()
        {
            ringAngle = Random.Range(0f, 360f);
            // Las medusas de una misma sala no disparan a la vez.
            nextShot = Time.time + Random.Range(0.6f, 1.6f);
        }

        void Enter(Phase phase, float duration)
        {
            Current = phase;
            phaseUntil = Time.time + duration;
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            switch (Current)
            {
                case Phase.Warn:
                    if (Time.time >= phaseUntil)
                    {
                        // El anillo sale en el primer cuadro de Shoot, al contraerse.
                        FireRing();
                        Enter(Phase.Shoot, Mathf.Max(0.05f, Animator != null ? Animator.LengthOf("Shoot") : 0.32f));
                        nextShot = Time.time + shotInterval;
                    }
                    return Vector2.zero;

                case Phase.Shoot:
                    if (Time.time >= phaseUntil)
                        Current = Phase.Drift;
                    return Vector2.zero;
            }

            Vector2 position = transform.position;
            bool sees = false;
            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.position - position;
                sees = toTarget.magnitude <= data.DetectionRange && AI.Sees(target.position);
                if (sees)
                    AI.Alert();
            }

            if (sees && Time.time >= nextShot)
            {
                Enter(Phase.Warn, Mathf.Max(0.05f, Animator != null ? Animator.LengthOf("Warn") : 0.6f));
                return Vector2.zero;
            }

            return Swim(position, target, AI.Speed(data));
        }

        /// <summary>Nada a impulsos: cada uno arranca rápido y se frena, como un latido.</summary>
        Vector2 Swim(Vector2 position, Transform target, float speed)
        {
            if (Time.time >= pulseStart + pulseInterval)
            {
                pulseStart = Time.time;
                pulseDirection = ChooseDirection(position, target);
            }
            float t = Mathf.Clamp01((Time.time - pulseStart) / pulseInterval);
            // 3·(1 − t)²: de media, la velocidad normal.
            float strength = 3f * (1f - t) * (1f - t);
            float max = speed * PulseBoost;
            return pulseDirection.sqrMagnitude > 0f ? AI.Separated(pulseDirection * speed * strength, max) : Vector2.zero;
        }

        Vector2 ChooseDirection(Vector2 position, Transform target)
        {
            if (target != null && AI.IsAlert)
            {
                Vector2 toTarget = (Vector2)target.position - position;
                float distance = toTarget.magnitude;
                if (distance > farDistance)
                    return AI.DirectionTo(target.position);
                Vector2 away = distance > 0.01f ? -toTarget / distance : Random.insideUnitCircle.normalized;
                if (distance < nearDistance)
                    return Free(position, away) ? away : Vector2.Perpendicular(away);
                // A buena distancia, se mueve de lado alrededor del jugador.
                Vector2 side = Vector2.Perpendicular(away) * (Random.value < 0.5f ? 1f : -1f);
                return Free(position, side) ? side : -side;
            }

            Vector2 wander = Random.insideUnitCircle.normalized;
            return Free(position, wander) ? wander : Vector2.zero;
        }

        /// <summary>True si puede nadar un poco en esa dirección sin chocar con una pared.</summary>
        bool Free(Vector2 position, Vector2 direction)
        {
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            return grid == null || grid.HasLineOfSight(position, position + direction * 1.5f, AI.BodyRadius);
        }

        /// <summary>Anillo de proyectiles repartidos fuera del hueco; luego el hueco gira.</summary>
        void FireRing()
        {
            if (weapon == null || weapon.Attack == null)
                return;
            float step = (360f - gapAngle) / ringCount;
            // El primero sale justo después del hueco: el hueco queda centrado en ringAngle.
            float first = ringAngle + gapAngle / 2f + step / 2f;
            for (int i = 0; i < ringCount; i++)
            {
                float angle = (first + i * step) * Mathf.Deg2Rad;
                weapon.Fire(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), weapon.Attack);
            }
            ringAngle = Mathf.Repeat(ringAngle + gapStep, 360f);
            RingFired?.Invoke();
        }
    }
}

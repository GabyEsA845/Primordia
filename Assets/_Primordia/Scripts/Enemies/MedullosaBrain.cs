using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Medullosa (piso 3, mortero): helecho con semillas, inmóvil. Al verte echa atrás su fronda-catapulta (Aim), marca en el suelo
    /// el punto donde estás (se queda fija, para leerla y apartarse) y lanza una vaina en arco (Shoot) que revienta ahí: daño en un
    /// radio pequeño y un charco de veneno. Entre tiros descansa unos segundos.
    /// </summary>
    public class MedullosaBrain : EnemyBrain
    {
        public enum Phase { Idle, Aim, Shoot }

        [SerializeField, Min(0.5f)] float range = 10f;
        [Tooltip("Segundos entre el inicio de un tiro y el siguiente.")]
        [SerializeField, Min(0.1f)] float cooldown = 4f;
        [Tooltip("Cuadro de Shoot en el que sale la vaina (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int shootFrame = 1;
        [SerializeField, Min(1)] int shootFrames = 4;
        [Tooltip("Lo que tarda la vaina en llegar a la marca.")]
        [SerializeField, Min(0.1f)] float flight = 1.1f;
        [SerializeField, Min(0f)] float arc = 2.5f;
        [SerializeField, Min(0)] int damage = 1;
        [SerializeField, Min(0.1f)] float burstRadius = 0.9f;
        [SerializeField, Min(0.1f)] float puddleRadius = 1.1f;
        [SerializeField, Min(0f)] float puddleSeconds = 6f;
        [Tooltip("De dónde sale la vaina respecto a sus pies (la punta de la fronda).")]
        [SerializeField] Vector2 launchOffset = new(0.5f, 1.3f);

        [Header("Efectos")]
        [SerializeField] MedullosaSeed seedPrefab;
        [Tooltip("Aviso en el suelo donde caerá (bucle hasta que revienta).")]
        [SerializeField] GameObject markerPrefab;

        const float DefaultAim = 0.6f;
        const float DefaultShoot = 0.28f;

        public Phase Current { get; private set; } = Phase.Idle;

        /// <summary>Se lanza al soltar la vaina, con el punto marcado.</summary>
        public event System.Action<Vector2> Fired;

        float phaseStart;
        float phaseUntil;
        float nextShotAt;
        bool fired;
        Vector2 aimPoint;

        public void Configure(MedullosaSeed seed, GameObject marker)
        {
            seedPrefab = seed;
            markerPrefab = marker;
        }

        public override string AnimationState => Current switch
        {
            Phase.Aim => "Aim",
            Phase.Shoot => "Shoot",
            _ => "Idle",
        };

        // Una planta no se voltea.
        public override Vector2? Facing => Vector2.right;

        public override bool ShowsHurt => Current == Phase.Idle;

        protected override void Awake()
        {
            base.Awake();
            if (Body != null)
                Body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        void Start() => nextShotAt = Time.time + Random.Range(0.5f, 1.5f);

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
                case Phase.Aim:
                    if (target != null)
                        aimPoint = target.position;
                    if (Time.time >= phaseUntil)
                    {
                        fired = false;
                        Enter(Phase.Shoot, Length("Shoot", DefaultShoot));
                    }
                    return Vector2.zero;

                case Phase.Shoot:
                    if (!fired && Time.time - phaseStart >= (phaseUntil - phaseStart) * shootFrame / shootFrames)
                    {
                        fired = true;
                        Fire();
                    }
                    if (Time.time >= phaseUntil)
                        Current = Phase.Idle;
                    return Vector2.zero;
            }

            if (target != null && Time.time >= nextShotAt)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                if (distance <= Mathf.Min(range, data.DetectionRange) && AI.Sees(target.position))
                {
                    AI.Alert();
                    nextShotAt = Time.time + cooldown;
                    aimPoint = target.position;
                    Enter(Phase.Aim, Length("Aim", DefaultAim));
                }
            }
            return Vector2.zero;
        }

        void Fire()
        {
            Fired?.Invoke(aimPoint);
            if (seedPrefab == null)
                return;
            Vector2 feet = transform.position;
            GameObject marker = markerPrefab != null ? Instantiate(markerPrefab, aimPoint, Quaternion.identity) : null;
            var seed = Instantiate(seedPrefab);
            seed.Launch(feet, aimPoint, flight, arc, damage, burstRadius, puddleSeconds, puddleRadius, marker, launchOffset);
        }
    }
}

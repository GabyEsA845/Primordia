using System;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Lluvia de proyectiles de la ameba gigante en su fase final: varios tramos seguidos, cada uno con su mezcla de
    /// patrones, cada vez más difíciles. Los patrones son distintos de los del virus (que hace una espiral en un solo
    /// sentido y ráfagas en círculo cerrado):
    /// <list type="bullet">
    /// <item>Latidos: anillos densos con un hueco por donde pasar, que gira de un anillo al siguiente.</item>
    /// <item>Remolino tejido: brazos que giran en los dos sentidos a la vez y forman una red de rombos.</item>
    /// <item>Abanico: unos pocos proyectiles rápidos apuntados al jugador.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(Weapon))]
    public class AmoebaBarrage : MonoBehaviour
    {
        [Serializable]
        public class Segment
        {
            [Min(0.1f)] public float duration = 10f;

            [Header("Latidos (anillos con hueco)")]
            [Tooltip("Segundos entre anillos. 0 = sin anillos.")]
            [Min(0f)] public float ringInterval;
            [Min(1)] public int ringCount = 28;
            [Tooltip("Proyectiles que faltan seguidos en cada anillo: el hueco por donde pasar.")]
            [Min(0)] public int ringGap = 4;
            [Tooltip("Grados que gira el hueco de un anillo al siguiente.")]
            public float ringGapStep = 40f;

            [Header("Remolino tejido (brazos en los dos sentidos)")]
            [Tooltip("Brazos que giran hacia un lado; otros tantos giran hacia el otro. 0 = sin remolino.")]
            [Min(0)] public int spiralArms;
            [Min(0.02f)] public float spiralInterval = 0.15f;
            [Tooltip("Grados que giran los brazos entre un disparo y el siguiente.")]
            public float spiralStep = 9f;

            [Header("Abanico apuntado")]
            [Tooltip("Segundos entre abanicos. 0 = sin abanicos.")]
            [Min(0f)] public float fanInterval;
            [Min(1)] public int fanCount = 5;
            [Tooltip("Grados entre el primer y el último proyectil del abanico.")]
            [Min(0f)] public float fanSpread = 40f;
        }

        [SerializeField] AttackData ringAttack;
        [SerializeField] AttackData spiralAttack;
        [SerializeField] AttackData fanAttack;
        [SerializeField] Segment[] segments =
        {
            new() { ringInterval = 1f, ringCount = 30, ringGap = 4, ringGapStep = 37f },
            new() { spiralArms = 3, spiralInterval = 0.14f, spiralStep = 9f },
            new()
            {
                ringInterval = 1.7f, ringCount = 26, ringGap = 4, ringGapStep = -53f, spiralArms = 2, spiralInterval = 0.2f,
                spiralStep = 11f, fanInterval = 1.9f, fanCount = 5, fanSpread = 44f,
            },
        };

        Weapon weapon;
        Transform target;
        float startTime;
        float nextRing;
        float nextSpiral;
        float nextFan;
        float ringAngle;
        float spiralAngle;

        public bool Running { get; private set; }

        /// <summary>Tramo actual (desde 0); -1 si no está disparando.</summary>
        public int SegmentIndex { get; private set; } = -1;

        public float Duration
        {
            get
            {
                float total = 0f;
                foreach (var segment in segments)
                    total += segment.duration;
                return total;
            }
        }

        public float Elapsed => Running ? Time.time - startTime : 0f;

        public int SegmentCount => segments.Length;

        /// <summary>Se lanza al empezar cada tramo, con su índice (la ameba cambia de animación).</summary>
        public event Action<int> SegmentStarted;

        /// <summary>Se lanza al acabar el último tramo.</summary>
        public event Action Finished;

        void Awake() => weapon = GetComponent<Weapon>();

        internal void Configure(AttackData ring, AttackData spiral, AttackData fan, Segment[] newSegments = null)
        {
            ringAttack = ring;
            spiralAttack = spiral;
            fanAttack = fan;
            if (newSegments != null)
                segments = newSegments;
        }

        /// <param name="newTarget">A quién apuntan los abanicos (el jugador).</param>
        public void Begin(Transform newTarget)
        {
            target = newTarget;
            startTime = Time.time;
            Running = true;
            SegmentIndex = -1;
            ringAngle = UnityEngine.Random.Range(0f, 360f);
            spiralAngle = 0f;
        }

        public void Stop()
        {
            Running = false;
            SegmentIndex = -1;
        }

        void Update()
        {
            if (!Running || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing))
                return;

            float elapsed = Time.time - startTime;
            int index = IndexAt(elapsed, out float segmentStart);
            if (index < 0)
            {
                Stop();
                Finished?.Invoke();
                return;
            }

            if (index != SegmentIndex)
            {
                SegmentIndex = index;
                // Cada tramo empieza con un respiro corto antes de su primer anillo y su primer abanico.
                float now = Time.time;
                nextRing = now + 0.4f;
                nextSpiral = now;
                nextFan = now + 0.8f;
                SegmentStarted?.Invoke(index);
            }

            var segment = segments[index];
            if (segment.ringInterval > 0f && Time.time >= nextRing)
            {
                FireRing(segment);
                nextRing = Time.time + segment.ringInterval;
            }

            if (segment.spiralArms > 0 && Time.time >= nextSpiral)
            {
                FireSpiral(segment);
                nextSpiral = Time.time + segment.spiralInterval;
            }

            if (segment.fanInterval > 0f && Time.time >= nextFan)
            {
                FireFan(segment);
                nextFan = Time.time + segment.fanInterval;
            }
        }

        int IndexAt(float elapsed, out float segmentStart)
        {
            segmentStart = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                if (elapsed < segmentStart + segments[i].duration)
                    return i;
                segmentStart += segments[i].duration;
            }
            return -1;
        }

        void FireRing(Segment segment)
        {
            float step = 360f / segment.ringCount;
            for (int i = segment.ringGap; i < segment.ringCount; i++)
                weapon.Fire(Direction(ringAngle + i * step), ringAttack);
            ringAngle += segment.ringGapStep;
        }

        void FireSpiral(Segment segment)
        {
            float step = 360f / segment.spiralArms;
            for (int arm = 0; arm < segment.spiralArms; arm++)
            {
                // Unos brazos giran hacia un lado y otros hacia el otro: sus proyectiles se cruzan formando rombos.
                weapon.Fire(Direction(spiralAngle + arm * step), spiralAttack);
                weapon.Fire(Direction(-spiralAngle + arm * step + step / 2f), spiralAttack);
            }
            spiralAngle += segment.spiralStep;
        }

        void FireFan(Segment segment)
        {
            if (target == null)
                return;

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float aim = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float step = segment.fanCount > 1 ? segment.fanSpread / (segment.fanCount - 1) : 0f;
            float first = aim - segment.fanSpread / 2f;
            for (int i = 0; i < segment.fanCount; i++)
                weapon.Fire(Direction(segment.fanCount > 1 ? first + i * step : aim), fanAttack);
        }

        static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}

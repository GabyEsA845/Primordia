using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Pez de un cardumen de Pikaia (piso 2): salen juntos (la entrada del piso los crea en grupo) y al nacer se reúnen en
    /// un mismo <see cref="School"/> con los que tienen cerca. Sin combate nadan juntos. Al verte te rodean, cada uno en
    /// su puesto de un corro que gira a tu alrededor, y por turnos uno se para un instante apuntándote y hace una pasada
    /// rápida que te atraviesa hasta el otro lado del corro; luego vuelve a su puesto. Pasan por encima de las fosas.
    /// </summary>
    public class PikaiaBrain : EnemyBrain
    {
        public enum Phase { Swim, Aim, Dash, Return }

        /// <summary>El cardumen: sus peces y de quién es el turno de la próxima pasada.</summary>
        public class School
        {
            readonly List<PikaiaBrain> members = new();
            int turn;
            float nextDash;

            public IReadOnlyList<PikaiaBrain> Members => members;

            /// <summary>Ángulo (grados) del corro, que gira alrededor del objetivo.</summary>
            public float Angle { get; private set; } = Random.Range(0f, 360f);

            float angleTime = -1f;

            internal void Add(PikaiaBrain fish)
            {
                if (!members.Contains(fish))
                    members.Add(fish);
            }

            /// <summary>Quita los peces muertos o destruidos.</summary>
            internal void Prune() => members.RemoveAll(fish => fish == null || fish.Health == null || fish.Health.IsDead);

            /// <summary>Puesto de ese pez en el corro (los vivos se reparten la vuelta entera).</summary>
            internal int SlotOf(PikaiaBrain fish) => members.IndexOf(fish);

            internal void Turn(float degreesPerSecond)
            {
                // Lo hace girar el primer pez que piensa en cada cuadro.
                if (Mathf.Approximately(angleTime, Time.time))
                    return;
                if (angleTime >= 0f)
                    Angle = Mathf.Repeat(Angle + degreesPerSecond * (Time.time - angleTime), 360f);
                angleTime = Time.time;
            }

            /// <summary>
            /// True si a este pez le toca ya hacer la pasada: el primero listo a partir del turno, cuando ha pasado el
            /// tiempo entre pasadas. Al tomarlo, el turno pasa al siguiente.
            /// </summary>
            internal bool TryTakeTurn(PikaiaBrain fish, float interval)
            {
                if (Time.time < nextDash || members.Count == 0)
                    return false;
                for (int k = 0; k < members.Count; k++)
                {
                    int index = (turn + k) % members.Count;
                    if (!members[index].ReadyToDash)
                        continue;
                    if (members[index] != fish)
                        return false;
                    turn = index + 1;
                    nextDash = Time.time + interval;
                    return true;
                }
                return false;
            }
        }

        [Tooltip("Distancia al objetivo de los puestos del corro.")]
        [SerializeField, Min(0.5f)] float ringRadius = 3.5f;
        [Tooltip("Grados por segundo que gira el corro.")]
        [SerializeField] float ringSpin = 30f;
        [Tooltip("Segundos entre una pasada del cardumen y la siguiente.")]
        [SerializeField, Min(0.1f)] float dashInterval = 1.1f;
        [Tooltip("Segundos que se para apuntando antes de la pasada (el aviso).")]
        [SerializeField, Min(0f)] float aimTime = 0.35f;
        [SerializeField, Min(0f)] float dashSpeed = 11f;
        [Tooltip("Distancia que recorre la pasada, más allá del objetivo: hasta el otro lado del corro.")]
        [SerializeField, Min(0.5f)] float dashOvershoot = 3f;
        [Tooltip("Distancia a su puesto a la que se considera colocado (y listo para su pasada).")]
        [SerializeField, Min(0.1f)] float slotTolerance = 1.2f;

        // Radio en el que se juntan al nacer los peces de un mismo cardumen.
        const float GatherRadius = 2.5f;
        // Los peces de un cardumen se buscan entre todos los que hay (pocos por sala).
        static readonly List<PikaiaBrain> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public Phase Current { get; private set; } = Phase.Swim;

        public School Shoal { get; private set; }

        /// <summary>Hacia dónde va la pasada.</summary>
        public Vector2 DashDirection { get; private set; } = Vector2.right;

        Vector2 slotPoint;
        bool hasSlot;
        float phaseUntil;

        bool ReadyToDash => Current == Phase.Swim && hasSlot && Health != null && !Health.IsDead
                            && ((Vector2)transform.position - slotPoint).magnitude <= slotTolerance;

        public override string AnimationState => Current == Phase.Dash ? "Dash" : "Swim";

        public override Vector2? Facing => Current is Phase.Aim or Phase.Dash ? DashDirection : null;

        public override bool ShowsHurt => Current is Phase.Swim or Phase.Return;

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        void Start() => JoinShoal();

        /// <summary>Se une al cardumen de algún pez cercano o, si no hay, empieza uno.</summary>
        void JoinShoal()
        {
            foreach (var other in all)
            {
                if (other != this && other.Shoal != null
                    && Vector2.Distance(other.transform.position, transform.position) <= GatherRadius)
                {
                    Shoal = other.Shoal;
                    break;
                }
            }
            Shoal ??= new School();
            Shoal.Add(this);
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            if (Shoal == null)
                JoinShoal();
            Shoal.Prune();
            Vector2 position = transform.position;
            float speed = AI.Speed(data);

            switch (Current)
            {
                case Phase.Aim:
                    if (target != null)
                        DashDirection = ((Vector2)target.position - position).normalized;
                    if (Time.time >= phaseUntil)
                    {
                        float distance = target != null ? Vector2.Distance(target.position, position) : ringRadius;
                        Current = Phase.Dash;
                        phaseUntil = Time.time + (distance + dashOvershoot) / Mathf.Max(0.1f, dashSpeed);
                        Sfx.Play(Sounds.PikaiaDash);
                    }
                    return Vector2.zero;

                case Phase.Dash:
                    if (Time.time >= phaseUntil || Blocked(position))
                        Current = Phase.Return;
                    return DashDirection * dashSpeed;
            }

            bool sees = false;
            if (target != null)
            {
                sees = Vector2.Distance(target.position, position) <= data.DetectionRange && AI.Sees(target.position);
                if (sees)
                    AI.Alert();
            }

            if (target == null || !AI.IsAlert)
            {
                // Sin combate, nadan juntos.
                hasSlot = false;
                Vector2 center = ShoalCenter();
                Vector2 wander = Wander(position, speed * 0.5f);
                Vector2 together = (center - position).magnitude > 1.2f ? (center - position).normalized * speed * 0.5f : Vector2.zero;
                return Vector2.ClampMagnitude(wander + together, speed * 0.6f);
            }

            // En el corro: cada uno va a su puesto, que gira alrededor del objetivo.
            Shoal.Turn(ringSpin);
            int count = Mathf.Max(1, Shoal.Members.Count);
            int slot = Mathf.Max(0, Shoal.SlotOf(this));
            float angle = (Shoal.Angle + 360f * slot / count) * Mathf.Deg2Rad;
            slotPoint = (Vector2)target.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
            hasSlot = true;

            if (Current == Phase.Return && (slotPoint - position).magnitude <= slotTolerance)
                Current = Phase.Swim;

            if (Current == Phase.Swim && sees && Shoal.TryTakeTurn(this, dashInterval))
            {
                Current = Phase.Aim;
                DashDirection = ((Vector2)target.position - position).normalized;
                phaseUntil = Time.time + aimTime;
                return Vector2.zero;
            }

            Vector2 to = slotPoint - position;
            if (to.magnitude < 0.15f)
                return Vector2.zero;
            // Por el camino si el puesto queda detrás de una pared; si no, derecho (y más despacio al llegar).
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            Vector2 direction = grid == null || grid.HasLineOfSight(position, slotPoint, AI.BodyRadius)
                ? to.normalized : AI.DirectionTo(slotPoint);
            return AI.Separated(direction * speed * Mathf.Clamp01(to.magnitude / 1.5f + 0.3f), speed);
        }

        Vector2 ShoalCenter()
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var fish in Shoal.Members)
            {
                sum += (Vector2)fish.transform.position;
                count++;
            }
            return count > 0 ? sum / count : (Vector2)transform.position;
        }

        /// <summary>True si justo delante de la pasada hay una pared.</summary>
        bool Blocked(Vector2 position)
        {
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            return grid != null && !grid.HasLineOfSight(position, position + DashDirection * (AI.BodyRadius + 0.2f), AI.BodyRadius * 0.8f);
        }
    }
}

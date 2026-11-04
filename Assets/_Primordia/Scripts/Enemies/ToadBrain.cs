using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Sapo de fango (piso 3, lodo): salta hacia ti y escupe bolas de lodo en arco que dejan un charco que ralentiza. Si te tiene
    /// cerca, o le queda poca vida, se hincha y revienta (daño alrededor y charco; el sapo muere). Busca el lodo profundo cercano y
    /// desde ahí ataca: dentro del lodo, al recibir daño se zambulle (inmune, nada unos segundos) y vuelve a asomar. Fuera del lodo
    /// es un sapo normal. Sale solo en salas con lodo profundo (<see cref="FloorConfig.EnemyEntry"/>).
    /// </summary>
    public class ToadBrain : EnemyBrain
    {
        public enum Phase { Surfaced, Spit, Inflate, Rush, Dive, Swim, Emerge }

        [Header("Escupitajo")]
        [SerializeField, Min(0.5f)] float spitRange = 8f;
        [SerializeField, Min(0.5f)] float spitMinRange = 2.2f;
        [SerializeField, Min(0.1f)] float spitCooldown = 3f;
        [Tooltip("Cuadro de Spit en el que sale la bola (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int spitFrame = 3;
        [SerializeField, Min(1)] int spitFrames = 5;
        [SerializeField, Min(0.1f)] float ballFlight = 0.8f;
        [SerializeField, Min(0f)] float ballArc = 1.5f;
        [SerializeField, Min(0)] int ballDamage = 1;
        [SerializeField, Min(0.1f)] float ballRadius = 0.7f;
        [SerializeField, Min(0f)] float puddleSeconds = 8f;
        [Tooltip("De dónde sale la bola respecto a sus pies, mirando a la derecha.")]
        [SerializeField] Vector2 mouthOffset = new(0.8f, 0.55f);

        [Header("Reventón")]
        [Tooltip("Si te acercas a menos de esto se hincha.")]
        [SerializeField, Min(0.5f)] float inflateDistance = 1.8f;
        [SerializeField, Min(0.1f)] float inflateTime = 0.5f;
        [Tooltip("Con esta fracción de vida o menos se hincha y corre hacia ti.")]
        [SerializeField, Range(0f, 1f)] float desperateHealth = 0.3f;
        [SerializeField, Min(0.1f)] float rushTime = 2f;
        [SerializeField, Min(1f)] float rushSpeedFactor = 1.6f;
        [SerializeField, Min(0.1f)] float burstRadius = 1.6f;
        [SerializeField, Min(0)] int burstDamage = 1;

        [Header("Lodo")]
        [Tooltip("A qué distancia busca el lodo profundo para meterse.")]
        [SerializeField, Min(0f)] float mudSearchRadius = 8f;
        [SerializeField, Min(0.1f)] float swimTime = 2f;
        [SerializeField, Min(0.1f)] float swimSpeed = 4f;

        [Header("Efectos")]
        [SerializeField] ToadMudBall ballPrefab;
        [SerializeField] GameObject burstEffect;
        [SerializeField] Sprite[] puddleFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float puddleFrameSeconds = 0.26f;

        const float DefaultSpit = 0.45f;
        const float DefaultDive = 0.27f;

        public Phase Current { get; private set; } = Phase.Surfaced;

        /// <summary>True mientras va bajo el lodo: inmune y sin cuerpo físico.</summary>
        public bool IsSubmerged => Current is Phase.Dive or Phase.Swim or Phase.Emerge;

        /// <summary>True si ha reventado (o va a hacerlo): ya no hay nada que hacer con él.</summary>
        public bool HasBurst { get; private set; }

        /// <summary>Se lanza cada vez que escupe, con el punto al que apunta.</summary>
        public event System.Action<Vector2> Spat;

        /// <summary>Se lanza al reventar.</summary>
        public event System.Action Burst;

        Collider2D bodyCollider;
        Vector2 facing = Vector2.right;
        bool moving;
        bool alerted;
        bool desperate;
        float phaseStart;
        float phaseUntil;
        float nextSpitAt;
        bool launched;
        Vector2 swimTo;
        bool hasMudGoal;
        Vector2 mudGoal;
        float nextMudSearch;

        public void Configure(ToadMudBall ball, GameObject burst, Sprite[] puddle, float puddleFrame)
        {
            ballPrefab = ball;
            burstEffect = burst;
            puddleFrames = puddle;
            puddleFrameSeconds = puddleFrame;
        }

        public override string AnimationState => Current switch
        {
            Phase.Spit => "Spit",
            Phase.Inflate or Phase.Rush => "Inflate",
            Phase.Dive => "Dive",
            Phase.Swim => "Swim",
            Phase.Emerge => "Emerge",
            _ => moving ? "Hop" : "Idle",
        };

        public override Vector2? Facing => alerted || IsSubmerged ? facing : null;

        public override bool ShowsHurt => Current == Phase.Surfaced;

        /// <summary>True si está sobre lodo profundo.</summary>
        public bool InMud => Pits.IsMud(transform.position);

        protected override void Awake()
        {
            base.Awake();
            bodyCollider = GetComponent<Collider2D>();
        }

        void Start()
        {
            if (Health != null)
                Health.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (Health != null)
                Health.Damaged -= OnDamaged;
        }

        void OnDisable() => SetSubmerged(false);

        void SetSubmerged(bool submerged)
        {
            if (Health != null)
                Health.IsInvulnerable = submerged;
            if (bodyCollider != null)
                bodyCollider.enabled = !submerged;
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

        void OnDamaged(int amount)
        {
            if (Health.IsDead || HasBurst || Current != Phase.Surfaced || !InMud)
                return;
            Submerge();
        }

        /// <summary>Se zambulle en el lodo: inmune y bajo el lodo unos segundos, y vuelve a asomar en otro punto.</summary>
        public void Submerge()
        {
            if (Health == null || Health.IsDead || HasBurst)
                return;
            // El lodo apaga al que arde.
            if (TryGetComponent<Combat.Burn>(out var burning))
                Destroy(burning);
            SetSubmerged(true);
            Enter(Phase.Dive, Length("Dive", DefaultDive));
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            Vector2 position = transform.position;
            float speed = AI.Speed(data);
            moving = false;

            switch (Current)
            {
                case Phase.Spit:
                    if (target != null)
                        facing = ((Vector2)target.position - position).normalized;
                    if (!launched && Time.time - phaseStart >= (phaseUntil - phaseStart) * spitFrame / spitFrames)
                    {
                        launched = true;
                        Throw(target);
                    }
                    if (Time.time >= phaseUntil)
                        Current = Phase.Surfaced;
                    return Vector2.zero;

                case Phase.Inflate:
                    if (Time.time >= phaseUntil)
                    {
                        if (desperate && target != null)
                            Enter(Phase.Rush, rushTime);
                        else
                            Explode();
                    }
                    return Vector2.zero;

                case Phase.Rush:
                    if (target == null || Time.time >= phaseUntil || Vector2.Distance(position, target.position) <= burstRadius * 0.6f)
                    {
                        Explode();
                        return Vector2.zero;
                    }
                    facing = ((Vector2)target.position - position).normalized;
                    moving = true;
                    return AI.Separated(AI.DirectionTo(target.position) * speed * rushSpeedFactor, speed * rushSpeedFactor);

                case Phase.Dive:
                    if (Time.time >= phaseUntil)
                    {
                        PickSwimTarget(target);
                        Enter(Phase.Swim, swimTime);
                    }
                    return Vector2.zero;

                case Phase.Swim:
                    if (Time.time >= phaseUntil || (swimTo - position).sqrMagnitude < 0.1f)
                    {
                        if (Time.time >= phaseUntil)
                            Enter(Phase.Emerge, Length("Emerge", DefaultDive));
                        return Vector2.zero;
                    }
                    facing = (swimTo - position).normalized;
                    return facing * swimSpeed;

                case Phase.Emerge:
                    if (Time.time >= phaseUntil)
                    {
                        SetSubmerged(false);
                        Current = Phase.Surfaced;
                    }
                    return Vector2.zero;
            }

            // En la superficie.
            if (target == null)
                return Wander(position, speed * 0.5f);

            Vector2 toTarget = (Vector2)target.position - position;
            float distance = toTarget.magnitude;
            bool sees = distance <= data.DetectionRange && AI.Sees(target.position);
            if (sees)
                AI.Alert();
            alerted = AI.IsAlert;
            if (!alerted)
                return Wander(position, speed * 0.5f);
            facing = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : facing;

            // Con poca vida se hincha y corre; si le pisas los talones, se hincha donde está.
            if (!desperate && Health != null && Health.Current <= Health.Max * desperateHealth)
            {
                desperate = true;
                Enter(Phase.Inflate, inflateTime);
                return Vector2.zero;
            }
            if (distance <= inflateDistance)
            {
                Enter(Phase.Inflate, inflateTime);
                return Vector2.zero;
            }

            if (sees && Time.time >= nextSpitAt && distance >= spitMinRange && distance <= spitRange)
            {
                nextSpitAt = Time.time + spitCooldown;
                launched = false;
                Enter(Phase.Spit, Length("Spit", DefaultSpit));
                return Vector2.zero;
            }

            // Si no está en el lodo y lo hay cerca, va a meterse; ya dentro, se queda y escupe desde ahí.
            if (InMud)
                return Vector2.zero;
            if (FindMud(position))
            {
                moving = true;
                return AI.Separated(AI.DirectionTo(mudGoal) * speed, speed);
            }

            // Sin lodo cerca: mantiene la distancia de tiro.
            if (distance > spitRange * 0.7f)
            {
                moving = true;
                return AI.Separated(AI.DirectionTo(target.position) * speed, speed);
            }
            if (distance < spitMinRange + 1f)
            {
                moving = true;
                return AI.Separated(-toTarget.normalized * speed, speed);
            }
            return Vector2.zero;
        }

        /// <summary>Busca (cada medio segundo) el lodo más cercano dentro del radio. Devuelve true si hay y fija <see cref="mudGoal"/>.</summary>
        bool FindMud(Vector2 position)
        {
            if (Pits.Map == null)
                return false;
            if (Time.time >= nextMudSearch)
            {
                nextMudSearch = Time.time + 0.5f;
                hasMudGoal = false;
                float best = mudSearchRadius * mudSearchRadius;
                int reach = Mathf.CeilToInt(mudSearchRadius);
                for (int dx = -reach; dx <= reach; dx++)
                {
                    for (int dy = -reach; dy <= reach; dy++)
                    {
                        var point = position + new Vector2(dx, dy);
                        float squared = (point - position).sqrMagnitude;
                        if (squared < best && Pits.IsMud(point))
                        {
                            best = squared;
                            mudGoal = point;
                            hasMudGoal = true;
                        }
                    }
                }
            }
            return hasMudGoal;
        }

        // Nada hacia un punto de lodo a la vista de donde está, lejos del jugador si puede ser; si no hay, se queda.
        void PickSwimTarget(Transform target)
        {
            Vector2 position = transform.position;
            swimTo = position;
            if (Pits.Map == null)
                return;
            float bestScore = float.MinValue;
            for (int i = 0; i < 12; i++)
            {
                var point = position + Random.insideUnitCircle.normalized * Random.Range(2f, 4f);
                if (!Pits.IsMud(point))
                    continue;
                float score = target != null ? Vector2.Distance(point, target.position) : Random.value;
                if (score > bestScore)
                {
                    bestScore = score;
                    swimTo = point;
                }
            }
        }

        void Throw(Transform target)
        {
            Vector2 position = transform.position;
            Vector2 aim = target != null ? (Vector2)target.position : position + facing * 4f;
            Spat?.Invoke(aim);
            if (ballPrefab == null)
                return;
            Vector2 mouth = position + new Vector2(facing.x < 0f ? -mouthOffset.x : mouthOffset.x, mouthOffset.y);
            var ball = Instantiate(ballPrefab);
            ball.Launch(position, aim, ballFlight, ballArc, ballDamage, ballRadius, puddleSeconds, mouth - position);
        }

        void Explode()
        {
            if (HasBurst)
                return;
            HasBurst = true;
            Vector2 position = transform.position;
            if (burstEffect != null)
                Instantiate(burstEffect, position, Quaternion.identity);

            var seen = new System.Collections.Generic.HashSet<Core.Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(position, burstRadius, LayerMask.GetMask(CombatLayers.Player)))
            {
                var health = hit.GetComponentInParent<Core.Health>();
                if (health != null && seen.Add(health) && !(hit.GetComponentInParent<Primordia.Player.PlayerController>() is { IsDodging: true }))
                    health.TakeDamage(burstDamage);
            }
            ToadMudBall.MakePuddle(position, puddleFrames, puddleFrameSeconds, puddleSeconds);
            Burst?.Invoke();

            // El cuerpo ya no está: no se ve su muerte.
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                renderer.enabled = false;
            Health.Kill();
        }
    }
}

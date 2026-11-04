using System.Linq;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Araña de las frondas (piso 3, emboscada): empieza escondida dentro de un helecho, que tiembla y deja ver sus ojos rojos
    /// (inmune y sin cuerpo físico). Sale de golpe si te acercas a menos de 3,2 u, si el helecho arde o, para no atascar la sala,
    /// pasados 10 s. Ya fuera, mantiene la distancia: lanza bolas de seda que dejan telarañas (frenan y arden) y salta para
    /// apartarse si te pegas a ella o para acortar distancia si estás lejos. Si no hay helecho donde nace, sale directamente.
    /// </summary>
    public class SpiderBrain : EnemyBrain
    {
        public enum Phase { Hidden, Reveal, Active, Shoot, Jump }

        [Header("Emboscada")]
        [SerializeField, Min(0.5f)] float revealDistance = 3.2f;
        [Tooltip("Segundos escondida como mucho, desde que la sala se activa.")]
        [SerializeField, Min(1f)] float hiddenMax = 10f;
        [Tooltip("A qué distancia busca el helecho donde esconderse.")]
        [SerializeField, Min(0.1f)] float fernSearchRadius = 1f;
        [Tooltip("Cuánto sube el dibujo de la capa escondida: va sobre la base del helecho, no sobre los pies de la araña.")]
        [SerializeField] Vector2 hiddenBodyOffset = new(0f, 0.3125f);
        [SerializeField] int hiddenSortingOrder = 13;

        [Header("Seda")]
        [SerializeField, Min(0.5f)] float shootRange = 9f;
        [SerializeField, Min(0.5f)] float shootMinRange = 3f;
        [SerializeField, Min(0.1f)] float shootCooldown = 2.8f;
        [Tooltip("Cuadro de Shoot en el que sale la seda (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int shootFrame = 1;
        [SerializeField, Min(1)] int shootFrames = 4;
        [SerializeField, Min(0.1f)] float silkFlight = 0.7f;
        [SerializeField, Min(0f)] float silkArc = 1.2f;
        [SerializeField, Min(0f)] float webSeconds = 12f;
        [Tooltip("De dónde sale la seda respecto a sus pies, mirando a la derecha (el abdomen alzado).")]
        [SerializeField] Vector2 mouthOffset = new(0.2f, 0.6f);

        [Header("Salto")]
        [SerializeField, Min(0.1f)] float jumpCooldown = 5f;
        [SerializeField, Min(1f)] float jumpSpeed = 9f;
        [Tooltip("Se aparta saltando si te acercas a menos de esto.")]
        [SerializeField, Min(0.5f)] float jumpAwayDistance = 2.5f;
        [Tooltip("Salta hacia ti si estás más lejos que esto.")]
        [SerializeField, Min(0.5f)] float jumpTowardDistance = 7f;
        [Tooltip("Cuadros de Jump entre los que se mueve (desde 0, de cuántos): despega en el 1.")]
        [SerializeField, Min(0)] int jumpFrom = 1;
        [SerializeField, Min(1)] int jumpTo = 5;
        [SerializeField, Min(1)] int jumpFrames = 6;

        [Header("Efectos")]
        [SerializeField] SpiderSilk silkPrefab;

        const float DefaultReveal = 0.28f;
        const float DefaultShoot = 0.28f;
        const float DefaultJump = 0.48f;

        public Phase Current { get; private set; } = Phase.Active;

        /// <summary>True mientras está escondida: inmune y sin cuerpo físico.</summary>
        public bool IsHidden => Current == Phase.Hidden;

        /// <summary>El escondite actual (null si no tiene).</summary>
        public IHidingPlace Hideout { get; private set; }

        /// <summary>Se lanza al lanzar la seda, con el punto al que apunta.</summary>
        public event System.Action<Vector2> Spat;

        /// <summary>Se lanza al saltar, con la dirección.</summary>
        public event System.Action<Vector2> Jumped;

        Collider2D bodyCollider;
        Transform bodyVisual;
        SpriteRenderer bodyRenderer;
        int normalSortingOrder;
        Vector3 normalBodyPosition;
        Vector2 facing = Vector2.right;
        Vector2 jumpDirection;
        bool moving;
        bool fired;
        float phaseStart;
        float phaseUntil;
        float hiddenUntil;
        float nextShotAt;
        float nextJumpAt;
        bool started;

        public void Configure(SpiderSilk silk) => silkPrefab = silk;

        public override string AnimationState => Current switch
        {
            Phase.Hidden => "Hidden",
            Phase.Reveal => "Reveal",
            Phase.Shoot => "Shoot",
            Phase.Jump => "Jump",
            _ => moving ? null : "Idle",
        };

        public override Vector2? Facing => Current is Phase.Hidden or Phase.Active && !moving ? null : facing;

        public override bool ShowsHurt => Current == Phase.Active;

        protected override void Awake()
        {
            base.Awake();
            bodyCollider = GetComponent<Collider2D>();
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bodyRenderer != null)
            {
                bodyVisual = bodyRenderer.transform;
                normalSortingOrder = bodyRenderer.sortingOrder;
                normalBodyPosition = bodyVisual.localPosition;
            }
        }

        void Start()
        {
            // Busca un helecho entero junto a donde nació (la sala los monta antes que a los enemigos).
            var place = Flammables.Near(transform.position, fernSearchRadius).OfType<IHidingPlace>().FirstOrDefault(p => p.Intact);
            if (place != null)
                HideIn(place);
            nextShotAt = Time.time + 1f;
        }

        void OnDisable()
        {
            if (Hideout != null)
                Hideout.Trembling = false;
        }

        /// <summary>Se esconde en ese sitio: inmune, sin cuerpo físico, con la capa de ojos sobre el escondite.</summary>
        public void HideIn(IHidingPlace place)
        {
            Hideout = place;
            place.Trembling = true;
            Current = Phase.Hidden;
            started = false;
            SetHidden(true);
        }

        void SetHidden(bool hidden)
        {
            if (Health != null)
                Health.IsInvulnerable = hidden;
            if (bodyCollider != null)
                bodyCollider.enabled = !hidden;
            if (bodyRenderer != null)
            {
                bodyRenderer.sortingOrder = hidden ? hiddenSortingOrder : normalSortingOrder;
                bodyVisual.localPosition = hidden ? normalBodyPosition + (Vector3)hiddenBodyOffset : normalBodyPosition;
            }
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

        /// <summary>Sale del escondite de golpe.</summary>
        public void Reveal()
        {
            if (Current != Phase.Hidden)
                return;
            if (Hideout != null)
                Hideout.Trembling = false;
            SetHidden(false);
            AI.Alert();
            Enter(Phase.Reveal, Length("Reveal", DefaultReveal));
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            Vector2 position = transform.position;
            moving = false;

            switch (Current)
            {
                case Phase.Hidden:
                    if (!started)
                    {
                        started = true;
                        hiddenUntil = Time.time + hiddenMax;
                    }
                    if ((Hideout != null && !Hideout.Intact) || Time.time >= hiddenUntil
                        || (target != null && Vector2.Distance(position, target.position) <= revealDistance))
                        Reveal();
                    return Vector2.zero;

                case Phase.Reveal:
                    if (target != null)
                        facing = ((Vector2)target.position - position).normalized;
                    if (Time.time >= phaseUntil)
                        Current = Phase.Active;
                    return Vector2.zero;

                case Phase.Shoot:
                    if (target != null)
                        facing = ((Vector2)target.position - position).normalized;
                    if (!fired && Time.time - phaseStart >= (phaseUntil - phaseStart) * shootFrame / shootFrames)
                    {
                        fired = true;
                        Throw(target);
                    }
                    if (Time.time >= phaseUntil)
                        Current = Phase.Active;
                    return Vector2.zero;

                case Phase.Jump:
                {
                    facing = jumpDirection;
                    float k = (Time.time - phaseStart) / Mathf.Max(0.01f, phaseUntil - phaseStart);
                    if (Time.time >= phaseUntil)
                    {
                        Current = Phase.Active;
                        return Vector2.zero;
                    }
                    bool airborne = k >= (float)jumpFrom / jumpFrames && k <= (float)jumpTo / jumpFrames;
                    return airborne ? jumpDirection * jumpSpeed : Vector2.zero;
                }
            }

            // Fuera: si te ve, ataca; si no, se queda.
            if (target == null)
                return Vector2.zero;
            Vector2 toTarget = (Vector2)target.position - position;
            float distance = toTarget.magnitude;
            bool sees = distance <= data.DetectionRange && AI.Sees(target.position);
            if (sees)
                AI.Alert();
            if (!AI.IsAlert)
                return Vector2.zero;
            facing = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : facing;
            float speed = AI.Speed(data);

            if (Time.time >= nextJumpAt)
            {
                if (distance < jumpAwayDistance)
                {
                    BeginJump(-toTarget.normalized);
                    return Vector2.zero;
                }
                if (distance > jumpTowardDistance)
                {
                    BeginJump(toTarget.normalized);
                    return Vector2.zero;
                }
            }

            if (sees && Time.time >= nextShotAt && distance >= shootMinRange && distance <= shootRange)
            {
                nextShotAt = Time.time + shootCooldown;
                fired = false;
                Enter(Phase.Shoot, Length("Shoot", DefaultShoot));
                return Vector2.zero;
            }

            // Mantiene la distancia de tiro.
            if (distance > shootRange * 0.75f)
            {
                moving = true;
                return AI.Separated(AI.DirectionTo(target.position) * speed, speed);
            }
            if (distance < shootMinRange)
            {
                moving = true;
                return AI.Separated(-toTarget.normalized * speed, speed);
            }
            return Vector2.zero;
        }

        void BeginJump(Vector2 direction)
        {
            nextJumpAt = Time.time + jumpCooldown;
            jumpDirection = direction;
            Enter(Phase.Jump, Length("Jump", DefaultJump));
            Jumped?.Invoke(direction);
        }

        void Throw(Transform target)
        {
            Vector2 position = transform.position;
            Vector2 aim = target != null ? (Vector2)target.position : position + facing * 4f;
            Spat?.Invoke(aim);
            if (silkPrefab == null)
                return;
            Vector2 mouth = position + new Vector2(facing.x < 0f ? -mouthOffset.x : mouthOffset.x, mouthOffset.y);
            var silk = Instantiate(silkPrefab);
            silk.Launch(position, aim, silkFlight, silkArc, webSeconds, mouth - position);
        }
    }
}

using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Helecho alto del piso 3: se mece, se atraviesa (al pasar el jugador o un enemigo se sacude y se calma), tapa la vista
    /// cuando el jugador está detrás de él y arde: al prenderlo (<see cref="Ignite"/>) arde unos segundos, enciende un fuego
    /// en su base (que salta a los helechos de al lado) y queda como restos, sin volver a arder. Los de la tierra quemada,
    /// ya carbonizados, no arden.
    /// </summary>
    public class Fern : MonoBehaviour, IFlammable, IHidingPlace
    {
        const float SwayFrameSeconds = 0.22f;
        const float RustleFrameSeconds = 0.07f;
        const float BurnFrameSeconds = 0.11f;
        // Cuánto arde antes de quedar en restos, y cuánto dura el fuego que deja en su base.
        const float BurnSeconds = 3f;
        const float BaseFireSeconds = 3.5f;

        // Por debajo del jugador (10) y los enemigos (5), o por encima del jugador cuando está detrás.
        const int BehindOrder = 3;
        const int InFrontOrder = 12;

        enum State
        {
            Sway,
            Rustle,
            Burning,
            Burnt
        }

        SpriteRenderer body;
        FernSpec spec;
        State state;
        float stateStart;
        float phase;
        Transform player;
        float halfWidth;

        public Vector2 Position => transform.position;

        public bool CanIgnite => spec != null && spec.IsBurnable && state != State.Burning && state != State.Burnt;

        // Escondite (la Araña de las frondas): mientras haya alguien dentro, el helecho se sacude sin parar.
        public bool Intact => CanIgnite;

        public bool Trembling { get; set; }

        internal void Configure(SpriteRenderer newBody, FernSpec newSpec, float newPhase)
        {
            body = newBody;
            spec = newSpec;
            phase = newPhase;
            halfWidth = spec.sway != null && spec.sway.Length > 0 && spec.sway[0] != null ? spec.sway[0].bounds.extents.x : 0.8f;
            Show();
        }

        void OnEnable() => Flammables.Register(this);

        void OnDisable() => Flammables.Unregister(this);

        public void Ignite()
        {
            if (!CanIgnite)
                return;
            SetState(State.Burning);
            GroundFire.Spawn(transform.position, BaseFireSeconds);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (state != State.Sway || other.isTrigger || spec.rustle == null || spec.rustle.Length == 0)
                return;
            var body2D = other.attachedRigidbody;
            if (body2D == null)
                return;
            // La sacudida está dibujada empujando hacia la derecha: si pasa por la derecha, se voltea.
            body.flipX = body2D.position.x > transform.position.x;
            SetState(State.Rustle);
        }

        void SetState(State newState)
        {
            state = newState;
            stateStart = Time.time;
            if (state != State.Rustle)
                body.flipX = false;
            Show();
        }

        void Update()
        {
            switch (state)
            {
                case State.Rustle when Time.time - stateStart >= spec.rustle.Length * RustleFrameSeconds:
                    SetState(Trembling ? State.Rustle : State.Sway);
                    break;
                case State.Sway when Trembling && spec.rustle != null && spec.rustle.Length > 0:
                    SetState(State.Rustle);
                    break;
                case State.Burning when Time.time - stateStart >= BurnSeconds:
                    SetState(State.Burnt);
                    break;
            }
            Show();
            Sort();
        }

        void Show()
        {
            if (body == null || spec == null)
                return;
            Sprite[] frames;
            float seconds;
            bool loop = true;
            float time = Time.time - stateStart;
            switch (state)
            {
                case State.Rustle:
                    frames = spec.rustle;
                    seconds = RustleFrameSeconds;
                    loop = false;
                    break;
                case State.Burning:
                    frames = spec.burn;
                    seconds = BurnFrameSeconds;
                    break;
                case State.Burnt:
                    body.sprite = spec.burnt != null ? spec.burnt : body.sprite;
                    return;
                default:
                    frames = spec.sway;
                    seconds = SwayFrameSeconds;
                    time = Time.time + phase;
                    break;
            }
            if (frames == null || frames.Length == 0)
                return;
            int frame = Mathf.FloorToInt(time / seconds);
            frame = loop ? ((frame % frames.Length) + frames.Length) % frames.Length : Mathf.Clamp(frame, 0, frames.Length - 1);
            body.sprite = frames[frame];
        }

        // Tapa al jugador cuando está detrás (más arriba que su base) y a la altura de sus hojas.
        void Sort()
        {
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
            }
            bool covers = player != null && player.position.y - 0.3f > transform.position.y + 0.05f
                && Mathf.Abs(player.position.x - transform.position.x) < halfWidth * 0.8f;
            body.sortingOrder = covers ? InFrontOrder : BehindOrder;
        }
    }

    /// <summary>Un tipo de helecho de un tileset: sus hojas de animación y lo que lo hace arder.</summary>
    [System.Serializable]
    public class FernSpec
    {
        public string name;
        [Tooltip("Se mece (en bucle).")]
        public Sprite[] sway = System.Array.Empty<Sprite>();
        [Tooltip("Se sacude al pasar alguien (una vez, hacia la derecha).")]
        public Sprite[] rustle = System.Array.Empty<Sprite>();
        [Tooltip("Arde (en bucle). Vacío: no arde (los carbonizados de la tierra quemada).")]
        public Sprite[] burn = System.Array.Empty<Sprite>();
        [Tooltip("Lo que queda tras arder.")]
        public Sprite burnt;
        [Min(0.05f)] public float footRadius = 0.35f;
        [Min(0.01f)] public float weight = 1f;

        public bool IsBurnable => burn != null && burn.Length > 0;
        public bool IsValid => sway != null && sway.Length > 0;
    }
}

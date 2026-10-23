using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Un cuerpo de la ameba gigante: el grande, o una de las dos medianas en que se divide. Es hijo del objeto que
    /// lleva la vida compartida (<see cref="AmoebaBoss"/>), así que los golpes que recibe bajan esa vida. Se mueve en
    /// línea recta hasta chocar con una pared (<see cref="Move"/>), tiembla antes de salir (<see cref="Shake"/>) y se
    /// estira y aplasta para que cada embestida se sienta (<see cref="SetStretch"/>, <see cref="Impact"/>).
    /// No choca con el jugador (embiste muy rápido y lo metería en la pared): lo atraviesa y le hace daño al tocarlo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class AmoebaBody : MonoBehaviour
    {
        // Hueco que se deja contra la pared al chocar.
        const float Skin = 0.02f;
        // Parte del radio que cuenta para tocar al jugador: el borde del dibujo es blando.
        const float TouchFraction = 0.85f;

        [Tooltip("Animator del dibujo, con un estado por clip.")]
        [SerializeField] Animator animator;
        [Tooltip("Objeto del dibujo: tiembla y se aplasta sin mover el cuerpo.")]
        [SerializeField] Transform visual;
        [Tooltip("Capas que lo paran (las paredes y las puertas cerradas).")]
        [SerializeField] LayerMask walls = 1;
        [Tooltip("Daño al tocar al jugador (1 = medio corazón).")]
        [SerializeField, Min(0)] int contactDamage = 1;
        [Tooltip("Cuánto se aplasta al chocar con la pared (0,3 = un 30 %).")]
        [SerializeField, Range(0f, 0.6f)] float impactSquash = 0.3f;
        [Tooltip("Segundos que tarda en recuperar la forma tras el choque.")]
        [SerializeField, Min(0.01f)] float impactTime = 0.2f;
        [Tooltip("Color del destello al recibir un golpe mientras avisa o embiste (sin cambiar de animación).")]
        [SerializeField] Color flashColor = new(1f, 0.35f, 0.35f);
        [SerializeField, Min(0.01f)] float flashTime = 0.1f;

        Rigidbody2D rb;
        CircleCollider2D circle;
        AnimatorStates states;
        Health health;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        readonly Collider2D[] touching = new Collider2D[4];
        readonly Collider2D[] overlapping = new Collider2D[8];
        Vector3 visualScale = Vector3.one;
        float shake;
        Vector2 stretchAxis = Vector2.right;
        float stretch;
        Vector2 impactAxis = Vector2.right;
        float impactStart = float.NegativeInfinity;
        float flashStart = float.NegativeInfinity;
        SpriteRenderer sprite;
        Color spriteColor = Color.white;

        public Vector2 Position => rb.position;

        /// <summary>Colisionador con el que chocó el último <see cref="Move"/> bloqueado (para depurar).</summary>
        public Collider2D LastHit { get; private set; }
        public float Radius => circle.radius * Mathf.Abs(transform.lossyScale.x);
        public AnimatorStates States => states ??= new AnimatorStates(animator);
        public Animator Animator => animator;

        public int ContactDamage
        {
            get => contactDamage;
            set => contactDamage = Mathf.Max(0, value);
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            health = GetComponentInParent<Health>();
            // Cinemático y sin choque con el jugador; los proyectiles y las mordidas sí le dan.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.excludeLayers = LayerMask.GetMask(CombatLayers.Player);
            states ??= new AnimatorStates(animator);
            if (visual != null)
                visualScale = visual.localScale;
            sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (sprite != null)
                spriteColor = sprite.color;
        }

        internal void Configure(Animator newAnimator, Transform newVisual, LayerMask newWalls)
        {
            animator = newAnimator;
            visual = newVisual;
            walls = newWalls;
            states = new AnimatorStates(animator);
            if (visual != null)
                visualScale = visual.localScale;
        }

        public void Play(string state, bool restart = false) => States.Play(state, restart);

        /// <summary>Cuánto tiembla el dibujo, en unidades (0 = quieto).</summary>
        public void Shake(float amount) => shake = Mathf.Max(0f, amount);

        /// <summary>
        /// Deforma el dibujo a lo largo de esa dirección: positivo lo estira (embistiendo), negativo lo encoge
        /// (cogiendo impulso). 0,2 = un 20 %.
        /// </summary>
        public void SetStretch(Vector2 direction, float amount)
        {
            if (direction.sqrMagnitude > 0.0001f)
                stretchAxis = direction;
            stretch = amount;
        }

        /// <summary>Destello rojo corto: el golpe se nota sin cortar la animación del aviso o de la embestida.</summary>
        public void Flash() => flashStart = Time.time;

        /// <summary>Aplastamiento contra la pared, que se recupera solo en <see cref="impactTime"/>.</summary>
        public void Impact(Vector2 normal)
        {
            if (normal.sqrMagnitude > 0.0001f)
                impactAxis = normal;
            impactStart = Time.time;
        }

        /// <summary>Lo lleva a ese punto sin mirar paredes (al aparecer o en la escena final).</summary>
        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
        }

        /// <summary>Lo acerca a ese punto este paso de física, sin mirar paredes.</summary>
        public void MoveTo(Vector2 position) => rb.MovePosition(position);

        /// <summary>
        /// Avanza en línea recta lo que pueda este paso de física. Devuelve true si choca con una pared,
        /// con la normal de esa pared.
        /// </summary>
        public bool Move(Vector2 delta, out Vector2 normal)
        {
            normal = Vector2.zero;
            float distance = delta.magnitude;
            if (distance < 0.0001f)
                return false;

            Vector2 direction = delta / distance;
            float free = FreeDistance(direction, distance + Skin, out normal);
            // Bloqueado solo si de verdad hay algo delante: comparar la distancia libre con distance + Skin falla por
            // redondeo (a veces el segundo cálculo sale un poco mayor y parecía chocar con nada).
            bool blocked = LastHit != null;
            rb.MovePosition(rb.position + direction * Mathf.Max(0f, Mathf.Min(distance, free - Skin)));
            return blocked;
        }

        /// <summary>
        /// Si el cuerpo ha quedado metido en una pared (por ejemplo, al aparecer pegado a ella), lo saca por el camino
        /// más corto. Devuelve true si estaba metido.
        /// </summary>
        public bool ResolveOverlap()
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(walls);
            int count = circle.Overlap(filter, overlapping);
            Vector2 push = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                var distance = circle.Distance(overlapping[i]);
                // Metido en la pared: el punto del cuerpo más hundido (A) tiene que llegar al borde de la pared (B).
                if (distance.isValid && distance.isOverlapped)
                {
                    var outward = distance.pointB - distance.pointA;
                    push += outward + outward.normalized * Skin;
                }
            }
            if (push == Vector2.zero)
                return false;
            Teleport(rb.position + push);
            return true;
        }

        /// <summary>Distancia libre hasta la pared en esa dirección, como mucho <paramref name="max"/>.</summary>
        public float FreeDistance(Vector2 direction, float max) => FreeDistance(direction, max, out _);

        float FreeDistance(Vector2 direction, float max, out Vector2 normal)
        {
            normal = Vector2.zero;
            LastHit = null;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(walls);
            int count = rb.Cast(direction, filter, hits, max);
            float free = max;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance < free)
                {
                    free = hits[i].distance;
                    normal = hits[i].normal;
                    LastHit = hits[i].collider;
                }
            }
            return free;
        }

        void FixedUpdate()
        {
            if (contactDamage <= 0 || !circle.enabled || (health != null && health.IsDead))
                return;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Player));
            int count = Physics2D.OverlapCircle(rb.position, Radius * TouchFraction, filter, touching);
            for (int i = 0; i < count; i++)
            {
                var target = touching[i].GetComponentInParent<Health>();
                if (target == null || target == health)
                    continue;
                target.TakeDamage(contactDamage);

                // Las Espinas pinchan a quien toca al jugador, aunque no haya choque.
                var thorns = touching[i].GetComponentInParent<Thorns>();
                if (thorns != null)
                    thorns.HurtAttacker(health);
            }
        }

        void LateUpdate()
        {
            if (visual == null)
                return;

            if (sprite != null)
            {
                float sinceFlash = (Time.time - flashStart) / flashTime;
                sprite.color = sinceFlash < 1f ? Color.Lerp(flashColor, spriteColor, sinceFlash) : spriteColor;
            }

            // Deformación: el estirón (o el encogimiento) de ahora más el aplastamiento del último choque.
            float sinceImpact = (Time.time - impactStart) / impactTime;
            float squash = sinceImpact < 1f ? impactSquash * (1f - sinceImpact) * (1f - sinceImpact) : 0f;
            var scale = Deform(stretchAxis, stretch);
            scale = Vector2.Scale(scale, Deform(impactAxis, -squash));
            visual.localScale = new Vector3(visualScale.x * scale.x, visualScale.y * scale.y, visualScale.z);

            if (shake <= 0f || Time.timeScale <= 0f)
            {
                visual.localPosition = Vector3.zero;
                return;
            }

            // A saltos de un píxel, como el resto del pixel art.
            const float pixel = 1f / 16f;
            var offset = Random.insideUnitCircle * shake;
            visual.localPosition = new Vector3(Mathf.Round(offset.x / pixel) * pixel, Mathf.Round(offset.y / pixel) * pixel, 0f);
        }

        /// <summary>
        /// Escala que estira (o encoge, si es negativo) a lo largo del eje dominante de la dirección y hace lo
        /// contrario en el otro, para que el volumen parezca el mismo.
        /// </summary>
        static Vector2 Deform(Vector2 direction, float amount)
        {
            float along = 1f + amount;
            float across = 1f - amount * 0.6f;
            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) ? new Vector2(along, across) : new Vector2(across, along);
        }
    }
}

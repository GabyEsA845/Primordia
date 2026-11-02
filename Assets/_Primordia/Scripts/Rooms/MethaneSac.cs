using System.Collections;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Odre de metano del piso 3: sólido, respira con un brillo cian y revienta con cualquier daño (golpes, disparos, fuego o
    /// la explosión de otro odre). Se infla, se raja y, en el cuadro que cuenta, explota (<see cref="Explosion"/>); la bolsa
    /// rota se queda el resto de la sala y ya no bloquea. Tiene un colisionador de bloqueo y, para los golpes cuerpo a cuerpo
    /// de cada equipo, otro en la capa de los enemigos y otro en la del jugador.
    /// </summary>
    public class MethaneSac : MonoBehaviour, IFlammable
    {
        const float IdleFrameSeconds = 0.26f;
        const float BurstFrameSeconds = 0.06f;
        // Cuadro de la hinchazón en el que explota.
        const int ExplodeFrame = 2;
        // Tamaño del cuerpo, en tiles.
        static readonly Vector2 BodySize = new(0.75f, 0.6f);

        enum State
        {
            Idle,
            Bursting,
            Spent
        }

        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();
        Sprite[] burst = System.Array.Empty<Sprite>();
        Sprite remains;
        Health health;
        Collider2D[] colliders = System.Array.Empty<Collider2D>();
        State state;
        float phase;

        /// <summary>Centro del odre (un poco por encima de su base), donde explota.</summary>
        public Vector2 Position => (Vector2)transform.position + Vector2.up * 0.45f;

        public bool CanIgnite => state == State.Idle;

        public bool IsSpent => state == State.Spent;

        /// <summary>Monta un odre en la base <paramref name="position"/> con ese arte.</summary>
        internal static MethaneSac Create(Transform parent, Vector2 position, Sprite[] idle, Sprite[] burst, Sprite remains)
        {
            var obj = new GameObject("MethaneSac");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.Length > 0 ? idle[0] : null;
            renderer.sortingOrder = 4;

            // El bloqueo y los disparos de los dos equipos: un colisionador en la capa por defecto (los proyectiles lo detienen).
            var blocker = obj.AddComponent<BoxCollider2D>();
            blocker.size = BodySize;
            blocker.offset = new Vector2(0f, BodySize.y / 2f);

            var health = obj.AddComponent<Health>();
            health.SetMax(1, healDifference: false);

            var sac = obj.AddComponent<MethaneSac>();
            sac.body = renderer;
            sac.idle = idle;
            sac.burst = burst;
            sac.remains = remains;
            sac.health = health;
            // OnEnable ya pasó (al añadir el componente) sin vida a la que suscribirse: se suscribe ahora.
            sac.Subscribe();
            // Los golpes de cada equipo buscan por capa: uno de los enemigos (para el jugador) y otro del jugador (para los enemigos).
            sac.colliders = new Collider2D[]
            {
                blocker,
                AddMeleeTarget(obj.transform, CombatLayers.Enemy),
                AddMeleeTarget(obj.transform, CombatLayers.Player),
            };
            return sac;
        }

        static Collider2D AddMeleeTarget(Transform parent, string layerName)
        {
            var obj = new GameObject($"Hit_{layerName}");
            obj.transform.SetParent(parent, false);
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                obj.layer = layer;
            var box = obj.AddComponent<BoxCollider2D>();
            box.size = BodySize;
            box.offset = new Vector2(0f, BodySize.y / 2f);
            return box;
        }

        void Awake() => phase = Random.value * 10f;

        void OnEnable()
        {
            Flammables.Register(this);
            Subscribe();
        }

        // Sin duplicar la suscripción si se llama dos veces.
        void Subscribe()
        {
            if (health == null)
                return;
            health.Died -= Pop;
            health.Died += Pop;
        }

        void OnDisable()
        {
            Flammables.Unregister(this);
            if (health != null)
                health.Died -= Pop;
        }

        void Update()
        {
            if (state != State.Idle || body == null || idle.Length == 0)
                return;
            body.sprite = idle[Mathf.FloorToInt((Time.time + phase) / IdleFrameSeconds) % idle.Length];
        }

        /// <summary>El fuego lo revienta.</summary>
        public void Ignite() => Pop();

        void Pop()
        {
            if (state != State.Idle)
                return;
            state = State.Bursting;
            Flammables.Unregister(this);
            // Deja de bloquear y de recibir golpes en cuanto empieza a reventar.
            foreach (var collider in colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }
            StartCoroutine(Burst());
        }

        IEnumerator Burst()
        {
            for (int i = 0; i < burst.Length; i++)
            {
                if (body != null)
                    body.sprite = burst[i];
                if (i == ExplodeFrame)
                    Explosion.Detonate(Position);
                yield return new WaitForSeconds(BurstFrameSeconds);
            }
            // Si el arte tuviera menos cuadros que el cuadro de la explosión, que explote igual.
            if (burst.Length <= ExplodeFrame)
                Explosion.Detonate(Position);

            if (body != null)
                body.sprite = remains;
            state = State.Spent;
        }
    }
}

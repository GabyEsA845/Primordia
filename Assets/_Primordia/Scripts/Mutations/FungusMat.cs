using System.Collections.Generic;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Dibujo animado de una mata de hongo del piso 3 (Colonizar la sala), sobre un <see cref="MyceliumPatch"/>: brota (Appear), se queda
    /// latiendo (Idle, en bucle) y destella y sopla esporas cada vez que daña (Pulse). A cada enemigo que está encima le enreda las patas
    /// con unos hilos (Snare) mientras lo frena. El daño y el frenado los hace el <see cref="MyceliumPatch"/>.
    /// </summary>
    [RequireComponent(typeof(MyceliumPatch))]
    public class FungusMat : MonoBehaviour
    {
        public enum Phase { Appear, Idle, Pulse }

        // Los hilos de enredo van sobre la mata (que va a 4) y bajo los personajes.
        const int SnareOrder = 5;

        [SerializeField] Sprite[] appear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] pulse = System.Array.Empty<Sprite>();
        [SerializeField] GameObject snare;
        [SerializeField, Min(0.01f)] float appearFrameSeconds = 0.09f;
        [SerializeField, Min(0.01f)] float idleFrameSeconds = 0.11f;
        [SerializeField, Min(0.01f)] float pulseFrameSeconds = 0.09f;

        // Enemigos que ya llevan hilos de alguna mata: varias matas juntas no los duplican.
        static readonly HashSet<Enemy> snared = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => snared.Clear();

        MyceliumPatch patch;
        SpriteRenderer body;
        Phase phase;
        float phaseStart;
        float nextPulse;
        readonly Dictionary<Enemy, GameObject> snares = new();
        readonly List<Enemy> gone = new();

        public Phase Current => phase;

        /// <summary>Hilos de enredo que lleva ahora esta mata (para las pruebas).</summary>
        public int SnareCount => snares.Count;

        internal void Configure(Sprite[] newAppear, Sprite[] newIdle, Sprite[] newPulse, GameObject newSnare)
        {
            appear = newAppear ?? System.Array.Empty<Sprite>();
            idle = newIdle ?? System.Array.Empty<Sprite>();
            pulse = newPulse ?? System.Array.Empty<Sprite>();
            snare = newSnare;
        }

        void Awake()
        {
            patch = GetComponent<MyceliumPatch>();
            body = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            phase = Phase.Appear;
            phaseStart = Time.time;
            Show();
        }

        void Update()
        {
            float elapsed = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Appear:
                    if (appear.Length == 0 || elapsed >= appear.Length * appearFrameSeconds)
                        Enter(Phase.Idle);
                    break;

                case Phase.Idle:
                    // Late al ritmo del daño mientras haya enemigos encima.
                    if (pulse.Length > 0 && patch.Covered.Count > 0 && Time.time >= nextPulse)
                    {
                        nextPulse = Time.time + 1f / Mathf.Max(1, patch.Current.damagePerSecond);
                        Enter(Phase.Pulse);
                    }
                    break;

                case Phase.Pulse:
                    if (elapsed >= pulse.Length * pulseFrameSeconds)
                        Enter(Phase.Idle);
                    break;
            }
            Show();
        }

        void Enter(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
        }

        void Show()
        {
            if (body == null)
                return;
            float elapsed = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Appear when appear.Length > 0:
                    body.sprite = appear[Mathf.Min(Mathf.FloorToInt(elapsed / appearFrameSeconds), appear.Length - 1)];
                    break;
                case Phase.Pulse when pulse.Length > 0:
                    body.sprite = pulse[Mathf.Min(Mathf.FloorToInt(elapsed / pulseFrameSeconds), pulse.Length - 1)];
                    break;
                default:
                    if (idle.Length > 0)
                        body.sprite = idle[Mathf.FloorToInt(elapsed / idleFrameSeconds) % idle.Length];
                    break;
            }
        }

        // Después del Update de la mata, que es quien sabe qué enemigos tiene encima.
        void LateUpdate()
        {
            if (snare == null)
                return;

            foreach (var enemy in patch.Covered)
            {
                if (enemy == null || snares.ContainsKey(enemy) || snared.Contains(enemy))
                    continue;
                var thread = Instantiate(snare, enemy.transform);
                foreach (var renderer in thread.GetComponentsInChildren<SpriteRenderer>())
                    renderer.sortingOrder = SnareOrder;
                snares[enemy] = thread;
                snared.Add(enemy);
            }

            gone.Clear();
            foreach (var pair in snares)
            {
                var enemy = pair.Key;
                if (enemy == null || !patch.Covers(enemy))
                    gone.Add(enemy);
                else
                    PlaceAtFeet(enemy, pair.Value.transform);
            }
            foreach (var enemy in gone)
                Release(enemy);
        }

        // Los hilos se enredan en las patas: abajo en el centro del cuerpo.
        static void PlaceAtFeet(Enemy enemy, Transform thread)
        {
            var collider = enemy.GetComponentInChildren<Collider2D>();
            thread.position = collider != null
                ? new Vector3(collider.bounds.center.x, collider.bounds.min.y, thread.position.z)
                : enemy.transform.position;
        }

        void Release(Enemy enemy)
        {
            if (snares.TryGetValue(enemy, out var thread) && thread != null)
                Destroy(thread);
            snares.Remove(enemy);
            snared.Remove(enemy);
        }

        void OnDestroy()
        {
            gone.Clear();
            gone.AddRange(snares.Keys);
            foreach (var enemy in gone)
                Release(enemy);
        }
    }
}

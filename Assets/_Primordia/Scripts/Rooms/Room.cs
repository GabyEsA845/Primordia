using System;
using Primordia.Audio;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Sala de combate. Sus enemigos esperan desactivados; cuando el jugador entra, las puertas
    /// se cierran y los enemigos aparecen ya alerta. Al morir el último (incluidos los que nacen
    /// al dividirse), las puertas se abren. Una sala sin enemigos queda limpia al entrar.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Room : MonoBehaviour
    {
        public enum RoomState
        {
            Waiting,
            Combat,
            Cleared
        }

        [Tooltip("Padre de los enemigos de la sala. Se activa al entrar el jugador.")]
        [SerializeField] Transform enemiesRoot;
        [SerializeField] Door[] doors = Array.Empty<Door>();

        public RoomState State { get; private set; }
        public int AliveEnemies { get; private set; }

        public event Action<Room> Entered;
        public event Action<Room> Cleared;

        /// <summary>
        /// Se lanza al morir cada enemigo de la sala, ya descontado de <see cref="AliveEnemies"/> y antes de
        /// <see cref="Cleared"/>: con el último, AliveEnemies ya vale 0.
        /// </summary>
        public event Action<Room, Enemy> EnemyDied;

        /// <summary>
        /// Se lanzan para cualquier sala (la música sigue al jugador): al entrar el jugador en ella, cada vez, después
        /// de empezarla; al salir; al cerrar las puertas para combatir; y al quedar limpia.
        /// </summary>
        public static event Action<Room> PlayerEnteredAny;
        public static event Action<Room> PlayerExitedAny;
        public static event Action<Room> CombatStartedAny;
        public static event Action<Room> ClearedAny;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PlayerEnteredAny = null;
            PlayerExitedAny = null;
            CombatStartedAny = null;
            ClearedAny = null;
        }

        void Awake()
        {
            // El trigger debe ser algo más pequeño que el interior: así el jugador ya
            // ha cruzado la puerta cuando esta se cierra.
            GetComponent<Collider2D>().isTrigger = true;
            HideEnemies();
        }

        internal void Configure(Transform newEnemiesRoot, Door[] newDoors)
        {
            enemiesRoot = newEnemiesRoot;
            doors = newDoors ?? Array.Empty<Door>();
            HideEnemies();
        }

        void HideEnemies()
        {
            if (State == RoomState.Waiting && enemiesRoot != null)
                enemiesRoot.gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body == null || !body.CompareTag("Player"))
                return;
            Begin();
            PlayerEnteredAny?.Invoke(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body != null && body.CompareTag("Player"))
                PlayerExitedAny?.Invoke(this);
        }

        /// <summary>Empieza la sala. Normalmente lo llama el trigger al entrar el jugador.</summary>
        public void Begin()
        {
            if (State != RoomState.Waiting)
                return;

            Entered?.Invoke(this);

            if (enemiesRoot != null)
                enemiesRoot.gameObject.SetActive(true);

            var enemies = enemiesRoot != null ? enemiesRoot.GetComponentsInChildren<Enemy>() : Array.Empty<Enemy>();
            if (enemies.Length == 0)
            {
                Clear();
                return;
            }

            State = RoomState.Combat;
            foreach (var door in doors)
                door.Close();
            if (doors.Length > 0)
                Sfx.Play(Sounds.DoorsClose);
            CombatStartedAny?.Invoke(this);

            foreach (var enemy in enemies)
            {
                Register(enemy);
                if (enemy.TryGetComponent<EnemyAI>(out var ai))
                    ai.Alert();
            }
        }

        void Register(Enemy enemy)
        {
            AliveEnemies++;
            enemy.Died += OnEnemyDied;
            enemy.ChildSpawned += Register;
        }

        void OnEnemyDied(Enemy enemy)
        {
            enemy.Died -= OnEnemyDied;
            enemy.ChildSpawned -= Register;

            AliveEnemies--;
            EnemyDied?.Invoke(this, enemy);
            if (AliveEnemies == 0 && State == RoomState.Combat)
                Clear();
        }

        void Clear()
        {
            bool fought = State == RoomState.Combat;
            State = RoomState.Cleared;
            foreach (var door in doors)
                door.Open();
            if (fought)
            {
                if (doors.Length > 0)
                    Sfx.Play(Sounds.DoorsOpen);
                Sfx.Play(Sounds.RoomCleared);
            }

            Cleared?.Invoke(this);
            ClearedAny?.Invoke(this);
        }
    }
}

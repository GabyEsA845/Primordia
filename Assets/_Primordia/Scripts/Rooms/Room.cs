using System;
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
            if (body != null && body.CompareTag("Player"))
                Begin();
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
            if (AliveEnemies == 0 && State == RoomState.Combat)
                Clear();
        }

        void Clear()
        {
            State = RoomState.Cleared;
            foreach (var door in doors)
                door.Open();

            Cleared?.Invoke(this);
        }
    }
}

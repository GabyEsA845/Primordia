using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Máquina de estados pequeña. Espera hasta detectar al jugador (o recibir daño) y luego:
    /// el Chaser va directo hacia él; el Shooter se acerca o se aleja para mantener su
    /// distancia preferida y dispara cuando lo tiene a tiro.
    /// No hay búsqueda de caminos: los obstáculos lo frenan.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Chase,
            Retreat,
            Hold
        }

        // Margen alrededor de la distancia preferida para que el Shooter no tiemble.
        const float DistanceTolerance = 1f;

        [Tooltip("A quién persigue. Si está vacío, busca el objeto con la etiqueta Player.")]
        [SerializeField] Transform target;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public State Current { get; private set; }
        public bool IsAlert { get; private set; }

        Enemy enemy;
        Health health;
        Rigidbody2D rb;
        Weapon weapon;
        Vector2 desiredVelocity;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            // La vida se pide aquí y no a Enemy: al cargar una escena, este Awake puede ejecutarse antes que el de Enemy.
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            weapon = GetComponent<Weapon>();
        }

        void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
        }

        void OnEnable() => health.Damaged += OnDamaged;

        void OnDisable() => health.Damaged -= OnDamaged;

        /// <summary>Despierta al enemigo aunque el jugador esté fuera de su alcance.</summary>
        public void Alert() => IsAlert = true;

        void OnDamaged(int _) => Alert();

        void Update()
        {
            desiredVelocity = Vector2.zero;
            var data = enemy.Data;

            if (data == null || !CanAct() || target == null || !target.gameObject.activeInHierarchy)
            {
                Current = State.Idle;
                return;
            }

            Vector2 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            if (!IsAlert && distance <= data.DetectionRange)
                Alert();

            if (!IsAlert)
            {
                Current = State.Idle;
                return;
            }

            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            if (data.Behaviour == EnemyBehaviour.Chaser)
            {
                Current = State.Chase;
                desiredVelocity = direction * data.MoveSpeed;
                return;
            }

            if (distance > data.PreferredDistance + DistanceTolerance)
            {
                Current = State.Chase;
                desiredVelocity = direction * data.MoveSpeed;
            }
            else if (distance < data.PreferredDistance - DistanceTolerance)
            {
                Current = State.Retreat;
                desiredVelocity = -direction * data.MoveSpeed;
            }
            else
            {
                Current = State.Hold;
            }

            if (weapon != null && weapon.Attack != null && distance <= weapon.Attack.Range)
                weapon.TryFire(toTarget);
        }

        void FixedUpdate() => rb.linearVelocity = desiredVelocity;

        bool CanAct() =>
            !health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);
    }
}

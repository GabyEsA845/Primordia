using Primordia.Combat;
using UnityEngine;

namespace Primordia.Enemies
{
    public enum EnemyBehaviour
    {
        /// <summary>Va directo hacia el jugador y daña al tocarlo.</summary>
        Chaser,

        /// <summary>Mantiene la distancia y dispara.</summary>
        Shooter
    }

    /// <summary>
    /// Estadísticas y comportamiento de un tipo de enemigo. Crear un enemigo nuevo es crear
    /// uno de estos y un prefab con su aspecto.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Enemy Data", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        [Header("Vida y movimiento")]
        [SerializeField, Min(1)] int maxHealth = 3;
        [Tooltip("Unidades por segundo. El jugador va a 6.")]
        [SerializeField, Min(0f)] float moveSpeed = 3f;

        [Header("Comportamiento")]
        [SerializeField] EnemyBehaviour behaviour;
        [Tooltip("Distancia a la que detecta al jugador. Recibir daño también lo despierta.")]
        [SerializeField, Min(0f)] float detectionRange = 9f;
        [Tooltip("Solo Shooter: distancia que intenta mantener con el jugador.")]
        [SerializeField, Min(0f)] float preferredDistance = 6f;

        [Header("Ataque")]
        [Tooltip("Daño al tocar al jugador. Contra el jugador, 1 = medio corazón.")]
        [SerializeField, Min(0)] int contactDamage = 1;
        [Tooltip("Solo Shooter: ataque a distancia.")]
        [SerializeField] AttackData attack;

        [Header("Al morir")]
        [Tooltip("Enemigo en que se divide al morir, como la ameba. Vacío si no se divide.")]
        [SerializeField] Enemy splitPrefab;
        [SerializeField, Min(0)] int splitCount;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public EnemyBehaviour Behaviour => behaviour;
        public float DetectionRange => detectionRange;
        public float PreferredDistance => preferredDistance;
        public int ContactDamage => contactDamage;
        public AttackData Attack => attack;
        public Enemy SplitPrefab => splitPrefab;
        public int SplitCount => splitPrefab != null ? splitCount : 0;

        internal void Configure(EnemyBehaviour behaviour, int maxHealth, float moveSpeed, float detectionRange,
            int contactDamage, float preferredDistance = 6f, AttackData attack = null,
            Enemy splitPrefab = null, int splitCount = 0)
        {
            this.behaviour = behaviour;
            this.maxHealth = Mathf.Max(1, maxHealth);
            this.moveSpeed = Mathf.Max(0f, moveSpeed);
            this.detectionRange = Mathf.Max(0f, detectionRange);
            this.contactDamage = Mathf.Max(0, contactDamage);
            this.preferredDistance = Mathf.Max(0f, preferredDistance);
            this.attack = attack;
            this.splitPrefab = splitPrefab;
            this.splitCount = Mathf.Max(0, splitCount);
        }
    }
}

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

        [Header("Biomasa")]
        [Tooltip("Probabilidad de soltar biomasa pequeña (vale 1) al morir.")]
        [SerializeField, Range(0f, 1f)] float smallBiomassChance = 0.4f;
        [Tooltip("Probabilidad de soltar biomasa grande (vale 5) al morir. Se tira antes que la pequeña: nunca suelta las dos.")]
        [SerializeField, Range(0f, 1f)] float bigBiomassChance = 0.1f;
        [Tooltip("Biomasa fija que suelta siempre, además de la tirada (el jefe).")]
        [SerializeField, Min(0)] int fixedBiomass;

        [Header("Sonido (nombres de la SoundLibrary; vacío = sin sonido)")]
        [SerializeField] string hurtSound;
        [SerializeField] string deathSound;
        [Tooltip("En lugar del de muerte, si se divide.")]
        [SerializeField] string splitSound;
        [Tooltip("Al darse cuenta del jugador.")]
        [SerializeField] string alertSound;
        [Tooltip("Al empezar el aviso antes de disparar.")]
        [SerializeField] string aimSound;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public EnemyBehaviour Behaviour => behaviour;
        public float DetectionRange => detectionRange;
        public float PreferredDistance => preferredDistance;
        public int ContactDamage => contactDamage;
        public AttackData Attack => attack;
        public Enemy SplitPrefab => splitPrefab;
        public int SplitCount => splitPrefab != null ? splitCount : 0;
        public float SmallBiomassChance => smallBiomassChance;
        public float BigBiomassChance => bigBiomassChance;
        public int FixedBiomass => fixedBiomass;
        public string HurtSound => hurtSound;
        public string DeathSound => deathSound;
        public string SplitSound => splitSound;
        public string AlertSound => alertSound;
        public string AimSound => aimSound;

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

        internal void ConfigureSounds(string hurt, string death, string split = null, string alert = null, string aim = null)
        {
            hurtSound = hurt;
            deathSound = death;
            splitSound = split;
            alertSound = alert;
            aimSound = aim;
        }

        internal void ConfigureBiomass(float smallChance, float bigChance, int fixedValue = 0)
        {
            smallBiomassChance = Mathf.Clamp01(smallChance);
            bigBiomassChance = Mathf.Clamp01(bigChance);
            fixedBiomass = Mathf.Max(0, fixedValue);
        }
    }
}

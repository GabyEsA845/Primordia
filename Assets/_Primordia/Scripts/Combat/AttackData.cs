using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Datos de un ataque a distancia: qué proyectil dispara, cuánto daña, a qué ritmo y en qué forma.
    /// Las mutaciones de Boca y los enemigos que disparan usan uno de estos.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Attack Data", fileName = "Attack_")]
    public class AttackData : ScriptableObject
    {
        [SerializeField] Projectile projectilePrefab;

        [Tooltip("Disparos por segundo como máximo.")]
        [SerializeField, Min(0.1f)] float fireRate = 4f;

        [Tooltip("Activo: dispara sin parar mientras se mantiene el botón. Inactivo: un disparo por clic.")]
        [SerializeField] bool automatic;

        [Tooltip("Daño por proyectil. Contra el jugador, 1 = medio corazón.")]
        [SerializeField, Min(0)] int damage = 1;

        [Tooltip("Velocidad del proyectil en unidades por segundo.")]
        [SerializeField, Min(0.1f)] float projectileSpeed = 12f;

        [Tooltip("Segundos antes de desaparecer. Junto con la velocidad define el alcance.")]
        [SerializeField, Min(0.05f)] float lifetime = 1f;

        [Tooltip("Proyectiles por disparo.")]
        [SerializeField, Min(1)] int projectileCount = 1;

        [Tooltip("Apertura total del abanico en grados. Solo cuenta con 2 o más proyectiles.")]
        [SerializeField, Range(0f, 360f)] float spreadAngle;

        [Header("Veneno")]
        [Tooltip("Daño por segundo del veneno que deja cada impacto. 0 = sin veneno.")]
        [SerializeField, Min(0)] int poisonDamagePerSecond;
        [Tooltip("Segundos que dura el veneno. Un impacto nuevo lo reinicia.")]
        [SerializeField, Min(0f)] float poisonDuration;

        public Projectile ProjectilePrefab => projectilePrefab;
        public float FireRate => fireRate;
        public float Cooldown => 1f / fireRate;
        public bool Automatic => automatic;
        public int Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public float Lifetime => lifetime;
        public float Range => projectileSpeed * lifetime;
        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public int PoisonDamagePerSecond => poisonDamagePerSecond;
        public float PoisonDuration => poisonDuration;
        public bool AppliesPoison => poisonDamagePerSecond > 0 && poisonDuration > 0f;

        internal void Configure(Projectile prefab, float fireRate, int damage, float speed, float lifetime,
            int count = 1, float spread = 0f, bool automatic = false, int poisonDamagePerSecond = 0, float poisonDuration = 0f)
        {
            this.poisonDamagePerSecond = Mathf.Max(0, poisonDamagePerSecond);
            this.poisonDuration = Mathf.Max(0f, poisonDuration);
            projectilePrefab = prefab;
            this.automatic = automatic;
            this.fireRate = Mathf.Max(0.1f, fireRate);
            this.damage = Mathf.Max(0, damage);
            projectileSpeed = Mathf.Max(0.1f, speed);
            this.lifetime = Mathf.Max(0.05f, lifetime);
            projectileCount = Mathf.Max(1, count);
            spreadAngle = Mathf.Clamp(spread, 0f, 360f);
        }
    }
}

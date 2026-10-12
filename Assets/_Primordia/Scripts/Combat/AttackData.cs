using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Datos de un ataque: qué proyectil dispara (o si es un golpe cuerpo a cuerpo), cuánto daña, a qué ritmo y en qué forma.
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
        [Tooltip("Efecto en bucle sobre el objetivo mientras dura el veneno.")]
        [SerializeField] GameObject poisonEffect;

        [Header("Cuerpo a cuerpo")]
        [Tooltip("Activo: no dispara proyectiles; daña a todo lo que hay en un arco delante (mordisco de Mandíbulas).")]
        [SerializeField] bool melee;
        [Tooltip("Alcance del golpe desde el centro del atacante, en unidades.")]
        [SerializeField, Min(0.1f)] float meleeRange = 1.5f;
        [Tooltip("Apertura del arco en grados, centrado en la dirección de ataque.")]
        [SerializeField, Range(1f, 360f)] float meleeArc = 180f;
        [Tooltip("Efecto visual del golpe. Se coloca en la boca y dura lo mismo que Lifetime.")]
        [SerializeField] GameObject meleeEffect;
        [Tooltip("Efecto sobre cada objetivo alcanzado por el golpe.")]
        [SerializeField] GameObject meleeHitEffect;

        public Projectile ProjectilePrefab => projectilePrefab;
        public bool IsMelee => melee;
        public float MeleeArc => meleeArc;
        public GameObject MeleeEffect => meleeEffect;
        public GameObject MeleeHitEffect => meleeHitEffect;
        public GameObject PoisonEffect => poisonEffect;

        /// <summary>True si con estos datos se puede atacar: tiene proyectil o es cuerpo a cuerpo.</summary>
        public bool IsUsable => melee || projectilePrefab != null;
        public float FireRate => fireRate;
        public float Cooldown => 1f / fireRate;
        public bool Automatic => automatic;
        public int Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public float Lifetime => lifetime;
        public float Range => melee ? meleeRange : projectileSpeed * lifetime;
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

        /// <summary>
        /// Lo convierte en un golpe cuerpo a cuerpo, sin proyectil.
        /// Daño, ritmo y Lifetime (duración del efecto) se conservan.
        /// </summary>
        internal void ConfigureMelee(float range, float arc, GameObject effect)
        {
            melee = true;
            projectilePrefab = null;
            meleeRange = Mathf.Max(0.1f, range);
            meleeArc = Mathf.Clamp(arc, 1f, 360f);
            meleeEffect = effect;
        }

        internal void SetMeleeHitEffect(GameObject effect) => meleeHitEffect = effect;

        internal void SetPoisonEffect(GameObject effect) => poisonEffect = effect;
    }
}

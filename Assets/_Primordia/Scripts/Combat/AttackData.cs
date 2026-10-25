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

        [Tooltip("Contagio: radio del estallido de veneno cuando muere un objetivo envenenado. 0 = sin contagio.")]
        [SerializeField, Min(0f)] float poisonBurstRadius;
        [Tooltip("Efecto del estallido de veneno, dibujado a su radio real.")]
        [SerializeField] GameObject poisonBurstEffect;

        [Header("Sangrado")]
        [Tooltip("Daño por segundo del sangrado que deja cada golpe. 0 = sin sangrado.")]
        [SerializeField, Min(0)] int bleedDamagePerSecond;
        [SerializeField, Min(0f)] float bleedDuration;
        [Tooltip("Marca en bucle sobre el objetivo mientras sangra.")]
        [SerializeField] GameObject bleedEffect;

        [Header("Área (Corona de brazos)")]
        [Tooltip("Remolino que aparece donde se apunta (con Whirlpool). Vacío = no es un ataque de área.")]
        [SerializeField] GameObject areaPrefab;
        [Tooltip("Distancia máxima desde el atacante a la que aparece.")]
        [SerializeField, Min(0.5f)] float areaRange = 6f;
        [SerializeField, Min(0.1f)] float areaRadius = 1.6f;
        [Tooltip("Segundos que dura.")]
        [SerializeField, Min(0.1f)] float areaDuration = 2f;
        [Tooltip("Segundos entre dos daños.")]
        [SerializeField, Min(0.05f)] float areaTick = 0.4f;
        [Tooltip("Velocidad con la que atrae a los enemigos al centro.")]
        [SerializeField, Min(0f)] float areaPull = 3f;
        [Tooltip("Marea: radio del estallido al acabar. 0 = sin Marea.")]
        [SerializeField, Min(0f)] float tideRadius;
        [SerializeField, Min(0)] int tideDamage = 4;
        [Tooltip("Velocidad del empujón hacia fuera.")]
        [SerializeField, Min(0f)] float tidePush = 10f;
        [SerializeField] GameObject tideEffect;

        [Header("Atravesar")]
        [Tooltip("Enemigos que atraviesa cada proyectil antes de desaparecer (los arpones de los Cnidocitos). 0 = se queda en el primero.")]
        [SerializeField, Min(0)] int pierce;

        [Header("Ramillete")]
        [Tooltip("Cada cuántos disparos sale un abanico (los Cnidocitos nivel III). 0 = nunca.")]
        [SerializeField, Min(0)] int volleyEvery;
        [Tooltip("Proyectiles del abanico.")]
        [SerializeField, Min(1)] int volleyCount = 3;
        [Tooltip("Apertura del abanico en grados.")]
        [SerializeField, Range(0f, 360f)] float volleySpread = 30f;

        [Header("Disparo")]
        [Tooltip("Destello en la boca al disparar (proyectiles), girado hacia donde sale.")]
        [SerializeField] GameObject muzzleEffect;

        [Header("Proyectiles buscadores")]
        [Tooltip("Grados por segundo que gira el proyectil hacia el enemigo más cercano. 0 = recto.")]
        [SerializeField, Min(0f)] float homingTurnRate;
        [Tooltip("Distancia a la que el proyectil busca objetivo.")]
        [SerializeField, Min(0f)] float homingRange = 5f;

        [Header("Proyectiles que frenan y flotan")]
        [Tooltip("Segundos que tarda el proyectil en frenar; después flota hasta acabar su Lifetime (las esporas). " +
                 "0 = vuela recto todo el tiempo.")]
        [SerializeField, Min(0f)] float brakeTime;
        [Tooltip("Velocidad a la que sigue alejándose, con un leve vaivén, después de frenar (se dispersa). 0 = quieto.")]
        [SerializeField, Min(0f)] float scatterSpeed;
        [Tooltip("Variación al azar de la velocidad de cada proyectil (0,2 = ±20 %).")]
        [SerializeField, Range(0f, 1f)] float speedJitter;
        [Tooltip("Variación al azar de la dirección de cada proyectil, en grados (±).")]
        [SerializeField, Min(0f)] float angleJitter;
        [Tooltip("Mientras flota, velocidad a la que se acerca al enemigo más cercano dentro de Homing Range. 0 = quieto.")]
        [SerializeField, Min(0f)] float driftSpeed;
        [Tooltip("Segundos finales en los que el proyectil se desvanece antes de desaparecer.")]
        [SerializeField, Min(0f)] float fadeTime;

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
        [Tooltip("Si no hay nadie a tiro, el atacante se lanza esta distancia hacia delante y golpea al llegar " +
                 "(Mandíbulas desde el nivel II). 0 = sin avance.")]
        [SerializeField, Min(0f)] float meleeLunge;
        [Tooltip("Segundos que dura el avance.")]
        [SerializeField, Min(0.01f)] float meleeLungeDuration = 0.1f;

        [Header("Placas mandibulares")]
        [Tooltip("Cuerpo a cuerpo: velocidad del empujón a quien alcanza el golpe (aturde un instante). 0 = sin empujón.")]
        [SerializeField, Min(0f)] float meleePush;
        [Tooltip("Quebrar: segundos que el objetivo recibe más daño de todo. 0 = sin Quebrar.")]
        [SerializeField, Min(0f)] float shatterDuration;
        [Tooltip("Quebrar: multiplicador del daño que recibe (1,25 = un 25 % más).")]
        [SerializeField, Min(1f)] float shatterMultiplier = 1.25f;
        [Tooltip("Marca de Quebrar sobre el enemigo mientras dura.")]
        [SerializeField] GameObject shatterMarker;
        [Tooltip("Cuerpo a cuerpo: el alcance se mide desde la boca (lo dibujado en el efecto) en lugar de desde el centro.")]
        [SerializeField] bool meleeFromMuzzle;
        [Tooltip("Guillotina: cada cuántos golpes cuerpo a cuerpo sale una segunda dentellada, más grande. 0 = nunca.")]
        [SerializeField, Min(0)] int guillotineEvery;
        [Tooltip("Guillotina: alcance de la segunda dentellada.")]
        [SerializeField, Min(0.1f)] float guillotineRange = 3f;
        [Tooltip("Guillotina: efecto de la segunda dentellada.")]
        [SerializeField] GameObject guillotineEffect;

        [Header("Sonido")]
        [Tooltip("Sonido de cada disparo o mordisco (nombre de la SoundLibrary). Vacío = sin sonido.")]
        [SerializeField] string fireSound;
        [Tooltip("Cuerpo a cuerpo: sonido cuando el golpe alcanza a alguien.")]
        [SerializeField] string hitSound;
        [Tooltip("Cuerpo a cuerpo: sonido del avance.")]
        [SerializeField] string lungeSound;
        [Tooltip("Proyectiles: sonido al chocar con algo.")]
        [SerializeField] string impactSound;

        public Projectile ProjectilePrefab => projectilePrefab;
        public string FireSound => fireSound;
        public string HitSound => hitSound;
        public string LungeSound => lungeSound;
        public string ImpactSound => impactSound;
        public bool IsMelee => melee;
        public float MeleeArc => meleeArc;
        public GameObject MeleeEffect => meleeEffect;
        public GameObject MeleeHitEffect => meleeHitEffect;
        public GameObject PoisonEffect => poisonEffect;

        /// <summary>True si con estos datos se puede atacar: tiene proyectil, es cuerpo a cuerpo o de área.</summary>
        public bool IsUsable => melee || projectilePrefab != null || IsArea;

        /// <summary>True si es un remolino de área (la Corona de brazos).</summary>
        public bool IsArea => areaPrefab != null;
        public GameObject AreaPrefab => areaPrefab;
        public float AreaRange => areaRange;
        public float AreaRadius => areaRadius;
        public float AreaDuration => areaDuration;
        public float AreaTick => areaTick;
        public float AreaPull => areaPull;
        public float TideRadius => tideRadius;
        public int TideDamage => tideDamage;
        public float TidePush => tidePush;
        public GameObject TideEffect => tideEffect;
        public float FireRate => fireRate;
        public float Cooldown => 1f / fireRate;
        public bool Automatic => automatic;
        public int Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public float Lifetime => lifetime;
        public float Range => IsArea ? areaRange : melee ? meleeRange
            : brakeTime > 0f ? projectileSpeed * Mathf.Min(brakeTime, lifetime) / 2f
            : projectileSpeed * lifetime;
        public float BrakeTime => brakeTime;
        public float ScatterSpeed => scatterSpeed;
        public float SpeedJitter => speedJitter;
        public float AngleJitter => angleJitter;
        public float DriftSpeed => driftSpeed;
        public float FadeTime => fadeTime;
        public float MeleeLunge => meleeLunge;
        public float MeleeLungeDuration => meleeLungeDuration;
        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public int PoisonDamagePerSecond => poisonDamagePerSecond;
        public float PoisonDuration => poisonDuration;
        public bool AppliesPoison => poisonDamagePerSecond > 0 && poisonDuration > 0f;
        public float PoisonBurstRadius => poisonBurstRadius;
        public GameObject PoisonBurstEffect => poisonBurstEffect;
        public int BleedDamagePerSecond => bleedDamagePerSecond;
        public float BleedDuration => bleedDuration;
        public GameObject BleedEffect => bleedEffect;
        public bool AppliesBleed => bleedDamagePerSecond > 0 && bleedDuration > 0f;
        public float MeleePush => meleePush;
        public float ShatterDuration => shatterDuration;
        public float ShatterMultiplier => shatterMultiplier;
        public GameObject ShatterMarker => shatterMarker;
        public bool MeleeFromMuzzle => meleeFromMuzzle;
        public int GuillotineEvery => guillotineEvery;
        public float GuillotineRange => guillotineRange;
        public GameObject GuillotineEffect => guillotineEffect;
        public int Pierce => pierce;
        public int VolleyEvery => volleyEvery;
        public int VolleyCount => volleyCount;
        public float VolleySpread => volleySpread;
        public GameObject MuzzleEffect => muzzleEffect;
        public float HomingTurnRate => homingTurnRate;
        public float HomingRange => homingRange;

        /// <summary>
        /// Envenena (y, con contagio, prepara el estallido) o hace sangrar al objetivo, según el ataque.
        /// <paramref name="poisonMultiplier"/> multiplica el daño del veneno (la Picadura oculta del Escorpión).
        /// </summary>
        public void ApplyStatus(Core.Health target, float poisonMultiplier = 1f)
        {
            if (target == null || target.IsDead)
                return;

            if (AppliesPoison)
            {
                int poison = Mathf.Max(1, Mathf.RoundToInt(poisonDamagePerSecond * poisonMultiplier));
                if (poisonBurstRadius > 0f)
                    Poison.ApplyContagious(target, poison, poisonDuration, poisonEffect, poisonBurstRadius, poisonBurstEffect);
                else
                    Poison.Apply(target, poison, poisonDuration, poisonEffect);
            }

            if (AppliesBleed)
                Bleed.Apply(target, bleedDamagePerSecond, bleedDuration, bleedEffect);

            if (shatterDuration > 0f)
                Shatter.Apply(target, shatterDuration, shatterMultiplier, shatterMarker);
        }

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

        /// <summary>Empujón del golpe y Quebrar (segundos y multiplicador; 0 segundos = sin Quebrar).</summary>
        internal void ConfigureJaws(float push, float newShatterDuration, float newShatterMultiplier)
        {
            meleePush = Mathf.Max(0f, push);
            shatterDuration = Mathf.Max(0f, newShatterDuration);
            shatterMultiplier = Mathf.Max(1f, newShatterMultiplier);
        }

        /// <summary>
        /// Guillotina: cada <paramref name="every"/> golpes, una segunda dentellada con ese alcance y ese efecto (0 = nunca).
        /// </summary>
        internal void ConfigureGuillotine(int every, float range, GameObject effect)
        {
            guillotineEvery = Mathf.Max(0, every);
            guillotineRange = Mathf.Max(0.1f, range);
            guillotineEffect = effect;
        }

        internal void SetShatterMarker(GameObject marker) => shatterMarker = marker;

        internal void SetMeleeFromMuzzle(bool fromMuzzle) => meleeFromMuzzle = fromMuzzle;

        /// <summary>Lo convierte en un remolino de área, sin proyectil.</summary>
        internal void ConfigureArea(GameObject prefab, float range, float radius, float duration, float tick, float pull)
        {
            melee = false;
            projectilePrefab = null;
            areaPrefab = prefab;
            areaRange = Mathf.Max(0.5f, range);
            areaRadius = Mathf.Max(0.1f, radius);
            areaDuration = Mathf.Max(0.1f, duration);
            areaTick = Mathf.Max(0.05f, tick);
            areaPull = Mathf.Max(0f, pull);
        }

        /// <summary>Marea: al acabar el remolino estalla con ese radio, daño y empujón (radio 0 = sin Marea).</summary>
        internal void ConfigureTide(float radius, int damage, float push, GameObject effect)
        {
            tideRadius = Mathf.Max(0f, radius);
            tideDamage = Mathf.Max(0, damage);
            tidePush = Mathf.Max(0f, push);
            tideEffect = effect;
        }

        internal void SetPierce(int enemies) => pierce = Mathf.Max(0, enemies);

        /// <summary>Cada <paramref name="every"/> disparos, un abanico de <paramref name="count"/> (0 = nunca).</summary>
        internal void ConfigureVolley(int every, int count, float spread)
        {
            volleyEvery = Mathf.Max(0, every);
            volleyCount = Mathf.Max(1, count);
            volleySpread = Mathf.Clamp(spread, 0f, 360f);
        }

        internal void SetMuzzleEffect(GameObject effect) => muzzleEffect = effect;

        internal void SetPoisonEffect(GameObject effect) => poisonEffect = effect;

        internal void ConfigureContagion(float radius, GameObject effect)
        {
            poisonBurstRadius = Mathf.Max(0f, radius);
            poisonBurstEffect = effect;
        }

        internal void ConfigureBleed(int damagePerSecond, float duration, GameObject effect)
        {
            bleedDamagePerSecond = Mathf.Max(0, damagePerSecond);
            bleedDuration = Mathf.Max(0f, duration);
            bleedEffect = effect;
        }

        internal void ConfigureHoming(float turnRate, float range)
        {
            homingTurnRate = Mathf.Max(0f, turnRate);
            homingRange = Mathf.Max(0f, range);
        }

        /// <summary>
        /// Proyectiles que frenan y flotan (las esporas): después de frenar se dispersan despacio y, con deriva, se
        /// acercan al enemigo.
        /// </summary>
        internal void ConfigureFloating(float brake, float speedVariation, float angleVariation, float fade, float drift = 0f,
            float scatter = 0f)
        {
            scatterSpeed = Mathf.Max(0f, scatter);
            brakeTime = Mathf.Max(0f, brake);
            speedJitter = Mathf.Clamp01(speedVariation);
            angleJitter = Mathf.Max(0f, angleVariation);
            fadeTime = Mathf.Max(0f, fade);
            driftSpeed = Mathf.Max(0f, drift);
        }

        internal void SetDrift(float speed, float range)
        {
            driftSpeed = Mathf.Max(0f, speed);
            homingRange = Mathf.Max(0f, range);
        }

        internal void SetLifetime(float seconds) => lifetime = Mathf.Max(0.05f, seconds);

        internal void SetFireRate(float rate) => fireRate = Mathf.Max(0.1f, rate);

        internal void ConfigureLunge(float distance, float duration)
        {
            meleeLunge = Mathf.Max(0f, distance);
            meleeLungeDuration = Mathf.Max(0.01f, duration);
        }

        internal void SetDamage(int newDamage) => damage = Mathf.Max(0, newDamage);

        internal void SetFan(int count, float spread)
        {
            projectileCount = Mathf.Max(1, count);
            spreadAngle = Mathf.Clamp(spread, 0f, 360f);
        }

        internal void SetPoison(int damagePerSecond, float duration)
        {
            poisonDamagePerSecond = Mathf.Max(0, damagePerSecond);
            poisonDuration = Mathf.Max(0f, duration);
        }

        internal void SetMeleeRange(float range) => meleeRange = Mathf.Max(0.1f, range);

        /// <summary>Copia todos los valores de otro ataque (para crear el de un nivel a partir del anterior).</summary>
        internal void CopyFrom(AttackData other) => JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(other), this);

        internal void ConfigureSounds(string newFire, string newHit = null, string newLunge = null, string newImpact = null)
        {
            fireSound = newFire;
            hitSound = newHit;
            lungeSound = newLunge;
            impactSound = newImpact;
        }
    }
}

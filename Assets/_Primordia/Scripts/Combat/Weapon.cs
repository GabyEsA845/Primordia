using System;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Dispara (o golpea, si es cuerpo a cuerpo) el ataque definido en un <see cref="AttackData"/>. Lo usan el jugador
    /// (cambia de ataque con las mutaciones de Boca) y los enemigos que disparan.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField] AttackData attack;
        [Tooltip("Equipo del que dispara. Decide a quién pueden dañar sus proyectiles.")]
        [SerializeField] Team team;
        [Tooltip("Punto de salida de los proyectiles. Si está vacío, salen del centro.")]
        [SerializeField] Transform muzzle;

        /// <summary>Ataque actual. Cambiarlo no reinicia la recarga.</summary>
        public AttackData Attack
        {
            get => attack;
            set => attack = value;
        }

        public Team Team => team;

        /// <summary>Punto de salida de los proyectiles (puede ser null).</summary>
        public Transform Muzzle => muzzle;
        public bool CanFire => attack != null && attack.IsUsable && Time.time >= nextFireTime;

        /// <summary>Momento (Time.time) a partir del cual termina la recarga.</summary>
        public float NextFireTime => nextFireTime;

        /// <summary>Se lanza en cada disparo, con la dirección central.</summary>
        public event Action<Vector2> Fired;

        /// <summary>
        /// Se lanza al empezar un ataque, antes de la preparación (<see cref="Windup"/>): ahí arranca la animación de ataque
        /// del cuerpo. Sin preparación, justo antes de <see cref="Fired"/>.
        /// </summary>
        public event Action<Vector2> Started;

        /// <summary>
        /// Segundos desde que se ataca hasta que sale el golpe o el proyectil: el cuadro del golpe de la animación de ataque
        /// del cuerpo de la criatura (la medusa suelta el arpón en el tercero, el pez cierra las placas en el cuarto).
        /// 0 = al instante.
        /// </summary>
        public float Windup
        {
            get => windup;
            set => windup = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Punto al que se apunta (el cursor del jugador, el enemigo de la copia de la Yema): ahí aparece el remolino de un
        /// ataque de área, como mucho a su alcance. Null = a todo el alcance en la dirección del ataque.
        /// </summary>
        public Vector2? AimPoint { get; set; }

        /// <summary>True mientras prepara un ataque que aún no ha salido.</summary>
        public bool IsWindingUp => windupAttack != null;

        float windup;
        AttackData windupAttack;
        Vector2 windupDirection;
        float windupUntil;

        /// <summary>
        /// Multiplica el daño de cada disparo o mordisco (la Emboscada de las Aletas). 1 = normal. Al leerlo incluye
        /// también los bonus de <see cref="SetDamageBonus"/>; asignarlo solo cambia el multiplicador base.
        /// </summary>
        public float DamageMultiplier
        {
            get
            {
                float total = baseMultiplier;
                foreach (var bonus in damageBonuses.Values)
                    total *= bonus;
                return total;
            }
            set => baseMultiplier = value;
        }

        /// <summary>Multiplicador de daño de una fuente (el Camuflaje); se combina con los demás.</summary>
        public void SetDamageBonus(object source, float multiplier) => damageBonuses[source] = multiplier;

        public void ClearDamageBonus(object source) => damageBonuses.Remove(source);

        /// <summary>Multiplicador del veneno que dejan sus golpes (la Picadura oculta del Escorpión). 1 = normal.</summary>
        public float PoisonMultiplier
        {
            get
            {
                float total = 1f;
                foreach (var bonus in poisonBonuses.Values)
                    total *= bonus;
                return total;
            }
        }

        public void SetPoisonBonus(object source, float multiplier) => poisonBonuses[source] = multiplier;

        public void ClearPoisonBonus(object source) => poisonBonuses.Remove(source);

        float baseMultiplier = 1f;
        readonly Dictionary<object, float> damageBonuses = new();
        readonly Dictionary<object, float> poisonBonuses = new();

        /// <summary>Daño de ese ataque con el multiplicador actual (redondeado hacia arriba).</summary>
        public int DamageOf(AttackData attack) =>
            Mathf.Max(0, Mathf.CeilToInt(attack.Damage * DamageMultiplier - 0.001f));

        /// <summary>Se lanza cuando un proyectil de esta arma hace daño. Si el objetivo murió, su IsDead es true.</summary>
        public event Action<Health> HitTarget;

        float nextFireTime;
        int projectileLayer;
        // Disparos para el Ramillete (cada N, un abanico) y golpes cuerpo a cuerpo para la Guillotina (cada N, uno doble).
        int shots;
        int strikes;

        /// <summary>Segundos que aturde el empujón normal del golpe (lo justo para que se note).</summary>
        const float PushStun = 0.15f;
        /// <summary>Segundos entre la dentellada normal y la segunda de la Guillotina.</summary>
        internal const float GuillotineGap = 0.18f;

        /// <summary>Se lanza cuando la Guillotina prepara su segunda dentellada.</summary>
        public event Action Guillotined;
        IMeleeLunger lunger;

        // Mordisco con avance que aún no ha golpeado: golpea al acabar el avance o al tener a alguien a tiro.
        AttackData pendingAttack;
        Vector2 pendingDirection;
        float pendingStrikeTime;

        /// <summary>True mientras se lanza hacia delante antes de morder.</summary>
        public bool IsLunging => pendingAttack != null;

        void Awake() => projectileLayer = CombatLayers.ProjectileLayer(team);

        void OnDisable()
        {
            pendingAttack = null;
            windupAttack = null;
        }

        void Update()
        {
            if (windupAttack != null && Time.time >= windupUntil)
            {
                var prepared = windupAttack;
                windupAttack = null;
                Fire(windupDirection, prepared);
            }

            if (pendingAttack == null)
                return;

            if (Time.time < pendingStrikeTime && !AnyTargetInReach(pendingDirection, pendingAttack))
                return;

            var attack = pendingAttack;
            pendingAttack = null;
            lunger?.StopLunge();
            Bite(pendingDirection, attack);
        }

        Vector2 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;

        /// <summary>Dispara si la recarga lo permite. Devuelve false si no disparó.</summary>
        public bool TryFire(Vector2 direction)
        {
            if (!CanFire || direction.sqrMagnitude < 0.0001f)
                return false;

            // El jugador no se frena en la cámara lenta: su cadencia va en su tiempo (ver TimeWarp).
            float warp = team == Team.Player ? TimeWarp.Scale : 1f;
            nextFireTime = Time.time + attack.Cooldown * warp;
            Started?.Invoke(direction.normalized);
            if (windup > 0f)
            {
                // Sale en el cuadro del golpe de la animación, hacia donde se apuntó al atacar.
                windupAttack = attack;
                windupDirection = direction;
                windupUntil = Time.time + windup * warp;
            }
            else
            {
                Fire(direction, attack);
            }
            return true;
        }

        /// <summary>Dispara ya, sin mirar la recarga ni la preparación.</summary>
        public void Fire(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                Started?.Invoke(direction.normalized);
            Fire(direction, attack);
        }

        /// <summary>
        /// Dispara otro ataque desde esta arma, sin mirar la recarga ni cambiar el ataque actual.
        /// Útil para patrones de jefe con varios tipos de proyectil.
        /// </summary>
        public void Fire(Vector2 direction, AttackData attack)
        {
            if (attack == null || !attack.IsUsable || direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            Vector2 origin = muzzle != null ? muzzle.position : transform.position;

            if (attack.IsArea)
            {
                Whirlpool.Spawn(attack, this, AreaPoint(direction, attack));
                Fired?.Invoke(direction);
                Sfx.Play(attack.FireSound);
                return;
            }

            if (attack.IsMelee)
            {
                if (!TryLunge(direction, attack))
                    Bite(direction, attack);
                Fired?.Invoke(direction);
                Sfx.Play(attack.FireSound);
                return;
            }

            int count = attack.ProjectileCount;
            float spread = attack.SpreadAngle;
            if (attack.VolleyEvery > 0 && ++shots % attack.VolleyEvery == 0)
            {
                count = Mathf.Max(count, attack.VolleyCount);
                spread = attack.VolleySpread;
            }
            float step = count > 1 ? spread / (count - 1) : 0f;
            float firstAngle = count > 1 ? -spread / 2f : 0f;

            for (int i = 0; i < count; i++)
            {
                var projectileDirection = Rotate(direction, firstAngle + step * i);
                ProjectilePool.Get(attack.ProjectilePrefab).Launch(origin, projectileDirection, attack, projectileLayer, this);
            }

            if (attack.MuzzleEffect != null)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                OneShotEffect.Spawn(attack.MuzzleEffect, origin, angle, muzzle != null ? muzzle : transform);
            }

            Fired?.Invoke(direction);
            Sfx.Play(attack.FireSound);
        }

        /// <summary>Dónde aparece el remolino: en el punto apuntado, como mucho a su alcance del atacante.</summary>
        Vector2 AreaPoint(Vector2 direction, AttackData attack)
        {
            Vector2 center = transform.position;
            Vector2 point = AimPoint ?? center + direction * attack.AreaRange;
            Vector2 offset = point - center;
            return offset.magnitude > attack.AreaRange ? center + offset.normalized * attack.AreaRange : point;
        }

        /// <summary>Disparos hechos con esta arma (cuenta el Ramillete).</summary>
        public int Shots => shots;

        internal void ReportHit(Health target) => HitTarget?.Invoke(target);

        internal void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;

        internal void SetTeam(Team newTeam)
        {
            team = newTeam;
            projectileLayer = CombatLayers.ProjectileLayer(team);
        }

        /// <summary>
        /// Mordisco con avance: si nadie está a tiro, se lanza hacia delante y deja el golpe para el final del
        /// avance (o para cuando alguien entre en el alcance). Devuelve false si golpea ya.
        /// </summary>
        bool TryLunge(Vector2 direction, AttackData attack)
        {
            // Se busca al usarlo: quien se lanza puede añadirse después del arma (pruebas).
            lunger ??= GetComponentInParent<IMeleeLunger>();
            if (attack.MeleeLunge <= 0f || lunger == null || AnyTargetInReach(direction, attack))
                return false;

            float duration = lunger.Lunge(direction, attack.MeleeLunge, attack.MeleeLungeDuration);
            if (duration <= 0f)
                return false;

            pendingAttack = attack;
            pendingDirection = direction;
            pendingStrikeTime = Time.time + duration;
            Sfx.Play(attack.LungeSound);
            return true;
        }

        bool AnyTargetInReach(Vector2 direction, AttackData attack)
        {
            foreach (var _ in TargetsInReach(direction, attack, ReachCenter(attack), attack.Range))
                return true;
            return false;
        }

        /// <summary>Desde dónde se mide el alcance: la boca (si el ataque lo pide) o el centro del atacante.</summary>
        Vector2 ReachCenter(AttackData attack) => attack.MeleeFromMuzzle ? MuzzlePosition : (Vector2)transform.position;

        /// <summary>
        /// Objetivos del otro equipo dentro del alcance (medido desde <paramref name="center"/>) y del arco delante de él,
        /// una vez cada uno.
        /// </summary>
        IEnumerable<(Health health, Collider2D collider)> TargetsInReach(Vector2 direction, AttackData attack, Vector2 center, float range)
        {
            var ownHealth = GetComponentInParent<Health>();
            int targets = LayerMask.GetMask(team == Team.Player ? CombatLayers.Enemy : CombatLayers.Player);
            var seen = new HashSet<Health>();

            foreach (var hit in Physics2D.OverlapCircleAll(center, range, targets))
            {
                if (hit.isTrigger)
                    continue;

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health == ownHealth || health.IsDead || !seen.Add(health))
                    continue;

                // El punto del colisionador más cercano: así cuenta aunque solo asome dentro del arco.
                Vector2 toTarget = hit.ClosestPoint(center) - center;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(direction, toTarget) > attack.MeleeArc / 2f)
                    continue;

                yield return (health, hit);
            }
        }

        /// <summary>Un mordisco y, si toca la Guillotina, la segunda dentellada más grande un momento después.</summary>
        void Bite(Vector2 direction, AttackData attack)
        {
            Strike(direction, attack, attack.Range, attack.MeleeEffect);
            if (attack.GuillotineEvery > 0 && ++strikes % attack.GuillotineEvery == 0)
            {
                Guillotined?.Invoke();
                if (isActiveAndEnabled)
                    StartCoroutine(SecondBite(direction, attack));
            }
        }

        System.Collections.IEnumerator SecondBite(Vector2 direction, AttackData attack)
        {
            yield return new WaitForSeconds(GuillotineGap);
            Strike(direction, attack, attack.GuillotineRange, attack.GuillotineEffect != null ? attack.GuillotineEffect : attack.MeleeEffect);
        }

        /// <summary>
        /// Golpe cuerpo a cuerpo: daña una vez a cada objetivo del otro equipo que esté dentro de ese alcance (desde la boca
        /// o desde el centro, según el ataque) y del arco delante del atacante.
        /// </summary>
        void Strike(Vector2 direction, AttackData attack, float range, GameObject effectPrefab)
        {
            Vector2 effectPosition = MuzzlePosition;
            Vector2 center = ReachCenter(attack);
            // Se recogen antes de dañar: una muerte puede cambiar la escena mientras se recorren.
            var hits = new List<(Health health, Collider2D collider)>(TargetsInReach(direction, attack, center, range));
            bool landed = false;
            int damage = DamageOf(attack);
            foreach (var (health, hit) in hits)
            {
                if (health == null)
                    continue;

                // El mordisco de un enemigo a un jugador que hace la esquiva justa (Cutícula) se le devuelve.
                if (team != Team.Player)
                {
                    var reflector = hit.GetComponentInParent<IHitReflector>();
                    if (reflector != null && reflector.TryReflect(GetComponentInParent<Health>()))
                        continue;
                }

                if (!health.TakeDamage(damage))
                    continue;

                landed = true;

                OneShotEffect.Spawn(attack.MeleeHitEffect, hit.bounds.center);
                attack.ApplyStatus(health, PoisonMultiplier);
                Push(health, hit, center, attack);
                ReportHit(health);
            }

            if (landed)
                Sfx.Play(attack.HitSound);

            if (effectPrefab != null)
            {
                // Cuelga del mismo padre que la boca, así sigue al jugador mientras dura. Los efectos de cuadros se
                // destruyen solos al acabar; los demás, al acabar el Lifetime del ataque.
                var parent = muzzle != null ? muzzle.parent : transform;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var effect = Instantiate(effectPrefab, effectPosition, Quaternion.Euler(0f, 0f, angle), parent);
                if (!effect.TryGetComponent<FrameEffect>(out _))
                    Destroy(effect, attack.Lifetime);
            }
        }

        /// <summary>El empujón del golpe (Placas mandibulares), hacia fuera de donde se mide el mordisco.</summary>
        static void Push(Health health, Collider2D hit, Vector2 from, AttackData attack)
        {
            if (health.IsDead || attack.MeleePush <= 0f)
                return;
            var pushable = hit.GetComponentInParent<IPushable>();
            if (pushable == null)
                return;
            Vector2 away = (Vector2)hit.bounds.center - from;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.right;
            pushable.Knockback(away * attack.MeleePush, PushStun);
        }

        static Vector2 Rotate(Vector2 vector, float degrees) => Quaternion.Euler(0f, 0f, degrees) * vector;
    }
}

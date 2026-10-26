using System;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Máquina de estados pequeña. Espera hasta detectar al jugador (o recibir daño) y luego:
    /// el Chaser va hacia él; el Shooter se acerca o se aleja para mantener su distancia preferida
    /// y dispara cuando lo tiene a tiro y lo ve (con aviso opcional: se para a apuntar).
    /// Con la rejilla del piso (<see cref="NavigationGrid"/>) rodean paredes y columnas, y el Shooter solo
    /// dispara con línea de visión; sin ella, van en línea recta. Además se apartan unos de otros.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyAI : MonoBehaviour, IPushable, IPullable
    {
        public enum State
        {
            Idle,
            Chase,
            Retreat,
            Hold,

            /// <summary>Empujado: se desliza sin control y no ataca.</summary>
            Stunned,

            /// <summary>Cegado: se queda quieto y no ataca.</summary>
            Blind,

            /// <summary>Se para a apuntar justo antes de disparar (solo con aviso de disparo).</summary>
            Aim,

            /// <summary>El jugador está camuflado: lo busca alrededor del último sitio donde lo vio y no dispara.</summary>
            Search,

            /// <summary>Asustado (Camuflaje nivel III): no ataca y se aleja despacio del jugador.</summary>
            Scared
        }

        // Margen alrededor de la distancia preferida para que el Shooter no tiemble.
        const float DistanceTolerance = 1f;

        // Cuánto frena el empujón, en unidades por segundo cada segundo.
        const float KnockbackDeceleration = 30f;

        // Grosor de la línea de visión del tirador (algo menos que un proyectil): ve por los huecos de puerta.
        const float SightRadius = 0.15f;
        // Distancia libre que necesita a su espalda para retroceder; si no la tiene, se queda quieto.
        const float RetreatCheckDistance = 1.5f;
        // Búsqueda del jugador camuflado: velocidad respecto a la normal, radio alrededor del último sitio donde
        // lo vio y segundos como mucho hacia cada punto.
        const float SearchSpeedFactor = 0.6f;
        const float SearchRadius = 3f;
        const float SearchPointTime = 2f;
        // Lo que tarda en olvidar la sospecha respecto a lo que tarda en llenarla.
        const float SuspicionDecay = 0.5f;
        // Separación entre enemigos: radio en el que se apartan y fuerza del empuje respecto a su velocidad.
        const float SeparationRadius = 1.2f;
        const float SeparationWeight = 0.6f;

        static readonly List<Collider2D> nearby = new();

        [Tooltip("A quién persigue. Si está vacío, busca el objeto con la etiqueta Player.")]
        [SerializeField] Transform target;

        [Tooltip("Segundos que se para a apuntar antes de cada disparo, para la animación Shoot. " +
                 "Empieza antes de que acabe la recarga, así que la cadencia no cambia. 0 = sin aviso.")]
        [SerializeField, Min(0f)] float fireWindup;

        /// <summary>Se lanza al empezar a apuntar; el disparo sale <see cref="FireWindup"/> segundos después.</summary>
        public event Action AimStarted;

        public float FireWindup
        {
            get => fireWindup;
            set => fireWindup = Mathf.Max(0f, value);
        }

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public State Current { get; private set; }
        public bool IsAlert { get; private set; }
        public bool IsStunned => Time.time < stunnedUntil;
        public bool IsBlind => Time.time < blindUntil;
        public bool IsScared => Time.time < scaredUntil;
        public bool IsSlowed => Time.time < slowedUntil;
        public bool IsAiming { get; private set; }

        /// <summary>De 0 a 1: cuánto le falta para descubrir al jugador camuflado que tiene cerca.</summary>
        public float Suspicion { get; private set; }

        Enemy enemy;
        Health health;
        Rigidbody2D rb;
        bool fallingIntoPit;

        /// <summary>True mientras cae a una fosa (ya no hace nada más).</summary>
        public bool IsFallingIntoPit => fallingIntoPit;
        Weapon weapon;
        Vector2 desiredVelocity;
        Vector2 knockbackVelocity;
        float stunnedUntil;
        float paralyzedUntil;
        float blindUntil;
        float scaredUntil;
        float scaredSpeed;
        float slowedUntil;
        float slowFactor = 1f;
        bool searching;
        Vector2 searchPoint;
        float searchPointUntil;
        float fireAt;
        float bodyRadius;
        EnemyBrain brain;

        /// <summary>Cerebro propio (enemigos del piso 2), o null: entonces se comporta según su EnemyBehaviour.</summary>
        public EnemyBrain Brain => brain;

        /// <summary>A quién va ahora: el objetivo o el señuelo más cercano.</summary>
        public Transform CurrentTarget { get; private set; }
        ContactFilter2D separationFilter;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            // La vida se pide aquí y no a Enemy: al cargar una escena, este Awake puede ejecutarse antes que el de Enemy.
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            weapon = GetComponent<Weapon>();

            var body = GetComponent<Collider2D>();
            bodyRadius = body != null ? Mathf.Min(body.bounds.extents.x, body.bounds.extents.y) : 0.45f;
            separationFilter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = LayerMask.GetMask(CombatLayers.Enemy) };
        }

        void Start()
        {
            // En Start y no en Awake: el cerebro se añade después de la IA (la pide con RequireComponent).
            brain = GetComponent<EnemyBrain>();

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
        public void Alert()
        {
            if (IsAlert)
                return;
            IsAlert = true;
            if (enemy != null && enemy.Data != null)
                Sfx.Play(enemy.Data.AlertSound);
        }

        void OnDamaged(int _) => Alert();

        /// <summary>
        /// Empuja al enemigo con esa velocidad y lo deja sin actuar durante el aturdimiento.
        /// Con <paramref name="stunMarker"/>, lo muestra sobre su cabeza mientras dura.
        /// </summary>
        public void Knockback(Vector2 velocity, float stunDuration, GameObject stunMarker = null)
        {
            knockbackVelocity = velocity;
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunDuration);
            Alert();
            if (stunDuration > 0f)
                StatusMarkers.Show(this, "Stun", stunMarker, stunDuration);
        }

        // ---------- Para los cerebros (EnemyBrain) ----------

        /// <summary>Lo pone alerta (al ver al jugador).</summary>
        internal void BecomeAlert() => Alert();

        /// <summary>Velocidad de movimiento con la ralentización actual.</summary>
        internal float Speed(EnemyData data) => SpeedOf(data);

        /// <summary>Dirección por el camino hasta ese punto (rodeando paredes y, si anda, fosas).</summary>
        internal Vector2 DirectionTo(Vector2 point) => PathTo(point);

        /// <summary>La velocidad con la separación de los enemigos cercanos.</summary>
        internal Vector2 Separated(Vector2 velocity, float speed) => Steer(velocity, speed);

        /// <summary>True si ve ese punto (línea de visión libre).</summary>
        internal bool Sees(Vector2 point) => CanSee(point);

        /// <summary>Radio del cuerpo.</summary>
        internal float BodyRadius => bodyRadius;

        /// <summary>Lo deja quieto y sin atacar durante ese tiempo, con su marca de cegado si la hay.</summary>
        public void Blind(float duration, GameObject blindMarker = null)
        {
            blindUntil = Mathf.Max(blindUntil, Time.time + duration);
            StatusMarkers.Show(this, "Blind", blindMarker, duration);
        }

        /// <summary>
        /// Paraliza: sin moverse por sí mismo ni atacar durante ese tiempo (Glándula de choque nivel III); un empujón
        /// recibido a la vez sigue frenándose. Muestra su marca encima y los rayos por el cuerpo.
        /// </summary>
        public void Paralyze(float duration, GameObject marker = null, GameObject bodyEffect = null)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
            paralyzedUntil = Mathf.Max(paralyzedUntil, Time.time + duration);
            Alert();
            StatusMarkers.Show(this, "Paralyzed", marker, duration);
            StatusMarkers.Show(this, "ParalyzedBody", bodyEffect, duration, overhead: false);
        }

        public bool IsParalyzed => Time.time < paralyzedUntil;

        /// <summary>
        /// Lo asusta (Camuflaje nivel III): durante ese tiempo no ataca y se aleja despacio del objetivo, a esa
        /// fracción de su velocidad. Muestra su marca encima.
        /// </summary>
        public void Scare(float duration, float speedFactor, GameObject marker = null)
        {
            scaredUntil = Mathf.Max(scaredUntil, Time.time + duration);
            scaredSpeed = Mathf.Clamp01(speedFactor);
            IsAiming = false;
            Alert();
            StatusMarkers.Show(this, "Scared", marker, duration);
        }

        /// <summary>Lo frena a esa fracción de su velocidad durante ese tiempo (el micelio de las Hifas).</summary>
        public void Slow(float factor, float duration)
        {
            if (!IsSlowed || factor < slowFactor)
                slowFactor = Mathf.Clamp01(factor);
            slowedUntil = Mathf.Max(slowedUntil, Time.time + duration);
        }

        float SpeedOf(EnemyData data) => data.MoveSpeed * (IsSlowed ? slowFactor : 1f);

        void Update()
        {
            desiredVelocity = Vector2.zero;
            var data = enemy.Data;

            if (fallingIntoPit)
                return;

            if (data == null || !CanAct())
            {
                Current = State.Idle;
                return;
            }

            if (IsStunned)
            {
                IsAiming = false;
                Current = State.Stunned;
                desiredVelocity = knockbackVelocity;
                knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, KnockbackDeceleration * Time.deltaTime);
                return;
            }

            if (IsBlind)
            {
                IsAiming = false;
                Current = State.Blind;
                return;
            }

            // Las copias de la Yema clonal son señuelos: va a por el más cercano.
            var aim = Decoys.Choose(transform.position, target);
            CurrentTarget = aim;

            // Los enemigos con cerebro propio deciden todo lo demás.
            if (brain != null)
            {
                IsAiming = false;
                desiredVelocity = brain.Think(aim != null && aim.gameObject.activeInHierarchy ? aim : null, data);
                return;
            }
            if (aim == null || !aim.gameObject.activeInHierarchy)
            {
                IsAiming = false;
                Current = State.Idle;
                return;
            }

            Vector2 toTarget = aim.position - transform.position;
            float distance = toTarget.magnitude;

            if (IsScared)
            {
                IsAiming = false;
                Current = State.Scared;
                Vector2 away = distance > 0.0001f ? -toTarget / distance : Vector2.right;
                float scaredMove = SpeedOf(data) * scaredSpeed;
                desiredVelocity = HasRoomBehind(away) ? Steer(away * scaredMove, scaredMove) : Vector2.zero;
                return;
            }

            if (aim.TryGetComponent<IConcealable>(out var concealable) && concealable.IsConcealed)
            {
                Search(data, concealable, distance);
                return;
            }

            searching = false;
            Suspicion = 0f;

            if (!IsAlert && distance <= data.DetectionRange)
                Alert();

            if (!IsAlert)
            {
                Current = State.Idle;
                return;
            }

            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;
            Vector2 targetPosition = aim.position;
            float speed = SpeedOf(data);

            if (data.Behaviour == EnemyBehaviour.Chaser)
            {
                Current = State.Chase;
                desiredVelocity = Steer(PathTo(targetPosition) * speed, speed);
                return;
            }

            // Sin línea de visión no dispara: lo busca por el camino hasta volver a verlo.
            if (!CanSee(targetPosition))
            {
                IsAiming = false;
                Current = State.Chase;
                desiredVelocity = Steer(PathTo(targetPosition) * speed, speed);
                return;
            }

            if (distance > data.PreferredDistance + DistanceTolerance)
            {
                Current = State.Chase;
                desiredVelocity = Steer(PathTo(targetPosition) * speed, speed);
            }
            else if (distance < data.PreferredDistance - DistanceTolerance && HasRoomBehind(-direction))
            {
                Current = State.Retreat;
                desiredVelocity = Steer(-direction * speed, speed);
            }
            else
            {
                Current = State.Hold;
                desiredVelocity = Steer(Vector2.zero, speed);
            }

            UpdateFiring(toTarget, distance);
        }

        /// <summary>
        /// El jugador está camuflado: si estaba alerta, lo busca por puntos al azar alrededor del último sitio donde
        /// lo vio, sin disparar. Si el jugador se le acerca, la sospecha sube (con su marca encima) y al llenarse lo
        /// descubre; lejos, la sospecha baja despacio.
        /// </summary>
        void Search(EnemyData data, IConcealable concealable, float distance)
        {
            IsAiming = false;

            if (distance <= concealable.NoticeRange)
                Suspicion += Time.deltaTime / Mathf.Max(0.01f, concealable.NoticeTime);
            else
                Suspicion = Mathf.Max(0f, Suspicion - Time.deltaTime * SuspicionDecay / Mathf.Max(0.01f, concealable.NoticeTime));

            if (Suspicion > 0f)
                StatusMarkers.Show(this, "Suspect", concealable.SuspectMarker, 0.15f);

            if (Suspicion >= 1f)
            {
                Suspicion = 0f;
                concealable.Reveal();
                Alert();
                return;
            }

            if (!IsAlert)
            {
                Current = State.Idle;
                return;
            }

            if (!searching)
            {
                searching = true;
                NextSearchPoint(concealable.LastSeenPosition, first: true);
            }
            else if (Time.time >= searchPointUntil || (searchPoint - rb.position).sqrMagnitude < 0.36f)
            {
                NextSearchPoint(concealable.LastSeenPosition, first: false);
            }

            Current = State.Search;
            float speed = SpeedOf(data) * SearchSpeedFactor;
            desiredVelocity = Steer(PathTo(searchPoint) * speed, speed);
        }

        void NextSearchPoint(Vector2 lastSeen, bool first)
        {
            searchPoint = first ? lastSeen : lastSeen + UnityEngine.Random.insideUnitCircle * SearchRadius;
            searchPointUntil = Time.time + SearchPointTime;
        }

        /// <summary>Rejilla con la que se mueve: los que nadan cruzan las fosas; los demás las rodean.</summary>
        NavigationGrid MoveGrid => enemy.Data != null && enemy.Data.Swims && NavigationGrid.Swimming != null
            ? NavigationGrid.Swimming
            : NavigationGrid.Current;

        /// <summary>Dirección hacia ese punto rodeando paredes y columnas (recta si no hay rejilla del piso).</summary>
        Vector2 PathTo(Vector2 point)
        {
            var grid = MoveGrid;
            return grid == null ? (point - rb.position).normalized : grid.DirectionTo(rb.position, point, bodyRadius * 0.9f);
        }

        /// <summary>True si no hay pared entre el enemigo y ese punto (siempre, si no hay rejilla del piso). Las fosas no tapan.</summary>
        bool CanSee(Vector2 point)
        {
            var grid = NavigationGrid.Swimming ?? NavigationGrid.Current;
            return grid == null || grid.HasLineOfSight(rb.position, point, SightRadius);
        }

        /// <summary>True si puede retroceder en esa dirección sin chocar enseguida con una pared.</summary>
        bool HasRoomBehind(Vector2 away)
        {
            var grid = MoveGrid;
            return grid == null || grid.HasLineOfSight(rb.position, rb.position + away * RetreatCheckDistance, bodyRadius * 0.9f);
        }

        /// <summary>Suma a la velocidad deseada un empuje para apartarse de los enemigos cercanos, sin pasar de la velocidad.</summary>
        Vector2 Steer(Vector2 velocity, float speed)
        {
            nearby.Clear();
            Physics2D.OverlapCircle(rb.position, SeparationRadius, separationFilter, nearby);

            var push = Vector2.zero;
            foreach (var other in nearby)
            {
                if (other.attachedRigidbody == rb)
                    continue;
                var away = rb.position - (Vector2)other.transform.position;
                float d = away.magnitude;
                if (d > 0.001f)
                    push += away / d * (1f - d / SeparationRadius);
            }

            return Vector2.ClampMagnitude(velocity + Vector2.ClampMagnitude(push, 1f) * speed * SeparationWeight, speed);
        }

        void UpdateFiring(Vector2 toTarget, float distance)
        {
            if (weapon == null || weapon.Attack == null)
                return;

            if (IsAiming)
            {
                Current = State.Aim;
                desiredVelocity = Vector2.zero;
                if (Time.time >= fireAt)
                {
                    IsAiming = false;
                    weapon.TryFire(toTarget);
                }
                return;
            }

            if (distance > weapon.Attack.Range)
                return;

            if (fireWindup <= 0f)
            {
                weapon.TryFire(toTarget);
                return;
            }

            // Empieza a apuntar cuando a la recarga le queda lo que dura el aviso.
            if (weapon.Attack.IsUsable && Time.time >= weapon.NextFireTime - fireWindup)
            {
                IsAiming = true;
                fireAt = Time.time + fireWindup;
                Current = State.Aim;
                desiredVelocity = Vector2.zero;
                AimStarted?.Invoke();
                Sfx.Play(enemy.Data.AimSound);
            }
        }

        // Arrastre de una corriente (el remolino de la Corona de brazos): se suma mientras se renueve.
        Vector2 pullVelocity;
        float pullUntil;
        const float PullHold = 0.12f;

        /// <summary>True mientras lo arrastra una corriente.</summary>
        public bool IsPulled => Time.time < pullUntil;

        public void Pull(Vector2 velocity)
        {
            pullVelocity = velocity;
            pullUntil = Time.time + PullHold;
        }

        void FixedUpdate()
        {
            // Los que andan no se meten solos en una fosa (si no hay camino hasta el jugador, esperan en la orilla); los
            // empujones (aturdido) sí pueden tirarlos.
            rb.linearVelocity = !IsStunned && !fallingIntoPit && Pits.Map != null && CanFall()
                ? AvoidPits(desiredVelocity)
                : desiredVelocity;
            // La corriente sí puede arrastrarlos a una fosa.
            if (IsPulled && !fallingIntoPit)
                rb.linearVelocity += pullVelocity;

            // Los que andan caen si acaban encima de una fosa (empujados); los que nadan y los jefes, nunca.
            if (!fallingIntoPit && !health.IsDead && Pits.IsDeepInPit(rb.position, PitEdgeGrace) && CanFall())
                StartCoroutine(FallIntoPit());
        }

        // Como el jugador: hay que entrar un poco en la fosa para caer, no basta con rozar el borde.
        const float PitEdgeGrace = 0.2f;

        // Lo que mira por delante (además de su radio) para no pisar una fosa.
        const float PitLookAhead = 0.2f;

        /// <summary>
        /// La velocidad sin lo que le metería en una fosa: si de frente hay fosa, se desliza por el borde (solo en x o solo en
        /// y); si tampoco puede, se queda quieto en la orilla.
        /// </summary>
        Vector2 AvoidPits(Vector2 velocity)
        {
            if (velocity.sqrMagnitude < 0.0001f)
                return velocity;

            float reach = bodyRadius + PitLookAhead;
            bool Blocked(Vector2 direction) => Pits.IsPit(rb.position + direction.normalized * reach);

            if (!Blocked(velocity))
                return velocity;
            var alongX = new Vector2(velocity.x, 0f);
            var alongY = new Vector2(0f, velocity.y);
            if (alongX.sqrMagnitude > 0.01f && !Blocked(alongX))
                return alongX;
            if (alongY.sqrMagnitude > 0.01f && !Blocked(alongY))
                return alongY;
            return Vector2.zero;
        }

        bool CanFall() => enemy.Data != null && !enemy.Data.Swims && !TryGetComponent<IBoss>(out _);

        System.Collections.IEnumerator FallIntoPit()
        {
            fallingIntoPit = true;
            desiredVelocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            // Lo que llevaba se pierde en el fondo.
            enemy.DropsBiomass = false;
            enemy.FellIntoPit = true;
            yield return PitFallEffect.Play(transform);
            health.Kill();
        }

        bool CanAct() =>
            !health.IsDead && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);
    }
}

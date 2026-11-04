using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Calamites madre (piso 3, plantas madre): mata de cañas inmóvil con 30 de vida. Mientras te ve, cada 4 s hincha su rizoma y
    /// echa un retoño (<see cref="CalamitesSproutBrain"/>), hasta 4 vivos; y de vez en cuando encoge las cañas y suelta un anillo de
    /// esporas lentas con huecos. Al morir, todos sus retoños se secan: hay que priorizarla. Los retoños cuentan para limpiar la
    /// sala (se registran como hijos).
    /// </summary>
    [RequireComponent(typeof(Weapon))]
    public class CalamitesBrain : EnemyBrain
    {
        public enum Phase { Idle, Spawn, Spores }

        [SerializeField] CalamitesSproutBrain sproutPrefab;
        [Tooltip("Segundos entre retoños.")]
        [SerializeField, Min(0.5f)] float sproutInterval = 4f;
        [SerializeField, Min(1)] int maxSprouts = 4;
        [Tooltip("Cuadro de Spawn en el que sale el retoño (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int sproutFrame = 3;
        [SerializeField, Min(1)] int spawnFrames = 5;
        [Tooltip("A qué distancia de los pies nace el retoño.")]
        [SerializeField, Min(0.5f)] float sproutDistance = 1.3f;

        [Header("Esporas")]
        [Tooltip("Segundos entre anillos de esporas.")]
        [SerializeField, Min(0.5f)] float sporeInterval = 7f;
        [SerializeField, Min(1)] int sporeCount = 10;
        [Tooltip("Cuadro de Spores en el que sale el anillo (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int sporeFrame = 2;
        [SerializeField, Min(1)] int sporeFrames = 5;
        [Tooltip("Cuánto gira el anillo de un disparo al siguiente, para que los huecos cambien de sitio.")]
        [SerializeField, Range(0f, 360f)] float ringTurn = 18f;

        const float DefaultSpawn = 0.55f;
        const float DefaultSpores = 0.55f;

        public Phase Current { get; private set; } = Phase.Idle;

        /// <summary>Retoños vivos de esta madre.</summary>
        public IReadOnlyList<CalamitesSproutBrain> Sprouts => sprouts;

        /// <summary>Se lanza al nacer un retoño.</summary>
        public event System.Action<CalamitesSproutBrain> SproutSpawned;

        /// <summary>Se lanza al soltar el anillo de esporas.</summary>
        public event System.Action RingFired;

        readonly List<CalamitesSproutBrain> sprouts = new();
        Weapon weapon;
        float phaseStart;
        float phaseUntil;
        float nextSprout;
        float nextSpores;
        float ringAngle;
        bool done;

        public void Configure(CalamitesSproutBrain sprout) => sproutPrefab = sprout;

        public override string AnimationState => Current switch
        {
            Phase.Spawn => "Spawn",
            Phase.Spores => "Spores",
            _ => "Idle",
        };

        // Una planta no se voltea.
        public override Vector2? Facing => Vector2.right;

        public override bool ShowsHurt => Current == Phase.Idle;

        protected override void Awake()
        {
            base.Awake();
            weapon = GetComponent<Weapon>();
            if (Body != null)
                Body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        void Start()
        {
            nextSprout = Time.time + 1f;
            nextSpores = Time.time + sporeInterval * 0.6f;
            if (Health != null)
                Health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (Health != null)
                Health.Died -= OnDied;
        }

        // Al morir la madre, los retoños se secan.
        void OnDied()
        {
            foreach (var sprout in sprouts)
            {
                if (sprout != null)
                    sprout.Wither();
            }
        }

        void Enter(Phase phase, float duration)
        {
            Current = phase;
            phaseStart = Time.time;
            phaseUntil = Time.time + duration;
            done = false;
        }

        float Length(string state, float fallback)
        {
            float length = Animator != null ? Animator.LengthOf(state) : 0f;
            return length > 0f ? length : fallback;
        }

        int AliveSprouts()
        {
            sprouts.RemoveAll(sprout => sprout == null);
            return sprouts.Count;
        }

        public override Vector2 Think(Transform target, EnemyData data)
        {
            switch (Current)
            {
                case Phase.Spawn:
                    if (!done && Time.time - phaseStart >= (phaseUntil - phaseStart) * sproutFrame / spawnFrames)
                    {
                        done = true;
                        SpawnSprout(target);
                    }
                    if (Time.time >= phaseUntil)
                        Current = Phase.Idle;
                    return Vector2.zero;

                case Phase.Spores:
                    if (!done && Time.time - phaseStart >= (phaseUntil - phaseStart) * sporeFrame / sporeFrames)
                    {
                        done = true;
                        FireRing();
                    }
                    if (Time.time >= phaseUntil)
                        Current = Phase.Idle;
                    return Vector2.zero;
            }

            if (target == null)
                return Vector2.zero;
            bool sees = Vector2.Distance(transform.position, target.position) <= data.DetectionRange && AI.Sees(target.position);
            if (!sees)
                return Vector2.zero;
            AI.Alert();

            if (Time.time >= nextSprout && AliveSprouts() < maxSprouts)
            {
                nextSprout = Time.time + sproutInterval;
                Enter(Phase.Spawn, Length("Spawn", DefaultSpawn));
            }
            else if (Time.time >= nextSpores)
            {
                nextSpores = Time.time + sporeInterval;
                Enter(Phase.Spores, Length("Spores", DefaultSpores));
            }
            return Vector2.zero;
        }

        void SpawnSprout(Transform target)
        {
            if (sproutPrefab == null)
                return;

            Vector2 feet = transform.position;
            Vector2 toTarget = target != null ? (Vector2)target.position - feet : Vector2.down;
            float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            // Nace hacia donde estás, con algo de dispersión; si hay pared en medio, prueba otros lados.
            Vector2 spot = feet;
            var grid = NavigationGrid.Current;
            for (int i = 0; i < 8; i++)
            {
                float angle = (baseAngle + (i == 0 ? 0f : Random.Range(-100f, 100f))) * Mathf.Deg2Rad;
                var point = feet + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sproutDistance;
                if (grid == null || grid.HasLineOfSight(feet, point, 0.2f))
                {
                    spot = point;
                    break;
                }
            }

            var sprout = Instantiate(sproutPrefab, spot, Quaternion.identity, transform.parent);
            sprout.Bind(this);
            if (sprout.TryGetComponent<Enemy>(out var enemy))
            {
                // Sin biomasa: son una molestia, no una granja. La sala los cuenta.
                enemy.DropsBiomass = false;
                Enemy.RegisterChild(enemy);
            }
            sprouts.Add(sprout);
            SproutSpawned?.Invoke(sprout);
        }

        void FireRing()
        {
            if (weapon != null && weapon.Attack != null)
            {
                float step = 360f / sporeCount;
                for (int i = 0; i < sporeCount; i++)
                {
                    float angle = (ringAngle + i * step) * Mathf.Deg2Rad;
                    weapon.Fire(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), weapon.Attack);
                }
            }
            ringAngle = Mathf.Repeat(ringAngle + ringTurn, 360f);
            RingFired?.Invoke();
        }

    }
}

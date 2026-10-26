using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="WiwaxiaScleritesMutation"/>: cuando toca y hay enemigos a su alcance, suelta una tanda de
    /// escamas repartidas entre los más cercanos. Con la Órbita, antes giran a su alrededor destruyendo proyectiles enemigos.
    /// </summary>
    public class ScleriteVolley : MonoBehaviour
    {
        [Serializable]
        public struct Settings
        {
            public float interval;
            public int count;
            public AttackData attack;
            public float range;
            public bool orbit;
            public float orbitDuration;
            public float orbitRadius;
            public float orbitSpeed;
            public Sprite[] scaleFrames;
            public Sprite[] releaseFrames;
            public Sprite[] blockFrames;
        }

        const int SortingOrder = 21;
        const float ScaleRadius = 0.25f;

        Weapon weapon;
        Settings settings;
        float nextVolley;
        bool orbiting;
        readonly List<Transform> orbiters = new();
        static readonly Collider2D[] hits = new Collider2D[8];

        /// <summary>Tandas soltadas y proyectiles destruidos por la Órbita (para las pruebas).</summary>
        public int Volleys { get; private set; }
        public int Blocked { get; private set; }
        public bool IsOrbiting => orbiting;

        /// <summary>Se lanza al salir cada tanda, con las escamas lanzadas (la Cosecha de la Wiwaxia cuenta a quién dan).</summary>
        public event Action<IReadOnlyList<Projectile>> Released;

        /// <summary>Lo mismo para cualquier tanda (la Cosecha no depende de qué componente la suelta).</summary>
        public static event Action<ScleriteVolley, IReadOnlyList<Projectile>> AnyReleased;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyReleased = null;

        internal void Configure(Weapon newWeapon, Settings newSettings)
        {
            weapon = newWeapon;
            settings = newSettings;
            nextVolley = Time.time + settings.interval;
        }

        /// <summary>La siguiente tanda sale en cuanto haya enemigos (para probar).</summary>
        internal void Ready() => nextVolley = Time.time;

        void Update()
        {
            if (orbiting || Time.time < nextVolley || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing))
                return;
            var targets = Targets();
            if (targets.Count == 0)
                return;
            nextVolley = Time.time + settings.interval;
            if (settings.orbit)
                StartCoroutine(Orbit());
            else
                Release(targets);
        }

        /// <summary>Enemigos vivos a su alcance, del más cercano al más lejano.</summary>
        List<Health> Targets()
        {
            Vector2 center = transform.position;
            return Physics2D.OverlapCircleAll(center, settings.range, LayerMask.GetMask(CombatLayers.Enemy))
                .Select(hit => hit.GetComponentInParent<Health>())
                .Where(health => health != null && !health.IsDead)
                .Distinct()
                .OrderBy(health => ((Vector2)health.transform.position - center).sqrMagnitude)
                .ToList();
        }

        /// <summary>Las escamas salen repartidas entre los enemigos más cercanos (si hay menos, varias al mismo).</summary>
        void Release(List<Health> targets, IReadOnlyList<Vector2> origins = null)
        {
            Volleys++;
            FrameEffect.Play("ScleritesRelease", settings.releaseFrames, 0.07f, SortingOrder, transform);
            Sfx.Play(Sounds.Quills);
            var launched = new List<Projectile>();
            if (settings.attack == null || settings.attack.ProjectilePrefab == null || targets.Count == 0)
            {
                Released?.Invoke(launched);
                AnyReleased?.Invoke(this, launched);
                return;
            }
            int layer = CombatLayers.ProjectileLayer(Team.Player);
            for (int i = 0; i < settings.count; i++)
            {
                Vector2 origin = origins != null && i < origins.Count ? origins[i] : (Vector2)transform.position;
                var target = targets[i % targets.Count];
                var direction = (Vector2)target.transform.position - origin;
                if (direction.sqrMagnitude < 0.0001f)
                    direction = Vector2.right;
                var projectile = ProjectilePool.Get(settings.attack.ProjectilePrefab);
                projectile.Launch(origin, direction.normalized, settings.attack, layer, weapon);
                launched.Add(projectile);
            }
            Released?.Invoke(launched);
            AnyReleased?.Invoke(this, launched);
        }

        /// <summary>Órbita: las escamas giran a su alrededor destruyendo proyectiles enemigos y luego salen.</summary>
        IEnumerator Orbit()
        {
            orbiting = true;
            for (int i = 0; i < settings.count; i++)
            {
                var scale = new GameObject("ScleriteOrbit");
                scale.transform.SetParent(transform, false);
                var renderer = scale.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrder;
                if (settings.scaleFrames != null && settings.scaleFrames.Length > 0)
                    renderer.sprite = settings.scaleFrames[0];
                orbiters.Add(scale.transform);
            }

            int enemyProjectiles = 1 << CombatLayers.ProjectileLayer(Team.Enemy);
            for (float t = 0f; t < settings.orbitDuration; t += Time.deltaTime)
            {
                for (int i = 0; i < orbiters.Count; i++)
                {
                    float angle = (t * settings.orbitSpeed + (float)i / orbiters.Count) * Mathf.PI * 2f;
                    var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * settings.orbitRadius;
                    var orbiter = orbiters[i];
                    orbiter.localPosition = offset;
                    // Apunta hacia donde gira (tangente).
                    orbiter.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f);
                    Block(orbiter.position, enemyProjectiles);
                }
                yield return null;
            }

            var origins = orbiters.Select(o => (Vector2)o.position).ToList();
            foreach (var orbiter in orbiters)
            {
                if (orbiter != null)
                    Destroy(orbiter.gameObject);
            }
            orbiters.Clear();
            orbiting = false;
            Release(Targets(), origins);
        }

        void Block(Vector2 position, int mask)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(position, ScaleRadius, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var projectile = hits[i].GetComponentInParent<Projectile>();
                if (projectile == null || !projectile.isActiveAndEnabled)
                    continue;
                projectile.Despawn();
                Blocked++;
                FrameEffect.Play("ScleriteBlock", settings.blockFrames, 0.06f, SortingOrder, null, position);
            }
        }

        void OnDisable()
        {
            foreach (var orbiter in orbiters)
            {
                if (orbiter != null)
                    Destroy(orbiter.gameObject);
            }
            orbiters.Clear();
            orbiting = false;
        }
    }
}

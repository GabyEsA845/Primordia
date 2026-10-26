using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Púa de las Espinas de Hallucigenia en el suelo: sale, espera y se deshace al acabar su tiempo. Hace daño una vez a
    /// cada enemigo que la pisa. Con el Erizo, al acabar sale disparada hacia el enemigo más cercano.
    /// Cuadros: sale (0-2), espera (3-4 en bucle) y se deshace (5-7).
    /// </summary>
    public class GroundSpine : MonoBehaviour
    {
        const int EmergeFrames = 3;
        const int IdleFrames = 2;

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.08f;
        [SerializeField, Min(0.05f)] float radius = 0.4f;

        int damage;
        float spawnTime;
        float until;
        AttackData shot;
        float shotRange;
        Weapon source;
        bool fading;
        readonly HashSet<Health> struck = new();
        static readonly Collider2D[] hits = new Collider2D[8];

        public int Damage => damage;

        /// <summary>Se lanza al hacer daño a un enemigo, con su vida (el Lecho urticante de la Anémona envenena aquí).</summary>
        public event Action<Health> Struck;

        /// <summary>Lo mismo para cualquier púa (el Lecho urticante de la Anémona).</summary>
        public static event Action<GroundSpine, Health> AnyStruck;

        /// <summary>Púa que está quitando vida ahora mismo, o null (la Doble ración: los restos de quien mata una púa).</summary>
        public static GroundSpine Striking { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AnyStruck = null;
            Striking = null;
        }

        public static GroundSpine Spawn(GroundSpine prefab, Vector2 position, int damage, float duration, AttackData shot,
            float shotRange, Weapon source)
        {
            var spine = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity)
                : new GameObject("GroundSpine").AddComponent<GroundSpine>();
            spine.transform.position = position;
            spine.damage = damage;
            spine.spawnTime = Time.time;
            spine.until = Time.time + duration;
            spine.shot = shot;
            spine.shotRange = shotRange;
            spine.source = source;
            return spine;
        }

        internal void Configure(SpriteRenderer newBody, Sprite[] newFrames, float newFrameDuration)
        {
            body = newBody;
            frames = newFrames ?? Array.Empty<Sprite>();
            frameDuration = newFrameDuration;
        }

        void Update()
        {
            if (fading)
                return;
            Animate();
            Sting();
            if (Time.time >= until)
                StartCoroutine(Fade());
        }

        void Animate()
        {
            if (body == null || frames.Length == 0)
                return;
            int step = Mathf.FloorToInt((Time.time - spawnTime) / frameDuration);
            int frame = step < EmergeFrames ? step : EmergeFrames + (step - EmergeFrames) % IdleFrames;
            body.sprite = frames[Mathf.Min(frame, frames.Length - 1)];
        }

        void Sting()
        {
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead || !struck.Add(health))
                    continue;
                Striking = this;
                try { health.TakeDamage(damage); }
                finally { Striking = null; }
                Struck?.Invoke(health);
                AnyStruck?.Invoke(this, health);
            }
        }

        System.Collections.IEnumerator Fade()
        {
            fading = true;
            if (shot != null)
                Shoot();
            for (int frame = EmergeFrames + IdleFrames; frame < frames.Length && body != null; frame++)
            {
                body.sprite = frames[frame];
                yield return new WaitForSeconds(frameDuration);
            }
            Destroy(gameObject);
        }

        /// <summary>Erizo: sale disparada hacia el enemigo vivo más cercano a su alcance.</summary>
        void Shoot()
        {
            Health nearest = null;
            float best = shotRange * shotRange;
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, shotRange, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                float distance = ((Vector2)health.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = health;
                }
            }
            if (nearest == null || shot.ProjectilePrefab == null)
                return;
            var direction = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
            ProjectilePool.Get(shot.ProjectilePrefab).Launch(transform.position, direction, shot,
                CombatLayers.ProjectileLayer(Team.Player), source);
            Shot?.Invoke(direction);
        }

        /// <summary>Se lanza al salir disparada (Erizo), con la dirección.</summary>
        public event Action<Vector2> Shot;
    }
}

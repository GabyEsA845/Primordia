using System.Collections;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Nube del Saco de metano: flota a la deriva hacia donde apuntaba el jugador y se deshace sola a los 6 s. Explota con cualquier
    /// ataque del jugador (tiene 1 de vida en la capa de los enemigos, como el odre) y con el fuego o con otra explosión
    /// (<see cref="IFlammable"/>): 6 de daño en su radio a todo menos al jugador, prende lo inflamable y, con el Incendio, deja
    /// fuego unos segundos. Ver <see cref="MethaneSacMutation"/>.
    /// </summary>
    public class MethaneCloud : MonoBehaviour, IFlammable
    {
        [System.Serializable]
        public struct Settings
        {
            public int damage;
            public float lifetime;
            public float speed;
            public float explosionRadius;
            public float fireSeconds;
        }

        const float BodyRadius = 0.6f;
        const int Order = 14;
        const float FrameSeconds = 0.12f;

        static readonly List<MethaneCloud> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        /// <summary>Las nubes que hay ahora.</summary>
        public static IReadOnlyList<MethaneCloud> All => all;

        Settings settings;
        Vector2 direction;
        Health health;
        Sprite[] cloudFrames, fadeFrames, explodeFrames;
        SpriteRenderer body;
        float endTime;
        float phase;
        bool exploded;
        bool fading;

        public Vector2 Position => transform.position;

        public bool CanIgnite => !exploded && !fading;

        public bool Exploded => exploded;

        public Health Health => health;

        /// <summary>Suelta una nube en ese punto que deriva en esa dirección. Los cuadros pueden faltar (en las pruebas).</summary>
        public static MethaneCloud Spawn(Vector2 position, Vector2 direction, Settings settings, Sprite[] cloud, Sprite[] fade, Sprite[] explode)
        {
            var obj = new GameObject("MethaneCloud");
            obj.transform.position = position;
            int layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            if (layer >= 0)
                obj.layer = layer;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = Order;
            // El cuerpo recibe los golpes (por capa) pero no choca con nadie: ni empuja a los enemigos ni estorba al jugador.
            var collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = BodyRadius;
            collider.excludeLayers = LayerMask.GetMask(CombatLayers.Player, CombatLayers.Enemy);

            var life = obj.AddComponent<Health>();
            life.SetMax(1, healDifference: false);
            life.ResetHealth();

            var component = obj.AddComponent<MethaneCloud>();
            component.settings = settings;
            component.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            component.health = life;
            component.cloudFrames = cloud ?? System.Array.Empty<Sprite>();
            component.fadeFrames = fade ?? System.Array.Empty<Sprite>();
            component.explodeFrames = explode ?? System.Array.Empty<Sprite>();
            component.body = renderer;
            component.endTime = Time.time + settings.lifetime;
            component.phase = Random.value * 10f;
            life.Died += component.Explode;
            return component;
        }

        void OnEnable() => Flammables.Register(this);

        void OnDisable()
        {
            Flammables.Unregister(this);
            all.Remove(this);
        }

        void Start()
        {
            if (!all.Contains(this))
                all.Add(this);
        }

        void Update()
        {
            if (exploded)
                return;

            if (cloudFrames.Length > 0 && !fading)
                body.sprite = cloudFrames[Mathf.FloorToInt((Time.time + phase) / FrameSeconds) % cloudFrames.Length];

            // Deriva despacio; una pared la para.
            if (!fading)
            {
                float step = settings.speed * Time.deltaTime;
                if (!Blocked(step))
                    transform.position += (Vector3)(direction * step);
            }

            float fadeSeconds = fadeFrames.Length * FrameSeconds;
            if (!fading && Time.time >= endTime - fadeSeconds)
                StartCoroutine(Fade());
            if (!fading && Time.time >= endTime)
                Destroy(gameObject);
        }

        bool Blocked(float step)
        {
            var hits = Physics2D.RaycastAll(transform.position, direction, step + BodyRadius);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger || hit.collider.gameObject == gameObject)
                    continue;
                // Solo lo que bloquea de verdad: paredes y obstáculos (los personajes y los proyectiles no la paran).
                int layer = hit.collider.gameObject.layer;
                if (layer == LayerMask.NameToLayer(CombatLayers.Player) || layer == LayerMask.NameToLayer(CombatLayers.Enemy)
                    || layer == LayerMask.NameToLayer(CombatLayers.PlayerProjectile) || layer == LayerMask.NameToLayer(CombatLayers.EnemyProjectile))
                    continue;
                return true;
            }
            return false;
        }

        IEnumerator Fade()
        {
            fading = true;
            foreach (var frame in fadeFrames)
            {
                if (exploded)
                    yield break;
                body.sprite = frame;
                yield return new WaitForSeconds(FrameSeconds);
            }
            if (!exploded)
                Destroy(gameObject);
        }

        /// <summary>El fuego o una explosión cercana la hace estallar.</summary>
        public void Ignite() => Explode();

        void Explode()
        {
            if (exploded)
                return;
            exploded = true;
            Flammables.Unregister(this);
            Vector2 center = transform.position;
            SpriteFramePlayer.PlayOnce(explodeFrames, 0.06f, center, Order + 2, settings.explosionRadius / 2f);

            // A todo lo que tiene vida salvo al jugador: sus explosiones no le dañan.
            var seen = new HashSet<Health> { health };
            foreach (var hit in Physics2D.OverlapCircleAll(center, settings.explosionRadius))
            {
                if (hit.isTrigger)
                    continue;
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !seen.Add(target) || target.GetComponentInParent<PlayerController>() != null)
                    continue;
                target.TakeDamage(settings.damage);
            }

            Flammables.IgniteNear(center, settings.explosionRadius);
            if (settings.fireSeconds > 0f)
                GroundFire.Spawn(center + Vector2.down * 0.3f, settings.fireSeconds, harmsPlayer: false);
            Destroy(gameObject);
        }
    }
}

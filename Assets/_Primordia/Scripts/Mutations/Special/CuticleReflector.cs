using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="MarrellaCuticleMutation"/>: mientras el jugador esquiva, devuelve lo que le alcanza. Los
    /// proyectiles enemigos desaparecen y sale uno del jugador que vuelve buscando a quien lo lanzó (en abanico con el
    /// Prisma); a quien le golpea cuerpo a cuerpo o por contacto le hace daño y lo aturde, una vez por esquiva.
    /// </summary>
    public class CuticleReflector : MonoBehaviour, IHitReflector
    {
        internal struct Settings
        {
            public int projectileDamage;
            public int contactDamage;
            public float stunDuration;
            public int prismCount;
            public float prismSpread;
            public AttackData reflectedAttack;
            public Sprite[] reflectFrames;
            public Sprite[] stunFrames;
            public float frameDuration;
            public GameObject stunMarker;
        }

        /// <summary>Encima del jugador y de los enemigos.</summary>
        const int SortingOrder = 22;

        PlayerController controller;
        Settings settings;
        // Cada atacante se aturde una sola vez por esquiva (el contacto llega en cada cuadro).
        readonly HashSet<Health> stunnedThisDodge = new();

        /// <summary>Proyectiles y golpes devueltos (para las pruebas).</summary>
        public int ProjectilesReflected { get; private set; }
        public int HitsReflected { get; private set; }

        public int PrismCount => settings.prismCount;

        bool IsDodging => controller != null && controller.IsDodging;

        internal void Configure(PlayerController newController, Settings newSettings)
        {
            if (controller != null)
                controller.DodgeStarted -= OnDodgeStarted;

            controller = newController;
            settings = newSettings;
            if (controller != null)
                controller.DodgeStarted += OnDodgeStarted;
        }

        void OnDestroy()
        {
            if (controller != null)
                controller.DodgeStarted -= OnDodgeStarted;
        }

        void OnDodgeStarted() => stunnedThisDodge.Clear();

        public bool TryReflect(Projectile projectile)
        {
            if (!IsDodging || projectile == null)
                return false;

            ProjectilesReflected++;
            Flash();
            Sfx.Play(Sounds.CuticleReflect);

            var shooter = projectile.Source != null ? projectile.Source.GetComponentInParent<Health>() : null;
            if (shooter != null && shooter.IsDead)
                shooter = null;

            Vector2 origin = transform.position;
            Vector2 direction = shooter != null ? (Vector2)shooter.transform.position - origin : -projectile.Velocity;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            direction.Normalize();

            Launch(origin, direction, shooter);
            return true;
        }

        /// <summary>
        /// Sale el proyectil reflejado (o, con el Prisma, un abanico: el del centro va a por quien lo lanzó, los demás
        /// rectos).
        /// </summary>
        void Launch(Vector2 origin, Vector2 direction, Health shooter)
        {
            var attack = settings.reflectedAttack;
            if (attack == null || attack.ProjectilePrefab == null)
                return;

            int count = Mathf.Max(1, settings.prismCount);
            float step = count > 1 ? settings.prismSpread / (count - 1) : 0f;
            float first = count > 1 ? -settings.prismSpread / 2f : 0f;
            int layer = CombatLayers.ProjectileLayer(Team.Player);
            for (int i = 0; i < count; i++)
            {
                float angle = first + step * i;
                Vector2 shot = Quaternion.Euler(0f, 0f, angle) * direction;
                var projectile = ProjectilePool.Get(attack.ProjectilePrefab);
                projectile.Launch(origin, shot, attack, layer, null);
                bool center = Mathf.Abs(angle) < 0.01f;
                projectile.Redirect(settings.projectileDamage, center && shooter != null ? shooter.transform : null);
            }
        }

        public bool TryReflect(Health attacker)
        {
            if (!IsDodging || attacker == null || attacker.IsDead)
                return false;

            // Ya devuelto en esta esquiva: tampoco hace daño (el jugador es invulnerable mientras esquiva).
            if (!stunnedThisDodge.Add(attacker))
                return true;

            HitsReflected++;
            Flash();
            Sfx.Play(Sounds.CuticleStun);

            var collider = attacker.GetComponentInChildren<Collider2D>();
            Vector2 center = collider != null ? (Vector2)collider.bounds.center : (Vector2)attacker.transform.position;
            FrameEffect.Play("CuticleStun", settings.stunFrames, settings.frameDuration, SortingOrder, null, center);

            if (settings.stunDuration > 0f && attacker.TryGetComponent<EnemyAI>(out var enemy))
                enemy.Knockback(Vector2.zero, settings.stunDuration, settings.stunMarker);
            if (settings.contactDamage > 0)
                attacker.TakeDamage(settings.contactDamage);
            return true;
        }

        void Flash() => FrameEffect.Play("CuticleReflect", settings.reflectFrames, settings.frameDuration, SortingOrder, transform);
    }
}

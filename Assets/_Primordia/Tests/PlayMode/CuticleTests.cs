using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Cutícula de Marrella (especiales de las almejas, paso 3): esquiva justa. Mientras esquivas, un proyectil enemigo vuelve
    /// buscando a quien lo lanzó (6 de daño; II 8; III Prisma en 3) y un golpe de contacto aturde y hace 3 (II 4) una vez por
    /// esquiva. Sin esquivar no hace nada.
    /// </summary>
    public class CuticleTests
    {
        readonly List<Object> created = new();
        GameObject projectileTemplate;
        PlayerMutations mutations;
        PlayerController controller;
        MarrellaCuticleMutation cuticle;

        [SetUp]
        public void SetUp()
        {
            projectileTemplate = Track(new GameObject("ProjectileTemplate"));
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = projectileTemplate.AddComponent<Projectile>();

            var reflected = Track(ScriptableObject.CreateInstance<AttackData>());
            reflected.Configure(projectile, 1f, 1, 15f, 2f);
            reflected.ConfigureHoming(720f, 10f);

            var player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = new Vector2(2f, 0f);
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            player.AddComponent<Weapon>().Attack = Track(ScriptableObject.CreateInstance<AttackData>());
            controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            controller.DodgeDistance = 0.1f;
            mutations = player.AddComponent<PlayerMutations>();

            cuticle = Track(ScriptableObject.CreateInstance<MarrellaCuticleMutation>());
            cuticle.ConfigureBase("Cutícula de Marrella", "", MutationSlot.Skin, Color.white);
            cuticle.SetSpecial(true);
            cuticle.SetReflectedAttack(reflected);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
                Object.DestroyImmediate(projectile.gameObject);
            ProjectilePool.Clear();
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        CuticleReflector Reflector => mutations.GetComponent<CuticleReflector>();

        Health Enemy(Vector2 position)
        {
            var enemy = Track(new GameObject("Enemy"));
            enemy.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            enemy.transform.position = position;
            enemy.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            Physics2D.SyncTransforms();
            return health;
        }

        Weapon Shooter(Health enemy)
        {
            var weapon = enemy.gameObject.AddComponent<Weapon>();
            weapon.SetTeam(Team.Enemy);
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectileTemplate.GetComponent<Projectile>(), 1f, 1, 20f, 2f);
            weapon.Attack = attack;
            return weapon;
        }

        static Projectile[] PlayerProjectiles() => Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
            .Where(p => p.gameObject.layer == CombatLayers.ProjectileLayer(Team.Player)).ToArray();

        [UnityTest]
        public IEnumerator Projectile_WhileDodging_GoesBackToTheShooter()
        {
            mutations.Equip(cuticle);
            var enemy = Enemy(new Vector2(-1f, 0f));
            var weapon = Shooter(enemy);
            yield return null;

            Assert.IsTrue(controller.TryDodge());
            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(1, Reflector.ProjectilesReflected);
            Assert.AreEqual(mutations.Health.Max, mutations.Health.Current, "No le hace daño al jugador.");
            Assert.AreEqual(94, enemy.Current, "Vuelve buscando a quien lo lanzó y le hace 6.");
        }

        [UnityTest]
        public IEnumerator Projectile_WithoutDodging_IsNotReflected()
        {
            mutations.Equip(cuticle);
            var enemy = Enemy(new Vector2(-1f, 0f));
            var weapon = Shooter(enemy);
            yield return null;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(0, Reflector.ProjectilesReflected);
            Assert.Less(mutations.Health.Current, mutations.Health.Max);
            Assert.AreEqual(100, enemy.Current);
        }

        [UnityTest]
        public IEnumerator Contact_WhileDodging_HurtsTheAttacker_OncePerDodge()
        {
            mutations.Equip(cuticle);
            var enemy = Enemy(new Vector2(5f, 0f));
            yield return null;

            Assert.IsFalse(Reflector.TryReflect(enemy), "Sin esquivar no devuelve nada.");
            controller.TryDodge();
            Assert.IsTrue(Reflector.TryReflect(enemy));
            Assert.IsTrue(Reflector.TryReflect(enemy), "El contacto de cada cuadro tampoco hace daño...");
            Assert.AreEqual(97, enemy.Current, "...pero solo le devuelve 3 una vez por esquiva.");
            Assert.AreEqual(1, Reflector.HitsReflected);
        }

        [UnityTest]
        public IEnumerator ContactDamage_WhileDodging_GoesToTheAttacker()
        {
            mutations.Equip(cuticle);
            var enemy = Enemy(new Vector2(5f, 0f));
            var contact = enemy.gameObject.AddComponent<ContactDamage>();
            yield return null;

            controller.TryDodge();
            contact.SendMessage("TryDamage", mutations.GetComponent<Collider2D>());
            Assert.AreEqual(97, enemy.Current);
            Assert.AreEqual(mutations.Health.Max, mutations.Health.Current);
        }

        [UnityTest]
        public IEnumerator LevelTwo_HitsHarder()
        {
            mutations.Equip(cuticle);
            mutations.LevelUp(MutationSlot.Skin);
            var enemy = Enemy(new Vector2(5f, 0f));
            yield return null;

            controller.TryDodge();
            Reflector.TryReflect(enemy);
            Assert.AreEqual(96, enemy.Current);
        }

        [UnityTest]
        public IEnumerator Prism_SplitsTheReflectedProjectileInThree()
        {
            mutations.Equip(cuticle);
            mutations.LevelUp(MutationSlot.Skin);
            mutations.LevelUp(MutationSlot.Skin);
            var enemy = Enemy(new Vector2(-1f, 0f));
            var weapon = Shooter(enemy);
            yield return null;

            Assert.AreEqual(3, Reflector.PrismCount);
            controller.TryDodge();
            weapon.Fire(Vector2.right);
            // Llega en unos 0,13 s y la vuelta tarda 0,2 s: los reflejados aún vuelan.
            yield return new WaitForSeconds(0.17f);
            Assert.AreEqual(1, Reflector.ProjectilesReflected);
            Assert.AreEqual(3, PlayerProjectiles().Length, "Se divide en 3 en abanico.");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Escupitajo venenoso (Boca de la rana): la bola atraviesa a los enemigos con un daño pequeño, estalla al acabar su vida, al chocar
    /// con una pared y al disparar otra con la misma arma, y no estalla si la quitan sin más (cambio de sala, jefes).
    /// </summary>
    public class PoisonSpitTests
    {
        readonly List<Object> created = new();
        Weapon weapon;
        AttackData attack;

        [SetUp]
        public void SetUp()
        {
            var template = Track(new GameObject("SpitTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.2f;
            var projectile = template.AddComponent<Projectile>();
            template.AddComponent<PoisonSpitBall>();

            attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectile, fireRate: 0.4f, damage: 1, speed: 2f, lifetime: 0.6f);
            attack.SetPierce(99);
            attack.ConfigureSpit(0.5f, 3f, burstDamage: 6, burstRadius: 1.5f, burstPuddleSeconds: 0f);

            var shooter = Track(new GameObject("Shooter"));
            shooter.layer = LayerMask.NameToLayer(CombatLayers.Player);
            weapon = shooter.AddComponent<Weapon>();
            weapon.Attack = attack;
            shooter.transform.position = new Vector2(100f, 100f);
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

        Health Enemy(Vector2 position)
        {
            var enemy = Track(new GameObject("Enemy"));
            enemy.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            enemy.transform.position = position;
            enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            return health;
        }

        void FireFrom(Vector2 origin)
        {
            weapon.transform.position = origin;
            weapon.Fire(Vector2.right);
        }

        [UnityTest]
        public IEnumerator Ball_PiercesWithSmallDamage_AndBurstsWhenItsLifeEnds()
        {
            var inPath = Enemy(new Vector2(101f, 100f));
            var nearEnd = Enemy(new Vector2(101.6f, 101f));
            var far = Enemy(new Vector2(110f, 100f));
            yield return null;

            FireFrom(new Vector2(100f, 100f));
            yield return new WaitForSeconds(1f);

            Assert.AreEqual(93, inPath.Current, "1 al atravesarlo y 6 del estallido.");
            Assert.AreEqual(94, nearEnd.Current, "Solo el estallido, que le llega por el radio.");
            Assert.AreEqual(100, far.Current, "Lejos del estallido no le pasa nada.");
        }

        [UnityTest]
        public IEnumerator Ball_BurstsAgainstAWall()
        {
            var wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector2(101f, 100f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 3f);
            var enemy = Enemy(new Vector2(100.6f, 101f));
            yield return null;

            attack.SetLifetime(5f);
            FireFrom(new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.9f);

            Assert.That(enemy.Current, Is.InRange(88, 94), "Estalla al chocar con la pared (6) y deja veneno virulento, mucho antes de acabar su vida.");
        }

        [UnityTest]
        public IEnumerator FiringAnotherBall_BurstsTheFirst()
        {
            attack.SetLifetime(5f);
            var enemy = Enemy(new Vector2(100.5f, 101f));
            yield return null;

            FireFrom(new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(100, enemy.Current, "Aún vuela.");

            FireFrom(new Vector2(100f, 98f));
            yield return null;
            Assert.AreEqual(94, enemy.Current, "Al disparar otra, la primera estalla donde estaba.");
        }

        [UnityTest]
        public IEnumerator ClearingProjectiles_DoesNotBurst()
        {
            attack.SetLifetime(5f);
            var enemy = Enemy(new Vector2(100.5f, 101f));
            yield return null;

            FireFrom(new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.2f);
            Projectile.DespawnAll();
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(100, enemy.Current, "Quitarla sin más no la hace estallar.");
        }

        [UnityTest]
        public IEnumerator Camada_SplitsIntoFourSmallBallsOnBurst()
        {
            var miniPrefab = Track(new GameObject("MiniTemplate"));
            miniPrefab.SetActive(false);
            miniPrefab.AddComponent<Rigidbody2D>();
            miniPrefab.AddComponent<CircleCollider2D>().radius = 0.1f;
            var miniProjectile = miniPrefab.AddComponent<Projectile>();
            miniPrefab.AddComponent<PoisonSpitBall>();
            var mini = Track(ScriptableObject.CreateInstance<AttackData>());
            mini.Configure(miniProjectile, fireRate: 1f, damage: 0, speed: 2f, lifetime: 1f);
            mini.SetPierce(99);
            mini.ConfigureSpit(0.5f, 3f, burstDamage: 3, burstRadius: 0.8f, burstPuddleSeconds: 0f, mini: true);
            attack.ConfigureSpit(0.5f, 3f, burstDamage: 6, burstRadius: 1.5f, burstPuddleSeconds: 0f, splitCount: 4, splitAttack: mini);
            yield return null;

            FireFrom(new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.8f);

            int minis = 0;
            foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if (p.Attack == mini)
                    minis++;
            }
            Assert.AreEqual(4, minis, "El estallido suelta 4 bolitas en cruz.");
        }
    }
}

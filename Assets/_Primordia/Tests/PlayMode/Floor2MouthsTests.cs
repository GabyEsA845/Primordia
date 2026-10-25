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
    /// Bocas del piso 2. Cnidocitos: el arpón atraviesa a tantos enemigos como diga el ataque (sin dar dos veces al mismo) y
    /// el Ramillete dispara un abanico cada N disparos.
    /// </summary>
    public class Floor2MouthsTests
    {
        readonly List<Object> created = new();
        GameObject projectileTemplate;
        Weapon weapon;
        AttackData attack;

        [SetUp]
        public void SetUp()
        {
            projectileTemplate = Track(new GameObject("ProjectileTemplate"));
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = projectileTemplate.AddComponent<Projectile>();

            attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectile, fireRate: 3f, damage: 2, speed: 20f, lifetime: 1f);

            var shooter = Track(new GameObject("Shooter"));
            shooter.layer = LayerMask.NameToLayer(CombatLayers.Player);
            weapon = shooter.AddComponent<Weapon>();
            weapon.Attack = attack;
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
            enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 2f);
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            return health;
        }

        static int Flying() => Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;

        [UnityTest]
        public IEnumerator Harpoon_PiercesOneEnemy_ThenStopsAtTheNext()
        {
            attack.SetPierce(1);
            var first = Enemy(new Vector2(2f, 0f));
            var second = Enemy(new Vector2(4f, 0f));
            var third = Enemy(new Vector2(6f, 0f));
            yield return null;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(98, first.Current, "Le da una sola vez al atravesarlo.");
            Assert.AreEqual(98, second.Current, "Atraviesa a 1 y se queda en el segundo.");
            Assert.AreEqual(100, third.Current);
            Assert.AreEqual(0, Flying());
        }

        [UnityTest]
        public IEnumerator WithoutPierce_StopsAtTheFirst()
        {
            var first = Enemy(new Vector2(2f, 0f));
            var second = Enemy(new Vector2(4f, 0f));
            yield return null;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(98, first.Current);
            Assert.AreEqual(100, second.Current);
        }

        [Test]
        public void Volley_EveryFourthShot_FiresAFanOfThree()
        {
            attack.ConfigureVolley(4, 3, 30f);
            var counts = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                int before = Flying();
                weapon.Fire(Vector2.right);
                counts.Add(Flying() - before);
            }
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 3, 1, 1, 1, 3 }, counts);
        }
    }
}

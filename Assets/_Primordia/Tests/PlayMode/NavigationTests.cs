using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class NavigationTests
    {
        readonly List<Object> created = new();
        GameObject target;

        // Sala de 20×12 (de -10 a 10 y de -6 a 6) con una pared vertical en x = 3, de y = -4 a y = 3:
        // entre (0, 0) y (6, 0) no hay línea recta, pero se puede rodear por arriba o por abajo.
        static NavigationGrid WallGrid() =>
            new(new RectInt(-10, -6, 20, 12), cell => !(cell.x == 3 && cell.y >= -4 && cell.y <= 3));

        [SetUp]
        public void SetUp()
        {
            target = Track(new GameObject("Target"));
        }

        [TearDown]
        public void TearDown()
        {
            NavigationGrid.Current = null;
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
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

        [Test]
        public void LineOfSight_IsBlockedByWalls()
        {
            var grid = WallGrid();

            Assert.IsFalse(grid.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(6.5f, 0.5f)));
            Assert.IsTrue(grid.HasLineOfSight(new Vector2(0.5f, 5.5f), new Vector2(6.5f, 5.5f)));
            Assert.IsFalse(grid.HasLineOfSight(new Vector2(0.5f, 4.5f), new Vector2(6.5f, 4.5f), radius: 0.9f),
                "Un cuerpo ancho roza la punta de la pared.");
        }

        [Test]
        public void DirectionTo_GoesAroundTheWall()
        {
            var grid = WallGrid();
            var from = new Vector2(2.5f, 0.5f);

            var direction = grid.DirectionTo(from, new Vector2(6.5f, 0.5f), radius: 0.4f);

            Assert.Less(direction.x, 0.8f, "No va de frente contra la pared.");
            Assert.Greater(Mathf.Abs(direction.y), 0.5f, "Sube o baja para rodearla.");
        }

        [Test]
        public void DirectionTo_IsStraight_WhenThePathIsClear()
        {
            var grid = WallGrid();

            var direction = grid.DirectionTo(new Vector2(-5.5f, 0.5f), new Vector2(-1.5f, 0.5f), radius: 0.4f);

            Assert.AreEqual(Vector2.right, direction);
        }

        [Test]
        public void StepsTo_CountsTheWayAround()
        {
            var grid = WallGrid();

            // Recto serían 6 pasos; rodeando la pared por arriba (y = 4, más corto que por abajo) son 6 + 2 × 4.
            Assert.AreEqual(14, grid.StepsTo(new Vector2Int(0, 0), new Vector2Int(6, 0)));
        }

        [UnityTest]
        public IEnumerator Chaser_GoesAroundAWall()
        {
            NavigationGrid.Current = WallGrid();
            target.transform.position = new Vector3(6.5f, 0.5f, 0f);
            var enemy = CreateEnemy(EnemyBehaviour.Chaser, new Vector2(0.5f, 0.5f), attack: null);

            float maxOffset = 0f;
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                maxOffset = Mathf.Max(maxOffset, Mathf.Abs(enemy.transform.position.y - 0.5f));
            }

            Assert.Greater(maxOffset, 1f, "Se aparta de la línea recta para rodear la pared.");
        }

        [UnityTest]
        public IEnumerator Shooter_DoesNotFire_WithoutLineOfSight()
        {
            NavigationGrid.Current = WallGrid();
            target.transform.position = new Vector3(6.5f, 0.5f, 0f);
            var enemy = CreateEnemy(EnemyBehaviour.Shooter, new Vector2(0.5f, 0.5f), CreateAttack());

            yield return null;
            Assert.AreEqual(EnemyAI.State.Chase, enemy.GetComponent<EnemyAI>().Current, "Lo busca en vez de quedarse disparando.");

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(0, Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude).Length);
        }

        Enemy CreateEnemy(EnemyBehaviour behaviour, Vector2 position, AttackData attack)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(behaviour, maxHealth: 3, moveSpeed: 4f, detectionRange: 20f, contactDamage: 1,
                preferredDistance: 6f, attack: attack);

            var go = new GameObject("Enemy");
            go.transform.position = position;
            if (attack != null)
                go.AddComponent<Weapon>();
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>().Target = target.transform;
            enemy.SetData(data);
            return enemy;
        }

        AttackData CreateAttack()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), fireRate: 10f, damage: 1, speed: 10f, lifetime: 2f);
            return attack;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Enemigos del piso 3. Escarabajo bombardero: al tenerte a tiro se da la vuelta, avisa y suelta por detrás un cono de
    /// fuego que quema a los enemigos que pille; es inmune al fuego y su espalda recibe +50 % de daño.
    /// </summary>
    public class Floor3EnemiesTests
    {
        readonly List<Object> created = new();
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player") { tag = "Player" });
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = new Vector2(2.5f, 0f);
            player.AddComponent<Health>();
        }

        [TearDown]
        public void TearDown()
        {
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

        EnemyData Data(int health, float detection = 10f, int contactDamage = 1)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: health, moveSpeed: 3f, detectionRange: detection, contactDamage: contactDamage);
            return data;
        }

        T CreateEnemy<T>(EnemyData data, Vector2 position) where T : EnemyBrain
        {
            var go = Track(new GameObject("Enemy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>().Target = player.transform;
            var brain = go.AddComponent<T>();
            enemy.SetData(data);
            go.GetComponent<Rigidbody2D>().gravityScale = 0f;
            return brain;
        }

        Enemy CreateDummy(Vector2 position)
        {
            var go = Track(new GameObject("Dummy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            var enemy = go.AddComponent<Enemy>();
            enemy.SetData(Data(14));
            if (go.TryGetComponent<Rigidbody2D>(out var body))
                body.gravityScale = 0f;
            return enemy;
        }

        [UnityTest]
        public IEnumerator Beetle_InRange_TurnsWarnsAndSpraysTowardYou()
        {
            var beetle = CreateEnemy<BeetleBrain>(Data(12), Vector2.zero);
            yield return null;
            yield return null;

            Assert.IsTrue(beetle.Current is BeetleBrain.Phase.Turn or BeetleBrain.Phase.Warn,
                $"Al tenerte a tiro se gira y avisa (está en {beetle.Current}).");

            for (float t = 0f; t < 2f && beetle.Current != BeetleBrain.Phase.Spray; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(BeetleBrain.Phase.Spray, beetle.Current);
            Assert.Greater(beetle.SprayDirection.x, 0.9f, "El cono sale hacia ti.");
            Assert.Less(beetle.Heading.x, -0.9f, "La cabeza mira al lado contrario.");
        }

        [UnityTest]
        public IEnumerator Beetle_Spray_BurnsEnemiesInTheCone_NotOutside()
        {
            var beetle = CreateEnemy<BeetleBrain>(Data(12), Vector2.zero);
            var inside = CreateDummy(new Vector2(1.5f, 0f));
            var outside = CreateDummy(new Vector2(-1.5f, 3f));
            for (float t = 0f; t < 2.5f && beetle.Current != BeetleBrain.Phase.Spray; t += Time.deltaTime)
                yield return null;
            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(inside.GetComponent<Burn>(), "Quien está en el cono arde.");
            Assert.IsNull(outside.GetComponent<Burn>(), "Quien está fuera, no.");
            Assert.IsNull(beetle.GetComponent<Burn>(), "El escarabajo es inmune al fuego.");
        }

        [UnityTest]
        public IEnumerator Beetle_Back_TakesFiftyPercentMoreDamage()
        {
            // Sin objetivo a la vista pasea; se le fija la cabeza hacia la derecha y se compara de frente y por la espalda.
            player.transform.position = new Vector2(50f, 0f);
            var front = CreateEnemy<BeetleBrain>(Data(20, detection: 1f), Vector2.zero);
            var back = CreateEnemy<BeetleBrain>(Data(20, detection: 1f), new Vector2(0f, 20f));
            yield return null;

            player.transform.position = new Vector2(3f, 0f);
            front.GetComponent<Health>().TakeDamage(4);
            player.transform.position = new Vector2(-3f, 20f);
            back.GetComponent<Health>().TakeDamage(4);

            Assert.AreEqual(16, front.GetComponent<Health>().Current, "De frente, el daño normal.");
            Assert.AreEqual(14, back.GetComponent<Health>().Current, "Por la espalda, +50 %.");
        }
    }
}

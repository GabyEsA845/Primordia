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
    /// Enemigos del piso 2. Trilobite: al ver al jugador avisa y embiste en línea recta; si choca, se da la vuelta y queda
    /// aturdido; su cabeza blindada para la mitad del daño de frente, pero no panza arriba.
    /// </summary>
    public class Floor2EnemiesTests
    {
        readonly List<Object> created = new();
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player") { tag = "Player" });
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = new Vector2(6f, 0f);
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

        EnemyData Data(int health, float detection = 10f)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: health, moveSpeed: 3f, detectionRange: detection, contactDamage: 1);
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

        [UnityTest]
        public IEnumerator Trilobite_Warns_ThenChargesStraightAtYou()
        {
            var trilobite = CreateEnemy<TrilobiteBrain>(Data(14), Vector2.zero);
            yield return null;
            yield return null;

            Assert.AreEqual(TrilobiteBrain.Phase.Warn, trilobite.Current, "Al verte, avisa.");
            Assert.Less(trilobite.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 0.01f, "Quieto mientras avisa.");

            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(TrilobiteBrain.Phase.Charge, trilobite.Current);
            Assert.Greater(trilobite.GetComponent<Rigidbody2D>().linearVelocity.x, 5f, "Embiste muy rápido hacia ti.");
        }

        [UnityTest]
        public IEnumerator Trilobite_HittingAWall_FlipsAndIsStunned()
        {
            player.transform.position = new Vector2(0f, 6f);
            var wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector2(0f, 3f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(6f, 1f);
            var trilobite = CreateEnemy<TrilobiteBrain>(Data(14, detection: 12f), Vector2.zero);
            // Ve al jugador por encima de la pared (sin rejilla de navegación, la vista no la tapa).
            yield return new WaitForSeconds(1.2f);

            Assert.IsTrue(trilobite.Current is TrilobiteBrain.Phase.Flip or TrilobiteBrain.Phase.Stunned,
                $"Al chocar se da la vuelta y queda aturdido (está en {trilobite.Current}).");
        }

        [UnityTest]
        public IEnumerator Trilobite_Armor_HalvesFrontDamage_ButNotFromBehind()
        {
            var trilobite = CreateEnemy<TrilobiteBrain>(Data(100), Vector2.zero);
            var health = trilobite.GetComponent<Health>();
            yield return null;
            yield return null;

            // Avisando mira al jugador: el golpe le entra de frente.
            Assert.IsTrue(trilobite.Shields(player.transform.position));
            int before = health.Current;
            health.TakeDamage(10);
            Assert.AreEqual(5, before - health.Current, "La cabeza blindada para la mitad.");

            player.transform.position = new Vector2(-6f, 0f);
            Assert.IsFalse(trilobite.Shields(player.transform.position), "Por detrás no.");
        }
    }
}

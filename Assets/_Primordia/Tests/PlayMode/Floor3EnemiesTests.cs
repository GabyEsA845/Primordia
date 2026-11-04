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
        public IEnumerator Toad_InRange_SpitsAtYou()
        {
            player.transform.position = new Vector2(5f, 0f);
            var toad = CreateEnemy<ToadBrain>(Data(10), Vector2.zero);
            Vector2? aimed = null;
            toad.Spat += aim => aimed = aim;
            for (float t = 0f; t < 1.5f && aimed == null; t += Time.deltaTime)
                yield return null;

            Assert.IsNotNull(aimed, "Te escupe al verte a tiro.");
            Assert.AreEqual(5f, aimed.Value.x, 0.01f, "Apunta a donde estás.");
        }

        [UnityTest]
        public IEnumerator Toad_Close_InflatesAndBurstsHurtingYou()
        {
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var playerHealth = player.GetComponent<Health>();
            player.transform.position = new Vector2(1.2f, 0f);
            var toad = CreateEnemy<ToadBrain>(Data(10), Vector2.zero);
            // El enemigo se destruye al morir: la vida se guarda antes para poder mirarla después.
            var toadHealth = toad.GetComponent<Health>();
            yield return null;
            yield return null;
            Assert.AreEqual(ToadBrain.Phase.Inflate, toad.Current, "Pegado a él, se hincha.");

            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(toad.HasBurst, "Y revienta.");
            Assert.IsTrue(toadHealth.IsDead, "El sapo muere al reventar.");
            Assert.Less(playerHealth.Current, playerHealth.Max, "El reventón te hace daño.");
        }

        [UnityTest]
        public IEnumerator Toad_LowHealth_InflatesAndRushesYou()
        {
            player.transform.position = new Vector2(5f, 0f);
            var toad = CreateEnemy<ToadBrain>(Data(10), Vector2.zero);
            yield return null;
            toad.GetComponent<Health>().TakeDamage(8);

            for (float t = 0f; t < 1.5f && toad.Current != ToadBrain.Phase.Rush; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(ToadBrain.Phase.Rush, toad.Current, "Con poca vida se hincha y corre hacia ti.");

            for (float t = 0f; t < 3f && !toad.HasBurst; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(toad.HasBurst, "Revienta al llegar o al acabar el tiempo.");
        }

        [UnityTest]
        public IEnumerator Toad_Submerge_IsImmuneAndComesBack()
        {
            player.transform.position = new Vector2(7f, 0f);
            var toad = CreateEnemy<ToadBrain>(Data(10), Vector2.zero);
            yield return null;
            var health = toad.GetComponent<Health>();

            toad.Submerge();
            Assert.IsTrue(toad.IsSubmerged);
            Assert.IsTrue(health.IsInvulnerable, "Bajo el lodo no recibe daño.");
            Assert.IsFalse(toad.GetComponent<Collider2D>().enabled, "Ni lo tocan los proyectiles.");

            for (float t = 0f; t < 4f && toad.Current != ToadBrain.Phase.Surfaced; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(ToadBrain.Phase.Surfaced, toad.Current, "Vuelve a asomar.");
            Assert.IsFalse(health.IsInvulnerable);
            Assert.IsTrue(toad.GetComponent<Collider2D>().enabled);
        }

        [UnityTest]
        public IEnumerator Medullosa_InRange_AimsThenFiresAtWhereYouStood()
        {
            player.transform.position = new Vector2(6f, 0f);
            var medullosa = CreateEnemy<MedullosaBrain>(Data(16, contactDamage: 0), Vector2.zero);
            Vector2? fired = null;
            medullosa.Fired += point => fired = point;
            bool aimed = false;
            for (float t = 0f; t < 3f && fired == null; t += Time.deltaTime)
            {
                aimed |= medullosa.Current == MedullosaBrain.Phase.Aim;
                yield return null;
            }

            Assert.IsTrue(aimed, "Primero apunta.");
            Assert.IsNotNull(fired, "Y lanza la vaina.");
            Assert.AreEqual(6f, fired.Value.x, 0.01f, "A donde estás.");
            Assert.AreEqual(RigidbodyConstraints2D.FreezeAll, medullosa.GetComponent<Rigidbody2D>().constraints, "Es una planta: nada la mueve.");
        }

        [UnityTest]
        public IEnumerator Medullosa_OutOfRange_DoesNotFire()
        {
            player.transform.position = new Vector2(40f, 0f);
            var medullosa = CreateEnemy<MedullosaBrain>(Data(16, contactDamage: 0), Vector2.zero);
            bool fired = false;
            medullosa.Fired += _ => fired = true;
            yield return new WaitForSeconds(2.5f);

            Assert.IsFalse(fired, "Sin verte a tiro no dispara.");
            Assert.AreEqual(MedullosaBrain.Phase.Idle, medullosa.Current);
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

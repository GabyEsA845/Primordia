using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Trampas del piso 3. Planta carnívora: atrapa 1 s al que se le acerca (al jugador lo hiere al soltarlo; a un enemigo se lo
    /// come) y se marchita. Enjambre de efímeras: flota hacia el jugador, lo pica, no tiene cuerpo y solo muere con fuego. Ninguna
    /// es un enemigo (no cuentan para limpiar la sala).
    /// </summary>
    public class Floor3TrapsTests
    {
        readonly List<Object> created = new();

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
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

        GameObject CreatePlayer(Vector2 position)
        {
            var player = Track(new GameObject("Player") { tag = "Player" });
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = position;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            player.AddComponent<Health>();
            return player;
        }

        Enemy CreateDummy(Vector2 position)
        {
            var go = Track(new GameObject("Dummy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var enemy = go.AddComponent<Enemy>();
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 8, moveSpeed: 0f, detectionRange: 5f, contactDamage: 0);
            enemy.SetData(data);
            if (go.TryGetComponent<Rigidbody2D>(out var body))
                body.gravityScale = 0f;
            return enemy;
        }

        CarnivorousPlant CreatePlant(Vector2 position)
        {
            var go = Track(new GameObject("Plant"));
            go.transform.position = position;
            return go.AddComponent<CarnivorousPlant>();
        }

        [UnityTest]
        public IEnumerator Plant_EatsEnemiesThatComeClose()
        {
            var plant = CreatePlant(Vector2.zero);
            var dummy = CreateDummy(plant.Mouth);
            var health = dummy.GetComponent<Health>();
            Enemy eaten = null;
            plant.Ate += enemy => eaten = enemy;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(CarnivorousPlant.State.Chew, plant.Current, "Lo atrapa.");
            Assert.IsFalse(health.IsDead, "Aún lo mastica.");

            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(health.IsDead, "Pasado el segundo, se lo come.");
            Assert.IsNotNull(eaten);
        }

        [UnityTest]
        public IEnumerator Plant_HoldsThePlayer_ThenHurtsHim()
        {
            var player = CreatePlayer(new Vector2(0.5f, 0.7f));
            var health = player.GetComponent<Health>();
            var plant = CreatePlant(Vector2.zero);
            Health released = null;
            plant.Released += hurt => released = hurt;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(CarnivorousPlant.State.Chew, plant.Current, "Atrapa al jugador.");
            Assert.AreEqual(health.Max, health.Current, "Aún no le ha hecho daño.");

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(health.Max - 1, health.Current, "Al soltarlo, le hace daño.");
            Assert.AreSame(health, released);
        }

        [UnityTest]
        public IEnumerator Plant_Wilted_StopsBiting()
        {
            var plant = CreatePlant(Vector2.zero);
            plant.Wither();
            var dummy = CreateDummy(plant.Mouth);
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(CarnivorousPlant.State.Wilted, plant.Current);
            Assert.IsFalse(dummy.GetComponent<Health>().IsDead, "Marchita, ya no muerde.");
            Assert.IsNull(plant.GetComponent<Enemy>(), "No es un enemigo: no cuenta para limpiar la sala.");
        }

        [UnityTest]
        public IEnumerator Mayflies_DriftToYou_StingOncePerSecond_AndHaveNoBody()
        {
            var player = CreatePlayer(Vector2.zero);
            var health = player.GetComponent<Health>();
            var go = Track(new GameObject("Mayflies"));
            go.transform.position = new Vector2(4f, 0f);
            var swarm = go.AddComponent<MayflySwarm>();
            yield return new WaitForSeconds(1f);

            Assert.Less(go.transform.position.x, 3f, "Flota hacia ti.");
            Assert.IsNull(go.GetComponent<Collider2D>(), "Sin cuerpo: los proyectiles y golpes la atraviesan.");
            Assert.IsNull(go.GetComponent<Enemy>(), "No es un enemigo.");

            go.transform.position = Vector2.zero;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(health.Max - 1, health.Current, "Te pica al atravesarla.");
            Assert.IsTrue(swarm.CanIgnite);
        }

        [UnityTest]
        public IEnumerator Mayflies_OnlyDieWithFire()
        {
            var go = Track(new GameObject("Mayflies"));
            var swarm = go.AddComponent<MayflySwarm>();
            yield return null;
            Assert.IsTrue(swarm.CanIgnite);

            swarm.Ignite();
            Assert.IsFalse(swarm.CanIgnite, "Ya arde.");
            Assert.IsTrue(swarm.IsBurning);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(swarm == null, "Cae como ascuas y no vuelve.");
        }

        [Test]
        public void PickTraps_RespectsMinDepth_AndChance()
        {
            var config = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var plant = Track(new GameObject("Plant"));
            var swarm = Track(new GameObject("Swarm"));
            config.SetTraps(new[]
            {
                new FloorConfig.TrapEntry(plant, minDepth: 2, chance: 1f, maxPerRoom: 2),
                new FloorConfig.TrapEntry(swarm, minDepth: 3, chance: 0f),
            });

            Assert.AreEqual(0, EncounterPlanner.PickTraps(config, depth: 1, new System.Random(1)).Count, "Aún no puede salir.");
            var picked = EncounterPlanner.PickTraps(config, depth: 3, new System.Random(1));
            Assert.AreEqual(2, picked.Count(t => t.prefab == plant), "Con probabilidad 1 salen las del tope.");
            Assert.AreEqual(0, picked.Count(t => t.prefab == swarm), "Con probabilidad 0, ninguna.");
        }
    }
}

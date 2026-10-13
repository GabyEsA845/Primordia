using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Biomass;
using Primordia.Enemies;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    public class BiomassTests
    {
        readonly List<Object> created = new();
        PlayerBiomass wallet;

        [TearDown]
        public void TearDown()
        {
            foreach (var pickup in Object.FindObjectsByType<BiomassPickup>(FindObjectsInactive.Include))
                Object.DestroyImmediate(pickup.gameObject);
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);

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

        void CreatePlayer(Vector2 position)
        {
            var player = Track(new GameObject("Player") { tag = "Player" });
            player.transform.position = position;
            wallet = player.AddComponent<PlayerBiomass>();
        }

        EnemyData CreateData(float smallChance, float bigChance, int fixedValue = 0)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 1, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            data.ConfigureBiomass(smallChance, bigChance, fixedValue);
            return data;
        }

        /// <summary>Pieza "prefab" lejos de todo, para instanciarla como haría la sala.</summary>
        BiomassPickup CreatePickupTemplate(int value)
        {
            var go = Track(new GameObject($"Biomass_{value}"));
            go.transform.position = new Vector3(500f, 500f, 0f);
            var pickup = go.AddComponent<BiomassPickup>();
            pickup.Configure(value, null);
            pickup.SetMagnet(0f, 0.4f);
            return pickup;
        }

        BiomassPickup CreatePickup(Vector2 position, int value = 1)
        {
            var go = Track(new GameObject("Biomass"));
            go.transform.position = position;
            var pickup = go.AddComponent<BiomassPickup>();
            pickup.Configure(value, null);
            return pickup;
        }

        (Room room, RoomBiomass biomass, Enemy[] enemies) CreateRoom(EnemyData data, int enemyCount, int minimum = 2)
        {
            var roomObject = Track(new GameObject("Room"));
            roomObject.AddComponent<BoxCollider2D>().size = new Vector2(10f, 10f);
            var room = roomObject.AddComponent<Room>();
            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);

            for (int i = 0; i < enemyCount; i++)
            {
                var go = new GameObject("Enemy");
                go.transform.SetParent(enemiesRoot, false);
                go.transform.position = new Vector2(i * 2f - 2f, 2f);
                go.AddComponent<Enemy>().SetData(data);
            }

            room.Configure(enemiesRoot, null);
            var biomass = roomObject.AddComponent<RoomBiomass>();
            biomass.Configure(CreatePickupTemplate(RoomBiomass.SmallValue), CreatePickupTemplate(RoomBiomass.BigValue), minimum, 0f);

            room.Begin();
            return (room, biomass, enemiesRoot.GetComponentsInChildren<Enemy>());
        }

        static void KillAll(IEnumerable<Enemy> enemies)
        {
            foreach (var enemy in enemies)
                enemy.Health.TakeDamage(enemy.Health.Max);
        }

        // ---------- Monedero ----------

        [Test]
        public void Wallet_AddsAndSpends()
        {
            CreatePlayer(Vector2.zero);
            var changes = new List<int>();
            wallet.Changed += (_, change) => changes.Add(change);

            wallet.Add(7);
            Assert.IsFalse(wallet.TrySpend(8));
            Assert.IsTrue(wallet.TrySpend(5));

            Assert.AreEqual(2, wallet.Amount);
            CollectionAssert.AreEqual(new[] { 7, -5 }, changes);
        }

        // ---------- Tiradas ----------

        [Test]
        public void Roll_BigFirst_ThenSmall_ThenNothing()
        {
            var data = CreateData(smallChance: 0.5f, bigChance: 0.1f);

            Assert.AreEqual(5, RoomBiomass.Roll(data, 0.05f));
            Assert.AreEqual(1, RoomBiomass.Roll(data, 0.3f));
            Assert.AreEqual(0, RoomBiomass.Roll(data, 0.7f));
        }

        [Test]
        public void Roll_AddsTheFixedBiomass()
        {
            var boss = CreateData(smallChance: 0f, bigChance: 0f, fixedValue: 15);

            Assert.AreEqual(15, RoomBiomass.Roll(boss, 0.99f));
        }

        [Test]
        public void Split_UsesBigPieces_AndAHandfulForLargeAmounts()
        {
            Assert.AreEqual((0, 1), RoomBiomass.Split(1));
            Assert.AreEqual((1, 0), RoomBiomass.Split(5));
            Assert.AreEqual((1, 1), RoomBiomass.Split(6));
            Assert.AreEqual((2, 5), RoomBiomass.Split(15));
        }

        // ---------- Sala ----------

        [Test]
        public void LastEnemy_CompletesTheRoomMinimum()
        {
            CreatePlayer(new Vector2(50f, 50f));
            var (_, biomass, enemies) = CreateRoom(CreateData(0f, 0f), enemyCount: 3, minimum: 2);

            KillAll(enemies);

            Assert.AreEqual(2, biomass.DroppedValue);
            Assert.AreEqual(2, biomass.OnFloor.Count);
        }

        [Test]
        public void Minimum_IsNotAdded_WhenEnemiesAlreadyDroppedEnough()
        {
            CreatePlayer(new Vector2(50f, 50f));
            var (_, biomass, enemies) = CreateRoom(CreateData(0f, bigChance: 1f), enemyCount: 2, minimum: 2);

            KillAll(enemies);

            Assert.AreEqual(10, biomass.DroppedValue);
            Assert.AreEqual(2, biomass.OnFloor.Count(p => p.Value == RoomBiomass.BigValue));
        }

        [Test]
        public void BossMinions_DropNothing()
        {
            CreatePlayer(new Vector2(50f, 50f));
            var (_, biomass, enemies) = CreateRoom(CreateData(0f, bigChance: 1f), enemyCount: 2, minimum: 0);
            foreach (var enemy in enemies)
                enemy.DropsBiomass = false;

            KillAll(enemies);

            Assert.AreEqual(0, biomass.DroppedValue);
        }

        [UnityTest]
        public IEnumerator ClearingTheRoom_SendsAllBiomassToThePlayer()
        {
            CreatePlayer(new Vector2(20f, 0f));
            var (room, biomass, enemies) = CreateRoom(CreateData(0f, bigChance: 1f), enemyCount: 2);

            KillAll(enemies);
            Assert.AreEqual(Room.RoomState.Cleared, room.State);

            for (float t = 0f; t < 3f && wallet.Amount < 10; t += Time.deltaTime)
                yield return null;

            Assert.AreEqual(10, wallet.Amount);
            Assert.AreEqual(0, biomass.OnFloor.Count);
        }

        // ---------- Recogida ----------

        [UnityTest]
        public IEnumerator Magnet_PullsNearbyBiomass_AndCollectsIt()
        {
            CreatePlayer(Vector2.zero);
            CreatePickup(new Vector2(1.2f, 0f), value: 1);

            for (float t = 0f; t < 1f && wallet.Amount == 0; t += Time.deltaTime)
                yield return null;

            Assert.AreEqual(1, wallet.Amount);
        }

        [UnityTest]
        public IEnumerator FarBiomass_Stays_UntilAttracted()
        {
            CreatePlayer(Vector2.zero);
            var pickup = CreatePickup(new Vector2(6f, 0f), value: 5);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, wallet.Amount);
            Assert.AreEqual(6f, pickup.transform.position.x, 0.001f);

            pickup.Attract();
            for (float t = 0f; t < 2f && wallet.Amount == 0; t += Time.deltaTime)
                yield return null;

            Assert.AreEqual(5, wallet.Amount);
        }

        // ---------- HUD ----------

        [UnityTest]
        public IEnumerator Counter_ShowsTheAmount()
        {
            CreatePlayer(Vector2.zero);
            var counterObject = Track(new GameObject("Counter", typeof(RectTransform)));
            var label = new GameObject("Amount", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(counterObject.transform, false);
            var counter = counterObject.AddComponent<BiomassCounter>();
            counter.Configure(wallet, null, label);

            yield return null;
            Assert.AreEqual("0", counter.AmountText);

            wallet.Add(42);
            Assert.AreEqual("42", counter.AmountText);
        }
    }
}

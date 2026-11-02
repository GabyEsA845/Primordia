using System.Collections;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Rooms;
using Primordia.Shop;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class RoomHeartsTests
    {
        [SetUp]
        public void SetUp() => RoomHearts.ResetLuck();

        [TearDown]
        public void TearDown()
        {
            RoomHearts.ResetLuck();
            foreach (var pickup in Object.FindObjectsByType<RoomHeartPickup>(FindObjectsInactive.Include))
                Object.DestroyImmediate(pickup.gameObject);
            foreach (var tagged in GameObject.FindGameObjectsWithTag("Player"))
                Object.DestroyImmediate(tagged);
        }

        [Test]
        public void ChanceStartsAt12AndRisesFourPerMissedRoom()
        {
            Assert.AreEqual(0.12f, RoomHearts.Chance, 1e-5f);
            Assert.IsFalse(RoomHearts.Roll(0.99f));
            Assert.AreEqual(0.16f, RoomHearts.Chance, 1e-5f);
            Assert.IsFalse(RoomHearts.Roll(0.99f));
            Assert.AreEqual(0.20f, RoomHearts.Chance, 1e-5f);
        }

        [Test]
        public void DroppingAHeartResetsTheProtection()
        {
            RoomHearts.Roll(0.99f);
            RoomHearts.Roll(0.99f);
            Assert.IsTrue(RoomHearts.Roll(0.0f));
            Assert.AreEqual(0, RoomHearts.MissedRooms);
            Assert.AreEqual(0.12f, RoomHearts.Chance, 1e-5f);
        }

        [Test]
        public void ResetLuckClearsTheCounter()
        {
            RoomHearts.Roll(0.99f);
            RoomHearts.ResetLuck();
            Assert.AreEqual(0.12f, RoomHearts.Chance, 1e-5f);
        }

        [Test]
        public void ShopPricesGrow1Point5TimesPerFloorFromTheBasePrice()
        {
            Assert.AreEqual(15, ShopRoom.ScaledPrice(15, 1));
            Assert.AreEqual(15, ShopRoom.ScaledPrice(15, 0));
            Assert.AreEqual(23, ShopRoom.ScaledPrice(15, 2));
            Assert.AreEqual(34, ShopRoom.ScaledPrice(15, 3));
            Assert.AreEqual(30, ShopRoom.ScaledPrice(20, 2));
            Assert.AreEqual(45, ShopRoom.ScaledPrice(30, 2));
            Assert.AreEqual(8, ShopRoom.ScaledPrice(5, 2));
            Assert.AreEqual(38, ShopRoom.ScaledPrice(25, 2));
        }

        [UnityTest]
        public IEnumerator PickupStaysWhenHealthIsFullAndHealsHalfAHeartWhenHurt()
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 0f, 0f);
            var health = player.AddComponent<Health>();
            yield return null;

            var pickup = RoomHeartPickup.Create(Vector2.zero, null);
            yield return null;
            yield return null;
            Assert.IsFalse(pickup.Collected, "Con la vida llena el corazón se queda en el suelo.");

            health.TakeDamage(1);
            int hurt = health.Current;
            yield return null;
            yield return null;
            Assert.IsTrue(pickup.Collected);
            Assert.AreEqual(hurt + 1, health.Current);
        }
    }
}

using System.Collections;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class TeleportTests
    {
        readonly System.Collections.Generic.List<Object> created = new();
        Rigidbody2D player;

        [SetUp]
        public void SetUp()
        {
            TeleportNetwork.Reset();
            var playerObject = new GameObject("Player") { tag = "Player" };
            created.Add(playerObject);
            player = playerObject.AddComponent<Rigidbody2D>();
            player.bodyType = RigidbodyType2D.Kinematic;
            playerObject.AddComponent<CircleCollider2D>().radius = 0.3f;
            playerObject.AddComponent<Primordia.Core.Health>();
            player.position = new Vector2(0f, -500f);
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
            TeleportNetwork.Reset();
        }

        (Room room, Teleporter teleporter) CreateRoom(Vector2 center, bool withTeleporter)
        {
            var roomObject = new GameObject("Room");
            created.Add(roomObject);
            roomObject.transform.position = center;
            roomObject.AddComponent<BoxCollider2D>().size = new Vector2(10f, 10f);
            var room = roomObject.AddComponent<Room>();
            Teleporter teleporter = null;
            if (withTeleporter)
            {
                var child = new GameObject("Teleporter");
                child.transform.SetParent(roomObject.transform, false);
                teleporter = child.AddComponent<Teleporter>();
            }
            return (room, teleporter);
        }

        IEnumerator Enter(Vector2 position)
        {
            player.position = position;
            player.transform.position = position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnteringARoomWakesItsTeleporterAndDestinationsExcludeTheCurrentRoom()
        {
            var (_, a) = CreateRoom(new Vector2(0f, 0f), true);
            var (_, b) = CreateRoom(new Vector2(100f, 0f), true);
            yield return null;
            Assert.IsFalse(a.IsActive);

            yield return Enter(new Vector2(0f, 0f));
            Assert.IsTrue(a.IsActive);
            Assert.IsFalse(b.IsActive);
            Assert.IsEmpty(TeleportNetwork.Destinations());

            yield return Enter(new Vector2(100f, 0f));
            Assert.IsTrue(b.IsActive);
            CollectionAssert.AreEqual(new[] { a }, TeleportNetwork.Destinations());
        }

        [UnityTest]
        public IEnumerator TravelFromARoomWithATeleporterLeavesNoReturnMark()
        {
            var (_, a) = CreateRoom(new Vector2(0f, 0f), true);
            var (_, b) = CreateRoom(new Vector2(100f, 0f), true);
            yield return null;
            yield return Enter(new Vector2(0f, 0f));
            yield return Enter(new Vector2(100f, 0f));
            Assert.IsTrue(TeleportNetwork.CanTravel());

            Assert.IsTrue(TeleportNetwork.TravelTo(a));
            Assert.IsTrue(TeleportNetwork.IsTraveling);
            Assert.IsFalse(TeleportNetwork.TravelTo(a), "No se puede viajar dos veces a la vez.");
            yield return new WaitForSecondsRealtime(0.9f);

            Assert.IsFalse(TeleportNetwork.IsTraveling);
            Assert.AreEqual(a.Position, player.position);
            Assert.IsFalse(TeleportNetwork.HasReturn);
        }

        [UnityTest]
        public IEnumerator TravelFromARoomWithoutTeleporterMarksReturnAndTravelBackClearsIt()
        {
            var (_, a) = CreateRoom(new Vector2(0f, 0f), true);
            var (_, _) = CreateRoom(new Vector2(200f, 0f), false);
            yield return null;
            yield return Enter(new Vector2(0f, 0f));
            yield return Enter(new Vector2(203f, 2f));
            Assert.IsFalse(TeleportNetwork.TravelBack(), "Sin marca no hay regreso.");

            Assert.IsTrue(TeleportNetwork.TravelTo(a));
            yield return new WaitForSecondsRealtime(0.9f);
            Assert.IsTrue(TeleportNetwork.HasReturn);
            Assert.AreEqual(new Vector2(203f, 2f), TeleportNetwork.ReturnPosition);
            Assert.AreEqual(a.Position, player.position);

            yield return Enter(a.Position);
            Assert.IsTrue(TeleportNetwork.TravelBack());
            yield return new WaitForSecondsRealtime(0.9f);
            Assert.AreEqual(new Vector2(203f, 2f), player.position);
            Assert.IsFalse(TeleportNetwork.HasReturn);
        }

        [UnityTest]
        public IEnumerator CannotTravelFromARoomAtCombat()
        {
            var (room, _) = CreateRoom(new Vector2(0f, 0f), true);
            var (_, b) = CreateRoom(new Vector2(100f, 0f), true);
            yield return null;
            yield return Enter(new Vector2(100f, 0f));
            yield return Enter(new Vector2(0f, 0f));
            b.Activate();
            Assert.IsTrue(TeleportNetwork.CanTravel());

            // Una sala con enemigos vivos está en combate hasta limpiarla.
            typeof(Room).GetProperty("State").SetValue(room, Room.RoomState.Combat);
            Assert.IsFalse(TeleportNetwork.CanTravel());
            Assert.IsFalse(TeleportNetwork.TravelTo(b));
        }
    }
}

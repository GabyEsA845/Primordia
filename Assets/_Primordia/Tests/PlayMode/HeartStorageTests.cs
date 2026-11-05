#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Almacén de corazones (vaina de semillas de la tienda) y cucaracha ladrona del piso 3 (paso 6, bloque 3).</summary>
    public class HeartStorageTests
    {
        readonly List<Object> created = new();

        [SetUp]
        public void SetUp() => HeartStorage.Reset(3);

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            foreach (var thief in Object.FindObjectsByType<HeartThief>(FindObjectsInactive.Include))
                Object.DestroyImmediate(thief.gameObject);
            HeartStorage.Reset(0);
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        Health Player(int damage = 0)
        {
            var player = Track(new GameObject("Player"));
            player.tag = "Player";
            var health = player.AddComponent<Health>();
            if (damage > 0)
                health.TakeDamage(damage);
            return health;
        }

        [Test]
        public void Capacity_IsFourAtFloorThree_AndTwoMorePerFloor_AndNoneBefore()
        {
            Assert.AreEqual(0, HeartStorage.CapacityOf(1));
            Assert.AreEqual(0, HeartStorage.CapacityOf(2));
            Assert.AreEqual(4, HeartStorage.CapacityOf(3));
            Assert.AreEqual(6, HeartStorage.CapacityOf(4));
            Assert.AreEqual(10, HeartStorage.CapacityOf(6));
        }

        [Test]
        public void Store_FillsUpToTheCapacity_Take_EmptiesIt_AndANewFloorStartsEmpty()
        {
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(HeartStorage.Store());
            Assert.IsTrue(HeartStorage.IsFull);
            Assert.IsFalse(HeartStorage.Store(), "Lleno: el corazón se queda en el suelo.");
            Assert.IsTrue(HeartStorage.Take());
            Assert.AreEqual(3, HeartStorage.Stored);
            HeartStorage.Reset(4);
            Assert.AreEqual(0, HeartStorage.Stored, "Los corazones no salen del piso.");
            Assert.AreEqual(6, HeartStorage.Capacity);
        }

        [Test]
        public void NoStorageBeforeFloorThree()
        {
            HeartStorage.Reset(2);
            Assert.IsFalse(HeartStorage.Exists);
            Assert.IsFalse(HeartStorage.Store());
        }

        [UnityTest]
        public IEnumerator Heart_WithFullHealth_CanBeSentToThePod_ButNotWhenHurtOrWhenTheStorageIsFull()
        {
            var health = Player();
            var heart = RoomHeartPickup.Create(new Vector2(5f, 0f), null);
            created.Add(heart.gameObject);
            yield return null;
            Assert.IsTrue(heart.IsAvailable, "Vida llena: se puede enviar.");

            heart.Interact(health.gameObject);
            Assert.AreEqual(1, HeartStorage.Stored);
            Assert.IsTrue(heart.Collected);

            // Con vida que curar, el imán lo da: no se envía.
            health.TakeDamage(2);
            var other = RoomHeartPickup.Create(new Vector2(30f, 0f), null);
            created.Add(other.gameObject);
            yield return null;
            Assert.IsFalse(other.IsAvailable);

            // Almacén lleno: se queda en el suelo.
            health.Heal(2);
            while (!HeartStorage.IsFull)
                HeartStorage.Store();
            yield return null;
            Assert.IsFalse(other.IsAvailable);
        }

        [Test]
        public void Pod_GivesHalfAHeartBack_OnlyWhenYouAreHurt()
        {
            HeartStorage.Store();
            HeartStorage.Store();
            var art = HeartStorageArt.Current;
            Assert.IsNotNull(art, "falta el arte: Primordia > Floor 3 > Importar almacén de corazones y cucaracha.");
            var pod = HeartPod.Create(Track(new GameObject("Shop")).transform, Vector2.zero, System.Array.Empty<Sprite>());
            Assert.IsNotNull(pod);

            var full = Player();
            pod.Interact(full.gameObject);
            Assert.AreEqual(2, HeartStorage.Stored, "Con la vida llena no se gasta.");

            var hurt = Player(2);
            int before = hurt.Current;
            pod.Interact(hurt.gameObject);
            Assert.AreEqual(1, HeartStorage.Stored);
            Assert.AreEqual(before + 1, hurt.Current, "Cura medio corazón.");
        }

        [UnityTest]
        public IEnumerator Thief_StealsTheHeartAndHides_AndCannotBeKilled()
        {
            Assert.IsNotNull(HeartStorageArt.Current, "falta el arte: Primordia > Floor 3 > Importar almacén de corazones y cucaracha.");
            Player();
            var heart = RoomHeartPickup.Create(new Vector2(4f, 0f), null);
            created.Add(heart.gameObject);
            yield return null;

            var cracks = new List<Vector2> { new(0f, 0f), new(14f, 0f), new(0f, 8f) };
            var thief = HeartThief.Send(heart, cracks, Track(new GameObject("Room")).transform);
            Assert.IsNotNull(thief);
            Assert.IsNull(thief.GetComponent<Health>(), "No se puede matar: no tiene vida.");
            Assert.IsNull(thief.GetComponent<Collider2D>(), "Ni cuerpo que golpear.");

            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(heart == null || heart.Collected, "Se ha llevado el corazón.");
            yield return new WaitForSeconds(5f);
            Assert.IsTrue(thief == null || thief.IsDone, "Y se ha escondido en su grieta.");
        }

        [Test]
        public void ShopPod_AlwaysSitsInTheFreeBottomRightCorner_AwayFromTheTeleporterPedestalsAndDoors()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<FloorConfig>("Assets/_Primordia/Data/Rooms/Floor_3.asset");
            Assert.IsNotNull(config);
            for (int seed = 1; seed <= 25; seed++)
            {
                var parent = new GameObject("Floor").transform;
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var built = FloorBuilder.Build(layout, config, parent, new System.Random(seed));
                var shopRoom = built.Rooms.Find(r => r.GetComponentInChildren<Primordia.Shop.ShopRoom>(true) != null);
                Assert.IsNotNull(shopRoom, $"semilla {seed}: sin tienda");
                var pod = shopRoom.GetComponentInChildren<HeartPod>(true);
                Assert.IsNotNull(pod, $"semilla {seed}: la tienda del piso 3 debe tener su vaina.");

                Vector2 at = pod.transform.position + Vector3.up * 0.5f;
                var teleporter = shopRoom.GetComponentInChildren<Teleporter>(true);
                if (teleporter != null)
                    Assert.GreaterOrEqual(Vector2.Distance(at, teleporter.transform.position), 3.4f, $"semilla {seed}: pegada al teletransporte.");
                // Suelo liso a su alrededor (sin paredes ni obstáculos) y abajo a la derecha de la sala.
                var mapRoom = built.MapRooms.Find(m => m.Room == shopRoom);
                var rect = mapRoom.Rect;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        Assert.AreEqual(Primordia.Rooms.FloorMap.Kind.Floor, built.Map.Get(Mathf.FloorToInt(at.x) + dx, Mathf.FloorToInt(at.y) + dy),
                            $"semilla {seed}: la vaina no está en suelo libre.");
                Assert.Greater(at.x, rect.center.x + 4f, $"semilla {seed}: a la derecha.");
                Assert.Less(at.y, rect.center.y, $"semilla {seed}: abajo.");

                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
            }
        }
    }
}
#endif

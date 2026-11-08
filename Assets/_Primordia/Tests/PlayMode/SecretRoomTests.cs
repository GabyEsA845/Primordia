using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Sala secreta del piso 3: antesala en el último tercio con su cámara, muro de raíces, flores moradas y semilla.</summary>
    public class SecretRoomTests
    {
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_3.asset";

        FloorConfig config;
        Transform parent;
        FloorLayout layout;
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            Assert.IsNotNull(config, "falta Floor_3: Primordia > Floor 3 > Crear salas especiales.");
        }

        [TearDown]
        public void TearDown()
        {
            if (parent != null)
                Object.DestroyImmediate(parent.gameObject);
            layout?.ReleaseGenerated();
            if (player != null)
                Object.DestroyImmediate(player);
        }

        void Build(int seed)
        {
            parent = new GameObject("Floor").transform;
            layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
            FloorBuilder.Build(layout, config, parent, new System.Random(seed));
        }

        Health NewPlayer()
        {
            player = new GameObject("Player") { tag = "Player" };
            return player.AddComponent<Health>();
        }

        [Test]
        public void Layout_AlwaysHasTheAntechamberInTheLastThird_WithItsChamberAboveIt()
        {
            int found = 0;
            for (int seed = 1; seed <= 30; seed++)
            {
                var generated = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var antechamber = generated.Rooms.FirstOrDefault(room => room.Special is { secretChamber: not null });
                if (antechamber != null)
                {
                    found++;
                    var chamber = generated.Rooms.Single(room => room.Special is { secretReward: true });
                    Assert.AreEqual(antechamber.Cell + Vector2Int.up, chamber.Cell, $"semilla {seed}: la cámara va justo encima.");
                    Assert.IsTrue(generated.Links.Contains((antechamber, chamber)), $"semilla {seed}: antesala y cámara enlazadas.");
                    // La puerta de arriba de la antesala es solo la de la cámara.
                    Assert.AreEqual(1, antechamber.Doors.Count(d => d == Vector2Int.up), $"semilla {seed}: una sola puerta de arriba.");
                    var combat = generated.Rooms.Where(room => room.Role == RoomRole.Combat).Max(room => room.Depth);
                    Assert.Greater(antechamber.Depth * 3, combat * 2, $"semilla {seed}: último tercio.");
                }
                generated.ReleaseGenerated();
            }
            Assert.GreaterOrEqual(found, 28, "Sale en (casi) todas las partidas.");
        }

        [Test]
        public void Build_PutsTheRootWallOnTheAntechamber_AndBlocksItsDoor()
        {
            for (int seed = 1; seed <= 6; seed++)
            {
                Build(seed);
                var wall = parent.GetComponentInChildren<RootWall>(true);
                Assert.IsNotNull(wall, $"semilla {seed}: falta el muro de raíces.");
                Assert.IsFalse(wall.IsOpen);
                Assert.IsTrue(wall.GetComponent<Collider2D>().enabled, "Cierra el paso.");
                Assert.IsNotNull(parent.GetComponentInChildren<SecretReward>(true), $"semilla {seed}: falta la recompensa de la cámara.");
                Assert.AreEqual(PurpleTrail.FlowerCount, parent.GetComponentsInChildren<PurpleFlower>(true).Length, $"semilla {seed}: tres flores.");
                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
                parent = null;
                layout = null;
            }
        }

        [UnityTest]
        public IEnumerator RootWall_WithAPurpleHeart_SpendsItAndOpens()
        {
            Build(3);
            var wall = parent.GetComponentInChildren<RootWall>(true);
            var health = NewPlayer();

            wall.Interact(player);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(wall.IsOpen, "Sin corazón ni semilla no se abre.");

            health.AddPurpleHeart();
            wall.Interact(player);
            Assert.AreEqual(0, health.PurpleHearts, "El corazón se queda en el hueco.");
            yield return new WaitForSeconds(3f);
            Assert.IsTrue(wall.IsOpen);
            Assert.IsFalse(wall.GetComponent<Collider2D>().enabled, "Ya no cierra el paso.");
        }

        [UnityTest]
        public IEnumerator RootWall_WithTheSeed_KeepsThePurpleHeart()
        {
            Build(3);
            var wall = parent.GetComponentInChildren<RootWall>(true);
            var health = NewPlayer();
            health.AddPurpleHeart();
            health.PurpleSeed = true;

            wall.Interact(player);
            Assert.IsFalse(health.PurpleSeed, "Se gasta la semilla.");
            Assert.AreEqual(1, health.PurpleHearts, "El corazón sigue parando un golpe.");
            yield return new WaitForSeconds(3.5f);
            Assert.IsTrue(wall.IsOpen);
        }

        [UnityTest]
        public IEnumerator PurpleFlowers_AppearOnlyWithABrokenHeart_AndThreeGiveTheSeed()
        {
            Build(2);
            var health = NewPlayer();
            var flowers = parent.GetComponentsInChildren<PurpleFlower>(true);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(flowers.All(flower => !flower.gameObject.activeSelf), "Sin haber cogido nunca el corazón, no se ven.");

            health.AddPurpleHeart();
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(flowers.All(flower => !flower.gameObject.activeSelf), "Con el corazón intacto, tampoco.");

            health.PurpleHearts = 0;
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(flowers.All(flower => flower.gameObject.activeSelf), "Roto el corazón, salen las tres.");

            foreach (var flower in flowers)
                flower.Interact(player);
            Assert.IsTrue(health.PurpleSeed, "Con las tres, la semilla.");
        }
    }
}

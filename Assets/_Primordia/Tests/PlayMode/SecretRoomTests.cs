using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Sala secreta y jefes opcionales del piso 3: antesala en el Bosque humeante, muro de raíces, flores moradas y semilla, paso a la
    /// arena del jefe secreto y el Nido de la Megarachne.
    /// </summary>
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
        public void Layout_AlwaysHasTheAntechamberInTheSmokeBand_WithTheSecretArenaApart()
        {
            int found = 0;
            for (int seed = 1; seed <= 30; seed++)
            {
                var generated = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var antechamber = generated.Rooms.FirstOrDefault(room => room.Special is { secretBoss: not null });
                if (antechamber != null)
                {
                    found++;
                    var arena = generated.Detached.Single();
                    Assert.AreEqual("Boss_Lepidodendron", arena.Special.Value.lairBoss.name, $"semilla {seed}");
                    Assert.IsFalse(generated.Rooms.Contains(arena), $"semilla {seed}: la arena va aparte de la rejilla.");
                    // La puerta de arriba de la antesala queda para el muro de raíces.
                    Assert.IsFalse(antechamber.Doors.Contains(Vector2Int.up), $"semilla {seed}: sin pasillo por arriba.");
                    Assert.AreEqual(1, SmokeBandOf(generated, antechamber), $"semilla {seed}: Bosque humeante.");
                }
                generated.ReleaseGenerated();
            }
            Assert.GreaterOrEqual(found, 28, "Sale en (casi) todas las partidas.");
        }

        int SmokeBandOf(FloorLayout generated, LayoutRoom room)
        {
            int deepest = generated.Rooms.Where(other => other.Role == RoomRole.Combat).Max(other => other.Depth);
            return FloorBuilder.BandOf(room.Depth, deepest, config.BandThemes.Length);
        }

        [Test]
        public void Layout_HasTheNestInTheSmokeBand_AboutOneInThreeTimes()
        {
            const int Runs = 300;
            int found = 0;
            for (int seed = 1; seed <= Runs; seed++)
            {
                var generated = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var nests = generated.Rooms.Where(room => room.Role == RoomRole.Lair).ToList();
                Assert.LessOrEqual(nests.Count, 1, $"semilla {seed}");
                if (nests.Count == 1)
                {
                    found++;
                    Assert.AreEqual("Boss_Megarachne", nests[0].Special.Value.lairBoss.name, $"semilla {seed}");
                    Assert.AreEqual(1, SmokeBandOf(generated, nests[0]), $"semilla {seed}: Bosque humeante.");
                    Assert.AreEqual(1, nests[0].Doors.Count, $"semilla {seed}: lateral sin salida.");
                    Assert.IsFalse(nests[0].Reward, "Su recompensa es la del minijefe.");
                }
                generated.ReleaseGenerated();
            }
            Assert.That(found / (float)Runs, Is.InRange(0.23f, 0.43f));
        }

        [Test]
        public void Build_PutsTheMegarachneAloneInItsNest()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                Build(seed);
                if (layout.Rooms.All(room => room.Role != RoomRole.Lair))
                {
                    Object.DestroyImmediate(parent.gameObject);
                    layout.ReleaseGenerated();
                    parent = null;
                    layout = null;
                    continue;
                }
                var boss = parent.GetComponentInChildren<Primordia.Enemies.MegarachneBoss>(true);
                Assert.IsNotNull(boss);
                var nest = boss.transform.parent.parent;
                Assert.AreEqual(1, nest.GetComponentsInChildren<Primordia.Enemies.Enemy>(true).Length, "Sin enemigos sueltos.");
                Assert.AreEqual(3, nest.GetComponentsInChildren<MethaneSac>(true).Length, "Solo sus tres odres, sin los del escenario.");
                Assert.IsNull(nest.GetComponent<RoomHearts>());
                return;
            }
            Assert.Fail("Ninguna semilla sacó el Nido.");
        }

        [UnityTest]
        public IEnumerator SecretPassage_CrossingTheOpenWall_TakesThePlayerToTheArena_AndBack()
        {
            Build(3);
            var wall = parent.GetComponentInChildren<RootWall>(true);
            var passage = parent.GetComponentInChildren<SecretPassage>(true);
            Assert.IsFalse(passage.Exit.gameObject.activeSelf, "La salida aparece con el jefe vencido.");
            var health = NewPlayer();
            health.AddPurpleHeart();
            wall.Interact(player);
            yield return new WaitForSeconds(3f);

            player.transform.position = passage.ReturnPoint + Vector2.up * 3.6f;
            yield return new WaitForSeconds(1.5f);
            Assert.Less(Vector2.Distance(player.transform.position, passage.ArenaEntry), 0.5f, "Al principio del pasillo.");

            // La arena está encima del mapa: andar por ella en la misma columna que el muro no vuelve a cruzarlo.
            var above = new Vector3(wall.transform.position.x, passage.ArenaEntry.y, 0f);
            player.transform.position = above;
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(wall.Crossing);
            Assert.Less(Vector2.Distance(player.transform.position, above), 0.1f, "No se repite la transición.");

            passage.Return(player);
            yield return new WaitForSeconds(1.5f);
            Assert.Less(Vector2.Distance(player.transform.position, passage.ReturnPoint), 0.5f, "De vuelta bajo el muro.");
            Assert.IsFalse(passage.Travelling, "No vuelve a cruzar solo.");
        }

        [Test]
        public void Layout_PicksOneOfTheTwoFinalBosses_WithItsOwnRoom()
        {
            CollectionAssert.AreEquivalent(new[] { "Boss_Meganeura", "Boss_Arthropleura" }, config.Bosses.Select(b => b.prefab.name));
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int seed = 1; seed <= 40; seed++)
            {
                var generated = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var boss = generated.Rooms.Single(room => room.Role == RoomRole.Boss);
                Assert.AreSame(generated.BossChoice.room, boss.Template, $"semilla {seed}");
                seen.Add(generated.BossChoice.prefab.name);
                generated.ReleaseGenerated();
            }
            Assert.AreEqual(2, seen.Count, "Salen los dos jefes.");
        }

        [Test]
        public void Build_PutsEachFinalBossInItsRoom()
        {
            var seen = new System.Collections.Generic.HashSet<System.Type>();
            for (int seed = 1; seed <= 20 && seen.Count < 2; seed++)
            {
                Build(seed);
                // Solo la sala del jefe final: el Nido y la arena secreta tienen el suyo.
                var final = parent.GetComponentsInChildren<Room>(true).Single(room => room.name == "Room_Boss");
                foreach (var boss in final.GetComponentsInChildren<MonoBehaviour>(true).OfType<Primordia.Enemies.IBoss>())
                    seen.Add(boss.GetType());
                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
                parent = null;
                layout = null;
            }
            CollectionAssert.AreEquivalent(
                new[] { typeof(Primordia.Enemies.MeganeuraBoss), typeof(Primordia.Enemies.ArthropleuraBoss) }, seen);
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
                Assert.IsNotNull(parent.GetComponentInChildren<SecretPassage>(true), $"semilla {seed}: falta el paso a la arena.");
                Assert.IsNotNull(parent.GetComponentInChildren<Primordia.Enemies.LepidodendronBoss>(true), $"semilla {seed}: falta el jefe.");
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

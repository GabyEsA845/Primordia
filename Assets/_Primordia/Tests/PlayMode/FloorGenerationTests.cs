using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    public class FloorGenerationTests
    {
        readonly List<Object> created = new();
        FloorConfig config;
        Enemy chaser;
        Enemy shooter;

        [SetUp]
        public void SetUp()
        {
            chaser = CreateEnemyTemplate("Chaser");
            shooter = CreateEnemyTemplate("Shooter");

            config = Track(ScriptableObject.CreateInstance<FloorConfig>());
            config.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal), Template(RoomLayouts.Pillars), Template(RoomLayouts.Cross),
                    Template(RoomLayouts.LShape), Template(RoomLayouts.Large) },
                new[] { Template(RoomLayouts.Small), Template(RoomLayouts.Pillars) },
                new Vector2Int(4, 5), new Vector2Int(1, 2));
            config.ConfigureEnemies(new[]
            {
                new FloorConfig.EnemyEntry(chaser, cost: 1),
                new FloorConfig.EnemyEntry(shooter, cost: 2, minDepth: 2)
            }, null);
            config.ConfigureRewards(null, new[] { 2, 4 });
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

        RoomTemplate Template(string name)
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.name = name;
            template.SetLayout(RoomLayouts.All[name]);
            return template;
        }

        Enemy CreateEnemyTemplate(string name)
        {
            var go = Track(new GameObject(name));
            go.SetActive(false);
            return go.AddComponent<Enemy>();
        }

        static FloorLayout Generate(FloorConfig config, int seed) => FloorLayoutGenerator.Generate(config, new System.Random(seed));

        [Test]
        public void Layout_KeepsItsShape_ForManySeeds()
        {
            for (int seed = 1; seed <= 300; seed++)
            {
                var layout = Generate(config, seed);
                string context = $"semilla {seed}";

                Assert.AreEqual(layout.Rooms.Count, layout.Rooms.Select(r => r.Cell).Distinct().Count(), $"Celdas repetidas ({context}).");
                foreach (var (from, to) in layout.Links)
                    Assert.AreEqual(1, Mathf.Abs(to.Cell.x - from.Cell.x) + Mathf.Abs(to.Cell.y - from.Cell.y), $"Pasillo entre celdas no vecinas ({context}).");

                int combat = layout.Rooms.Count(r => r.Role == RoomRole.Combat);
                int sides = layout.Rooms.Count(r => r.Role == RoomRole.Side);
                Assert.That(combat, Is.InRange(4, 5), context);
                Assert.That(sides, Is.InRange(1, 2), context);

                Assert.AreEqual(Vector2Int.zero, layout.Start.Cell, context);
                Assert.AreEqual(1, layout.Start.Doors.Count, $"El inicio tiene una sola puerta ({context}).");
                Assert.AreEqual(1, layout.Boss.Doors.Count, $"El jefe está al final, sin otra salida ({context}).");
                Assert.AreEqual(combat + 1, layout.Boss.Depth, context);

                foreach (var side in layout.Rooms.Where(r => r.Role == RoomRole.Side))
                {
                    Assert.AreEqual(1, side.Doors.Count, $"Las laterales no tienen salida ({context}).");
                    var parent = layout.Links.Single(link => link.to == side).from;
                    Assert.AreEqual(RoomRole.Combat, parent.Role, $"Las laterales cuelgan de salas de combate ({context}).");
                    Assert.IsTrue(side.Reward, context);
                }

                foreach (var room in layout.Rooms.Where(r => r.Role == RoomRole.Combat))
                    Assert.AreEqual(room.Depth == 2 || room.Depth == 4, room.Reward, $"Recompensa en la sala {room.Depth} ({context}).");
            }
        }

        [Test]
        public void Layout_IsTheSame_ForTheSameSeed()
        {
            var first = Generate(config, 1234);
            var second = Generate(config, 1234);

            CollectionAssert.AreEqual(first.Rooms.Select(r => r.Cell), second.Rooms.Select(r => r.Cell));
            CollectionAssert.AreEqual(first.Rooms.Select(r => r.Template), second.Rooms.Select(r => r.Template));
        }

        [Test]
        public void Layout_DoesNotRepeatAShape_InConsecutiveCombatRooms()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                var path = Generate(config, seed).Rooms.Where(r => r.Role == RoomRole.Combat).OrderBy(r => r.Depth).ToList();
                for (int i = 1; i < path.Count; i++)
                    Assert.AreNotSame(path[i - 1].Template, path[i].Template, $"semilla {seed}");
            }
        }

        [Test]
        public void Budget_GrowsWithDepth_AndWithFloorArea()
        {
            int normal = 26 * 12;

            Assert.AreEqual(3, EncounterPlanner.Budget(config, depth: 1, normal));
            Assert.Greater(EncounterPlanner.Budget(config, depth: 4, normal), EncounterPlanner.Budget(config, depth: 2, normal));
            Assert.Greater(EncounterPlanner.Budget(config, depth: 2, 38 * 20), EncounterPlanner.Budget(config, depth: 2, normal));
        }

        [Test]
        public void PickEnemies_SpendsAtMostTheBudget_AndRespectsMinDepth()
        {
            for (int seed = 1; seed <= 50; seed++)
            {
                var random = new System.Random(seed);
                var first = EncounterPlanner.PickEnemies(config, depth: 1, budget: 3, random);
                Assert.IsTrue(first.All(e => e == chaser), "Los tiradores salen desde la segunda sala.");
                Assert.AreEqual(3, first.Count);

                var later = EncounterPlanner.PickEnemies(config, depth: 3, budget: 6, random);
                int spent = later.Sum(e => e == shooter ? 2 : 1);
                Assert.AreEqual(6, spent);
            }
        }

        [Test]
        public void PickEnemies_RespectsMaxPerRoom()
        {
            var sponge = CreateEnemyTemplate("Sponge");
            config.ConfigureEnemies(new[]
            {
                new FloorConfig.EnemyEntry(sponge, cost: 1, maxPerRoom: 1),
                new FloorConfig.EnemyEntry(shooter, cost: 2)
            }, null);

            for (int seed = 1; seed <= 50; seed++)
            {
                var picked = EncounterPlanner.PickEnemies(config, depth: 3, budget: 9, new System.Random(seed));
                Assert.LessOrEqual(picked.Count(e => e == sponge), 1, $"semilla {seed}");
            }

            // Con solo la esponja, sale una aunque sobre presupuesto.
            config.ConfigureEnemies(new[] { new FloorConfig.EnemyEntry(sponge, cost: 1, maxPerRoom: 1) }, null);
            Assert.AreEqual(1, EncounterPlanner.PickEnemies(config, depth: 3, budget: 9, new System.Random(1)).Count);
        }

        [Test]
        public void PickEnemies_MudEnemies_OnlyComeInRoomsWithMud()
        {
            var toad = CreateEnemyTemplate("Toad");
            config.ConfigureEnemies(new[]
            {
                new FloorConfig.EnemyEntry(toad, cost: 1, needsMud: true),
                new FloorConfig.EnemyEntry(shooter, cost: 2)
            }, null);

            for (int seed = 1; seed <= 30; seed++)
            {
                Assert.IsFalse(EncounterPlanner.PickEnemies(config, depth: 3, budget: 9, new System.Random(seed), roomHasMud: false).Contains(toad),
                    $"Sin lodo no sale el sapo (semilla {seed}).");
            }
            Assert.IsTrue(Enumerable.Range(1, 30).Any(seed => EncounterPlanner.PickEnemies(config, depth: 3, budget: 9, new System.Random(seed), roomHasMud: true).Contains(toad)),
                "Con lodo sí puede salir.");
        }

        [Test]
        public void MudSideCells_AreFloorNearMud_NotOnItNorOnTheShore()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout(string.Join("\n", new[]
            {
                "............",
                "............",
                "............",
                "............",
                "............",
                "............",
                ".......mm...",
                ".......mm...",
                "............",
                "............",
            }));

            Assert.IsTrue(EncounterPlanner.HasMud(template));
            var cells = EncounterPlanner.MudSideCells(template, new Vector2Int[0]);
            Assert.IsNotEmpty(cells);
            foreach (var cell in cells)
            {
                Assert.AreEqual(RoomTemplate.Mark.Floor, template.Get(cell.x, cell.y));
                int distance = Mathf.Max(Mathf.Max(7 - cell.x, cell.x - 8, 0), Mathf.Max(2 - cell.y, cell.y - 3, 0));
                Assert.GreaterOrEqual(distance, 2, "No en la orilla.");
                Assert.LessOrEqual(distance, 4, "Pero cerca del lodo.");
            }
        }

        [Test]
        public void Spawn_GroupEntries_CreateTheWholeGroup_ForOneCost()
        {
            var fish = CreateEnemyTemplate("Fish");
            config.ConfigureEnemies(new[] { new FloorConfig.EnemyEntry(fish, cost: 2, groupSize: 5) }, null);

            var picked = EncounterPlanner.PickEnemies(config, depth: 1, budget: 3, new System.Random(1));
            Assert.AreEqual(1, picked.Count, "El grupo cuesta 2 una sola vez.");

            var parent = Track(new GameObject("Enemies")).transform;
            var spawned = EncounterPlanner.Spawn(config, picked[0], new Vector2(10f, 5f), parent);
            foreach (var enemy in spawned)
                Track(enemy.gameObject);
            Assert.AreEqual(5, spawned.Count);
            Assert.IsTrue(spawned.All(e => Vector2.Distance(e.transform.position, new Vector2(10f, 5f)) < 1f), "Juntos, en corro.");
            Assert.IsTrue(spawned.All(e => e.transform.parent == parent));
        }

        [Test]
        public void SpawnCells_AvoidDoorsAndWalls_AndPositionsAreSpacedOut()
        {
            var template = Template(RoomLayouts.Normal);
            var doors = template.DoorCells(Vector2Int.left).Concat(template.DoorCells(Vector2Int.up)).ToList();

            var cells = EncounterPlanner.SpawnCells(template, doors);
            Assert.IsNotEmpty(cells);
            foreach (var cell in cells)
            {
                Assert.IsTrue(doors.All(door => Vector2.Distance(door, cell) >= 5f));
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        Assert.AreEqual(RoomTemplate.Mark.Floor, template.Get(cell.x + dx, cell.y + dy));
            }

            var positions = EncounterPlanner.Positions(cells, 8, new System.Random(7));
            Assert.AreEqual(8, positions.Count);
            for (int i = 0; i < positions.Count; i++)
                for (int j = i + 1; j < positions.Count; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(positions[i], positions[j]), 2f);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Cuevas generadas, túneles irregulares y camino que baja (piso 2, entrega 1b).</summary>
    public class CaveGeneratorTests
    {
        static readonly Vector2Int[] Sizes = { new(26, 18), new(30, 20), new(34, 22), new(38, 24), new(44, 26) };

        readonly List<Object> created = new();
        FloorConfig floor2;

        [SetUp]
        public void SetUp()
        {
            floor2 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor2.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal) }, new[] { Template(RoomLayouts.Small) }, new Vector2Int(10, 12), new Vector2Int(2, 3));
            floor2.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor2.ConfigureRewards(null, new[] { 2, 4, 6, 8, 10 });
            floor2.ConfigureArt(Theme("Reef"), Theme("Boss"), null);
            floor2.ConfigureBands(new[] { Theme("Shallows"), Theme("Reef"), Theme("Abyss") });
            floor2.ConfigureCaves(true, new[] { new Vector2Int(30, 20), new Vector2Int(38, 24) }, new[] { new Vector2Int(26, 18) });
            floor2.ConfigureCorridors(new Vector2Int(4, 6), new Vector2Int(3, 4), FloorConfig.PathWeights.Descending);
            floor2.ConfigureFloor(2, null);
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

        static IEnumerable<Vector2Int> Cells(RectInt rect)
        {
            foreach (var cell in rect.allPositionsWithin)
                yield return cell;
        }

        RoomTheme Theme(string name)
        {
            var theme = Track(ScriptableObject.CreateInstance<RoomTheme>());
            theme.name = name;
            return theme;
        }

        [Test]
        public void Caves_PassTheChecks_ForManySeedsAndSizes()
        {
            for (int seed = 1; seed <= 300; seed++)
            {
                var size = Sizes[seed % Sizes.Length];
                var layout = CaveGenerator.Generate(size, new System.Random(seed));
                Assert.IsNotNull(layout, $"semilla {seed}, {size}: ninguna cueva pasó la comprobación.");
                CollectionAssert.IsEmpty(CaveGenerator.Check(layout), $"semilla {seed}, {size}");

                var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
                template.SetLayout(layout);
                Assert.AreEqual(size, new Vector2Int(template.Width, template.Height), $"semilla {seed}: tamaño pedido.");
                CollectionAssert.IsEmpty(template.Validate(), $"semilla {seed}: el arte puede dibujar toda la cueva.");
                foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    Assert.AreEqual(2, template.DoorCells(direction).Count, $"semilla {seed}: hueco de puerta hacia {direction}.");
            }
        }

        [Test]
        public void FloorThreeCaves_LShapesAndLakes_PassTheChecks_AndKeepSolidGroundBetweenDoors()
        {
            var sizes = new[] { new Vector2Int(40, 26), new Vector2Int(44, 28), new Vector2Int(50, 18), new Vector2Int(22, 30),
                new Vector2Int(52, 34) };
            int withMud = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var size = sizes[seed % sizes.Length];
                var shape = seed % 2 == 0 ? CaveGenerator.CaveShape.L : CaveGenerator.CaveShape.Oval;
                var layout = CaveGenerator.Generate(size, new System.Random(seed), true, shape, lakes: true);
                Assert.IsNotNull(layout, $"semilla {seed}, {size}, {shape}: ninguna cueva pasó la comprobación.");
                CollectionAssert.IsEmpty(CaveGenerator.Check(layout), $"semilla {seed}, {size}, {shape}");
                if (layout.Contains('m'))
                    withMud++;

                var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
                template.SetLayout(layout);
                CollectionAssert.IsEmpty(template.Validate(), $"semilla {seed}: el arte puede dibujar toda la cueva.");
                int mud = Cells(new RectInt(0, 0, template.Width, template.Height)).Count(cell => template.Get(cell.x, cell.y) == RoomTemplate.Mark.Mud);
                int floor = Cells(new RectInt(0, 0, template.Width, template.Height)).Count(cell => RoomTemplate.IsInterior(template.Get(cell.x, cell.y)));
                Assert.LessOrEqual(mud, floor * 0.31f, $"semilla {seed}: el lodo no pasa del 30 % del suelo.");
            }
            Assert.Greater(withMud, 100, "La mayoría de salas con lagos llevan lodo.");
        }

        FloorConfig.EncounterTheme[] MakeThemes(int count)
        {
            var themes = new FloorConfig.EncounterTheme[count];
            for (int i = 0; i < count; i++)
            {
                var enemy = Track(new GameObject($"ThemeEnemy_{i}")).AddComponent<Primordia.Enemies.Enemy>();
                themes[i] = new FloorConfig.EncounterTheme
                {
                    name = $"Tema {i}",
                    enemies = new[] { new FloorConfig.ThemedEnemy(enemy, 4) },
                    enemyMultiplier = 1.25f,
                    minDepth = 3,
                    customCave = true,
                    cave = new FloorConfig.CaveSpec(new Vector2Int(44, 26), CaveGenerator.CaveShape.Oval, lakes: false),
                    fernScale = 1f, methaneScale = 1f, bulbScale = 1f, ventScale = 1f, puddleScale = 1f,
                };
            }
            return themes;
        }

        [Test]
        public void EncounterThemes_TakeCombatRoomsOfThePath_OneOfEachAtMost()
        {
            floor2.ConfigureEncounterThemes(MakeThemes(5), new Vector2Int(2, 4));
            for (int seed = 1; seed <= 60; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var themed = layout.Rooms.Where(room => room.Encounter.HasValue).ToList();
                Assert.That(themed.Count, Is.InRange(2, 4), $"semilla {seed}: de 2 a 4 salas con tema.");
                Assert.AreEqual(themed.Count, themed.Select(room => room.Encounter.Value.name).Distinct().Count(), $"semilla {seed}: sin repetir tema.");
                int lastCombat = layout.Rooms.Where(room => room.Role == RoomRole.Combat).Max(room => room.Depth);
                foreach (var room in themed)
                {
                    Assert.AreEqual(RoomRole.Combat, room.Role, $"semilla {seed}: solo sustituye salas de combate del camino.");
                    Assert.GreaterOrEqual(room.Depth, 3, $"semilla {seed}: no en las dos primeras.");
                    Assert.Less(room.Depth, lastCombat, $"semilla {seed}: no en la última antes del jefe.");
                    Assert.AreEqual(1.25f, room.EnemyMultiplier, 0.001f);
                }
            }
        }

        [Test]
        public void EncounterThemes_WithoutThemes_DoNothingToTheLayout()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                Assert.IsFalse(layout.Rooms.Any(room => room.Encounter.HasValue), $"semilla {seed}");
            }
        }

        [Test]
        public void Caves_WithoutLakesOrShape_AreTheSameAsBefore()
        {
            var size = new Vector2Int(34, 22);
            Assert.AreEqual(CaveGenerator.Generate(size, new System.Random(5)),
                CaveGenerator.Generate(size, new System.Random(5), true, CaveGenerator.CaveShape.Oval, lakes: false));
            Assert.IsFalse(CaveGenerator.Generate(size, new System.Random(5)).Contains('m'));
        }

        [Test]
        public void Shortcuts_JoinNeighbouringRoomsFarApartOnThePath_OnlyWhenTheFloorAsksForThem()
        {
            int withShortcut = 0;
            for (int seed = 1; seed <= 100; seed++)
            {
                floor2.ConfigureShortcuts(Vector2Int.zero);
                var without = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                for (int i = 0; i < without.Links.Count; i++)
                    Assert.IsFalse(without.IsShortcut(i), $"semilla {seed}: sin atajos pedidos no hay atajos.");

                floor2.ConfigureShortcuts(new Vector2Int(1, 2));
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                int shortcuts = 0;
                for (int i = 0; i < layout.Links.Count; i++)
                {
                    if (!layout.IsShortcut(i))
                        continue;
                    shortcuts++;
                    var (from, to) = layout.Links[i];
                    var step = to.Cell - from.Cell;
                    Assert.AreEqual(1, Mathf.Abs(step.x) + Mathf.Abs(step.y), $"semilla {seed}: celdas vecinas.");
                    Assert.GreaterOrEqual(to.Depth - from.Depth, 3, $"semilla {seed}: la maleza está en la sala menos profunda.");
                    Assert.AreEqual(1, layout.Links.Count(link => (link.from == from && link.to == to) || (link.from == to && link.to == from)),
                        $"semilla {seed}: no repite un enlace que ya existía.");
                }
                Assert.LessOrEqual(shortcuts, 2, $"semilla {seed}");
                if (shortcuts > 0)
                    withShortcut++;
            }
            Assert.Greater(withShortcut, 10, "Algunos pisos tienen algún atajo.");
        }

        [Test]
        public void Caves_AreTheSame_WithTheSameSeed_AndChangeWithAnother()
        {
            var size = new Vector2Int(34, 22);
            Assert.AreEqual(CaveGenerator.Generate(size, new System.Random(7)), CaveGenerator.Generate(size, new System.Random(7)));
            Assert.AreNotEqual(CaveGenerator.Generate(size, new System.Random(7)), CaveGenerator.Generate(size, new System.Random(8)));
        }

        [Test]
        public void Caves_AreNotRectangles()
        {
            var layout = CaveGenerator.Generate(new Vector2Int(34, 22), new System.Random(3));
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout(layout);
            var bounds = template.FloorBounds();
            int floor = Cells(bounds).Count(cell => template.Get(cell.x, cell.y) == RoomTemplate.Mark.Floor);
            Assert.Less(floor, bounds.width * bounds.height * 0.92f, "La roca entra en la sala por los bordes.");
        }

        [Test]
        public void Check_FindsWallsTheArtCannotDraw()
        {
            var rows = RoomLayouts.All[RoomLayouts.Normal].Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();
            // Pared de 1 tile de grosor en medio de la sala.
            var row = rows[8].ToCharArray();
            row[10] = '#';
            rows[8] = new string(row);
            CollectionAssert.IsNotEmpty(CaveGenerator.Check(string.Join("\n", rows)));
        }

        [UnityTest]
        public IEnumerator Layout_WithCaves_UsesGeneratedShapes_ExceptBossAndShop()
        {
            var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(5));
            foreach (var room in layout.Rooms)
            {
                if (room.Role == RoomRole.Boss)
                    Assert.AreSame(floor2.BossRoom, room.Template, "La sala del jefe es la hecha a mano.");
                else
                    CollectionAssert.Contains(layout.GeneratedTemplates, room.Template, $"{room.Name} es una cueva generada.");
            }

            var templates = layout.GeneratedTemplates.ToList();
            layout.ReleaseGenerated();
            // En Play Mode se destruyen al final del cuadro.
            yield return null;
            Assert.IsTrue(templates.All(template => template == null), "Las cuevas se destruyen con el piso.");
        }

        [Test]
        public void Path_OfFloor2_TendsToGoDown()
        {
            int down = 0, up = 0, sides = 0;
            for (int seed = 1; seed <= 60; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var path = layout.Rooms.Where(r => r.Role is RoomRole.Start or RoomRole.Combat or RoomRole.Boss).OrderBy(r => r.Depth).ToList();
                for (int i = 1; i < path.Count; i++)
                {
                    var step = path[i].Cell - path[i - 1].Cell;
                    if (step == Vector2Int.down) down++;
                    else if (step == Vector2Int.up) up++;
                    else sides++;
                }
                layout.ReleaseGenerated();
            }
            Assert.Greater(down, sides, "Abajo a menudo…");
            Assert.Greater(sides, up * 3, "…a los lados a veces y casi nunca hacia arriba.");
        }

        [Test]
        public void Build_WithCavesAndTunnels_LeavesNoCellTheArtCannotDraw()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                var parent = Track(new GameObject("Floor")).transform;
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var built = FloorBuilder.Build(layout, floor2, parent, new System.Random(seed));
                var missing = Cells(built.Map.Bounds).Where(cell => WallRules.Classify(built.Map, cell) == Piece.Missing).ToList();
                CollectionAssert.IsEmpty(missing, $"semilla {seed}: celdas que el arte no puede dibujar.");
                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
            }
        }

        [Test]
        public void Tunnels_AreFourToSixLong_AndTwoToFourWide()
        {
            bool anyWide = false;
            for (int seed = 1; seed <= 8; seed++)
            {
                var parent = Track(new GameObject("Floor")).transform;
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var built = FloorBuilder.Build(layout, floor2, parent, new System.Random(seed));
                // Los pasillos van en el orden de los enlaces del plano.
                for (int i = 0; i < built.Corridors.Count; i++)
                {
                    var corridor = built.Corridors[i];
                    var (from, to) = layout.Links[i];
                    bool horizontal = from.Cell.y == to.Cell.y;
                    var along = corridor.Cells.GroupBy(cell => horizontal ? cell.x : cell.y).ToList();
                    Assert.GreaterOrEqual(along.Count, 4, $"semilla {seed}: pasillo de al menos 4 tiles.");
                    foreach (var step in along)
                    {
                        Assert.That(step.Count(), Is.InRange(2, 4), $"semilla {seed}: ancho del túnel.");
                        anyWide |= step.Count() > 2;
                    }
                }
                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
            }
            Assert.IsTrue(anyWide, "Los túneles se ensanchan a 3 o 4 tiles en algún punto.");
        }

        [Test]
        public void Floor1_KeepsStraightCorridors_AndRandomTurns()
        {
            var floor1 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            Assert.IsFalse(floor1.GeneratesCaves);
            Assert.AreEqual(new Vector2Int(FloorBuilder.CorridorLength, FloorBuilder.CorridorLength), floor1.CorridorLength);
            Assert.AreEqual(new Vector2Int(2, 2), floor1.CorridorWidth);
            Assert.IsTrue(floor1.Path.IsEven, "El piso 1 conserva los giros al azar de siempre.");
        }
    }
}

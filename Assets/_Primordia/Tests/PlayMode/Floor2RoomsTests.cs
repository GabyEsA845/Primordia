using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Tests
{
    /// <summary>
    /// Piso 2, paso 2: salas especiales (de 2 a 4, el Corazón morado siempre en el Abismo, nunca en las salas 1–2 ni en la
    /// última), el Parkour de dos casillas, la tienda con la sala de cambio detrás, la antesala en línea con el jefe y las fosas.
    /// </summary>
    public class Floor2RoomsTests
    {
        const string LayoutsFolder = "docs/multimedia-temp/floor2-rooms/Layouts";

        readonly List<Object> created = new();
        FloorConfig floor2;
        RoomTemplate parkour, heart, grotto, exchange, shop;

        [SetUp]
        public void SetUp()
        {
            parkour = Rectangle("Room_Special_Parkour", 18, 44);
            heart = Rectangle("Room_Special_CorazonMorado", 34, 22);
            grotto = Rectangle("Room_Special_GrutaEscondida", 24, 18);
            exchange = Rectangle("Room_Exchange", 22, 16);
            shop = Rectangle("Room_Shop_Floor2", 26, 18);

            floor2 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor2.ConfigureRooms(Rectangle("Start", 22, 15), Rectangle("Boss", 32, 25),
                new[] { Rectangle("Normal", 28, 16) }, new[] { Rectangle("Small", 22, 15) }, new Vector2Int(10, 12), new Vector2Int(2, 3));
            floor2.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null, newBaseBudget: 4f);
            floor2.ConfigureRewards(null, new[] { 2, 4, 6, 8, 10 });
            var band = Track(ScriptableObject.CreateInstance<RoomTheme>());
            floor2.ConfigureArt(band, Track(ScriptableObject.CreateInstance<RoomTheme>()), null);
            floor2.ConfigureBands(new[] { band, band, band });
            floor2.ConfigureCorridors(new Vector2Int(4, 6), new Vector2Int(2, 2), FloorConfig.PathWeights.Descending);
            floor2.ConfigureShopDepth(new Vector2(1f / 3f, 2f / 3f));
            var shopObject = Track(new GameObject("Shop"));
            shopObject.SetActive(false);
            floor2.ConfigureShop(shop, shopObject.AddComponent<Shop.ShopRoom>());
            floor2.ConfigureFixedRooms(exchange, Rectangle("Antechamber_Vertical", 14, 28), Rectangle("Antechamber_Horizontal", 30, 13));

            var specials = new List<FloorConfig.SpecialRoom>
            {
                Special(parkour, FloorConfig.SpecialPlacement.Path, false, 0f, tall: true),
                Special(heart, FloorConfig.SpecialPlacement.Side, false, 2f, always: true, band: 2),
                Special(grotto, FloorConfig.SpecialPlacement.Side, false, 0f),
            };
            foreach (var name in new[] { "AnilloSima", "PuenteRoto", "BosqueCoral", "FosilGigante", "JardinEsponjas", "SalaSellada", "Islas" })
                specials.Add(Special(Rectangle($"Room_Special_{name}", 32, 20), FloorConfig.SpecialPlacement.Path, true, 1f));
            floor2.ConfigureSpecialRooms(specials.ToArray(), new Vector2Int(2, 4));
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

        RoomTemplate Rectangle(string name, int width, int height)
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.name = name;
            template.SetLayout(RoomLayouts.Rectangle(width, height));
            return template;
        }

        static FloorConfig.SpecialRoom Special(RoomTemplate template, FloorConfig.SpecialPlacement placement, bool combat, float enemies,
            bool always = false, int band = -1, bool tall = false) => new()
        {
            template = template,
            placement = placement,
            countsAsCombat = combat,
            enemyMultiplier = enemies,
            always = always,
            band = band,
            tall = tall,
        };

        [Test]
        public void Layout_HasTwoToFourSpecials_AlwaysTheHeart_NeverInTheFirstTwoNorTheLast()
        {
            for (int seed = 1; seed <= 150; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                string context = $"semilla {seed}";
                var specials = layout.Rooms.Where(r => r.Special.HasValue).ToList();
                Assert.That(specials.Count, Is.InRange(2, 4), context);
                int combat = layout.Rooms.Count(r => r.Role == RoomRole.Combat);
                Assert.That(combat, Is.InRange(10, 12), $"Las especiales de combate cuentan entre las 10–12 ({context}).");

                var heartRoom = specials.SingleOrDefault(r => r.Template == heart);
                Assert.IsNotNull(heartRoom, $"El Corazón morado sale siempre ({context}).");
                Assert.AreEqual(RoomRole.Side, heartRoom.Role);
                Assert.AreEqual(2, FloorBuilder.BandOf(heartRoom.Depth, combat, 3), $"El Corazón morado, en el Abismo ({context}).");

                foreach (var room in specials)
                {
                    Assert.GreaterOrEqual(room.Depth, 2, $"{room.Name}: nunca en las dos primeras salas ({context}).");
                    Assert.Less(room.Depth, combat, $"{room.Name}: nunca en la última antes del jefe ({context}).");
                    if (room.Role == RoomRole.Combat || room.Role == RoomRole.Side)
                        Assert.GreaterOrEqual(room.Depth, 3, $"{room.Name} ({context}).");
                }
            }
        }

        [Test]
        public void Parkour_TakesTwoCells_AndIsCrossedFromTopToBottom()
        {
            int seen = 0;
            for (int seed = 1; seed <= 150; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var room = layout.Rooms.SingleOrDefault(r => r.Template == parkour);
                if (room == null)
                    continue;
                seen++;
                Assert.IsTrue(room.Tall);
                Assert.AreEqual(RoomRole.Special, room.Role, "No cuenta como sala de combate.");
                var others = layout.Rooms.Where(r => r != room).SelectMany(r => r.Cells).ToHashSet();
                Assert.IsFalse(room.Cells.Any(others.Contains), $"semilla {seed}: sus dos casillas son solo suyas.");

                var directions = Enumerable.Range(0, layout.Links.Count)
                    .Where(i => layout.Links[i].from == room || layout.Links[i].to == room)
                    .Select(i => layout.Links[i].to == room ? layout.Direction(i) : -layout.Direction(i)).ToList();
                CollectionAssert.AreEquivalent(new[] { Vector2Int.down, Vector2Int.up }, directions,
                    $"semilla {seed}: se entra bajando por arriba y se sale por abajo.");
            }
            Assert.Greater(seen, 0, "En alguna partida sale el Parkour.");
        }

        [Test]
        public void Antechamber_IsRightBeforeTheBoss_InAStraightLine()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var antechamber = layout.Rooms.Single(r => r.Role == RoomRole.Antechamber);
                int into = layout.Links.FindIndex(link => link.to == antechamber);
                int toBoss = layout.Links.FindIndex(link => link.from == antechamber);
                Assert.AreEqual(RoomRole.Boss, layout.Links[toBoss].to.Role, $"semilla {seed}");
                Assert.AreEqual(layout.Direction(into), layout.Direction(toBoss), $"semilla {seed}: en línea recta con el jefe.");
                bool vertical = layout.Direction(into).x == 0;
                Assert.AreSame(vertical ? floor2.AntechamberVertical : floor2.AntechamberHorizontal, antechamber.Template);
            }
        }

        [Test]
        public void Exchange_IsBehindTheShop_InADeadEnd()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var shopRoom = layout.Rooms.Single(r => r.Role == RoomRole.Shop);
                var exchangeRoom = layout.Rooms.Single(r => r.Role == RoomRole.Exchange);
                var exchangeLinks = layout.Links.Where(l => l.from == exchangeRoom || l.to == exchangeRoom).ToList();
                Assert.AreEqual(1, exchangeLinks.Count, $"semilla {seed}: fondo de saco.");
                Assert.AreSame(shopRoom, exchangeLinks[0].from, $"semilla {seed}: camino → tienda → sala de cambio.");
                Assert.AreSame(exchange, exchangeRoom.Template);
            }
        }

        [Test]
        public void Build_WithSpecialRooms_PlacesEveryRoomWithoutOverlaps()
        {
            for (int seed = 1; seed <= 15; seed++)
            {
                var parent = Track(new GameObject("Floor")).transform;
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                var built = FloorBuilder.Build(layout, floor2, parent, new System.Random(seed));
                var rects = built.MapRooms.Select(r => r.Rect).ToList();
                for (int i = 0; i < rects.Count; i++)
                    for (int j = i + 1; j < rects.Count; j++)
                        Assert.IsFalse(rects[i].Overlaps(rects[j]), $"semilla {seed}: {built.Rooms[i].name} y {built.Rooms[j].name} se pisan.");
                Assert.IsTrue(built.Corridors.All(c => c.Cells.Count > 0), $"semilla {seed}: todos los pasillos se abren.");
                var missing = Cells(built.Map.Bounds).Where(cell => WallRules.Classify(built.Map, cell) == Piece.Missing).ToList();
                CollectionAssert.IsEmpty(missing, $"semilla {seed}");
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void Floor1_StaysTheSame_WithoutSpecials()
        {
            var floor1 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor1.ConfigureRooms(Rectangle("Start", 22, 15), Rectangle("Boss", 28, 16), new[] { Rectangle("Normal", 28, 16) },
                new[] { Rectangle("Small", 22, 15) }, new Vector2Int(4, 5), new Vector2Int(1, 2));
            Assert.IsFalse(floor1.HasAntechamber);
            Assert.IsEmpty(floor1.SpecialRooms);
            for (int seed = 1; seed <= 30; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor1, new System.Random(seed));
                Assert.IsFalse(layout.Rooms.Any(r => r.Special.HasValue || r.Role is RoomRole.Antechamber or RoomRole.Exchange or RoomRole.Special));
                for (int i = 0; i < layout.Links.Count; i++)
                    Assert.AreEqual(layout.Links[i].to.Cell - layout.Links[i].from.Cell, layout.Direction(i));
            }
        }

        [Test]
        public void Pits_AreDrawnByNeighbours_AndEnemiesWalkAroundThem()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            var rows = RoomLayouts.Rectangle(28, 16).Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();
            for (int row = 7; row <= 9; row++)
                rows[row] = rows[row].Substring(0, 10) + "~~~" + rows[row].Substring(13);
            template.SetLayout(string.Join("\n", rows));
            CollectionAssert.IsEmpty(template.Validate());

            var map = template.ToFloorMap(Vector2Int.zero, openDoors: true);
            var center = new Vector2Int(11, template.Height - 1 - 8);
            Assert.AreEqual(FloorMap.Kind.Pit, map.Get(center));
            Assert.AreEqual(0, RoomPainter.PitMask(map, center), "En medio de la fosa, ningún vecino es suelo.");
            Assert.AreEqual(1 | 8, RoomPainter.PitMask(map, center + new Vector2Int(-1, 1)), "Esquina de arriba a la izquierda: suelo al norte y al oeste.");
            Assert.IsFalse(new NavigationGrid(map).IsWalkable(center), "Los enemigos que andan la rodean.");
        }

        [Test]
        public void HandmadeLayouts_PassTheChecks()
        {
            if (!Directory.Exists(LayoutsFolder))
                Assert.Ignore($"No están los planos en {LayoutsFolder}.");

            foreach (var file in Directory.GetFiles(LayoutsFolder, "Room_*.txt"))
            {
                var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
                template.name = Path.GetFileNameWithoutExtension(file);
                template.SetLayout(File.ReadAllText(file));
                CollectionAssert.IsEmpty(template.Validate(), $"{template.name}: el arte puede dibujarla entera.");
            }

            var parkourLayout = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            parkourLayout.SetLayout(File.ReadAllText($"{LayoutsFolder}/Room_Special_Parkour.txt"));
            Assert.AreEqual(2, parkourLayout.DoorCells(Vector2Int.up).Count);
            Assert.AreEqual(2, parkourLayout.DoorCells(Vector2Int.down).Count);
            Assert.IsEmpty(parkourLayout.DoorCells(Vector2Int.left), "El Parkour solo tiene puertas arriba y abajo.");
            Assert.Greater(parkourLayout.CellsOf(RoomTemplate.Mark.Pit).Count, 100);
        }

        static IEnumerable<Vector2Int> Cells(RectInt rect)
        {
            foreach (var cell in rect.allPositionsWithin)
                yield return cell;
        }
    }
}

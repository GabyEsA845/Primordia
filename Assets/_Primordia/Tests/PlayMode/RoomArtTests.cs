using System.Collections;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class RoomArtTests
    {
        /// <summary>Mapa de prueba escrito como texto, de arriba abajo: # pared, . suelo, g hueco, p paso, a arco.</summary>
        static FloorMap Map(params string[] rows)
        {
            var map = new FloorMap(new RectInt(0, 0, rows[0].Length, rows.Length));
            for (int row = 0; row < rows.Length; row++)
            {
                int y = rows.Length - 1 - row;
                for (int x = 0; x < rows[row].Length; x++)
                {
                    map.Set(new Vector2Int(x, y), rows[row][x] switch
                    {
                        '.' => FloorMap.Kind.Floor,
                        'g' => FloorMap.Kind.DoorGap,
                        'p' => FloorMap.Kind.Passage,
                        'a' => FloorMap.Kind.DoorArch,
                        _ => FloorMap.Kind.Wall
                    });
                }
            }
            return map;
        }

        static Piece At(FloorMap map, int x, int y) => WallRules.Classify(map, new Vector2Int(x, y));

        [Test]
        public void RectangularRoom_UsesTheSamplePieces()
        {
            var map = Map(
                "#######",
                "#######",
                "#######",
                "#.....#",
                "#.....#",
                "#######");

            Assert.AreEqual(Piece.Cap, At(map, 3, 5));
            Assert.AreEqual(Piece.FaceUpper, At(map, 3, 4));
            Assert.AreEqual(Piece.FaceLower, At(map, 3, 3));
            Assert.AreEqual(Piece.FloorShadow, At(map, 3, 2));
            Assert.AreEqual(Piece.Floor, At(map, 3, 1));
            Assert.AreEqual(Piece.EdgeBottom, At(map, 3, 0));

            Assert.AreEqual(Piece.Cap, At(map, 0, 5), "La esquina de arriba es relleno.");
            Assert.AreEqual(Piece.EdgeLeft, At(map, 0, 4), "El borde lateral sube junto a la cara.");
            Assert.AreEqual(Piece.EdgeLeft, At(map, 0, 1));
            Assert.AreEqual(Piece.EdgeRight, At(map, 6, 3));
            Assert.AreEqual(Piece.CornerBottomLeft, At(map, 0, 0));
            Assert.AreEqual(Piece.CornerBottomRight, At(map, 6, 0));
        }

        [Test]
        public void LShapedRoom_NeedsFaceEnds()
        {
            var map = Map(
                "##########",
                "##########",
                "##########",
                "#...######",
                "#...######",
                "#...######",
                "#........#",
                "#........#",
                "##########");

            Assert.AreEqual(Piece.FaceLowerEndLeft, At(map, 4, 3));
            Assert.AreEqual(Piece.FaceUpperEndLeft, At(map, 4, 4));
            Assert.AreEqual(Piece.EdgeRight, At(map, 4, 5), "Lado del entrante que da al suelo de arriba.");
            Assert.AreEqual(Piece.FaceLower, At(map, 6, 3));
            Assert.AreEqual(Piece.EdgeRight, At(map, 9, 3), "La pared derecha sigue junto a la cara del entrante.");
        }

        [Test]
        public void Pillar_HasOuterCornersAndFaceEnds()
        {
            var map = Map(
                "########",
                "########",
                "########",
                "#......#",
                "#..##..#",
                "#..##..#",
                "#..##..#",
                "#......#",
                "#......#",
                "########");

            Assert.AreEqual(Piece.OuterTopLeft, At(map, 3, 5));
            Assert.AreEqual(Piece.OuterTopRight, At(map, 4, 5));
            Assert.AreEqual(Piece.FaceUpperEndLeft, At(map, 3, 4));
            Assert.AreEqual(Piece.FaceUpperEndRight, At(map, 4, 4));
            Assert.AreEqual(Piece.FaceLowerEndLeft, At(map, 3, 3));
            Assert.AreEqual(Piece.FaceLowerEndRight, At(map, 4, 3));
        }

        [Test]
        public void ThinWall_IsMissing()
        {
            var map = Map(
                "#######",
                "#######",
                "#######",
                "#.....#",
                "#..#..#",
                "#.....#",
                "#######");

            Assert.AreEqual(Piece.Missing, At(map, 3, 2));
        }

        [Test]
        public void CorridorJunction_KeepsTheRoomEdge()
        {
            var map = Map(
                "#########",
                "#########",
                "#########",
                "#...#####",
                "#...g....",
                "#...g....",
                "#########");

            Assert.AreEqual(Piece.EdgeRight, At(map, 4, 3), "La pared de la sala sobre el hueco no se rompe por la cara del pasillo.");
            Assert.AreEqual(Piece.FaceLower, At(map, 5, 3));
            Assert.AreEqual(Piece.Floor, At(map, 4, 2), "El hueco se dibuja como suelo.");
            Assert.AreEqual(Piece.FloorShadow, At(map, 5, 2));
            Assert.AreEqual(Piece.EdgeBottom, At(map, 5, 0));
        }

        [Test]
        public void TopDoor_LeavesTheArchEmpty_AndOpensTheCap()
        {
            var map = Map(
                "##.##",
                "##p##",
                "##a##",
                "##a##",
                "#...#",
                "#####");

            Assert.AreEqual(Piece.None, At(map, 2, 3));
            Assert.AreEqual(Piece.Floor, At(map, 2, 4), "El paso se dibuja como suelo.");
            Assert.AreEqual(Piece.EdgeLeft, At(map, 1, 4), "La pared del pasillo empieza en la fila de la tapa.");
            Assert.AreEqual(Piece.FloorShadow, At(map, 2, 1), "Bajo el arco hay sombra, como bajo la cara.");
        }

        [Test]
        public void Template_ParsesRowsFromTheTop_AndFindsDoors()
        {
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            try
            {
                template.SetLayout("#^^#\n#..#\n<..>\n#vv#\n");

                Assert.AreEqual(4, template.Width);
                Assert.AreEqual(4, template.Height);
                Assert.AreEqual(RoomTemplate.Mark.DoorUp, template.Get(1, 3));
                Assert.AreEqual(RoomTemplate.Mark.DoorDown, template.Get(2, 0));
                Assert.AreEqual(2, template.DoorCells(Vector2Int.up).Count);
                Assert.AreEqual(1, template.DoorCells(Vector2Int.left).Count);
                Assert.AreEqual(new RectInt(1, 1, 2, 2), template.FloorBounds());
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void Catalog_HasNoImpossibleCells_AndAlignedDoors()
        {
            foreach (var (name, layout) in RoomLayouts.All)
            {
                var template = ScriptableObject.CreateInstance<RoomTemplate>();
                try
                {
                    template.SetLayout(layout);
                    var problems = template.Validate();
                    Assert.IsEmpty(problems, $"{name}:\n{string.Join("\n", problems)}");

                    foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        Assert.AreEqual(2, template.DoorCells(direction).Count, $"{name} debe tener puerta hacia {direction}.");
                }
                finally
                {
                    Object.DestroyImmediate(template);
                }
            }
        }

        [TestCase(28, 16, 26, 12)]
        [TestCase(22, 15, 20, 11)]
        public void Rectangle_HasThreeWallRowsOnTop(int width, int height, int floorWidth, int floorHeight)
        {
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            try
            {
                template.SetLayout(RoomLayouts.Rectangle(width, height));

                Assert.AreEqual(new RectInt(1, 1, floorWidth, floorHeight), template.FloorBounds());
                Assert.AreEqual(new Vector2Int(width / 2, 1 + floorHeight / 2), template.Anchor,
                    "El anclaje queda en medio de las puertas, que están centradas.");
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void Validate_ReportsMisalignedDoors()
        {
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            try
            {
                template.SetLayout(
                    "#^^####\n" +
                    "#######\n" +
                    "#######\n" +
                    "#.....#\n" +
                    "#.....#\n" +
                    "####vv#\n");

                Assert.IsTrue(template.Validate().Exists(p => p.Contains("arriba y abajo")));
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        [UnityTest]
        public IEnumerator Door_BlocksAtOnce_AndAnimatesItsMembrane()
        {
            var doorObject = new GameObject("Door");
            try
            {
                var renderer = doorObject.AddComponent<SpriteRenderer>();
                var blocker = doorObject.AddComponent<BoxCollider2D>();
                var frames = new Sprite[3];
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);

                var door = doorObject.AddComponent<Door>();
                door.Configure(blocker, renderer, frames, newFrameDuration: 0.05f);
                door.Open();
                yield return new WaitForSeconds(0.2f);
                Assert.IsFalse(renderer.enabled);

                door.Close();
                Assert.IsFalse(door.IsOpen);
                Assert.IsTrue(blocker.enabled, "Bloquea en el acto, aunque la membrana aún se esté cerrando.");
                Assert.IsTrue(door.IsAnimating);

                yield return new WaitForSeconds(0.25f);
                Assert.IsFalse(door.IsAnimating);
                Assert.IsTrue(renderer.enabled);
                Assert.AreSame(frames[^1], renderer.sprite);

                door.Open();
                Assert.IsFalse(blocker.enabled);
                yield return new WaitForSeconds(0.25f);
                Assert.IsFalse(renderer.enabled);
            }
            finally
            {
                Object.DestroyImmediate(doorObject);
            }
        }
    }
}

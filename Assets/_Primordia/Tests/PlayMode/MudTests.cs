using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>Lodo profundo del piso 3: se distingue de la fosa, los enemigos que andan lo tratan como agujero y la tierra firme lo evita.</summary>
    public class MudTests
    {
        FloorMap previous;

        [SetUp]
        public void SetUp() => previous = Pits.Map;

        [TearDown]
        public void TearDown() => Pits.Map = previous;

        static FloorMap MapWith(FloorMap.Kind middle)
        {
            var map = new FloorMap(new RectInt(0, 0, 9, 9));
            foreach (var cell in map.Bounds.allPositionsWithin)
                map.Set(cell, FloorMap.Kind.Floor);
            for (int x = 3; x <= 5; x++)
                for (int y = 3; y <= 5; y++)
                    map.Set(new Vector2Int(x, y), middle);
            return map;
        }

        [Test]
        public void Mud_IsNotAPit_ButIsAHole()
        {
            Pits.Map = MapWith(FloorMap.Kind.Mud);
            var inside = new Vector2(4.5f, 4.5f);
            Assert.IsTrue(Pits.IsMud(inside));
            Assert.IsFalse(Pits.IsPit(inside), "El jugador no cae al instante en el lodo.");
            Assert.IsTrue(Pits.IsHole(inside));
            Assert.IsTrue(Pits.IsDeepInHole(inside, 0.2f), "Un enemigo que anda en medio del lodo se hunde.");
            Assert.IsFalse(Pits.IsDeepInPit(inside, 0.2f));
        }

        [Test]
        public void Pits_AreNotMud()
        {
            Pits.Map = MapWith(FloorMap.Kind.Pit);
            var inside = new Vector2(4.5f, 4.5f);
            Assert.IsFalse(Pits.IsMud(inside));
            Assert.IsTrue(Pits.IsDeepInPit(inside, 0.2f));
        }

        [Test]
        public void IsFirm_NeedsMarginAwayFromMud()
        {
            Pits.Map = MapWith(FloorMap.Kind.Mud);
            Assert.IsFalse(Pits.IsFirm(new Vector2(2.9f, 4.5f), 0.45f), "Pegado al lodo no es suelo firme para volver.");
            Assert.IsTrue(Pits.IsFirm(new Vector2(1.5f, 4.5f), 0.45f));
            Assert.IsTrue(Pits.IsSafe(new Vector2(2.9f, 4.5f), 0.45f), "Para las fosas, el lodo no cuenta.");
        }

        [Test]
        public void Mud_CountsAsOpenForWalls_AndNavigationAvoidsIt()
        {
            var map = MapWith(FloorMap.Kind.Mud);
            Assert.IsTrue(map.IsOpen(4, 4));
            var navigation = new NavigationGrid(map);
            Assert.IsFalse(navigation.IsWalkable(new Vector2Int(4, 4)), "Los enemigos que andan rodean el lodo.");
            Assert.IsTrue(navigation.IsWalkable(new Vector2Int(1, 1)));
        }
    }
}

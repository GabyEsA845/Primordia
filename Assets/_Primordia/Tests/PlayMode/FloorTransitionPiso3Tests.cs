using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Primordia.Tests
{
    public class FloorTransitionPiso3Tests
    {
        readonly List<Object> created = new();
        T Track<T>(T value) where T : Object { created.Add(value); return value; }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
            Time.timeScale = 1f;
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void Frontera_OndulaYConservaElOrdenEnCadaFila(int dx, int dy)
        {
            var direction = new Vector2Int(dx, dy);
            var tangent = new Vector2Int(-dy, dx);
            var door = Vector2.one * 0.5f;
            var center = door + (Vector2)direction * 16f;
            var changes = new List<int>();
            for (int row = -8; row <= 8; row++)
            {
                int previous = 0, first = -1;
                var seen = new HashSet<int>();
                for (int column = 0; column <= 22; column++)
                {
                    float t = FloorBuilder.RippleTransitionDistance(direction * column + tangent * row, door, center) / 16f;
                    int rank = t < 0.4f ? 0 : t < 0.75f ? 1 : 2;
                    Assert.GreaterOrEqual(rank, previous);
                    if (rank > 0 && first < 0) first = column;
                    previous = rank;
                    seen.Add(rank);
                }
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, seen);
                changes.Add(first);
            }
            Assert.GreaterOrEqual(changes.Max() - changes.Min(), 3);
        }

        [Test]
        public void Mezcla_PintaMascaraYVariasEsquinasSinInvadirElInterior()
        {
            var old = Track(ScriptableObject.CreateInstance<RoomTheme>());
            var next = Track(ScriptableObject.CreateInstance<RoomTheme>());
            var maskTiles = Enumerable.Range(0, 16).Select(_ => (TileBase)Track(ScriptableObject.CreateInstance<Tile>())).ToArray();
            var corners = Enumerable.Range(0, 4).Select(_ => (TileBase)Track(ScriptableObject.CreateInstance<Tile>())).ToArray();
            old.ConfigureBlend(new[] { new BlendSet(old, new[] { next }, maskTiles, corners) },
                System.Array.Empty<BlendSet>(), System.Array.Empty<TileBase>(), System.Array.Empty<Sprite>());
            var map = new FloorMap(new RectInt(-5, -5, 11, 11));
            foreach (var at in map.Bounds.allPositionsWithin) map.Set(at, FloorMap.Kind.Floor);
            map.SetTheme(new Vector2Int(1, 0), 1);
            map.SetTheme(new Vector2Int(-1, 1), 1);
            map.SetTheme(new Vector2Int(-1, -1), 1);
            var grid = Track(new GameObject("Grid", typeof(Grid)));
            Tilemap Layer(string name)
            {
                var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                child.transform.SetParent(grid.transform);
                return child.GetComponent<Tilemap>();
            }
            RoomPainter.Paint(map, new[] { old, next }, Layer("Floor"), Layer("Decor"), Layer("Walls"));
            TileBase At(string layer, Vector3Int cell) => grid.transform.Find(layer).GetComponent<Tilemap>().GetTile(cell);
            Assert.AreSame(maskTiles[2], At("BlendFloor0", Vector3Int.zero));
            Assert.AreSame(corners[1], At("BlendFloor2", Vector3Int.zero));
            Assert.AreSame(corners[3], At("BlendFloor4", Vector3Int.zero));
            Assert.IsNull(At("BlendFloor1", Vector3Int.zero));
            Assert.IsNull(At("BlendFloor0", new Vector3Int(3, 3, 0)));
            Assert.IsNull(At("BlendFloor0", Vector3Int.right));
        }

#if UNITY_EDITOR
        const string Art = "Assets/_Primordia/Art/FloorTransition/Sand";
        static FloorConfig Config(string name) => AssetDatabase.LoadAssetAtPath<FloorConfig>($"Assets/_Primordia/Data/Rooms/{name}.asset");
        static RoomTheme Theme(string name) => AssetDatabase.LoadAssetAtPath<RoomTheme>($"{Art}/{name}/Theme_{name}.asset");

        [TestCase(false)]
        [TestCase(true)]
        public void Paredes_MezclanTapasYConservanLaVarianteDelParDeCaras(bool sandOnLeft)
        {
            var mid = Theme("BurntSand");
            var burnt = mid.BlendFloorTiles[0].source;
            var map = new FloorMap(new RectInt(-4, -4, 9, 9));
            foreach (var cell in map.Bounds.allPositionsWithin)
            {
                map.Set(cell, cell.y < 0 ? FloorMap.Kind.Floor : FloorMap.Kind.Wall);
                map.SetTheme(cell, (sandOnLeft ? cell.x < 0 : cell.x > 0) ? 2 : 0);
            }
            var grid = Track(new GameObject("Grid", typeof(Grid)));
            Tilemap Layer(string name)
            {
                var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                child.transform.SetParent(grid.transform);
                return child.GetComponent<Tilemap>();
            }
            var walls = Layer("Walls");
            RoomPainter.Paint(map, new[] { burnt, mid, Theme("Permian") }, Layer("Floor"), Layer("Decor"), walls);
            var upper = walls.GetTile(new Vector3Int(0, 1, 0));
            int index = System.Array.IndexOf(mid.BlendFaces, upper);
            int side = sandOnLeft ? 4 : 0;
            Assert.That(index, Is.InRange(side, side + 1));
            Assert.AreSame(mid.BlendFaces[index + 2], walls.GetTile(Vector3Int.zero));
            Assert.AreSame(mid.BlendCapTiles[0].tiles[sandOnLeft ? 8 : 2],
                grid.transform.Find("BlendCap0").GetComponent<Tilemap>().GetTile(new Vector3Int(0, 2, 0)));
            Assert.AreEqual(Tile.ColliderType.Grid, ((Tile)upper).colliderType);
        }

        [TestCase("BurntSand")]
        [TestCase("Permian")]
        public void Temas_TienenTodasLasPiezasObligatoriasYSinVegetacion(string name)
        {
            var theme = Theme(name);
            Assert.IsNotNull(theme);
            var serializedTheme = new SerializedObject(theme);
            foreach (string field in new[] { "floorVariants", "floorShadow", "capVariants", "faceUpper", "faceLower" })
            {
                var tiles = serializedTheme.FindProperty(field);
                Assert.Greater(tiles.arraySize, 0, field);
                for (int i = 0; i < tiles.arraySize; i++)
                    Assert.IsNotNull(tiles.GetArrayElementAtIndex(i).objectReferenceValue, $"{field}[{i}]");
            }
            foreach (Piece piece in System.Enum.GetValues(typeof(Piece)))
            {
                if (piece is Piece.None or Piece.Missing) continue;
                for (int variant = 0; variant < 12; variant++)
                {
                    var tile = theme.Get(piece, variant);
                    Assert.IsNotNull(tile, piece.ToString());
                    Assert.AreNotSame(theme.MissingTile, tile, piece.ToString());
                }
            }
            Assert.IsFalse(theme.HasMud);
            Assert.IsEmpty(theme.Ferns);
            Assert.IsNull(theme.BrushFor(Vector2Int.up));
            var mid = Theme("BurntSand");
            foreach (var set in mid.BlendFloorTiles.Concat(mid.BlendCapTiles))
            {
                Assert.AreEqual(16, set.tiles.Length);
                Assert.IsNull(set.tiles[0]);
                Assert.IsTrue(set.tiles.Skip(1).All(tile => tile != null));
                Assert.AreEqual(4, set.corners.Length);
                Assert.IsTrue(set.corners.All(tile => tile != null));
            }
            Assert.AreEqual(8, mid.BlendFaces.Length);
            Assert.IsTrue(mid.BlendFaces.All(tile => tile != null));
        }

        [TestCase(7)]
        [TestCase(42)]
        [TestCase(91)]
        public void Piso3_SeGeneraConJefeSumideroYTresTemas(int seed)
        {
            var config = Config("Floor_3");
            Assert.IsNotNull(config);
            Assert.IsTrue(config.HasTransition);
            Assert.IsNull(config.NextFloor);
            var parent = Track(new GameObject("Piso3"));
            var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
            var built = FloorBuilder.Build(layout, config, parent.transform, new System.Random(seed));
            var transition = built.MapRooms.Single(room => room.Role == RoomRole.Transition);
            var pit = transition.Room.GetComponentInChildren<TransitionPit>(true);
            Assert.IsNotNull(pit);
            Assert.AreEqual(3, built.Map.GetTheme(Vector2Int.FloorToInt(pit.transform.position)));
            var seen = new HashSet<int>();
            foreach (var cell in transition.Rect.allPositionsWithin)
                if (built.Map.Get(cell) == FloorMap.Kind.Floor) seen.Add(built.Map.GetTheme(cell));
            CollectionAssert.IsSubsetOf(new[] { 6, 2, 3 }, seen.ToArray());
            foreach (var cell in transition.Rect.allPositionsWithin)
                if (RoomPainter.IsLowerFace(WallRules.Classify(built.Map, cell)))
                    Assert.AreEqual(built.Map.GetTheme(cell + Vector2Int.up), built.Map.GetTheme(cell), cell.ToString());
            var boss = built.MapRooms.Single(room => room.Role == RoomRole.Boss);
            Assert.IsNotEmpty(boss.Room.GetComponentsInChildren<Primordia.Enemies.Enemy>(true));
            Assert.IsNull(boss.Room.transform.Find("Exit"));
            Assert.IsEmpty(transition.Room.GetComponentsInChildren<Fern>(true));
        }

        [Test]
        public void EscenaYSumidero_EstanGuardadosConOchoCuadrosYPolvo()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Primordia/Scenes/Test_Floor3Transition.unity"));
            var config = Config("Floor_3TransitionTest");
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.TransitionPit);
            var serialized = new SerializedObject(config.TransitionPit);
            Assert.AreEqual(8, serialized.FindProperty("pitFrames").arraySize);
            Assert.AreEqual(4, serialized.FindProperty("bubbleFrames").arraySize);
            Assert.AreEqual(2, serialized.FindProperty("bubblePopFrames").intValue);
            Assert.AreEqual(1f / 0.167f, serialized.FindProperty("pitFramesPerSecond").floatValue, 0.001f);
            Assert.IsNull(config.NextFloor);
        }

        [TestCase("Floor_1")]
        [TestCase("Floor_2")]
        public void PisosAnteriores_ConservanExactamenteElMapaDeTemas(string name)
        {
            var config = Config(name);
            Assert.IsFalse(config.TransitionMidTheme.RippleBorders);
            var room = new FloorBuilder.PlacedRoom
            {
                Layout = new LayoutRoom { Role = RoomRole.Transition, Template = config.TransitionRoom },
                Origin = new Vector2Int(43, -21),
            };
            var entry = config.TransitionRoom.DoorCells(Vector2Int.left).Select(cell => cell + room.Origin).ToList();
            room.DoorCells.Add(Vector2Int.left, entry);
            var map = new FloorMap(new RectInt(room.Origin - Vector2Int.one * 16, room.Rect.size + Vector2Int.one * 32));
            FloorBuilder.ThemeTransition(map, room, new List<FloorBuilder.PlacedRoom> { room }, config);
            var door = new Vector2((float)entry.Average(cell => cell.x), (float)entry.Average(cell => cell.y)) + Vector2.one * 0.5f;
            float span = Mathf.Max(1f, Vector2.Distance(door, room.FloorCenter));
            int entryTheme = config.HasBands ? 4 + config.BandThemes.Length - 1 : 0;
            // Referencia anterior a la tarea 20: se compara cada celda, incluido el relleno de pared.
            foreach (var cell in map.Bounds.allPositionsWithin)
            {
                int noise;
                unchecked
                {
                    noise = cell.x * 92837111 ^ cell.y * 689287499;
                    noise ^= noise >> 15;
                    noise = (noise * 1103515245) & int.MaxValue;
                }
                float t = Vector2.Distance(cell + Vector2.one * 0.5f, door) / span
                    + ((noise & 1023) / 1023f * 2f - 1f) * 0.12f;
                Assert.AreEqual(t < 0.4f ? entryTheme : t < 0.75f ? 2 : 3, map.GetTheme(cell), cell.ToString());
            }
        }
#endif
    }
}

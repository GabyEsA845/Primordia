using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Primordia.Tests
{
    /// <summary>Piso 2, entrega 1c: puertas de losas, obstáculos de cobertura, cáusticas, rayos, nieve marina y luces 2D.</summary>
    public class Floor2AmbienceTests
    {
        readonly List<Object> created = new();
        FloorConfig floor2;
        RoomTheme band;

        [SetUp]
        public void SetUp()
        {
            band = Theme("Band");
            floor2 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor2.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal) }, new[] { Template(RoomLayouts.Small) }, new Vector2Int(3, 3), Vector2Int.zero);
            floor2.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor2.ConfigureRewards(null, System.Array.Empty<int>());
            floor2.ConfigureArt(band, Theme("Boss"), null);
            floor2.ConfigureBands(new[] { band, band, band });
            floor2.ConfigureCaves(true, new[] { new Vector2Int(34, 22) }, new[] { new Vector2Int(28, 19) });
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

        RoomTheme Theme(string name)
        {
            var theme = Track(ScriptableObject.CreateInstance<RoomTheme>());
            theme.name = name;
            return theme;
        }

        Sprite Sprite(string name, int width = 16, int height = 16)
        {
            var texture = Track(new Texture2D(width, height));
            var sprite = Track(UnityEngine.Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f));
            sprite.name = name;
            return sprite;
        }

        Sprite[] Frames(string name, int count) => Enumerable.Range(0, count).Select(i => Sprite($"{name}_{i}")).ToArray();

        (FloorLayout layout, FloorBuilder.BuiltFloor built, Transform parent) Build(int seed)
        {
            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
            var built = FloorBuilder.Build(layout, floor2, parent, new System.Random(seed));
            return (layout, built, parent);
        }

        [Test]
        public void Caves_HaveCover_ThatNeverBlocksThePath()
        {
            int withCover = 0;
            for (int seed = 1; seed <= 100; seed++)
            {
                var layout = CaveGenerator.Generate(new Vector2Int(34, 22), new System.Random(seed));
                CollectionAssert.IsEmpty(CaveGenerator.Check(layout), $"semilla {seed}: con obstáculos sigue todo unido.");
                if (layout.Contains('o') || layout.Contains('O'))
                    withCover++;

                var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
                template.SetLayout(layout);
                int big = Enumerable.Range(0, template.Width * template.Height)
                    .Count(i => template.Get(i % template.Width, i / template.Width) == RoomTemplate.Mark.BigObstacle);
                Assert.AreEqual(big, template.BigObstacleCorners().Count * 4, $"semilla {seed}: obstáculos grandes en bloques de 2×2.");
            }
            Assert.Greater(withCover, 90, "Casi todas las cuevas tienen cobertura dentro.");
            Assert.IsTrue(CaveGenerator.Generate(new Vector2Int(34, 22), new System.Random(1), cover: false).All(c => c != 'o' && c != 'O'));
        }

        [Test]
        public void Obstacles_CountAsFloorForWalls_ButBlockTheWay()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            var rows = RoomLayouts.All[RoomLayouts.Normal].Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();
            rows[8] = rows[8].Substring(0, 5) + "o" + rows[8].Substring(6, 4) + "OO" + rows[8].Substring(12);
            rows[9] = rows[9].Substring(0, 10) + "OO" + rows[9].Substring(12);
            template.SetLayout(string.Join("\n", rows));

            CollectionAssert.IsEmpty(template.Validate(), "Los obstáculos no dejan paredes imposibles.");
            Assert.AreEqual(1, template.BigObstacleCorners().Count);
            var map = template.ToFloorMap(Vector2Int.zero, openDoors: true);
            var cell = new Vector2Int(5, template.Height - 1 - 8);
            Assert.AreEqual(FloorMap.Kind.Obstacle, map.Get(cell));
            Assert.IsFalse(new NavigationGrid(map).IsWalkable(cell), "Los enemigos los rodean.");
        }

        [Test]
        public void Build_PutsObstacles_WithCollision_AndASpriteOfTheTheme()
        {
            var rock = Sprite("Obstacle_Rock");
            var big = Sprite("Obstacle_RockBig", 32, 32);
            band.ConfigureObstacles(new[] { rock }, new[] { big });

            var (layout, built, _) = Build(3);
            int expected = layout.Rooms.Sum(room =>
            {
                var t = room.Template;
                int small = Enumerable.Range(0, t.Width * t.Height).Count(i => t.Get(i % t.Width, i / t.Width) == RoomTemplate.Mark.Obstacle);
                return small + t.BigObstacleCorners().Count;
            });
            Assert.Greater(expected, 0, "Las cuevas de la partida tienen obstáculos.");

            var obstacles = built.Rooms.SelectMany(room => room.GetComponentsInChildren<BoxCollider2D>(true))
                .Where(c => c.name.StartsWith("Obstacle")).ToList();
            Assert.AreEqual(expected, obstacles.Count);
            Assert.IsTrue(obstacles.All(o => !o.isTrigger), "Paran al jugador y los proyectiles.");
            Assert.IsTrue(obstacles.All(o => o.GetComponent<SpriteRenderer>().sprite == (o.size.x > 1f ? big : rock)));
            layout.ReleaseGenerated();
        }

        [UnityTest]
        public IEnumerator SlabDoor_ClosesAndOpens_WithItsOwnFrames()
        {
            var close = Frames("Close", 7);
            var open = Frames("Open", 6);
            var doorObject = Track(new GameObject("Door"));
            var renderer = doorObject.AddComponent<SpriteRenderer>();
            var blocker = doorObject.AddComponent<BoxCollider2D>();
            var door = doorObject.AddComponent<Door>();
            door.Configure(blocker, renderer, new DoorAnimation(close, Enumerable.Repeat(0.01f, 7).ToArray(), open,
                Enumerable.Repeat(0.01f, 6).ToArray()));

            door.Close();
            Assert.IsTrue(blocker.enabled, "Bloquea en cuanto empieza a cerrarse.");
            Assert.AreSame(close[0], renderer.sprite);
            yield return new WaitUntil(() => !door.IsAnimating);
            Assert.AreSame(close[^1], renderer.sprite, "Cerrada, en el último cuadro de cerrar.");
            Assert.IsTrue(renderer.enabled);

            door.Open();
            Assert.IsFalse(blocker.enabled);
            Assert.AreSame(open[0], renderer.sprite, "Abrir tiene sus propios cuadros (el sello se enciende).");
            yield return new WaitUntil(() => !door.IsAnimating);
            Assert.IsFalse(renderer.enabled, "Abierta, se oculta.");
        }

        [Test]
        public void Build_UsesSlabDoors_WhenTheThemeHasThem()
        {
            var slab = new DoorAnimation(Frames("Close", 7), null, Frames("Open", 6), null);
            band.ConfigureSlabs(slab, slab, slab);
            var (layout, built, _) = Build(4);
            var doors = built.Rooms.Where(room => room.name != "Room_Boss").SelectMany(room => room.GetComponentsInChildren<Door>(true)).ToList();
            Assert.IsNotEmpty(doors);
            Assert.IsTrue(doors.All(d => d.GetComponent<SpriteRenderer>().sprite == slab.close[^1]), "Las puertas de la franja son de losas.");
            layout.ReleaseGenerated();
        }

        [Test]
        public void Build_PaintsCaustics_OnlyOnTheFloor()
        {
            var caustics = Enumerable.Range(0, 16).Select(i =>
            {
                var tile = Track(ScriptableObject.CreateInstance<Tile>());
                tile.name = $"Caustics_{i}";
                return (TileBase)tile;
            }).ToArray();
            band.ConfigureAmbience(caustics, null, 0.25f, 0, null);
            // Un tile de suelo para saber qué celdas se dibujan como suelo (las paredes quedan sin tile).
            var floorTile = Track(ScriptableObject.CreateInstance<Tile>());
            var none = System.Array.Empty<TileBase>();
            band.Configure(floorTile, none, none, null, none, none, null, null, null, null, null, null);

            var (layout, built, parent) = Build(5);
            var overlay = parent.GetComponentsInChildren<Tilemap>().Single(t => t.name == "Caustics");
            var ground = parent.GetComponentsInChildren<Tilemap>().Single(t => t.name == "Ground");
            int checkedCells = 0;
            foreach (var cell in built.MapRooms[0].Rect.allPositionsWithin)
            {
                var position = new Vector3Int(cell.x, cell.y, 0);
                var tile = overlay.GetTile(position);
                // Todo lo que se dibuja como suelo, también los huecos de las puertas; nunca las paredes.
                bool open = ground.GetTile(position) != null;
                Assert.AreEqual(open, tile != null, $"Cáusticas en {cell} solo si se dibuja suelo.");
                if (open)
                    Assert.AreSame(caustics[((cell.x % 4) + 4) % 4 + 4 * (((cell.y % 4) + 4) % 4)], tile, "Mosaico de 4×4.");
                checkedCells++;
            }
            Assert.Greater(checkedCells, 0);
            layout.ReleaseGenerated();
        }

        [Test]
        public void Build_HangsTwoOrThreeLightRays_AndAddsMarineSnow()
        {
            band.ConfigureAmbience(null, Frames("Ray", 4), 0.25f, 30, null);
            floor2.ConfigureMarineSnow(Frames("Flake", 4), Track(new Material(Shader.Find("Sprites/Default"))));

            var (layout, built, _) = Build(6);
            foreach (var room in built.Rooms.Where(room => room.name != "Room_Boss"))
            {
                int rays = room.GetComponentsInChildren<SpriteLoop>(true).Length;
                Assert.That(rays, Is.InRange(1, 3), $"{room.name}: rayos de luz.");
                var snow = room.GetComponentInChildren<ParticleSystem>(true);
                Assert.IsNotNull(snow, $"{room.name}: nieve marina.");
                Assert.Greater(snow.main.maxParticles, 0);
            }
            layout.ReleaseGenerated();
        }

        [Test]
        public void Build_LightsTheGlowingPieces_WithAGlobalLight()
        {
            var spire = Sprite("Obstacle_Spire", 32, 32);
            band.ConfigureObstacles(new[] { Sprite("Obstacle_Rock") }, new[] { spire });
            band.ConfigureAmbience(null, null, 0.25f, 0, new[] { new Glow(spire, Color.cyan, 2f, new Vector2(0.3f, 0.3f), 0f) });

            var (layout, built, parent) = Build(7);
            var lights = parent.GetComponentsInChildren<Light2D>(true);
            var spires = built.Rooms.SelectMany(room => room.GetComponentsInChildren<SpriteRenderer>(true)).Count(r => r.sprite == spire);
            Assert.Greater(spires, 0);
            var globals = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude).Count(l => l.lightType == Light2D.LightType.Global);
            Assert.AreEqual(1, globals, "Una luz global para que lo demás no quede a oscuras.");
            Assert.AreEqual(spires, lights.Count(l => l.lightType == Light2D.LightType.Point), "Una luz por aguja.");
            layout.ReleaseGenerated();
        }

        [Test]
        public void Build_ReusesTheGlobalLight_OfTheScene()
        {
            var sceneLight = Track(new GameObject("SceneGlobalLight"));
            sceneLight.SetActive(false);
            sceneLight.AddComponent<Light2D>().lightType = Light2D.LightType.Global;
            sceneLight.SetActive(true);

            var spire = Sprite("Obstacle_Spire", 32, 32);
            band.ConfigureObstacles(new[] { Sprite("Obstacle_Rock") }, new[] { spire });
            band.ConfigureAmbience(null, null, 0.25f, 0, new[] { new Glow(spire, Color.cyan, 2f, new Vector2(0.3f, 0.3f), 0f) });

            var (layout, _, parent) = Build(7);
            Assert.IsEmpty(parent.GetComponentsInChildren<Light2D>(true).Where(l => l.lightType == Light2D.LightType.Global),
                "Con la luz global de la escena no se crea otra (dos a la vez dan error).");
            Assert.IsNotEmpty(parent.GetComponentsInChildren<Light2D>(true).Where(l => l.lightType == Light2D.LightType.Point));
            layout.ReleaseGenerated();
        }

        [Test]
        public void Obstacles_HaveAColoredOutline()
        {
            var rock = Sprite("Obstacle_Rock");
            var big = Sprite("Obstacle_RockBig", 32, 32);
            var rockOutline = Sprite("Obstacle_Rock_Outline", 18, 18);
            var bigOutline = Sprite("Obstacle_RockBig_Outline", 34, 34);
            band.ConfigureObstacles(new[] { rock }, new[] { big }, new[] { rockOutline }, new[] { bigOutline });
            var color = new Color(1f, 0.8f, 0.3f, 0.85f);
            band.ConfigureObstacleOutline(color);

            var (layout, built, _) = Build(3);
            var obstacles = built.Rooms.SelectMany(room => room.GetComponentsInChildren<BoxCollider2D>(true))
                .Where(c => c.name.StartsWith("Obstacle")).ToList();
            Assert.IsNotEmpty(obstacles);
            foreach (var obstacle in obstacles)
            {
                var sprite = obstacle.GetComponent<SpriteRenderer>();
                var outline = obstacle.transform.Find("Outline").GetComponent<SpriteRenderer>();
                Assert.AreSame(sprite.sprite == rock ? rockOutline : bigOutline, outline.sprite, "El contorno de su silueta.");
                Assert.AreEqual(color, outline.color, "Teñido con el color del tileset.");
                Assert.Less(outline.sortingOrder, sprite.sortingOrder, "Por detrás del obstáculo.");
            }
            layout.ReleaseGenerated();
        }

        [Test]
        public void Floor1_HasNoLights_NorAmbience()
        {
            var floor1 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor1.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal) }, new[] { Template(RoomLayouts.Small) }, new Vector2Int(3, 3), Vector2Int.zero);
            floor1.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor1.ConfigureRewards(null, System.Array.Empty<int>());
            floor1.ConfigureArt(Theme("Soup"), Theme("Boss"), null);

            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(floor1, new System.Random(1));
            FloorBuilder.Build(layout, floor1, parent, new System.Random(1));
            Assert.IsEmpty(parent.GetComponentsInChildren<Light2D>(true));
            Assert.IsEmpty(parent.GetComponentsInChildren<ParticleSystem>(true));
            Assert.IsEmpty(parent.GetComponentsInChildren<SpriteLoop>(true));
            Assert.IsFalse(parent.GetComponentsInChildren<Tilemap>().Any(t => t.name == "Caustics"));
        }
    }
}

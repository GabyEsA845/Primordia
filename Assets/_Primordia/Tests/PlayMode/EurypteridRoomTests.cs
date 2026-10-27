using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Sala del Euriptérido: rocas agrietadas (marca R) que se derrumban con la muda y dejan escombros sin colisión, y el
    /// derrumbe de la cueva (piedras del techo que dañan y suelo de las zonas Z que se hunde en fosa).
    /// </summary>
    public class EurypteridRoomTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            foreach (var effect in Object.FindObjectsByType<FrameEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(effect.gameObject);
            Pits.Map = null;
            NavigationGrid.Current = null;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        Sprite Sprite(string name)
        {
            var texture = Track(new Texture2D(16, 16));
            var sprite = Track(UnityEngine.Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f));
            sprite.name = name;
            return sprite;
        }

        Sprite[] Frames(string name, int count) => Enumerable.Range(0, count).Select(i => Sprite($"{name}_{i}")).ToArray();

        CollapsingRock Rock(Sprite[] frames, Sprite rubble, out GameObject outline, out BoxCollider2D blocker)
        {
            var go = Track(new GameObject("ObstacleCollapsing"));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite("Cracked");
            blocker = go.AddComponent<BoxCollider2D>();
            outline = new GameObject("Outline");
            outline.transform.SetParent(go.transform, false);
            var rock = go.AddComponent<CollapsingRock>();
            rock.Configure(renderer, outline, blocker, frames, rubble);
            return rock;
        }

        [UnityTest]
        public IEnumerator Collapse_StopsBlockingWhenItBreaks_AndLeavesRubble()
        {
            var frames = Frames("Collapse", 10);
            var rubble = Sprite("Rubble");
            var rock = Rock(frames, rubble, out var outline, out var blocker);
            var renderer = rock.GetComponent<SpriteRenderer>();

            Assert.IsTrue(rock.Blocks, "De pie bloquea como cualquier roca.");
            rock.Collapse();
            Assert.AreEqual(CollapsingRock.RockState.Collapsing, rock.State);
            Assert.IsFalse(outline.activeSelf, "El contorno de cobertura se apaga al empezar a caer.");
            Assert.IsTrue(rock.Blocks, "Mientras tiembla aún bloquea.");

            yield return new WaitForSeconds(0.09f * 3.5f);
            Assert.IsFalse(rock.Blocks, "Al partirse deja de bloquear.");

            yield return new WaitForSeconds(rock.Duration);
            Assert.IsTrue(rock.IsCollapsed);
            Assert.AreSame(rubble, renderer.sprite, "Quedan los escombros.");
            Assert.IsFalse(blocker.enabled, "Los escombros no tienen colisión.");
        }

        [Test]
        public void Collapse_WithoutFrames_LeavesRubbleAtOnce_AndOnlyHappensOnce()
        {
            var rubble = Sprite("Rubble");
            var rock = Rock(System.Array.Empty<Sprite>(), rubble, out _, out _);

            rock.Collapse();
            Assert.IsTrue(rock.IsCollapsed);
            Assert.IsFalse(rock.Blocks);
            Assert.AreSame(rubble, rock.GetComponent<SpriteRenderer>().sprite);

            rock.Collapse();
            Assert.IsTrue(rock.IsCollapsed, "Una roca derrumbada no vuelve a caer.");
        }

        [Test]
        public void CollapseAll_CollapsesEveryStandingRockOfTheRoom()
        {
            var room = Track(new GameObject("Room"));
            var a = Rock(Frames("A", 4), Sprite("RubbleA"), out _, out _);
            var b = Rock(Frames("B", 4), Sprite("RubbleB"), out _, out _);
            a.transform.SetParent(room.transform);
            b.transform.SetParent(room.transform);

            Assert.AreEqual(2, CollapsingRock.CollapseAll(room.transform));
            Assert.AreEqual(CollapsingRock.RockState.Collapsing, a.State);
            Assert.AreEqual(CollapsingRock.RockState.Collapsing, b.State);
            Assert.AreEqual(0, CollapsingRock.CollapseAll(room.transform), "Las que ya caen no cuentan.");
            Assert.AreEqual(0, CollapsingRock.CollapseAll(null));
        }

        [Test]
        public void Build_TurnsRMarksIntoCrackedRocksThatCollapse()
        {
            var rows = RoomLayouts.All[RoomLayouts.Normal].Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();
            // Una roca R de 2×2 (y desde abajo, como en la plantilla).
            for (int y = 6; y <= 7; y++)
            {
                int row = rows.Length - 1 - y;
                rows[row] = rows[row].Substring(0, 6) + "RR" + rows[row].Substring(8);
            }

            var floor = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var boss = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            boss.SetLayout(string.Join("\n", rows));
            var small = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            small.SetLayout(RoomLayouts.All[RoomLayouts.Small]);
            var theme = Track(ScriptableObject.CreateInstance<RoomTheme>());
            floor.ConfigureRooms(small, boss, new[] { small }, System.Array.Empty<RoomTemplate>(), Vector2Int.one, Vector2Int.zero);
            floor.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor.ConfigureRewards(null, System.Array.Empty<int>());
            floor.ConfigureArt(theme, theme, null);
            var cracked = Sprite("Cracked");
            var rubble = Sprite("Rubble");
            floor.ConfigurePieces(new FloorConfig.RoomPieces
            {
                crackedRock = cracked,
                crackedRockOutline = Sprite("CrackedOutline"),
                rockCollapse = Frames("Collapse", 10),
                rubble = rubble,
            });

            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(floor, new System.Random(1));
            FloorBuilder.Build(layout, floor, parent, new System.Random(1));

            var rocks = parent.GetComponentsInChildren<CollapsingRock>();
            Assert.AreEqual(1, rocks.Length, "Una roca por cada bloque R de 2×2.");
            var rock = rocks[0];
            Assert.AreSame(cracked, rock.GetComponent<SpriteRenderer>().sprite, "Se ve agrietada.");
            Assert.AreEqual(Vector2.one * 2f, rock.GetComponent<BoxCollider2D>().size, "Y bloquea sus 2×2.");
            Assert.IsNotNull(rock.transform.Find("Outline"), "Con su contorno de cobertura.");

            rock.Collapse();
            Assert.AreEqual(CollapsingRock.RockState.Collapsing, rock.State);
        }

        GameObject Target(string name, Vector2 position, bool player)
        {
            var go = Track(new GameObject(name));
            if (player)
                go.tag = "Player";
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            go.AddComponent<Health>();
            return go;
        }

        class RockTarget : MonoBehaviour, IFallingRockTarget
        {
            public int Damage;
            public float Stun;

            public void HitByFallingRock(int damage, float stunDuration)
            {
                Damage += damage;
                Stun = stunDuration;
            }
        }

        [UnityTest]
        public IEnumerator FallingRock_WarnsFirst_ThenHurtsWhoIsUnderIt()
        {
            var under = Target("Player", new Vector2(0.4f, 0f), player: true);
            var away = Target("Away", new Vector2(3f, 0f), player: false);
            var boss = Target("Boss", new Vector2(-0.4f, 0.2f), player: false);
            var bossTarget = boss.AddComponent<RockTarget>();
            yield return null;

            var rock = FallingRock.Spawn(null, Vector2.zero, Frames("Shadow", 6), Frames("Rock", 4), Frames("Impact", 6));
            Track(rock.gameObject);
            yield return new WaitForSeconds(rock.Warning * 0.6f);
            Assert.AreEqual(6, under.GetComponent<Health>().Current, "Durante el aviso no hace nada.");

            yield return new WaitForSeconds(rock.Warning * 0.6f);
            Assert.AreEqual(5, under.GetComponent<Health>().Current, "Al caer quita medio corazón al de debajo.");
            Assert.AreEqual(6, away.GetComponent<Health>().Current, "Fuera de la sombra no pasa nada.");
            Assert.Greater(bossTarget.Damage, 0, "El jefe recibe daño a su manera.");
            Assert.AreEqual(1f, bossTarget.Stun, 0.001f, "Y queda aturdido un segundo.");
        }

        /// <summary>Sala con un mapa de suelo de 20×12 y zonas de 3×3 desde (4, 4).</summary>
        CaveCollapse Cave(out FloorMap map, out RectInt zone, int zones = 1)
        {
            map = new FloorMap(new RectInt(0, 0, 20, 12));
            foreach (var cell in map.Bounds.allPositionsWithin)
                map.Set(cell, FloorMap.Kind.Floor);
            Pits.Map = map;
            NavigationGrid.Current = new NavigationGrid(map);

            zone = new RectInt(4, 4, 3, 3);
            var list = new List<RectInt> { zone };
            for (int i = 1; i < zones; i++)
                list.Add(new RectInt(4 + i * 3, 4, 3, 3));
            var room = Track(new GameObject("Room"));
            var cave = room.AddComponent<CaveCollapse>();
            cave.Configure(null, map.Bounds, list, null, null, null, new CaveCollapse.Art
            {
                fallShadow = Frames("Shadow", 6),
                fallingRock = Frames("Rock", 4),
                rockImpact = Frames("Impact", 6),
                pebbles = Frames("Pebble", 3),
                pebblePuff = Frames("Puff", 4),
                floorCrack = Frames("Crack", 8),
            });
            return cave;
        }

        [UnityTest]
        public IEnumerator SinkNext_CracksTheZone_ThenTurnsItIntoPit()
        {
            Target("Player", new Vector2(15f, 9f), player: true);
            var cave = Cave(out var map, out var zone);
            cave.SetMode(CaveCollapse.Mode.Collapse);
            Assert.AreEqual(1, cave.PendingZones);

            Assert.IsTrue(cave.SinkNext());
            Assert.AreEqual(FloorMap.Kind.Floor, map.Get(zone.position), "Durante el aviso sigue siendo suelo.");
            yield return new WaitForSeconds(cave.SinkWarning + 0.1f);

            foreach (var cell in zone.allPositionsWithin)
                Assert.AreEqual(FloorMap.Kind.Pit, map.Get(cell), $"La celda {cell} se hunde.");
            Assert.IsFalse(NavigationGrid.Current.IsWalkable(zone.position), "Los que andan ya no pasan por ahí.");
            Assert.AreEqual(FloorMap.Kind.Floor, map.Get(new Vector2Int(3, 4)), "Lo de alrededor sigue en pie.");
            Assert.AreEqual(1, cave.SunkZones);
            Assert.IsFalse(cave.SinkNext(), "No quedan zonas.");
            cave.SetMode(CaveCollapse.Mode.Calm);
        }

        [Test]
        public void SinkNext_NeverPicksTheZoneThePlayerIsOn()
        {
            Target("Player", new Vector2(5.5f, 5.5f), player: true);
            var cave = Cave(out _, out _);
            cave.SetMode(CaveCollapse.Mode.Collapse);
            Assert.IsFalse(cave.SinkNext(), "Con el jugador encima, esa zona espera.");
            Assert.AreEqual(1, cave.PendingZones);
            cave.SetMode(CaveCollapse.Mode.Calm);
        }

        [Test]
        public void Collapse_SinksThreeOrFourZonesPerFight()
        {
            var cave = Cave(out _, out _, zones: 5);
            cave.SetMode(CaveCollapse.Mode.Collapse);
            Assert.That(cave.PendingZones, Is.InRange(3, 4));
            cave.SetMode(CaveCollapse.Mode.Calm);
        }

        [Test]
        public void Quake_CollapsesTheCrackedRocks_AndShakes_UntilCalm()
        {
            var cave = Cave(out _, out _);
            var rock = Rock(Frames("Collapse", 4), Sprite("Rubble"), out _, out _);
            rock.transform.SetParent(cave.transform);

            cave.SetMode(CaveCollapse.Mode.Quake);
            Assert.AreNotEqual(CollapsingRock.RockState.Standing, rock.State, "El terremoto derrumba las rocas agrietadas.");
            Assert.Greater(ScreenShake.RumbleOf(cave), 0f, "Y la pantalla tiembla.");

            cave.SetMode(CaveCollapse.Mode.Calm);
            Assert.AreEqual(0f, ScreenShake.RumbleOf(cave), "En calma deja de temblar.");
        }

        [Test]
        public void DropVolley_DropsOneToThreeRocks_TheFirstAtThePlayer()
        {
            var player = Target("Player", new Vector2(10f, 6f), player: true);
            var cave = Cave(out _, out _);
            cave.DropVolley();
            var rocks = cave.GetComponentsInChildren<FallingRock>();
            Assert.That(rocks.Length, Is.InRange(1, 3));
            Assert.IsTrue(rocks.Any(r => Vector2.Distance(r.transform.position, player.transform.position) < 0.01f),
                "Una cae justo donde está el jugador quieto.");
        }

        [Test]
        public void Build_GivesTheRoomItsCaveCollapse_WithTheZZones()
        {
            var rows = RoomLayouts.All[RoomLayouts.Normal].Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();
            for (int y = 3; y <= 5; y++)
            {
                int row = rows.Length - 1 - y;
                rows[row] = rows[row].Substring(0, 15) + "ZZZ" + rows[row].Substring(18);
            }

            var floor = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var boss = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            boss.SetLayout(string.Join("\n", rows));
            Assert.AreEqual(new RectInt(15, 3, 3, 3), boss.SinkZones().Single());
            var small = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            small.SetLayout(RoomLayouts.All[RoomLayouts.Small]);
            var theme = Track(ScriptableObject.CreateInstance<RoomTheme>());
            floor.ConfigureRooms(small, boss, new[] { small }, System.Array.Empty<RoomTemplate>(), Vector2Int.one, Vector2Int.zero);
            floor.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor.ConfigureRewards(null, System.Array.Empty<int>());
            floor.ConfigureArt(theme, theme, null);

            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(floor, new System.Random(1));
            var built = FloorBuilder.Build(layout, floor, parent, new System.Random(1));

            var caves = parent.GetComponentsInChildren<CaveCollapse>();
            Assert.AreEqual(1, caves.Length, "Solo la sala con zonas Z lleva derrumbe.");
            Assert.AreEqual(1, caves[0].Zones.Count);
            Assert.AreEqual(FloorMap.Kind.Floor, built.Map.Get(caves[0].Zones[0].position), "Las zonas Z empiezan siendo suelo.");
        }
    }
}

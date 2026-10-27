using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class FloorTransitionTests
    {
        readonly List<Object> created = new();
        FloorConfig config;
        TransitionPit pitTemplate;

        [SetUp]
        public void SetUp()
        {
            var pitObject = Track(new GameObject("TransitionPit"));
            pitObject.SetActive(false);
            pitObject.AddComponent<CapsuleCollider2D>();
            pitTemplate = pitObject.AddComponent<TransitionPit>();

            config = Track(ScriptableObject.CreateInstance<FloorConfig>());
            config.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal), Template(RoomLayouts.Pillars), Template(RoomLayouts.Large) },
                new[] { Template(RoomLayouts.Small) },
                new Vector2Int(4, 5), new Vector2Int(1, 2));
            config.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            config.ConfigureRewards(null, System.Array.Empty<int>());
            config.ConfigureTransition(Template(RoomLayouts.TransitionCave), pitTemplate, Theme("Mid"), Theme("Next"));
            config.ConfigureArt(Theme("Soup"), Theme("Boss"), null);
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
            Time.timeScale = 1f;
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

        [Test]
        public void Cave_CanBeDrawn_AndHasADoorOnEachSide()
        {
            var cave = Template(RoomLayouts.TransitionCave);

            CollectionAssert.IsEmpty(cave.Validate());
            foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                Assert.AreEqual(2, cave.DoorCells(direction).Count, $"Puerta hacia {direction}.");
        }

        [Test]
        public void Layout_PutsTheTransitionRoom_RightAfterTheBoss()
        {
            for (int seed = 1; seed <= 300; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                string context = $"semilla {seed}";

                var transition = layout.Transition;
                Assert.IsNotNull(transition, context);
                Assert.AreEqual(1, transition.Doors.Count, $"La sala de paso no tiene otra salida ({context}).");
                Assert.AreEqual(layout.Boss, layout.Links.Single(link => link.to == transition).from, context);
                Assert.AreEqual(2, layout.Boss.Doors.Count, $"El jefe solo da a la sala de paso ({context}).");
                Assert.AreEqual(layout.Boss.Depth + 1, transition.Depth, context);

                foreach (var room in layout.Rooms.Where(r => r.Role is RoomRole.Side or RoomRole.Shop))
                {
                    var parent = layout.Links.Single(link => link.to == room).from;
                    Assert.AreEqual(RoomRole.Combat, parent.Role, $"{room.Name} cuelga de una sala de combate ({context}).");
                }
            }
        }

        [Test]
        public void Layout_WithoutTransition_EndsAtTheBoss()
        {
            config.ConfigureTransition(null, null, null, null);
            var layout = FloorLayoutGenerator.Generate(config, new System.Random(7));

            Assert.IsNull(layout.Transition);
            Assert.AreEqual(1, layout.Boss.Doors.Count);
        }

        [Test]
        public void Build_GoesFromThisFloorToTheNext_AroundThePit()
        {
            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(config, new System.Random(42));
            var built = FloorBuilder.Build(layout, config, parent, new System.Random(42));

            var transition = built.MapRooms.Single(room => room.Role == RoomRole.Transition);
            var pit = transition.Room.GetComponentInChildren<TransitionPit>(true);
            Assert.IsNotNull(pit, "La fosa está en la sala de paso.");

            var pitCell = Vector2Int.FloorToInt(pit.transform.position);
            Assert.AreEqual(3, built.Map.GetTheme(pitCell), "Junto a la fosa, el tema del piso siguiente.");

            var themes = new HashSet<int>();
            foreach (var cell in transition.Rect.allPositionsWithin)
            {
                if (built.Map.Get(cell) == FloorMap.Kind.Floor)
                    themes.Add(built.Map.GetTheme(cell));
            }
            CollectionAssert.IsSubsetOf(new[] { 0, 2, 3 }, themes, "Del piso actual, al intermedio y al siguiente.");

            var boss = built.MapRooms.Single(room => room.Role == RoomRole.Boss);
            Assert.IsNull(boss.Room.transform.Find("Exit"), "Con sala de paso, el jefe no deja la salida de siempre.");
        }

        [Test]
        public void Build_TheCorridorToTheTransitionRoom_IsLong()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var parent = Track(new GameObject("Floor")).transform;
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var built = FloorBuilder.Build(layout, config, parent, new System.Random(seed));

                var corridor = built.Corridors.Single(c => c.A.Role == RoomRole.Transition || c.B.Role == RoomRole.Transition);
                // Dos carriles de ancho: cada uno mide al menos el pasillo largo.
                Assert.That(corridor.Cells.Count / 2, Is.GreaterThanOrEqualTo(FloorBuilder.TransitionCorridorLength), $"semilla {seed}");
                foreach (var other in built.Corridors.Where(c => c != corridor))
                    Assert.That(other.Cells.Count / 2, Is.LessThan(FloorBuilder.TransitionCorridorLength + 20), $"semilla {seed}");
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Music_FadesOut_AndTheOceanFadesIn_NearThePit()
        {
            if (AudioManager.Instance != null)
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            // El director lee el piso montado al empezar: otra prueba puede haber dejado el 2 (los static no se limpian
            // entre pruebas).
            FloorGenerator.CurrentFloor = 1;
            var library = Track(ScriptableObject.CreateInstance<SoundLibrary>());
            library.Configure(new[]
            {
                new SoundLibrary.Sound { id = Sounds.AmbFloor1, clips = new[] { Clip("Floor1") }, volume = 1f, loop = true },
                new SoundLibrary.Sound { id = Sounds.AmbFloor2, clips = new[] { Clip("Floor2") }, volume = 1f, loop = true },
            }, System.Array.Empty<SoundLibrary.Track>());
            var audio = AudioManager.Create(library);
            Track(audio.gameObject);
            var director = audio.GetComponent<MusicDirector>();

            var player = Track(new GameObject("Player") { tag = "Player" });
            player.transform.position = new Vector3(100f, 0f, 0f);
            CreatePit(Vector2.zero);
            yield return null;
            yield return null;

            Assert.AreEqual(0f, director.TransitionBlend, "Lejos de la fosa, el piso suena como siempre.");
            Assert.AreEqual(1f, audio.Music.DistanceGain);
            Assert.IsFalse(audio.IsLoopPlaying(Sounds.AmbFloor2));

            player.transform.position = new Vector3((MusicDirector.TransitionFadeFrom + MusicDirector.TransitionFadeTo) / 2f, 0f, 0f);
            yield return null;
            Assert.That(director.TransitionBlend, Is.InRange(0.2f, 0.8f), "A medio camino, mezcla de los dos.");
            Assert.That(audio.Music.DistanceGain, Is.InRange(0.2f, 0.8f));
            Assert.IsTrue(audio.IsLoopPlaying(Sounds.AmbFloor2));

            player.transform.position = new Vector3(MusicDirector.TransitionFadeTo - 1f, 0f, 0f);
            yield return null;
            Assert.AreEqual(1f, director.TransitionBlend, "En el borde solo suena el océano.");
            Assert.AreEqual(0f, audio.Music.DistanceGain);
        }

        static AudioClip Clip(string name) => AudioClip.Create(name, 44100, 1, 44100, false);

        [Test]
        public void EdgePoint_IsOnTheRim_TowardsThePlayer()
        {
            var pit = CreatePit(Vector2.zero);

            Assert.That(Vector2.Distance(new Vector2(3.1f, 0f), pit.EdgePoint(new Vector2(10f, 0f))), Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(new Vector2(0f, -2.1f), pit.EdgePoint(new Vector2(0f, -8f))), Is.LessThan(0.001f));

            var diagonal = pit.EdgePoint(new Vector2(5f, 5f));
            float onEllipse = Mathf.Pow(diagonal.x / 3.1f, 2f) + Mathf.Pow(diagonal.y / 2.1f, 2f);
            Assert.That(onEllipse, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Interact_JumpsIntoThePit_AndWins()
        {
            var manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();
            var player = Track(new GameObject("Player") { tag = "Player" });
            player.transform.position = new Vector3(0f, -3f, 0f);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            var pit = CreatePit(Vector2.zero);
            pit.ConfigureJump(0.1f, 0.1f);
            yield return null;

            Assert.IsTrue(pit.IsAvailable);
            pit.Interact(player);

            Assert.IsTrue(pit.Used);
            Assert.IsFalse(pit.IsAvailable, "Solo se salta una vez.");
            Assert.AreEqual(GameState.Cinematic, manager.State, "Durante el salto el jugador no controla nada.");
            Assert.IsFalse(body.simulated, "Sin física mientras cruza el borde.");

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(GameState.Victory, manager.State, "Mientras no haya piso siguiente, la caída da la victoria.");
        }

        TransitionPit CreatePit(Vector2 position)
        {
            var go = Track(new GameObject("Pit"));
            go.transform.position = position;
            go.AddComponent<CapsuleCollider2D>();
            var pit = go.AddComponent<TransitionPit>();
            pit.Configure(null, null, null, null, null, new Vector2(3.1f, 2.1f));
            return pit;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Sala del Anomalocaris: columnas de algas que se transparentan, plantas luminosas y penumbra al entrar.</summary>
    public class AnomalocarisRoomTests
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

        GameObject Player(Vector2 position)
        {
            var player = Track(new GameObject("Player") { tag = "Player" });
            player.transform.position = position;
            return player;
        }

        LightPlant Plant(Vector2 position, out Sprite[] offFrames)
        {
            var piece = Track(new GameObject("LightPlant"));
            piece.transform.position = position;
            var renderer = piece.AddComponent<SpriteRenderer>();
            var loop = piece.AddComponent<SpriteLoop>();
            loop.Configure(renderer, Frames("Glow", 4), 0.22f, 0f);
            var glow = Ambience.CreateGlow(piece.transform, position, new Glow(null, Color.cyan, 4f, new Vector2(0.75f, 1.05f), 0.88f), 0f);
            offFrames = Frames("Off", 3);
            var plant = piece.AddComponent<LightPlant>();
            plant.Configure(renderer, loop, offFrames, glow, glow.GetComponent<GlowPulse>());
            return plant;
        }

        [UnityTest]
        public IEnumerator AlgaeColumn_TurnsSeeThrough_OnlyWithThePlayerUnderIt()
        {
            var player = Player(new Vector2(0f, 2f));
            var column = Track(new GameObject("AlgaeColumn"));
            var renderer = column.AddComponent<SpriteRenderer>();
            var algae = column.AddComponent<AlgaeColumn>();
            algae.Configure(renderer);

            Assert.IsTrue(algae.Covers(new Vector2(0f, 2f)));
            Assert.IsFalse(algae.Covers(new Vector2(0f, -1f)), "Por debajo de la base no tapa.");
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(0.4f, algae.Alpha, 0.01f, "Con el jugador debajo se vuelve translúcida.");

            player.transform.position = new Vector2(6f, 2f);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1f, algae.Alpha, 0.01f, "Sin el jugador debajo, opaca otra vez.");
        }

        [UnityTest]
        public IEnumerator LightPlant_GoesDark_AndLightsUpAgain()
        {
            var plant = Plant(Vector2.zero, out var offFrames);
            var renderer = plant.GetComponent<SpriteRenderer>();

            plant.SetLit(false, 0.2f);
            Assert.IsFalse(plant.Lit);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0f, plant.Glow.intensity, 0.001f, "Apagada, su luz no alumbra.");
            Assert.AreSame(offFrames[^1], renderer.sprite, "Se queda en el último cuadro de apagarse.");
            Assert.IsFalse(plant.GetComponent<SpriteLoop>().enabled);

            plant.SetLit(true, 0.2f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(plant.Lit);
            Assert.IsTrue(plant.GetComponent<SpriteLoop>().enabled, "Encendida, vuelve a latir.");
            Assert.Greater(plant.Glow.intensity, 0.5f);
        }

        [UnityTest]
        public IEnumerator RoomDarkness_DimsTheGlobalLight_WithAHaloOnThePlayer_AndRestoresIt()
        {
            var player = Player(Vector2.zero);
            var roomObject = Track(new GameObject("Room"));
            var global = Ambience.CreateGlobalLight(roomObject.transform);
            Track(global.gameObject);
            float normal = global.intensity;
            var normalColor = global.color;
            var plant = Plant(new Vector2(3f, 0f), out _);
            var darkness = roomObject.AddComponent<RoomDarkness>();
            darkness.Configure(null, new[] { plant });

            darkness.Dim();
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(1f, darkness.Darkness, 0.001f);
            Assert.AreEqual(0.12f, global.intensity, 0.01f, "Penumbra: se ve, pero poco.");
            Assert.Less(global.color.r, global.color.b, "Y azulada, de fondo marino.");
            var halo = player.GetComponentsInChildren<Light2D>().FirstOrDefault();
            Assert.IsNotNull(halo, "El jugador lleva un halo tenue.");
            Assert.AreEqual(0.7f, halo.intensity, 0.01f);
            Assert.AreEqual(2.5f, halo.pointLightOuterRadius, 0.01f);

            darkness.SetPlantsLit(false, 0f);
            Assert.IsFalse(plant.Lit, "La sala apaga sus plantas (escena de los ojos).");
            darkness.SetPlantsLit(true, 0f);

            darkness.Restore();
            yield return new WaitForSeconds(2.7f);
            Assert.AreEqual(normal, global.intensity, 0.001f, "Al despejar la sala, la luz vuelve.");
            Assert.AreEqual(normalColor, global.color, "Con su color de siempre.");
            Assert.IsEmpty(player.GetComponentsInChildren<Light2D>(), "Y el halo se va.");
        }

        /// <summary>Piso de prueba cuya sala del jefe es la Normal con estas filas; devuelve el padre del piso construido.</summary>
        Transform BuildWithBossRoom(string[] rows, out FloorConfig floor)
        {
            floor = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var boss = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            boss.SetLayout(string.Join("\n", rows));
            var small = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            small.SetLayout(RoomLayouts.All[RoomLayouts.Small]);
            var theme = Track(ScriptableObject.CreateInstance<RoomTheme>());
            floor.ConfigureRooms(small, boss, new[] { small }, System.Array.Empty<RoomTemplate>(), Vector2Int.one, Vector2Int.zero);
            floor.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor.ConfigureRewards(null, System.Array.Empty<int>());
            floor.ConfigureArt(theme, theme, null);
            floor.ConfigurePieces(new FloorConfig.RoomPieces
            {
                algaeColumnA = Frames("AlgaeA", 4),
                algaeColumnB = Frames("AlgaeB", 4),
                lightPlant = Frames("Plant", 4),
                lightPlantOff = Frames("PlantOff", 3),
            });

            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(floor, new System.Random(1));
            FloorBuilder.Build(layout, floor, parent, new System.Random(1));
            return parent;
        }

        static string[] NormalRows() =>
            RoomLayouts.All[RoomLayouts.Normal].Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();

        static void Put(string[] rows, int x, int y, char mark)
        {
            // y desde abajo, como en la plantilla.
            int row = rows.Length - 1 - y;
            rows[row] = rows[row].Substring(0, x) + mark + rows[row].Substring(x + 1);
        }

        [Test]
        public void Build_PutsAlgaeAboveEverything_AndPlantsWithLight_AndMakesTheRoomDark()
        {
            var rows = NormalRows();
            rows[8] = rows[8].Substring(0, 5) + "A" + rows[8].Substring(6, 2) + "L" + rows[8].Substring(9);
            var parent = BuildWithBossRoom(rows, out _);

            var algae = parent.GetComponentsInChildren<AlgaeColumn>(true);
            Assert.AreEqual(1, algae.Length);
            Assert.IsNull(algae[0].GetComponent<Collider2D>(), "Las algas no bloquean.");
            Assert.AreEqual(AlgaeColumn.SortingOrder, algae[0].GetComponent<SpriteRenderer>().sortingOrder);

            var plants = parent.GetComponentsInChildren<LightPlant>(true);
            Assert.AreEqual(1, plants.Length);
            Assert.IsNotNull(plants[0].Glow, "Cada planta lleva su luz.");
            Assert.Greater(plants[0].Glow.pointLightInnerRadius, 1f, "Con un círculo de luz plena alrededor.");

            var dark = parent.GetComponentsInChildren<RoomDarkness>(true);
            Assert.AreEqual(1, dark.Length, "Solo la sala con plantas luminosas se queda en penumbra.");
            CollectionAssert.AreEquivalent(plants, dark[0].Plants);
        }

        [Test]
        public void Build_LeavesTheBossSpotClear_OfAlgaeAndPlants()
        {
            var rows = NormalRows();
            // Las de lejos se quedan.
            rows[8] = rows[8].Substring(0, 5) + "A" + rows[8].Substring(6, 2) + "L" + rows[8].Substring(9);
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout(string.Join("\n", rows));
            var bounds = template.FloorBounds();
            var defaults = Track(ScriptableObject.CreateInstance<FloorConfig>());
            // Donde aparece el jefe: centro del suelo más su desplazamiento.
            var spot = new Vector2(bounds.x + bounds.width / 2f, bounds.y + bounds.height / 2f) + defaults.BossOffset;
            int sx = Mathf.FloorToInt(spot.x), sy = Mathf.FloorToInt(spot.y);
            Put(rows, sx, sy, 'L');
            Put(rows, sx + 2, sy - 1, 'A');

            var parent = BuildWithBossRoom(rows, out _);

            Assert.AreEqual(1, parent.GetComponentsInChildren<LightPlant>(true).Length, "La planta del sitio del jefe no se pone.");
            Assert.AreEqual(1, parent.GetComponentsInChildren<AlgaeColumn>(true).Length, "Ni las algas que lo taparían.");
        }
    }
}

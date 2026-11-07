using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Salas especiales del piso 3 (veta de carbón, fumarolas, ladronas): marcas de la plantilla y vida de una casilla de carbón.</summary>
    public class SpecialRoomsTests
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

        Sprite[] Frames(int count)
        {
            var texture = Track(new Texture2D(4, 4));
            var sprite = Track(Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f)));
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = sprite;
            return frames;
        }

        [Test]
        public void Template_ReadsCoalVentAndPileMarks_AsWalkableFloor()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout("######\n######\n######\n#cVP.#\n######\n");
            Assert.AreEqual(RoomTemplate.Mark.Coal, template.Get(1, 1));
            Assert.AreEqual(RoomTemplate.Mark.Vent, template.Get(2, 1));
            Assert.AreEqual(RoomTemplate.Mark.Pile, template.Get(3, 1));
            Assert.IsTrue(RoomTemplate.IsInterior(RoomTemplate.Mark.Coal));
            Assert.IsFalse(RoomTemplate.IsObstacle(RoomTemplate.Mark.Coal), "Se pisa: es suelo.");
            Assert.AreEqual(1, template.CellsOf(RoomTemplate.Mark.Vent).Count);
        }

        [Test]
        public void Template_MudWithAPile_IsDeepMudInTheFloorMap_AndCoalIsNot()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout("######\n######\n######\n#mMc.#\n######\n");
            var map = template.ToFloorMap(Vector2Int.zero, openDoors: false);
            Assert.AreEqual(FloorMap.Kind.Mud, map.Get(1, 1));
            Assert.AreEqual(FloorMap.Kind.Mud, map.Get(2, 1), "El lodo con biomasa también hunde.");
            Assert.AreEqual(FloorMap.Kind.Floor, map.Get(3, 1));
        }

        [Test]
        public void Template_ReadsBubblerAndPlantMarks_AsWalkableFloor()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout("######\n######\n######\n#bY.##\n######\n");
            Assert.AreEqual(RoomTemplate.Mark.Bubbler, template.Get(1, 1));
            Assert.AreEqual(RoomTemplate.Mark.Plant, template.Get(2, 1));
            Assert.IsTrue(RoomTemplate.IsInterior(RoomTemplate.Mark.Bubbler));
            Assert.IsFalse(RoomTemplate.IsObstacle(RoomTemplate.Mark.Plant));
        }

        [Test]
        public void Template_LogCells_PaintAsMud_ButAreWalkableInterior()
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.SetLayout("######\n######\n######\n#mT.##\n######\n");
            Assert.AreEqual(RoomTemplate.Mark.Log, template.Get(2, 1));
            Assert.IsTrue(RoomTemplate.IsInterior(RoomTemplate.Mark.Log));
            var map = template.ToFloorMap(Vector2Int.zero, openDoors: false);
            Assert.AreEqual(FloorMap.Kind.Mud, map.Get(2, 1), "Se pinta como lodo para que el lodo rodee el tronco.");
        }

        [Test]
        public void ImportedArt_HasTheLogTheConeChestsAndTheStumpLight()
        {
            var art = SpecialRoomArt.Current;
            Assert.IsNotNull(art, "Falta Resources/SpecialRoomArt: Primordia > Floor 3 > Importar arte de las salas especiales.");
            for (int band = 0; band < SpecialRoomArt.Bands; band++)
            {
                var log = art.LogFor(band);
                Assert.AreEqual(6, log.mid.Length, $"tramo {band}: 6 variantes del tronco.");
                Assert.AreEqual(8, log.mid[0].idle.frames.Length);
                Assert.AreEqual(6, log.mid[0].step.frames.Length);
                Assert.AreEqual(4, log.mid[0].dip.frames.Length);
                Assert.IsNotNull(log.endLeft);
                Assert.IsNotNull(log.endRight);
            }
            Assert.AreEqual(7, art.mudBubble.Length);
            Assert.AreEqual(6, art.mudBubbleSmall.Length);
            Assert.AreEqual(8, art.stumpLight.Length);
            Assert.IsTrue(art.coneBig.IsValid && art.coneSmall.IsValid);
            Assert.AreEqual(4, art.coneBig.idle.Length);
            Assert.AreEqual(6, art.coneBig.open.Length);
        }

        [Test]
        public void ConeChest_UsesTheRealArt_AndReleasesOnTheFourthOpeningFrame()
        {
            var parent = Track(new GameObject("Chests")).transform;
            var big = ConeChest.Spawn(parent, Vector2.zero, null, null, null);
            var small = ConeChest.Spawn(parent, Vector2.right * 4f, null, null, null, large: false);
            Assert.AreEqual(SpecialRoomArt.Current.coneBig.idle[0], big.GetComponent<SpriteRenderer>().sprite);
            Assert.AreEqual(SpecialRoomArt.Current.coneSmall.idle[0], small.GetComponent<SpriteRenderer>().sprite);
            Assert.AreEqual(new Vector2(2f, 1f), big.GetComponent<BoxCollider2D>().size);
            Assert.AreEqual(Vector2.one, small.GetComponent<BoxCollider2D>().size);
        }

        [Test]
        public void FallenLog_And_HollowStump_AreBuilt_AndTheLogCellsCanBeWalked()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<FloorConfig>("Assets/_Primordia/Data/Rooms/Floor_3_SpecialRooms.asset");
            Assert.IsNotNull(config, "falta Floor_3_SpecialRooms: Primordia > Floor 3 > Crear escena de prueba de las salas especiales.");
            bool log = false, stump = false;
            for (int seed = 1; seed <= 6 && !(log && stump); seed++)
            {
                var parent = Track(new GameObject("Floor")).transform;
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var built = FloorBuilder.Build(layout, config, parent, new System.Random(seed));
                foreach (var room in layout.Rooms)
                {
                    if (room.Special is { template: not null } special && special.template.CellsOf(RoomTemplate.Mark.Log).Count > 0)
                    {
                        log = true;
                        Assert.IsNotNull(parent.GetComponentInChildren<FallenLog>(), $"semilla {seed}: falta el tronco.");
                        var mapRoom = built.MapRooms.First(r => r.Role == room.Role && r.Rect.size == new Vector2Int(room.Template.Width, room.Template.Height));
                        foreach (var cell in room.Template.CellsOf(RoomTemplate.Mark.Log))
                            Assert.AreEqual(FloorMap.Kind.Floor, built.Map.Get(mapRoom.Rect.position + cell), "Las casillas del tronco se andan.");
                    }
                    if (room.Special is { coneChest: true })
                    {
                        stump = true;
                        Assert.IsNotNull(parent.GetComponentInChildren<StumpLight>(), $"semilla {seed}: falta el rayo de luz.");
                    }
                }
                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
            }
            Assert.IsTrue(log && stump, "Las dos salas salen en la escena de prueba.");
        }

        [Test]
        public void SmallExplosion_HurtsWhoIsInsideItsRadius_AndSparesWhoIsOutside()
        {
            var near = Track(new GameObject("Near"));
            near.AddComponent<CircleCollider2D>().radius = 0.3f;
            var nearHealth = near.AddComponent<Health>();
            nearHealth.SetMax(6, true);
            var far = Track(new GameObject("Far"));
            far.transform.position = new Vector3(5f, 0f, 0f);
            far.AddComponent<CircleCollider2D>().radius = 0.3f;
            var farHealth = far.AddComponent<Health>();
            farHealth.SetMax(6, true);
            Physics2D.SyncTransforms();

            Explosion.Detonate(Vector2.zero, 1.7f, 3, 1, false);
            Assert.AreEqual(3, nearHealth.Current, "Un enemigo cercano recibe 3.");
            Assert.AreEqual(6, farHealth.Current, "El lejano, nada.");
        }

        [UnityTest]
        public IEnumerator CoalCell_BurnsOnIgnite_ThenEmbers_ThenGoesOutForever()
        {
            var art = Track(ScriptableObject.CreateInstance<SpecialRoomArt>());
            art.ignite = Frames(2);
            art.SetBurn(Frames(2), Frames(2), Frames(2));
            art.embers = Frames(2);
            art.coalOut = Frames(1)[0];
            var parent = Track(new GameObject("Seam"));
            var cell = CoalCell.Create(parent.transform, Vector2.zero, Frames(1)[0], new Sprite[0], art, 0);

            Assert.IsTrue(cell.CanIgnite, "Apagada hasta que le llega fuego.");
            Assert.AreEqual(1, Flammables.Near(Vector2.zero, 1f).Count, "El fuego del suelo la encuentra.");

            cell.Ignite();
            Assert.IsFalse(cell.CanIgnite);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(cell.IsBurning, "Prende y arde.");
            Assert.IsFalse(cell.IsEmbers);
            Assert.IsFalse(cell.IsOut);
        }

        [UnityTest]
        public IEnumerator CoalCell_Burning_HurtsThePlayerWhoStandsOnIt()
        {
            var art = Track(ScriptableObject.CreateInstance<SpecialRoomArt>());
            art.ignite = Frames(1);
            art.SetBurn(Frames(2), Frames(2), Frames(2));
            art.embers = Frames(2);
            art.coalOut = Frames(1)[0];
            var parent = Track(new GameObject("Seam"));
            var player = Track(new GameObject("Player"));
            player.AddComponent<CircleCollider2D>().radius = 0.3f;
            var health = player.AddComponent<Health>();
            health.SetMax(6, true);
            player.AddComponent<PlayerController>().readInput = false;
            player.AddComponent<PlayerBurn>();
            var cell = CoalCell.Create(parent.transform, Vector2.zero, Frames(1)[0], new Sprite[0], art, 0);

            cell.Ignite();
            yield return new WaitForSeconds(0.5f);
            Assert.Less(health.Current, health.Max, "Arde y quema de verdad, no solo se ve.");
        }

        [UnityTest]
        public IEnumerator CoalCell_Burning_SpreadsToTheNextCellByItself()
        {
            var art = Track(ScriptableObject.CreateInstance<SpecialRoomArt>());
            art.ignite = Frames(1);
            art.SetBurn(Frames(2), Frames(2), Frames(2));
            art.embers = Frames(2);
            art.coalOut = Frames(1)[0];
            var parent = Track(new GameObject("Seam"));
            var first = CoalCell.Create(parent.transform, Vector2.zero, Frames(1)[0], new Sprite[0], art, 0);
            var second = CoalCell.Create(parent.transform, Vector2.right, Frames(1)[0], new Sprite[0], art, 1);
            var far = CoalCell.Create(parent.transform, Vector2.right * 6f, Frames(1)[0], new Sprite[0], art, 2);

            first.Ignite();
            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(second.CanIgnite, "El fuego salta a la casilla de al lado.");
            Assert.IsTrue(far.CanIgnite, "Pero no a la lejana.");
        }

        [UnityTest]
        public IEnumerator CoalCell_Lit_NeverGoesOut_AndSparksIgniteTheSeamNextToIt()
        {
            var art = Track(ScriptableObject.CreateInstance<SpecialRoomArt>());
            art.ignite = Frames(1);
            art.SetBurn(Frames(2), Frames(2), Frames(2));
            art.embers = Frames(2);
            art.coalOut = Frames(1)[0];
            art.floatingEmbers = Frames(2);
            var parent = Track(new GameObject("Seam"));
            var seam = CoalCell.Create(parent.transform, Vector2.zero, Frames(1)[0], new Sprite[0], art, 0);
            var lit = CoalCell.CreateLit(parent.transform, Vector2.up * 2f, Frames(1)[0], new Sprite[0], art, 1);
            Assert.IsTrue(lit.IsLit);
            Assert.IsTrue(lit.IsEmbers);
            Assert.IsFalse(lit.CanIgnite, "No se puede prender: ya está encendida.");

            bool allowed = false;
            lit.ConfigureSparks(new Vector2(0.1f, 0.2f), 2.5f, () => allowed);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(seam.CanIgnite, "Sin que el jugador esté dentro, no hay chispas.");

            allowed = true;
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(seam.CanIgnite, "Con el jugador dentro, una chispa prende la veta.");
            Assert.IsTrue(lit.IsEmbers, "La encendida sigue en brasas.");
        }

        [UnityTest]
        public IEnumerator SteamVent_WithAnOffset_WaitsBeforeItsFirstCycle()
        {
            var parent = Track(new GameObject("Vents"));
            var vent = SteamVent.Create(parent.transform, Vector2.zero, Frames(2), Frames(2), Frames(4), 1.5f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(vent.IsActive, "Aún espera su turno (el ciclo empezaría en reposo).");
            Assert.AreEqual(5.5f, SteamVent.CycleSeconds, 0.001f);
        }
    }
}

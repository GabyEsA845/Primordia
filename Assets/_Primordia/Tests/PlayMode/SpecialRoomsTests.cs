using System.Collections;
using System.Collections.Generic;
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

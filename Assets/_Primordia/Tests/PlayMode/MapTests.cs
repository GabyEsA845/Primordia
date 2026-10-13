using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Tests
{
    public class MapTests
    {
        readonly List<Object> created = new();

        // Un color distinto por muestra, para reconocerlas en la textura.
        static readonly Color32[] Palette =
        {
            new(10, 0, 0, 255), new(20, 0, 0, 255), new(30, 0, 0, 255), new(40, 0, 0, 255),
            new(50, 0, 0, 255), new(60, 0, 0, 255), new(70, 0, 0, 255), new(80, 0, 0, 255),
        };

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

        [Test]
        public void Tab_OpensTheMapWhilePlaying_AndPausesTime()
        {
            var manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();

            manager.ToggleMap();
            Assert.AreEqual(GameState.Map, manager.State);
            Assert.AreEqual(0f, Time.timeScale);

            manager.ToggleMap();
            Assert.AreEqual(GameState.Playing, manager.State);

            manager.ToggleMap();
            manager.TogglePause();
            Assert.AreEqual(GameState.Playing, manager.State, "Esc cierra el mapa.");

            manager.SetState(GameState.Paused);
            manager.ToggleMap();
            Assert.AreEqual(GameState.Paused, manager.State, "El mapa no se abre desde la pausa.");
        }

        [Test]
        public void Painter_ShowsVisitedRooms_AndNeighboursAsSilhouettes()
        {
            // Dos salas de 10×10 (suelo de 8×8) unidas por un pasillo de 2 de ancho entre x = 10 y x = 19.
            var map = new FloorMap(new RectInt(0, 0, 30, 10));
            var a = CreateRoom(map, new RectInt(0, 0, 10, 10));
            var b = CreateRoom(map, new RectInt(20, 0, 10, 10));
            var corridor = new MapCorridor { A = a, B = b };
            for (int x = 9; x <= 20; x++)
            {
                for (int y = 4; y <= 5; y++)
                {
                    var cell = new Vector2Int(x, y);
                    bool door = x == 9 || x == 20;
                    map.Set(cell, door ? FloorMap.Kind.DoorGap : FloorMap.Kind.Floor);
                    if (!door)
                        corridor.Cells.Add(cell);
                }
            }
            a.Neighbors.Add(b);
            b.Neighbors.Add(a);

            var painter = new MapPainter(map, new[] { a, b }, new[] { corridor }, Palette);
            Color32 At(int x, int y) => painter.Texture.GetPixel(x, y);
            Color32 Swatch(MapPainter.Swatch swatch) => Palette[(int)swatch];

            painter.Paint(null);
            Assert.AreEqual(0, At(5, 5).a, "Sin visitar nada, no se ve nada.");
            Assert.AreEqual(0, painter.Discovered.width);

            a.Room.Begin();
            painter.Paint(a);

            Assert.AreEqual(Swatch(MapPainter.Swatch.FloorCurrent), At(5, 5));
            Assert.AreEqual(Swatch(MapPainter.Swatch.Wall), At(0, 0), "La esquina toca suelo en diagonal.");
            Assert.AreEqual(Swatch(MapPainter.Swatch.Corridor), At(9, 4), "Hueco de puerta de la sala visitada.");
            Assert.AreEqual(Swatch(MapPainter.Swatch.Unexplored), At(15, 4), "Pasillo hacia una sala sin visitar.");
            Assert.AreEqual(Swatch(MapPainter.Swatch.Unexplored), At(25, 5));
            Assert.AreEqual(Swatch(MapPainter.Swatch.UnexploredWall), At(29, 9));
            Assert.AreEqual(new RectInt(0, 0, 30, 10), painter.Discovered);

            b.Room.Begin();
            painter.Paint(b);

            Assert.AreEqual(Swatch(MapPainter.Swatch.Floor), At(5, 5));
            Assert.AreEqual(Swatch(MapPainter.Swatch.FloorCurrent), At(25, 5));
            Assert.AreEqual(Swatch(MapPainter.Swatch.Corridor), At(15, 4));
            Assert.AreSame(b, painter.RoomAt(new Vector2Int(25, 5)));
            Assert.IsNull(painter.RoomAt(new Vector2Int(15, 4)), "En el pasillo no hay sala.");
        }

        [Test]
        public void Painter_KeepsUnlinkedRoomsHidden()
        {
            var map = new FloorMap(new RectInt(0, 0, 30, 10));
            var a = CreateRoom(map, new RectInt(0, 0, 10, 10));
            var far = CreateRoom(map, new RectInt(20, 0, 10, 10));
            a.Room.Begin();

            var painter = new MapPainter(map, new[] { a, far }, new MapCorridor[0], Palette);
            painter.Paint(a);

            Assert.AreEqual(0, painter.Texture.GetPixel(25, 5).a);
            Assert.AreEqual(new RectInt(0, 0, 10, 10), painter.Discovered);
        }

        [Test]
        public void Markers_GoInsideTheFloor_EvenInAnLShapedRoom()
        {
            // Sala de 20×20 en L: falta el cuarto de arriba a la derecha, justo donde cae el centro del rectángulo.
            var map = new FloorMap(new RectInt(0, 0, 20, 20));
            var room = CreateRoom(map, new RectInt(0, 0, 20, 20));
            for (int y = 9; y < 20; y++)
                for (int x = 9; x < 20; x++)
                    map.Set(new Vector2Int(x, y), FloorMap.Kind.Wall);

            var painter = new MapPainter(map, new[] { room }, new MapCorridor[0], Palette);
            var cell = Vector2Int.FloorToInt(painter.MarkerPosition(room));

            Assert.AreEqual(FloorMap.Kind.Wall, map.Get(Vector2Int.FloorToInt(room.Rect.center)), "El centro del rectángulo está fuera del suelo.");
            // Bien dentro de un brazo: rodeada de suelo al menos 3 casillas en todas las direcciones.
            for (int dy = -3; dy <= 3; dy++)
                for (int dx = -3; dx <= 3; dx++)
                    Assert.AreEqual(FloorMap.Kind.Floor, map.Get(cell + new Vector2Int(dx, dy)), $"Casilla {cell} demasiado cerca de una pared.");
        }

        MapRoom CreateRoom(FloorMap map, RectInt rect)
        {
            for (int y = rect.yMin + 1; y < rect.yMax - 1; y++)
                for (int x = rect.xMin + 1; x < rect.xMax - 1; x++)
                    map.Set(new Vector2Int(x, y), FloorMap.Kind.Floor);

            var roomObject = Track(new GameObject("Room"));
            roomObject.AddComponent<BoxCollider2D>();
            return new MapRoom { Room = roomObject.AddComponent<Room>(), Rect = rect, Role = RoomRole.Combat };
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }
    }
}

using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Pinta el plano del piso en una textura, 1 píxel por celda, con niebla: las salas visitadas se ven enteras
    /// (contorno, suelo y huecos de puerta) y sus vecinas sin visitar como silueta apagada; el resto, transparente.
    /// La sala del jugador y las que aún tienen enemigos llevan su propio color, y los pasillos se ven en cuanto
    /// se ha visitado una de sus dos salas. El píxel (0, 0) es la esquina inferior izquierda del mapa.
    /// </summary>
    public class MapPainter
    {
        /// <summary>Colores de la paleta, en el orden de la tira <c>UI_Map_Palette</c>.</summary>
        public enum Swatch
        {
            Background,
            Wall,
            Floor,
            FloorCurrent,
            FloorUncleared,
            Corridor,
            Unexplored,
            UnexploredWall
        }

        static readonly Color32 Clear = new(0, 0, 0, 0);

        readonly FloorMap map;
        readonly IReadOnlyList<MapRoom> rooms;
        readonly IReadOnlyList<MapCorridor> corridors;
        readonly Color32[] palette;
        readonly Color32[] pixels;
        readonly Dictionary<MapRoom, Vector2> markerPositions = new();

        public Texture2D Texture { get; }

        /// <summary>Lo descubierto (salas visitadas y vecinas), en píxeles de la textura. Vacío si no hay nada.</summary>
        public RectInt Discovered { get; private set; }

        public MapPainter(FloorMap map, IReadOnlyList<MapRoom> rooms, IReadOnlyList<MapCorridor> corridors, Color32[] palette)
        {
            this.map = map;
            this.rooms = rooms;
            this.corridors = corridors;
            this.palette = palette;

            var size = map.Bounds.size;
            pixels = new Color32[size.x * size.y];
            Texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                name = "FloorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        public static bool IsVisited(MapRoom room) => room.Room != null && room.Room.State != Room.RoomState.Waiting;

        public static bool IsVisible(MapRoom room)
        {
            if (IsVisited(room))
                return true;
            foreach (var neighbor in room.Neighbors)
            {
                if (IsVisited(neighbor))
                    return true;
            }
            return false;
        }

        /// <summary>Color de la paleta (magenta si falta, para que se note).</summary>
        public Color32 ColorOf(Swatch swatch) =>
            palette != null && (int)swatch < palette.Length ? palette[(int)swatch] : new Color32(255, 0, 255, 255);

        /// <summary>Posición de un punto del mundo (en celdas, relativo al piso) en píxeles de la textura.</summary>
        public Vector2 ToPixel(Vector2 floorPosition) => floorPosition - map.Bounds.min;

        /// <summary>
        /// Dónde va la marca de una sala (jefe, recompensa), en celdas del mapa: el centro de la casilla de suelo más
        /// alejada de las paredes y, entre las que empatan, la más cercana al centro del suelo. En una sala en L
        /// queda en mitad de su brazo más ancho, no en la esquina vacía del rectángulo.
        /// </summary>
        public Vector2 MarkerPosition(MapRoom room)
        {
            if (markerPositions.TryGetValue(room, out var cached))
                return cached;

            var rect = room.Rect;
            var distance = new int[rect.width, rect.height];
            var queue = new Queue<Vector2Int>();
            Vector2 floorSum = Vector2.zero;
            int floorCount = 0;

            // Distancia (en pasos de 8 direcciones) desde cada casilla de suelo a la pared más cercana.
            foreach (var cell in rect.allPositionsWithin)
            {
                var local = cell - rect.min;
                if (map.Get(cell) == FloorMap.Kind.Floor)
                {
                    distance[local.x, local.y] = int.MaxValue;
                    floorSum += cell;
                    floorCount++;
                }
                else
                {
                    queue.Enqueue(local);
                }
            }

            while (queue.Count > 0)
            {
                var local = queue.Dequeue();
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var next = local + new Vector2Int(dx, dy);
                        if (next.x < 0 || next.y < 0 || next.x >= rect.width || next.y >= rect.height)
                            continue;
                        if (distance[next.x, next.y] <= distance[local.x, local.y] + 1)
                            continue;
                        distance[next.x, next.y] = distance[local.x, local.y] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            Vector2 position = rect.center;
            if (floorCount > 0)
            {
                var centroid = floorSum / floorCount;
                int best = -1;
                float bestOffset = float.MaxValue;
                foreach (var cell in rect.allPositionsWithin)
                {
                    var local = cell - rect.min;
                    int d = distance[local.x, local.y];
                    if (map.Get(cell) != FloorMap.Kind.Floor || d < best)
                        continue;

                    float offset = (cell - centroid).sqrMagnitude;
                    if (d > best || offset < bestOffset)
                    {
                        best = d;
                        bestOffset = offset;
                        position = cell + new Vector2(0.5f, 0.5f);
                    }
                }
            }

            markerPositions[room] = position;
            return position;
        }

        /// <summary>La sala que contiene esa celda del mapa, o null (pasillos, fuera del piso).</summary>
        public MapRoom RoomAt(Vector2Int cell)
        {
            foreach (var room in rooms)
            {
                if (room.Rect.Contains(cell))
                    return room;
            }
            return null;
        }

        /// <summary>Vuelve a pintar todo el plano; <paramref name="current"/> es la sala donde está el jugador.</summary>
        public void Paint(MapRoom current)
        {
            System.Array.Fill(pixels, Clear);
            RectInt? discovered = null;

            foreach (var room in rooms)
            {
                if (!IsVisible(room))
                    continue;

                PaintRoom(room, room == current);
                discovered = discovered is RectInt sum ? Union(sum, room.Rect) : room.Rect;
            }

            foreach (var corridor in corridors)
            {
                bool visitedA = IsVisited(corridor.A), visitedB = IsVisited(corridor.B);
                if (!visitedA && !visitedB)
                    continue;

                var color = ColorOf(visitedA && visitedB ? Swatch.Corridor : Swatch.Unexplored);
                foreach (var cell in corridor.Cells)
                    Set(cell, color);
            }

            Discovered = discovered is RectInt rect
                ? new RectInt(rect.position - map.Bounds.min, rect.size)
                : new RectInt(0, 0, 0, 0);

            Texture.SetPixels32(pixels);
            Texture.Apply(false);
        }

        void PaintRoom(MapRoom room, bool isCurrent)
        {
            bool visited = IsVisited(room);
            var floor = !visited ? Swatch.Unexplored
                : isCurrent ? Swatch.FloorCurrent
                : room.Room.State == Room.RoomState.Combat ? Swatch.FloorUncleared
                : Swatch.Floor;

            foreach (var cell in room.Rect.allPositionsWithin)
            {
                var kind = map.Get(cell);
                if (kind == FloorMap.Kind.Floor)
                    Set(cell, ColorOf(floor));
                else if (kind == FloorMap.Kind.Obstacle)
                    // Los obstáculos de cobertura, como las columnas.
                    Set(cell, ColorOf(visited ? Swatch.Wall : Swatch.UnexploredWall));
                else if (kind != FloorMap.Kind.Wall)
                    // Huecos y arcos de puerta.
                    Set(cell, ColorOf(visited ? Swatch.Corridor : Swatch.Unexplored));
                else if (TouchesOpen(cell))
                    // Solo el contorno: las paredes que tocan suelo, también en diagonal (y las columnas).
                    Set(cell, ColorOf(visited ? Swatch.Wall : Swatch.UnexploredWall));
            }
        }

        bool TouchesOpen(Vector2Int cell)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx != 0 || dy != 0) && map.Get(cell.x + dx, cell.y + dy) != FloorMap.Kind.Wall)
                        return true;
                }
            }
            return false;
        }

        void Set(Vector2Int cell, Color32 color)
        {
            var pixel = cell - map.Bounds.min;
            if (pixel.x >= 0 && pixel.y >= 0 && pixel.x < Texture.width && pixel.y < Texture.height)
                pixels[pixel.y * Texture.width + pixel.x] = color;
        }

        static RectInt Union(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin), yMin = Mathf.Min(a.yMin, b.yMin);
            return new RectInt(xMin, yMin, Mathf.Max(a.xMax, b.xMax) - xMin, Mathf.Max(a.yMax, b.yMax) - yMin);
        }
    }
}

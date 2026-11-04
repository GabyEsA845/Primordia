using System.Collections.Generic;
using System.Linq;
using Primordia.Biomass;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Shop;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Rooms
{
    /// <summary>
    /// Monta un <see cref="FloorLayout"/> en la escena: coloca las plantillas en la rejilla, las une con pasillos,
    /// pinta todo el piso en Tilemaps y crea salas, puertas, enemigos, recompensas, tienda, jefe y salida.
    /// Funciona igual en el juego (piso aleatorio) que en el editor.
    /// </summary>
    public static class FloorBuilder
    {
        /// <summary>Pasillo más corto entre dos salas vecinas; si una sala es más pequeña que su celda, crece.</summary>
        public const int CorridorLength = 4;

        /// <summary>
        /// Pasillo del jefe a la sala de paso: largo, para que dé tiempo a oír cómo se apaga la música del piso y sube
        /// el ambiente del siguiente.
        /// </summary>
        public const int TransitionCorridorLength = 14;

        // Relleno de pared alrededor: media vista (15×8,5) más el adelanto de la cámara (3), con holgura.
        const int Margin = 20;
        // Sin decoración tan cerca de una puerta, para que no tape el paso.
        const int DoorClearance = 2;

        // Distancia del centro del mostrador a la pared izquierda cuando se aparta de una puerta de arriba.
        const float ShopCounterMargin = 3.5f;

        const int SoupTheme = 0;
        const int BossTheme = 1;
        // Sala de paso: zona intermedia y zona del piso siguiente (la de la fosa).
        const int MidTheme = 2;
        const int NextTheme = 3;
        // Franjas de profundidad (piso 2): sus tilesets van a continuación de los anteriores.
        const int FirstBandTheme = 4;

        // Tramos de la sala de paso, en fracción de la distancia de su puerta a la fosa: hasta aquí, tema del piso actual;
        // luego el intermedio, y desde el segundo, el del piso siguiente. Un poco de ruido para que el corte no sea recto.
        const float MidThemeFrom = 0.4f;
        const float NextThemeFrom = 0.75f;
        const float ThemeNoise = 0.12f;
        // Relleno de pared alrededor de la sala de paso que se pinta con sus temas (lo que se ve desde dentro).
        const int TransitionThemePadding = 16;
        // Sin decoración bajo la fosa ni pegada a su borde.
        const float PitClearance = 1.5f;

        /// <summary>Resultado de montar el piso.</summary>
        public class BuiltFloor
        {
            public readonly List<Room> Rooms = new();
            public Vector2 StartPosition;
            public int MissingCells;

            /// <summary>Qué hay en cada celda del piso: sirve para la navegación de los enemigos y para el mapa.</summary>
            public FloorMap Map;

            /// <summary>Salas y pasillos para el mapa, en el mismo orden que <see cref="Rooms"/>.</summary>
            public readonly List<MapRoom> MapRooms = new();
            public readonly List<MapCorridor> Corridors = new();
        }

        /// <summary>Una sala del plano ya colocada en el mapa.</summary>
        class PlacedRoom
        {
            public LayoutRoom Layout;
            public Vector2Int Origin;
            public readonly Dictionary<Vector2Int, List<Vector2Int>> DoorCells = new();
            public readonly Dictionary<Vector2Int, List<Vector2Int>> ArchCells = new();

            /// <summary>Puertas selladas con maleza quemable (atajos): la dirección de cada una.</summary>
            public readonly HashSet<Vector2Int> BrushDirections = new();

            public RoomTemplate Template => Layout.Template;

            /// <summary>Índice de su tileset: el del piso, el del jefe o el de su franja de profundidad.</summary>
            public int Theme;
            public RectInt Rect => new(Origin, new Vector2Int(Template.Width, Template.Height));

            public Vector2 FloorCenter
            {
                get
                {
                    var floor = Template.FloorBounds();
                    return new Vector2(Origin.x + floor.x + floor.width / 2f, Origin.y + floor.y + floor.height / 2f);
                }
            }
        }

        public static BuiltFloor Build(FloorLayout layout, FloorConfig config, Transform parent, System.Random random)
        {
            var built = new BuiltFloor();
            var boss = layout.BossChoice.IsValid ? layout.BossChoice : config.BossOptions()[0];
            var bossTheme = boss.theme != null ? boss.theme : config.BossTheme != null ? config.BossTheme : config.Theme;
            var themes = new[]
            {
                config.Theme, bossTheme,
                config.TransitionMidTheme != null ? config.TransitionMidTheme : config.Theme,
                config.NextFloorTheme != null ? config.NextFloorTheme : config.Theme
            }.Concat(config.BandThemes.Select(band => band != null ? band : config.Theme)).ToArray();
            var placed = Place(layout, config, random);
            AssignThemes(placed, config);
            var byLayout = placed.ToDictionary(room => room.Layout);

            var map = new FloorMap(MapBounds(placed));
            built.Map = map;
            var noDecoration = new HashSet<Vector2Int>();

            foreach (var room in placed)
                Stamp(map, room);
            var corridorCells = new List<(LayoutRoom from, LayoutRoom to, List<Vector2Int> cells)>();
            var widened = new HashSet<Vector2Int>();
            var roomRects = placed.Select(room => room.Rect).ToList();
            for (int link = 0; link < layout.Links.Count; link++)
            {
                var (from, to) = layout.Links[link];
                var direction = layout.Direction(link);
                if (layout.IsShortcut(link))
                    byLayout[from].BrushDirections.Add(direction);
                OpenDoor(map, byLayout[from], direction);
                OpenDoor(map, byLayout[to], -direction);
                var (fromTheme, toTheme) = CorridorThemes(byLayout[from], byLayout[to], config);
                var cells = DigCorridor(map, byLayout[from], byLayout[to], direction, noDecoration, fromTheme, toTheme);
                if (config.CorridorWidth.y > 2)
                    Widen(map, cells, direction, config.CorridorWidth, roomRects, widened, noDecoration, random);
                corridorCells.Add((from, to, cells));
            }
            if (widened.Count > 0)
                RepairCorridors(map, widened, corridorCells.Select(corridor => corridor.cells).ToList(), noDecoration);

            if (config.HasBands)
                ThemeFiller(map, placed);
            if (config.HasBands && config.BlendBandBorders)
                BlendBandBorders(map, placed, layout, byLayout, random);

            foreach (var room in placed.Where(room => room.Layout.Role == RoomRole.Transition))
            {
                ThemeTransition(map, room, placed, config);
                foreach (var cell in PitCells(room, config))
                    noDecoration.Add(cell);
            }

            foreach (var room in placed)
            {
                foreach (var cell in room.DoorCells.Values.Concat(room.ArchCells.Values).SelectMany(cells => cells))
                {
                    for (int dx = -DoorClearance; dx <= DoorClearance; dx++)
                        for (int dy = -DoorClearance - 2; dy <= DoorClearance; dy++)
                            noDecoration.Add(cell + new Vector2Int(dx, dy));
                }
            }

            var decorations = new List<(Vector2Int cell, TileBase tile)>();
            built.MissingCells = Paint(parent, map, themes, noDecoration, random.Next(), decorations);

            // Luces de la bioluminiscencia: de la decoración que brilla y, más abajo, de los obstáculos.
            var glows = new List<(Vector2 position, Glow glow)>();
            foreach (var (cell, tile) in decorations)
            {
                var glow = themes[Mathf.Clamp(map.GetTheme(cell), 0, themes.Length - 1)].GlowOf(tile);
                if (glow.HasValue)
                    glows.Add((cell + Vector2.one * 0.5f, glow.Value));
            }

            var mapRooms = new Dictionary<LayoutRoom, MapRoom>();
            var brushes = new Dictionary<(LayoutRoom room, Vector2Int direction), BrushWall>();
            foreach (var room in placed)
            {
                var builtRoom = BuildRoom(parent, room, themes[room.Theme], config, boss, random, glows, brushes, map);
                builtRoom.SetDepthBand(room.Theme >= FirstBandTheme ? room.Theme - FirstBandTheme : -1);
                built.Rooms.Add(builtRoom);
                var mapRoom = new MapRoom
                {
                    Room = builtRoom,
                    Rect = room.Rect,
                    Role = room.Layout.Role,
                    HasReward = builtRoom.TryGetComponent<MutationReward>(out _),
                };
                built.MapRooms.Add(mapRoom);
                mapRooms[room.Layout] = mapRoom;

                if (room.Layout.Role == RoomRole.Start)
                    built.StartPosition = room.FloorCenter;
                // Con sala de paso, el piso se deja por su fosa; si no, por la salida de siempre en la sala del jefe.
                if (room.Layout.Role == RoomRole.Boss && !config.HasTransition)
                    BuildExit(builtRoom, config);
            }

            if (glows.Count > 0)
            {
                var lights = new GameObject("Lights").transform;
                lights.SetParent(parent, false);
                Ambience.CreateGlobalLight(lights);
                foreach (var (position, glow) in glows)
                    Ambience.CreateGlow(lights, position, glow, (float)random.NextDouble() * Mathf.Max(0.1f, glow.period));
            }

            for (int link = 0; link < corridorCells.Count; link++)
            {
                var (from, to, cells) = corridorCells[link];
                var corridor = new MapCorridor { A = mapRooms[from], B = mapRooms[to] };
                corridor.Cells.AddRange(cells);
                built.Corridors.Add(corridor);
                // Un atajo no revela la sala vecina: hasta que se descubre la maleza, el mapa no lo cuenta.
                if (layout.IsShortcut(link))
                {
                    brushes.TryGetValue((from, layout.Direction(link)), out var brush);
                    corridor.Brush = brush;
                    continue;
                }
                corridor.A.Neighbors.Add(corridor.B);
                corridor.B.Neighbors.Add(corridor.A);
            }

            return built;
        }

        /// <summary>
        /// Tileset de cada sala: el del jefe en la suya; con franjas de profundidad, la de su tercio del camino (las
        /// laterales y la tienda, la de la sala de la que cuelgan; el inicio, la primera); si no, el del piso.
        /// </summary>
        static void AssignThemes(List<PlacedRoom> placed, FloorConfig config)
        {
            int bands = config.HasBands ? config.BandThemes.Length : 0;
            int deepest = placed.Where(room => room.Layout.Role == RoomRole.Combat).Select(room => room.Layout.Depth).DefaultIfEmpty(1).Max();
            foreach (var room in placed)
            {
                if (room.Layout.Role == RoomRole.Boss)
                    room.Theme = BossTheme;
                else if (bands == 0)
                    room.Theme = SoupTheme;
                else
                    room.Theme = FirstBandTheme + BandOf(room.Layout.Depth, deepest, bands);
            }
        }

        /// <summary>
        /// Tileset de un pasillo, en su salida y en su llegada: sin franjas, el del piso (también el que entra a la sala del
        /// jefe); con franjas, pasa a lo largo del túnel del de la sala de la que sale al de la sala a la que llega, salvo la
        /// del jefe (el pasillo sigue con el de la sala de la que sale).
        /// </summary>
        static (int from, int to) CorridorThemes(PlacedRoom from, PlacedRoom to, FloorConfig config)
        {
            if (!config.HasBands)
                return (SoupTheme, SoupTheme);
            if (to.Theme == BossTheme)
                return (from.Theme, from.Theme);
            return (from.Theme == BossTheme ? to.Theme : from.Theme, to.Theme);
        }

        /// <summary>
        /// Con franjas, el relleno de pared que no es de ninguna sala lleva el tileset de la sala más cercana: así desde dentro
        /// de cada sala (y a lo largo de los pasillos) no se ve pared de otra franja.
        /// </summary>
        static void ThemeFiller(FloorMap map, List<PlacedRoom> placed)
        {
            foreach (var cell in map.Bounds.allPositionsWithin)
            {
                if (map.Get(cell) != FloorMap.Kind.Wall || placed.Any(room => room.Rect.Contains(cell)))
                    continue;

                PlacedRoom nearest = null;
                float best = float.MaxValue;
                foreach (var room in placed)
                {
                    float distance = Distance(room.Rect, cell);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = room;
                    }
                }
                if (nearest != null)
                    map.SetTheme(cell, nearest.Theme);
            }
        }

        /// <summary>
        /// Salas de frontera (piso 3, "el incendio que avanza"): la primera sala de combate de cada franja nueva mezcla en su
        /// suelo el tileset de la franja anterior y el suyo con una mancha irregular de ruido, todo lo anterior junto a la
        /// puerta por la que se entra y todo lo nuevo hacia el lado contrario. Las paredes siguen siendo las de la sala.
        /// </summary>
        static void BlendBandBorders(FloorMap map, List<PlacedRoom> placed, FloorLayout layout, Dictionary<LayoutRoom, PlacedRoom> byLayout,
            System.Random random)
        {
            foreach (var room in placed)
            {
                if (room.Layout.Role != RoomRole.Combat || room.Theme <= FirstBandTheme)
                    continue;

                // El enlace por el que se llega desde la franja anterior (la sala anterior del camino).
                int previousTheme = room.Theme - 1;
                int link = layout.Links.FindIndex(pair => pair.to == room.Layout && byLayout[pair.from].Theme == previousTheme);
                if (link < 0)
                    continue;

                var entranceCells = room.DoorCells.TryGetValue(-layout.Direction(link), out var cells) ? cells : null;
                if (entranceCells == null || entranceCells.Count == 0)
                    continue;
                var entrance = new Vector2((float)entranceCells.Average(cell => cell.x), (float)entranceCells.Average(cell => cell.y));
                var inward = room.FloorCenter - entrance;
                if (inward.sqrMagnitude < 1f)
                    continue;

                float offset = random.Next(1000);
                foreach (var cell in room.Rect.allPositionsWithin)
                {
                    if (map.Get(cell) is FloorMap.Kind.Wall or FloorMap.Kind.DoorArch or FloorMap.Kind.DoorGap or FloorMap.Kind.Passage)
                        continue;

                    // 0 en la puerta de entrada, 1 en el extremo contrario (el centro vale 0,5).
                    float along = Mathf.Clamp01(Vector2.Dot((Vector2)cell - entrance, inward) / inward.sqrMagnitude / 2f);
                    float noise = Mathf.PerlinNoise(cell.x * 0.12f + offset, cell.y * 0.12f + offset);
                    if (along + (noise - 0.5f) * 0.8f <= 0.4f)
                        map.SetTheme(cell, previousTheme);
                }
            }
        }

        /// <summary>Franja de la sala de combate número <paramref name="depth"/> (1 = la primera) de <paramref name="deepest"/>.</summary>
        internal static int BandOf(int depth, int deepest, int bands)
        {
            if (depth <= 1 || deepest <= 1)
                return 0;
            return Mathf.Clamp((depth - 1) * bands / deepest, 0, bands - 1);
        }

        /// <summary>
        /// Coloca cada sala en su celda alineando su anclaje (el centro de sus puertas) con el de la celda: así
        /// las puertas de salas vecinas quedan enfrentadas aunque tengan tamaños distintos, y el pasillo es recto.
        /// Cada columna mide lo que su sala más ancha y cada fila lo que su sala más alta, más el pasillo mínimo.
        /// </summary>
        static List<PlacedRoom> Place(FloorLayout layout, FloorConfig config, System.Random random)
        {
            // Pasillo mínimo de cada hueco entre columnas (o filas): fijo en el piso 1, al azar en su tramo en el piso 2.
            var length = config.CorridorLength;
            int minLength = Mathf.Max(1, Mathf.Min(length.x, length.y)), maxLength = Mathf.Max(minLength, Mathf.Max(length.x, length.y));
            System.Func<int, int> gap = minLength == maxLength ? _ => minLength : _ => random.Next(minLength, maxLength + 1);

            // El pasillo de la sala de paso sale más largo: se ensancha el hueco entre la columna (o fila) del jefe y la suya.
            int? longGapX = null, longGapY = null;
            if (layout.Transition != null)
            {
                var from = layout.Boss.Cell;
                var to = layout.Transition.Cell;
                if (from.x != to.x)
                    longGapX = Mathf.Min(from.x, to.x);
                else
                    longGapY = Mathf.Min(from.y, to.y);
            }

            var (anchorX, _) = Anchors(layout.Rooms, room => room.Cell.x, room => room.Template.Anchor.x, room => room.Template.Width,
                _ => 1, longGapX, gap);
            var (anchorY, bottomY) = Anchors(layout.Rooms, room => room.Cell.y, room => room.Template.Anchor.y, room => room.Template.Height,
                room => room.Tall ? 2 : 1, longGapY, gap);

            return layout.Rooms.Select(room => new PlacedRoom
            {
                Layout = room,
                // Las de dos casillas (Parkour) se apoyan en lo más bajo de su fila de abajo; solo tienen puertas arriba y abajo.
                Origin = room.Tall
                    ? new Vector2Int(anchorX[room.Cell.x] - room.Template.Anchor.x, bottomY[room.Cell.y])
                    : new Vector2Int(anchorX[room.Cell.x], anchorY[room.Cell.y]) - room.Template.Anchor
            }).ToList();
        }

        /// <summary>
        /// Coordenada del anclaje de cada columna (o fila) y la de su borde de abajo (o izquierdo). Cada una mide lo que su sala
        /// más grande de una casilla, más el pasillo mínimo. Las salas de dos casillas (<paramref name="span"/> 2) no cuentan
        /// en ninguna de sus dos filas, pero si no caben entre el borde de abajo de la primera y el de arriba de la segunda,
        /// la segunda se aparta lo que falte.
        /// </summary>
        /// <param name="longGap">Índice de la columna o fila tras la que el hueco es el del pasillo largo de la sala de paso.</param>
        /// <param name="gap">Pasillo mínimo tras la columna o fila de ese índice.</param>
        static (Dictionary<int, int> anchors, Dictionary<int, int> lower) Anchors(List<LayoutRoom> rooms, System.Func<LayoutRoom, int> index,
            System.Func<LayoutRoom, int> anchor, System.Func<LayoutRoom, int> size, System.Func<LayoutRoom, int> span, int? longGap,
            System.Func<int, int> gap)
        {
            var result = new Dictionary<int, int>();
            var lower = new Dictionary<int, int>();
            var keys = rooms.SelectMany(room => Enumerable.Range(index(room), span(room))).Distinct().OrderBy(key => key);
            int next = 0;
            foreach (int key in keys)
            {
                var group = rooms.Where(room => span(room) == 1 && index(room) == key).ToList();
                int before = group.Count > 0 ? group.Max(anchor) : 0;
                int after = group.Count > 0 ? group.Max(room => size(room) - anchor(room)) : 0;
                int at = next + before;

                foreach (var tall in rooms.Where(room => span(room) == 2 && index(room) + 1 == key))
                {
                    int have = at + after - lower[key - 1];
                    if (have < size(tall))
                        at += size(tall) - have;
                }

                result[key] = at;
                lower[key] = at - before;
                next = at + after + (key == longGap ? TransitionCorridorLength : gap(key));
            }
            return (result, lower);
        }

        /// <summary>
        /// Temas de la sala de paso por celda: el del piso actual junto a su puerta, el intermedio a medio camino y el del
        /// piso siguiente alrededor de la fosa y más allá. También el relleno de pared de alrededor que queda más cerca de
        /// esta sala que de las demás, para que desde dentro no se vea el corte.
        /// </summary>
        static void ThemeTransition(FloorMap map, PlacedRoom room, List<PlacedRoom> placed, FloorConfig config)
        {
            // Se entra con el tileset del piso; con franjas (piso 2), con el de la última, la más honda (el Abismo).
            int entryTheme = config.HasBands ? FirstBandTheme + config.BandThemes.Length - 1 : SoupTheme;
            var entry = room.DoorCells.Values.FirstOrDefault();
            if (entry == null || entry.Count == 0)
                return;

            var door = new Vector2((float)entry.Average(cell => cell.x), (float)entry.Average(cell => cell.y)) + Vector2.one * 0.5f;
            float span = Mathf.Max(1f, Vector2.Distance(door, room.FloorCenter));
            var others = placed.Where(other => other != room).Select(other => other.Rect).ToList();
            var area = room.Rect;
            area.SetMinMax(area.min - Vector2Int.one * TransitionThemePadding, area.max + Vector2Int.one * TransitionThemePadding);

            foreach (var cell in area.allPositionsWithin)
            {
                if (!room.Rect.Contains(cell))
                {
                    // Fuera de la plantilla, solo el relleno de pared que no está más cerca de otra sala.
                    if (map.Get(cell) != FloorMap.Kind.Wall)
                        continue;
                    float own = Distance(room.Rect, cell);
                    if (others.Any(other => Distance(other, cell) <= own))
                        continue;
                }

                float t = Vector2.Distance(cell + Vector2.one * 0.5f, door) / span
                    + ((CellNoise(cell) & 1023) / 1023f * 2f - 1f) * ThemeNoise;
                map.SetTheme(cell, t < MidThemeFrom ? entryTheme : t < NextThemeFrom ? MidTheme : NextTheme);
            }
        }

        static float Distance(RectInt rect, Vector2Int cell)
        {
            int dx = Mathf.Max(rect.xMin - cell.x, 0, cell.x - (rect.xMax - 1));
            int dy = Mathf.Max(rect.yMin - cell.y, 0, cell.y - (rect.yMax - 1));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static int CellNoise(Vector2Int cell)
        {
            unchecked
            {
                int hash = cell.x * 92837111 ^ cell.y * 689287499;
                hash ^= hash >> 15;
                return (hash * 1103515245) & int.MaxValue;
            }
        }

        /// <summary>Celdas bajo la fosa de la sala de paso y junto a su borde.</summary>
        static IEnumerable<Vector2Int> PitCells(PlacedRoom room, FloorConfig config)
        {
            if (config.TransitionPit == null)
                yield break;

            var radii = config.TransitionPit.EdgeRadii + Vector2.one * PitClearance;
            var center = room.FloorCenter;
            for (int y = Mathf.FloorToInt(center.y - radii.y); y <= Mathf.CeilToInt(center.y + radii.y); y++)
            {
                for (int x = Mathf.FloorToInt(center.x - radii.x); x <= Mathf.CeilToInt(center.x + radii.x); x++)
                {
                    float nx = (x + 0.5f - center.x) / radii.x, ny = (y + 0.5f - center.y) / radii.y;
                    if (nx * nx + ny * ny <= 1f)
                        yield return new Vector2Int(x, y);
                }
            }
        }

        static RectInt MapBounds(List<PlacedRoom> rooms)
        {
            int minX = rooms.Min(r => r.Rect.xMin) - Margin, minY = rooms.Min(r => r.Rect.yMin) - Margin;
            int maxX = rooms.Max(r => r.Rect.xMax) + Margin, maxY = rooms.Max(r => r.Rect.yMax) + Margin;
            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Copia la plantilla en el mapa. Los huecos de puerta quedan como pared hasta que se abren.</summary>
        static void Stamp(FloorMap map, PlacedRoom room)
        {
            for (int y = 0; y < room.Template.Height; y++)
            {
                for (int x = 0; x < room.Template.Width; x++)
                {
                    var cell = room.Origin + new Vector2Int(x, y);
                    var mark = room.Template.Get(x, y);
                    map.Set(cell, RoomTemplate.IsObstacle(mark) ? FloorMap.Kind.Obstacle
                        : mark == RoomTemplate.Mark.Pit ? FloorMap.Kind.Pit
                        : mark == RoomTemplate.Mark.Mud ? FloorMap.Kind.Mud
                        : RoomTemplate.IsInterior(mark) ? FloorMap.Kind.Floor
                        : FloorMap.Kind.Wall);
                    map.SetTheme(cell, room.Theme);
                }
            }
        }

        /// <summary>
        /// Abre el hueco de puerta en esa dirección. Arriba: la fila de la tapa pasa a ser paso y la cara
        /// que hay debajo, arco. Abajo y a los lados: hueco de suelo.
        /// </summary>
        static void OpenDoor(FloorMap map, PlacedRoom room, Vector2Int direction)
        {
            var cells = room.Template.DoorCells(direction).Select(cell => room.Origin + cell).ToList();
            if (cells.Count == 0)
            {
                Debug.LogError($"FloorBuilder: la plantilla {room.Template.name} de {room.Layout.Name} no tiene puerta hacia {direction}.");
                return;
            }

            room.DoorCells[direction] = cells;
            if (direction != Vector2Int.up)
            {
                foreach (var cell in cells)
                    map.Set(cell, FloorMap.Kind.DoorGap);
                return;
            }

            var arch = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                map.Set(cell, FloorMap.Kind.Passage);
                for (var below = cell + Vector2Int.down; map.Get(below) == FloorMap.Kind.Wall; below += Vector2Int.down)
                {
                    map.Set(below, FloorMap.Kind.DoorArch);
                    arch.Add(below);
                }
            }
            room.ArchCells[direction] = arch;
        }

        /// <summary>
        /// Abre el pasillo recto de 2 tiles de ancho entre las puertas de dos salas vecinas y devuelve sus celdas. Lleva el
        /// tileset <paramref name="fromTheme"/> en la primera mitad y <paramref name="toTheme"/> en la segunda, con un corte
        /// irregular (iguales salvo entre dos franjas).
        /// </summary>
        static List<Vector2Int> DigCorridor(FloorMap map, PlacedRoom from, PlacedRoom to, Vector2Int direction, HashSet<Vector2Int> corridorCells,
            int fromTheme, int toTheme)
        {
            var dug = new List<Vector2Int>();
            if (!from.DoorCells.TryGetValue(direction, out var start) || !to.DoorCells.TryGetValue(-direction, out var end))
                return dug;

            foreach (var cell in start)
            {
                // Avanza desde el hueco de la primera sala hasta el de la segunda.
                for (var step = cell + direction; !end.Contains(step) && map.Get(step) == FloorMap.Kind.Wall; step += direction)
                {
                    map.Set(step, FloorMap.Kind.Floor);
                    corridorCells.Add(step);
                    dug.Add(step);
                }
            }

            int length = Mathf.Max(1, dug.Count / Mathf.Max(1, start.Count));
            var origin = start[0];
            foreach (var cell in dug)
                map.SetTheme(cell, CorridorThemeAt(cell, origin, direction, length, fromTheme, toTheme));
            return dug;
        }

        /// <summary>Tileset de una celda del pasillo según lo que ha avanzado desde la primera puerta.</summary>
        static int CorridorThemeAt(Vector2Int cell, Vector2Int origin, Vector2Int direction, int length, int fromTheme, int toTheme)
        {
            if (fromTheme == toTheme)
                return fromTheme;
            var offset = cell - origin;
            int along = offset.x * direction.x + offset.y * direction.y;
            float t = (along - 0.5f) / length + ((CellNoise(cell) & 1023) / 1023f * 2f - 1f) * 0.2f;
            return t < 0.5f ? fromTheme : toTheme;
        }

        /// <summary>
        /// Ensancha el pasillo recto a un túnel de cueva de <paramref name="width"/> tiles (mínimo y máximo): a cada paso el
        /// ancho y el lado hacia el que crece cambian como mucho en 1, y junto a las puertas se queda en 2. Solo cava relleno
        /// de pared fuera de las salas; lo que el arte no pueda dibujar lo deshace <see cref="RepairCorridors"/>.
        /// </summary>
        static void Widen(FloorMap map, List<Vector2Int> dug, Vector2Int direction, Vector2Int width, List<RectInt> roomRects,
            HashSet<Vector2Int> widened, HashSet<Vector2Int> corridorCells, System.Random random)
        {
            if (dug.Count == 0)
                return;

            var across = direction.x != 0 ? Vector2Int.up : Vector2Int.right;
            int Along(Vector2Int cell) => cell.x * direction.x + cell.y * direction.y;
            int Across(Vector2Int cell) => cell.x * across.x + cell.y * across.y;

            int lowLane = dug.Min(Across), highLane = dug.Max(Across);
            var steps = dug.GroupBy(Along).OrderBy(group => group.Key).ToList();
            if (steps.Count < 3)
                return;

            int minWidth = Mathf.Max(2, Mathf.Min(width.x, width.y)), maxWidth = Mathf.Max(minWidth, Mathf.Max(width.x, width.y));
            int current = random.Next(minWidth, maxWidth + 1);
            int low = random.Next(0, current - 1);
            var theme = map.GetTheme(dug[0]);

            // Sin el primer ni el último paso: el túnel se estrecha a 2 junto a cada puerta.
            for (int i = 1; i < steps.Count - 1; i++)
            {
                if (random.NextDouble() < 0.45)
                    current = Mathf.Clamp(current + (random.Next(2) == 0 ? -1 : 1), minWidth, maxWidth);
                if (random.NextDouble() < 0.45)
                    low += random.Next(2) == 0 ? -1 : 1;
                low = Mathf.Clamp(low, 0, current - 2);
                int high = current - 2 - low;

                foreach (var cell in steps[i])
                {
                    theme = map.GetTheme(cell);
                    if (Across(cell) == lowLane)
                        for (int k = 1; k <= low; k++)
                            Dig(cell - across * k);
                    if (Across(cell) == highLane)
                        for (int k = 1; k <= high; k++)
                            Dig(cell + across * k);
                }
            }

            void Dig(Vector2Int cell)
            {
                if (map.Get(cell) != FloorMap.Kind.Wall || roomRects.Any(rect => rect.Contains(cell)))
                    return;
                map.Set(cell, FloorMap.Kind.Floor);
                map.SetTheme(cell, theme);
                widened.Add(cell);
                corridorCells.Add(cell);
                dug.Add(cell);
            }
        }

        // Celdas abiertas que deciden si una pared se puede dibujar (WallRules.Classify): encima, a los lados y las dos de debajo.
        static readonly Vector2Int[] ClassifyNeighbors = { Vector2Int.up, Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.down * 2 };

        /// <summary>
        /// Vuelve a cerrar las celdas ensanchadas de los túneles que dejan alguna pared imposible de dibujar (de 1 tile entre
        /// el túnel y una sala, una cara sin pared encima…), hasta que no quede ninguna. En el peor caso el pasillo vuelve a
        /// ser el recto de 2 tiles, que siempre se puede dibujar.
        /// </summary>
        static void RepairCorridors(FloorMap map, HashSet<Vector2Int> widened, List<List<Vector2Int>> corridors, HashSet<Vector2Int> corridorCells)
        {
            var check = new HashSet<Vector2Int>();
            foreach (var cell in widened)
                foreach (var neighbor in ClassifyNeighbors)
                    check.Add(cell - neighbor);

            while (check.Count > 0)
            {
                var closed = new HashSet<Vector2Int>();
                foreach (var cell in check)
                {
                    if (WallRules.Classify(map, cell) != Piece.Missing)
                        continue;
                    foreach (var neighbor in ClassifyNeighbors)
                    {
                        var open = cell + neighbor;
                        if (widened.Remove(open))
                        {
                            map.Set(open, FloorMap.Kind.Wall);
                            closed.Add(open);
                        }
                    }
                }

                check.Clear();
                foreach (var cell in closed)
                {
                    corridorCells.Remove(cell);
                    foreach (var corridor in corridors)
                        corridor.Remove(cell);
                    check.Add(cell);
                    foreach (var neighbor in ClassifyNeighbors)
                    {
                        check.Add(cell - neighbor);
                        check.Add(cell + neighbor);
                    }
                }
            }
        }

        static int Paint(Transform parent, FloorMap map, RoomTheme[] themes, HashSet<Vector2Int> noDecoration, int seed,
            List<(Vector2Int cell, TileBase tile)> decorations)
        {
            var grid = new GameObject("Level", typeof(Grid));
            grid.transform.SetParent(parent, false);

            var floor = CreateTilemap(grid.transform, "Ground", -20);
            var decoration = CreateTilemap(grid.transform, "Decoration", -15);
            // Cáusticas: luz que ondula sobre el suelo y la decoración, por debajo de paredes y personajes.
            var overlay = themes.Any(theme => theme != null && theme.HasCaustics) ? CreateTilemap(grid.transform, "Caustics", -14) : null;
            var walls = CreateTilemap(grid.transform, "Walls", -10);

            var missing = RoomPainter.Paint(map, themes, floor, decoration, walls, noDecoration, seed, overlay, decorations);
            if (missing.Count > 0)
                Debug.LogWarning($"FloorBuilder: {missing.Count} celdas sin pieza de arte (en magenta), por ejemplo {string.Join(", ", missing.Take(5))}.");

            // Una sola forma de colisión para todas las paredes. En contornos y no en polígonos: las salas son
            // huecos dentro de la pared, y en modo polígonos esos huecos quedarían rellenos.
            var body = walls.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = walls.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            walls.gameObject.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Outlines;

            return missing.Count;
        }

        static Tilemap CreateTilemap(Transform grid, string name, int sortingOrder)
        {
            var layer = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(grid, false);
            layer.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return layer.GetComponent<Tilemap>();
        }

        static Room BuildRoom(Transform parent, PlacedRoom placed, RoomTheme theme, FloorConfig config, FloorConfig.BossEntry boss,
            System.Random random, List<(Vector2 position, Glow glow)> glows, Dictionary<(LayoutRoom room, Vector2Int direction), BrushWall> brushes, FloorMap map)
        {
            var center = placed.FloorCenter;
            var floorBounds = placed.Template.FloorBounds();
            var roomObject = new GameObject(placed.Layout.Name);
            roomObject.transform.SetParent(parent, false);
            roomObject.transform.position = center;

            // Trigger 1,5 unidades más pequeño por lado: el jugador ya cruzó la puerta cuando se cierra.
            var trigger = roomObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(floorBounds.width - 3f, floorBounds.height - 3f);

            var doors = new List<Door>();
            foreach (var (direction, cells) in placed.DoorCells)
            {
                var doorCells = direction == Vector2Int.up ? placed.ArchCells[direction] : cells;
                doors.Add(BuildDoor(roomObject.transform, theme, direction, doorCells));
                if (placed.BrushDirections.Contains(direction))
                {
                    var brush = BuildBrush(roomObject.transform, theme, direction, doorCells);
                    if (brush != null)
                        brushes[(placed.Layout, direction)] = brush;
                }
            }

            // Con su propia semilla (la posición de la sala): los adornos no cambian lo que sale del resto de la sala.
            BuildWallDeco(roomObject.transform, placed, theme, new System.Random(placed.Origin.x * 73856093 ^ placed.Origin.y * 19349663));
            BuildObstacles(roomObject.transform, placed, theme, config.Pieces, random, glows);
            // Odres, bulbos y helechos no aparecen unos encima de otros ni de las rocas de cobertura: comparten lo ocupado de la sala.
            var taken = ObstacleFootprints(placed);
            BuildMethane(roomObject.transform, placed, theme, random, taken);
            BuildBulbs(roomObject.transform, placed, theme, random, taken);
            var puddleSpots = new List<(Vector2 position, float radius)>();
            BuildScenery(roomObject.transform, placed, theme, random, taken, puddleSpots);
            BuildMoss(roomObject.transform, placed, theme, random);
            // En la sala de paso, la vegetación del piso siguiente solo crece donde el suelo ya es suyo (junto a la charca).
            if (placed.Layout.Role == RoomRole.Transition && config.NextFloorTheme != null)
            {
                var pitCenter = placed.FloorCenter;
                BuildFerns(roomObject.transform, placed, config.NextFloorTheme, random, taken, cell =>
                {
                    var global = placed.Origin + cell;
                    var offset = (Vector2)global + Vector2.one * 0.5f - pitCenter;
                    return map.GetTheme(global) == NextTheme && (Mathf.Abs(offset.x) > PitFernClearance.x || Mathf.Abs(offset.y) > PitFernClearance.y);
                });
            }
            else
            {
                BuildFerns(roomObject.transform, placed, theme, random, taken);
            }
            var vents = BuildPieces(roomObject.transform, placed, config, random);
            // Donde aparece el jefe no hay algas ni plantas: taparían su presentación.
            Vector2? bossSpot = placed.Layout.Role == RoomRole.Boss ? placed.FloorCenter + boss.offset : null;
            var lightPlants = BuildDarkRoomPieces(roomObject.transform, placed, config, random, bossSpot);
            BuildAmbience(roomObject.transform, placed, theme, config, random);

            // Los enemigos esperan desactivados hasta que el jugador entra.
            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);
            enemiesRoot.gameObject.SetActive(false);
            // Los helechos enteros de la sala (en coordenadas de la plantilla): donde pueden esconderse las arañas.
            var fernSpots = roomObject.GetComponentsInChildren<Fern>().Where(fern => fern.Intact)
                .Select(fern => fern.Position - (Vector2)placed.Origin).ToList();
            var special = placed.Layout.Special;
            var laterWaves = System.Array.Empty<Transform>();
            if (special.HasValue && special.Value.waves > 0 && vents.Count > 0)
                laterWaves = SpawnWaves(placed, roomObject.transform, enemiesRoot, config, random, vents, special.Value.waves, puddleSpots, fernSpots);
            else
                SpawnEnemies(placed, enemiesRoot, config, boss, random, puddleSpots, fernSpots);
            SpawnTraps(placed, enemiesRoot, config, random, puddleSpots);

            var room = roomObject.AddComponent<Room>();
            room.Configure(enemiesRoot, doors.ToArray());
            room.ConfigureWaves(laterWaves, vents.ToArray());

            // La sala con plantas luminosas (la del Anomalocaris) se queda en penumbra al entrar.
            if (lightPlants.Count > 0)
                roomObject.AddComponent<RoomDarkness>().Configure(room, lightPlants);

            // La del Euriptérido (rocas R o zonas Z): el derrumbe de la cueva, que el jefe enciende por fases.
            var sinkZones = placed.Template.SinkZones();
            if (sinkZones.Count > 0 || placed.Template.BlockCorners(RoomTemplate.Mark.CollapsingRock).Count > 0)
                BuildCaveCollapse(parent, roomObject, room, placed, theme, config.Pieces, sinkZones);

            // La de inicio, la tienda y la sala de paso no tienen enemigos, así que no sueltan nada.
            if (HasEnemies(placed.Layout) && config.SmallBiomass != null)
                roomObject.AddComponent<RoomBiomass>().Configure(config.SmallBiomass, config.BigBiomass, config.MinimumBiomassPerRoom);

            // El Parkour se recorre a la velocidad base: las mejoras de velocidad no cuentan dentro.
            if (placed.Layout.Role == RoomRole.Special)
                roomObject.AddComponent<ParkourPace>();

            // Las salas de combate (no el jefe) pueden soltar medio corazón al limpiarse.
            if (placed.Layout.Role is (RoomRole.Combat or RoomRole.Side) && HasEnemies(placed.Layout))
                roomObject.AddComponent<RoomHearts>();

            if (placed.Layout.Reward && config.RewardPool != null)
                roomObject.AddComponent<MutationReward>().Configure(config.RewardPool, config.RewardOptions);

            if (placed.Layout.Role == RoomRole.Shop && config.ShopPrefab != null)
            {
                var shop = Object.Instantiate(config.ShopPrefab, placed.FloorCenter, Quaternion.identity, roomObject.transform);
                // Cada piso vende sus mutaciones (el prefab trae las del piso 1).
                if (config.FloorNumber > 1)
                    shop.SetPool(config.RewardPool);
                // El mostrador va arriba en el centro; si la puerta está arriba, se aparta para no taparla.
                if (placed.DoorCells.ContainsKey(Vector2Int.up))
                    shop.MoveCounter(-(placed.Template.FloorBounds().width / 2f) + ShopCounterMargin);
            }

            // Sala inicial, tienda y antesala del jefe: un teletransporte cerca de una esquina libre.
            if (config.TeleporterPrefab != null && placed.Layout.Role is (RoomRole.Start or RoomRole.Shop or RoomRole.Antechamber))
                BuildTeleporter(roomObject.transform, placed, config);

            if (placed.Layout.Role == RoomRole.Exchange && config.ExchangeCapsule != null)
            {
                // En el centro de la K de 2×2 del plano (o en el del suelo si el plano no la tiene).
                var corners = placed.Template.BlockCorners(RoomTemplate.Mark.Capsule);
                var position = corners.Count > 0 ? (Vector2)(placed.Origin + corners[0]) + Vector2.one : placed.FloorCenter;
                var capsule = Object.Instantiate(config.ExchangeCapsule, position, Quaternion.identity, roomObject.transform);
                capsule.SetFloor(config.RewardPool, config.FloorNumber);
                // El líquido da una luz verde suave.
                Ambience.CreateGlobalLight(roomObject.transform);
                Ambience.CreateGlow(capsule.transform, position + Vector2.up, new Glow(null, CapsuleGlow, 2.4f, new Vector2(0.45f, 0.8f), 1.4f), 0f);
            }

            if (placed.Layout.Role == RoomRole.Transition && config.TransitionPit != null)
            {
                var pit = Object.Instantiate(config.TransitionPit, placed.FloorCenter, Quaternion.identity, roomObject.transform);
                pit.SetNextFloor(config.NextFloorMouths, config.NextFloorName);
            }

            return room;
        }

        /// <summary>
        /// El teletransporte va en la casilla libre (suelo rodeado de suelo y lejos de las puertas) más cercana a la
        /// esquina de abajo a la izquierda del suelo, para no estorbar a la tienda ni a lo que haya en el centro.
        /// </summary>
        static void BuildTeleporter(Transform roomObject, PlacedRoom placed, FloorConfig config)
        {
            var floor = placed.Template.FloorBounds();
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(placed.Template, doorCells);
            var corner = new Vector2(floor.xMin + 3f, floor.yMin + 3f);
            Vector2 position = placed.FloorCenter;
            if (candidates.Count > 0)
            {
                var best = candidates.OrderBy(cell => Vector2.Distance(cell + new Vector2(0.5f, 0.5f), corner)).First();
                position = (Vector2)(placed.Origin + best) + new Vector2(0.5f, 0.5f);
            }

            var teleporter = Object.Instantiate(config.TeleporterPrefab, position, Quaternion.identity, roomObject);
            teleporter.Role = placed.Layout.Role;
        }

        static void BuildCaveCollapse(Transform parent, GameObject roomObject, Room room, PlacedRoom placed, RoomTheme theme,
            FloorConfig.RoomPieces pieces, List<RectInt> sinkZones)
        {
            var floor = placed.Template.FloorBounds();
            var floorArea = new RectInt(placed.Origin + floor.position, floor.size);
            var zones = sinkZones.Select(zone => new RectInt(placed.Origin + zone.position, zone.size)).ToList();
            var level = parent.Find("Level");
            var ground = level != null ? level.Find("Ground")?.GetComponent<Tilemap>() : null;
            var decoration = level != null ? level.Find("Decoration")?.GetComponent<Tilemap>() : null;
            roomObject.AddComponent<CaveCollapse>().Configure(room, floorArea, zones, theme, ground, decoration, new CaveCollapse.Art
            {
                fallShadow = pieces.fallShadow,
                fallingRock = pieces.fallingRock,
                rockImpact = pieces.rockImpact,
                pebbles = pieces.pebbles,
                pebblePuff = pieces.pebblePuff,
                floorCrack = pieces.floorCrack,
            });
        }

        // Los obstáculos van por encima de las paredes y por debajo de los personajes, como las columnas.
        const int ObstacleOrder = -5;
        // Rayos de luz: entre 2 y 3 por sala, con al menos esta separación (en tiles) entre ellos.
        const int MinLightRays = 2;
        const int MaxLightRays = 3;
        const int LightRaySpacing = 6;

        /// <summary>
        /// Obstáculos de cobertura de la plantilla (o de 1×1, O de 2×2): un sprite del tileset con su colisión, que también
        /// para los proyectiles. Los que brillan dejan su luz en <paramref name="glows"/>. Las rocas R (sala del Euriptérido)
        /// son rocas agrietadas de 2×2 con un <see cref="CollapsingRock"/>, que se derrumban con la muda.
        /// </summary>
        static void BuildObstacles(Transform roomObject, PlacedRoom placed, RoomTheme theme, FloorConfig.RoomPieces pieces,
            System.Random random, List<(Vector2 position, Glow glow)> glows)
        {
            var template = placed.Template;
            var small = new List<Vector2Int>();
            for (int y = 0; y < template.Height; y++)
                for (int x = 0; x < template.Width; x++)
                    if (template.Get(x, y) == RoomTemplate.Mark.Obstacle)
                        small.Add(new Vector2Int(x, y));
            var big = template.BigObstacleCorners();
            var collapsing = template.BlockCorners(RoomTemplate.Mark.CollapsingRock);
            if (small.Count == 0 && big.Count == 0 && collapsing.Count == 0)
                return;

            var root = new GameObject("Obstacles").transform;
            root.SetParent(roomObject, false);
            foreach (var cell in small)
                Obstacle(cell, 1, theme.SmallObstacles, "Obstacle");
            foreach (var cell in big)
                Obstacle(cell, 2, theme.BigObstacles, "ObstacleBig");
            foreach (var cell in collapsing)
            {
                // Sin el arte de la roca agrietada, una roca grande del tema (que también se derrumba).
                var cracked = pieces.crackedRock != null ? new[] { pieces.crackedRock } : theme.BigObstacles;
                var obstacle = Obstacle(cell, 2, cracked, "ObstacleCollapsing", pieces.crackedRock != null ? pieces.crackedRockOutline : null);
                var outline = obstacle.transform.Find("Outline");
                obstacle.AddComponent<CollapsingRock>().Configure(obstacle.GetComponent<SpriteRenderer>(),
                    outline != null ? outline.gameObject : null, obstacle.GetComponent<BoxCollider2D>(),
                    pieces.rockCollapse, pieces.rubble);
            }

            GameObject Obstacle(Vector2Int cell, int size, Sprite[] sprites, string name, Sprite ownOutline = null)
            {
                var obstacle = new GameObject(name);
                obstacle.transform.SetParent(root, false);
                var position = (Vector2)(placed.Origin + cell) + Vector2.one * (size / 2f);
                obstacle.transform.position = position;

                var renderer = obstacle.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = ObstacleOrder;
                if (sprites.Length > 0)
                    renderer.sprite = sprites[random.Next(sprites.Length)];
                obstacle.AddComponent<BoxCollider2D>().size = Vector2.one * size;
                // Los de 1×1 son bajos: un salto (Patas de salto) los cruza.
                if (size == 1)
                    obstacle.AddComponent<JumpableObstacle>();

                // Los tocones de la tierra quemada echan humo.
                var scenery = theme.Scenery;
                if (scenery != null && scenery.stump != null && renderer.sprite == scenery.stump && scenery.stumpSmoke.Length > 0)
                {
                    var smoke = new GameObject("StumpSmoke");
                    smoke.transform.SetParent(obstacle.transform, false);
                    smoke.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                    var smokeRenderer = smoke.AddComponent<SpriteRenderer>();
                    smokeRenderer.sortingOrder = ObstacleOrder + 1;
                    smoke.AddComponent<SpriteFramePlayer>().Play(smokeRenderer, scenery.stumpSmoke, 0.16f, newLoop: true);
                }

                // Contorno de color (1 px alrededor de la silueta): que se note que sirve de cobertura y no se pierda en el fondo.
                var outlineSprite = ownOutline != null ? ownOutline : theme.OutlineOf(renderer.sprite);
                if (outlineSprite != null)
                {
                    var outline = new GameObject("Outline");
                    outline.transform.SetParent(obstacle.transform, false);
                    var outlineRenderer = outline.AddComponent<SpriteRenderer>();
                    outlineRenderer.sprite = outlineSprite;
                    outlineRenderer.color = theme.ObstacleOutline;
                    outlineRenderer.sortingOrder = ObstacleOrder - 1;
                }

                var glow = theme.GlowOf(renderer.sprite);
                if (glow.HasValue)
                    glows.Add((position, glow.Value));
                return obstacle;
            }
        }

        /// <summary>
        /// Piezas de las salas especiales: el esqueleto del Fósil gigante (con un bloque de colisión por hueso), las almejas
        /// (dos C seguidas, la grande), el coral del corazón morado y las entradas de oleadas. De momento solo se ven y
        /// bloquean; abrirlas, cogerlas y las oleadas llegan en el paso 4.
        /// </summary>
        static readonly Color ClamGlow = new(1f, 0.85f, 0.55f);
        static readonly Color HeartGlow = new(0.75f, 0.45f, 1f);
        static readonly Color CapsuleGlow = new(0.45f, 0.95f, 0.65f);

        static List<WaveVent> BuildPieces(Transform roomObject, PlacedRoom placed, FloorConfig config, System.Random random)
        {
            var builtVents = new List<WaveVent>();
            var template = placed.Template;
            var pieces = config.Pieces;
            var fossil = template.CellsOf(RoomTemplate.Mark.Fossil);
            var chests = template.CellsOf(RoomTemplate.Mark.Chest);
            var hearts = template.BlockCorners(RoomTemplate.Mark.HeartCoral);
            var vents = template.CellsOf(RoomTemplate.Mark.WaveVent);
            if (fossil.Count == 0 && chests.Count == 0 && hearts.Count == 0 && vents.Count == 0)
                return builtVents;

            var root = new GameObject("Pieces").transform;
            root.SetParent(roomObject, false);

            if (fossil.Count > 0)
            {
                // El esqueleto se dibuja entero desde la esquina de arriba a la izquierda de la sala.
                var skeleton = Piece("Fossil", pieces.fossil, (Vector2)placed.Origin + new Vector2(0f, template.Height), Vector2.zero);
                skeleton.GetComponent<SpriteRenderer>().sortingOrder = ObstacleOrder;
                foreach (var cell in fossil)
                {
                    var bone = new GameObject("Bone");
                    bone.transform.SetParent(skeleton.transform, false);
                    bone.transform.position = (Vector2)(placed.Origin + cell) + Vector2.one * 0.5f;
                    bone.AddComponent<BoxCollider2D>().size = Vector2.one;
                }
            }

            var clams = config.Clams;
            var player = GameObject.FindWithTag("Player");
            var playerMutations = player != null ? player.GetComponent<PlayerMutations>() : null;
            var taken = new HashSet<Vector2Int>();
            foreach (var cell in chests)
            {
                if (taken.Contains(cell))
                    continue;
                bool big = chests.Contains(cell + Vector2Int.right);
                if (big)
                    taken.Add(cell + Vector2Int.right);
                var size = big ? new Vector2(2f, 1f) : Vector2.one;
                var position = (Vector2)(placed.Origin + cell) + size / 2f;
                var piece = Piece(big ? "ClamBig" : "Clam", big ? pieces.clamBig : pieces.clamSmall, position, size);
                var clam = piece.AddComponent<Clam>();
                // La grande guarda, además, una mutación especial si el piso tiene alguna (solo salen en almejas).
                var mutation = big && clams.bigMutations is { Length: > 0 }
                    ? Clam.PickMutation(clams.bigMutations, playerMutations, random, Clam.AssignedMutations())
                    : null;
                clam.Configure(piece.GetComponent<SpriteRenderer>(), big ? pieces.clamBigIdle : pieces.clamSmallIdle,
                    big ? pieces.clamBigOpen : pieces.clamSmallOpen, big ? pieces.clamBigOpened : pieces.clamSmallOpened, big,
                    big ? clams.bigBiomass : clams.smallBiomass, mutation, clams.mutationPickup, config.SmallBiomass, config.BigBiomass);
                clam.ConfigurePrompt(pieces.keyPrompt, big ? 1.4f : 1f);
                clam.SetRadius(big ? 1.8f : 1.3f);
                // Un brillo suave para que no se pase por alto.
                Ambience.CreateGlobalLight(roomObject);
                clam.SetGlow(Ambience.CreateGlow(piece.transform, position, new Glow(null, ClamGlow, big ? 2f : 1.4f, new Vector2(0.35f, 0.7f), 1.6f), 0f));
            }

            foreach (var cell in hearts)
            {
                var position = (Vector2)(placed.Origin + cell) + Vector2.one;
                var piece = Piece("HeartCoral", pieces.heartCoral, position, Vector2.one * 2f);
                var coral = piece.AddComponent<HeartCoral>();
                coral.Configure(piece.GetComponent<SpriteRenderer>(), pieces.heartCoralIdle, pieces.heartCoralTaken);
                coral.ConfigurePrompt(pieces.keyPrompt, 1.6f);
                coral.SetRadius(1.8f);
                Ambience.CreateGlobalLight(roomObject);
                coral.SetGlow(Ambience.CreateGlow(piece.transform, position, new Glow(null, HeartGlow, 2.2f, new Vector2(0.4f, 0.8f), 1.2f), 0f));
            }

            foreach (var cell in vents)
            {
                var piece = Piece("WaveVent", pieces.waveVent, (Vector2)(placed.Origin + cell) + Vector2.one * 0.5f, Vector2.zero);
                // Grieta del suelo: por debajo de todo lo que se mueve.
                piece.GetComponent<SpriteRenderer>().sortingOrder = -14;
                var vent = piece.AddComponent<WaveVent>();
                vent.Configure(piece.GetComponent<SpriteRenderer>(), pieces.waveVentIdle, pieces.waveVentActive);
                builtVents.Add(vent);
            }

            // Pieza con su sprite y, si bloquea (tamaño no nulo), su colisión.
            GameObject Piece(string name, Sprite sprite, Vector2 position, Vector2 size)
            {
                var piece = new GameObject(name);
                piece.transform.SetParent(root, false);
                piece.transform.position = position;
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = ObstacleOrder;
                if (size != Vector2.zero)
                    piece.AddComponent<BoxCollider2D>().size = size;
                return piece;
            }

            return builtVents;
        }

        static readonly Color LightPlantGlow = new(0.42f, 0.91f, 0.94f);
        // Altura de los bulbos sobre la base de la planta, donde va su luz.
        const float LightPlantGlowHeight = 0.9f;
        // Círculo de luz de cada planta: toda la intensidad hasta el radio interior y caída hasta el exterior.
        const float LightPlantInnerRadius = 1.8f;
        const float LightPlantOuterRadius = 4.5f;
        static readonly Vector2 LightPlantIntensity = new(1.3f, 1.7f);

        // Distancia mínima entre el sitio donde aparece el jefe y una planta luminosa o lo que tapa una columna de algas.
        const float BossClearance = 3.5f;

        static float DistanceToRect(Vector2 point, Rect rect)
        {
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Columnas de algas (A) y plantas luminosas (L) de la sala del Anomalocaris. Las algas se dibujan encima de todo
        /// y no bloquean; las plantas llevan su luz. Ninguna cerca de <paramref name="keepClear"/> (donde aparece el jefe).
        /// Devuelve las plantas.
        /// </summary>
        static List<LightPlant> BuildDarkRoomPieces(Transform roomObject, PlacedRoom placed, FloorConfig config, System.Random random,
            Vector2? keepClear = null)
        {
            var plants = new List<LightPlant>();
            var template = placed.Template;
            var algae = template.CellsOf(RoomTemplate.Mark.Algae);
            var lights = template.CellsOf(RoomTemplate.Mark.LightPlant);
            if (algae.Count == 0 && lights.Count == 0)
                return plants;

            var pieces = config.Pieces;
            var root = new GameObject("DarkRoom").transform;
            root.SetParent(roomObject, false);

            foreach (var cell in algae)
            {
                var basePosition = (Vector2)(placed.Origin + cell) + Vector2.one * 0.5f;
                if (keepClear.HasValue && DistanceToRect(keepClear.Value - basePosition, AlgaeColumn.DefaultCover) < BossClearance)
                    continue;
                var frames = random.Next(2) == 0 ? pieces.algaeColumnA : pieces.algaeColumnB;
                if (frames == null || frames.Length == 0)
                    frames = pieces.algaeColumnA ?? pieces.algaeColumnB;
                var column = new GameObject("AlgaeColumn");
                column.transform.SetParent(root, false);
                column.transform.position = basePosition;
                var renderer = column.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = AlgaeColumn.SortingOrder;
                if (frames != null && frames.Length > 0)
                {
                    renderer.sprite = frames[0];
                    column.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.2f, (float)random.NextDouble() * 0.8f);
                }
                column.AddComponent<AlgaeColumn>().Configure(renderer);
            }

            foreach (var cell in lights)
            {
                var position = (Vector2)(placed.Origin + cell) + Vector2.one * 0.5f;
                if (keepClear.HasValue && Vector2.Distance(keepClear.Value, position) < BossClearance)
                    continue;
                var piece = new GameObject("LightPlant");
                piece.transform.SetParent(root, false);
                piece.transform.position = position;
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = ObstacleOrder;
                SpriteLoop loop = null;
                if (pieces.lightPlant != null && pieces.lightPlant.Length > 0)
                {
                    renderer.sprite = pieces.lightPlant[0];
                    loop = piece.AddComponent<SpriteLoop>();
                    loop.Configure(renderer, pieces.lightPlant, 0.22f, (float)random.NextDouble() * 0.88f);
                }
                Ambience.CreateGlobalLight(roomObject);
                var glow = Ambience.CreateGlow(piece.transform, position + Vector2.up * LightPlantGlowHeight,
                    new Glow(null, LightPlantGlow, LightPlantOuterRadius, LightPlantIntensity, 0.88f), (float)random.NextDouble());
                glow.pointLightInnerRadius = LightPlantInnerRadius;
                var plant = piece.AddComponent<LightPlant>();
                plant.Configure(renderer, loop, pieces.lightPlantOff, glow, glow.GetComponent<GlowPulse>());
                plants.Add(plant);
            }
            return plants;
        }

        /// <summary>Salas con enemigos (y biomasa al limpiarlas): las de combate, las laterales y la del jefe, salvo las especiales sin enemigos.</summary>
        static bool HasEnemies(LayoutRoom room) =>
            room.Role is (RoomRole.Combat or RoomRole.Side or RoomRole.Boss) && room.EnemyMultiplier > 0f;

        /// <summary>Rayos de luz colgando de la pared de arriba y nieve marina, según el tileset de la sala.</summary>
        static void BuildAmbience(Transform roomObject, PlacedRoom placed, RoomTheme theme, FloorConfig config, System.Random random)
        {
            var template = placed.Template;
            var floor = template.FloorBounds();
            if (floor.width <= 0)
                return;

            if (theme.LightRay.Length > 0)
            {
                // Columnas cuyo suelo más alto toca la cara de la pared de arriba (sin los huecos de las puertas).
                var tops = new List<(int x, int y)>();
                for (int x = floor.xMin; x < floor.xMax; x++)
                {
                    for (int y = floor.yMax - 1; y >= floor.yMin; y--)
                    {
                        if (template.Get(x, y) == RoomTemplate.Mark.Floor && template.Get(x, y + 1) == RoomTemplate.Mark.Wall
                            && template.Get(x, y + 2) == RoomTemplate.Mark.Wall && template.Get(x, y + 3) == RoomTemplate.Mark.Wall)
                        {
                            tops.Add((x, y));
                            break;
                        }
                        if (template.Get(x, y) != RoomTemplate.Mark.Wall)
                            break;
                    }
                }

                int count = random.Next(MinLightRays, MaxLightRays + 1);
                var chosen = new List<int>();
                foreach (var (x, y) in tops.OrderBy(_ => random.Next()))
                {
                    if (chosen.Count >= count)
                        break;
                    if (chosen.Any(other => Mathf.Abs(other - x) < LightRaySpacing))
                        continue;
                    chosen.Add(x);
                    // Arriba de la cara de la pared (dos filas por encima del suelo).
                    var top = (Vector2)(placed.Origin + new Vector2Int(x, y + 3)) + new Vector2(0.5f, 0f);
                    Ambience.CreateLightRay(roomObject, top, theme.LightRay, theme.LightRayFrameDuration,
                        (float)random.NextDouble() * theme.LightRayFrameDuration * theme.LightRay.Length);
                }
            }

            if (theme.MarineSnow > 0 && config.MarineSnowMaterial != null && config.MarineSnowFlakes.Length > 0)
            {
                // Más copos cuanto más suelo, con la sala normal como referencia.
                int count = Mathf.Max(1, Mathf.RoundToInt(theme.MarineSnow * floor.width * floor.height / (float)config.ReferenceFloorArea));
                var area = new Rect(placed.Origin + floor.position, floor.size);
                Ambience.CreateMarineSnow(roomObject, area, count, config.MarineSnowFlakes, config.MarineSnowMaterial);
            }
        }

        // Distancia a una entrada de oleadas a la que pueden aparecer sus enemigos.
        const float WaveSpawnRadius = 3.5f;

        /// <summary>
        /// Oleadas de una sala con entradas de oleadas (Sala sellada, Puente roto): cada una con los enemigos de una sala normal
        /// por el multiplicador de la especial, junto a las entradas. La primera va en <paramref name="firstWave"/>; devuelve
        /// las demás, que esperan desactivadas.
        /// </summary>
        static Transform[] SpawnWaves(PlacedRoom placed, Transform roomObject, Transform firstWave, FloorConfig config, System.Random random,
            List<WaveVent> vents, int waves, List<(Vector2 position, float radius)> puddleSpots = null, IReadOnlyList<Vector2> fernSpots = null)
        {
            var template = placed.Template;
            var floorArea = Enumerable.Range(0, template.Width * template.Height)
                .Count(i => template.Get(i % template.Width, i / template.Width) == RoomTemplate.Mark.Floor);
            int budget = Mathf.Max(1, Mathf.RoundToInt(EncounterPlanner.Budget(config, placed.Layout.Depth, floorArea) * placed.Layout.EnemyMultiplier));

            // Salen por las entradas: sitios de aparición a poca distancia de alguna.
            var ventCells = vents.Select(vent => (Vector2)vent.transform.position - placed.Origin).ToList();
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var all = EncounterPlanner.SpawnCells(template, doorCells);
            var near = all.Where(cell => ventCells.Any(vent => Vector2.Distance(cell + Vector2.one * 0.5f, vent) <= WaveSpawnRadius)).ToList();
            var candidates = near.Count > 0 ? near : all;

            var later = new List<Transform>();
            for (int wave = 0; wave < waves; wave++)
            {
                var root = firstWave;
                if (wave > 0)
                {
                    root = new GameObject($"Wave_{wave + 1}").transform;
                    root.SetParent(roomObject, false);
                    root.gameObject.SetActive(false);
                    later.Add(root);
                }

                var enemies = EncounterPlanner.PickEnemies(config, placed.Layout.Depth, budget, random, EncounterPlanner.HasMud(template));
                foreach (var (enemy, position) in EncounterPlanner.Place(config, template, doorCells, enemies, candidates, random, WetGround(placed, puddleSpots), fernSpots))
                    EncounterPlanner.Spawn(config, enemy, placed.Origin + position, root);
            }
            return later.ToArray();
        }

        /// <summary>
        /// Dice si un punto de la plantilla cae sobre un charco del escenario (lodo o agua): las plantas no salen ahí. Null si no hay
        /// charcos.
        /// </summary>
        static System.Func<Vector2, bool> WetGround(PlacedRoom placed, List<(Vector2 position, float radius)> puddleSpots)
        {
            if (puddleSpots == null || puddleSpots.Count == 0)
                return null;
            // Con un poco de margen: el cuerpo de la planta mide más que su punto.
            return local => puddleSpots.Any(puddle => Vector2.Distance(puddle.position - (Vector2)placed.Origin, local) < puddle.radius + 0.3f);
        }

        /// <summary>
        /// Trampas de la sala (planta carnívora, enjambre de efímeras): en sitios libres, lejos de las puertas, las plantas solo sobre
        /// tierra. Cuelgan de los enemigos de la sala, así que esperan inactivas hasta que entra el jugador, y como no son enemigos no
        /// cuentan para limpiarla.
        /// </summary>
        static void SpawnTraps(PlacedRoom placed, Transform enemiesRoot, FloorConfig config, System.Random random,
            List<(Vector2 position, float radius)> puddleSpots)
        {
            // Salas de combate y laterales con enemigos; la del jefe, no.
            if (config.Traps.Length == 0 || placed.Layout.Role is not (RoomRole.Combat or RoomRole.Side) || !HasEnemies(placed.Layout))
                return;
            var picked = EncounterPlanner.PickTraps(config, placed.Layout.Depth, random);
            if (picked.Count == 0)
                return;

            var template = placed.Template;
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin).ToList();
            var candidates = EncounterPlanner.SpawnCells(template, doorCells);
            var wet = WetGround(placed, puddleSpots);

            // Cada trampa va a un sitio distinto, separadas entre sí.
            var spots = EncounterPlanner.Positions(candidates, picked.Count * 3, random);
            var used = new List<Vector2>();
            foreach (var trap in picked)
            {
                var pool = trap.dryGround && wet != null ? spots.Where(spot => !wet(spot)) : spots;
                var free = pool.Where(spot => used.All(other => Vector2.Distance(other, spot) >= 3f)).ToList();
                if (free.Count == 0)
                    continue;
                var spot = free[random.Next(free.Count)];
                used.Add(spot);
                Object.Instantiate(trap.prefab, placed.Origin + spot, Quaternion.identity, enemiesRoot);
            }
        }

        static void SpawnEnemies(PlacedRoom placed, Transform enemiesRoot, FloorConfig config, FloorConfig.BossEntry bossEntry,
            System.Random random, List<(Vector2 position, float radius)> puddleSpots = null, IReadOnlyList<Vector2> fernSpots = null)
        {
            switch (placed.Layout.Role)
            {
                case RoomRole.Start:
                case RoomRole.Shop:
                case RoomRole.Transition:
                case RoomRole.Exchange:
                case RoomRole.Antechamber:
                case RoomRole.Special:
                    return;

                case RoomRole.Boss:
                    if (bossEntry.prefab == null)
                        return;
                    var boss = Object.Instantiate(bossEntry.prefab, placed.FloorCenter + bossEntry.offset, Quaternion.identity, enemiesRoot);
                    // El jefe conoce su sala: el virus orbita alrededor del centro, la ameba se junta allí y la cámara
                    // se aleja hasta verla entera.
                    if (boss.TryGetComponent<IBoss>(out var bossAI))
                        bossAI.SetArena(enemiesRoot.parent, new Vector2(placed.Template.Width, placed.Template.Height));
                    return;
            }

            var template = placed.Template;
            var floorArea = Enumerable.Range(0, template.Width * template.Height)
                .Count(i => template.Get(i % template.Width, i / template.Width) == RoomTemplate.Mark.Floor);
            if (!HasEnemies(placed.Layout))
                return;
            // Las especiales cambian la cantidad (el Corazón morado, el doble).
            int budget = Mathf.RoundToInt(EncounterPlanner.Budget(config, placed.Layout.Depth, floorArea) * placed.Layout.EnemyMultiplier);
            var enemies = EncounterPlanner.PickEnemies(config, placed.Layout.Depth, budget, random, EncounterPlanner.HasMud(template));

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin).ToList();
            var candidates = EncounterPlanner.SpawnCells(template, doorCells);
            var placements = EncounterPlanner.Place(config, template, doorCells, enemies, candidates, random, WetGround(placed, puddleSpots), fernSpots);
            if (placements.Count < enemies.Count)
                Debug.LogWarning($"FloorBuilder: en {placed.Layout.Name} solo caben {placements.Count} de {enemies.Count} enemigos.");

            foreach (var (enemy, position) in placements)
                EncounterPlanner.Spawn(config, enemy, placed.Origin + position, enemiesRoot);
        }

        /// <summary>Membrana animada sobre el hueco, con su colisión. Arriba, además, el arco fijo de la pared.</summary>
        static Door BuildDoor(Transform parent, RoomTheme theme, Vector2Int direction, List<Vector2Int> cells)
        {
            int minX = cells.Min(c => c.x), minY = cells.Min(c => c.y);
            int maxX = cells.Max(c => c.x), maxY = cells.Max(c => c.y);
            var size = new Vector2(maxX - minX + 1, maxY - minY + 1);
            var position = new Vector2(minX, minY) + size / 2f;

            if (direction == Vector2Int.up && theme.Doorway != null)
            {
                var doorway = new GameObject("Doorway");
                doorway.transform.SetParent(parent, false);
                doorway.transform.position = position;
                var doorwayRenderer = doorway.AddComponent<SpriteRenderer>();
                doorwayRenderer.sprite = theme.Doorway;
                doorwayRenderer.sortingOrder = -9;

                // Con el arte del pulido, el jugador cruza el arco por detrás de su marco.
                var layers = PolishArt.Current != null ? PolishArt.Current.DoorFor(theme.Doorway) : null;
                if (layers != null)
                    DoorwayCrossing.Create(doorway.transform, layers.front, layers.shade);
            }

            BuildDoorFrame(parent, theme, direction, position);

            var doorObject = new GameObject($"Door_{DirectionName(direction)}");
            doorObject.transform.SetParent(parent, false);
            doorObject.transform.position = position;

            var frames = direction == Vector2Int.up ? theme.DoorTop : direction == Vector2Int.down ? theme.DoorBottom : theme.DoorSide;
            // Losas del piso 2: cerrar y abrir con sus cuadros; si el tileset no las tiene, la membrana de siempre.
            var slabs = direction == Vector2Int.up ? theme.SlabTop : direction == Vector2Int.down ? theme.SlabBottom : theme.SlabSide;
            bool useSlabs = slabs != null && slabs.IsValid;
            var renderer = doorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = useSlabs ? slabs.close[^1] : frames != null && frames.Length > 0 ? frames[^1] : null;
            renderer.sortingOrder = -8;
            renderer.flipX = direction == Vector2Int.right;

            var blocker = doorObject.AddComponent<BoxCollider2D>();
            blocker.size = size;

            var door = doorObject.AddComponent<Door>();
            if (useSlabs)
                door.Configure(blocker, renderer, slabs);
            else
                door.Configure(blocker, renderer, frames);
            return door;
        }

        // Dónde van los marcos de las puertas respecto al centro del arco o del hueco (README de doors-frames) y su orden de dibujo.
        const float CrownLift = 1.25f;
        const float SideFrameOffsetX = 0.75f;
        const float SideFrameOffsetY = 0.5f;
        const int DoorCrownOrder = 41;
        const int DoorBackOrder = -7;
        // Por encima del jugador (y de lo que lleva), como el marco del arco de arriba.
        const int DoorFrontOrder = 40;

        /// <summary>
        /// La corona del arco de las puertas de arriba y el par de piezas (detrás y delante del jugador) de las laterales, pintadas con
        /// la textura del tema. Las de abajo no cambian. Sin ese arte en el tema, no se pone nada.
        /// </summary>
        static void BuildDoorFrame(Transform parent, RoomTheme theme, Vector2Int direction, Vector2 position)
        {
            if (direction == Vector2Int.up && theme.DoorCrown != null)
            {
                FramePiece(parent, "DoorwayCrown", theme.DoorCrown, position + Vector2.up * CrownLift, false, DoorCrownOrder);
            }
            else if ((direction == Vector2Int.left || direction == Vector2Int.right) && theme.DoorSideBack != null && theme.DoorSideFront != null)
            {
                bool right = direction == Vector2Int.right;
                var spot = position + new Vector2(right ? -SideFrameOffsetX : SideFrameOffsetX, SideFrameOffsetY);
                FramePiece(parent, "DoorwaySideBack", theme.DoorSideBack, spot, right, DoorBackOrder);
                FramePiece(parent, "DoorwaySideFront", theme.DoorSideFront, spot, right, DoorFrontOrder);
            }
        }

        static void FramePiece(Transform parent, string name, Sprite sprite, Vector2 position, bool flipX, int order)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.position = position;
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = flipX;
            renderer.sortingOrder = order;
        }

        // Adornos sueltos en las paredes laterales: uno de cada 4-5 tiles de borde, al azar, nunca pegados a una puerta ni en las esquinas.
        const float WallDecoChance = 0.22f;
        const int WallDecoSpacing = 3;
        // Los adornos asoman 8 px a la sala (el dibujo mide 24 px y el tile 16): el centro se corre 4 px hacia ella.
        const float WallDecoShift = 0.25f;
        const int WallDecoOrder = -9;

        static void BuildWallDeco(Transform parent, PlacedRoom placed, RoomTheme theme, System.Random random)
        {
            var art = theme.WallSideDeco;
            if (art.Length == 0)
                return;

            var template = placed.Template;
            var map = template.ToFloorMap(Vector2Int.zero, openDoors: true);
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin).ToList();
            var holder = new GameObject("WallDeco").transform;
            holder.SetParent(parent, false);
            var lastY = new Dictionary<int, int>();
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var piece = WallRules.Classify(map, cell);
                    if (piece is not (Piece.EdgeLeft or Piece.EdgeRight))
                        continue;
                    // Sin esquinas: encima y debajo sigue el mismo borde.
                    if (WallRules.Classify(map, cell + Vector2Int.up) != piece || WallRules.Classify(map, cell + Vector2Int.down) != piece)
                        continue;
                    if (doorCells.Any(door => Mathf.Abs(door.x - x) <= 1 && Mathf.Abs(door.y - y) <= 1))
                        continue;
                    if (random.NextDouble() >= WallDecoChance)
                        continue;
                    int column = x * 2 + (piece == Piece.EdgeLeft ? 0 : 1);
                    if (lastY.TryGetValue(column, out int previous) && y - previous < WallDecoSpacing)
                        continue;
                    lastY[column] = y;

                    bool left = piece == Piece.EdgeLeft;
                    var position = placed.Origin + new Vector2(x + 0.5f + (left ? WallDecoShift : -WallDecoShift), y + 0.5f);
                    FramePiece(holder, "WallDeco", art[random.Next(art.Length)], position, !left, WallDecoOrder);
                }
            }
        }

        // Luz cian suave de los odres de metano (el cian pálido de su arte).
        static readonly Glow MethaneGlow = new(null, new Color(0.42f, 0.82f, 0.93f), 2.2f, new Vector2(0.25f, 0.45f), 1.8f);

        /// <summary>
        /// Metano del tramo (piso 3): unos pocos odres y manchas de niebla en las salas de combate y laterales, lejos de las
        /// puertas, las paredes y el lodo. Ni en el inicio, ni en la tienda, ni en las salas de los jefes.
        /// </summary>
        static void BuildMethane(Transform parent, PlacedRoom placed, RoomTheme theme, System.Random random, List<(Vector2 position, float radius)> taken)
        {
            var spec = theme.Methane;
            if (spec == null || placed.Layout.Role is not (RoomRole.Combat or RoomRole.Side))
                return;
            if (!spec.HasSacs && !spec.HasMist)
                return;

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(placed.Template, doorCells);
            if (candidates.Count == 0)
                return;

            var holder = new GameObject("Methane").transform;
            holder.SetParent(parent, false);
            if (spec.HasSacs)
            {
                int sacs = random.Next(Mathf.Min(spec.sacCount.x, spec.sacCount.y), Mathf.Max(spec.sacCount.x, spec.sacCount.y) + 1);
                int placedSacs = 0;
                foreach (var local in EncounterPlanner.Positions(candidates, sacs * RetryFactor, random))
                {
                    if (placedSacs == sacs)
                        break;
                    // El centro de la celda, pero con la base del odre un poco más abajo.
                    var position = placed.Origin + local;
                    if (!TakeSpot(taken, position, BigSceneryRadius))
                        continue;
                    var sac = MethaneSac.Create(holder, new Vector2(position.x, position.y - 0.4f), spec.sacIdle, spec.sacBurst, spec.sacRemains);
                    // Una luz cian suave que late (con la luz global que hace falta para que lo demás no quede a oscuras).
                    Ambience.CreateGlobalLight(holder);
                    sac.SetGlow(Ambience.CreateGlow(sac.transform, sac.Position, MethaneGlow, (float)random.NextDouble() * MethaneGlow.period));
                    placedSacs++;
                }
            }
            if (spec.HasMist)
            {
                int patches = Mathf.RoundToInt(candidates.Count * spec.mistPerCell);
                foreach (var local in EncounterPlanner.Positions(candidates, patches, random))
                {
                    var position = placed.Origin + local;
                    MethaneMist.Create(holder, new Vector2(position.x, position.y - 0.5f), spec.mist, (float)random.NextDouble() * 5f);
                }
            }
        }

        /// <summary>
        /// Plantas venenosas del tramo (piso 3): unos pocos bulbos en las salas de combate y laterales, lejos de las puertas, las
        /// paredes y el lodo. Ni en el inicio, ni en la tienda, ni en las salas de los jefes.
        /// </summary>
        static void BuildBulbs(Transform parent, PlacedRoom placed, RoomTheme theme, System.Random random, List<(Vector2 position, float radius)> taken)
        {
            var spec = theme.Poison;
            if (spec == null || !spec.HasBulbs || placed.Layout.Role is not (RoomRole.Combat or RoomRole.Side))
                return;

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(placed.Template, doorCells);
            int count = random.Next(Mathf.Min(spec.bulbCount.x, spec.bulbCount.y), Mathf.Max(spec.bulbCount.x, spec.bulbCount.y) + 1);
            if (candidates.Count == 0 || count == 0)
                return;

            var holder = new GameObject("PoisonBulbs").transform;
            holder.SetParent(parent, false);
            int placedBulbs = 0;
            foreach (var local in EncounterPlanner.Positions(candidates, count * RetryFactor, random))
            {
                if (placedBulbs == count)
                    break;
                var position = placed.Origin + local;
                if (!TakeSpot(taken, position, BigSceneryRadius))
                    continue;
                PoisonBulb.Create(holder, new Vector2(position.x, position.y - 0.4f), spec);
                placedBulbs++;
            }
        }

        static int RandomCount(System.Random random, Vector2Int range) =>
            random.Next(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y) + 1);

        /// <summary>
        /// Escenario vivo del tramo (piso 3): fumarolas (solo donde se combate), charcos de lodo y de agua con sus mosquitos,
        /// escarabajos que huyen y brasas, repartidos por los sitios libres de la sala, lejos de las puertas, las paredes y el lodo
        /// profundo. No en las salas de los jefes, la tienda ni las fijas.
        /// </summary>
        static void BuildScenery(Transform parent, PlacedRoom placed, RoomTheme theme, System.Random random, List<(Vector2 position, float radius)> taken,
            List<(Vector2 position, float radius)> puddleSpots = null)
        {
            var art = theme.Scenery;
            var role = placed.Layout.Role;
            if (art == null || role is not (RoomRole.Start or RoomRole.Combat or RoomRole.Side or RoomRole.Special))
                return;

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(placed.Template, doorCells);
            if (candidates.Count == 0)
                return;

            var holder = new GameObject("Scenery").transform;
            holder.SetParent(parent, false);

            // Fumarolas: solo en las salas de combate y laterales (hacen daño).
            if (art.HasVents && role is RoomRole.Combat or RoomRole.Side)
            {
                int vents = RandomCount(random, art.ventCount);
                int placedVents = 0;
                foreach (var local in EncounterPlanner.Positions(candidates, vents * RetryFactor, random))
                {
                    if (placedVents == vents)
                        break;
                    var position = placed.Origin + local;
                    if (!TakeSpot(taken, position, VentRadius))
                        continue;
                    // La boca ocupa la celda: la base del dibujo, en el borde de abajo de la celda.
                    SteamVent.Create(holder, new Vector2(position.x, position.y - 0.5f), art.ventIdle, art.ventWarn, art.ventBlast);
                    placedVents++;
                }
            }

            if (art.HasPuddles)
            {
                int puddles = RandomCount(random, art.puddleCount);
                int placedPuddles = 0;
                var water = art.puddles.Where(type => type != null && !type.sludge && type.frames.Length > 0).ToArray();
                var sludge = art.puddles.Where(type => type != null && type.sludge && type.frames.Length > 0).ToArray();
                foreach (var local in EncounterPlanner.Positions(candidates, puddles * RetryFactor, random))
                {
                    if (placedPuddles == puddles)
                        break;
                    var position = placed.Origin + local;
                    if (!TakeSpot(taken, position, PuddleRadius))
                        continue;

                    bool isWater = water.Length > 0 && (sludge.Length == 0 || random.NextDouble() < art.waterShare);
                    var options = isWater ? water : sludge;
                    if (options.Length == 0)
                        continue;
                    var type = options[random.Next(options.Length)];
                    GroundPuddle.Create(holder, position, type.frames, type.frameSeconds, type.sludge);
                    puddleSpots?.Add((position, PuddleRadius));
                    placedPuddles++;

                    // Mosquitos sobre algunos charcos de agua.
                    if (isWater && art.gnats.Length > 0 && random.NextDouble() < art.gnatChance)
                    {
                        var gnats = new GameObject("Gnats");
                        gnats.transform.SetParent(holder, false);
                        gnats.transform.position = position + Vector2.up * 0.6f;
                        var gnatRenderer = gnats.AddComponent<SpriteRenderer>();
                        gnatRenderer.sortingOrder = 8;
                        gnats.AddComponent<SpriteFramePlayer>().Play(gnatRenderer, art.gnats, 0.12f, newLoop: true);
                    }
                }
            }

            if (art.beetleIdle.Length > 0 && art.beetleCount.y > 0)
            {
                foreach (var local in EncounterPlanner.Positions(candidates, RandomCount(random, art.beetleCount), random))
                    BeetleCritter.Create(holder, placed.Origin + local, art.beetleIdle, art.beetleRun);
            }

            if (art.embers.Length > 0 && art.emberCount.y > 0)
            {
                foreach (var local in EncounterPlanner.Positions(candidates, RandomCount(random, art.emberCount), random))
                {
                    var embers = new GameObject("Embers");
                    embers.transform.SetParent(holder, false);
                    embers.transform.position = placed.Origin + local;
                    var emberRenderer = embers.AddComponent<SpriteRenderer>();
                    emberRenderer.sortingOrder = -2;
                    embers.AddComponent<SpriteFramePlayer>().Play(emberRenderer, art.embers, 0.3f, newLoop: true);
                }
            }
        }

        /// <summary>
        /// Musgo que gotea (piso 3): sobre las dos filas de la cara de la pared de arriba, con la gota cayendo a la primera fila de
        /// suelo. Algunos sitios al azar, separados entre sí.
        /// </summary>
        static void BuildMoss(Transform parent, PlacedRoom placed, RoomTheme theme, System.Random random)
        {
            var art = theme.Scenery;
            var role = placed.Layout.Role;
            if (art == null || art.dripMoss.Length == 0 || art.mossChance <= 0f
                || role is not (RoomRole.Start or RoomRole.Combat or RoomRole.Side or RoomRole.Special))
                return;

            var template = placed.Template;
            var map = template.ToFloorMap(Vector2Int.zero, openDoors: true);
            var holder = new GameObject("DripMoss").transform;
            holder.SetParent(parent, false);
            var lastX = new Dictionary<int, int>();
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    // La fila de arriba de la cara, con la de abajo debajo y suelo después.
                    if (WallRules.Classify(map, cell) != Piece.FaceUpper
                        || WallRules.Classify(map, cell + Vector2Int.down) != Piece.FaceLower
                        || !map.IsOpen(x, y - 2))
                        continue;
                    if (random.NextDouble() >= art.mossChance)
                        continue;
                    if (lastX.TryGetValue(y, out int previous) && x - previous < MossSpacing)
                        continue;
                    lastX[y] = x;

                    var moss = new GameObject("DripMoss");
                    moss.transform.SetParent(holder, false);
                    // La base del dibujo en el borde de abajo de la primera fila de suelo (48 px = cara de 2 filas + esa fila).
                    var position = placed.Origin + new Vector2(x + 0.5f, y - 2f);
                    moss.transform.position = position;
                    var mossRenderer = moss.AddComponent<SpriteRenderer>();
                    mossRenderer.sortingOrder = 0;
                    moss.AddComponent<SpriteFramePlayer>().Play(mossRenderer, art.dripMoss, 0.15f, newLoop: true);
                }
            }
        }

        // Radios de lo que ocupa cada cosa del escenario (en tiles): dos cosas se separan al menos la suma de sus radios, así los
        // helechos (de unas 2 u de ancho), los odres y los bulbos no se montan unos sobre otros.
        const float FernRadius = 0.9f;
        const float BigSceneryRadius = 1.5f;
        const float SmallObstacleRadius = 1.0f;
        const float BigObstacleRadius = 1.6f;
        const float VentRadius = 1.0f;
        const float PuddleRadius = 1.3f;
        // Entre dos trozos de musgo de la misma fila de pared, al menos estas celdas.
        const int MossSpacing = 4;

        // Se piden más sitios de los necesarios: algunos se descartan por estar demasiado cerca de otra cosa.
        const int RetryFactor = 4;

        /// <summary>Lo que ya ocupan las rocas y los troncos de cobertura de la sala (centro y radio), en coordenadas del mundo.</summary>
        static List<(Vector2 position, float radius)> ObstacleFootprints(PlacedRoom placed)
        {
            var taken = new List<(Vector2, float)>();
            var template = placed.Template;
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    if (template.Get(x, y) == RoomTemplate.Mark.Obstacle)
                        taken.Add(((Vector2)(placed.Origin + new Vector2Int(x, y)) + Vector2.one * 0.5f, SmallObstacleRadius));
                }
            }
            foreach (var corner in template.BigObstacleCorners())
                taken.Add(((Vector2)(placed.Origin + corner) + Vector2.one, BigObstacleRadius));
            return taken;
        }

        /// <summary>Reserva un sitio si no pisa nada de lo ya ocupado (distancia al menos la suma de radios). False si está ocupado.</summary>
        static bool TakeSpot(List<(Vector2 position, float radius)> taken, Vector2 position, float radius)
        {
            foreach (var (other, otherRadius) in taken)
            {
                if (Vector2.Distance(other, position) < radius + otherRadius)
                    return false;
            }
            taken.Add((position, radius));
            return true;
        }

        /// <summary>
        /// Helechos altos del tileset (piso 3): tantos como la densidad del tramo por cada tile de suelo libre, repartidos por
        /// la sala lejos de las puertas, de las paredes y del lodo, y separados entre sí. Sin tipos o sin densidad, nada.
        /// </summary>
        // Sin helechos sobre la fosa de la sala de paso ni pegados a su borde.
        static readonly Vector2 PitFernClearance = new(4.5f, 3.5f);

        static void BuildFerns(Transform parent, PlacedRoom placed, RoomTheme theme, System.Random random, List<(Vector2 position, float radius)> taken,
            System.Func<Vector2Int, bool> accept = null)
        {
            var specs = theme.Ferns.Where(spec => spec != null && spec.IsValid).ToList();
            if (specs.Count == 0 || theme.FernDensity <= 0f)
                return;

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(placed.Template, doorCells);
            if (accept != null)
                candidates = candidates.Where(accept).ToList();
            int count = Mathf.RoundToInt(candidates.Count * theme.FernDensity);
            var positions = EncounterPlanner.Positions(candidates, count * RetryFactor, random);
            if (positions.Count == 0)
                return;

            var holder = new GameObject("Ferns").transform;
            holder.SetParent(parent, false);
            float totalWeight = specs.Sum(spec => spec.weight);
            int placedFerns = 0;
            foreach (var local in positions)
            {
                if (placedFerns == count)
                    break;
                var position = placed.Origin + local;
                if (!TakeSpot(taken, position, FernRadius))
                    continue;
                placedFerns++;

                float roll = (float)random.NextDouble() * totalWeight;
                var spec = specs[specs.Count - 1];
                foreach (var option in specs)
                {
                    roll -= option.weight;
                    if (roll <= 0f)
                    {
                        spec = option;
                        break;
                    }
                }

                // Pivote en el pie: la posición es la base del helecho, en el centro de la celda pero por abajo.
                var fern = new GameObject($"Fern_{spec.name}");
                fern.transform.SetParent(holder, false);
                fern.transform.position = new Vector3(position.x, position.y - 0.4f, 0f);
                var renderer = fern.AddComponent<SpriteRenderer>();
                var trigger = fern.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = spec.footRadius;
                trigger.offset = new Vector2(0f, spec.footRadius * 0.6f);
                fern.AddComponent<Fern>().Configure(renderer, spec, (float)random.NextDouble() * 10f);
            }
        }

        /// <summary>
        /// Maleza quemable delante de una puerta de atajo: tapa el hueco (por encima de la puerta) y bloquea el paso hasta que
        /// arde. Null si el tileset no tiene maleza.
        /// </summary>
        static BrushWall BuildBrush(Transform parent, RoomTheme theme, Vector2Int direction, List<Vector2Int> cells)
        {
            var animation = theme.BrushFor(direction);
            if (animation == null)
            {
                Debug.LogWarning($"FloorBuilder: el tileset {theme.name} no tiene maleza; el atajo de {parent.name} queda sin sellar.");
                return null;
            }

            int minX = cells.Min(c => c.x), minY = cells.Min(c => c.y);
            int maxX = cells.Max(c => c.x), maxY = cells.Max(c => c.y);
            var size = new Vector2(maxX - minX + 1, maxY - minY + 1);
            var brushObject = new GameObject($"Brush_{DirectionName(direction)}");
            brushObject.transform.SetParent(parent, false);
            brushObject.transform.position = new Vector2(minX, minY) + size / 2f;

            var renderer = brushObject.AddComponent<SpriteRenderer>();
            renderer.sprite = animation.idle;
            renderer.sortingOrder = -7;
            renderer.flipX = direction == Vector2Int.right;
            var blocker = brushObject.AddComponent<BoxCollider2D>();
            blocker.size = size;

            var brush = brushObject.AddComponent<BrushWall>();
            brush.Configure(blocker, renderer, animation, cells);
            return brush;
        }

        static string DirectionName(Vector2Int direction) =>
            direction == Vector2Int.up ? "Up" : direction == Vector2Int.down ? "Down" : direction == Vector2Int.left ? "Left" : "Right";

        /// <summary>Salida en el centro de la sala del jefe: aparece al vencerlo.</summary>
        static void BuildExit(Room room, FloorConfig config)
        {
            var exitObject = new GameObject("Exit");
            exitObject.transform.SetParent(room.transform, false);
            exitObject.transform.localScale = new Vector3(2f, 2f, 1f);

            var renderer = exitObject.AddComponent<SpriteRenderer>();
            renderer.sprite = config.ExitSprite;
            renderer.color = config.ExitColor;
            renderer.sortingOrder = 1;

            exitObject.AddComponent<BoxCollider2D>().isTrigger = true;
            exitObject.AddComponent<RoomExit>().SetRoom(room);
        }
    }
}

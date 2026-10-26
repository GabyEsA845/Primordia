using System.Collections.Generic;
using System.Linq;
using Primordia.Biomass;
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

            foreach (var room in placed.Where(room => room.Layout.Role == RoomRole.Transition))
            {
                ThemeTransition(map, room, placed);
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
            foreach (var room in placed)
            {
                var builtRoom = BuildRoom(parent, room, themes[room.Theme], config, boss, random, glows);
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

            foreach (var (from, to, cells) in corridorCells)
            {
                var corridor = new MapCorridor { A = mapRooms[from], B = mapRooms[to] };
                corridor.Cells.AddRange(cells);
                built.Corridors.Add(corridor);
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
        static void ThemeTransition(FloorMap map, PlacedRoom room, List<PlacedRoom> placed)
        {
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
                map.SetTheme(cell, t < MidThemeFrom ? SoupTheme : t < NextThemeFrom ? MidTheme : NextTheme);
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
            System.Random random, List<(Vector2 position, Glow glow)> glows)
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
            }

            BuildObstacles(roomObject.transform, placed, theme, random, glows);
            var vents = BuildPieces(roomObject.transform, placed, config, random);
            BuildAmbience(roomObject.transform, placed, theme, config, random);

            // Los enemigos esperan desactivados hasta que el jugador entra.
            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);
            enemiesRoot.gameObject.SetActive(false);
            var special = placed.Layout.Special;
            var laterWaves = System.Array.Empty<Transform>();
            if (special.HasValue && special.Value.waves > 0 && vents.Count > 0)
                laterWaves = SpawnWaves(placed, roomObject.transform, enemiesRoot, config, random, vents, special.Value.waves);
            else
                SpawnEnemies(placed, enemiesRoot, config, boss, random);

            var room = roomObject.AddComponent<Room>();
            room.Configure(enemiesRoot, doors.ToArray());
            room.ConfigureWaves(laterWaves, vents.ToArray());

            // La de inicio, la tienda y la sala de paso no tienen enemigos, así que no sueltan nada.
            if (HasEnemies(placed.Layout) && config.SmallBiomass != null)
                roomObject.AddComponent<RoomBiomass>().Configure(config.SmallBiomass, config.BigBiomass, config.MinimumBiomassPerRoom);

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

            if (placed.Layout.Role == RoomRole.Transition && config.TransitionPit != null)
            {
                var pit = Object.Instantiate(config.TransitionPit, placed.FloorCenter, Quaternion.identity, roomObject.transform);
                pit.SetNextFloor(config.NextFloorMouths, config.NextFloorName);
            }

            return room;
        }

        // Los obstáculos van por encima de las paredes y por debajo de los personajes, como las columnas.
        const int ObstacleOrder = -5;
        // Rayos de luz: entre 2 y 3 por sala, con al menos esta separación (en tiles) entre ellos.
        const int MinLightRays = 2;
        const int MaxLightRays = 3;
        const int LightRaySpacing = 6;

        /// <summary>
        /// Obstáculos de cobertura de la plantilla (o de 1×1, O de 2×2): un sprite del tileset con su colisión, que también
        /// para los proyectiles. Los que brillan dejan su luz en <paramref name="glows"/>.
        /// </summary>
        static void BuildObstacles(Transform roomObject, PlacedRoom placed, RoomTheme theme, System.Random random,
            List<(Vector2 position, Glow glow)> glows)
        {
            var template = placed.Template;
            var small = new List<Vector2Int>();
            for (int y = 0; y < template.Height; y++)
                for (int x = 0; x < template.Width; x++)
                    if (template.Get(x, y) == RoomTemplate.Mark.Obstacle)
                        small.Add(new Vector2Int(x, y));
            var big = template.BigObstacleCorners();
            // Rocas del Euriptérido que se derrumban con la muda: de momento, rocas grandes normales.
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
                Obstacle(cell, 2, theme.BigObstacles, "ObstacleCollapsing");

            void Obstacle(Vector2Int cell, int size, Sprite[] sprites, string name)
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

                // Contorno de color (1 px alrededor de la silueta): que se note que sirve de cobertura y no se pierda en el fondo.
                var outlineSprite = theme.OutlineOf(renderer.sprite);
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
            }
        }

        /// <summary>
        /// Piezas de las salas especiales: el esqueleto del Fósil gigante (con un bloque de colisión por hueso), las almejas
        /// (dos C seguidas, la grande), el coral del corazón morado y las entradas de oleadas. De momento solo se ven y
        /// bloquean; abrirlas, cogerlas y las oleadas llegan en el paso 4.
        /// </summary>
        static readonly Color ClamGlow = new(1f, 0.85f, 0.55f);
        static readonly Color HeartGlow = new(0.75f, 0.45f, 1f);

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
            List<WaveVent> vents, int waves)
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

                var enemies = EncounterPlanner.PickEnemies(config, placed.Layout.Depth, budget, random);
                var positions = EncounterPlanner.Positions(candidates, enemies.Count, random);
                for (int i = 0; i < positions.Count; i++)
                    EncounterPlanner.Spawn(config, enemies[i], placed.Origin + positions[i], root);
            }
            return later.ToArray();
        }

        static void SpawnEnemies(PlacedRoom placed, Transform enemiesRoot, FloorConfig config, FloorConfig.BossEntry bossEntry,
            System.Random random)
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
            var enemies = EncounterPlanner.PickEnemies(config, placed.Layout.Depth, budget, random);

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(template, doorCells);
            var positions = EncounterPlanner.Positions(candidates, enemies.Count, random);
            if (positions.Count < enemies.Count)
                Debug.LogWarning($"FloorBuilder: en {placed.Layout.Name} solo caben {positions.Count} de {enemies.Count} enemigos.");

            for (int i = 0; i < positions.Count; i++)
                EncounterPlanner.Spawn(config, enemies[i], placed.Origin + positions[i], enemiesRoot);
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
            }

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

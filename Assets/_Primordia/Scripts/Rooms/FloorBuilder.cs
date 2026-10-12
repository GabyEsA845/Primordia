using System.Collections.Generic;
using System.Linq;
using Primordia.Enemies;
using Primordia.Mutations;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Rooms
{
    /// <summary>
    /// Monta un <see cref="FloorLayout"/> en la escena: coloca las plantillas en la rejilla, las une con pasillos,
    /// pinta todo el piso en Tilemaps y crea salas, puertas, enemigos, recompensas, jefe y salida.
    /// Funciona igual en el juego (piso aleatorio) que en el editor.
    /// </summary>
    public static class FloorBuilder
    {
        /// <summary>Pasillo más corto entre dos salas vecinas; si una sala es más pequeña que su celda, crece.</summary>
        public const int CorridorLength = 4;

        // Relleno de pared alrededor: media vista (15×8,5) más el adelanto de la cámara (3), con holgura.
        const int Margin = 20;
        // Sin decoración tan cerca de una puerta, para que no tape el paso.
        const int DoorClearance = 2;

        const int SoupTheme = 0;
        const int BossTheme = 1;

        /// <summary>Resultado de montar el piso.</summary>
        public class BuiltFloor
        {
            public readonly List<Room> Rooms = new();
            public Vector2 StartPosition;
            public int MissingCells;
        }

        /// <summary>Una sala del plano ya colocada en el mapa.</summary>
        class PlacedRoom
        {
            public LayoutRoom Layout;
            public Vector2Int Origin;
            public readonly Dictionary<Vector2Int, List<Vector2Int>> DoorCells = new();
            public readonly Dictionary<Vector2Int, List<Vector2Int>> ArchCells = new();

            public RoomTemplate Template => Layout.Template;
            public int Theme => Layout.Role == RoomRole.Boss ? BossTheme : SoupTheme;
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
            var themes = new[] { config.Theme, config.BossTheme != null ? config.BossTheme : config.Theme };
            var placed = Place(layout);
            var byLayout = placed.ToDictionary(room => room.Layout);

            var map = new FloorMap(MapBounds(placed));
            var noDecoration = new HashSet<Vector2Int>();

            foreach (var room in placed)
                Stamp(map, room);
            foreach (var (from, to) in layout.Links)
            {
                var direction = to.Cell - from.Cell;
                OpenDoor(map, byLayout[from], direction);
                OpenDoor(map, byLayout[to], -direction);
                DigCorridor(map, byLayout[from], byLayout[to], direction, noDecoration);
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

            built.MissingCells = Paint(parent, map, themes, noDecoration, random.Next());

            foreach (var room in placed)
            {
                var builtRoom = BuildRoom(parent, room, themes[room.Theme], config, random);
                built.Rooms.Add(builtRoom);

                if (room.Layout.Role == RoomRole.Start)
                    built.StartPosition = room.FloorCenter;
                if (room.Layout.Role == RoomRole.Boss)
                    BuildExit(builtRoom, config);
            }

            return built;
        }

        /// <summary>
        /// Coloca cada sala en su celda alineando su anclaje (el centro de sus puertas) con el de la celda: así
        /// las puertas de salas vecinas quedan enfrentadas aunque tengan tamaños distintos, y el pasillo es recto.
        /// Cada columna mide lo que su sala más ancha y cada fila lo que su sala más alta, más el pasillo mínimo.
        /// </summary>
        static List<PlacedRoom> Place(FloorLayout layout)
        {
            var anchorX = Anchors(layout.Rooms, room => room.Cell.x, room => room.Template.Anchor.x, room => room.Template.Width);
            var anchorY = Anchors(layout.Rooms, room => room.Cell.y, room => room.Template.Anchor.y, room => room.Template.Height);

            return layout.Rooms.Select(room => new PlacedRoom
            {
                Layout = room,
                Origin = new Vector2Int(anchorX[room.Cell.x], anchorY[room.Cell.y]) - room.Template.Anchor
            }).ToList();
        }

        static Dictionary<int, int> Anchors(List<LayoutRoom> rooms, System.Func<LayoutRoom, int> index,
            System.Func<LayoutRoom, int> anchor, System.Func<LayoutRoom, int> size)
        {
            var result = new Dictionary<int, int>();
            int next = 0;
            foreach (var group in rooms.GroupBy(index).OrderBy(g => g.Key))
            {
                int before = group.Max(anchor);
                int after = group.Max(room => size(room) - anchor(room));
                result[group.Key] = next + before;
                next += before + after + CorridorLength;
            }
            return result;
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
                    map.Set(cell, room.Template.Get(x, y) == RoomTemplate.Mark.Floor ? FloorMap.Kind.Floor : FloorMap.Kind.Wall);
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

        /// <summary>Pasillo recto de 2 tiles de ancho entre las puertas de dos salas vecinas.</summary>
        static void DigCorridor(FloorMap map, PlacedRoom from, PlacedRoom to, Vector2Int direction, HashSet<Vector2Int> corridorCells)
        {
            if (!from.DoorCells.TryGetValue(direction, out var start) || !to.DoorCells.TryGetValue(-direction, out var end))
                return;

            foreach (var cell in start)
            {
                // Avanza desde el hueco de la primera sala hasta el de la segunda.
                for (var step = cell + direction; !end.Contains(step) && map.Get(step) == FloorMap.Kind.Wall; step += direction)
                {
                    map.Set(step, FloorMap.Kind.Floor);
                    map.SetTheme(step, SoupTheme);
                    corridorCells.Add(step);
                }
            }
        }

        static int Paint(Transform parent, FloorMap map, RoomTheme[] themes, HashSet<Vector2Int> noDecoration, int seed)
        {
            var grid = new GameObject("Level", typeof(Grid));
            grid.transform.SetParent(parent, false);

            var floor = CreateTilemap(grid.transform, "Ground", -20);
            var decoration = CreateTilemap(grid.transform, "Decoration", -15);
            var walls = CreateTilemap(grid.transform, "Walls", -10);

            var missing = RoomPainter.Paint(map, themes, floor, decoration, walls, noDecoration, seed);
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

        static Room BuildRoom(Transform parent, PlacedRoom placed, RoomTheme theme, FloorConfig config, System.Random random)
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

            // Los enemigos esperan desactivados hasta que el jugador entra.
            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);
            enemiesRoot.gameObject.SetActive(false);
            SpawnEnemies(placed, enemiesRoot, config, random);

            var room = roomObject.AddComponent<Room>();
            room.Configure(enemiesRoot, doors.ToArray());

            if (placed.Layout.Reward && config.RewardPool != null)
                roomObject.AddComponent<MutationReward>().Configure(config.RewardPool, config.RewardOptions);

            return room;
        }

        static void SpawnEnemies(PlacedRoom placed, Transform enemiesRoot, FloorConfig config, System.Random random)
        {
            switch (placed.Layout.Role)
            {
                case RoomRole.Start:
                    return;

                case RoomRole.Boss:
                    if (config.Boss == null)
                        return;
                    var boss = Object.Instantiate(config.Boss, placed.FloorCenter + config.BossOffset, Quaternion.identity, enemiesRoot);
                    // El jefe orbita alrededor del centro de su sala.
                    if (boss.TryGetComponent<BossAI>(out var bossAI))
                        bossAI.SetArenaCenter(enemiesRoot.parent);
                    return;
            }

            var template = placed.Template;
            var floorArea = Enumerable.Range(0, template.Width * template.Height)
                .Count(i => template.Get(i % template.Width, i / template.Width) == RoomTemplate.Mark.Floor);
            int budget = EncounterPlanner.Budget(config, placed.Layout.Depth, floorArea);
            var enemies = EncounterPlanner.PickEnemies(config, placed.Layout.Depth, budget, random);

            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var candidates = EncounterPlanner.SpawnCells(template, doorCells);
            var positions = EncounterPlanner.Positions(candidates, enemies.Count, random);
            if (positions.Count < enemies.Count)
                Debug.LogWarning($"FloorBuilder: en {placed.Layout.Name} solo caben {positions.Count} de {enemies.Count} enemigos.");

            for (int i = 0; i < positions.Count; i++)
                Object.Instantiate(enemies[i], placed.Origin + positions[i], Quaternion.identity, enemiesRoot);
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
            var renderer = doorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = frames != null && frames.Length > 0 ? frames[^1] : null;
            renderer.sortingOrder = -8;
            renderer.flipX = direction == Vector2Int.right;

            var blocker = doorObject.AddComponent<BoxCollider2D>();
            blocker.size = size;

            var door = doorObject.AddComponent<Door>();
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

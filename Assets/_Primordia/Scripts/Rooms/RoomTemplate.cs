using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Forma de una sala escrita como texto, una fila por línea y de arriba abajo:
    /// <c>#</c> pared, <c>.</c> suelo, y los huecos posibles para puertas, que se marcan sobre la pared:
    /// <c>^</c> arriba (en la fila superior; el arco ocupa la cara que hay debajo), <c>v</c> abajo,
    /// <c>&lt;</c> izquierda y <c>&gt;</c> derecha, de 2 tiles cada uno. Los huecos que no se usan quedan como pared.
    /// Obstáculos de cobertura sobre el suelo: <c>o</c> de 1×1 y <c>O</c> de 2×2 (bloques completos). Piezas de las salas
    /// especiales y de las fijas del piso 2: <c>~</c> fosa, <c>F</c> hueso del fósil, <c>S</c> esponja, <c>C</c> almeja,
    /// <c>H</c> coral del corazón morado (2×2), <c>E</c> entrada de oleadas, <c>A</c> columna de algas, <c>L</c> planta
    /// luminosa, <c>K</c> cápsula de Darwin (2×2) y <c>R</c> roca que se derrumba con la muda (2×2).
    /// La pared de arriba debe tener 3 filas (tapa y dos de cara) y las demás 1, como el arte del escenario.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Rooms/Room Template", fileName = "Room_")]
    public class RoomTemplate : ScriptableObject
    {
        public enum Mark
        {
            Wall,
            Floor,
            DoorUp,
            DoorDown,
            DoorLeft,
            DoorRight,
            /// <summary>Obstáculo de 1×1 sobre el suelo: bloquea el paso y los proyectiles.</summary>
            Obstacle,
            /// <summary>Parte de un obstáculo de 2×2.</summary>
            BigObstacle,
            /// <summary>Fosa: grieta del fondo marino (bloques de al menos 2×2).</summary>
            Pit,
            /// <summary>Hueso del esqueleto del Fósil gigante: bloquea.</summary>
            Fossil,
            /// <summary>Sitio de una esponja Vauxia (enemigo fijo).</summary>
            Sponge,
            /// <summary>Almeja con biomasa (cofre): bloquea. Dos seguidas en horizontal, la almeja grande.</summary>
            Chest,
            /// <summary>Coral del corazón morado (2×2): bloquea.</summary>
            HeartCoral,
            /// <summary>Entrada de oleadas: suelo.</summary>
            WaveVent,
            /// <summary>Columna de algas: suelo; tapa la vista sin bloquear.</summary>
            Algae,
            /// <summary>Planta luminosa: suelo, con luz.</summary>
            LightPlant,
            /// <summary>Cápsula del laboratorio de Darwin (2×2).</summary>
            Capsule,
            /// <summary>Roca de 2×2 que se derrumba con la muda del Euriptérido: bloquea hasta entonces.</summary>
            CollapsingRock,
            /// <summary>Suelo que se puede hundir y volverse fosa en el derrumbe de la cueva del Euriptérido (zonas de 3×3).</summary>
            SinkZone
        }

        [Tooltip("Una fila por línea, de arriba abajo. # pared, . suelo, ^ v < > huecos de puerta, o obstáculo 1×1, O obstáculo 2×2, "
            + "~ fosa, F hueso, S esponja, C almeja, H coral del corazón, E entrada de oleadas, A algas, L planta luminosa, K cápsula, "
            + "R roca que se derrumba, Z suelo que se hunde (zonas de 3×3).")]
        [SerializeField, TextArea(16, 40)] string layout;

        Mark[,] cells;
        string parsedLayout;

        public int Width => Parse().GetLength(0);
        public int Height => Parse().GetLength(1);

        /// <summary>Marca de la celda (x hacia la derecha, y hacia arriba, desde la esquina inferior izquierda).</summary>
        public Mark Get(int x, int y)
        {
            var grid = Parse();
            if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1))
                return Mark.Wall;
            return grid[x, y];
        }

        /// <summary>Celdas del hueco de puerta en esa dirección (vacío si la plantilla no tiene).</summary>
        public List<Vector2Int> DoorCells(Vector2Int direction)
        {
            var mark = DoorMark(direction);
            var result = new List<Vector2Int>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Get(x, y) == mark)
                        result.Add(new Vector2Int(x, y));
                }
            }
            return result;
        }

        /// <summary>Rectángulo que ocupa el suelo, en celdas de la plantilla.</summary>
        public RectInt FloorBounds()
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!IsInterior(Get(x, y)))
                        continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            return maxX < minX ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Punto por el que la sala se alinea con sus vecinas, en bordes de celda: x en medio de las puertas de
        /// arriba y abajo, y en medio de las de los lados. Sin puertas en un eje, el centro de la plantilla.
        /// </summary>
        public Vector2Int Anchor
        {
            get
            {
                var vertical = DoorCells(Vector2Int.up);
                if (vertical.Count == 0)
                    vertical = DoorCells(Vector2Int.down);
                var horizontal = DoorCells(Vector2Int.left);
                if (horizontal.Count == 0)
                    horizontal = DoorCells(Vector2Int.right);

                int x = vertical.Count > 0 ? vertical.Min(c => c.x) + 1 : Width / 2;
                int y = horizontal.Count > 0 ? horizontal.Min(c => c.y) + 1 : Height / 2;
                return new Vector2Int(x, y);
            }
        }

        /// <summary>
        /// Problemas de la plantilla: puertas que no son de 2 tiles o no están alineadas, y celdas que el arte
        /// no puede dibujar (paredes de 1 tile, caras sin pared encima…), con todas las puertas abiertas.
        /// </summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var direction in directions)
            {
                var cells = DoorCells(direction);
                if (cells.Count != 0 && cells.Count != 2)
                    problems.Add($"La puerta {DoorMark(direction)} tiene {cells.Count} celdas (deben ser 2).");
            }

            var up = DoorCells(Vector2Int.up);
            var down = DoorCells(Vector2Int.down);
            if (up.Count > 0 && down.Count > 0 && up.Min(c => c.x) != down.Min(c => c.x))
                problems.Add("Las puertas de arriba y abajo no están en las mismas columnas.");
            var left = DoorCells(Vector2Int.left);
            var right = DoorCells(Vector2Int.right);
            if (left.Count > 0 && right.Count > 0 && left.Min(c => c.y) != right.Min(c => c.y))
                problems.Add("Las puertas de los lados no están en las mismas filas.");

            var map = ToFloorMap(Vector2Int.zero, openDoors: true);
            foreach (var cell in map.Bounds.allPositionsWithin)
            {
                if (WallRules.Classify(map, cell) == Piece.Missing)
                    problems.Add($"La celda ({cell.x}, {cell.y}) no se puede dibujar.");
            }

            return problems;
        }

        /// <summary>
        /// La plantilla como mapa, en <paramref name="origin"/>. Con <paramref name="openDoors"/>, todas las puertas
        /// abiertas (arriba: paso en la fila superior y arco en la cara de debajo); si no, son pared.
        /// </summary>
        public FloorMap ToFloorMap(Vector2Int origin, bool openDoors)
        {
            var map = new FloorMap(new RectInt(origin, new Vector2Int(Width, Height)));
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var mark = Get(x, y);
                    var kind = mark == Mark.Floor ? FloorMap.Kind.Floor
                        : IsObstacle(mark) ? FloorMap.Kind.Obstacle
                        : mark == Mark.Pit ? FloorMap.Kind.Pit
                        : IsInterior(mark) ? FloorMap.Kind.Floor
                        : !openDoors || mark == Mark.Wall ? FloorMap.Kind.Wall
                        : mark == Mark.DoorUp ? FloorMap.Kind.Passage
                        : FloorMap.Kind.DoorGap;
                    map.Set(origin + new Vector2Int(x, y), kind);
                }
            }

            if (openDoors)
            {
                foreach (var cell in DoorCells(Vector2Int.up))
                {
                    for (int y = cell.y - 1; y >= 0 && Get(cell.x, y) == Mark.Wall; y--)
                        map.Set(origin + new Vector2Int(cell.x, y), FloorMap.Kind.DoorArch);
                }
            }

            return map;
        }

        /// <summary>
        /// Lo que bloquea el paso y los proyectiles: obstáculos, huesos, almejas, el coral del corazón, las rocas que se
        /// derrumban y la cápsula de la sala de cambio.
        /// </summary>
        public static bool IsObstacle(Mark mark) =>
            mark is Mark.Obstacle or Mark.BigObstacle or Mark.Fossil or Mark.Chest or Mark.HeartCoral or Mark.CollapsingRock
                or Mark.Capsule;

        /// <summary>Lo que está dentro de la sala: todo menos la pared y los huecos de puerta.</summary>
        public static bool IsInterior(Mark mark) =>
            mark is not (Mark.Wall or Mark.DoorUp or Mark.DoorDown or Mark.DoorLeft or Mark.DoorRight);

        /// <summary>Esquinas de abajo a la izquierda de los obstáculos de 2×2.</summary>
        public List<Vector2Int> BigObstacleCorners() => BlockCorners(Mark.BigObstacle);

        /// <summary>Celdas con esa marca.</summary>
        public List<Vector2Int> CellsOf(Mark mark)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (Get(x, y) == mark)
                        result.Add(new Vector2Int(x, y));
            return result;
        }

        /// <summary>Zonas de suelo que se pueden hundir (marca Z): cada grupo de celdas Z que se tocan, como rectángulo.</summary>
        public List<RectInt> SinkZones()
        {
            var result = new List<RectInt>();
            var seen = new HashSet<Vector2Int>();
            foreach (var start in CellsOf(Mark.SinkZone))
            {
                if (!seen.Add(start))
                    continue;
                int xMin = start.x, yMin = start.y, xMax = start.x, yMax = start.y;
                var pending = new Stack<Vector2Int>();
                pending.Push(start);
                while (pending.Count > 0)
                {
                    var cell = pending.Pop();
                    xMin = Mathf.Min(xMin, cell.x);
                    yMin = Mathf.Min(yMin, cell.y);
                    xMax = Mathf.Max(xMax, cell.x);
                    yMax = Mathf.Max(yMax, cell.y);
                    foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        var next = cell + step;
                        if (Get(next.x, next.y) == Mark.SinkZone && seen.Add(next))
                            pending.Push(next);
                    }
                }
                result.Add(new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1));
            }
            return result;
        }

        /// <summary>Esquinas de abajo a la izquierda de los bloques de 2×2 con esa marca.</summary>
        public List<Vector2Int> BlockCorners(Mark mark)
        {
            var result = new List<Vector2Int>();
            var taken = new HashSet<Vector2Int>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (taken.Contains(cell) || Get(x, y) != mark || Get(x + 1, y) != mark
                        || Get(x, y + 1) != mark || Get(x + 1, y + 1) != mark)
                        continue;
                    result.Add(cell);
                    taken.Add(cell);
                    taken.Add(cell + Vector2Int.right);
                    taken.Add(cell + Vector2Int.up);
                    taken.Add(cell + Vector2Int.one);
                }
            }
            return result;
        }

        public static Mark DoorMark(Vector2Int direction)
        {
            if (direction == Vector2Int.up)
                return Mark.DoorUp;
            if (direction == Vector2Int.down)
                return Mark.DoorDown;
            if (direction == Vector2Int.left)
                return Mark.DoorLeft;
            if (direction == Vector2Int.right)
                return Mark.DoorRight;
            throw new ArgumentException($"Dirección de puerta no válida: {direction}");
        }

        internal void SetLayout(string newLayout)
        {
            layout = newLayout;
            cells = null;
        }

        Mark[,] Parse()
        {
            if (cells != null && parsedLayout == layout)
                return cells;

            var lines = (layout ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int width = 0;
            foreach (var line in lines)
                width = Mathf.Max(width, line.TrimEnd().Length);

            cells = new Mark[width, lines.Length];
            for (int row = 0; row < lines.Length; row++)
            {
                // La primera línea es la fila de arriba.
                int y = lines.Length - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    char c = x < lines[row].Length ? lines[row][x] : '#';
                    cells[x, y] = c switch
                    {
                        '.' => Mark.Floor,
                        '^' => Mark.DoorUp,
                        'v' => Mark.DoorDown,
                        '<' => Mark.DoorLeft,
                        '>' => Mark.DoorRight,
                        'o' => Mark.Obstacle,
                        'O' => Mark.BigObstacle,
                        '~' => Mark.Pit,
                        'F' => Mark.Fossil,
                        'S' => Mark.Sponge,
                        'C' => Mark.Chest,
                        'H' => Mark.HeartCoral,
                        'E' => Mark.WaveVent,
                        'A' => Mark.Algae,
                        'L' => Mark.LightPlant,
                        'K' => Mark.Capsule,
                        'R' => Mark.CollapsingRock,
                        'Z' => Mark.SinkZone,
                        _ => Mark.Wall
                    };
                }
            }

            parsedLayout = layout;
            return cells;
        }
    }
}

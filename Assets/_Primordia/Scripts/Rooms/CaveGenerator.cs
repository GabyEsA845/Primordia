using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Forma de cueva para una sala, sacada de la semilla del piso con el método de la sala de paso: una mancha orgánica
    /// con ruido dentro del rectángulo de la plantilla, los cuellos de las cuatro puertas (que llegan hasta el centro, así
    /// todas quedan unidas) y una reparación automática de las celdas que el arte no puede dibujar. La roca que queda
    /// suelta dentro de la mancha sirve de cobertura, y además se reparten obstáculos de 1×1 (<c>o</c>) y de 2×2 (<c>O</c>)
    /// fuera de los cuellos, cada uno con un anillo de suelo libre alrededor para que nunca cierren el paso. Si tras varios
    /// intentos ninguna forma pasa la comprobación, devuelve null y el generador del piso usa una del catálogo hecho a mano.
    /// </summary>
    public static class CaveGenerator
    {
        /// <summary>Tamaño mínimo de una cueva: lo que hace falta para la pared de arriba, los cuellos y algo de sala.</summary>
        public static readonly Vector2Int MinSize = new(16, 13);

        // Pared de arriba (tapa y dos filas de cara) y de los demás lados, como en todas las plantillas.
        const int TopWall = 3;
        const int SideWall = 1;

        // Medio ancho de los cuellos: el hueco de 2 tiles más 2 de roca apartada a cada lado (cuellos de 6 tiles).
        const int NeckHalf = 3;

        // Cuánto entra la roca desde el borde de la mancha, en fracción del radio, y cuántas ondas tiene el contorno.
        const float NoiseDepth = 0.5f;
        const int MinWaves = 2;
        const int MaxWaves = 8;

        // Lo que tiene que quedar de suelo, en fracción del interior (sin las paredes del borde).
        const float MinFill = 0.5f;

        // Obstáculos de cobertura: uno grande por cada tantos tiles de suelo, y uno pequeño por cada tantos.
        const int FloorPerBigObstacle = 170;
        const int FloorPerSmallObstacle = 120;
        const int CoverTries = 60;

        const int Attempts = 8;
        const int RepairPasses = 60;

        /// <summary>
        /// Forma de cueva de <paramref name="size"/> tiles (paredes incluidas) como texto para <see cref="RoomTemplate"/>,
        /// con un hueco de puerta en cada lado. Null si ningún intento pasa la comprobación.
        /// </summary>
        /// <param name="cover">Con obstáculos de cobertura dentro.</param>
        public static string Generate(Vector2Int size, System.Random random, bool cover = true)
        {
            size = Vector2Int.Max(size, MinSize);
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                var layout = TryGenerate(size, random, cover);
                if (layout != null)
                    return layout;
            }
            return null;
        }

        /// <summary>
        /// Problemas de una forma de cueva: los de <see cref="RoomTemplate.Validate"/>, suelo que no se alcanza desde las
        /// puertas, puertas sin salida a la sala y poco suelo. Vacío si vale.
        /// </summary>
        public static List<string> Check(string layout)
        {
            var problems = new List<string>();
            var lines = (layout ?? "").Replace("\r", "").Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
            int height = lines.Length, width = height > 0 ? lines[0].Length : 0;
            if (width < MinSize.x || height < MinSize.y)
            {
                problems.Add($"Cueva demasiado pequeña: {width}×{height}.");
                return problems;
            }

            // Con y hacia arriba, como RoomTemplate.
            var floor = new bool[width, height];
            var cover = new char[width, height];
            var doors = new Dictionary<char, List<Vector2Int>> { ['^'] = new(), ['v'] = new(), ['<'] = new(), ['>'] = new() };
            for (int row = 0; row < height; row++)
            {
                if (lines[row].Length != width)
                    problems.Add($"La fila {row} no mide {width}.");
                for (int x = 0; x < width; x++)
                {
                    char c = x < lines[row].Length ? lines[row][x] : '#';
                    int y = height - 1 - row;
                    floor[x, y] = c is '.' or 'o' or 'O';
                    if (c is 'o' or 'O')
                        cover[x, y] = c;
                    if (doors.TryGetValue(c, out var cells))
                        cells.Add(new Vector2Int(x, y));
                }
            }

            foreach (var (mark, cells) in doors)
            {
                if (cells.Count != 2)
                    problems.Add($"La puerta {mark} tiene {cells.Count} celdas (deben ser 2).");
            }
            if (problems.Count > 0)
                return problems;

            int doorX = Mathf.Min(doors['^'][0].x, doors['^'][1].x), doorY = Mathf.Min(doors['<'][0].y, doors['<'][1].y);
            if (Mathf.Min(doors['v'][0].x, doors['v'][1].x) != doorX || doors['^'][0].y != height - 1 || doors['v'][0].y != 0)
                problems.Add("Las puertas de arriba y abajo no están en las mismas columnas.");
            if (Mathf.Min(doors['>'][0].y, doors['>'][1].y) != doorY || doors['<'][0].x != 0 || doors['>'][0].x != width - 1)
                problems.Add("Las puertas de los lados no están en las mismas filas.");
            if (problems.Count > 0)
                return problems;

            var map = ToMap(floor, cover, width, height, doorX, doorY);
            foreach (var cell in map.Bounds.allPositionsWithin)
            {
                if (WallRules.Classify(map, cell) == Piece.Missing)
                    problems.Add($"La celda ({cell.x}, {cell.y}) no se puede dibujar.");
            }

            // Los obstáculos grandes, en bloques completos de 2×2.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (cover[x, y] != 'O')
                        continue;
                    bool inBlock = false;
                    for (int dy = -1; dy <= 0 && !inBlock; dy++)
                        for (int dx = -1; dx <= 0 && !inBlock; dx++)
                            inBlock = IsBigBlock(cover, x + dx, y + dy);
                    if (!inBlock)
                        problems.Add($"El obstáculo grande en ({x}, {y}) no forma un bloque de 2×2.");
                }
            }

            // Lo que se pisa: el suelo sin obstáculo.
            var walk = new bool[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    walk[x, y] = floor[x, y] && cover[x, y] == 0;

            var inner = new List<Vector2Int>();
            foreach (var (mark, cells) in doors)
            {
                var inward = mark == '^' ? Vector2Int.down : mark == 'v' ? Vector2Int.up : mark == '<' ? Vector2Int.right : Vector2Int.left;
                var cell = InnerCell(walk, cells[0], inward);
                if (cell == null)
                    problems.Add($"La puerta {mark} no da a ningún suelo.");
                else
                    inner.Add(cell.Value);
            }

            if (inner.Count > 0)
            {
                var reached = Reach(walk, inner[0]);
                int total = 0, lost = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!walk[x, y])
                            continue;
                        total++;
                        if (!reached[x, y])
                            lost++;
                    }
                }
                if (lost > 0)
                    problems.Add($"{lost} tiles de suelo no se alcanzan desde las puertas.");
                foreach (var cell in inner)
                {
                    if (!reached[cell.x, cell.y])
                        problems.Add($"La puerta junto a ({cell.x}, {cell.y}) no está unida a las demás.");
                }
                int interior = (width - 2 * SideWall) * (height - TopWall - SideWall);
                if (total < interior * MinFill)
                    problems.Add($"Poco suelo: {total} de {interior} tiles.");
            }

            return problems;
        }

        static string TryGenerate(Vector2Int size, System.Random random, bool withCover)
        {
            int width = size.x, height = size.y;
            // Interior: lo que no es pared del borde (x de 1 a width - 2, y de 1 a height - 4, con y hacia arriba).
            int minX = SideWall, maxX = width - 1 - SideWall;
            int minY = SideWall, maxY = height - 1 - TopWall;

            // Puertas: arriba y abajo en las mismas columnas, a los lados en las mismas filas, cerca del centro.
            int doorX = Mathf.Clamp(width / 2 - 1 + random.Next(-2, 3), minX + NeckHalf, maxX - NeckHalf);
            int doorY = Mathf.Clamp((minY + maxY) / 2 + random.Next(-1, 2), minY + NeckHalf, maxY - NeckHalf);

            var floor = new bool[width, height];
            var neck = new bool[width, height];
            Blob(floor, minX, maxX, minY, maxY, random);
            Smooth(floor, minX, maxX, minY, maxY);
            Necks(floor, neck, doorX, doorY, minX, maxX, minY, maxY);

            if (!Repair(floor, neck, width, height, doorX, doorY, minX, maxX, minY, maxY))
                return null;

            var cover = new char[width, height];
            if (withCover)
                PlaceCover(floor, neck, cover, minX, maxX, minY, maxY, random);

            var layout = ToLayout(floor, cover, width, height, doorX, doorY);
            return Check(layout).Count == 0 ? layout : null;
        }

        /// <summary>
        /// Mancha orgánica: una elipse que llena el interior con el radio deformado por unas cuantas ondas de fase al azar,
        /// así el borde entra y sale como el de una cueva.
        /// </summary>
        static void Blob(bool[,] floor, int minX, int maxX, int minY, int maxY, System.Random random)
        {
            float cx = (minX + maxX + 1) / 2f, cy = (minY + maxY + 1) / 2f;
            float rx = (maxX - minX + 1) / 2f, ry = (maxY - minY + 1) / 2f;

            int waveCount = 4;
            var waves = new (int k, float amplitude, float phase)[waveCount];
            float total = 0f;
            for (int i = 0; i < waveCount; i++)
            {
                float amplitude = 0.4f + (float)random.NextDouble() * 0.6f;
                waves[i] = (random.Next(MinWaves, MaxWaves + 1), amplitude, (float)(random.NextDouble() * Mathf.PI * 2f));
                total += amplitude;
            }
            // Las esquinas del rectángulo se aprovechan un poco: la elipse se infla y el ruido la recorta.
            float inflate = 1.2f + (float)random.NextDouble() * 0.12f;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
                    float angle = Mathf.Atan2(ny, nx);
                    float noise = 0f;
                    foreach (var (k, amplitude, phase) in waves)
                        noise += amplitude * Mathf.Sin(k * angle + phase);
                    // Entre 0 (sin roca) y 1 (la roca entra NoiseDepth del radio).
                    float rock = (noise / total + 1f) / 2f;
                    float radius = inflate * (1f - NoiseDepth * rock);
                    floor[x, y] = nx * nx + ny * ny <= radius * radius;
                }
            }
        }

        /// <summary>Dos pasadas de mayoría entre los 8 vecinos: quita dientes de 1 tile y redondea el contorno.</summary>
        static void Smooth(bool[,] floor, int minX, int maxX, int minY, int maxY)
        {
            int width = floor.GetLength(0), height = floor.GetLength(1);
            for (int pass = 0; pass < 2; pass++)
            {
                var copy = (bool[,])floor.Clone();
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        int open = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if ((dx != 0 || dy != 0) && x + dx >= 0 && y + dy >= 0 && x + dx < width && y + dy < height && copy[x + dx, y + dy])
                                    open++;
                            }
                        }
                        if (open >= 5)
                            floor[x, y] = true;
                        else if (open <= 3)
                            floor[x, y] = false;
                    }
                }
            }
        }

        /// <summary>
        /// Cuellos de las puertas: un canal de 6 tiles desde cada hueco hasta el centro de la mancha. Sus celdas no se
        /// cierran nunca al reparar, así las cuatro puertas siempre quedan unidas.
        /// </summary>
        static void Necks(bool[,] floor, bool[,] neck, int doorX, int doorY, int minX, int maxX, int minY, int maxY)
        {
            int cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
            void Carve(int x0, int x1, int y0, int y1)
            {
                for (int y = Mathf.Max(minY, Mathf.Min(y0, y1)); y <= Mathf.Min(maxY, Mathf.Max(y0, y1)); y++)
                {
                    for (int x = Mathf.Max(minX, Mathf.Min(x0, x1)); x <= Mathf.Min(maxX, Mathf.Max(x0, x1)); x++)
                    {
                        floor[x, y] = true;
                        neck[x, y] = true;
                    }
                }
            }

            // El hueco de arriba y abajo ocupa las columnas doorX y doorX + 1; el de los lados, las filas doorY y doorY + 1.
            int left = doorX + 1 - NeckHalf, right = doorX + NeckHalf;
            int bottom = doorY + 1 - NeckHalf, top = doorY + NeckHalf;
            Carve(left, right, maxY, Mathf.Min(cy, doorY));
            Carve(left, right, minY, Mathf.Max(cy, doorY + 1));
            Carve(minX, Mathf.Max(cx, doorX + 1), bottom, top);
            Carve(Mathf.Min(cx, doorX), maxX, bottom, top);
        }

        /// <summary>
        /// Arregla la forma hasta que el arte pueda dibujarla: la pared que no se puede dibujar (de 1 tile de grosor, una
        /// cara con suelo encima…) pasa a ser suelo, y el suelo que ha quedado aislado de las puertas pasa a ser roca.
        /// False si no se estabiliza.
        /// </summary>
        static bool Repair(bool[,] floor, bool[,] neck, int width, int height, int doorX, int doorY, int minX, int maxX, int minY, int maxY)
        {
            for (int pass = 0; pass < RepairPasses; pass++)
            {
                bool changed = false;
                var map = ToMap(floor, null, width, height, doorX, doorY);
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!floor[x, y] && WallRules.Classify(map, new Vector2Int(x, y)) == Piece.Missing)
                        {
                            floor[x, y] = true;
                            changed = true;
                        }
                    }
                }

                // Suelo que no se alcanza desde el centro de los cuellos (que siempre está abierto).
                var reached = Reach(floor, new Vector2Int(doorX, doorY));
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (floor[x, y] && !reached[x, y] && !neck[x, y])
                        {
                            floor[x, y] = false;
                            changed = true;
                        }
                    }
                }

                if (!changed)
                    return true;
            }
            return false;
        }

        /// <summary>La forma como mapa con las cuatro puertas abiertas, como lo comprueba <see cref="RoomTemplate.Validate"/>.</summary>
        static FloorMap ToMap(bool[,] floor, char[,] cover, int width, int height, int doorX, int doorY)
        {
            var map = new FloorMap(new RectInt(0, 0, width, height));
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map.Set(new Vector2Int(x, y), !floor[x, y] ? FloorMap.Kind.Wall
                        : cover != null && cover[x, y] != 0 ? FloorMap.Kind.Obstacle : FloorMap.Kind.Floor);

            for (int i = 0; i < 2; i++)
            {
                map.Set(new Vector2Int(doorX + i, height - 1), FloorMap.Kind.Passage);
                for (int y = height - 2; y >= 0 && !floor[doorX + i, y]; y--)
                    map.Set(new Vector2Int(doorX + i, y), FloorMap.Kind.DoorArch);
                map.Set(new Vector2Int(doorX + i, 0), FloorMap.Kind.DoorGap);
                map.Set(new Vector2Int(0, doorY + i), FloorMap.Kind.DoorGap);
                map.Set(new Vector2Int(width - 1, doorY + i), FloorMap.Kind.DoorGap);
            }
            return map;
        }

        /// <summary>
        /// Obstáculos de cobertura: cada uno en un hueco con todo suelo alrededor (un anillo de 1 tile libre, sin tocar otros
        /// obstáculos ni paredes) y nunca sobre los cuellos, así no cortan el paso entre las puertas ni dejan suelo encerrado.
        /// </summary>
        static void PlaceCover(bool[,] floor, bool[,] neck, char[,] cover, int minX, int maxX, int minY, int maxY, System.Random random)
        {
            int area = 0;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (floor[x, y])
                        area++;

            int big = Mathf.RoundToInt(area / (float)FloorPerBigObstacle * (0.7f + (float)random.NextDouble() * 0.6f));
            int small = Mathf.RoundToInt(area / (float)FloorPerSmallObstacle * (0.7f + (float)random.NextDouble() * 0.6f));
            Scatter(2, 'O', big);
            Scatter(1, 'o', small);

            void Scatter(int size, char mark, int count)
            {
                for (int placed = 0, tries = 0; placed < count && tries < CoverTries * Mathf.Max(1, count); tries++)
                {
                    int x = random.Next(minX + 1, maxX - size + 1), y = random.Next(minY + 1, maxY - size + 1);
                    if (!Fits(x, y, size))
                        continue;
                    for (int dy = 0; dy < size; dy++)
                        for (int dx = 0; dx < size; dx++)
                            cover[x + dx, y + dy] = mark;
                    placed++;
                }
            }

            // Suelo libre de 1 tile alrededor, y al menos 2 tiles entre dos obstáculos.
            bool Fits(int x, int y, int size)
            {
                for (int dy = -2; dy <= size + 1; dy++)
                {
                    for (int dx = -2; dx <= size + 1; dx++)
                    {
                        int cx = x + dx, cy = y + dy;
                        bool ring = dx >= -1 && dy >= -1 && dx <= size && dy <= size;
                        bool outside = cx < minX || cy < minY || cx > maxX || cy > maxY;
                        if (ring && (outside || !floor[cx, cy]))
                            return false;
                        if (!outside && cover[cx, cy] != 0)
                            return false;
                        bool inside = dx >= 0 && dy >= 0 && dx < size && dy < size;
                        if (inside && neck[cx, cy])
                            return false;
                    }
                }
                return true;
            }
        }

        static bool IsBigBlock(char[,] cover, int x, int y)
        {
            int width = cover.GetLength(0), height = cover.GetLength(1);
            if (x < 0 || y < 0 || x + 1 >= width || y + 1 >= height)
                return false;
            return cover[x, y] == 'O' && cover[x + 1, y] == 'O' && cover[x, y + 1] == 'O' && cover[x + 1, y + 1] == 'O';
        }

        static string ToLayout(bool[,] floor, char[,] cover, int width, int height, int doorX, int doorY)
        {
            var text = new StringBuilder();
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    bool doorColumn = x == doorX || x == doorX + 1;
                    bool doorRow = y == doorY || y == doorY + 1;
                    char c = floor[x, y] ? (cover[x, y] != 0 ? cover[x, y] : '.')
                        : y == height - 1 && doorColumn ? '^'
                        : y == 0 && doorColumn ? 'v'
                        : x == 0 && doorRow ? '<'
                        : x == width - 1 && doorRow ? '>'
                        : '#';
                    text.Append(c);
                }
                text.Append('\n');
            }
            return text.ToString();
        }

        /// <summary>Primer suelo al entrar por un hueco de puerta, avanzando hacia dentro. Null si no hay.</summary>
        static Vector2Int? InnerCell(bool[,] floor, Vector2Int door, Vector2Int inward)
        {
            for (var cell = door + inward; cell.x >= 0 && cell.y >= 0 && cell.x < floor.GetLength(0) && cell.y < floor.GetLength(1); cell += inward)
            {
                if (floor[cell.x, cell.y])
                    return cell;
            }
            return null;
        }

        static bool[,] Reach(bool[,] floor, Vector2Int start)
        {
            int width = floor.GetLength(0), height = floor.GetLength(1);
            var seen = new bool[width, height];
            if (start.x < 0 || start.y < 0 || start.x >= width || start.y >= height || !floor[start.x, start.y])
                return seen;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || seen[next.x, next.y] || !floor[next.x, next.y])
                        continue;
                    seen[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
            return seen;
        }
    }
}

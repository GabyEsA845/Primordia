using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Forma de una sala escrita como texto, una fila por línea y de arriba abajo:
    /// <c>#</c> pared, <c>.</c> suelo, y los huecos posibles para puertas, que se marcan sobre la pared:
    /// <c>^</c> arriba (en la fila superior; el arco ocupa la cara que hay debajo), <c>v</c> abajo,
    /// <c>&lt;</c> izquierda y <c>&gt;</c> derecha, de 2 tiles cada uno. Los huecos que no se usan quedan como pared.
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
            DoorRight
        }

        [Tooltip("Una fila por línea, de arriba abajo. # pared, . suelo, ^ v < > huecos de puerta.")]
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
                    if (Get(x, y) != Mark.Floor)
                        continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            return maxX < minX ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
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
                        _ => Mark.Wall
                    };
                }
            }

            parsedLayout = layout;
            return cells;
        }
    }
}

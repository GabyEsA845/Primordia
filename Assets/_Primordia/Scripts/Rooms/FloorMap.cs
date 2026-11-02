using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Qué hay en cada celda del piso entero (salas y pasillos), antes de elegir tiles.
    /// Fuera de los límites todo es pared.
    /// </summary>
    public class FloorMap
    {
        public enum Kind
        {
            /// <summary>Pared o relleno de fuera de las salas.</summary>
            Wall,

            Floor,

            /// <summary>Paso de una puerta de arriba en la fila superior de la pared: se dibuja como suelo.</summary>
            Passage,

            /// <summary>Hueco de puerta lateral o inferior: suelo, pero para los vecinos cuenta como pared.</summary>
            DoorGap,

            /// <summary>Arco de puerta de arriba, sobre la cara de la pared: sin tile, lo dibuja el arco.</summary>
            DoorArch,

            /// <summary>Suelo con un obstáculo de cobertura encima: para las paredes cuenta como suelo, pero no se pisa.</summary>
            Obstacle,

            /// <summary>Fosa: para las paredes cuenta como suelo; los enemigos que andan la rodean.</summary>
            Pit,

            /// <summary>Lodo profundo del piso 3: para las paredes cuenta como suelo. Hasta que tenga su efecto se pisa como suelo.</summary>
            Mud
        }

        readonly Kind[,] kinds;
        readonly int[,] themes;

        public RectInt Bounds { get; }

        public FloorMap(RectInt bounds)
        {
            Bounds = bounds;
            kinds = new Kind[bounds.width, bounds.height];
            themes = new int[bounds.width, bounds.height];
        }

        public Kind Get(Vector2Int cell) => Get(cell.x, cell.y);

        public Kind Get(int x, int y) => Contains(x, y) ? kinds[x - Bounds.xMin, y - Bounds.yMin] : Kind.Wall;

        public void Set(Vector2Int cell, Kind kind)
        {
            if (Contains(cell.x, cell.y))
                kinds[cell.x - Bounds.xMin, cell.y - Bounds.yMin] = kind;
        }

        /// <summary>Índice del tema (sopa, jefe…) con el que se dibuja la celda.</summary>
        public int GetTheme(Vector2Int cell) =>
            Contains(cell.x, cell.y) ? themes[cell.x - Bounds.xMin, cell.y - Bounds.yMin] : 0;

        public void SetTheme(Vector2Int cell, int theme)
        {
            if (Contains(cell.x, cell.y))
                themes[cell.x - Bounds.xMin, cell.y - Bounds.yMin] = theme;
        }

        /// <summary>Suelo (también bajo un obstáculo) que deja ver los bordes de las paredes vecinas.</summary>
        public bool IsOpen(int x, int y)
        {
            var kind = Get(x, y);
            return kind == Kind.Floor || kind == Kind.Passage || kind == Kind.Obstacle || kind == Kind.Pit || kind == Kind.Mud;
        }

        bool Contains(int x, int y) =>
            x >= Bounds.xMin && x < Bounds.xMax && y >= Bounds.yMin && y < Bounds.yMax;
    }
}

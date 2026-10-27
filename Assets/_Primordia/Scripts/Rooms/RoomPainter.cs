using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Rooms
{
    /// <summary>
    /// Vuelca un <see cref="FloorMap"/> en tres Tilemaps (suelo, decoración y paredes) eligiendo cada tile
    /// con <see cref="WallRules"/>. Sirve igual en el editor que en tiempo de juego (para el mapa aleatorio).
    /// </summary>
    public static class RoomPainter
    {
        /// <summary>Celdas que no se pueden dibujar con el arte del tema (pieza que falta o forma imposible).</summary>
        /// <param name="overlay">Capa de las cáusticas, sobre el suelo de los temas que las tienen. Null: sin cáusticas.</param>
        /// <param name="placedDecorations">Si no es null, recibe cada decoración puesta con su celda (para sus luces).</param>
        public static List<Vector2Int> Paint(FloorMap map, IReadOnlyList<RoomTheme> themes,
            Tilemap floor, Tilemap decoration, Tilemap walls, ISet<Vector2Int> decorationFree = null, int seed = 0,
            Tilemap overlay = null, List<(Vector2Int cell, TileBase tile)> placedDecorations = null)
        {
            var missing = new List<Vector2Int>();
            var random = new System.Random(seed);

            foreach (var position in map.Bounds.allPositionsWithin)
            {
                var theme = themes[Mathf.Clamp(map.GetTheme(position), 0, themes.Count - 1)];
                var piece = WallRules.Classify(map, position);
                var tile = theme.Get(piece, CellHash(position));
                var cell = new Vector3Int(position.x, position.y, 0);

                // Fosa: el tile según qué vecinos no son fosa y, encima, las esquinas de dentro o unas motas que suben.
                if (map.Get(position) == FloorMap.Kind.Pit && theme.HasPits)
                {
                    floor.SetTile(cell, theme.PitTile(PitMask(map, position)));
                    var corner = PitCorner(map, position, theme);
                    if (corner != null)
                        decoration.SetTile(cell, corner);
                    else if (theme.PitMotes != null && random.NextDouble() < theme.PitMotesChance)
                        decoration.SetTile(cell, theme.PitMotes);
                    continue;
                }

                if (piece == Piece.Missing || (piece != Piece.None && (tile == null || tile == theme.MissingTile)))
                    missing.Add(position);

                if (WallRules.IsWall(piece))
                {
                    walls.SetTile(cell, tile);
                    continue;
                }

                if (piece == Piece.None)
                    continue;

                floor.SetTile(cell, tile);

                // Cáusticas en mosaico de 4×4 tiles sobre todo el suelo del tema (también bajo sombras y obstáculos).
                if (overlay != null && theme.HasCaustics)
                    overlay.SetTile(cell, theme.Caustics[Mod(position.x, 4) + 4 * Mod(position.y, 4)]);

                // Decoración solo en suelo de las salas, sin sombra y lejos de las puertas.
                bool free = piece == Piece.Floor && map.Get(position) == FloorMap.Kind.Floor
                    && (decorationFree == null || !decorationFree.Contains(position));
                if (free && theme.Decorations != null && theme.Decorations.Length > 0
                    && random.NextDouble() < theme.DecorationChance)
                {
                    var decorationTile = theme.Decorations[random.Next(theme.Decorations.Length)];
                    decoration.SetTile(cell, decorationTile);
                    placedDecorations?.Add((position, decorationTile));
                }
            }

            return missing;
        }

        /// <summary>
        /// Vuelve a dibujar las fosas de esas celdas y de sus vecinas (el borde de las que ya había cambia) cuando el suelo se
        /// hunde mientras se juega. Quita la decoración que quedaba encima.
        /// </summary>
        public static void RepaintPits(FloorMap map, RoomTheme theme, Tilemap floor, Tilemap decoration, IEnumerable<Vector2Int> cells)
        {
            if (map == null || theme == null || !theme.HasPits || floor == null)
                return;
            var touched = new HashSet<Vector2Int>();
            foreach (var cell in cells)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        touched.Add(cell + new Vector2Int(dx, dy));
            foreach (var position in touched)
            {
                if (map.Get(position) != FloorMap.Kind.Pit)
                    continue;
                var cell = new Vector3Int(position.x, position.y, 0);
                floor.SetTile(cell, theme.PitTile(PitMask(map, position)));
                if (decoration != null)
                    decoration.SetTile(cell, PitCorner(map, position, theme));
            }
        }

        static int Mod(int value, int divisor) => ((value % divisor) + divisor) % divisor;

        static bool IsPit(FloorMap map, int x, int y) => map.Get(x, y) == FloorMap.Kind.Pit;

        /// <summary>Máscara del tile de fosa: suma de los vecinos que no son fosa (N=1, E=2, S=4, O=8).</summary>
        internal static int PitMask(FloorMap map, Vector2Int cell)
        {
            int x = cell.x, y = cell.y;
            return (IsPit(map, x, y + 1) ? 0 : 1) | (IsPit(map, x + 1, y) ? 0 : 2) | (IsPit(map, x, y - 1) ? 0 : 4) | (IsPit(map, x - 1, y) ? 0 : 8);
        }

        // Esquina de dentro: los dos vecinos de ese lado son fosa y la diagonal no. Orden del tema: NE, NO, SE, SO.
        static TileBase PitCorner(FloorMap map, Vector2Int cell, RoomTheme theme)
        {
            int x = cell.x, y = cell.y;
            var corners = new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) };
            for (int i = 0; i < corners.Length; i++)
            {
                var (dx, dy) = corners[i];
                if (IsPit(map, x + dx, y) && IsPit(map, x, y + dy) && !IsPit(map, x + dx, y + dy))
                    return theme.PitCorner(i);
            }
            return null;
        }

        // Variante estable por celda: el mismo piso se dibuja siempre igual.
        static int CellHash(Vector2Int cell)
        {
            unchecked
            {
                int hash = cell.x * 73856093 ^ cell.y * 19349663;
                hash ^= hash >> 13;
                return (hash * 1274126177) & int.MaxValue;
            }
        }
    }
}

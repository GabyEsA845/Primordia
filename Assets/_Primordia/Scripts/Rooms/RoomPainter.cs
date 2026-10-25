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

        static int Mod(int value, int divisor) => ((value % divisor) + divisor) % divisor;

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

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
        static readonly Color ProvisionalMud = new(0.38f, 0.42f, 0.22f, 1f);

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
                // La cara de abajo elige variante con la celda de arriba para hacer pareja con la cara de arriba.
                var tile = theme.Get(piece, CellHash(piece == Piece.FaceLower ? position + Vector2Int.up : position));
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

                // Lodo profundo: igual que la fosa, con sus tiles de orilla, sus esquinas y burbujas que revientan.
                if (map.Get(position) == FloorMap.Kind.Mud && theme.HasMud)
                {
                    floor.SetTile(cell, theme.MudTile(KindMask(map, position, FloorMap.Kind.Mud)));
                    var mudCorner = Corner(map, position, FloorMap.Kind.Mud, theme.MudCorner);
                    if (mudCorner != null)
                        decoration.SetTile(cell, mudCorner);
                    else if (theme.MudBubbles != null && random.NextDouble() < theme.MudBubblesChance)
                        decoration.SetTile(cell, theme.MudBubbles);
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

                // Lodo profundo: hasta que tenga sus tiles (subtanda 3), el suelo oscurecido y verdoso para ver su forma.
                if (map.Get(position) == FloorMap.Kind.Mud)
                {
                    floor.SetTileFlags(cell, TileFlags.None);
                    floor.SetColor(cell, ProvisionalMud);
                }

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

            PaintBlends(map, themes, floor, walls, decorationFree);
            return missing;
        }

        // Cada esquina tiene su capa: una celda puede necesitar varias, además de la máscara cardinal.
        static void PaintBlends(FloorMap map, IReadOnlyList<RoomTheme> themes, Tilemap floor, Tilemap walls,
            ISet<Vector2Int> decorationFree)
        {
            foreach (var provider in themes)
            {
                if (provider == null || !provider.RippleBorders)
                    continue;
                var ground = BlendLayers(floor.transform.parent, "BlendFloor", -14);
                var caps = BlendLayers(floor.transform.parent, "BlendCap", -9);
                foreach (var cell in map.Bounds.allPositionsWithin)
                {
                    var theme = themes[Mathf.Clamp(map.GetTheme(cell), 0, themes.Count - 1)];
                    var piece = WallRules.Classify(map, cell);
                    bool wall = WallRules.IsWall(piece);
                    var sets = wall ? provider.BlendCapTiles : provider.BlendFloorTiles;
                    if (!wall && map.Get(cell) != FloorMap.Kind.Floor)
                        continue;
                    foreach (var set in sets)
                    {
                        if (set.source != theme)
                            continue;
                        bool Neighbor(Vector2Int at) => map.Bounds.Contains(at)
                            && (wall ? WallRules.IsWall(WallRules.Classify(map, at)) : map.Get(at) == FloorMap.Kind.Floor)
                            && System.Array.IndexOf(set.neighbors, themes[map.GetTheme(at)]) >= 0;
                        var position = new Vector3Int(cell.x, cell.y, 0);
                        if (wall && IsFace(piece))
                        {
                            // El mismo hash en las dos mitades conserva la juntura del par.
                            bool lower = IsLowerFace(piece);
                            int variant = CellHash(lower ? cell + Vector2Int.up : cell) % 2;
                            bool FaceNeighbor(Vector2Int at) => Neighbor(at) && IsFace(WallRules.Classify(map, at));
                            int side = FaceNeighbor(cell + Vector2Int.right) ? 0 : FaceNeighbor(cell + Vector2Int.left) ? 1 : -1;
                            if (side >= 0 && provider.BlendFaces.Length == 8)
                                walls.SetTile(position, provider.BlendFaces[(side * 2 + (lower ? 1 : 0)) * 2 + variant]);
                            continue;
                        }
                        int mask = BlendMask(cell, Neighbor);
                        var layers = wall ? caps : ground;
                        if (mask != 0)
                            layers[0].SetTile(position, set.tiles[mask]);
                        for (int corner = 0; corner < 4; corner++)
                        {
                            int dx = corner % 2 == 0 ? 1 : -1, dy = corner < 2 ? 1 : -1;
                            if (!Neighbor(cell + new Vector2Int(dx, 0)) && !Neighbor(cell + new Vector2Int(0, dy))
                                && Neighbor(cell + new Vector2Int(dx, dy)))
                                layers[corner + 1].SetTile(position, set.corners[corner]);
                        }
                    }
                    // Provisional: una calcomanía cada 32 celdas candidatas, sin invadir puertas ni el sumidero.
                    if (wall || provider.BlendDecals.Length == 0 || CellHash(cell) % 32 != 0)
                        continue;
                    bool near = false, free = true;
                    for (int dx = -2; dx <= 2; dx++)
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            var at = cell + new Vector2Int(dx, dy);
                            if (map.Get(at) != FloorMap.Kind.Floor || (decorationFree != null && decorationFree.Contains(at)))
                                free = false;
                            var other = themes[Mathf.Clamp(map.GetTheme(at), 0, themes.Count - 1)];
                            foreach (var set in provider.BlendFloorTiles)
                                if (set.source == theme && System.Array.IndexOf(set.neighbors, other) >= 0)
                                    near = true;
                        }
                    if (!free || !near)
                        continue;
                    var decal = new GameObject("SandDrift").AddComponent<SpriteRenderer>();
                    decal.transform.SetParent(floor.transform.parent, false);
                    decal.transform.localPosition = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0);
                    decal.sprite = provider.BlendDecals[(CellHash(cell) / 32) % provider.BlendDecals.Length];
                    decal.sortingOrder = -13;
                }
            }
        }

        internal static bool IsLowerFace(Piece piece) => piece is Piece.FaceLower or Piece.FaceLowerEndLeft or Piece.FaceLowerEndRight;
        static bool IsFace(Piece piece) => IsLowerFace(piece)
            || piece is Piece.FaceUpper or Piece.FaceUpperEndLeft or Piece.FaceUpperEndRight;

        internal static int BlendMask(Vector2Int cell, System.Func<Vector2Int, bool> neighbor) =>
            (neighbor(cell + Vector2Int.up) ? 1 : 0) | (neighbor(cell + Vector2Int.right) ? 2 : 0)
            | (neighbor(cell + Vector2Int.down) ? 4 : 0) | (neighbor(cell + Vector2Int.left) ? 8 : 0);

        static Tilemap[] BlendLayers(Transform parent, string name, int order)
        {
            var layers = new Tilemap[5];
            for (int i = 0; i < layers.Length; i++)
            {
                var child = new GameObject(name + i, typeof(Tilemap), typeof(TilemapRenderer));
                child.transform.SetParent(parent, false);
                child.GetComponent<TilemapRenderer>().sortingOrder = order;
                layers[i] = child.GetComponent<Tilemap>();
            }
            return layers;
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
        internal static int PitMask(FloorMap map, Vector2Int cell) => KindMask(map, cell, FloorMap.Kind.Pit);

        /// <summary>Máscara de los tiles de fosa y de lodo: suma de los vecinos que no son de ese tipo (N=1, E=2, S=4, O=8).</summary>
        internal static int KindMask(FloorMap map, Vector2Int cell, FloorMap.Kind kind)
        {
            int x = cell.x, y = cell.y;
            return (map.Get(x, y + 1) == kind ? 0 : 1) | (map.Get(x + 1, y) == kind ? 0 : 2) | (map.Get(x, y - 1) == kind ? 0 : 4)
                | (map.Get(x - 1, y) == kind ? 0 : 8);
        }

        // Esquina de dentro: los dos vecinos de ese lado son fosa y la diagonal no. Orden del tema: NE, NO, SE, SO.
        static TileBase PitCorner(FloorMap map, Vector2Int cell, RoomTheme theme) => Corner(map, cell, FloorMap.Kind.Pit, theme.PitCorner);

        static TileBase Corner(FloorMap map, Vector2Int cell, FloorMap.Kind kind, System.Func<int, TileBase> corner)
        {
            int x = cell.x, y = cell.y;
            var corners = new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) };
            for (int i = 0; i < corners.Length; i++)
            {
                var (dx, dy) = corners[i];
                if (map.Get(x + dx, y) == kind && map.Get(x, y + dy) == kind && map.Get(x + dx, y + dy) != kind)
                    return corner(i);
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

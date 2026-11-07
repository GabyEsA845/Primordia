using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Piezas de las salas especiales del piso 3 hechas a mano (paso 6, bloque 4): la veta de carbón (casillas <c>c</c>), las filas de
    /// fumarolas (<c>V</c>), los montones de biomasa de las Ladronas (<c>P</c>) y el humo del Bosque en humo. Se colocan antes que el
    /// escenario, que esquiva lo que ocupan.
    /// </summary>
    public static partial class FloorBuilder
    {
        // Lo que ocupan en el reparto del escenario.
        const float SpecialCellRadius = 0.75f;

        // Color de la luz de una veta encendida.
        static readonly Color EmberGlow = new(1f, 0.46f, 0.14f);
        // Las vetas encendidas de la Veta de carbón sueltan una chispa cada 14-26 s que prende lo inflamable a 2,8 tiles.
        static readonly Vector2 SparkEvery = new(14f, 26f);
        const float SparkRadius = 2.8f;

        static void BuildSpecialRoomPieces(Transform roomObject, PlacedRoom placed, RoomTheme theme, FloorConfig config, List<(Vector2 position, float radius)> taken)
        {
            var template = placed.Template;
            var special = placed.Layout.Special;
            var coal = template.CellsOf(RoomTemplate.Mark.Coal);
            var embers = template.CellsOf(RoomTemplate.Mark.Ember);
            var vents = template.CellsOf(RoomTemplate.Mark.Vent);
            var piles = template.CellsOf(RoomTemplate.Mark.Pile);
            var mudPiles = template.CellsOf(RoomTemplate.Mark.MudPile);
            var bubblers = template.CellsOf(RoomTemplate.Mark.Bubbler);
            var plantCells = template.CellsOf(RoomTemplate.Mark.Plant);
            bool fireFront = special is { fireFront: true };
            bool cone = special is { coneChest: true };
            var chests = cone ? template.CellsOf(RoomTemplate.Mark.Chest) : new List<Vector2Int>();
            bool smoke = special is { smoke: true };
            if (coal.Count == 0 && embers.Count == 0 && vents.Count == 0 && piles.Count == 0 && mudPiles.Count == 0 && chests.Count == 0 && bubblers.Count == 0 && plantCells.Count == 0 && !fireFront && !smoke)
                return;

            var root = new GameObject("SpecialRoom").transform;
            root.SetParent(roomObject, false);
            var presence = root.gameObject.AddComponent<RoomPresence>();
            var art = SpecialRoomArt.Current;

            foreach (var cell in plantCells.Concat(coal).Concat(embers).Concat(vents).Concat(piles).Concat(mudPiles).Concat(chests).Concat(bubblers))
                taken.Add((CellCenter(placed, cell), SpecialCellRadius));

            // En el Bosque en humo las vetas encendidas solo alumbran y dispersan el humo; en la Veta de carbón, además, sueltan chispas.
            if ((coal.Count > 0 || embers.Count > 0) && art != null && art.HasCoal)
                BuildCoalSeam(root, roomObject, placed, art, new HashSet<Vector2Int>(coal), new HashSet<Vector2Int>(embers), !smoke, presence);

            if (vents.Count > 0)
                BuildVentRows(root, placed, theme, vents);

            if (special is { thieves: true })
                BuildThieves(roomObject, placed, config, piles);

            if (mudPiles.Count > 0)
                BuildMudTreasure(root, placed, config, mudPiles);

            if (bubblers.Count > 0)
                BuildBubblers(root, placed, theme, art, bubblers);

            if (fireFront)
            {
                var size = new Vector3(template.Width, template.Height, 0f);
                FireFrontController.Create(root, new Bounds((Vector2)placed.Origin + (Vector2)size / 2f, size));
            }

            if (chests.Count > 0)
                BuildConeChests(root, placed, config, chests);

            if (smoke && art != null && art.HasSmoke)
            {
                var size = new Vector3(template.Width, template.Height, 0f);
                SmokeVision.Create(root, new Bounds((Vector2)placed.Origin + (Vector2)size / 2f, size), art,
                    embers.Select(cell => CellCenter(placed, cell)), presence);
            }
        }

        /// <summary>
        /// Tesoro hundido: biomasa sobre el lodo profundo. La del montón más hundido (el más lejos de tierra firme) es grande; el resto,
        /// pequeña. Con el lodo hay que esquivar para llegar sin hundirse.
        /// </summary>
        static void BuildMudTreasure(Transform root, PlacedRoom placed, FloorConfig config, List<Vector2Int> piles)
        {
            if (config.SmallBiomass == null)
                return;
            var template = placed.Template;
            var firm = new List<Vector2Int>();
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    if (template.Get(x, y) == RoomTemplate.Mark.Floor)
                        firm.Add(new Vector2Int(x, y));
                }
            }
            var deepest = piles.OrderByDescending(cell => firm.Count == 0 ? 0f : firm.Min(f => Vector2Int.Distance(f, cell))).First();
            foreach (var cell in piles)
            {
                var prefab = cell == deepest && config.BigBiomass != null ? config.BigBiomass : config.SmallBiomass;
                Object.Instantiate(prefab, CellCenter(placed, cell), Quaternion.identity, root);
            }
        }

        /// <summary>Grietas de burbujas de metano en las casillas b, cada una con su propio desfase.</summary>
        static void BuildBubblers(Transform root, PlacedRoom placed, RoomTheme theme, SpecialRoomArt art, List<Vector2Int> cells)
        {
            var scenery = theme.Scenery;
            var bubble = art != null ? art.BubbleFor(ThemeBand(placed)) : System.Array.Empty<Sprite>();
            if (bubble.Length == 0)
                return;
            var random = new System.Random(placed.Origin.x * 31 ^ placed.Origin.y * 17 ^ 0xb0b);
            foreach (var cell in cells)
            {
                var position = CellCenter(placed, cell);
                // La grieta ocupa la celda: la base del dibujo, en el borde de abajo de la celda (como las fumarolas).
                MethaneBubbler.Create(root, new Vector2(position.x, position.y - 0.5f),
                    scenery != null ? scenery.ventIdle : System.Array.Empty<Sprite>(), scenery != null ? scenery.ventWarn : System.Array.Empty<Sprite>(),
                    bubble, (float)random.NextDouble() * 7f);
            }
        }

        /// <summary>
        /// Claro de la carnívora: una planta carnívora (la trampa del piso, sacada de las trampas del piso) en cada casilla Y. Cuelgan de los
        /// enemigos de la sala: esperan inactivas hasta que entra el jugador y se marchitan al limpiarla.
        /// </summary>
        static void BuildPlantRing(Transform enemiesRoot, PlacedRoom placed, FloorConfig config)
        {
            var plants = placed.Template.CellsOf(RoomTemplate.Mark.Plant);
            if (plants.Count == 0)
                return;
            var prefab = config.Traps.Select(trap => trap.prefab).FirstOrDefault(p => p != null && p.GetComponentInChildren<CarnivorousPlant>(true) != null);
            if (prefab == null)
            {
                Debug.LogWarning($"FloorBuilder: {placed.Layout.Name} pide plantas carnívoras y el piso no tiene esa trampa.");
                return;
            }
            foreach (var cell in plants)
                Object.Instantiate(prefab, CellCenter(placed, cell), Quaternion.identity, enemiesRoot);
        }

        /// <summary>Cofres de cono en las casillas C (dos seguidas en horizontal, uno grande de 2 casillas).</summary>
        static void BuildConeChests(Transform root, PlacedRoom placed, FloorConfig config, List<Vector2Int> chests)
        {
            var done = new HashSet<Vector2Int>();
            foreach (var cell in chests)
            {
                if (!done.Add(cell))
                    continue;
                bool wide = chests.Contains(cell + Vector2Int.right);
                if (wide)
                    done.Add(cell + Vector2Int.right);
                var position = (Vector2)(placed.Origin + cell) + new Vector2(wide ? 1f : 0.5f, 0.5f);
                ConeChest.Spawn(root, position, config, config.SmallBiomass, config.BigBiomass);
            }
        }

        static Vector2 CellCenter(PlacedRoom placed, Vector2Int cell) => (Vector2)(placed.Origin + cell) + Vector2.one * 0.5f;

        /// <summary>
        /// Casillas de carbón (apagadas, que arden con fuego) y de veta encendida (brasas fijas), con su tile por máscara (vecinos que
        /// NO son veta: N=1, E=2, S=4, O=8) y sus esquinas de dentro. Una luz cálida por cada racimo de casillas encendidas.
        /// </summary>
        static void BuildCoalSeam(Transform root, Transform roomObject, PlacedRoom placed, SpecialRoomArt art, HashSet<Vector2Int> coal, HashSet<Vector2Int> embers,
            bool sparks, RoomPresence presence)
        {
            var band = art.CoalFor(ThemeBand(placed));
            var all = new HashSet<Vector2Int>(coal);
            all.UnionWith(embers);
            // Esquinas en el orden del arte: NE, NO, SE, SO.
            var diagonals = new[] { new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
            foreach (var cell in all)
            {
                int mask = (all.Contains(cell + Vector2Int.up) ? 0 : 1) | (all.Contains(cell + Vector2Int.right) ? 0 : 2)
                    | (all.Contains(cell + Vector2Int.down) ? 0 : 4) | (all.Contains(cell + Vector2Int.left) ? 0 : 8);
                var corners = new List<Sprite>();
                for (int i = 0; i < diagonals.Length; i++)
                {
                    var d = diagonals[i];
                    if (all.Contains(cell + new Vector2Int(d.x, 0)) && all.Contains(cell + new Vector2Int(0, d.y)) && !all.Contains(cell + d)
                        && band.corners.Length > i)
                        corners.Add(band.corners[i]);
                }
                int variant = Mathf.Abs(cell.x * 3 + cell.y * 5);
                if (embers.Contains(cell))
                {
                    var lit = CoalCell.CreateLit(root, CellCenter(placed, cell), band.tiles[mask], corners.ToArray(), art, variant);
                    if (sparks)
                        lit.ConfigureSparks(SparkEvery, SparkRadius, () => presence.PlayerInside);
                }
                else
                {
                    CoalCell.Create(root, CellCenter(placed, cell), band.tiles[mask], corners.ToArray(), art, variant);
                }
            }

            // Una luz por racimo de casillas encendidas (las que se tocan).
            if (embers.Count == 0)
                return;
            Ambience.CreateGlobalLight(roomObject);
            var pending = new HashSet<Vector2Int>(embers);
            while (pending.Count > 0)
            {
                var start = pending.First();
                var cluster = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                pending.Remove(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    cluster.Add(current);
                    foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                    {
                        if (pending.Remove(current + step))
                            queue.Enqueue(current + step);
                    }
                }
                var center = cluster.Aggregate(Vector2.zero, (sum, cell) => sum + CellCenter(placed, cell)) / cluster.Count;
                Ambience.CreateGlow(root, center, new Glow(null, EmberGlow, 3.2f, new Vector2(0.55f, 0.9f), 2.4f), cluster.Count * 0.37f);
            }
        }

        /// <summary>Fumarolas en filas: cada fila suelta su chorro en su turno, repartidas a lo largo del ciclo (una, otra, otra…).</summary>
        static void BuildVentRows(Transform root, PlacedRoom placed, RoomTheme theme, List<Vector2Int> vents)
        {
            var art = theme.Scenery;
            if (art == null || art.ventIdle == null || art.ventIdle.Length == 0 || art.ventBlast == null || art.ventBlast.Length == 0)
                return;

            var rows = vents.Select(cell => cell.y).Distinct().OrderByDescending(y => y).ToList();
            foreach (var cell in vents)
            {
                int row = rows.IndexOf(cell.y);
                float offset = row * SteamVent.CycleSeconds / rows.Count;
                // La boca ocupa la celda: la base del dibujo, en el borde de abajo de la celda.
                var position = CellCenter(placed, cell);
                SteamVent.Create(root, new Vector2(position.x, position.y - 0.5f), art.ventIdle, art.ventWarn, art.ventBlast, offset);
            }
        }

        /// <summary>Ladronas: biomasa en las casillas P, grietas en los bordes de la sala y recompensa en el centro.</summary>
        static void BuildThieves(Transform roomObject, PlacedRoom placed, FloorConfig config, List<Vector2Int> piles)
        {
            var template = placed.Template;
            var cracks = new List<Vector2>();
            for (int y = 1; y < template.Height - 1; y++)
            {
                for (int x = 1; x < template.Width - 1; x++)
                {
                    if (template.Get(x, y) != RoomTemplate.Mark.Floor)
                        continue;
                    bool nextToWall = template.Get(x, y + 1) == RoomTemplate.Mark.Wall || template.Get(x, y - 1) == RoomTemplate.Mark.Wall
                        || template.Get(x + 1, y) == RoomTemplate.Mark.Wall || template.Get(x - 1, y) == RoomTemplate.Mark.Wall;
                    // Dos de margen a los lados para que quepa la salida corriendo, y lejos de las puertas.
                    if (nextToWall && template.Get(x - 2, y) == RoomTemplate.Mark.Floor && template.Get(x + 2, y) == RoomTemplate.Mark.Floor
                        && !placed.DoorCells.Values.SelectMany(cells => cells).Any(door => Vector2Int.Distance(door - placed.Origin, new Vector2Int(x, y)) < 4f))
                        cracks.Add(CellCenter(placed, new Vector2Int(x, y)));
                }
            }

            // Se reparten unas pocas grietas por toda la sala (las dos primeras de cada cuarto de la lista de sitios).
            var chosen = new List<Vector2>();
            var random = new System.Random(placed.Origin.x * 31 ^ placed.Origin.y * 17);
            foreach (var crack in cracks.OrderBy(_ => random.Next()))
            {
                if (chosen.Count >= 5)
                    break;
                if (chosen.All(other => Vector2.Distance(other, crack) > 5f))
                    chosen.Add(crack);
            }

            var art = HeartStorageArt.Current;
            if (art != null && art.crack != null)
            {
                foreach (var crack in chosen)
                {
                    var piece = new GameObject("RoachCrack");
                    piece.transform.SetParent(roomObject, true);
                    piece.transform.position = crack;
                    var renderer = piece.AddComponent<SpriteRenderer>();
                    renderer.sprite = art.crack;
                    renderer.sortingOrder = -7;
                }
            }

            var controller = roomObject.gameObject.AddComponent<ThiefRoomController>();
            controller.Configure(config, config.SmallBiomass, config.BigBiomass, piles.Select(cell => CellCenter(placed, cell)).ToArray(), chosen.ToArray(),
                placed.FloorCenter);
        }
    }
}

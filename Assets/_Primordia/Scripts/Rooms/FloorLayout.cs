using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Papel de una sala en el piso.</summary>
    public enum RoomRole
    {
        Start,
        Combat,
        /// <summary>Sala de combate sin salida que cuelga del camino principal.</summary>
        Side,
        Boss,
        /// <summary>Tienda: sala sin salida y sin enemigos que cuelga de una sala de combate de la segunda mitad del camino.</summary>
        Shop
    }

    /// <summary>Una sala del plano: su celda en la rejilla, su forma y hacia dónde tiene puertas.</summary>
    public class LayoutRoom
    {
        public Vector2Int Cell;
        public RoomRole Role;

        /// <summary>
        /// Orden de la sala de combate en el camino principal (1 = la primera). Las laterales llevan el de la
        /// sala de la que cuelgan; el inicio, 0; el jefe, uno más que la última.
        /// </summary>
        public int Depth;

        public bool Reward;
        public RoomTemplate Template;
        public readonly List<Vector2Int> Doors = new();

        public string Name => Role switch
        {
            RoomRole.Start => "Room_Start",
            RoomRole.Boss => "Room_Boss",
            RoomRole.Side => $"Room_{Depth}_Side",
            RoomRole.Shop => "Room_Shop",
            _ => $"Room_{Depth}"
        };
    }

    /// <summary>
    /// Plano de un piso: qué salas hay, en qué celda de la rejilla y qué pares están unidos por un pasillo.
    /// Solo hay pasillos entre celdas vecinas; el resto de lados de cada sala queda cerrado.
    /// </summary>
    public class FloorLayout
    {
        public readonly List<LayoutRoom> Rooms = new();
        public readonly List<(LayoutRoom from, LayoutRoom to)> Links = new();

        /// <summary>Jefe de esta partida, con su sala y su tileset.</summary>
        public FloorConfig.BossEntry BossChoice;

        public LayoutRoom Start => Rooms.First(room => room.Role == RoomRole.Start);
        public LayoutRoom Boss => Rooms.First(room => room.Role == RoomRole.Boss);

        public void Link(LayoutRoom from, LayoutRoom to)
        {
            Links.Add((from, to));
            from.Doors.Add(to.Cell - from.Cell);
            to.Doors.Add(from.Cell - to.Cell);
        }
    }

    /// <summary>
    /// Genera el plano de un piso: un camino con giros al azar desde el inicio hasta el jefe, unas pocas
    /// salas laterales sin salida colgadas de salas de combate y, si el piso tiene tienda, una sala más para ella
    /// colgada de la segunda mitad del camino. Con la misma semilla sale siempre el mismo plano.
    /// </summary>
    public static class FloorLayoutGenerator
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        const int MaxAttempts = 200;

        public static FloorLayout Generate(FloorConfig config, System.Random random)
        {
            // El jefe primero: su sala decide el tamaño de la última celda del camino.
            var boss = config.ChooseBoss(random);
            int combatCount = RandomRange(random, config.MainCombatRooms);
            int sideCount = RandomRange(random, config.SideRoomCount);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var path = RandomPath(random, combatCount + 2);
                if (path == null)
                    continue;

                var sides = SideCells(random, path, sideCount);
                if (sides == null)
                    continue;

                (int parent, Vector2Int cell)? shop = null;
                if (config.HasShop)
                {
                    shop = ShopCell(random, path, sides);
                    if (shop == null)
                        continue;
                }

                return BuildWithBoss(boss, config, random, path, sides, shop);
            }

            // Muy improbable: sin sitio para las laterales tras muchos intentos. Camino recto, sin laterales.
            var straight = Enumerable.Range(0, combatCount + 2).Select(i => new Vector2Int(i, 0)).ToList();
            var none = new List<(int, Vector2Int)>();
            return BuildWithBoss(boss, config, random, straight, none, config.HasShop ? ShopCell(random, straight, none) : null);
        }

        /// <summary>
        /// Celda libre para la tienda, vecina de una sala de combate de la segunda mitad del camino (para llegar
        /// con biomasa). Null si no cabe.
        /// </summary>
        static (int parent, Vector2Int cell)? ShopCell(System.Random random, List<Vector2Int> path, List<(int parent, Vector2Int cell)> sides)
        {
            var used = new HashSet<Vector2Int>(path.Concat(sides.Select(side => side.cell)));
            int combatCount = path.Count - 2;
            var parents = Enumerable.Range(combatCount / 2 + 1, combatCount - combatCount / 2).OrderBy(_ => random.Next()).ToList();

            foreach (int parent in parents)
            {
                var free = Directions.Select(d => path[parent] + d).Where(cell => !used.Contains(cell)).ToList();
                if (free.Count > 0)
                    return (parent, free[random.Next(free.Count)]);
            }

            return null;
        }

        /// <summary>Camino que no se cruza consigo mismo, con giros al azar. Null si se encierra.</summary>
        static List<Vector2Int> RandomPath(System.Random random, int length)
        {
            var path = new List<Vector2Int> { Vector2Int.zero };
            var used = new HashSet<Vector2Int> { Vector2Int.zero };

            while (path.Count < length)
            {
                var current = path[^1];
                var options = Directions.Select(d => current + d).Where(cell => !used.Contains(cell)).ToList();
                if (options.Count == 0)
                    return null;

                var next = options[random.Next(options.Count)];
                path.Add(next);
                used.Add(next);
            }

            return path;
        }

        /// <summary>
        /// Celdas libres para las salas laterales, vecinas de salas de combate distintas (ni el inicio ni el jefe).
        /// Cada una se devuelve con el índice de la sala del camino de la que cuelga. Null si no caben.
        /// </summary>
        static List<(int parent, Vector2Int cell)> SideCells(System.Random random, List<Vector2Int> path, int count)
        {
            var used = new HashSet<Vector2Int>(path);
            var parents = Enumerable.Range(1, path.Count - 2).OrderBy(_ => random.Next()).ToList();
            var result = new List<(int, Vector2Int)>();

            foreach (int parent in parents)
            {
                if (result.Count == count)
                    break;

                var free = Directions.Select(d => path[parent] + d).Where(cell => !used.Contains(cell)).ToList();
                if (free.Count == 0)
                    continue;

                var cell = free[random.Next(free.Count)];
                used.Add(cell);
                result.Add((parent, cell));
            }

            return result.Count == count ? result : null;
        }

        static FloorLayout BuildWithBoss(FloorConfig.BossEntry boss, FloorConfig config, System.Random random, List<Vector2Int> path,
            List<(int parent, Vector2Int cell)> sides, (int parent, Vector2Int cell)? shop)
        {
            var layout = Build(config, random, path, sides, shop, boss.room);
            layout.BossChoice = boss;
            return layout;
        }

        static FloorLayout Build(FloorConfig config, System.Random random, List<Vector2Int> path, List<(int parent, Vector2Int cell)> sides,
            (int parent, Vector2Int cell)? shop, RoomTemplate bossRoom)
        {
            var layout = new FloorLayout();
            var rewardAfter = new HashSet<int>(config.RewardAfter ?? new int[0]);
            RoomTemplate previousCombat = null;

            for (int i = 0; i < path.Count; i++)
            {
                var role = i == 0 ? RoomRole.Start : i == path.Count - 1 ? RoomRole.Boss : RoomRole.Combat;
                var room = new LayoutRoom { Cell = path[i], Role = role, Depth = i };

                if (role == RoomRole.Start)
                    room.Template = config.StartRoom;
                else if (role == RoomRole.Boss)
                    room.Template = bossRoom;
                else
                {
                    // Dos salas seguidas del camino no repiten forma, si hay donde elegir.
                    room.Template = Pick(random, config.CombatRooms, previousCombat);
                    previousCombat = room.Template;
                    room.Reward = rewardAfter.Contains(i);
                }

                layout.Rooms.Add(room);
                if (i > 0)
                    layout.Link(layout.Rooms[i - 1], room);
            }

            foreach (var (parent, cell) in sides)
            {
                var side = new LayoutRoom
                {
                    Cell = cell,
                    Role = RoomRole.Side,
                    Depth = layout.Rooms[parent].Depth,
                    Reward = true,
                    Template = Pick(random, config.SideRooms, null)
                };
                layout.Rooms.Add(side);
                layout.Link(layout.Rooms[parent], side);
            }

            if (shop.HasValue)
            {
                var (shopParent, shopCell) = shop.Value;
                var shopRoom = new LayoutRoom
                {
                    Cell = shopCell,
                    Role = RoomRole.Shop,
                    Depth = layout.Rooms[shopParent].Depth,
                    Template = config.ShopTemplate,
                };
                layout.Rooms.Add(shopRoom);
                layout.Link(layout.Rooms[shopParent], shopRoom);
            }

            return layout;
        }

        static RoomTemplate Pick(System.Random random, RoomTemplate[] options, RoomTemplate avoid)
        {
            var candidates = options.Where(t => t != null && t != avoid).ToList();
            if (candidates.Count == 0)
                candidates = options.Where(t => t != null).ToList();
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }

        static int RandomRange(System.Random random, Vector2Int range) =>
            random.Next(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y) + 1);
    }
}

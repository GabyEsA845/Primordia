using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Papel de una sala en el piso.</summary>
    public enum RoomRole
    {
        Start,
        /// <summary>Sala de combate del camino (también las especiales que cuentan como combate).</summary>
        Combat,
        /// <summary>Sala sin salida que cuelga del camino principal (también las especiales laterales).</summary>
        Side,
        Boss,
        /// <summary>Tienda: sala sin salida y sin enemigos que cuelga de una sala de combate de un tramo del camino.</summary>
        Shop,
        /// <summary>Sala de paso: cueva sin enemigos pegada a la del jefe, con la fosa que lleva al piso siguiente.</summary>
        Transition,
        /// <summary>Sala de cambio (laboratorio de Darwin): en fondo de saco detrás de la tienda.</summary>
        Exchange,
        /// <summary>Antesala del jefe: túnel sin enemigos justo antes de su sala, en línea recta con ella.</summary>
        Antechamber,
        /// <summary>Sala especial del camino que no es de combate (el Parkour).</summary>
        Special
    }

    /// <summary>Una sala del plano: su celda en la rejilla, su forma y hacia dónde tiene puertas.</summary>
    public class LayoutRoom
    {
        /// <summary>Su casilla; si ocupa dos en vertical (<see cref="Tall"/>), la de abajo.</summary>
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

        /// <summary>Sala especial hecha a mano, si lo es.</summary>
        public FloorConfig.SpecialRoom? Special;

        /// <summary>Tema de la sala de combate (Pradera de Calamites, Nido de bombarderos…), si lo tiene.</summary>
        public FloorConfig.EncounterTheme? Encounter;

        /// <summary>Ocupa dos casillas en vertical: <see cref="Cell"/> y la de encima.</summary>
        public bool Tall;

        public IEnumerable<Vector2Int> Cells
        {
            get
            {
                yield return Cell;
                if (Tall)
                    yield return Cell + Vector2Int.up;
            }
        }

        /// <summary>Enemigos respecto a una sala normal (el Corazón morado lleva el doble; el Parkour y la Gruta, ninguno).</summary>
        public float EnemyMultiplier => Special.HasValue ? Special.Value.enemyMultiplier : Encounter.HasValue ? Encounter.Value.enemyMultiplier : 1f;

        string SpecialName => Special.HasValue && Special.Value.template != null
            ? Special.Value.template.name.Replace("Room_Special_", "")
            : "Special";

        public string Name => Role switch
        {
            RoomRole.Start => "Room_Start",
            RoomRole.Boss => "Room_Boss",
            RoomRole.Side when Special.HasValue => $"Room_{Depth}_Side_{SpecialName}",
            RoomRole.Side => $"Room_{Depth}_Side",
            RoomRole.Shop => "Room_Shop",
            RoomRole.Transition => "Room_Transition",
            RoomRole.Exchange => "Room_Exchange",
            RoomRole.Antechamber => "Room_Antechamber",
            RoomRole.Special => $"Room_{Depth}_{SpecialName}",
            _ when Special.HasValue => $"Room_{Depth}_{SpecialName}",
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
        readonly List<Vector2Int> linkDirections = new();
        readonly HashSet<int> shortcutLinks = new();

        /// <summary>Jefe de esta partida, con su sala y su tileset.</summary>
        public FloorConfig.BossEntry BossChoice;

        public LayoutRoom Start => Rooms.First(room => room.Role == RoomRole.Start);
        public LayoutRoom Boss => Rooms.First(room => room.Role == RoomRole.Boss);

        /// <summary>La sala de paso, o null si el piso no tiene (el jefe da la victoria).</summary>
        public LayoutRoom Transition => Rooms.FirstOrDefault(room => room.Role == RoomRole.Transition);

        /// <summary>Formas de cueva creadas para esta partida: se destruyen con el piso (<see cref="ReleaseGenerated"/>).</summary>
        public readonly List<RoomTemplate> GeneratedTemplates = new();

        /// <summary>Destruye las formas de cueva de esta partida.</summary>
        public void ReleaseGenerated()
        {
            foreach (var template in GeneratedTemplates)
            {
                if (template == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(template);
                else
                    Object.DestroyImmediate(template);
            }
            GeneratedTemplates.Clear();
        }

        /// <summary>Dirección del pasillo del enlace <paramref name="index"/>, de la primera sala a la segunda.</summary>
        public Vector2Int Direction(int index) => linkDirections[index];

        /// <summary>True si el enlace <paramref name="index"/> es un atajo sellado con maleza (la maleza está en la puerta de la primera sala).</summary>
        public bool IsShortcut(int index) => shortcutLinks.Contains(index);

        /// <summary>Une dos salas vecinas con un atajo; <paramref name="from"/> es la menos profunda y lleva la maleza.</summary>
        public void LinkShortcut(LayoutRoom from, LayoutRoom to)
        {
            shortcutLinks.Add(Links.Count);
            Link(from, to);
        }

        public void Link(LayoutRoom from, LayoutRoom to) => Link(from, to, to.Cell - from.Cell);

        /// <summary>Une dos salas por las casillas vecinas que dan una a la otra (hace falta con las de dos casillas).</summary>
        public void Link(LayoutRoom from, LayoutRoom to, Vector2Int direction)
        {
            Links.Add((from, to));
            linkDirections.Add(direction);
            from.Doors.Add(direction);
            to.Doors.Add(-direction);
        }
    }

    /// <summary>
    /// Genera el plano de un piso: un camino con giros al azar desde el inicio hasta el jefe, unas pocas
    /// salas laterales sin salida colgadas de salas de combate y, si el piso tiene tienda, una sala más para ella
    /// colgada de un tramo del camino (con la sala de cambio detrás, si la hay). Si el piso tiene sala de paso, el camino
    /// sigue una celda más allá del jefe para ella. Desde el piso 2, además, salas especiales sorteadas y la antesala del
    /// jefe, en línea recta con él. Con la misma semilla sale siempre el mismo plano.
    /// </summary>
    public static class FloorLayoutGenerator
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        const int MaxAttempts = 200;

        // Las especiales nunca en las dos primeras salas de combate ni en la última antes del jefe.
        const int FirstSpecialDepth = 3;

        /// <summary>Un hueco del camino principal, en orden: inicio, combates, especiales, antesala, jefe y sala de paso.</summary>
        class Slot
        {
            public RoomRole Role;
            public int Depth;
            public FloorConfig.SpecialRoom? Special;
            public bool Tall => Special.HasValue && Special.Value.tall;
            /// <summary>Índice de su primera casilla en el camino.</summary>
            public int PathIndex;
        }

        public static FloorLayout Generate(FloorConfig config, System.Random random)
        {
            // El jefe primero: su sala decide el tamaño de la última celda del camino.
            var boss = config.ChooseBoss(random);
            int combatCount = RandomRange(random, config.MainCombatRooms);
            int sideCount = RandomRange(random, config.SideRoomCount);

            var specials = PickSpecials(config, random);
            var slots = Slots(config, combatCount, specials.Where(s => s.placement == FloorConfig.SpecialPlacement.Path).ToList(), random);
            var sideSpecials = specials.Where(s => s.placement == FloorConfig.SpecialPlacement.Side).ToList();
            int length = slots.Sum(slot => slot.Tall ? 2 : 1);
            var (required, sameAsBefore) = Constraints(slots, length);
            bool constrained = required.Any(d => d.HasValue) || sameAsBefore.Any(same => same);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var path = RandomPath(random, length, config.Path, required, sameAsBefore);
                if (path == null)
                    continue;

                var combatSlots = CombatSlots(slots);
                var sides = SideCells(random, slots, path, combatSlots, sideCount);
                if (sides == null)
                    continue;

                var used = new HashSet<Vector2Int>(path.Concat(sides.Select(side => side.cell)));
                var specialSides = SpecialSideCells(random, config, slots, path, sideSpecials, used, combatCount);
                if (specialSides == null)
                    continue;

                (int parent, Vector2Int cell)? shop = null;
                Vector2Int? exchange = null;
                if (config.HasShop)
                {
                    shop = ShopCell(random, slots, path, combatSlots, used, config.ShopDepth, combatCount);
                    if (shop == null)
                        continue;
                    if (config.HasExchange)
                    {
                        used.Add(shop.Value.cell);
                        exchange = ExchangeCell(random, path[slots[shop.Value.parent].PathIndex], shop.Value.cell, used);
                        if (exchange == null)
                            continue;
                    }
                }

                return BuildWithBoss(boss, config, random, slots, path, sides, specialSides, shop, exchange);
            }

            // Muy improbable: sin sitio tras muchos intentos. Camino recto (hacia abajo si hay salas que lo piden), sin laterales.
            var straight = Enumerable.Range(0, length).Select(i => constrained ? new Vector2Int(0, -i) : new Vector2Int(i, 0)).ToList();
            var none = new List<(int, Vector2Int)>();
            var noneSet = new HashSet<Vector2Int>(straight);
            // Las especiales laterales también (el Corazón morado sale siempre): en un camino recto siempre hay sitio a los lados.
            var straightSpecials = SpecialSideCells(random, config, slots, straight, sideSpecials, noneSet, combatCount)
                ?? new List<(FloorConfig.SpecialRoom, int, Vector2Int)>();
            var straightShop = config.HasShop ? ShopCell(random, slots, straight, CombatSlots(slots), noneSet, config.ShopDepth, combatCount) : null;
            Vector2Int? straightExchange = null;
            if (straightShop.HasValue && config.HasExchange)
            {
                noneSet.Add(straightShop.Value.cell);
                straightExchange = ExchangeCell(random, straight[slots[straightShop.Value.parent].PathIndex], straightShop.Value.cell, noneSet);
            }
            return BuildWithBoss(boss, config, random, slots, straight, none, straightSpecials, straightShop, straightExchange);
        }

        /// <summary>
        /// Salas especiales de la partida: las que salen siempre y, hasta el número sorteado, otras al azar. Aparte, sin
        /// contar en ese número, la sala de la almeja: una de las candidatas, para que cada piso tenga una almeja grande.
        /// </summary>
        static List<FloorConfig.SpecialRoom> PickSpecials(FloorConfig config, System.Random random)
        {
            var all = config.SpecialRooms;
            if (all.Length == 0)
                return new List<FloorConfig.SpecialRoom>();

            var always = all.Where(room => room.always).ToList();
            var candidates = all.Where(room => room.clamRoom && !room.always).ToList();
            var clamRoom = candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : (FloorConfig.SpecialRoom?)null;
            var pool = all.Where(room => !room.always && (!clamRoom.HasValue || room.template != clamRoom.Value.template)).ToList();

            int total = Mathf.Max(always.Count, RandomRange(random, config.SpecialRoomCount));
            var others = pool.OrderBy(_ => random.Next()).Take(total - always.Count);
            var picked = always.Concat(others).ToList();
            if (clamRoom.HasValue)
                picked.Add(clamRoom.Value);
            return picked;
        }

        /// <summary>
        /// Huecos del camino: inicio, combates (las especiales que cuentan como combate ocupan uno, nunca de las salas 1–2 ni
        /// de la última), las especiales que no son de combate entre dos combates, antesala, jefe y sala de paso.
        /// </summary>
        static List<Slot> Slots(FloorConfig config, int combatCount, List<FloorConfig.SpecialRoom> pathSpecials, System.Random random)
        {
            var slots = new List<Slot> { new() { Role = RoomRole.Start, Depth = 0 } };

            // Especiales de combate: en salas de combate al azar de las permitidas (sin sorteo si no hay: el piso 1 no gasta azar).
            var combatSpecials = new Dictionary<int, FloorConfig.SpecialRoom>();
            var asCombat = pathSpecials.Where(s => s.countsAsCombat).ToList();
            if (asCombat.Count > 0)
            {
                var free = Enumerable.Range(FirstSpecialDepth, Mathf.Max(0, combatCount - FirstSpecialDepth)).OrderBy(_ => random.Next()).ToList();
                foreach (var special in asCombat)
                {
                    // Algunas (el Bosque en humo) solo salen de cierta franja en adelante; si no hay hueco así, esa sala no sale.
                    int pick = free.FindIndex(depth => !config.HasBands || (special.minBand <= 0
                        || FloorBuilder.BandOf(depth, combatCount, config.BandThemes.Length) >= special.minBand)
                        && (special.bandsBelow <= 0 || FloorBuilder.BandOf(depth, combatCount, config.BandThemes.Length) < special.bandsBelow));
                    if (pick < 0)
                        continue;
                    combatSpecials[free[pick]] = special;
                    free.RemoveAt(pick);
                }
            }

            // Las demás (Parkour): entre la sala d y la d + 1, sin pasar de la penúltima.
            var between = new Dictionary<int, FloorConfig.SpecialRoom>();
            var passages = pathSpecials.Where(s => !s.countsAsCombat).ToList();
            if (passages.Count > 0)
            {
                var gaps = Enumerable.Range(FirstSpecialDepth - 1, Mathf.Max(0, combatCount - FirstSpecialDepth)).OrderBy(_ => random.Next()).ToList();
                foreach (var special in passages.Take(gaps.Count))
                {
                    between[gaps[0]] = special;
                    gaps.RemoveAt(0);
                }
            }

            for (int depth = 1; depth <= combatCount; depth++)
            {
                slots.Add(new Slot
                {
                    Role = RoomRole.Combat,
                    Depth = depth,
                    Special = combatSpecials.TryGetValue(depth, out var special) ? special : null
                });
                if (between.TryGetValue(depth, out var passage))
                    slots.Add(new Slot { Role = RoomRole.Special, Depth = depth, Special = passage });
            }

            if (config.HasAntechamber)
                slots.Add(new Slot { Role = RoomRole.Antechamber, Depth = combatCount });
            slots.Add(new Slot { Role = RoomRole.Boss, Depth = combatCount + 1 });
            if (config.HasTransition)
                slots.Add(new Slot { Role = RoomRole.Transition, Depth = combatCount + 2 });

            int index = 0;
            foreach (var slot in slots)
            {
                slot.PathIndex = index;
                index += slot.Tall ? 2 : 1;
            }
            return slots;
        }

        /// <summary>
        /// Direcciones obligadas del camino: las salas altas (Parkour) se recorren de arriba abajo, y del hueco de la antesala
        /// al jefe se sigue en la misma dirección con la que se llegó a la antesala.
        /// </summary>
        static (Vector2Int?[] required, bool[] sameAsBefore) Constraints(List<Slot> slots, int length)
        {
            var required = new Vector2Int?[length];
            var same = new bool[length];
            foreach (var slot in slots)
            {
                if (slot.Tall)
                {
                    for (int i = slot.PathIndex; i <= slot.PathIndex + 2 && i < length; i++)
                        required[i] = Vector2Int.down;
                }
                if (slot.Role == RoomRole.Antechamber && slot.PathIndex + 1 < length)
                    same[slot.PathIndex + 1] = true;
            }
            return (required, same);
        }

        /// <summary>Huecos de combate normales (sin especial), de los que cuelgan laterales y tienda.</summary>
        static List<int> CombatSlots(List<Slot> slots) =>
            Enumerable.Range(0, slots.Count).Where(i => slots[i].Role == RoomRole.Combat && !slots[i].Special.HasValue).ToList();

        /// <summary>
        /// Celda libre para la tienda, vecina de una sala de combate del tramo <paramref name="depth"/> del camino (en
        /// fracción de las salas de combate: la segunda mitad en el piso 1, para llegar con biomasa; el tercio del medio en
        /// el piso 2). Null si no cabe.
        /// </summary>
        static (int parent, Vector2Int cell)? ShopCell(System.Random random, List<Slot> slots, List<Vector2Int> path, List<int> combatSlots,
            HashSet<Vector2Int> used, Vector2 depth, int combatCount)
        {
            float from = Mathf.Min(depth.x, depth.y), to = Mathf.Max(depth.x, depth.y);
            var parents = combatSlots
                .Where(i => combatCount <= 1 || ((slots[i].Depth - 1f) / (combatCount - 1f) >= from - 0.001f
                    && (slots[i].Depth - 1f) / (combatCount - 1f) <= to + 0.001f))
                .OrderBy(_ => random.Next()).ToList();

            foreach (int parent in parents)
            {
                var cell = path[slots[parent].PathIndex];
                var free = Directions.Select(d => cell + d).Where(c => !used.Contains(c)).ToList();
                if (free.Count > 0)
                    return (parent, free[random.Next(free.Count)]);
            }

            return null;
        }

        /// <summary>
        /// Celda de la sala de cambio, en fondo de saco detrás de la tienda: la de enfrente de la entrada de la tienda si está
        /// libre; si no, otra vecina libre. Null si no hay.
        /// </summary>
        static Vector2Int? ExchangeCell(System.Random random, Vector2Int parentCell, Vector2Int shopCell, HashSet<Vector2Int> used)
        {
            var ahead = shopCell + (shopCell - parentCell);
            if (!used.Contains(ahead))
                return ahead;
            var free = Directions.Select(d => shopCell + d).Where(c => !used.Contains(c)).ToList();
            return free.Count > 0 ? free[random.Next(free.Count)] : null;
        }

        /// <summary>
        /// Camino que no se cruza consigo mismo, con giros al azar. Con pesos distintos por dirección (piso 2), cada paso
        /// elige según ellos, así el camino tiende a bajar. Respeta las direcciones obligadas. Null si se encierra.
        /// </summary>
        static List<Vector2Int> RandomPath(System.Random random, int length, FloorConfig.PathWeights weights,
            Vector2Int?[] required, bool[] sameAsBefore)
        {
            var path = new List<Vector2Int> { Vector2Int.zero };
            var used = new HashSet<Vector2Int> { Vector2Int.zero };

            while (path.Count < length)
            {
                int index = path.Count;
                var current = path[^1];
                var options = Directions.Select(d => current + d).Where(cell => !used.Contains(cell)).ToList();
                if (required[index].HasValue)
                    options = options.Where(cell => cell - current == required[index].Value).ToList();
                if (sameAsBefore[index] && index >= 2)
                    options = options.Where(cell => cell - current == current - path[index - 2]).ToList();
                if (options.Count == 0)
                    return null;

                var next = weights.IsEven ? options[random.Next(options.Count)] : Weighted(random, options, current, weights);
                path.Add(next);
                used.Add(next);
            }

            return path;
        }

        static Vector2Int Weighted(System.Random random, List<Vector2Int> options, Vector2Int current, FloorConfig.PathWeights weights)
        {
            float total = options.Sum(cell => weights.For(cell - current));
            if (total <= 0f)
                return options[random.Next(options.Count)];

            float roll = (float)random.NextDouble() * total;
            foreach (var cell in options)
            {
                roll -= weights.For(cell - current);
                if (roll < 0f)
                    return cell;
            }
            return options[^1];
        }

        /// <summary>
        /// Celdas libres para las salas laterales, vecinas de salas de combate distintas (ni el inicio, ni el jefe,
        /// ni la sala de paso). Cada una se devuelve con el hueco del camino del que cuelga. Null si no caben.
        /// </summary>
        static List<(int parent, Vector2Int cell)> SideCells(System.Random random, List<Slot> slots, List<Vector2Int> path,
            List<int> combatSlots, int count)
        {
            var used = new HashSet<Vector2Int>(path);
            var parents = combatSlots.OrderBy(_ => random.Next()).ToList();
            var result = new List<(int, Vector2Int)>();

            foreach (int parent in parents)
            {
                if (result.Count == count)
                    break;

                var cell = path[slots[parent].PathIndex];
                var free = Directions.Select(d => cell + d).Where(c => !used.Contains(c)).ToList();
                if (free.Count == 0)
                    continue;

                var chosen = free[random.Next(free.Count)];
                used.Add(chosen);
                result.Add((parent, chosen));
            }

            return result.Count == count ? result : null;
        }

        /// <summary>
        /// Celdas de las especiales laterales (Corazón morado, Gruta escondida): colgando de salas de combate que no son ni las
        /// dos primeras ni la última, en su franja si la piden. Null si alguna no cabe.
        /// </summary>
        static List<(FloorConfig.SpecialRoom special, int parent, Vector2Int cell)> SpecialSideCells(System.Random random, FloorConfig config,
            List<Slot> slots, List<Vector2Int> path, List<FloorConfig.SpecialRoom> specials, HashSet<Vector2Int> used, int combatCount)
        {
            var result = new List<(FloorConfig.SpecialRoom, int, Vector2Int)>();
            int bands = config.HasBands ? config.BandThemes.Length : 0;
            var taken = new HashSet<int>();
            foreach (var special in specials)
            {
                // De cualquier sala de combate, también de las especiales: si no, con las especiales de combate en las salas del
                // Abismo, el Corazón morado podría quedarse sin sitio.
                var parents = Enumerable.Range(0, slots.Count).Where(i => slots[i].Role == RoomRole.Combat)
                    .Where(i => slots[i].Depth >= FirstSpecialDepth && slots[i].Depth < combatCount && !taken.Contains(i))
                    // La antesala de la sala secreta, en el último tercio del camino.
                    .Where(i => special.secretChamber == null || slots[i].Depth * 3 > combatCount * 2)
                    .Where(i => special.band < 0 || bands == 0 || FloorBuilder.BandOf(slots[i].Depth, combatCount, bands) == special.band)
                    .OrderBy(_ => random.Next()).ToList();

                bool placed = false;
                foreach (int parent in parents)
                {
                    var cell = path[slots[parent].PathIndex];
                    var free = Directions.Select(d => cell + d).Where(c => !used.Contains(c)).ToList();
                    // La antesala necesita libre la casilla de arriba (su cámara) y su puerta de arriba para ella: no puede colgar desde arriba.
                    if (special.secretChamber != null)
                        free = free.Where(c => c != cell + Vector2Int.down && !used.Contains(c + Vector2Int.up)).ToList();
                    if (free.Count == 0)
                        continue;
                    var chosen = free[random.Next(free.Count)];
                    used.Add(chosen);
                    if (special.secretChamber != null)
                        used.Add(chosen + Vector2Int.up);
                    taken.Add(parent);
                    result.Add((special, parent, chosen));
                    placed = true;
                    break;
                }
                if (!placed)
                    return null;
            }
            return result;
        }

        static FloorLayout BuildWithBoss(FloorConfig.BossEntry boss, FloorConfig config, System.Random random, List<Slot> slots,
            List<Vector2Int> path, List<(int parent, Vector2Int cell)> sides,
            List<(FloorConfig.SpecialRoom special, int parent, Vector2Int cell)> specialSides,
            (int parent, Vector2Int cell)? shop, Vector2Int? exchange)
        {
            var layout = Build(config, random, slots, path, sides, specialSides, shop, exchange, boss.room);
            layout.BossChoice = boss;
            return layout;
        }

        static FloorLayout Build(FloorConfig config, System.Random random, List<Slot> slots, List<Vector2Int> path,
            List<(int parent, Vector2Int cell)> sides, List<(FloorConfig.SpecialRoom special, int parent, Vector2Int cell)> specialSides,
            (int parent, Vector2Int cell)? shop, Vector2Int? exchange, RoomTemplate bossRoom)
        {
            var layout = new FloorLayout();
            var rewardAfter = new HashSet<int>(config.RewardAfter ?? new int[0]);
            RoomTemplate previousCombat = null;
            var bySlot = new LayoutRoom[slots.Count];
            var encounters = PickEncounters(config, random, slots);

            for (int s = 0; s < slots.Count; s++)
            {
                var slot = slots[s];
                int first = slot.PathIndex, last = slot.PathIndex + (slot.Tall ? 1 : 0);
                var room = new LayoutRoom
                {
                    Cell = path[last],
                    Role = slot.Role,
                    Depth = slot.Depth,
                    Special = slot.Special,
                    Tall = slot.Tall,
                };

                switch (slot.Role)
                {
                    case RoomRole.Start:
                        room.Template = config.GeneratesCaves ? Cave(layout, random, config.SmallCaveSizes, config.StartRoom, room) : config.StartRoom;
                        break;
                    case RoomRole.Boss:
                        room.Template = bossRoom;
                        break;
                    case RoomRole.Transition:
                        room.Template = config.TransitionRoom;
                        break;
                    case RoomRole.Antechamber:
                        // En línea con el jefe: el túnel vertical si se llega de arriba o de abajo, el horizontal si no.
                        var into = path[first] - path[first - 1];
                        room.Template = into.x == 0 ? config.AntechamberVertical : config.AntechamberHorizontal;
                        break;
                    case RoomRole.Special:
                        room.Template = slot.Special.Value.template;
                        break;
                    default:
                        if (slot.Special.HasValue)
                            room.Template = slot.Special.Value.template;
                        else
                        {
                            // Dos salas seguidas del camino no repiten forma, si hay donde elegir.
                            var shape = Pick(random, config.CombatRooms, previousCombat);
                            if (encounters.TryGetValue(slot.Depth, out var encounter))
                                room.Encounter = encounter;
                            room.Template = !config.GeneratesCaves ? shape
                                : room.Encounter is { customCave: true } themed ? Cave(layout, random, new[] { themed.cave }, shape, room)
                                : config.CaveSpecs.Length > 0 ? Cave(layout, random, config.CaveSpecs, shape, room)
                                : Cave(layout, random, config.CaveSizes, shape, room);
                            previousCombat = shape;
                        }
                        room.Reward = rewardAfter.Contains(slot.Depth) || (slot.Special.HasValue && slot.Special.Value.rewardAtEnd);
                        break;
                }

                layout.Rooms.Add(room);
                bySlot[s] = room;
                if (s > 0)
                {
                    var previous = slots[s - 1];
                    var exit = path[previous.PathIndex + (previous.Tall ? 1 : 0)];
                    layout.Link(bySlot[s - 1], room, path[first] - exit);
                }
            }

            foreach (var (parent, cell) in sides)
            {
                var side = new LayoutRoom
                {
                    Cell = cell,
                    Role = RoomRole.Side,
                    Depth = bySlot[parent].Depth,
                    Reward = true,
                };
                var sideShape = Pick(random, config.SideRooms, null);
                side.Template = config.GeneratesCaves ? Cave(layout, random, config.SmallCaveSizes, sideShape, side) : sideShape;
                layout.Rooms.Add(side);
                layout.Link(bySlot[parent], side);
            }

            foreach (var (special, parent, cell) in specialSides)
            {
                // Su premio es lo que tiene dentro (el corazón morado, la almeja), no una elección de mutación.
                var side = new LayoutRoom { Cell = cell, Role = RoomRole.Side, Depth = bySlot[parent].Depth, Special = special, Template = special.template };
                layout.Rooms.Add(side);
                layout.Link(bySlot[parent], side);

                // Detrás del muro de raíces de la antesala: la cámara de la sala secreta, una casilla más arriba.
                if (special.secretChamber != null)
                {
                    var chamber = new LayoutRoom
                    {
                        Cell = cell + Vector2Int.up, Role = RoomRole.Side, Depth = side.Depth,
                        Special = new FloorConfig.SpecialRoom { template = special.secretChamber, placement = FloorConfig.SpecialPlacement.Side, secretReward = true },
                        Template = special.secretChamber,
                    };
                    layout.Rooms.Add(chamber);
                    layout.Link(side, chamber);
                }
            }

            if (shop.HasValue)
            {
                var (shopParent, shopCell) = shop.Value;
                var shopRoom = new LayoutRoom
                {
                    Cell = shopCell,
                    Role = RoomRole.Shop,
                    Depth = bySlot[shopParent].Depth,
                    Template = config.ShopTemplate,
                };
                layout.Rooms.Add(shopRoom);
                layout.Link(bySlot[shopParent], shopRoom);

                if (exchange.HasValue)
                {
                    var exchangeRoom = new LayoutRoom
                    {
                        Cell = exchange.Value,
                        Role = RoomRole.Exchange,
                        Depth = shopRoom.Depth,
                        Template = config.ExchangeRoom,
                    };
                    layout.Rooms.Add(exchangeRoom);
                    layout.Link(shopRoom, exchangeRoom);
                }
            }

            AddShortcuts(layout, config, random);
            return layout;
        }

        // Un atajo une salas con al menos tantas de diferencia a lo largo del camino.
        const int MinShortcutDepthGap = 3;

        /// <summary>
        /// Atajos de maleza (piso 3): pares de salas de combate (o el inicio) en celdas vecinas que el camino no unía y que
        /// distan al menos <see cref="MinShortcutDepthGap"/> salas en el camino. Solo gasta números aleatorios si el piso los pide.
        /// </summary>
        static void AddShortcuts(FloorLayout layout, FloorConfig config, System.Random random)
        {
            if (config.ShortcutCount.y <= 0)
                return;

            int wanted = RandomRange(random, config.ShortcutCount);
            var eligible = layout.Rooms
                .Where(room => !room.Tall && !room.Special.HasValue && room.Role is RoomRole.Start or RoomRole.Combat)
                .ToList();
            var candidates = new List<(LayoutRoom shallow, LayoutRoom deep)>();
            for (int i = 0; i < eligible.Count; i++)
            {
                for (int j = i + 1; j < eligible.Count; j++)
                {
                    var a = eligible[i];
                    var b = eligible[j];
                    var step = a.Cell - b.Cell;
                    if (Mathf.Abs(step.x) + Mathf.Abs(step.y) != 1 || Mathf.Abs(a.Depth - b.Depth) < MinShortcutDepthGap)
                        continue;
                    if (layout.Links.Any(link => (link.from == a && link.to == b) || (link.from == b && link.to == a)))
                        continue;
                    candidates.Add(a.Depth <= b.Depth ? (a, b) : (b, a));
                }
            }

            foreach (var (shallow, deep) in candidates.OrderBy(_ => random.Next()).Take(wanted))
                layout.LinkShortcut(shallow, deep);
        }

        /// <summary>
        /// Cueva generada para la sala, de uno de los tamaños posibles. Si no sale ninguna que pase la comprobación, la forma
        /// de reserva del catálogo (<paramref name="fallback"/>).
        /// </summary>
        /// <summary>
        /// Salas con tema de la partida: hasta el número sorteado, cada tema una vez, en salas de combate del camino sin especial
        /// (nunca de las dos primeras ni la última). Sin temas en el piso, no gasta azar. Devuelve el tema por profundidad.
        /// </summary>
        static Dictionary<int, FloorConfig.EncounterTheme> PickEncounters(FloorConfig config, System.Random random, List<Slot> slots)
        {
            var result = new Dictionary<int, FloorConfig.EncounterTheme>();
            var themes = config.EncounterThemes;
            if (themes.Length == 0)
                return result;

            var combat = slots.Where(slot => slot.Role == RoomRole.Combat).ToList();
            int last = combat.Count == 0 ? 0 : combat.Max(slot => slot.Depth);
            var free = combat.Where(slot => !slot.Special.HasValue && slot.Depth >= FirstSpecialDepth && slot.Depth < last)
                .Select(slot => slot.Depth).ToList();
            if (config.ThemeShowcase)
            {
                // Escena de pruebas: cada tema en una sala de combate, en orden, sin gastar azar.
                var rooms = combat.Where(slot => !slot.Special.HasValue).Select(slot => slot.Depth).OrderBy(depth => depth).ToList();
                for (int i = 0; i < Mathf.Min(rooms.Count, themes.Length); i++)
                    result[rooms[i]] = themes[i];
                return result;
            }

            int wanted = Mathf.Min(RandomRange(random, config.EncounterThemeCount), themes.Length);
            foreach (var theme in themes.OrderBy(_ => random.Next()))
            {
                if (result.Count == wanted)
                    break;
                var fits = free.Where(depth => depth >= theme.minDepth).ToList();
                if (fits.Count == 0)
                    continue;
                int pick = fits[random.Next(fits.Count)];
                result[pick] = theme;
                free.Remove(pick);
            }
            return result;
        }

        static RoomTemplate Cave(FloorLayout layout, System.Random random, Vector2Int[] sizes, RoomTemplate fallback, LayoutRoom room)
        {
            if (sizes.Length == 0)
                return fallback;

            return CaveTemplate(layout, CaveGenerator.Generate(sizes[random.Next(sizes.Length)], random), fallback, room);
        }

        /// <summary>Cueva de combate de un tipo sorteado por peso (tamaño, silueta y lagos).</summary>
        static RoomTemplate Cave(FloorLayout layout, System.Random random, FloorConfig.CaveSpec[] specs, RoomTemplate fallback, LayoutRoom room)
        {
            float total = specs.Sum(spec => Mathf.Max(0.01f, spec.weight));
            float roll = (float)random.NextDouble() * total;
            var chosen = specs[specs.Length - 1];
            foreach (var spec in specs)
            {
                roll -= Mathf.Max(0.01f, spec.weight);
                if (roll <= 0f)
                {
                    chosen = spec;
                    break;
                }
            }
            return CaveTemplate(layout, CaveGenerator.Generate(chosen.size, random, true, chosen.shape, chosen.lakes), fallback, room);
        }

        static RoomTemplate CaveTemplate(FloorLayout layout, string text, RoomTemplate fallback, LayoutRoom room)
        {
            if (text == null)
            {
                Debug.LogWarning($"FloorLayoutGenerator: ninguna cueva pasó la comprobación para {room.Name}; se usa {(fallback != null ? fallback.name : "nada")}.");
                return fallback;
            }

            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.name = $"Cave_{room.Name}";
            template.SetLayout(text);
            layout.GeneratedTemplates.Add(template);
            return template;
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

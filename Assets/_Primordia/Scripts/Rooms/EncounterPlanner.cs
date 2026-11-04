using System.Collections.Generic;
using System.Linq;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Decide qué enemigos tiene una sala y dónde aparecen. Cada sala recibe un presupuesto que sube con su
    /// orden en el camino y con el tamaño de su suelo; se gasta al azar en enemigos que cuestan según su peligro.
    /// Aparecen en suelo libre, lejos de las puertas y sin pegarse a las paredes ni entre sí.
    /// </summary>
    public static class EncounterPlanner
    {
        // Distancia mínima (en tiles) de un enemigo a cualquier puerta, para no recibir al jugador encima.
        const float DoorDistance = 5f;
        // Separación mínima entre enemigos al aparecer.
        const float EnemySpacing = 2f;
        // Radio del corro en que aparece un grupo (el cardumen): cabe en la celda libre de alrededor del sitio elegido.
        const float GroupRadius = 0.7f;

        /// <summary>
        /// Presupuesto de una sala: el base más lo que sube por cada sala anterior, escalado con la raíz del
        /// área del suelo respecto a una sala normal. Al menos 1.
        /// </summary>
        public static int Budget(FloorConfig config, int depth, int floorArea)
        {
            float scale = Mathf.Sqrt(floorArea / (float)config.ReferenceFloorArea);
            float budget = (config.BaseBudget + config.BudgetPerRoom * Mathf.Max(0, depth - 1)) * scale;
            return Mathf.Max(1, Mathf.RoundToInt(budget));
        }

        /// <summary>
        /// Enemigos elegidos al azar hasta gastar el presupuesto (o hasta que no quepa ninguno), sin pasar del tope por
        /// sala de cada uno.
        /// </summary>
        public static List<Enemy> PickEnemies(FloorConfig config, int depth, int budget, System.Random random, bool roomHasMud = true)
        {
            var result = new List<Enemy>();
            var entries = config.Enemies;
            // Cuántos lleva ya de cada entrada, para su tope por sala.
            var picked = new int[entries.Length];
            while (budget > 0)
            {
                var affordable = Enumerable.Range(0, entries.Length)
                    .Where(i => entries[i].prefab != null && entries[i].cost <= budget && depth >= entries[i].minDepth
                                && (entries[i].maxPerRoom == 0 || picked[i] < entries[i].maxPerRoom)
                                && (!entries[i].needsMud || roomHasMud))
                    .ToList();
                if (affordable.Count == 0)
                    break;

                int index = affordable[random.Next(affordable.Count)];
                result.Add(entries[index].prefab);
                picked[index]++;
                budget -= entries[index].cost;
            }
            return result;
        }

        /// <summary>
        /// Crea el enemigo elegido en esa posición; si su entrada sale en grupo (el cardumen), crea el grupo entero en un
        /// corro pequeño alrededor. Devuelve los creados.
        /// </summary>
        public static List<Enemy> Spawn(FloorConfig config, Enemy prefab, Vector2 position, Transform parent)
        {
            int size = 1;
            foreach (var entry in config.Enemies)
            {
                if (entry.prefab == prefab)
                {
                    size = entry.GroupSize;
                    break;
                }
            }

            var result = new List<Enemy>();
            for (int i = 0; i < size; i++)
            {
                var offset = size == 1 ? Vector2.zero
                    : (Vector2)(Quaternion.Euler(0f, 0f, 360f * i / size) * Vector2.right) * GroupRadius;
                result.Add(Object.Instantiate(prefab, position + offset, Quaternion.identity, parent));
            }
            return result;
        }

        /// <summary>
        /// Celdas de la plantilla donde puede aparecer un enemigo: suelo rodeado de suelo (sin pegarse a la
        /// pared) y lejos de las puertas que se usan.
        /// </summary>
        public static List<Vector2Int> SpawnCells(RoomTemplate template, IEnumerable<Vector2Int> doorCells)
        {
            var doors = doorCells.ToList();
            var result = new List<Vector2Int>();
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!SurroundedByFloor(template, cell) || NearMud(template, cell))
                        continue;
                    if (doors.Any(door => Vector2.Distance(door, cell) < DoorDistance))
                        continue;
                    result.Add(cell);
                }
            }
            return result;
        }

        /// <summary>True si la sala tiene lodo profundo.</summary>
        public static bool HasMud(RoomTemplate template)
        {
            for (int y = 0; y < template.Height; y++)
                for (int x = 0; x < template.Width; x++)
                    if (template.Get(x, y) == RoomTemplate.Mark.Mud)
                        return true;
            return false;
        }

        /// <summary>
        /// Celdas junto al lodo profundo (suelo a 2–3 tiles de él, sin pisarlo ni pegarse a la orilla) y lejos de las puertas:
        /// donde salen los enemigos que viven del lodo, para que puedan meterse en él.
        /// </summary>
        public static List<Vector2Int> MudSideCells(RoomTemplate template, IEnumerable<Vector2Int> doorCells)
        {
            var doors = doorCells.ToList();
            var result = new List<Vector2Int>();
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (template.Get(x, y) != RoomTemplate.Mark.Floor || MudWithin(template, cell, 1) || !MudWithin(template, cell, 3))
                        continue;
                    if (doors.Any(door => Vector2.Distance(door, cell) < DoorDistance))
                        continue;
                    result.Add(cell);
                }
            }
            return result;
        }

        /// <summary>
        /// Reparte los enemigos en sitios: los que necesitan lodo, junto a él; los demás, en <paramref name="floorCells"/>.
        /// Los que no caben se quedan fuera. Posiciones en coordenadas de la plantilla.
        /// </summary>
        public static List<(Enemy enemy, Vector2 position)> Place(FloorConfig config, RoomTemplate template, IEnumerable<Vector2Int> doorCells,
            List<Enemy> enemies, List<Vector2Int> floorCells, System.Random random, System.Func<Vector2, bool> onWetGround = null,
            IReadOnlyList<Vector2> fernSpots = null)
        {
            var result = new List<(Enemy, Vector2)>();
            var mudEnemies = enemies.Where(enemy => NeedsMud(config, enemy)).ToList();
            var otherEnemies = enemies.Where(enemy => !NeedsMud(config, enemy)).ToList();

            // Los que se esconden en un helecho salen donde hay uno, lejos de las puertas y unos de otros; si no hay helechos
            // suficientes, salen en suelo libre como cualquiera.
            var doors = doorCells.ToList();
            var hiders = otherEnemies.Where(enemy => HidesInFern(config, enemy)).ToList();
            otherEnemies = otherEnemies.Where(enemy => !HidesInFern(config, enemy)).ToList();
            var usedFerns = new List<Vector2>();
            if (fernSpots != null)
            {
                var shuffled = fernSpots.Where(spot => doors.All(door => Vector2.Distance(door, spot) >= DoorDistance))
                    .OrderBy(_ => random.Next()).ToList();
                foreach (var hider in hiders.ToList())
                {
                    var spot = shuffled.Cast<Vector2?>().FirstOrDefault(s => usedFerns.All(used => Vector2.Distance(used, s.Value) >= EnemySpacing));
                    if (spot == null)
                        break;
                    usedFerns.Add(spot.Value);
                    result.Add((hider, spot.Value));
                    hiders.Remove(hider);
                }
            }
            otherEnemies.AddRange(hiders);

            // Las plantas (dryGround) solo salen sobre tierra: no sobre charcos de lodo o de agua. Se colocan primero, en los sitios
            // secos, y los demás reparten lo que queda.
            var plants = otherEnemies.Where(enemy => IsDryGroundOnly(config, enemy)).ToList();
            otherEnemies = otherEnemies.Where(enemy => !IsDryGroundOnly(config, enemy)).ToList();
            var dryCells = onWetGround == null ? floorCells : floorCells.Where(cell => !onWetGround(cell + new Vector2(0.5f, 0.5f))).ToList();
            var plantSpots = Positions(dryCells, plants.Count, random);
            for (int i = 0; i < plantSpots.Count; i++)
                result.Add((plants[i], plantSpots[i]));

            var free = floorCells.Where(cell => !plantSpots.Any(spot => Vector2.Distance(spot, cell + new Vector2(0.5f, 0.5f)) < EnemySpacing)).ToList();
            var others = Positions(free, otherEnemies.Count, random);
            for (int i = 0; i < others.Count; i++)
                result.Add((otherEnemies[i], others[i]));
            if (mudEnemies.Count > 0)
            {
                var mud = Positions(MudSideCells(template, doorCells), mudEnemies.Count, random);
                for (int i = 0; i < mud.Count; i++)
                    result.Add((mudEnemies[i], mud[i]));
            }
            return result;
        }

        static bool IsDryGroundOnly(FloorConfig config, Enemy prefab) => config.Enemies.Any(entry => entry.prefab == prefab && entry.dryGround);

        static bool HidesInFern(FloorConfig config, Enemy prefab) => config.Enemies.Any(entry => entry.prefab == prefab && entry.hidesInFern);

        static bool NeedsMud(FloorConfig config, Enemy prefab) => config.Enemies.Any(entry => entry.prefab == prefab && entry.needsMud);

        static bool MudWithin(RoomTemplate template, Vector2Int cell, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
                for (int dy = -distance; dy <= distance; dy++)
                    if (template.Get(cell.x + dx, cell.y + dy) == RoomTemplate.Mark.Mud)
                        return true;
            return false;
        }

        /// <summary>
        /// Elige una celda para cada enemigo, separadas entre sí. Si no caben todos con la separación mínima,
        /// devuelve los que caben. Las posiciones son el centro de la celda, en coordenadas de la plantilla.
        /// </summary>
        public static List<Vector2> Positions(List<Vector2Int> candidates, int count, System.Random random)
        {
            var shuffled = candidates.OrderBy(_ => random.Next()).ToList();
            var chosen = new List<Vector2>();
            foreach (var cell in shuffled)
            {
                if (chosen.Count == count)
                    break;

                var center = cell + new Vector2(0.5f, 0.5f);
                if (chosen.All(other => Vector2.Distance(other, center) >= EnemySpacing))
                    chosen.Add(center);
            }
            return chosen;
        }

        // Nadie aparece a menos de 2 tiles del lodo profundo: ni en la orilla ni en las islas (que no se alcanzan andando).
        static bool NearMud(RoomTemplate template, Vector2Int cell)
        {
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (template.Get(cell.x + dx, cell.y + dy) == RoomTemplate.Mark.Mud)
                        return true;
            return false;
        }

        static bool SurroundedByFloor(RoomTemplate template, Vector2Int cell)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (template.Get(cell.x + dx, cell.y + dy) != RoomTemplate.Mark.Floor)
                        return false;
                }
            }
            return true;
        }
    }
}

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
        public static List<Enemy> PickEnemies(FloorConfig config, int depth, int budget, System.Random random)
        {
            var result = new List<Enemy>();
            var entries = config.Enemies;
            // Cuántos lleva ya de cada entrada, para su tope por sala.
            var picked = new int[entries.Length];
            while (budget > 0)
            {
                var affordable = Enumerable.Range(0, entries.Length)
                    .Where(i => entries[i].prefab != null && entries[i].cost <= budget && depth >= entries[i].minDepth
                                && (entries[i].maxPerRoom == 0 || picked[i] < entries[i].maxPerRoom))
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
                    if (!SurroundedByFloor(template, cell))
                        continue;
                    if (doors.Any(door => Vector2.Distance(door, cell) < DoorDistance))
                        continue;
                    result.Add(cell);
                }
            }
            return result;
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

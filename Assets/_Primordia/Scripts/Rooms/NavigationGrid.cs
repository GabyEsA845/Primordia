using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Rejilla de navegación del piso (una casilla por tile): por dónde se puede caminar, si hay línea de visión
    /// entre dos puntos y por dónde ir hacia un objetivo rodeando paredes y columnas. Para lo último guarda un
    /// mapa de distancias al objetivo (recorrido en anchura), que se rehace solo cuando el objetivo cambia de
    /// casilla y sirve para todos los enemigos a la vez.
    /// </summary>
    public class NavigationGrid
    {
        /// <summary>Rejilla del piso actual. Null si no hay piso generado (escenas de prueba): se va en línea recta.</summary>
        public static NavigationGrid Current { get; set; }

        /// <summary>
        /// Rejilla del piso actual en la que las fosas se cruzan: la de los enemigos que nadan, y la de la línea de visión de
        /// todos (se ve y se dispara por encima de una fosa). Null si no hay piso generado.
        /// </summary>
        public static NavigationGrid Swimming { get; set; }

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            Swimming = null;
        }

        const int Unreachable = int.MaxValue;
        // Paso al comprobar la línea de visión, en unidades.
        const float SightStep = 0.2f;

        static readonly Vector2Int[] Neighbours4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        static readonly Vector2Int[] Neighbours8 =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        readonly RectInt bounds;
        readonly bool[] walkable;
        readonly int[] distance;
        readonly Queue<Vector2Int> queue = new();
        Vector2Int flowTarget = new(int.MinValue, int.MinValue);

        public RectInt Bounds => bounds;

        /// <summary>Se puede caminar por todo lo que no es pared, obstáculo ni fosa (suelo, pasillos y huecos de puerta).</summary>
        public NavigationGrid(FloorMap map) : this(map.Bounds, cell => map.Get(cell) is not (FloorMap.Kind.Wall or FloorMap.Kind.Obstacle or FloorMap.Kind.Pit))
        {
        }

        /// <summary>Para los que nadan: como la normal, pero las fosas se cruzan por encima.</summary>
        public static NavigationGrid ForSwimmers(FloorMap map) =>
            new(map.Bounds, cell => map.Get(cell) is not (FloorMap.Kind.Wall or FloorMap.Kind.Obstacle));

        public NavigationGrid(RectInt bounds, Func<Vector2Int, bool> isWalkable)
        {
            this.bounds = bounds;
            walkable = new bool[bounds.width * bounds.height];
            distance = new int[walkable.Length];
            foreach (var cell in bounds.allPositionsWithin)
                walkable[Index(cell)] = isWalkable(cell);
        }

        public static Vector2Int CellOf(Vector2 position) => Vector2Int.FloorToInt(position);

        public bool IsWalkable(Vector2Int cell) => Contains(cell) && walkable[Index(cell)];

        /// <summary>Cambia una celda mientras se juega (el suelo que se hunde en la sala del Euriptérido).</summary>
        public void SetWalkable(Vector2Int cell, bool value)
        {
            if (!Contains(cell))
                return;
            walkable[Index(cell)] = value;
            // El campo de distancias guardado ya no vale.
            flowTarget = new Vector2Int(int.MinValue, int.MinValue);
        }

        /// <summary>
        /// True si se puede ir en línea recta de un punto a otro sin pisar pared. Con <paramref name="radius"/>,
        /// comprueba también dos líneas paralelas a esa distancia, para que quepa un cuerpo de ese radio.
        /// </summary>
        public bool HasLineOfSight(Vector2 from, Vector2 to, float radius = 0f)
        {
            var delta = to - from;
            float length = delta.magnitude;
            if (length < 0.0001f)
                return IsWalkable(CellOf(from));

            var side = new Vector2(-delta.y, delta.x) / length * radius;
            int steps = Mathf.CeilToInt(length / SightStep);
            for (int i = 0; i <= steps; i++)
            {
                var point = from + delta * (i / (float)steps);
                if (!IsWalkable(CellOf(point)))
                    return false;
                if (radius > 0f && (!IsWalkable(CellOf(point + side)) || !IsWalkable(CellOf(point - side))))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Dirección (normalizada) para ir desde <paramref name="from"/> hacia <paramref name="target"/>: recta si
        /// cabe un cuerpo de ese radio sin tocar pared; si no, hacia la casilla vecina más cercana al objetivo
        /// según el mapa de distancias. Si no hay camino, recta.
        /// </summary>
        public Vector2 DirectionTo(Vector2 from, Vector2 target, float radius)
        {
            var straight = (target - from).normalized;
            if (HasLineOfSight(from, target, radius))
                return straight;

            UpdateFlow(CellOf(target));
            var cell = CellOf(from);
            if (!IsWalkable(cell) || distance[Index(cell)] == Unreachable)
                return straight;

            var best = cell;
            int bestDistance = distance[Index(cell)];
            foreach (var offset in Neighbours8)
            {
                var next = cell + offset;
                if (!IsWalkable(next))
                    continue;
                // En diagonal solo si las dos casillas de al lado también son suelo: no se cortan esquinas.
                if (offset.x != 0 && offset.y != 0
                    && (!IsWalkable(cell + new Vector2Int(offset.x, 0)) || !IsWalkable(cell + new Vector2Int(0, offset.y))))
                    continue;

                int d = distance[Index(next)];
                if (d < bestDistance)
                {
                    best = next;
                    bestDistance = d;
                }
            }

            if (best == cell)
                return straight;

            var toNext = best + new Vector2(0.5f, 0.5f) - from;
            return toNext.sqrMagnitude > 0.0001f ? toNext.normalized : straight;
        }

        /// <summary>Pasos (en casillas) desde esa casilla hasta el objetivo del mapa de distancias actual.</summary>
        public int StepsTo(Vector2Int cell, Vector2Int target)
        {
            UpdateFlow(target);
            return IsWalkable(cell) ? distance[Index(cell)] : Unreachable;
        }

        /// <summary>Rehace el mapa de distancias si el objetivo cambió de casilla.</summary>
        void UpdateFlow(Vector2Int target)
        {
            // Si el objetivo pisa una casilla de pared (el borde de su cuerpo), se usa la vecina libre.
            if (!IsWalkable(target))
            {
                foreach (var offset in Neighbours8)
                {
                    if (IsWalkable(target + offset))
                    {
                        target += offset;
                        break;
                    }
                }
            }

            if (target == flowTarget)
                return;
            flowTarget = target;

            Array.Fill(distance, Unreachable);
            if (!IsWalkable(target))
                return;

            distance[Index(target)] = 0;
            queue.Clear();
            queue.Enqueue(target);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                int next = distance[Index(cell)] + 1;
                foreach (var offset in Neighbours4)
                {
                    var neighbour = cell + offset;
                    if (!IsWalkable(neighbour) || distance[Index(neighbour)] <= next)
                        continue;
                    distance[Index(neighbour)] = next;
                    queue.Enqueue(neighbour);
                }
            }
        }

        bool Contains(Vector2Int cell) =>
            cell.x >= bounds.xMin && cell.x < bounds.xMax && cell.y >= bounds.yMin && cell.y < bounds.yMax;

        int Index(Vector2Int cell) => (cell.y - bounds.yMin) * bounds.width + (cell.x - bounds.xMin);
    }
}

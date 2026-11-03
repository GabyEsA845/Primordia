using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Colonizar la sala (Hongo): al empezar el combate de una sala brotan matas de hongo al azar por su suelo, que
    /// frenan y dañan a los enemigos como el micelio (<see cref="MyceliumPatch"/>). Desaparecen al limpiarla. Con
    /// Colonia grande (Moho) son más y más grandes.
    /// </summary>
    public class ColonyBenefit : MonoBehaviour
    {
        // Intentos por mata para encontrar un hueco libre de paredes y de otras matas.
        const int Attempts = 30;

        MyceliumPatch matPrefab;
        MyceliumPatch.Settings settings;
        int count;
        int sproutLimit;
        Room[] rooms = System.Array.Empty<Room>();
        Room pendingRoom;
        readonly List<MyceliumPatch> mats = new();

        /// <summary>Matas que hay ahora (para las pruebas).</summary>
        public IReadOnlyList<MyceliumPatch> Mats => mats;

        public void Configure(MyceliumPatch newMatPrefab, MyceliumPatch.Settings newSettings, int newCount)
        {
            Unsubscribe();
            matPrefab = newMatPrefab;
            settings = newSettings;
            count = Mathf.Max(0, newCount);

            rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            foreach (var room in rooms)
            {
                room.Entered += OnRoomEntered;
                room.Cleared += OnRoomCleared;
                // Si el linaje se completa en mitad de un combate, la sala también se coloniza.
                if (room.State == Room.RoomState.Combat)
                    pendingRoom = room;
            }
        }

        /// <summary>
        /// Piso 3: un enemigo que muere sobre una mata hace brotar otra junto a él, hasta ese máximo de matas en la sala
        /// (0 = sin brotes, como en el piso 1).
        /// </summary>
        public void SetSprouting(int maxMats)
        {
            Enemy.AnyDied -= OnEnemyDied;
            sproutLimit = Mathf.Max(0, maxMats);
            if (sproutLimit > 0)
                Enemy.AnyDied += OnEnemyDied;
        }

        void OnEnemyDied(Enemy enemy)
        {
            if (enemy == null || enemy.FellIntoPit || mats.Count >= sproutLimit)
                return;

            Vector2 corpse = enemy.transform.position;
            bool onMat = false;
            foreach (var mat in mats)
            {
                if (mat != null && ((Vector2)mat.transform.position - corpse).sqrMagnitude <= settings.radius * settings.radius)
                {
                    onMat = true;
                    break;
                }
            }
            if (!onMat)
                return;

            // La nueva sale a un lado, sin pisar a las otras ni las paredes.
            float spacing = settings.radius * 2f;
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                var point = corpse + Random.insideUnitCircle.normalized * Random.Range(spacing, spacing * 1.6f);
                if (!IsFree(point, spacing))
                    continue;
                Sfx.Play(Sounds.LineageColonize);
                Spawn(point);
                return;
            }
        }

        // Al entrar todavía no se sabe si la sala tiene enemigos: se mira después.
        void OnRoomEntered(Room room) => pendingRoom = room;

        void OnRoomCleared(Room _)
        {
            pendingRoom = null;
            ClearMats();
        }

        void Update()
        {
            if (pendingRoom == null)
                return;

            var room = pendingRoom;
            pendingRoom = null;
            if (room.State == Room.RoomState.Combat)
                Colonize(room);
        }

        /// <summary>Hace brotar las matas en esa sala.</summary>
        public void Colonize(Room room) =>
            Colonize(room.TryGetComponent<Collider2D>(out var area) ? area.bounds : new Bounds(room.transform.position, Vector3.one * 8f));

        /// <summary>Hace brotar las matas por esa zona (las pruebas, sin sala).</summary>
        public void Colonize(Bounds bounds)
        {
            ClearMats();
            Sfx.Play(Sounds.LineageColonize);
            float spacing = settings.radius * 2f;

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < Attempts; attempt++)
                {
                    var point = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
                    if (!IsFree(point, spacing))
                        continue;

                    Spawn(point);
                    break;
                }
            }
        }

        bool IsFree(Vector2 point, float spacing)
        {
            foreach (var mat in mats)
            {
                if (mat != null && ((Vector2)mat.transform.position - point).sqrMagnitude < spacing * spacing)
                    return false;
            }

            // Toda la mata sobre el suelo: el centro y cuatro puntos de su borde.
            var grid = NavigationGrid.Current;
            if (grid == null)
                return true;

            float r = settings.radius * 0.8f;
            foreach (var offset in new[] { Vector2.zero, new Vector2(r, 0f), new Vector2(-r, 0f), new Vector2(0f, r), new Vector2(0f, -r) })
            {
                if (!grid.IsWalkable(NavigationGrid.CellOf(point + offset)))
                    return false;
            }
            return true;
        }

        void Spawn(Vector2 point)
        {
            // Sin prefab (pruebas, o antes de importar el arte) la mata funciona igual, sin dibujo.
            var mat = matPrefab != null
                ? Instantiate(matPrefab, point, Quaternion.identity)
                : new GameObject("FungusMat").AddComponent<MyceliumPatch>();
            mat.transform.position = point;
            mat.Configure(settings);
            mats.Add(mat);
        }

        void ClearMats()
        {
            foreach (var mat in mats)
            {
                if (mat != null)
                    Destroy(mat.gameObject);
            }
            mats.Clear();
        }

        void Unsubscribe()
        {
            foreach (var room in rooms)
            {
                if (room == null)
                    continue;
                room.Entered -= OnRoomEntered;
                room.Cleared -= OnRoomCleared;
            }
        }

        void OnDestroy()
        {
            Enemy.AnyDied -= OnEnemyDied;
            Unsubscribe();
            ClearMats();
        }
    }
}

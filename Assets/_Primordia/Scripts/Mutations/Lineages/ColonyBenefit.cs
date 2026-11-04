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
        MyceliumPatch[] mediumMats = System.Array.Empty<MyceliumPatch>();
        MyceliumPatch[] largeMats = System.Array.Empty<MyceliumPatch>();
        float largeScale = 1.42f;
        float matScale = 1f;
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
        /// Piso 3: las matas nuevas (4 especies, cada una mediana y grande). Las del principio del combate son medianas y las que
        /// brotan de un enemigo muerto, grandes (el radio de las grandes es el de las medianas por <paramref name="newLargeScale"/>).
        /// </summary>
        public void SetVariants(MyceliumPatch[] medium, MyceliumPatch[] large, float newLargeScale, float newMatScale = 1f)
        {
            mediumMats = medium ?? System.Array.Empty<MyceliumPatch>();
            largeMats = large ?? System.Array.Empty<MyceliumPatch>();
            largeScale = Mathf.Max(1f, newLargeScale);
            matScale = Mathf.Max(1f, newMatScale);
        }

        // Tamaño general de las matas nuevas (dibujo y radio): solo se aplica a las variantes del piso 3, no a la mata de siempre.
        float VariantScale => mediumMats.Length > 0 || largeMats.Length > 0 ? matScale : 1f;

        // Radio de una mata nueva: el de los ajustes por el tamaño general y, si es grande, por su escala.
        float MatRadius(bool large) => settings.radius * VariantScale * (large ? largeScale : 1f);

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
                float reach = mat != null ? mat.Current.radius : 0f;
                if (mat != null && ((Vector2)mat.transform.position - corpse).sqrMagnitude <= reach * reach)
                {
                    onMat = true;
                    break;
                }
            }
            if (!onMat)
                return;

            // La nueva sale a un lado, sin pisar a las otras ni las paredes (grande si hay matas grandes).
            bool large = largeMats.Length > 0;
            float radius = MatRadius(large);
            float spacing = radius * 2f;
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                var point = corpse + Random.insideUnitCircle.normalized * Random.Range(spacing, spacing * 1.6f);
                if (!IsFree(point, radius))
                    continue;
                Sfx.Play(Sounds.LineageColonize);
                Spawn(point, large);
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

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < Attempts; attempt++)
                {
                    var point = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
                    if (!IsFree(point, MatRadius(false)))
                        continue;

                    Spawn(point);
                    break;
                }
            }
        }

        // Libre de otras matas (sin solaparse) y de paredes, para una mata de ese radio.
        bool IsFree(Vector2 point, float radius)
        {
            foreach (var mat in mats)
            {
                if (mat == null)
                    continue;
                float apart = radius + mat.Current.radius;
                if (((Vector2)mat.transform.position - point).sqrMagnitude < apart * apart)
                    return false;
            }

            // Toda la mata sobre el suelo: el centro y cuatro puntos de su borde.
            var grid = NavigationGrid.Current;
            if (grid == null)
                return true;

            float r = radius * 0.8f;
            foreach (var offset in new[] { Vector2.zero, new Vector2(r, 0f), new Vector2(-r, 0f), new Vector2(0f, r), new Vector2(0f, -r) })
            {
                if (!grid.IsWalkable(NavigationGrid.CellOf(point + offset)))
                    return false;
            }
            return true;
        }

        void Spawn(Vector2 point, bool large = false)
        {
            // Piso 3: una especie al azar, mediana o grande. Sin variantes, la mata de siempre.
            var variants = large ? largeMats : mediumMats;
            var prefab = variants.Length > 0 ? variants[Random.Range(0, variants.Length)] : matPrefab;
            var mat = prefab != null
                ? Instantiate(prefab, point, Quaternion.identity)
                // Sin prefab (pruebas, o antes de importar el arte) la mata funciona igual, sin dibujo.
                : new GameObject("FungusMat").AddComponent<MyceliumPatch>();
            mat.transform.position = point;
            var matSettings = settings;
            if (variants.Length > 0)
            {
                // El dibujo y el radio crecen a la vez con el tamaño general.
                matSettings.radius = MatRadius(large);
                mat.transform.localScale = Vector3.one * VariantScale;
            }
            mat.Configure(matSettings);
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

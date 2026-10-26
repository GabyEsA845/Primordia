using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Marcas del frenesí del Anomalocaris en el suelo: la trayectoria de sus zarpazos (flechas rojizas en mosaico, una
    /// tira por tramo) y el área del mordisco (círculo). Se ven en la oscuridad (material sin luz) y se quitan con
    /// <see cref="Clear"/> al lanzarse.
    /// </summary>
    public class AnomalocarisMarkers : MonoBehaviour
    {
        // Sobre el suelo y sus grietas, por debajo de los obstáculos y los personajes.
        public const int SortingOrder = -7;

        // Alto de la tira de flechas: 8 px a 16 px por unidad.
        const float PathWidth = 0.5f;
        const float FrameDuration = 0.1f;

        /// <summary>Cuántas tiras de trayectoria hay (una por tramo).</summary>
        public int PathPieces { get; private set; }

        public bool HasArea { get; private set; }

        /// <summary>Crea las marcas: la trayectoria que une <paramref name="path"/> y el área en <paramref name="area"/>.</summary>
        public static AnomalocarisMarkers Create(Transform parent, IReadOnlyList<Vector2> path, Vector2 area, float areaRadius,
            Sprite[] pathFrames, Sprite[] areaFrames, Material material)
        {
            var root = new GameObject("AnomalocarisMarkers");
            root.transform.SetParent(parent, false);
            var markers = root.AddComponent<AnomalocarisMarkers>();
            for (int i = 0; i + 1 < path.Count; i++)
                markers.AddPath(path[i], path[i + 1], pathFrames, material);
            markers.AddArea(area, areaRadius, areaFrames, material);
            return markers;
        }

        void AddPath(Vector2 from, Vector2 to, Sprite[] frames, Material material)
        {
            var delta = to - from;
            float length = delta.magnitude;
            if (length < 0.05f)
                return;
            Part("Path", (from + to) / 2f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, frames, material, out var renderer);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(length, PathWidth);
            PathPieces++;
        }

        void AddArea(Vector2 center, float radius, Sprite[] frames, Material material)
        {
            var area = Part("Area", center, 0f, frames, material, out var renderer);
            // El dibujo del área llena su celda: se escala para que su radio sea el del mordisco.
            if (renderer.sprite != null)
            {
                float size = renderer.sprite.bounds.size.x;
                if (size > 0f)
                    area.transform.localScale = Vector3.one * (radius * 2f / size);
            }
            HasArea = true;
        }

        GameObject Part(string name, Vector2 position, float angle, Sprite[] frames, Material material, out SpriteRenderer renderer)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);
            part.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            renderer = part.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortingOrder;
            if (material != null)
                renderer.sharedMaterial = material;
            if (frames != null && frames.Length > 0)
            {
                renderer.sprite = frames[0];
                if (frames.Length > 1)
                    part.AddComponent<SpriteLoop>().Configure(renderer, frames, FrameDuration, 0f);
            }
            return part;
        }

        /// <summary>Quita las marcas.</summary>
        public void Clear() => Destroy(gameObject);
    }
}

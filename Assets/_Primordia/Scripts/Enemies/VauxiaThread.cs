using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Hilo de luz de la Esponja Vauxia: una hilera de cuentas a escala real (sin estirar ningún sprite) que se enciende
    /// de la esponja al enemigo que cura y se apaga detrás. Sigue al curado si se mueve. Se destruye al acabar.
    /// </summary>
    public class VauxiaThread : MonoBehaviour
    {
        // Una cuenta cada 4 px; el hilo tarda lo mismo en llegar sea cual sea la distancia.
        const float Spacing = 0.25f;
        const float TravelTime = 0.18f;
        const float FrameTime = 0.06f;
        const int SortingOrder = 6;

        Sprite[] frames;
        Transform from;
        Transform to;
        Vector2 fromPoint;
        Vector2 toPoint;
        float start;
        readonly List<SpriteRenderer> beads = new();

        /// <summary>Lanza un hilo de <paramref name="source"/> a <paramref name="target"/> con esos cuadros (encendida → apagada).</summary>
        public static VauxiaThread Spawn(Sprite[] frames, Transform source, Transform target)
        {
            if (frames == null || frames.Length == 0 || source == null || target == null)
                return null;
            var thread = new GameObject("VauxiaThread").AddComponent<VauxiaThread>();
            thread.frames = frames;
            thread.from = source;
            thread.to = target;
            thread.fromPoint = source.position;
            thread.toPoint = target.position;
            thread.start = Time.time;
            return thread;
        }

        void LateUpdate()
        {
            // Si alguno desaparece, el hilo se queda donde estaba.
            if (from != null)
                fromPoint = from.position;
            if (to != null)
                toPoint = to.position;

            Vector2 line = toPoint - fromPoint;
            int count = Mathf.Max(1, Mathf.FloorToInt(line.magnitude / Spacing));
            float age = Time.time - start;
            float life = FrameTime * frames.Length;
            bool alive = false;

            for (int i = 0; i < count; i++)
            {
                // Cada cuenta se enciende cuando el hilo llega a ella y se apaga en sus cuadros.
                float lit = age - TravelTime * (i + 1) / count;
                var bead = Bead(i);
                if (lit < 0f || lit >= life)
                {
                    bead.enabled = false;
                    alive |= lit < 0f;
                    continue;
                }
                alive = true;
                bead.enabled = true;
                bead.sprite = frames[Mathf.Min(frames.Length - 1, (int)(lit / FrameTime))];
                bead.transform.position = fromPoint + line * ((i + 1f) / count);
            }
            for (int i = count; i < beads.Count; i++)
                beads[i].enabled = false;

            if (!alive)
                Destroy(gameObject);
        }

        SpriteRenderer Bead(int index)
        {
            while (beads.Count <= index)
            {
                var bead = new GameObject("Bead").AddComponent<SpriteRenderer>();
                bead.transform.SetParent(transform, false);
                bead.sortingOrder = SortingOrder;
                bead.enabled = false;
                beads.Add(bead);
            }
            return beads[index];
        }
    }
}

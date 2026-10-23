using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Para objetivos cuyo cuerpo no está donde está su vida (la ameba gigante: la vida en un objeto quieto y los
    /// cuerpos moviéndose como hijos): dice dónde van sus marcas de estado.
    /// </summary>
    public interface IStatusAnchor
    {
        /// <summary>Centro del cuerpo que lleva las marcas, en el mundo.</summary>
        Vector3 MarkerOrigin { get; }

        /// <summary>Altura de la parte de arriba de ese cuerpo sobre su centro.</summary>
        float MarkerTop { get; }
    }

    /// <summary>
    /// Marcas de estado de un objetivo (veneno, sangrado, cegado, aturdido, paralizado): efectos en bucle encima de
    /// su cabeza, una al lado de la otra, mientras dura cada estado. También efectos sobre todo el cuerpo (los
    /// rayos de la parálisis), centrados. Se añade solo al objetivo la primera vez que se muestra una marca.
    /// </summary>
    public class StatusMarkers : MonoBehaviour
    {
        // Separación entre marcas (14 px) y altura del centro de la marca sobre la parte de arriba del cuerpo.
        const float Spacing = 14f / 16f;
        const float HeightAboveBody = 0.45f;
        // Sin colisionador, la parte de arriba del cuerpo se supone a esta altura del centro.
        const float DefaultTop = 0.25f;

        class Marker
        {
            public GameObject Effect;
            public float Until;
            public bool Overhead;
        }

        readonly Dictionary<string, Marker> markers = new();
        readonly List<string> expired = new();
        Health health;
        Collider2D body;
        IStatusAnchor anchor;
        Transform holder;

        public int Count => markers.Count;

        /// <summary>
        /// Muestra la marca <paramref name="key"/> durante ese tiempo (0 = hasta <see cref="Hide"/>). Si ya está,
        /// solo alarga su duración. <paramref name="overhead"/>: encima de la cabeza; si no, centrada en el cuerpo.
        /// </summary>
        public static GameObject Show(Component target, string key, GameObject prefab, float duration, bool overhead = true)
        {
            if (target == null || prefab == null)
                return null;

            if (!target.TryGetComponent<StatusMarkers>(out var markers))
                markers = target.gameObject.AddComponent<StatusMarkers>();
            return markers.Set(key, prefab, duration, overhead);
        }

        public static void Hide(Component target, string key)
        {
            if (target != null && target.TryGetComponent<StatusMarkers>(out var markers))
                markers.Remove(key);
        }

        public bool Has(string key) => markers.ContainsKey(key);

        public GameObject EffectOf(string key) => markers.TryGetValue(key, out var marker) ? marker.Effect : null;

        void Awake()
        {
            health = GetComponent<Health>();
            holder = transform;
            if (TryGetComponent(out anchor))
            {
                holder = new GameObject("StatusMarkers").transform;
                holder.SetParent(transform, false);
            }
            foreach (var candidate in GetComponentsInChildren<Collider2D>())
            {
                if (!candidate.isTrigger)
                {
                    body = candidate;
                    break;
                }
            }
        }

        GameObject Set(string key, GameObject prefab, float duration, bool overhead)
        {
            float until = duration > 0f ? Time.time + duration : float.PositiveInfinity;
            if (markers.TryGetValue(key, out var marker) && marker.Effect != null)
            {
                marker.Until = Mathf.Max(marker.Until, until);
                return marker.Effect;
            }

            var effect = Instantiate(prefab, holder);
            markers[key] = new Marker { Effect = effect, Until = until, Overhead = overhead };
            Arrange();
            return effect;
        }

        void Remove(string key)
        {
            if (!markers.TryGetValue(key, out var marker))
                return;

            if (marker.Effect != null)
                Destroy(marker.Effect);
            markers.Remove(key);
            Arrange();
        }

        void LateUpdate()
        {
            bool dead = health != null && health.IsDead;
            expired.Clear();
            foreach (var (key, marker) in markers)
            {
                if (dead || marker.Effect == null || Time.time >= marker.Until)
                    expired.Add(key);
            }

            foreach (var key in expired)
                Remove(key);

            // Las marcas siguen al cuerpo, que se mueve por su cuenta (y cambia de tamaño al dividirse).
            if (anchor != null && markers.Count > 0)
            {
                holder.position = anchor.MarkerOrigin;
                Arrange();
            }
        }

        /// <summary>Las marcas de encima, centradas en fila sobre la cabeza; las del cuerpo, en el centro.</summary>
        void Arrange()
        {
            float top = anchor != null ? anchor.MarkerTop : body != null ? body.bounds.max.y - transform.position.y : DefaultTop;
            int overheadCount = 0;
            foreach (var marker in markers.Values)
            {
                if (marker.Overhead)
                    overheadCount++;
            }

            int index = 0;
            foreach (var marker in markers.Values)
            {
                if (marker.Effect == null)
                    continue;

                if (marker.Overhead)
                {
                    float x = (index - (overheadCount - 1) / 2f) * Spacing;
                    marker.Effect.transform.localPosition = new Vector3(x, top + HeightAboveBody, 0f);
                    index++;
                }
                else
                {
                    marker.Effect.transform.localPosition = Vector3.zero;
                }
            }
        }
    }
}

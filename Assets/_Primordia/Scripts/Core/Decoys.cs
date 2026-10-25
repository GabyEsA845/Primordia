using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Señuelos que los enemigos atacan como si fueran el jugador (las copias de la Yema clonal): cada enemigo va a por el
    /// más cercano entre su objetivo y los señuelos activos.
    /// </summary>
    public static class Decoys
    {
        static readonly List<Transform> active = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        public static int Count => active.Count;

        public static void Add(Transform decoy)
        {
            if (decoy != null && !active.Contains(decoy))
                active.Add(decoy);
        }

        public static void Remove(Transform decoy) => active.Remove(decoy);

        /// <summary>El más cercano a <paramref name="from"/> entre <paramref name="target"/> y los señuelos activos.</summary>
        public static Transform Choose(Vector2 from, Transform target)
        {
            if (active.Count == 0)
                return target;

            var best = target;
            float bestDistance = target != null && target.gameObject.activeInHierarchy
                ? ((Vector2)target.position - from).sqrMagnitude
                : float.MaxValue;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var decoy = active[i];
                if (decoy == null)
                {
                    active.RemoveAt(i);
                    continue;
                }
                float distance = ((Vector2)decoy.position - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = decoy;
                }
            }
            return best;
        }
    }
}

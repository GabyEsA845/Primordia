using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Registro de lo inflamable del piso montado, para que el fuego encuentre qué prender cerca.</summary>
    public static class Flammables
    {
        static readonly List<IFlammable> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public static void Register(IFlammable flammable)
        {
            if (!all.Contains(flammable))
                all.Add(flammable);
        }

        public static void Unregister(IFlammable flammable) => all.Remove(flammable);

        /// <summary>Lo inflamable que se puede prender a menos de <paramref name="radius"/> de ese punto.</summary>
        public static List<IFlammable> Near(Vector2 position, float radius)
        {
            var result = new List<IFlammable>();
            float squared = radius * radius;
            foreach (var flammable in all)
            {
                if (flammable.CanIgnite && (flammable.Position - position).sqrMagnitude <= squared)
                    result.Add(flammable);
            }
            return result;
        }

        /// <summary>
        /// Vuelve nube tóxica el metano que toca el segmento de <paramref name="from"/> a <paramref name="to"/> (con ese grosor).
        /// Devuelve cuántos.
        /// </summary>
        public static int ToxifyAlong(Vector2 from, Vector2 to, float radius)
        {
            var found = new List<IToxifiable>();
            foreach (var flammable in all)
            {
                if (flammable is IToxifiable toxifiable && toxifiable.CanToxify && Touches(toxifiable.ToxifyBounds, from, to, radius))
                    found.Add(toxifiable);
            }
            foreach (var toxifiable in found)
                toxifiable.Toxify();
            return found.Count;
        }

        // Se prueban puntos del segmento cada 0,2 u contra la zona ensanchada por el grosor.
        static bool Touches(Bounds bounds, Vector2 from, Vector2 to, float radius)
        {
            // El centro del chorro se queda a un radio del borde cuando choca: margen de sobra para no fallar por redondeo.
            bounds.Expand(radius * 2f + 0.3f);
            float length = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / 0.2f));
            for (int i = 0; i <= steps; i++)
            {
                if (bounds.Contains(Vector2.Lerp(from, to, i / (float)steps)))
                    return true;
            }
            return false;
        }

        /// <summary>Prende todo lo inflamable que se pueda a menos de <paramref name="radius"/>. Devuelve cuántos.</summary>
        public static int IgniteNear(Vector2 position, float radius)
        {
            var near = Near(position, radius);
            foreach (var flammable in near)
                flammable.Ignite();
            return near.Count;
        }
    }
}

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

using System.Collections.Generic;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Lo que un viento puede arrastrar por el suelo (la niebla de metano, las nubes tóxicas).</summary>
    public interface IWindPushable
    {
        Vector2 Position { get; }

        /// <summary>Lo desplaza ese vector (en unidades del mundo).</summary>
        void Push(Vector2 delta);
    }

    /// <summary>
    /// Viento de un jefe (el aleteo fuerte de la Meganeura): empuja lo arrastrable y los proyectiles que están dentro de un cono.
    /// </summary>
    public static class Wind
    {
        static readonly List<IWindPushable> pushables = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => pushables.Clear();

        public static void Register(IWindPushable pushable)
        {
            if (!pushables.Contains(pushable))
                pushables.Add(pushable);
        }

        public static void Unregister(IWindPushable pushable) => pushables.Remove(pushable);

        /// <summary>True si <paramref name="point"/> está en el cono que sale de <paramref name="origin"/> hacia <paramref name="direction"/>.</summary>
        public static bool InCone(Vector2 origin, Vector2 direction, float range, float halfAngle, Vector2 point)
        {
            var offset = point - origin;
            return offset.magnitude <= range && (offset.sqrMagnitude < 0.01f || Vector2.Angle(direction, offset) <= halfAngle);
        }

        /// <summary>
        /// Un instante de viento (llámalo cada cuadro mientras sopla): arrastra <paramref name="dragSpeed"/> u/s lo arrastrable y
        /// cambia la velocidad de los proyectiles <paramref name="projectileAcceleration"/> u/s². Devuelve cuántas cosas ha movido.
        /// </summary>
        public static int Blow(Vector2 origin, Vector2 direction, float range, float halfAngle, float dragSpeed, float projectileAcceleration, float dt)
        {
            direction = direction.normalized;
            int moved = 0;
            // Copia: empujar no cambia la lista, pero una nube puede destruirse mientras tanto.
            foreach (var pushable in pushables.ToArray())
            {
                if (pushable != null && InCone(origin, direction, range, halfAngle, pushable.Position))
                {
                    pushable.Push(direction * (dragSpeed * dt));
                    moved++;
                }
            }
            foreach (var projectile in Projectile.Active)
            {
                if (projectile != null && InCone(origin, direction, range, halfAngle, projectile.transform.position))
                {
                    projectile.Nudge(direction * (projectileAcceleration * dt));
                    moved++;
                }
            }
            return moved;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Obstáculo bajo (roca, tocón, tronco pequeño: los de 1×1) que un salto del jugador puede cruzar. Las paredes y los obstáculos
    /// grandes no lo llevan y siguen parando el salto. Durante el salto las Patas de salto dejan de chocar con estos colliders
    /// (<see cref="SetPassable"/>) y, al caer, los vuelven a activar uno a uno cuando el jugador ya no los pisa (<see cref="Restore"/>).
    /// </summary>
    public class JumpableObstacle : MonoBehaviour
    {
        static readonly List<JumpableObstacle> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        Collider2D[] colliders;
        // Quién tiene este obstáculo ignorado ahora mismo (el jugador que salta).
        Collider2D ignoredBy;

        void Awake() => colliders = GetComponents<Collider2D>();

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        /// <summary>El jugador deja de chocar con todos los obstáculos bajos (true) o vuelve a chocar con todos (false).</summary>
        public static void SetPassable(Collider2D player, bool passable)
        {
            if (player == null)
                return;
            foreach (var obstacle in all)
                obstacle.Ignore(player, passable);
        }

        /// <summary>
        /// Vuelve a activar el choque con los obstáculos que el jugador ya no pisa. Devuelve cuántos siguen ignorados (el jugador
        /// cayó encima de ellos): hay que llamar otra vez más tarde.
        /// </summary>
        public static int Restore(Collider2D player)
        {
            int pending = 0;
            foreach (var obstacle in all)
            {
                if (obstacle.ignoredBy != player)
                    continue;
                if (player != null && obstacle.Overlaps(player))
                    pending++;
                else
                    obstacle.Ignore(player, false);
            }
            return pending;
        }

        void Ignore(Collider2D player, bool ignore)
        {
            if (colliders == null || player == null)
                return;
            foreach (var collider in colliders)
            {
                if (collider != null)
                    Physics2D.IgnoreCollision(player, collider, ignore);
            }
            ignoredBy = ignore ? player : null;
        }

        bool Overlaps(Collider2D player)
        {
            foreach (var collider in colliders)
            {
                if (collider != null && collider.Distance(player).distance < 0.01f)
                    return true;
            }
            return false;
        }
    }
}

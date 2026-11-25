using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Bucle de escenario que piden varias fuentes a la vez (las cucarachas que corren): suena mientras alguna lo necesite y se
    /// apaga cuando la última lo suelta. Cada fuente llama una sola vez a <see cref="Acquire"/> y una sola vez a <see cref="Release"/>.
    /// </summary>
    public static class SharedLoop
    {
        static readonly Dictionary<string, int> counts = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => counts.Clear();

        public static void Acquire(string id)
        {
            counts.TryGetValue(id, out int count);
            counts[id] = count + 1;
            if (count == 0)
                Sfx.StartLoop(id);
        }

        public static void Release(string id)
        {
            if (!counts.TryGetValue(id, out int count) || count <= 0)
                return;
            counts[id] = count - 1;
            if (count == 1)
                Sfx.StopLoop(id);
        }
    }
}

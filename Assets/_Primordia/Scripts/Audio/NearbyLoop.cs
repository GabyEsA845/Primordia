using System;
using System.Collections.Generic;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Bucle de escenario que solo suena cerca (el fuego, la niebla de metano): cada 0,25 s mira a qué distancia del jugador
    /// está la fuente más cercana y, si hay alguna dentro del alcance, enciende el bucle con un volumen que baja con la
    /// distancia; si no queda ninguna, lo apaga. Un solo gestor por sonido (<see cref="Ensure"/>), sin que cada fuente
    /// tenga que llamar a StartLoop/StopLoop. El gestor se crea la primera vez que nace una fuente y se queda en la escena.
    /// </summary>
    public class NearbyLoop : MonoBehaviour
    {
        const float CheckEvery = 0.25f;

        static readonly Dictionary<string, NearbyLoop> active = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        string id;
        Func<Vector2, float> nearest;
        float range;
        float maxVolume;
        float nextCheck;
        bool playing;
        Transform player;

        /// <summary>
        /// Deja funcionando el bucle <paramref name="id"/>. <paramref name="nearestDistance"/> da la distancia desde un punto
        /// (el jugador) a la fuente más cercana que suene ahora, o float.MaxValue si no hay ninguna.
        /// </summary>
        public static void Ensure(string id, Func<Vector2, float> nearestDistance, float range, float maxVolume = 1f)
        {
            if (active.TryGetValue(id, out var existing) && existing != null)
                return;
            var obj = new GameObject($"NearbyLoop {id}");
            var loop = obj.AddComponent<NearbyLoop>();
            loop.id = id;
            loop.nearest = nearestDistance;
            loop.range = range;
            loop.maxVolume = maxVolume;
            active[id] = loop;
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck)
                return;
            nextCheck = Time.unscaledTime + CheckEvery;

            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                player = controller != null ? controller.transform : null;
            }

            float distance = player != null ? nearest(player.position) : float.MaxValue;
            if (distance >= range)
            {
                Stop();
                return;
            }

            playing = true;
            if (AudioManager.Instance != null)
                AudioManager.Instance.StartLoop(id, maxVolume * Mathf.Clamp01(1.25f * (1f - distance / range)));
        }

        void Stop()
        {
            if (!playing)
                return;
            playing = false;
            Sfx.StopLoop(id);
        }

        void OnDisable() => Stop();
    }
}

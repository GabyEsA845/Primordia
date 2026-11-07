using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Frente de fuego (sala especial del piso 3): una sala con muchísimos helechos secos. Al empezar el combate prenden unos pocos focos,
    /// lejos entre sí y lejos del jugador, y el fuego los va extendiendo de helecho en helecho hasta juntarse. Si el incendio se apaga con
    /// helechos todavía sin quemar, prende otro foco para que la sala siga ardiendo. Lo ya quemado es seguro.
    /// </summary>
    public class FireFrontController : MonoBehaviour
    {
        const int Seeds = 4;
        const float FirstDelay = 2f;
        const float MinDistanceFromPlayer = 6f;
        const float CheckEvery = 5f;

        Room room;
        Bounds area;
        bool started;
        float nextCheck;

        /// <summary>Focos que ha prendido hasta ahora (para las pruebas).</summary>
        public int SeedsLit { get; private set; }

        internal static FireFrontController Create(Transform parent, Bounds roomBounds)
        {
            var go = new GameObject("FireFront");
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<FireFrontController>();
            controller.area = roomBounds;
            return controller;
        }

        void Start()
        {
            room = GetComponentInParent<Room>();
            Room.CombatStartedAny += OnCombatStarted;
        }

        void OnDestroy() => Room.CombatStartedAny -= OnCombatStarted;

        void OnCombatStarted(Room started)
        {
            if (started != room || this.started)
                return;
            this.started = true;
            nextCheck = Time.time + FirstDelay + CheckEvery;
            StartCoroutine(LightSeeds(FirstDelay, Seeds));
        }

        void Update()
        {
            if (!started || room == null || room.State != Room.RoomState.Combat || Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;
            // Si ya no arde nada en la sala pero quedan helechos sin quemar, otro foco.
            bool burning = GroundFire.All.Any(fire => fire.IsBurning && area.Contains(fire.transform.position));
            if (!burning && IntactFerns().Count > 0)
                StartCoroutine(LightSeeds(0f, 1));
        }

        List<Fern> IntactFerns() => room.GetComponentsInChildren<Fern>().Where(fern => fern.CanIgnite).ToList();

        IEnumerator LightSeeds(float delay, int count)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            var ferns = IntactFerns();
            var player = GameObject.FindWithTag("Player");
            Vector2 playerAt = player != null ? (Vector2)player.transform.position : (Vector2)area.center;
            // Lejos del jugador (si no queda ninguno lejos, todos valen).
            var far = ferns.Where(fern => Vector2.Distance(fern.Position, playerAt) >= MinDistanceFromPlayer).ToList();
            if (far.Count > 0)
                ferns = far;
            if (ferns.Count == 0)
                yield break;

            // Los focos lo más separados posible: el primero al azar y cada siguiente el más lejano de los ya elegidos.
            var chosen = new List<Fern> { ferns[Random.Range(0, ferns.Count)] };
            while (chosen.Count < count && chosen.Count < ferns.Count)
            {
                var next = ferns.Where(fern => !chosen.Contains(fern))
                    .OrderByDescending(fern => chosen.Min(other => Vector2.Distance(other.Position, fern.Position))).First();
                chosen.Add(next);
            }
            foreach (var fern in chosen)
            {
                fern.Ignite();
                SeedsLit++;
            }
        }
    }
}

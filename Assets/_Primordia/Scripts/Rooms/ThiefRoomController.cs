using System;
using Primordia.Biomass;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Controla la sala Ladronas: al entrar el jugador por primera vez, saca los montones de biomasa (cogibles de verdad: puedes
    /// llevártelos tú) y, de uno en uno, deja salir cucarachas de las grietas del borde de la sala. Cada una roba el montón más cercano
    /// que quede. La sala se cierra al entrar (mantiene sus puertas hasta cumplirse): cuando todas han muerto o se han ido, las
    /// puertas se abren y aparece un cofre de cono de Lepidodendron en el centro de la sala.
    /// </summary>
    public class ThiefRoomController : MonoBehaviour
    {
        const int Thieves = 3;
        const float FirstDelay = 1.5f;
        const float Gap = 2f;

        Room room;
        BiomassPickup small;
        BiomassPickup big;
        FloorConfig config;
        Vector2[] piles = Array.Empty<Vector2>();
        Vector2[] cracks = Array.Empty<Vector2>();
        Vector2 rewardAt;
        readonly System.Collections.Generic.List<BiomassPickup> pickups = new();
        readonly System.Collections.Generic.HashSet<BiomassPickup> claimed = new();
        readonly System.Collections.Generic.List<BiomassThief> thieves = new();
        bool started;
        bool released;
        bool rewarded;

        public bool Rewarded => rewarded;

        internal void Configure(FloorConfig floorConfig, BiomassPickup smallPrefab, BiomassPickup bigPrefab, Vector2[] pileSpots, Vector2[] crackSpots, Vector2 reward)
        {
            config = floorConfig;
            small = smallPrefab;
            big = bigPrefab;
            piles = pileSpots;
            cracks = crackSpots;
            rewardAt = reward;
        }

        void Start()
        {
            room = GetComponent<Room>();
            // Mientras haya cucarachas por resolver, la sala se cierra al entrar.
            if (room != null)
                room.AddHold();
            Room.PlayerEnteredAny += OnEntered;
        }

        void OnDestroy() => Room.PlayerEnteredAny -= OnEntered;

        void OnEntered(Room entered)
        {
            if (entered != room || started || small == null || cracks.Length < 2)
                return;
            started = true;
            foreach (var spot in piles)
                pickups.Add(Instantiate(small, spot, Quaternion.identity, transform));
            StartCoroutine(Release());
        }

        System.Collections.IEnumerator Release()
        {
            yield return new WaitForSeconds(FirstDelay);
            for (int i = 0; i < Thieves; i++)
            {
                // Sale de una grieta lejos de los montones y se mete en otra lejos de donde salió.
                Vector2 from = cracks[UnityEngine.Random.Range(0, cracks.Length)];
                Vector2 to = Farthest(from);
                var thief = BiomassThief.Send(transform, this, from, to, small);
                if (thief != null)
                    thieves.Add(thief);
                if (i < Thieves - 1)
                    yield return new WaitForSeconds(Gap);
            }
            released = true;
        }

        Vector2 Farthest(Vector2 from)
        {
            Vector2 best = cracks[0];
            float bestDistance = -1f;
            foreach (var crack in cracks)
            {
                float d = Vector2.Distance(crack, from);
                if (d > bestDistance)
                {
                    best = crack;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>El montón sin reclamar más cercano a ese punto (y lo reclama); null si no queda ninguno.</summary>
        internal BiomassPickup Claim(Vector2 from)
        {
            BiomassPickup best = null;
            float bestDistance = float.MaxValue;
            foreach (var pickup in pickups)
            {
                if (pickup == null || claimed.Contains(pickup))
                    continue;
                float d = Vector2.Distance(pickup.transform.position, from);
                if (d < bestDistance)
                {
                    best = pickup;
                    bestDistance = d;
                }
            }
            if (best != null)
                claimed.Add(best);
            return best;
        }

        void Update()
        {
            if (!released || rewarded)
                return;
            foreach (var thief in thieves)
            {
                if (thief != null && !thief.IsDone)
                    return;
            }
            rewarded = true;
            SpawnChest();
            if (room != null)
                room.ReleaseHold();
        }

        // El cofre de cono pequeño (1 casilla): se abre con E y suelta biomasa grande.
        void SpawnChest() => ConeChest.Spawn(transform, rewardAt, config, small, big, large: false);
    }
}

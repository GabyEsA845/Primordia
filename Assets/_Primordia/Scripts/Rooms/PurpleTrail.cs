using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Camino secreto del piso 3: tres flores moradas repartidas por el piso (<see cref="PurpleFlower"/>). Solo se ven cuando el corazón
    /// morado se rompió (el jugador lo cogió alguna vez y ya no lo tiene), mientras no tenga ya la semilla y el muro de raíces siga
    /// cerrado. Coger las tres da la semilla morada, que abre el muro como el corazón.
    /// </summary>
    public class PurpleTrail : MonoBehaviour
    {
        public const int FlowerCount = 3;
        const float CheckSeconds = 0.5f;

        readonly List<PurpleFlower> flowers = new();
        RootWall wall;
        Health health;
        float nextCheck;

        /// <summary>Flores cogidas.</summary>
        public int Taken { get; private set; }

        internal static PurpleTrail Create(Transform parent, RootWall wall) =>
            new GameObject("PurpleTrail").AddComponent<PurpleTrail>().Init(parent, wall);

        PurpleTrail Init(Transform parent, RootWall newWall)
        {
            transform.SetParent(parent, false);
            wall = newWall;
            return this;
        }

        internal void Add(PurpleFlower flower)
        {
            flower.transform.SetParent(transform, true);
            flowers.Add(flower);
            flower.gameObject.SetActive(false);
        }

        internal void OnTaken(GameObject player)
        {
            Taken++;
            if (Taken >= FlowerCount && player != null && player.TryGetComponent<Health>(out var carrier))
                carrier.PurpleSeed = true;
        }

        void Update()
        {
            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckSeconds;
            if (health == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player == null || !player.TryGetComponent(out health))
                    return;
            }
            bool broken = health.PurpleHeartFound && health.PurpleHearts == 0 && !health.PurpleSeed && Taken < FlowerCount
                && (wall == null || !wall.IsOpen);
            foreach (var flower in flowers)
            {
                if (flower != null && flower.gameObject.activeSelf != broken)
                    flower.gameObject.SetActive(broken);
            }
        }
    }
}

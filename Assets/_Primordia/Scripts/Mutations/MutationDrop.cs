using System.Collections.Generic;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Al morir, el enemigo (el jefe) deja varias mutaciones al azar en el suelo, sin las que ya lleva el
    /// jugador. Al coger una, las demás revientan: se elige 1 de 3. Caen en fila por encima del centro de
    /// la sala, lejos de la salida, que aparece en el centro.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class MutationDrop : MonoBehaviour
    {
        [SerializeField] MutationPool pool;
        [SerializeField] MutationPickup pickupPrefab;
        [SerializeField, Min(1)] int count = 3;
        [Tooltip("Distancia entre burbujas, en unidades.")]
        [SerializeField, Min(0.5f)] float spacing = 2.5f;
        [Tooltip("Altura de la fila sobre el centro de la sala, en unidades.")]
        [SerializeField] float height = 3f;

        Enemy enemy;

        /// <summary>Las burbujas que dejó al morir (vacía hasta entonces).</summary>
        public IReadOnlyList<MutationPickup> Dropped => dropped;

        readonly List<MutationPickup> dropped = new();

        void Awake() => enemy = GetComponent<Enemy>();

        void OnEnable() => enemy.Died += OnDied;

        void OnDisable() => enemy.Died -= OnDied;

        internal void Configure(MutationPool newPool, MutationPickup newPickupPrefab, int newCount = 3)
        {
            pool = newPool;
            pickupPrefab = newPickupPrefab;
            count = Mathf.Max(1, newCount);
        }

        void OnDied(Enemy _)
        {
            if (pool == null || pickupPrefab == null)
                return;

            var playerObject = GameObject.FindWithTag("Player");
            var player = playerObject != null ? playerObject.GetComponent<PlayerMutations>() : null;
            var options = pool.PickOffer(count, player);

            Vector2 center = TryGetComponent<IBoss>(out var boss) ? boss.ArenaCenter : (Vector2)transform.position;
            for (int i = 0; i < options.Count; i++)
            {
                float x = (i - (options.Count - 1) / 2f) * spacing;
                var pickup = Instantiate(pickupPrefab, center + new Vector2(x, height), Quaternion.identity, transform.parent);
                pickup.SetMutation(options[i]);
                pickup.PickedUp += OnPickedUp;
                dropped.Add(pickup);
            }
        }

        void OnPickedUp(MutationPickup chosen)
        {
            foreach (var pickup in dropped)
            {
                if (pickup != null && pickup != chosen)
                    pickup.Burst();
            }
            dropped.Clear();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Banco de pruebas (escena Test_Lineages): al empezar, pone en la sala de inicio varias burbujas de cada
    /// mutación, en columnas agrupadas por ranura, para probar mutaciones, niveles y linajes. Cada burbuja va suelta:
    /// coger una no revienta las demás, y coger la misma otra vez la sube de nivel.
    /// </summary>
    public class MutationBench : MonoBehaviour
    {
        [SerializeField] MutationPickup pickupPrefab;
        [SerializeField] MutationData[] mutations = System.Array.Empty<MutationData>();
        [Tooltip("Burbujas de cada mutación (3 = hasta el nivel III).")]
        [SerializeField, Min(1)] int copies = 3;
        [Tooltip("Distancia entre columnas y entre filas, en unidades.")]
        [SerializeField] Vector2 spacing = new(1.5f, 1.4f);
        [Tooltip("Hueco extra entre los grupos de cada ranura.")]
        [SerializeField, Min(0f)] float slotGap = 1f;
        [Tooltip("Dónde empieza la fila más baja, respecto al punto de inicio del jugador.")]
        [SerializeField] float firstRow = 2f;

        readonly List<MutationPickup> spawned = new();

        public IReadOnlyList<MutationPickup> Spawned => spawned;

        internal void Configure(MutationPickup newPickupPrefab, MutationData[] newMutations, int newCopies = 3)
        {
            pickupPrefab = newPickupPrefab;
            mutations = newMutations;
            copies = newCopies;
        }

        void Start()
        {
            // El generador monta el piso en su Awake: aquí ya se sabe dónde empieza el jugador.
            var generator = FindAnyObjectByType<FloorGenerator>();
            Vector2 origin = generator != null && generator.Built != null ? generator.Built.StartPosition : (Vector2)transform.position;
            Spawn(origin);
        }

        /// <summary>Pone las burbujas encima del punto de inicio.</summary>
        public void Spawn(Vector2 origin)
        {
            var ordered = mutations.Where(m => m != null).OrderBy(m => m.Slot).ThenBy(m => m.DisplayName).ToList();
            int groups = ordered.Select(m => m.Slot).Distinct().Count();
            float width = (ordered.Count - 1) * spacing.x + (groups - 1) * slotGap;

            float x = origin.x - width / 2f;
            MutationSlot? previous = null;
            foreach (var mutation in ordered)
            {
                if (previous.HasValue && previous.Value != mutation.Slot)
                    x += slotGap;
                previous = mutation.Slot;

                for (int row = 0; row < copies; row++)
                {
                    var position = new Vector2(x, origin.y + firstRow + row * spacing.y);
                    var pickup = pickupPrefab != null
                        ? Instantiate(pickupPrefab, position, Quaternion.identity, transform)
                        : null;
                    if (pickup == null)
                        continue;

                    pickup.SetMutation(mutation);
                    spawned.Add(pickup);
                }
                x += spacing.x;
            }
        }
    }
}

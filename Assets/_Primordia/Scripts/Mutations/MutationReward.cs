using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Al limpiar su sala, ofrece al jugador elegir 1 de varias mutaciones al azar
    /// (sin las que ya tiene equipadas). Va en el mismo objeto que la <see cref="Room"/>.
    /// </summary>
    [RequireComponent(typeof(Room))]
    public class MutationReward : MonoBehaviour
    {
        [SerializeField] MutationPool pool;
        [SerializeField, Min(1)] int optionCount = 3;

        Room room;

        void Awake() => room = GetComponent<Room>();

        void OnEnable() => room.Cleared += OnRoomCleared;

        void OnDisable() => room.Cleared -= OnRoomCleared;

        void OnRoomCleared(Room _)
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (pool == null || playerObject == null || !playerObject.TryGetComponent<PlayerMutations>(out var player))
                return;

            var options = pool.PickOffer(optionCount, player.Equipped);
            if (options.Count == 0)
                return;

            var choiceUI = FindFirstObjectByType<MutationChoiceUI>(FindObjectsInactive.Include);
            if (choiceUI == null)
            {
                Debug.LogWarning("No hay MutationChoiceUI en la escena; la recompensa se omite.", this);
                return;
            }

            choiceUI.Show(options, player);
        }

        internal void Configure(MutationPool newPool, int newOptionCount)
        {
            pool = newPool;
            optionCount = newOptionCount;
        }
    }
}

using System.Collections;
using Primordia.Core;
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
        [Tooltip("Segundos entre limpiar la sala y mostrar la elección, para que el último clic de combate no elija nada.")]
        [SerializeField, Min(0f)] float delay = 1f;

        Room room;

        void Awake() => room = GetComponent<Room>();

        void OnEnable() => room.Cleared += OnRoomCleared;

        void OnDisable() => room.Cleared -= OnRoomCleared;

        void OnRoomCleared(Room _)
        {
            if (delay > 0f)
                StartCoroutine(OfferAfterDelay());
            else
                Offer();
        }

        IEnumerator OfferAfterDelay()
        {
            yield return new WaitForSeconds(delay);

            // Con la pausa o el mapa abiertos, espera a que se vuelva al juego.
            while (GameManager.Instance != null && GameManager.Instance.State is GameState.Paused or GameState.Map or GameState.Evolution)
                yield return null;

            // Si mientras tanto el jugador murió o la partida terminó, ya no hay recompensa.
            if (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing)
                Offer();
        }

        void Offer()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (pool == null || playerObject == null || !playerObject.TryGetComponent<PlayerMutations>(out var player))
                return;

            if (player.Health.IsDead)
                return;

            var options = pool.PickOffer(optionCount, player);
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

        internal void Configure(MutationPool newPool, int newOptionCount, float newDelay = 1f)
        {
            pool = newPool;
            optionCount = newOptionCount;
            delay = newDelay;
        }
    }
}

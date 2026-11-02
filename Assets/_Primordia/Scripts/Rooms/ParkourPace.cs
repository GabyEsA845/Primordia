using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// La sala Parkour (piso 2) se salta de columna en columna con la esquiva, y con muchas mejoras de velocidad se
    /// vuelve incontrolable: dentro de ella el jugador se mueve a su velocidad base, y al salir recupera la suya.
    /// </summary>
    [RequireComponent(typeof(Room))]
    public class ParkourPace : MonoBehaviour
    {
        Room room;
        PlayerController controller;

        public bool Active { get; private set; }

        void Awake() => room = GetComponent<Room>();

        void OnEnable()
        {
            Room.PlayerEnteredAny += OnEntered;
            Room.PlayerExitedAny += OnExited;
        }

        void OnDisable()
        {
            Room.PlayerEnteredAny -= OnEntered;
            Room.PlayerExitedAny -= OnExited;
            Release();
        }

        void OnEntered(Room entered)
        {
            if (entered != room)
                return;

            var player = GameObject.FindWithTag("Player");
            if (player == null || !player.TryGetComponent(out controller))
                return;

            float baseSpeed = player.TryGetComponent<PlayerMutations>(out var mutations) ? mutations.BaseMoveSpeed : 0f;
            if (baseSpeed <= 0f)
                return;

            controller.SpeedOverride = baseSpeed;
            Active = true;
        }

        void OnExited(Room exited)
        {
            if (exited == room)
                Release();
        }

        void Release()
        {
            if (controller != null)
                controller.SpeedOverride = null;
            Active = false;
        }
    }
}

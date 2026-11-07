using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Sabe si el jugador está dentro de la sala a la que pertenece (cuelga de ella): el humo del Bosque en humo solo se ve con el
    /// jugador dentro, y las chispas de la Veta de carbón solo saltan entonces.
    /// </summary>
    public class RoomPresence : MonoBehaviour
    {
        Room room;

        public bool PlayerInside { get; private set; }

        void Start()
        {
            room = GetComponentInParent<Room>();
            Room.PlayerEnteredAny += OnEntered;
            Room.PlayerExitedAny += OnExited;
        }

        void OnDestroy()
        {
            Room.PlayerEnteredAny -= OnEntered;
            Room.PlayerExitedAny -= OnExited;
        }

        void OnEntered(Room entered)
        {
            if (entered == room)
                PlayerInside = true;
        }

        void OnExited(Room exited)
        {
            if (exited == room)
                PlayerInside = false;
        }

        /// <summary>Para las pruebas: pone la presencia a mano.</summary>
        internal void Set(bool inside) => PlayerInside = inside;
    }
}

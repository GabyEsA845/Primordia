using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Salida del piso. Aparece cuando su sala queda limpia; al tocarla, el jugador gana.
    /// El generador del piso la pone en el centro de la sala del jefe, así que aparece al vencerlo.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomExit : MonoBehaviour
    {
        [SerializeField] Room room;

        public bool IsOpen { get; private set; }

        Collider2D trigger;
        SpriteRenderer sprite;

        void Awake()
        {
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            sprite = GetComponent<SpriteRenderer>();
            SetOpen(room == null);
        }

        void OnEnable()
        {
            if (room != null)
                room.Cleared += OnRoomCleared;
        }

        void OnDisable()
        {
            if (room != null)
                room.Cleared -= OnRoomCleared;
        }

        internal void SetRoom(Room newRoom)
        {
            if (room != null)
                room.Cleared -= OnRoomCleared;

            room = newRoom;
            room.Cleared += OnRoomCleared;
            SetOpen(room.State == Room.RoomState.Cleared);
        }

        void OnRoomCleared(Room _) => SetOpen(true);

        void SetOpen(bool open)
        {
            IsOpen = open;
            trigger.enabled = open;
            if (sprite != null)
                sprite.enabled = open;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (!IsOpen || body == null || !body.CompareTag("Player"))
                return;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Victory);
        }
    }
}

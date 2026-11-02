using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Teletransporte de una sala (sala inicial, tienda y antesala del jefe). Duerme hasta que el jugador entra en su
    /// sala; entonces despierta y queda activo para siempre en ese piso. Desde el mapa grande se puede viajar a
    /// cualquier teletransporte activo (ver <see cref="TeleportNetwork"/>).
    /// Hojas de <c>Teleport_Floor{N}</c>: Dormant, Wake (110 ms), Idle (130 ms) y Warp (80 ms).
    /// </summary>
    public class Teleporter : MonoBehaviour
    {
        const float WakeFrame = 0.11f;
        const float IdleFrame = 0.13f;
        const float WarpFrame = 0.08f;

        enum Mode
        {
            Dormant,
            Waking,
            Idle,
            Warping
        }

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] dormant = Array.Empty<Sprite>();
        [SerializeField] Sprite[] wake = Array.Empty<Sprite>();
        [SerializeField] Sprite[] idle = Array.Empty<Sprite>();
        [SerializeField] Sprite[] warp = Array.Empty<Sprite>();

        static readonly List<Teleporter> all = new();

        /// <summary>Los teletransportes del piso que hay ahora (activos o no).</summary>
        public static IReadOnlyList<Teleporter> All => all;

        /// <summary>Un teletransporte acaba de despertar.</summary>
        public static event Action<Teleporter> Activated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            Activated = null;
        }

        Mode mode;
        float modeStart;

        public Room Room { get; private set; }
        public RoomRole Role { get; internal set; }
        public bool IsActive { get; private set; }
        public Vector2 Position => transform.position;

        internal void Configure(SpriteRenderer newBody, Sprite[] newDormant, Sprite[] newWake, Sprite[] newIdle, Sprite[] newWarp)
        {
            body = newBody;
            dormant = newDormant ?? Array.Empty<Sprite>();
            wake = newWake ?? Array.Empty<Sprite>();
            idle = newIdle ?? Array.Empty<Sprite>();
            warp = newWarp ?? Array.Empty<Sprite>();
        }

        void OnEnable()
        {
            all.Add(this);
            Room = GetComponentInParent<Room>();
            Room.PlayerEnteredAny += OnPlayerEntered;
        }

        void OnDisable()
        {
            all.Remove(this);
            Room.PlayerEnteredAny -= OnPlayerEntered;
        }

        void Start()
        {
            Show(dormant, 0);
            // La sala inicial ya contiene al jugador al empezar: su trigger puede haberse lanzado antes de existir esto.
            if (Room != null && Room.State != Room.RoomState.Waiting)
                Activate();
        }

        void OnPlayerEntered(Room room)
        {
            if (room == Room)
                Activate();
        }

        /// <summary>Despierta el teletransporte (una sola vez).</summary>
        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            SetMode(wake.Length > 0 ? Mode.Waking : Mode.Idle);
            Activated?.Invoke(this);
        }

        /// <summary>Anima el destello del salto (al salir y al llegar).</summary>
        public void PlayWarp()
        {
            if (warp.Length > 0)
                SetMode(Mode.Warping);
        }

        void SetMode(Mode newMode)
        {
            mode = newMode;
            modeStart = Time.time;
        }

        void Update()
        {
            float elapsed = Time.time - modeStart;
            switch (mode)
            {
                case Mode.Dormant:
                    Show(dormant, 0);
                    break;
                case Mode.Waking:
                    int wakeFrame = Mathf.FloorToInt(elapsed / WakeFrame);
                    if (wakeFrame >= wake.Length)
                        SetMode(Mode.Idle);
                    else
                        Show(wake, wakeFrame);
                    break;
                case Mode.Idle:
                    Show(idle, idle.Length > 0 ? Mathf.FloorToInt(elapsed / IdleFrame) % idle.Length : 0);
                    break;
                case Mode.Warping:
                    int warpFrame = Mathf.FloorToInt(elapsed / WarpFrame);
                    if (warpFrame >= warp.Length)
                        SetMode(IsActive ? Mode.Idle : Mode.Dormant);
                    else
                        Show(warp, warpFrame);
                    break;
            }
        }

        void Show(Sprite[] frames, int index)
        {
            if (body != null && frames.Length > 0)
                body.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }
    }
}

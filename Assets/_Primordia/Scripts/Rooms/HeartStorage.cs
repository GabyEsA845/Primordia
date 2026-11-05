using System;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Almacén de corazones (desde el piso 3): la vaina de semillas de la tienda guarda los medios corazones que el jugador envía
    /// con E cuando tiene la vida llena. Capacidad de 4 medios en el piso 3 y 2 más por piso. Los corazones no salen del piso:
    /// el contador se vacía al montar cada piso. Antes del piso 3 no hay almacén.
    /// </summary>
    public static class HeartStorage
    {
        public const int FirstFloor = 3;
        public const int BaseCapacity = 4;
        public const int PerFloor = 2;

        static int floor;
        static int stored;

        /// <summary>Tecla E flotante de las piezas con aviso (la de FloorConfig.Pieces), para el corazón del suelo.</summary>
        public static Sprite[] KeyPrompt { get; set; } = Array.Empty<Sprite>();

        /// <summary>Se lanza al cambiar lo guardado (la vaina cambia de cuadro).</summary>
        public static event Action Changed;

        /// <summary>Medios corazones que caben en ese piso (0 antes del piso 3).</summary>
        public static int CapacityOf(int floorNumber) => floorNumber < FirstFloor ? 0 : BaseCapacity + PerFloor * (floorNumber - FirstFloor);

        public static int Capacity => CapacityOf(floor);
        public static int Stored => stored;
        public static bool Exists => Capacity > 0;
        public static bool IsFull => stored >= Capacity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            floor = 0;
            stored = 0;
            KeyPrompt = Array.Empty<Sprite>();
            Changed = null;
        }

        /// <summary>Empieza un piso: la vaina vacía y con la capacidad de ese piso.</summary>
        public static void Reset(int floorNumber)
        {
            floor = floorNumber;
            stored = 0;
            Changed?.Invoke();
        }

        /// <summary>Guarda un medio corazón. False si no hay almacén o está lleno.</summary>
        public static bool Store()
        {
            if (!Exists || IsFull)
                return false;
            stored++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Saca un medio corazón. False si está vacía.</summary>
        public static bool Take()
        {
            if (stored <= 0)
                return false;
            stored--;
            Changed?.Invoke();
            return true;
        }
    }
}

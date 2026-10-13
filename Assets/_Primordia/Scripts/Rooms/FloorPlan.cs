using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Una sala del piso tal como la necesita el mapa: dónde está, qué es y con qué salas conecta.</summary>
    public class MapRoom
    {
        public Room Room;
        /// <summary>Celdas que ocupa la plantilla de la sala (paredes incluidas), en coordenadas del mapa.</summary>
        public RectInt Rect;
        public RoomRole Role;
        public bool HasReward;
        public readonly List<MapRoom> Neighbors = new();
    }

    /// <summary>Pasillo entre dos salas: las celdas de suelo que hay entre sus puertas.</summary>
    public class MapCorridor
    {
        public MapRoom A;
        public MapRoom B;
        public readonly List<Vector2Int> Cells = new();
    }
}

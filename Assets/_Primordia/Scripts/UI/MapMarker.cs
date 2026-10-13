using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>Marca del mapa: recuerda su posición en el piso para colocarla en cada vista (minimapa y mapa grande).</summary>
    public class MapMarker : MonoBehaviour
    {
        internal Image Image;
        internal Vector2 FloorPosition;
    }
}

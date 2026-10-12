using Primordia.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>Muestra la semilla del piso actual (en la pausa), para poder reproducir un mapa concreto.</summary>
    [RequireComponent(typeof(Text))]
    public class SeedLabel : MonoBehaviour
    {
        Text text;

        void Awake() => text = GetComponent<Text>();

        void OnEnable() => text.text = Format(FloorGenerator.CurrentSeed);

        public static string Format(int seed) => seed == 0 ? "" : $"Semilla: {seed}";
    }
}

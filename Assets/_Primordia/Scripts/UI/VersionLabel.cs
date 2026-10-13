using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>Escribe la versión del juego (Project Settings > Player > Version) en su texto.</summary>
    [RequireComponent(typeof(Text))]
    public class VersionLabel : MonoBehaviour
    {
        void Start() => GetComponent<Text>().text = $"v{Application.version}";
    }
}

using Primordia.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Genera y monta el piso al cargar la escena de juego: cada partida (cada vez que se carga la escena) usa
    /// una semilla nueva, salvo que se fije una para reproducir un mapa. La semilla se ve en la pausa y en la consola.
    /// </summary>
    // Antes que el resto: el piso, el jugador y la cámara tienen que estar en su sitio cuando empiezan los demás.
    [DefaultExecutionOrder(-100)]
    public class FloorGenerator : MonoBehaviour
    {
        [SerializeField] FloorConfig config;
        [Tooltip("0 = semilla al azar en cada partida. Otro número reproduce siempre el mismo piso.")]
        [SerializeField] int fixedSeed;
        [SerializeField] Transform player;
        [Tooltip("Punto que sigue la cámara; se coloca sobre el jugador al empezar.")]
        [SerializeField] CameraLookAhead cameraTarget;
        [SerializeField] CinemachineCamera virtualCamera;

        /// <summary>Semilla del piso actual (0 si todavía no se ha generado ninguno).</summary>
        public static int CurrentSeed { get; private set; }

        public FloorLayout Layout { get; private set; }
        public FloorBuilder.BuiltFloor Built { get; private set; }

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => CurrentSeed = 0;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogError("FloorGenerator: falta el FloorConfig.", this);
                return;
            }

            int seed = fixedSeed != 0 ? fixedSeed : new System.Random().Next(1, int.MaxValue);
            Generate(seed);
        }

        internal void Configure(FloorConfig newConfig, Transform newPlayer, CameraLookAhead newCameraTarget,
            CinemachineCamera newCamera, int newFixedSeed = 0)
        {
            config = newConfig;
            player = newPlayer;
            cameraTarget = newCameraTarget;
            virtualCamera = newCamera;
            fixedSeed = newFixedSeed;
        }

        /// <summary>Genera y monta el piso de esa semilla, y lleva al jugador y a la cámara a la sala de inicio.</summary>
        internal void Generate(int seed)
        {
            CurrentSeed = seed;
            Debug.Log($"FloorGenerator: piso generado con la semilla {seed}.", this);

            var random = new System.Random(seed);
            Layout = FloorLayoutGenerator.Generate(config, random);
            Built = FloorBuilder.Build(Layout, config, transform, random);

            Vector3 start = Built.StartPosition;
            if (player != null)
                player.position = start;
            if (cameraTarget != null)
                cameraTarget.transform.position = start;
            if (virtualCamera != null)
            {
                // Que la cámara empiece ya en la sala de inicio, sin recorrer el piso desde donde estaba.
                virtualCamera.ForceCameraPosition(start + Vector3.back * 10f, Quaternion.identity);
                virtualCamera.PreviousStateIsValid = false;
            }
        }
    }
}

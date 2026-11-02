using System;
using System.Collections;
using Primordia.Biomass;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Genera y monta el piso al cargar la escena de juego: cada partida (cada vez que se carga la escena) usa
    /// una semilla nueva, salvo que se fije una para reproducir un mapa. La semilla se ve en la pausa y en la consola.
    /// Al saltar a la fosa de la sala de paso, <see cref="GoToNextFloor"/> borra el piso y monta el siguiente en la misma
    /// escena: el jugador es el mismo objeto, así que conserva sus mutaciones, su vida y su biomasa.
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

        /// <summary>Número del piso actual (1 = sopa primordial). 0 si todavía no se ha generado ninguno.</summary>
        public static int CurrentFloor { get; internal set; }

        /// <summary>Se lanza cada vez que se monta un piso nuevo (también el primero), con su número.</summary>
        public static event Action<int> FloorChanged;

        [Tooltip("Segundos del fundido desde negro al llegar al piso siguiente.")]
        [SerializeField, Min(0f)] float arrivalFade = 0.8f;

        public FloorConfig Config => config;

        /// <summary>True si la fosa de este piso lleva a otro (si no, da la victoria).</summary>
        public bool HasNextFloor => config != null && config.NextFloor != null;

        /// <summary>True mientras se cambia de piso.</summary>
        public bool ChangingFloor { get; private set; }

        public FloorLayout Layout { get; private set; }
        public FloorBuilder.BuiltFloor Built { get; private set; }

        NavigationGrid navigation;
        NavigationGrid swimming;

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            CurrentSeed = 0;
            CurrentFloor = 0;
            FloorChanged = null;
        }

        // Al salir de la escena, los enemigos de otra escena (pruebas, menú) no deben usar la rejilla de este piso.
        void OnDestroy()
        {
            if (navigation != null && NavigationGrid.Current == navigation)
                NavigationGrid.Current = null;
            if (swimming != null && NavigationGrid.Swimming == swimming)
                NavigationGrid.Swimming = null;
            if (Built != null && Pits.Map == Built.Map)
                Pits.Map = null;
            // Las formas de cueva de la partida son objetos creados en memoria: se van con el piso.
            Layout?.ReleaseGenerated();
        }

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

        internal void SetArrivalFade(float seconds) => arrivalFade = Mathf.Max(0f, seconds);

        /// <summary>Sin cámara (pruebas): solo el piso, el jugador y la semilla.</summary>
        internal void Configure(FloorConfig newConfig, Transform newPlayer, int newFixedSeed)
        {
            config = newConfig;
            player = newPlayer;
            fixedSeed = newFixedSeed;
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

            CurrentFloor = config.FloorNumber;
            RoomHearts.ResetLuck();
            var random = new System.Random(seed);
            Layout = FloorLayoutGenerator.Generate(config, random);
            Built = FloorBuilder.Build(Layout, config, transform, random);
            navigation = new NavigationGrid(Built.Map);
            NavigationGrid.Current = navigation;
            swimming = NavigationGrid.ForSwimmers(Built.Map);
            NavigationGrid.Swimming = swimming;
            Pits.Map = Built.Map;

            // El jugador cae en las fosas (se le añade aquí para no tocar su prefab).
            if (player != null && !player.TryGetComponent<PlayerPitFall>(out _))
                player.gameObject.AddComponent<PlayerPitFall>();

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
            FloorChanged?.Invoke(CurrentFloor);
        }

        /// <summary>
        /// Lleva al jugador al piso siguiente: la pantalla queda en negro, se borra este piso (salas, enemigos,
        /// proyectiles y objetos sueltos), se monta el siguiente con una semilla nueva, el jugador aparece en su sala de
        /// inicio y la pantalla se aclara. Si no hay piso siguiente, da la victoria.
        /// </summary>
        public void GoToNextFloor()
        {
            if (ChangingFloor)
                return;
            if (!HasNextFloor)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.SetState(GameState.Victory);
                return;
            }
            StartCoroutine(ChangeFloor());
        }

        IEnumerator ChangeFloor()
        {
            ChangingFloor = true;
            // Negro encima de todo desde el primer momento: tapa el borrado y el negro del final de la metamorfosis.
            var fader = CreateFader();

            ClearFloor();
            // Un cuadro para que Unity destruya de verdad el piso anterior (sus salas siguen existiendo hasta el final del cuadro).
            yield return null;

            config = config.NextFloor;
            Generate(fixedSeed != 0 ? fixedSeed + config.FloorNumber : new System.Random().Next(1, int.MaxValue));
            RestorePlayer();

            var manager = GameManager.Instance;
            if (manager != null)
                manager.SetState(GameState.Playing);

            for (float t = 0f; t < arrivalFade; t += Time.unscaledDeltaTime)
            {
                fader.color = new Color(0f, 0f, 0f, 1f - t / arrivalFade);
                yield return null;
            }
            Destroy(fader.gameObject);
            ChangingFloor = false;
        }

        /// <summary>Borra el piso montado y lo que ha quedado suelto por él.</summary>
        void ClearFloor()
        {
            Projectile.DespawnAll();
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            foreach (var pickup in FindObjectsByType<BiomassPickup>(FindObjectsSortMode.None))
                Destroy(pickup.gameObject);
            foreach (var pickup in FindObjectsByType<MutationPickup>(FindObjectsSortMode.None))
                Destroy(pickup.gameObject);
            if (navigation != null && NavigationGrid.Current == navigation)
                NavigationGrid.Current = null;
            if (swimming != null && NavigationGrid.Swimming == swimming)
                NavigationGrid.Swimming = null;
            if (Built != null && Pits.Map == Built.Map)
                Pits.Map = null;
            Layout?.ReleaseGenerated();
            Built = null;
            Layout = null;
        }

        /// <summary>El salto a la fosa deja al jugador sin física y lejos: vuelve a moverse con normalidad.</summary>
        void RestorePlayer()
        {
            if (player == null)
                return;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
            {
                body.simulated = true;
                body.linearVelocity = Vector2.zero;
            }
            if (player.TryGetComponent<MutationLook>(out var look))
                look.ReleaseForm();
        }

        SpriteRenderer CreateFader()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var fader = new GameObject("FloorChangeFade").AddComponent<SpriteRenderer>();
            fader.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            // Por encima del negro de la metamorfosis (1000).
            fader.sortingOrder = 1001;
            fader.color = Color.black;
            var camera = Camera.main;
            if (camera != null)
            {
                fader.transform.SetParent(camera.transform, false);
                fader.transform.localPosition = new Vector3(0f, 0f, 1f);
            }
            fader.transform.localScale = new Vector3(200f, 200f, 1f);
            return fader;
        }
    }
}

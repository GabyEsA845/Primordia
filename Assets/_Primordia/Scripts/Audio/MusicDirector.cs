using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using Primordia.Shop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Primordia.Audio
{
    /// <summary>
    /// Elige la música según dónde está el jugador y el estado de la partida:
    /// - Menú principal: su pista.
    /// - Piso: exploración y combate a la vez, en sincronía; con las puertas cerradas suena el combate. En un piso con
    ///   franjas de profundidad (el océano), cada franja tiene su música: cambia al entrar en la primera sala de otra
    ///   franja, con un fundido cruzado de <see cref="BandFade"/> s (nunca a mitad de un combate). El Abismo tiene su ambiente.
    /// - Tienda: su vals (el mismo en todos los pisos). Sala de cambio: el laboratorio de Darwin.
    /// - Sala del jefe: aviso y la pista de ese jefe (la ameba pone ella misma la de su escena final). El Anomalocaris
    ///   calla en la escena de los ojos y pasa a su frenesí; el Euriptérido calla al caer y su último aliento tiene
    ///   su propia pista, que empieza con la primera salva.
    /// - Pausa, mapa y elección de mutación: la música baja. Evolución, victoria y derrota: sus fragmentos.
    /// - Sala de paso: al acercarse a la fosa, la música y el ambiente del piso se apagan poco a poco y sube el ambiente
    ///   del océano (el del piso siguiente), desde el pasillo que llega del jefe hasta el borde de la fosa.
    /// También hace sonar los cambios de estado de la interfaz (abrir la pausa, el mapa, las franjas de cine) y el
    /// ambiente del piso o de la sala del jefe.
    /// </summary>
    [RequireComponent(typeof(AudioManager))]
    public class MusicDirector : MonoBehaviour
    {
        /// <summary>Distancia a la fosa (en unidades) a la que empieza el cambio: más o menos la entrada del pasillo.</summary>
        public const float TransitionFadeFrom = 30f;

        /// <summary>Distancia a la fosa a la que ya solo suena el océano: el borde.</summary>
        public const float TransitionFadeTo = 4.5f;

        /// <summary>Segundos del fundido cruzado al pasar a otra franja de profundidad.</summary>
        public const float BandFade = 3f;

        /// <summary>Franja del Abismo (la última del piso 2): su música y su ambiente.</summary>
        const int AbyssBand = 2;

        static readonly string[] Ambiences =
        {
            Sounds.AmbFloor1, Sounds.AmbBossRoom, Sounds.AmbFloor2, Sounds.AmbFloor2Abyss, Sounds.AmbFloor2BossRoom
        };

        AudioManager manager;
        Transform player;
        float transitionBlend;
        GameManager game;
        Room currentRoom;
        Room bossRoomStarted;
        bool inFloor;
        int floorNumber = 1;
        int band;
        AnomalocarisBoss anomalocaris;
        EurypteridBoss eurypterid;

        /// <summary>Franja de profundidad cuya música suena (0 en los pisos sin franjas).</summary>
        public int CurrentBand => band;

        // Ambiente de cada piso: la sopa en el primero y el océano desde el segundo (más hondo en el Abismo).
        string FloorAmbience => floorNumber < 2 ? Sounds.AmbFloor1 : band >= AbyssBand ? Sounds.AmbFloor2Abyss : Sounds.AmbFloor2;

        string BossAmbience => floorNumber < 2 ? Sounds.AmbBossRoom : Sounds.AmbFloor2BossRoom;

        /// <summary>Exploración y combate del piso y la franja actuales.</summary>
        (string explore, string combat) FloorTracks()
        {
            if (floorNumber < 2)
                return (Sounds.MusicFloor1Explore, Sounds.MusicFloor1Combat);
            return band switch
            {
                0 => (Sounds.MusicFloor2ShallowsExplore, Sounds.MusicFloor2ShallowsCombat),
                1 => (Sounds.MusicFloor2ReefExplore, Sounds.MusicFloor2ReefCombat),
                _ => (Sounds.MusicFloor2AbyssExplore, Sounds.MusicFloor2AbyssCombat)
            };
        }

        void Awake() => manager = GetComponent<AudioManager>();

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Room.PlayerEnteredAny += OnPlayerEntered;
            Room.PlayerExitedAny += OnPlayerExited;
            Room.CombatStartedAny += OnCombatStarted;
            Room.ClearedAny += OnCleared;
            FloorGenerator.FloorChanged += OnFloorChanged;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Room.PlayerEnteredAny -= OnPlayerEntered;
            Room.PlayerExitedAny -= OnPlayerExited;
            Room.CombatStartedAny -= OnCombatStarted;
            Room.ClearedAny -= OnCleared;
            FloorGenerator.FloorChanged -= OnFloorChanged;
            Watch(null);
            WatchBosses(null);
        }

        void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        void Update()
        {
            // El GameManager se crea con cada escena (el de la nueva se destruye si ya hay uno): se sigue al que quede.
            if (game != GameManager.Instance)
                Watch(GameManager.Instance);

            UpdateTransition();
        }

        /// <summary>
        /// Mezcla del piso actual con el océano según la distancia a la fosa (0 = lejos, 1 = en el borde o ya saltando).
        /// Durante un combate no cambia nada: el jefe puede pelear cerca de la puerta del pasillo.
        /// </summary>
        void UpdateTransition()
        {
            float blend = 0f;
            var pit = inFloor ? TransitionPit.Current : null;
            if (pit != null)
            {
                if (player == null)
                {
                    var found = GameObject.FindWithTag("Player");
                    player = found != null ? found.transform : null;
                }

                if (pit.Used)
                    blend = 1f;
                else if (player != null && (currentRoom == null || currentRoom.State != Room.RoomState.Combat))
                {
                    float distance = Vector2.Distance(player.position, pit.transform.position);
                    blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TransitionFadeFrom, TransitionFadeTo, distance));
                }
            }

            if (Mathf.Approximately(blend, transitionBlend))
                return;
            transitionBlend = blend;

            manager.Music.SetDistanceGain(1f - blend);
            if (floorNumber == 1 && manager.IsLoopPlaying(Sounds.AmbFloor1))
                manager.StartLoop(Sounds.AmbFloor1, 1f - blend);
            if (blend > 0f)
                manager.StartLoop(Sounds.AmbFloor2, blend);
            else if (floorNumber == 1)
                manager.StopLoop(Sounds.AmbFloor2);
        }

        /// <summary>Cuánto suena ya el océano de la sala de paso (0 = nada, 1 = solo él).</summary>
        public float TransitionBlend => transitionBlend;

        void Watch(GameManager newGame)
        {
            if (game != null)
                game.StateChanged -= OnStateChanged;
            game = newGame;
            if (game != null)
            {
                game.StateChanged += OnStateChanged;
                manager.Music.SetDucked(IsDucked(game.State));
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentRoom = null;
            bossRoomStarted = null;
            player = null;
            transitionBlend = 0f;
            WatchBosses(null);
            manager.Music.SetDistanceGain(1f);
            inFloor = scene.name != SceneNames.MainMenu;
            // El piso ya se montó en el Awake de su generador, antes de que llegara este aviso (y antes de que existiera
            // el gestor de audio en la primera escena): se lee el que hay (Test_Floor2 empieza en el piso 2).
            floorNumber = Mathf.Max(1, FloorGenerator.CurrentFloor);
            band = 0;
            if (!inFloor)
            {
                StopAmbience(0.25f);
                manager.Music.Play(Sounds.MusicMainMenu, 1.5f);
                return;
            }

            manager.StopLoop(Sounds.AmbFloor2, 0f);
            PlayFloor(fade: 1.5f);
        }

        void PlayFloor(float fade = 1.5f)
        {
            var (explore, combat) = FloorTracks();
            manager.Music.Play(explore, combat, fade);
            PlayAmbience(FloorAmbience);
        }

        /// <summary>Deja sonando solo ese ambiente.</summary>
        void PlayAmbience(string ambience)
        {
            foreach (var id in Ambiences)
            {
                // En el piso 1, el océano solo suena junto a la fosa y lo lleva UpdateTransition.
                if (id != ambience && !(floorNumber == 1 && id == Sounds.AmbFloor2))
                    manager.StopLoop(id);
            }
            manager.StartLoop(ambience);
        }

        // Piso nuevo en la misma escena (tras la fosa): se olvida el anterior y suena el nuevo.
        internal void OnFloorChanged(int floor)
        {
            if (floor == floorNumber)
                return;
            floorNumber = floor;
            band = 0;
            currentRoom = null;
            bossRoomStarted = null;
            WatchBosses(null);
            transitionBlend = 0f;
            manager.Music.SetDistanceGain(1f);
            PlayFloor(fade: 2f);
        }

        internal void OnPlayerEntered(Room room)
        {
            if (!inFloor || room == null)
                return;
            currentRoom = room;

            if (room.GetComponentInChildren<ShopRoom>(true) != null)
            {
                manager.Music.Play(Sounds.MusicShop, 1f);
                return;
            }

            if (room.GetComponentInChildren<ExchangeCapsule>(true) != null)
            {
                manager.Music.Play(Sounds.MusicExchange, 1f);
                return;
            }

            var boss = room.GetComponentInChildren<IBoss>(true);
            if (boss != null && room.State != Room.RoomState.Cleared)
            {
                StartBoss(room, boss);
                return;
            }

            // Otra franja: la música funde despacio a la suya (la sala aún no ha empezado su combate).
            float fade = 1.5f;
            if (room.DepthBand >= 0 && room.DepthBand != band)
            {
                band = room.DepthBand;
                fade = BandFade;
            }
            PlayFloor(fade);
            manager.Music.SetLayer(room.State == Room.RoomState.Combat ? 1f : 0f);
        }

        void StartBoss(Room room, IBoss boss)
        {
            if (bossRoomStarted == room)
                return;
            bossRoomStarted = room;

            manager.Music.Play(BossTrack(boss), 0.3f);
            manager.Music.Stinger(Sounds.StingBossIntro, resume: true);
            if (boss is AmoebaBoss)
                manager.PlaySound(Sounds.AmoebaIntro);
            else if (boss is BossAI)
                manager.PlaySound(Sounds.VirusIntro);
            PlayAmbience(BossAmbience);
            WatchBosses(boss);
        }

        static string BossTrack(IBoss boss) => boss switch
        {
            AmoebaBoss => Sounds.MusicBossAmoeba,
            AnomalocarisBoss => Sounds.MusicBossAnomalocaris,
            EurypteridBoss => Sounds.MusicBossEurypterid,
            _ => Sounds.MusicBossVirus
        };

        /// <summary>Sigue las fases del jefe del piso 2 que acaba de empezar (null = ninguno).</summary>
        void WatchBosses(IBoss boss)
        {
            if (anomalocaris != null)
                anomalocaris.ModeChanged -= OnAnomalocarisMode;
            if (eurypterid != null)
                eurypterid.ModeChanged -= OnEurypteridMode;
            anomalocaris = boss as AnomalocarisBoss;
            eurypterid = boss as EurypteridBoss;
            if (anomalocaris != null)
                anomalocaris.ModeChanged += OnAnomalocarisMode;
            if (eurypterid != null)
                eurypterid.ModeChanged += OnEurypteridMode;
        }

        void OnAnomalocarisMode(AnomalocarisBoss.Mode mode)
        {
            switch (mode)
            {
                // Se apagan las luces: solo sus ojos en la oscuridad, sin música.
                case AnomalocarisBoss.Mode.Eyes:
                    manager.Music.Stop(0.4f);
                    break;
                case AnomalocarisBoss.Mode.Charge:
                case AnomalocarisBoss.Mode.Frenzy:
                case AnomalocarisBoss.Mode.Leap:
                case AnomalocarisBoss.Mode.Stun:
                case AnomalocarisBoss.Mode.Chase:
                case AnomalocarisBoss.Mode.Silhouette:
                    manager.Music.Play(Sounds.MusicBossAnomalocarisFrenzy, 0.3f);
                    break;
            }
        }

        void OnEurypteridMode(EurypteridBoss.Mode mode)
        {
            switch (mode)
            {
                // Último aliento: calla mientras va al centro y jadea; su pista de 25 s empieza con la primera salva.
                case EurypteridBoss.Mode.Downed:
                    manager.Music.Stop(1.5f);
                    break;
                case EurypteridBoss.Mode.Fury:
                    manager.Music.Play(Sounds.MusicBossEurypteridLastStand, 0f);
                    break;
            }
        }

        void OnPlayerExited(Room room)
        {
            if (room == currentRoom)
                currentRoom = null;
        }

        void OnCombatStarted(Room room)
        {
            if (inFloor && room == currentRoom && room.GetComponentInChildren<IBoss>(true) == null)
                manager.Music.SetLayer(1f);
        }

        void OnCleared(Room room)
        {
            if (!inFloor || room != currentRoom)
                return;

            // Tras el jefe, la calma del piso mientras se elige la recompensa.
            if (room == bossRoomStarted)
            {
                WatchBosses(null);
                PlayFloor(fade: 3f);
            }
            manager.Music.SetLayer(0f, 2.5f);
        }

        void OnStateChanged(GameState previous, GameState current)
        {
            manager.Music.SetDucked(IsDucked(current));

            switch (current)
            {
                case GameState.Paused when previous == GameState.Playing:
                    manager.PlaySound(Sounds.UIPauseOpen);
                    break;
                case GameState.Map when previous == GameState.Playing:
                    manager.PlaySound(Sounds.UIMapOpen);
                    break;
                case GameState.MutationChoice:
                    manager.PlaySound(Sounds.MutationChoiceOpen);
                    break;
                case GameState.Evolution:
                    manager.Music.Stinger(Sounds.StingLineageEvolution, resume: true);
                    break;
                case GameState.Cinematic:
                    manager.PlaySound(Sounds.CinematicBars);
                    break;
                case GameState.Victory:
                    StopAmbience(1f);
                    manager.Music.Stinger(Sounds.StingVictory, resume: false);
                    break;
                case GameState.Defeat:
                    StopAmbience(1f);
                    manager.Music.Stinger(Sounds.StingDefeat, resume: false);
                    break;
                case GameState.Playing when previous == GameState.Paused:
                    manager.PlaySound(Sounds.UIPauseClose);
                    break;
                case GameState.Playing when previous == GameState.Map:
                    manager.PlaySound(Sounds.UIMapClose);
                    break;
            }
        }

        void StopAmbience(float fade)
        {
            foreach (var id in Ambiences)
                manager.StopLoop(id, fade);
        }

        static bool IsDucked(GameState state) => state is GameState.Paused or GameState.Map or GameState.MutationChoice;
    }
}

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
    /// - Piso: exploración y combate a la vez, en sincronía; con las puertas cerradas suena el combate.
    /// - Tienda: su vals. Sala del jefe: aviso y la pista de ese jefe (la ameba pone ella misma la de su escena final).
    /// - Pausa, mapa y elección de mutación: la música baja. Evolución, victoria y derrota: sus fragmentos.
    /// También hace sonar los cambios de estado de la interfaz (abrir la pausa, el mapa, las franjas de cine) y el
    /// ambiente del piso o de la sala del jefe.
    /// </summary>
    [RequireComponent(typeof(AudioManager))]
    public class MusicDirector : MonoBehaviour
    {
        AudioManager manager;
        GameManager game;
        Room currentRoom;
        Room bossRoomStarted;
        bool inFloor;

        void Awake() => manager = GetComponent<AudioManager>();

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Room.PlayerEnteredAny += OnPlayerEntered;
            Room.PlayerExitedAny += OnPlayerExited;
            Room.CombatStartedAny += OnCombatStarted;
            Room.ClearedAny += OnCleared;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Room.PlayerEnteredAny -= OnPlayerEntered;
            Room.PlayerExitedAny -= OnPlayerExited;
            Room.CombatStartedAny -= OnCombatStarted;
            Room.ClearedAny -= OnCleared;
            Watch(null);
        }

        void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        void Update()
        {
            // El GameManager se crea con cada escena (el de la nueva se destruye si ya hay uno): se sigue al que quede.
            if (game != GameManager.Instance)
                Watch(GameManager.Instance);
        }

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
            inFloor = scene.name != SceneNames.MainMenu;
            if (!inFloor)
            {
                manager.StopLoop(Sounds.AmbFloor1);
                manager.StopLoop(Sounds.AmbBossRoom);
                manager.Music.Play(Sounds.MusicMainMenu, 1.5f);
                return;
            }

            PlayFloor(fade: 1.5f);
        }

        void PlayFloor(float fade = 1.5f)
        {
            manager.Music.Play(Sounds.MusicFloor1Explore, Sounds.MusicFloor1Combat, fade);
            manager.StopLoop(Sounds.AmbBossRoom);
            manager.StartLoop(Sounds.AmbFloor1);
        }

        void OnPlayerEntered(Room room)
        {
            if (!inFloor || room == null)
                return;
            currentRoom = room;

            if (room.GetComponentInChildren<ShopRoom>(true) != null)
            {
                manager.Music.Play(Sounds.MusicShop, 1f);
                return;
            }

            var boss = room.GetComponentInChildren<IBoss>(true);
            if (boss != null && room.State != Room.RoomState.Cleared)
            {
                StartBoss(room, boss);
                return;
            }

            PlayFloor();
            manager.Music.SetLayer(room.State == Room.RoomState.Combat ? 1f : 0f);
        }

        void StartBoss(Room room, IBoss boss)
        {
            if (bossRoomStarted == room)
                return;
            bossRoomStarted = room;

            bool amoeba = boss is AmoebaBoss;
            manager.Music.Play(amoeba ? Sounds.MusicBossAmoeba : Sounds.MusicBossVirus, 0.3f);
            manager.Music.Stinger(Sounds.StingBossIntro, resume: true);
            manager.PlaySound(amoeba ? Sounds.AmoebaIntro : Sounds.VirusIntro);
            manager.StopLoop(Sounds.AmbFloor1);
            manager.StartLoop(Sounds.AmbBossRoom);
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
                PlayFloor(fade: 3f);
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
                    StopAmbience();
                    manager.Music.Stinger(Sounds.StingVictory, resume: false);
                    break;
                case GameState.Defeat:
                    StopAmbience();
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

        void StopAmbience()
        {
            manager.StopLoop(Sounds.AmbFloor1, 1f);
            manager.StopLoop(Sounds.AmbBossRoom, 1f);
        }

        static bool IsDucked(GameState state) => state is GameState.Paused or GameState.Map or GameState.MutationChoice;
    }
}

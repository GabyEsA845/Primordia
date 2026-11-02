using System;
using System.Collections;
using System.Linq;
using Primordia.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Rooms
{
    /// <summary>
    /// Viajes entre teletransportes. Se puede viajar desde cualquier sala limpia (o la inicial) a cualquier
    /// teletransporte activo. Si la sala de la que sales no tiene teletransporte propio, queda marcada como «regreso»
    /// (el punto exacto donde estabas) y el siguiente viaje de vuelta te deja ahí y borra la marca; con una marca
    /// puesta, otro viaje desde una sala con teletransporte no la cambia. La marca se borra al cambiar de piso.
    /// </summary>
    public static class TeleportNetwork
    {
        const float FadeSeconds = 0.25f;

        static Room currentRoom;
        static bool traveling;
        static TravelRunner runner;

        /// <summary>Sala de la que sales a viajar: su marca de «regreso» si existe.</summary>
        public static bool HasReturn { get; private set; }
        public static Room ReturnRoom { get; private set; }
        public static Vector2 ReturnPosition { get; private set; }

        /// <summary>Se viaja ahora mismo (con el fundido en marcha).</summary>
        public static bool IsTraveling => traveling;

        public static Room CurrentRoom => currentRoom;

        /// <summary>Cambia el estado del viaje: se lanza al empezar y al acabar.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        internal static void Reset()
        {
            if (runner != null)
                UnityEngine.Object.Destroy(runner.gameObject);
            currentRoom = null;
            traveling = false;
            runner = null;
            HasReturn = false;
            ReturnRoom = null;
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Subscribe()
        {
            Room.PlayerEnteredAny += OnRoomEntered;
            FloorGenerator.FloorChanged += _ => ClearReturn();
        }

        static void OnRoomEntered(Room room) => currentRoom = room;

        /// <summary>Quita la marca de regreso (al cambiar de piso).</summary>
        public static void ClearReturn()
        {
            HasReturn = false;
            ReturnRoom = null;
            Changed?.Invoke();
        }

        /// <summary>Teletransportes a los que se puede ir desde aquí: los activos, menos el de la sala en la que estás.</summary>
        public static Teleporter[] Destinations() =>
            Teleporter.All.Where(t => t != null && t.IsActive && t.Room != currentRoom).ToArray();

        /// <summary>El teletransporte de la sala actual, si lo tiene.</summary>
        public static Teleporter HereTeleporter() => Teleporter.All.FirstOrDefault(t => t != null && t.Room == currentRoom);

        /// <summary>Solo desde una sala limpia y con el jugador dentro; sin viaje en marcha ni escenas de jefe.</summary>
        public static bool CanTravel()
        {
            if (traveling || currentRoom == null || currentRoom.State == Room.RoomState.Combat)
                return false;
            var manager = GameManager.Instance;
            if (manager != null && manager.State is not (GameState.Playing or GameState.Map))
                return false;
            var player = FindPlayer();
            return player != null && currentRoom.TryGetComponent<Collider2D>(out var area) && area.OverlapPoint(player.position);
        }

        static Transform FindPlayer()
        {
            var playerObject = GameObject.FindWithTag("Player");
            return playerObject != null ? playerObject.transform : null;
        }

        /// <summary>Viaja a ese teletransporte. Devuelve false si ahora no se puede.</summary>
        public static bool TravelTo(Teleporter destination)
        {
            if (destination == null || !destination.IsActive || !CanTravel())
                return false;

            Teleporter origin = HereTeleporter();
            var player = FindPlayer();
            // Una sala sin teletransporte propio se marca para poder volver; con la marca ya puesta no se toca.
            if (origin == null && !HasReturn)
            {
                HasReturn = true;
                ReturnRoom = currentRoom;
                ReturnPosition = player.position;
            }
            Run(destination.Position, origin, destination);
            return true;
        }

        /// <summary>Vuelve a la sala marcada, al punto donde estabas, y borra la marca.</summary>
        public static bool TravelBack()
        {
            if (!HasReturn || !CanTravel())
                return false;

            var target = ReturnPosition;
            HasReturn = false;
            ReturnRoom = null;
            Run(target, HereTeleporter(), null);
            return true;
        }

        static void Run(Vector2 target, Teleporter origin, Teleporter destination)
        {
            if (runner == null)
            {
                var go = new GameObject("TeleportRunner");
                runner = go.AddComponent<TravelRunner>();
            }
            traveling = true;
            Changed?.Invoke();
            runner.StartCoroutine(runner.Go(target, origin, destination));
        }

        static void Finish()
        {
            traveling = false;
            Changed?.Invoke();
        }

        /// <summary>Sale del mapa si está abierto, funde a negro, mueve al jugador, y funde de vuelta.</summary>
        sealed class TravelRunner : MonoBehaviour
        {
            Image fade;

            public IEnumerator Go(Vector2 target, Teleporter origin, Teleporter destination)
            {
                var manager = GameManager.Instance;
                if (manager != null && manager.State == GameState.Map)
                    manager.SetState(GameState.Playing);

                var player = FindPlayer();
                if (player != null && player.TryGetComponent<Health>(out var health))
                    health.MakeInvulnerable(FadeSeconds * 2f + 0.3f);

                EnsureFade();
                if (origin != null)
                    origin.PlayWarp();
                yield return Fade(0f, 1f);

                if (player != null)
                    Move(player, target);
                if (destination != null)
                    destination.PlayWarp();
                yield return null;
                yield return Fade(1f, 0f);
                Finish();
            }

            static void Move(Transform player, Vector2 target)
            {
                Vector3 before = player.position;
                if (player.TryGetComponent<Rigidbody2D>(out var body))
                {
                    body.linearVelocity = Vector2.zero;
                    body.position = target;
                }
                player.position = target;
                // La cámara no debe cruzar el piso corriendo detrás del jugador.
                Vector3 delta = (Vector3)target - before;
                foreach (var camera in UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude))
                    camera.OnTargetObjectWarped(player, delta);
            }

            void EnsureFade()
            {
                if (fade != null)
                    return;

                var canvasObject = new GameObject("TeleportFade");
                canvasObject.transform.SetParent(transform, false);
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 900;
                var imageObject = new GameObject("Black");
                imageObject.transform.SetParent(canvasObject.transform, false);
                fade = imageObject.AddComponent<Image>();
                fade.color = new Color(0f, 0f, 0f, 0f);
                fade.raycastTarget = false;
                var rect = fade.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            IEnumerator Fade(float from, float to)
            {
                for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
                {
                    fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / FadeSeconds));
                    yield return null;
                }
                fade.color = new Color(0f, 0f, 0f, to);
            }
        }
    }
}

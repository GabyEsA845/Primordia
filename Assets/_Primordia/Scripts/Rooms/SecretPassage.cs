using System.Collections;
using Primordia.Player;
using Primordia.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Paso a la arena del jefe secreto (piso 3), que está aparte del mapa: al meterse por el muro de raíces abierto, la pantalla
    /// funde a negro y el jugador aparece al principio del pasillo. Con la arena limpia sale junto al tocón una salida que devuelve
    /// a la antesala; si se vuelve a cruzar el muro, se llega directamente junto a ella.
    /// </summary>
    public class SecretPassage : MonoBehaviour
    {
        // Cuánto hay que meterse en el arco (desde su borde de abajo, con los pies) para cruzar, y dónde se reaparece bajo él.
        const float EnterDepth = 0.8f, ReturnBelow = 2f;
        const float FootOffset = -0.3f;
        const float FadeSeconds = 0.4f, HoldSeconds = 0.25f;

        RootWall wall;
        Room arena;
        Transform player;
        SecretExit exit;

        /// <summary>Principio del pasillo de la arena.</summary>
        public Vector2 ArenaEntry { get; private set; }

        /// <summary>Dónde está la salida de la arena (aparece con ella limpia).</summary>
        public Vector2 ArenaExit { get; private set; }

        /// <summary>Dónde se reaparece en la antesala.</summary>
        public Vector2 ReturnPoint => new(wall.DoorCenter.x, wall.ArchBottom - ReturnBelow);

        public bool Travelling { get; private set; }

        public SecretExit Exit => exit;

        internal static SecretPassage Create(Transform parent, RootWall wall, Room arena, Vector2 entry, Vector2 exitPoint,
            Sprite[] exitFrames, Sprite[] keyFrames)
        {
            var go = new GameObject("SecretPassage");
            go.transform.SetParent(parent, false);
            var passage = go.AddComponent<SecretPassage>();
            passage.wall = wall;
            passage.arena = arena;
            passage.ArenaEntry = entry;
            passage.ArenaExit = exitPoint;
            passage.exit = SecretExit.Create(go.transform, passage, exitPoint, exitFrames, keyFrames);
            arena.Cleared += passage.OnArenaCleared;
            return passage;
        }

        void OnDestroy()
        {
            if (arena != null)
                arena.Cleared -= OnArenaCleared;
        }

        void OnArenaCleared(Room room) => exit.gameObject.SetActive(true);

        void Update()
        {
            if (Travelling || wall == null || !wall.Crossing)
                return;
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                    return;
                player = found.transform;
            }
            if (player.position.y + FootOffset < wall.ArchBottom + EnterDepth)
                return;
            // Vencido el jefe, no hay que recorrer otra vez el pasillo.
            var destination = arena.State == Room.RoomState.Cleared ? ArenaExit + Vector2.left * ReturnBelow : ArenaEntry;
            StartCoroutine(Travel(player, destination));
        }

        internal void Return(GameObject traveller)
        {
            if (!Travelling && traveller != null)
                StartCoroutine(Travel(traveller.transform, ReturnPoint));
        }

        IEnumerator Travel(Transform traveller, Vector2 destination)
        {
            Travelling = true;
            var controller = traveller.GetComponent<PlayerController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null)
                controller.enabled = false;
            traveller.TryGetComponent<Rigidbody2D>(out var body);
            if (body != null)
                body.linearVelocity = Vector2.zero;

            var presenter = RewardPresenter.Create();
            yield return presenter.Fade(Color.black, FadeSeconds, 0f, 1f);

            traveller.position = destination;
            if (body != null)
            {
                body.position = destination;
                body.linearVelocity = Vector2.zero;
            }
            SnapCamera(destination);

            yield return new WaitForSeconds(HoldSeconds);
            if (controller != null)
                controller.enabled = wasEnabled;
            yield return presenter.Fade(Color.black, FadeSeconds, 1f, 0f);
            Destroy(presenter.gameObject);
            Travelling = false;
        }

        // La cámara aparece ya en el destino, sin recorrer el vacío entre el piso y la arena.
        static void SnapCamera(Vector2 position)
        {
            var target = FindAnyObjectByType<CameraLookAhead>();
            if (target != null)
                target.transform.position = position;
            var camera = FindAnyObjectByType<CinemachineCamera>();
            if (camera == null)
                return;
            camera.ForceCameraPosition((Vector3)position + Vector3.back * 10f, Quaternion.identity);
            camera.PreviousStateIsValid = false;
        }
    }
}

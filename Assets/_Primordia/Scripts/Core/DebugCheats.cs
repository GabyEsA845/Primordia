#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Core
{
    /// <summary>
    /// Atajos para probar (solo en el editor y en compilaciones de desarrollo; la build de itch.io no los lleva):
    /// F1 invulnerable, F2 daño ×5, F3 vacía la sala en combate y F4 da una mutación al azar de la reserva del piso (o
    /// sube de nivel una que ya se lleva). Arriba a la izquierda se ve qué está activo.
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
        const float DamageBoost = 5f;
        const int KillDamage = 100000;

        static DebugCheats instance;

        bool invulnerable;
        bool strong;
        Room current;
        PlayerMutations player;
        string message;
        float messageUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (instance != null)
                return;
            var cheats = new GameObject("DebugCheats");
            DontDestroyOnLoad(cheats);
            instance = cheats.AddComponent<DebugCheats>();
        }

        void OnEnable() => Room.PlayerEnteredAny += OnRoomEntered;

        void OnDisable() => Room.PlayerEnteredAny -= OnRoomEntered;

        void OnRoomEntered(Room room) => current = room;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (player == null)
                player = FindAnyObjectByType<PlayerMutations>();
            if (player == null)
                return;

            if (keyboard.f1Key.wasPressedThisFrame)
                Say((invulnerable = !invulnerable) ? "Invulnerable" : "Vulnerable");
            if (keyboard.f2Key.wasPressedThisFrame)
            {
                strong = !strong;
                if (!strong && player.Weapon != null)
                    player.Weapon.ClearDamageBonus(this);
                Say(strong ? $"Daño ×{DamageBoost}" : "Daño normal");
            }
            if (keyboard.f3Key.wasPressedThisFrame)
                ClearRoom();
            if (keyboard.f4Key.wasPressedThisFrame)
                GiveMutation();

            // La esquiva enciende y apaga la invulnerabilidad: aquí se renueva cada cuadro sin tocarla.
            if (invulnerable && player.Health != null)
                player.Health.MakeInvulnerable(0.2f);
            if (strong && player.Weapon != null)
                player.Weapon.SetDamageBonus(this, DamageBoost);
        }

        void ClearRoom()
        {
            if (current == null || current.State != Room.RoomState.Combat)
            {
                Say("No hay combate en esta sala");
                return;
            }
            foreach (var enemy in current.GetComponentsInChildren<Enemy>())
            {
                if (enemy.Health != null && !enemy.Health.IsDead)
                    enemy.Health.TakeDamage(KillDamage);
            }
            Say("Sala vaciada");
        }

        void GiveMutation()
        {
            var generator = FindAnyObjectByType<FloorGenerator>();
            var pool = generator != null && generator.Config != null ? generator.Config.RewardPool : null;
            var offer = pool != null ? pool.PickOffer(1, player) : null;
            if (offer == null || offer.Count == 0)
            {
                Say("No quedan mutaciones que dar");
                return;
            }

            var mutation = offer[0];
            if (player.IsEquipped(mutation))
            {
                player.LevelUp(mutation.Slot);
                Say($"{mutation.name} sube a nivel {player.LevelOf(mutation.Slot)}");
            }
            else
            {
                player.Equip(mutation);
                Say($"{mutation.name} equipada");
            }
        }

        void Say(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 2f;
        }

        void OnGUI()
        {
            string state = (invulnerable ? "INVULNERABLE  " : "") + (strong ? $"DAÑO ×{DamageBoost}  " : "");
            string line = Time.unscaledTime < messageUntil ? message : null;
            if (state.Length == 0 && line == null)
                return;
            GUI.Label(new Rect(10, 10, 600, 22), $"[pruebas] {state}{line}");
        }
    }
}
#endif

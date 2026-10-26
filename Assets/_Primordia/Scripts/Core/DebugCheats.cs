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
    /// F1 invulnerable, F2 daño ×5, F3 vacía la sala en combate, F4 da una mutación al azar de la reserva del piso (o
    /// sube de nivel una que ya se lleva), F5 una mutación especial de las almejas (o sube una que ya se lleva) y F6 cambia a
    /// la siguiente Boca del piso, con el mismo nivel (para probar las Bocas del piso 2 en Test_Floor2) y F7 lleva junto a
    /// la cápsula de la sala de cambio. Arriba a la izquierda se ve qué está activo.
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
            if (keyboard.f5Key.wasPressedThisFrame)
                GiveSpecial();
            if (keyboard.f6Key.wasPressedThisFrame)
                NextMouth();
            if (keyboard.f7Key.wasPressedThisFrame)
                GoToExchange();

            // La esquiva enciende y apaga la invulnerabilidad: aquí se renueva cada cuadro sin tocarla.
            if (invulnerable && player.Health != null)
                player.Health.MakeInvulnerable(0.2f);
            if (strong && player.Weapon != null)
                player.Weapon.SetDamageBonus(this, DamageBoost);
        }

        void GoToExchange()
        {
            var capsule = FindAnyObjectByType<ExchangeCapsule>();
            if (capsule == null)
            {
                Say("Este piso no tiene sala de cambio");
                return;
            }
            // Delante de la cápsula, por debajo de su bloque de 2×2.
            var position = (Vector2)capsule.transform.position + Vector2.down * 2.5f;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
            player.transform.position = position;
            Say("Sala de cambio");
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

        void GiveSpecial()
        {
            var generator = FindAnyObjectByType<FloorGenerator>();
            var specials = generator != null && generator.Config != null ? generator.Config.Clams.bigMutations : null;
            var free = new System.Collections.Generic.List<MutationData>();
            var upgradable = new System.Collections.Generic.List<MutationData>();
            foreach (var special in specials ?? System.Array.Empty<MutationData>())
            {
                if (special == null)
                    continue;
                if (!player.IsEquipped(special))
                    free.Add(special);
                else if (player.CanLevelUp(special))
                    upgradable.Add(special);
            }

            var list = free.Count > 0 ? free : upgradable;
            if (list.Count == 0)
            {
                Say("No quedan especiales que dar en este piso");
                return;
            }
            var mutation = list[Random.Range(0, list.Count)];
            player.Equip(mutation);
            Say(free.Count > 0 ? $"{mutation.name} equipada (especial)" : $"{mutation.name} sube a nivel {player.LevelOf(mutation.Slot)}");
        }

        /// <summary>Cambia a la siguiente Boca del piso actual (en orden de nombre) y la deja al nivel de la que se llevaba.</summary>
        void NextMouth()
        {
            var mouths = FloorMouths(Mathf.Max(1, FloorGenerator.CurrentFloor));
            if (mouths.Count == 0)
            {
                Say("Este piso no tiene Bocas");
                return;
            }

            var current = player.Get(MutationSlot.Mouth);
            int level = Mathf.Max(1, player.LevelOf(MutationSlot.Mouth));
            var next = mouths[(mouths.IndexOf(current) + 1) % mouths.Count];
            if (current != null)
                player.Unequip(MutationSlot.Mouth);
            player.Equip(next);
            while (player.LevelOf(MutationSlot.Mouth) < level && player.LevelUp(MutationSlot.Mouth))
            {
            }
            Say($"Boca: {next.DisplayName} (nivel {player.LevelOf(MutationSlot.Mouth)})");
        }

        /// <summary>Las Bocas de ese piso que no son especiales: en el editor, todas las del proyecto; si no, las cargadas.</summary>
        static System.Collections.Generic.List<MutationData> FloorMouths(int floor)
        {
#if UNITY_EDITOR
            var all = new System.Collections.Generic.List<MutationData>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MutationData"))
            {
                var mutation = UnityEditor.AssetDatabase.LoadAssetAtPath<MutationData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (mutation != null)
                    all.Add(mutation);
            }
#else
            var all = new System.Collections.Generic.List<MutationData>(Resources.FindObjectsOfTypeAll<MutationData>());
#endif
            all.RemoveAll(m => m.Slot != MutationSlot.Mouth || m.Floor != floor || m.IsSpecial);
            all.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return all;
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

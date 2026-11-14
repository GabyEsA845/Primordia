using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Reliquias y objetos clave de la partida en curso (provisional hasta el enganche real al piso): persisten entre salas y pisos
    /// y se vacían al empezar o reiniciar la partida. También recuerda qué recompensas únicas ya se cogieron.
    /// </summary>
    public static class RunInventory
    {
        public const string RootAncestral = "RootAncestral";
        public const string PurpleSpore = "PurpleSpore";
        public const string LepidoHeartTaken = "Lepido.Heart";

        public sealed class Entry
        {
            public string Id, Name, Effect;
            public Sprite Icon16, Icon32;
        }

        static readonly List<Entry> relics = new();
        static readonly List<Entry> keyItems = new();
        static readonly HashSet<string> flags = new();

        public static IReadOnlyList<Entry> Relics => relics;
        public static IReadOnlyList<Entry> KeyItems => keyItems;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            relics.Clear();
            keyItems.Clear();
            flags.Clear();
            Changed = null;
        }

        public static bool Has(string id) => relics.Exists(e => e.Id == id) || keyItems.Exists(e => e.Id == id);

        public static bool AddRelic(Entry entry) => Add(relics, entry);

        public static bool AddKeyItem(Entry entry) => Add(keyItems, entry);

        public static bool HasFlag(string flag) => flags.Contains(flag);

        public static void SetFlag(string flag)
        {
            if (flags.Add(flag))
                Changed?.Invoke();
        }

        /// <summary>Efecto de la Raíz ancestral: al llegar a un piso nuevo se recupera toda la vida.</summary>
        public static void OnFloorEntered(GameObject player)
        {
            if (!Has(RootAncestral) || player == null || !player.TryGetComponent<Health>(out var health) || health.IsDead)
                return;
            health.Heal(health.Max - health.Current);
        }

        static bool Add(List<Entry> list, Entry entry)
        {
            if (entry == null || Has(entry.Id))
                return false;
            list.Add(entry);
            flags.Add(entry.Id);
            Changed?.Invoke();
            return true;
        }
    }
}

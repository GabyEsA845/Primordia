using System;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Dibujos del letrero de las salas con tema del piso 3 (docs/multimedia-temp/floor3-room-signs): el tronco común y, por
    /// tema, el nombre ya rotulado y el icono. Lo crea Primordia > Floor 3 > Importar letreros de las salas con tema en
    /// Resources/RoomSignArt. Sin él, las salas con tema simplemente no muestran letrero.
    /// </summary>
    public class RoomSignArt : ScriptableObject
    {
        public const string ResourceName = "RoomSignArt";

        [Serializable]
        public struct Entry
        {
            [Tooltip("Clave del tema (FloorConfig.EncounterTheme.signKey).")]
            public string key;
            public Sprite title;
            public Sprite icon;
        }

        public Sprite log;
        public Entry[] entries = Array.Empty<Entry>();

        static RoomSignArt current;
        static bool loaded;

        public static RoomSignArt Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<RoomSignArt>(ResourceName);
                }
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            loaded = false;
        }

        public bool TryGet(string key, out Entry entry)
        {
            foreach (var candidate in entries ?? Array.Empty<Entry>())
            {
                if (candidate.key == key && candidate.title != null)
                {
                    entry = candidate;
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}

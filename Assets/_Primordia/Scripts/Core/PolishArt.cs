using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Hojas de docs/multimedia-temp/floor3-polish (lodo en el cuerpo, llamas, iconos de barras, agua de charca, ráfaga de la
    /// fumarola y capas de las puertas). Lo crea Primordia > Art > Importar pulido del piso 3 en Resources/PolishArt. Quien no
    /// lo encuentra usa su dibujo provisional o no muestra el efecto, así que las pruebas y las escenas sin importar siguen
    /// funcionando.
    /// </summary>
    public class PolishArt : ScriptableObject
    {
        public const string ResourceName = "PolishArt";

        /// <summary>Un tamaño de efecto: S (bichos pequeños), M (jugador y normales) o L (grandes).</summary>
        [System.Serializable]
        public class Sized
        {
            public Sprite[] s = System.Array.Empty<Sprite>();
            public Sprite[] m = System.Array.Empty<Sprite>();
            public Sprite[] l = System.Array.Empty<Sprite>();

            /// <summary>El tamaño más cercano a esa altura del cuerpo en píxeles (menos de 14 → S, más de 26 → L).</summary>
            public Sprite[] ForHeight(float pixels) => pixels < 14f ? s : pixels > 26f ? l : m;
        }

        [System.Serializable]
        public class DoorLayers
        {
            public string theme;
            /// <summary>El arco de la puerta de ese tema (<c>Doorway_Top</c>): con él se reconoce el tema.</summary>
            public Sprite arch;
            public Sprite front;
            public Sprite shade;
        }

        [Header("Lodo en el cuerpo")]
        public Sprite mudTexture;
        public Sprite mudNoise;
        public Sized mudRingFront = new();
        public Sized mudRingBack = new();
        public Sprite[] mudBubble = System.Array.Empty<Sprite>();
        public Sprite[] mudDrip = System.Array.Empty<Sprite>();
        [Header("Eryops (floor3-lineage-art/Eryops)")]
        [Tooltip("Barro seco tileable (16×16): las placas del barro al secarse.")]
        public Sprite mudTextureDry;
        [Tooltip("Estela en V en el lodo (4 cuadros, bucle; pivote a la derecha) y terrones que saltan de las patas (6 cuadros) y escamas que caen (5).")]
        public Sprite[] eryopsWake = System.Array.Empty<Sprite>();
        public Sprite[] eryopsKick = System.Array.Empty<Sprite>();
        public Sprite[] eryopsFlake = System.Array.Empty<Sprite>();

        [Header("Llamas")]
        public Sized bodyFlame = new();
        public Sprite[] flameLick = System.Array.Empty<Sprite>();

        [Header("Iconos de barras")]
        public Sprite burnEmpty;
        public Sprite burnFull;
        public Sprite toxicEmpty;
        public Sprite toxicFull;

        [Header("Agua de charca")]
        public Sprite[] waterRipple = System.Array.Empty<Sprite>();
        public Sprite[] waterSplash = System.Array.Empty<Sprite>();
        public Sprite[] waterDrip = System.Array.Empty<Sprite>();
        public Sprite[] steamHiss = System.Array.Empty<Sprite>();

        [Header("Fumarola")]
        public Sprite[] ventGust = System.Array.Empty<Sprite>();

        [Header("Puertas")]
        public DoorLayers[] doors = System.Array.Empty<DoorLayers>();

        static PolishArt current;
        static bool loaded;

        /// <summary>El arte importado, o null si aún no se ha importado.</summary>
        public static PolishArt Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<PolishArt>(ResourceName);
                }
                return current;
            }
        }

        public DoorLayers Door(string theme)
        {
            foreach (var door in doors)
                if (door.theme == theme)
                    return door;
            return null;
        }

        /// <summary>Las capas de la puerta de un tema, buscadas por su arco. Null si no hay.</summary>
        public DoorLayers DoorFor(Sprite arch)
        {
            if (arch == null)
                return null;
            foreach (var door in doors)
                if (door.arch == arch && door.front != null && door.shade != null)
                    return door;
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            loaded = false;
        }
    }
}

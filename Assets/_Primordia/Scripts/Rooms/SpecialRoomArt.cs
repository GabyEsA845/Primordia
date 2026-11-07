using System;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Arte de las salas especiales del piso 3 (docs/multimedia-temp/floor3-special-rooms): la veta de carbón (tiles por tramo y
    /// estados de fuego) y el Bosque en humo (capas de humo, máscara de visión, volutas y ascuas). Lo crea Primordia > Floor 3 >
    /// Importar arte de las salas especiales en Resources/SpecialRoomArt. Sin él, esas salas salen sin su arte.
    /// </summary>
    public class SpecialRoomArt : ScriptableObject
    {
        public const string ResourceName = "SpecialRoomArt";
        public const int Bands = 3;

        [Serializable]
        public class CoalBand
        {
            [Tooltip("16 tiles: la máscara suma los vecinos que NO son veta (N=1, E=2, S=4, O=8).")]
            public Sprite[] tiles = Array.Empty<Sprite>();
            [Tooltip("Esquinas de dentro: NE, NO, SE, SO.")]
            public Sprite[] corners = Array.Empty<Sprite>();
        }

        [Serializable]
        public class BubbleBand
        {
            [Tooltip("Burbuja de metano que sube (16×24, 6 cuadros, pivote abajo).")]
            public Sprite[] frames = Array.Empty<Sprite>();
        }

        /// <summary>Hoja de cuadros (Unity no serializa arrays de arrays).</summary>
        [Serializable]
        public class Frames
        {
            public Sprite[] frames = Array.Empty<Sprite>();
        }

        /// <summary>Un tramo del tronco en el lago: reposo, pisada y casillas vecinas a la pisada.</summary>
        [Serializable]
        public class LogPiece
        {
            public Frames idle = new();
            public Frames step = new();
            public Frames dip = new();
        }

        [Serializable]
        public class LogBand
        {
            [Tooltip("Tramos del lago (16×48; las filas 8..39 son las dos casillas; pivote (0, 8/48)).")]
            public LogPiece[] mid = Array.Empty<LogPiece>();
            [Tooltip("Tramos apoyados en la orilla (16×48, mismo pivote).")]
            public Sprite[] ground = Array.Empty<Sprite>();
            [Tooltip("Cepa de la izquierda (40×64, pivote (0, 16/64)).")]
            public Sprite endLeft;
            [Tooltip("Copa de la derecha (48×64, pivote (0, 16/64)).")]
            public Sprite endRight;
        }

        /// <summary>Cofre de cono de Lepidodendron: reposo (4), apertura (6) y abierto (1).</summary>
        [Serializable]
        public class ConeArt
        {
            public Sprite[] idle = Array.Empty<Sprite>();
            public Sprite[] open = Array.Empty<Sprite>();
            public Sprite opened;
            public bool IsValid => idle.Length > 0 && open.Length > 0 && opened != null;
        }

        [Header("Tronco caído: una versión por tramo; burbujas de lodo y rayo de luz del Tocón hueco")]
        public LogBand[] log = new LogBand[Bands];
        public Sprite[] mudBubble = Array.Empty<Sprite>();
        public Sprite[] mudBubbleSmall = Array.Empty<Sprite>();
        [Tooltip("Rayo de luz sobre el cono del Tocón hueco (64×112, 8 cuadros; el centro del charco, en el pivote).")]
        public Sprite[] stumpLight = Array.Empty<Sprite>();

        [Header("Cofres de cono de Lepidodendron")]
        public ConeArt coneBig = new();
        public ConeArt coneSmall = new();

        [Header("Burbujeo: burbuja de metano de cada tramo")]
        public BubbleBand[] bubble = new BubbleBand[Bands];

        [Header("Veta de carbón (16×16; las capas de fuego 16×32 con la casilla en la mitad de abajo)")]
        public CoalBand[] coal = new CoalBand[Bands];
        public Sprite[] ignite = Array.Empty<Sprite>();
        [Tooltip("Tres variantes de las llamas para que las casillas vecinas no repitan.")]
        [SerializeField] Sprite[] burn0 = Array.Empty<Sprite>();
        [SerializeField] Sprite[] burn1 = Array.Empty<Sprite>();
        [SerializeField] Sprite[] burn2 = Array.Empty<Sprite>();
        public Sprite[] embers = Array.Empty<Sprite>();
        public Sprite coalOut;

        [Header("Bosque en humo")]
        public Sprite smokeA;
        public Sprite smokeB;
        [Tooltip("Material de la malla del humo (Sprites/Default, con color por vértice); referenciarlo lo mantiene en las builds.")]
        public Material smokeMaterial;
        public Sprite[] wisp = Array.Empty<Sprite>();
        public Sprite[] floatingEmbers = Array.Empty<Sprite>();

        static SpecialRoomArt current;
        static bool loaded;

        public static SpecialRoomArt Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<SpecialRoomArt>(ResourceName);
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

        public Sprite[] BubbleFor(int band) =>
            bubble != null && bubble.Length > 0 && bubble[Mathf.Clamp(band, 0, bubble.Length - 1)] != null
                ? bubble[Mathf.Clamp(band, 0, bubble.Length - 1)].frames : Array.Empty<Sprite>();

        public LogBand LogFor(int band) =>
            log != null && log.Length > 0 && log[Mathf.Clamp(band, 0, log.Length - 1)] != null ? log[Mathf.Clamp(band, 0, log.Length - 1)] : null;

        public CoalBand CoalFor(int band) => coal != null && coal.Length > 0 ? coal[Mathf.Clamp(band, 0, coal.Length - 1)] : null;

        public Sprite[] BurnVariant(int index)
        {
            var variants = new[] { burn0, burn1, burn2 };
            return variants[((index % 3) + 3) % 3];
        }

        internal void SetBurn(Sprite[] first, Sprite[] second, Sprite[] third)
        {
            burn0 = first ?? Array.Empty<Sprite>();
            burn1 = second ?? Array.Empty<Sprite>();
            burn2 = third ?? Array.Empty<Sprite>();
        }

        public bool HasCoal => CoalFor(0) != null && CoalFor(0).tiles.Length == 16 && burn0.Length > 0 && embers.Length > 0;

        public bool HasSmoke => smokeA != null && smokeMaterial != null;
    }
}

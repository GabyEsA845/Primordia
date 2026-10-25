using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Rooms
{
    /// <summary>
    /// Tileset de un tipo de sala (sopa primordial, sala del jefe): un tile por pieza, variantes,
    /// decoración y sprites de las puertas. Lo crea el menú Primordia > Art > Importar escenario.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Rooms/Room Theme", fileName = "Theme_")]
    public class RoomTheme : ScriptableObject
    {
        [Header("Suelo")]
        [SerializeField] TileBase floor;
        [Tooltip("Variantes que salen más o menos en 1 de cada 3 tiles de suelo.")]
        [SerializeField] TileBase[] floorVariants;
        [SerializeField] TileBase[] floorShadow;

        [Header("Paredes")]
        [SerializeField] TileBase cap;
        [SerializeField] TileBase[] faceUpper;
        [SerializeField] TileBase[] faceLower;
        [SerializeField] TileBase edgeLeft;
        [SerializeField] TileBase edgeRight;
        [SerializeField] TileBase edgeBottom;
        [SerializeField] TileBase cornerBottomLeft;
        [SerializeField] TileBase cornerBottomRight;

        [Header("Paredes para salas con formas (opcionales)")]
        [SerializeField] TileBase faceUpperEndLeft;
        [SerializeField] TileBase faceUpperEndRight;
        [SerializeField] TileBase faceLowerEndLeft;
        [SerializeField] TileBase faceLowerEndRight;
        [SerializeField] TileBase outerTopLeft;
        [SerializeField] TileBase outerTopRight;
        [Tooltip("Se usa donde falta una pieza, para verla enseguida.")]
        [SerializeField] TileBase missing;

        [Header("Decoración (sin colisión)")]
        [SerializeField] TileBase[] decorations;
        [Tooltip("Probabilidad de decoración en cada tile de suelo libre.")]
        [SerializeField, Range(0f, 1f)] float decorationChance = 0.05f;

        [Header("Puertas")]
        [Tooltip("Arco de la pared superior (2×2 tiles), siempre visible.")]
        [SerializeField] Sprite doorway;
        [Tooltip("Membranas, de abierta (0) a cerrada (último).")]
        [SerializeField] Sprite[] doorTop;
        [SerializeField] Sprite[] doorBottom;
        [Tooltip("Dibujada para la pared izquierda; en la derecha se voltea.")]
        [SerializeField] Sprite[] doorSide;

        [Header("Puertas de losas (piso 2, opcionales)")]
        [Tooltip("Cerrar: del cuadro vacío a la puerta cerrada (último). Si están, sustituyen a la membrana.")]
        [SerializeField] DoorAnimation slabTop;
        [SerializeField] DoorAnimation slabBottom;
        [Tooltip("Dibujada para la pared izquierda; en la derecha se voltea.")]
        [SerializeField] DoorAnimation slabSide;

        [Header("Obstáculos de cobertura (bloquean)")]
        [Tooltip("De 1×1 tile (marca o de las plantillas).")]
        [SerializeField] Sprite[] smallObstacles;
        [Tooltip("De 2×2 tiles (marca O de las plantillas).")]
        [SerializeField] Sprite[] bigObstacles;
        [Tooltip("Contorno blanco de 1 px de cada obstáculo (mismo orden que arriba), para que se note que cubren.")]
        [SerializeField] Sprite[] smallObstacleOutlines;
        [SerializeField] Sprite[] bigObstacleOutlines;
        [Tooltip("Color del contorno de los obstáculos.")]
        [SerializeField] Color obstacleOutline = new(0.95f, 0.77f, 0.35f, 0.85f);

        [Header("Ambiente (piso 2, opcional)")]
        [Tooltip("Cáusticas sobre el suelo: 16 tiles animados, uno por celda de un mosaico de 4×4 (índice x % 4 + 4 · (y % 4)).")]
        [SerializeField] TileBase[] caustics;
        [Tooltip("Rayos de luz que cuelgan de la pared de arriba (cuadros de la animación).")]
        [SerializeField] Sprite[] lightRay;
        [SerializeField, Min(0.01f)] float lightRayFrameDuration = 0.25f;
        [Tooltip("Copos de nieve marina en una sala normal (26×12 de suelo); crece con el área.")]
        [SerializeField, Min(0)] int marineSnow;
        [Tooltip("Luces 2D de la decoración y los obstáculos que brillan.")]
        [SerializeField] Glow[] glows = System.Array.Empty<Glow>();

        public TileBase MissingTile => missing;
        public TileBase[] Decorations => decorations;
        public float DecorationChance => decorationChance;
        public Sprite Doorway => doorway;
        public Sprite[] DoorTop => doorTop;
        public Sprite[] DoorBottom => doorBottom;
        public Sprite[] DoorSide => doorSide;
        public DoorAnimation SlabTop => slabTop;
        public DoorAnimation SlabBottom => slabBottom;
        public DoorAnimation SlabSide => slabSide;
        public Sprite[] SmallObstacles => smallObstacles ?? System.Array.Empty<Sprite>();
        public Sprite[] BigObstacles => bigObstacles ?? System.Array.Empty<Sprite>();
        public Color ObstacleOutline => obstacleOutline;

        /// <summary>Contorno del sprite de un obstáculo, o null si no tiene.</summary>
        public Sprite OutlineOf(Sprite obstacle)
        {
            if (obstacle == null)
                return null;
            int small = System.Array.IndexOf(SmallObstacles, obstacle);
            if (small >= 0 && smallObstacleOutlines != null && small < smallObstacleOutlines.Length)
                return smallObstacleOutlines[small];
            int big = System.Array.IndexOf(BigObstacles, obstacle);
            if (big >= 0 && bigObstacleOutlines != null && big < bigObstacleOutlines.Length)
                return bigObstacleOutlines[big];
            return null;
        }

        public TileBase[] Caustics => caustics;
        public bool HasCaustics => caustics != null && caustics.Length == 16;
        public Sprite[] LightRay => lightRay ?? System.Array.Empty<Sprite>();
        public float LightRayFrameDuration => lightRayFrameDuration;
        public int MarineSnow => marineSnow;
        public Glow[] Glows => glows ?? System.Array.Empty<Glow>();

        /// <summary>Luz 2D de lo que se ve con este tile o este sprite, o null si no brilla.</summary>
        public Glow? GlowOf(Object source)
        {
            if (source == null)
                return null;
            foreach (var glow in Glows)
            {
                if (glow.source == source)
                    return glow;
            }
            return null;
        }

        /// <summary>
        /// Tile de una pieza. <paramref name="hash"/> elige variante de forma estable para cada celda.
        /// Devuelve el tile de "falta pieza" si el tema no la tiene.
        /// </summary>
        public TileBase Get(Piece piece, int hash)
        {
            var tile = piece switch
            {
                Piece.Floor => hash % 3 == 0 ? Pick(floorVariants, hash / 3) ?? floor : floor,
                Piece.FloorShadow => Pick(floorShadow, hash) ?? floor,
                Piece.Cap => cap,
                Piece.FaceUpper => Pick(faceUpper, hash),
                Piece.FaceLower => Pick(faceLower, hash),
                Piece.FaceUpperEndLeft => faceUpperEndLeft,
                Piece.FaceUpperEndRight => faceUpperEndRight,
                Piece.FaceLowerEndLeft => faceLowerEndLeft,
                Piece.FaceLowerEndRight => faceLowerEndRight,
                Piece.EdgeLeft => edgeLeft,
                Piece.EdgeRight => edgeRight,
                Piece.EdgeBottom => edgeBottom,
                Piece.OuterTopLeft => outerTopLeft,
                Piece.OuterTopRight => outerTopRight,
                Piece.CornerBottomLeft => cornerBottomLeft,
                Piece.CornerBottomRight => cornerBottomRight,
                _ => null
            };

            return tile != null || piece == Piece.None ? tile : missing;
        }

        static TileBase Pick(TileBase[] tiles, int hash) =>
            tiles == null || tiles.Length == 0 ? null : tiles[(hash & int.MaxValue) % tiles.Length];

        internal void Configure(TileBase newFloor, TileBase[] newFloorVariants, TileBase[] newFloorShadow, TileBase newCap,
            TileBase[] newFaceUpper, TileBase[] newFaceLower, TileBase newEdgeLeft, TileBase newEdgeRight, TileBase newEdgeBottom,
            TileBase newCornerBottomLeft, TileBase newCornerBottomRight, TileBase newMissing)
        {
            floor = newFloor;
            floorVariants = newFloorVariants;
            floorShadow = newFloorShadow;
            cap = newCap;
            faceUpper = newFaceUpper;
            faceLower = newFaceLower;
            edgeLeft = newEdgeLeft;
            edgeRight = newEdgeRight;
            edgeBottom = newEdgeBottom;
            cornerBottomLeft = newCornerBottomLeft;
            cornerBottomRight = newCornerBottomRight;
            missing = newMissing;
        }

        internal void ConfigureShapes(TileBase newFaceUpperEndLeft, TileBase newFaceUpperEndRight, TileBase newFaceLowerEndLeft,
            TileBase newFaceLowerEndRight, TileBase newOuterTopLeft, TileBase newOuterTopRight)
        {
            faceUpperEndLeft = newFaceUpperEndLeft;
            faceUpperEndRight = newFaceUpperEndRight;
            faceLowerEndLeft = newFaceLowerEndLeft;
            faceLowerEndRight = newFaceLowerEndRight;
            outerTopLeft = newOuterTopLeft;
            outerTopRight = newOuterTopRight;
        }

        internal void ConfigureDecorations(TileBase[] newDecorations, float newChance)
        {
            decorations = newDecorations;
            decorationChance = newChance;
        }

        internal void ConfigureDoors(Sprite newDoorway, Sprite[] newDoorTop, Sprite[] newDoorBottom, Sprite[] newDoorSide)
        {
            doorway = newDoorway;
            doorTop = newDoorTop;
            doorBottom = newDoorBottom;
            doorSide = newDoorSide;
        }

        internal void ConfigureSlabs(DoorAnimation newTop, DoorAnimation newBottom, DoorAnimation newSide)
        {
            slabTop = newTop;
            slabBottom = newBottom;
            slabSide = newSide;
        }

        internal void ConfigureObstacles(Sprite[] newSmall, Sprite[] newBig, Sprite[] newSmallOutlines = null, Sprite[] newBigOutlines = null)
        {
            smallObstacles = newSmall;
            bigObstacles = newBig;
            smallObstacleOutlines = newSmallOutlines;
            bigObstacleOutlines = newBigOutlines;
        }

        internal void ConfigureObstacleOutline(Color newColor) => obstacleOutline = newColor;

        internal void ConfigureAmbience(TileBase[] newCaustics, Sprite[] newLightRay, float newLightRayFrameDuration, int newMarineSnow,
            Glow[] newGlows)
        {
            caustics = newCaustics;
            lightRay = newLightRay;
            lightRayFrameDuration = Mathf.Max(0.01f, newLightRayFrameDuration);
            marineSnow = Mathf.Max(0, newMarineSnow);
            glows = newGlows ?? System.Array.Empty<Glow>();
        }
    }

    /// <summary>
    /// Animaciones de una puerta con cuadros distintos para cerrar y para abrir, cada cuadro con su duración (las losas del
    /// piso 2: al cerrar chocan, al abrir se enciende el sello y se retiran).
    /// </summary>
    [System.Serializable]
    public class DoorAnimation
    {
        [Tooltip("De vacía (0) a cerrada (último).")]
        public Sprite[] close = System.Array.Empty<Sprite>();
        [Tooltip("Duración de cada cuadro de cerrar, en segundos.")]
        public float[] closeDurations = System.Array.Empty<float>();
        [Tooltip("De cerrada (0) a vacía (último).")]
        public Sprite[] open = System.Array.Empty<Sprite>();
        public float[] openDurations = System.Array.Empty<float>();

        public bool IsValid => close is { Length: > 0 } && open is { Length: > 0 };

        public DoorAnimation() { }

        public DoorAnimation(Sprite[] close, float[] closeDurations, Sprite[] open, float[] openDurations)
        {
            this.close = close ?? System.Array.Empty<Sprite>();
            this.closeDurations = closeDurations ?? System.Array.Empty<float>();
            this.open = open ?? System.Array.Empty<Sprite>();
            this.openDurations = openDurations ?? System.Array.Empty<float>();
        }
    }

    /// <summary>Luz 2D suave de una pieza que brilla (bioluminiscencia del Abismo), con un latido opcional.</summary>
    [System.Serializable]
    public struct Glow
    {
        [Tooltip("Tile de decoración o sprite de obstáculo que lleva la luz.")]
        public Object source;
        public Color color;
        [Min(0.1f)] public float radius;
        [Tooltip("Intensidad mínima y máxima; iguales, sin latido.")]
        public Vector2 intensity;
        [Tooltip("Segundos de un latido completo (0: sin latido).")]
        [Min(0f)] public float period;

        public Glow(Object source, Color color, float radius, Vector2 intensity, float period)
        {
            this.source = source;
            this.color = color;
            this.radius = radius;
            this.intensity = intensity;
            this.period = period;
        }
    }
}

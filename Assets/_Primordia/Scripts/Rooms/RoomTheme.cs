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
        [Tooltip("Variantes de la tapa (una mata más); salen en 1 de cada 4 tiles. Vacío: siempre la tapa normal.")]
        [SerializeField] TileBase[] capVariants;
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
        [Header("Columna de pared junto a la cara (opcionales: sin ellas, EdgeLeft / EdgeRight)")]
        [SerializeField] TileBase edgeLeftFaceUpper;
        [SerializeField] TileBase edgeLeftFaceLower;
        [SerializeField] TileBase edgeRightFaceUpper;
        [SerializeField] TileBase edgeRightFaceLower;
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

        [Header("Fosas (piso 2, opcional)")]
        [Tooltip("16 tiles por máscara: suma de los vecinos que NO son fosa (N=1, E=2, S=4, O=8).")]
        [SerializeField] TileBase[] pitTiles;
        [Tooltip("Esquinas de dentro, encima del tile: NE, NO, SE, SO.")]
        [SerializeField] TileBase[] pitCorners;
        [Tooltip("Motas que suben del fondo, en algunas celdas de fosa.")]
        [SerializeField] TileBase pitMotes;
        [SerializeField, Range(0f, 1f)] float pitMotesChance = 0.12f;

        [Header("Maleza quemable (piso 3, opcional)")]
        [Tooltip("Sella una puerta de atajo: en la pared de arriba (2×2), en una pared lateral (izquierda; la derecha se voltea) y en la de abajo.")]
        [SerializeField] BrushAnimation brushTop;
        [SerializeField] BrushAnimation brushSide;
        [SerializeField] BrushAnimation brushBottom;

        [Header("Helechos altos (piso 3, opcional)")]
        [Tooltip("Tipos de helecho del tramo; cada sala los reparte al azar según el peso de cada uno.")]
        [SerializeField] FernSpec[] ferns = System.Array.Empty<FernSpec>();
        [Tooltip("Helechos por cada tile de suelo libre de la sala (0 = ninguno).")]
        [SerializeField, Range(0f, 0.2f)] float fernDensity;

        [Header("Metano (piso 3, opcional)")]
        [SerializeField] MethaneSpec methane;

        [Header("Planta venenosa (piso 3, opcional)")]
        [SerializeField] PoisonSpec poison;

        [Header("Escenario vivo (piso 3, opcional)")]
        [SerializeField] SceneryArt scenery;

        [Header("Lodo profundo (piso 3, opcional)")]
        [Tooltip("16 tiles por máscara: suma de los vecinos que NO son lodo (N=1, E=2, S=4, O=8).")]
        [SerializeField] TileBase[] mudTiles;
        [Tooltip("Esquinas de dentro, encima del tile: NE, NO, SE, SO.")]
        [SerializeField] TileBase[] mudCorners;
        [Tooltip("Burbujas que suben y revientan, en algunas celdas de lodo.")]
        [SerializeField] TileBase mudBubbles;
        [SerializeField, Range(0f, 1f)] float mudBubblesChance = 0.1f;

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
        public bool HasPits => pitTiles != null && pitTiles.Length == 16;
        public TileBase PitTile(int mask) => HasPits ? pitTiles[mask & 15] : null;
        public TileBase PitCorner(int index) => pitCorners != null && index < pitCorners.Length ? pitCorners[index] : null;
        public BrushAnimation BrushFor(Vector2Int direction)
        {
            var brush = direction == Vector2Int.up ? brushTop : direction == Vector2Int.down ? brushBottom : brushSide;
            return brush != null && brush.IsValid ? brush : null;
        }

        public MethaneSpec Methane => methane;
        public SceneryArt Scenery => scenery;
        public PoisonSpec Poison => poison;
        public FernSpec[] Ferns => ferns ?? System.Array.Empty<FernSpec>();
        public float FernDensity => fernDensity;

        public bool HasMud => mudTiles != null && mudTiles.Length == 16;
        public TileBase MudTile(int mask) => HasMud ? mudTiles[mask & 15] : null;
        public TileBase MudCorner(int index) => mudCorners != null && index < mudCorners.Length ? mudCorners[index] : null;
        public TileBase MudBubbles => mudBubbles;
        public float MudBubblesChance => mudBubblesChance;
        public TileBase PitMotes => pitMotes;
        public float PitMotesChance => pitMotesChance;

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
        /// Tile de una pieza. <paramref name="hash"/> elige variante de forma estable para cada celda. Las caras van en pares:
        /// la de abajo se pide con el hash de la celda de arriba, así las dos eligen el mismo índice y el tronco sigue.
        /// Devuelve el tile de "falta pieza" si el tema no la tiene.
        /// </summary>
        public TileBase Get(Piece piece, int hash)
        {
            var tile = piece switch
            {
                Piece.Floor => hash % 3 == 0 ? Pick(floorVariants, hash / 3) ?? floor : floor,
                Piece.FloorShadow => Pick(floorShadow, hash) ?? floor,
                Piece.Cap => hash % 4 == 0 ? Pick(capVariants, hash / 4) ?? cap : cap,
                Piece.FaceUpper => Pick(faceUpper, hash),
                Piece.FaceLower => Pick(faceLower, hash),
                Piece.FaceUpperEndLeft => faceUpperEndLeft,
                Piece.FaceUpperEndRight => faceUpperEndRight,
                Piece.FaceLowerEndLeft => faceLowerEndLeft,
                Piece.FaceLowerEndRight => faceLowerEndRight,
                Piece.EdgeLeft => edgeLeft,
                Piece.EdgeRight => edgeRight,
                Piece.EdgeBottom => edgeBottom,
                Piece.EdgeLeftFaceUpper => edgeLeftFaceUpper != null ? edgeLeftFaceUpper : edgeLeft,
                Piece.EdgeLeftFaceLower => edgeLeftFaceLower != null ? edgeLeftFaceLower : edgeLeft,
                Piece.EdgeRightFaceUpper => edgeRightFaceUpper != null ? edgeRightFaceUpper : edgeRight,
                Piece.EdgeRightFaceLower => edgeRightFaceLower != null ? edgeRightFaceLower : edgeRight,
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

        internal void ConfigureWallExtras(TileBase[] newCapVariants, TileBase newEdgeLeftFaceUpper, TileBase newEdgeLeftFaceLower,
            TileBase newEdgeRightFaceUpper, TileBase newEdgeRightFaceLower)
        {
            capVariants = newCapVariants;
            edgeLeftFaceUpper = newEdgeLeftFaceUpper;
            edgeLeftFaceLower = newEdgeLeftFaceLower;
            edgeRightFaceUpper = newEdgeRightFaceUpper;
            edgeRightFaceLower = newEdgeRightFaceLower;
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

        internal void ConfigurePits(TileBase[] newTiles, TileBase[] newCorners, TileBase newMotes)
        {
            pitTiles = newTiles;
            pitCorners = newCorners;
            pitMotes = newMotes;
        }

        internal void ConfigureBrush(BrushAnimation newTop, BrushAnimation newSide, BrushAnimation newBottom)
        {
            brushTop = newTop;
            brushSide = newSide;
            brushBottom = newBottom;
        }

        internal void ConfigureMethane(MethaneSpec newMethane) => methane = newMethane;

        internal void ConfigureScenery(SceneryArt newScenery) => scenery = newScenery;

        internal void ConfigurePoison(PoisonSpec newPoison) => poison = newPoison;

        internal void ConfigureFerns(FernSpec[] newFerns, float newDensity)
        {
            ferns = newFerns;
            fernDensity = newDensity;
        }

        internal void ConfigureMud(TileBase[] newTiles, TileBase[] newCorners, TileBase newBubbles)
        {
            mudTiles = newTiles;
            mudCorners = newCorners;
            mudBubbles = newBubbles;
        }

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

    /// <summary>Metano de un tramo: el odre (reposo, reventón, restos), la niebla y cuánto sale en cada sala.</summary>
    [System.Serializable]
    public class MethaneSpec
    {
        [Tooltip("Odre (24×26, bucle). Pivote: abajo.")]
        public Sprite[] sacIdle = System.Array.Empty<Sprite>();
        [Tooltip("Reventón del odre (32×32). El cuadro 2 es el que explota.")]
        public Sprite[] sacBurst = System.Array.Empty<Sprite>();
        [Tooltip("La bolsa rota que queda.")]
        public Sprite sacRemains;
        [Tooltip("Niebla de metano (64×32, bucle). Pivote: abajo.")]
        public Sprite[] mist = System.Array.Empty<Sprite>();
        [Tooltip("Odres por sala (mínimo y máximo).")]
        public Vector2Int sacCount = new(1, 2);
        [Tooltip("Manchas de niebla por cada tile de suelo libre de la sala.")]
        [Range(0f, 0.05f)] public float mistPerCell = 0.004f;

        public bool HasSacs => sacIdle != null && sacIdle.Length > 0 && sacBurst != null && sacBurst.Length > 0 && sacCount.y > 0;
        public bool HasMist => mist != null && mist.Length > 0 && mistPerCell > 0f;
    }

    /// <summary>Planta venenosa de un tramo: el bulbo (reposo, reventón, cáscara, rebrote) y cuántos salen en cada sala.</summary>
    [System.Serializable]
    public class PoisonSpec
    {
        [Tooltip("Bulbo (32×32, bucle). Pivote: abajo.")]
        public Sprite[] bulbIdle = System.Array.Empty<Sprite>();
        [Tooltip("Reventón (32×32). El cuadro 2 suelta el charco.")]
        public Sprite[] bulbBurst = System.Array.Empty<Sprite>();
        [Tooltip("Cáscara abierta mientras espera el rebrote.")]
        public Sprite bulbStump;
        [Tooltip("Rebrote (32×32).")]
        public Sprite[] bulbRegrow = System.Array.Empty<Sprite>();
        [Tooltip("Bulbos por sala (mínimo y máximo).")]
        public Vector2Int bulbCount = new(0, 1);

        public bool HasBulbs => bulbIdle != null && bulbIdle.Length > 0 && bulbBurst != null && bulbBurst.Length > 0 && bulbCount.y > 0;
    }

    /// <summary>Un tipo de charco del escenario (lodo o agua) con su forma.</summary>
    [System.Serializable]
    public class PuddleType
    {
        public string name;
        [Tooltip("Cuadros en bucle (centro).")]
        public Sprite[] frames = System.Array.Empty<Sprite>();
        [Min(0.01f)] public float frameSeconds = 0.25f;
        [Tooltip("Lodo: frena un 40 %. Agua de charca: solo se ve.")]
        public bool sludge;
    }

    /// <summary>
    /// Escenario vivo de un tramo del piso 3: fumarolas, charcos de lodo y agua, musgo que gotea, escarabajos, mosquitos, brasas y
    /// humo de tocones, con cuántos salen en cada sala.
    /// </summary>
    [System.Serializable]
    public class SceneryArt
    {
        [Header("Fumarolas")]
        public Sprite[] ventIdle = System.Array.Empty<Sprite>();
        public Sprite[] ventWarn = System.Array.Empty<Sprite>();
        public Sprite[] ventBlast = System.Array.Empty<Sprite>();
        public Vector2Int ventCount;

        [Header("Charcos")]
        public PuddleType[] puddles = System.Array.Empty<PuddleType>();
        public Vector2Int puddleCount;
        [Tooltip("De los charcos, la fracción que son de agua (el resto, de lodo).")]
        [Range(0f, 1f)] public float waterShare = 0.6f;
        [Tooltip("Probabilidad de que un charco de agua tenga mosquitos encima.")]
        [Range(0f, 1f)] public float gnatChance;
        public Sprite[] gnats = System.Array.Empty<Sprite>();

        [Header("Bichitos y ambiente")]
        public Sprite[] beetleIdle = System.Array.Empty<Sprite>();
        public Sprite[] beetleRun = System.Array.Empty<Sprite>();
        public Vector2Int beetleCount;
        public Sprite[] embers = System.Array.Empty<Sprite>();
        public Vector2Int emberCount;

        [Header("Musgo que gotea")]
        public Sprite[] dripMoss = System.Array.Empty<Sprite>();
        [Tooltip("Probabilidad de musgo en cada sitio posible de la cara de la pared.")]
        [Range(0f, 0.3f)] public float mossChance;

        [Header("Tocones quemados")]
        [Tooltip("El sprite del tocón de las rocas pequeñas: lleva humo encima.")]
        public Sprite stump;
        public Sprite[] stumpSmoke = System.Array.Empty<Sprite>();

        public bool HasVents => ventIdle != null && ventIdle.Length > 0 && ventBlast != null && ventBlast.Length > 0 && ventCount.y > 0;
        public bool HasPuddles => puddles != null && puddles.Length > 0 && puddleCount.y > 0;
    }

    /// <summary>Maleza quemable: reposo, fuego cuadro a cuadro (con la duración de cada uno) y lo que queda, ya abierta.</summary>
    [System.Serializable]
    public class BrushAnimation
    {
        public Sprite idle;
        [Tooltip("El fuego prende, avanza y deshace los tallos.")]
        public Sprite[] burn = System.Array.Empty<Sprite>();
        public float[] durations = System.Array.Empty<float>();
        [Tooltip("Paso abierto para quedarse (ceniza con ascuas).")]
        public Sprite gone;
        [Tooltip("Cuadro del fuego (desde 0) a partir del cual ya no bloquea.")]
        public int opensAtFrame = 5;

        public bool IsValid => idle != null && burn is { Length: > 0 };

        public BrushAnimation() { }

        public BrushAnimation(Sprite idle, Sprite[] burn, float[] durations, Sprite gone, int opensAtFrame)
        {
            this.idle = idle;
            this.burn = burn ?? System.Array.Empty<Sprite>();
            this.durations = durations ?? System.Array.Empty<float>();
            this.gone = gone;
            this.opensAtFrame = opensAtFrame;
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

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

        public TileBase MissingTile => missing;
        public TileBase[] Decorations => decorations;
        public float DecorationChance => decorationChance;
        public Sprite Doorway => doorway;
        public Sprite[] DoorTop => doorTop;
        public Sprite[] DoorBottom => doorBottom;
        public Sprite[] DoorSide => doorSide;

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
    }
}

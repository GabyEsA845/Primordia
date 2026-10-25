using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Pieza del escenario que va en una celda. Los nombres siguen los del arte (Wall_EdgeLeft…).</summary>
    public enum Piece
    {
        /// <summary>Nada en la capa de paredes ni en la de suelo (el arco de puerta lo dibuja otro objeto).</summary>
        None,

        Floor,

        /// <summary>Primera fila de suelo bajo una pared: sombra.</summary>
        FloorShadow,

        /// <summary>Relleno de pared visto desde arriba.</summary>
        Cap,

        /// <summary>Cara frontal de una pared con suelo debajo: fila de arriba y fila que toca el suelo.</summary>
        FaceUpper,
        FaceLower,

        /// <summary>Extremos de la cara cuando hay suelo al lado (entrantes y columnas). Arte pendiente.</summary>
        FaceUpperEndLeft,
        FaceUpperEndRight,
        FaceLowerEndLeft,
        FaceLowerEndRight,

        /// <summary>Pared con suelo a la derecha: borde a la derecha.</summary>
        EdgeLeft,

        /// <summary>Pared con suelo a la izquierda: borde a la izquierda.</summary>
        EdgeRight,

        /// <summary>Pared con suelo encima: borde arriba.</summary>
        EdgeBottom,

        /// <summary>Esquina de una masa de pared con suelo encima y a un lado (entrantes y columnas). Arte pendiente.</summary>
        OuterTopLeft,
        OuterTopRight,

        /// <summary>Esquinas inferiores de la sala: suelo solo en la diagonal de arriba.</summary>
        CornerBottomLeft,
        CornerBottomRight,

        /// <summary>Forma que el arte no puede dibujar (pared de 1 tile de grosor, cara demasiado baja…).</summary>
        Missing
    }

    /// <summary>
    /// Elige la pieza de cada celda mirando a sus vecinos. Vista cenital en 3/4: la pared de arriba enseña
    /// su cara (dos filas) sobre el suelo; las demás paredes solo un borde del lado que da al suelo.
    /// </summary>
    public static class WallRules
    {
        public static Piece Classify(FloorMap map, Vector2Int cell)
        {
            int x = cell.x, y = cell.y;
            switch (map.Get(x, y))
            {
                case FloorMap.Kind.DoorArch:
                    return Piece.None;
                case FloorMap.Kind.DoorGap:
                    return Piece.Floor;
                case FloorMap.Kind.Floor:
                case FloorMap.Kind.Passage:
                case FloorMap.Kind.Obstacle:
                case FloorMap.Kind.Pit:
                    return HasWallAbove(map, x, y) ? Piece.FloorShadow : Piece.Floor;
            }

            bool up = map.IsOpen(x, y + 1);
            bool left = map.IsOpen(x - 1, y);
            bool right = map.IsOpen(x + 1, y);

            bool faceLower = map.IsOpen(x, y - 1);
            bool faceUpper = !faceLower && map.IsOpen(x, y - 2);
            if (faceLower || faceUpper)
            {
                // Una cara necesita al menos una fila de pared encima, y no puede tener suelo a ambos lados.
                if (up || (left && right))
                    return Piece.Missing;
                if (faceLower)
                    return left ? Piece.FaceLowerEndLeft : right ? Piece.FaceLowerEndRight : Piece.FaceLower;
                return left ? Piece.FaceUpperEndLeft : right ? Piece.FaceUpperEndRight : Piece.FaceUpper;
            }

            if (left && right)
                return Piece.Missing;
            if (up && left)
                return Piece.OuterTopLeft;
            if (up && right)
                return Piece.OuterTopRight;
            if (up)
                return Piece.EdgeBottom;
            if (left)
                return Piece.EdgeRight;
            if (right)
                return Piece.EdgeLeft;

            // Junto a la cara de una pared de arriba, el borde lateral sigue hasta lo alto de la cara.
            if (IsFace(map, x - 1, y))
                return Piece.EdgeRight;
            if (IsFace(map, x + 1, y))
                return Piece.EdgeLeft;

            if (map.IsOpen(x + 1, y + 1))
                return Piece.CornerBottomLeft;
            if (map.IsOpen(x - 1, y + 1))
                return Piece.CornerBottomRight;

            return Piece.Cap;
        }

        /// <summary>True si la pieza es de la capa de paredes (con colisión).</summary>
        public static bool IsWall(Piece piece) =>
            piece != Piece.None && piece != Piece.Floor && piece != Piece.FloorShadow;

        static bool IsFace(FloorMap map, int x, int y) =>
            map.Get(x, y) == FloorMap.Kind.Wall && (map.IsOpen(x, y - 1) || map.IsOpen(x, y - 2));

        // Los huecos de puerta se dibujan como suelo: no proyectan sombra.
        static bool HasWallAbove(FloorMap map, int x, int y)
        {
            var above = map.Get(x, y + 1);
            return above == FloorMap.Kind.Wall || above == FloorMap.Kind.DoorArch;
        }
    }
}

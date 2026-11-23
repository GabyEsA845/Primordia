using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Arte del decorado de la sala de paso del piso 3 (docs/multimedia-temp/floor3-exit/decor): piezas sueltas (una imagen = un
    /// cuadro) y hojas de animación (plantas, ráfagas), cada una por su nombre. Lo crea el menú de la sala de paso del piso 3.
    /// </summary>
    public class ExitDecorArt : ScriptableObject
    {
        [Serializable]
        public class Piece
        {
            public string id;
            public Sprite[] frames = Array.Empty<Sprite>();
        }

        public Piece[] pieces = Array.Empty<Piece>();

        public Sprite[] Frames(string id)
        {
            foreach (var piece in pieces ?? Array.Empty<Piece>())
                if (piece.id == id && piece.frames != null && piece.frames.Length > 0)
                    return piece.frames;
            return Array.Empty<Sprite>();
        }

        public bool Has(string id) => Frames(id).Length > 0;

        /// <summary>Las variantes numeradas de una pieza (prefijo_0, prefijo_1…), en orden.</summary>
        public List<string> Variants(string prefix)
        {
            var ids = new List<string>();
            for (int i = 0; Has($"{prefix}_{i}"); i++)
                ids.Add($"{prefix}_{i}");
            return ids;
        }
    }
}

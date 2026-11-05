using System;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Arte del decorado de las salas con tema del piso 3 (docs/multimedia-temp/floor3-theme-decor): las piezas de los 5 temas en
    /// cada tramo del incendio (pantano, bosque humeante, tierra quemada) y los tiles de la gran charca de agua. Lo crea
    /// Primordia > Floor 3 > Importar decorado de las salas con tema en Resources/ThemeDecorArt. Sin él, las salas con tema
    /// salen sin decorado.
    /// </summary>
    public class ThemeDecorArt : ScriptableObject
    {
        public const string ResourceName = "ThemeDecorArt";
        public const int Bands = 3;

        /// <summary>Una pieza en un tramo: en reposo (varios cuadros = animada o variantes), ardiendo y quemada.</summary>
        [Serializable]
        public class Look
        {
            public Sprite[] idle = Array.Empty<Sprite>();
            public Sprite[] burn = Array.Empty<Sprite>();
            public Sprite burnt;
        }

        [Serializable]
        public class Piece
        {
            [Tooltip("Nombre de la hoja sin tramo ni estado, p. ej. Nest_Big.")]
            public string id;
            [Tooltip("Pantano, bosque humeante y tierra quemada.")]
            public Look[] bands = new Look[Bands];
        }

        /// <summary>Tiles de la gran charca de un tramo: 16 por máscara, 4 esquinas (NE, NO, SE, SO) y el brillo animado.</summary>
        [Serializable]
        public class Water
        {
            public Sprite[] tiles = Array.Empty<Sprite>();
            public Sprite[] corners = Array.Empty<Sprite>();
            public Sprite[] shimmer = Array.Empty<Sprite>();

            public bool IsValid => tiles != null && tiles.Length == 16;
        }

        public Piece[] pieces = Array.Empty<Piece>();
        public Water[] waters = new Water[Bands];

        static ThemeDecorArt current;
        static bool loaded;

        public static ThemeDecorArt Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<ThemeDecorArt>(ResourceName);
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

        /// <summary>La pieza <paramref name="id"/> en el tramo <paramref name="band"/>, o null si no existe o no tiene dibujo.</summary>
        public Look Get(string id, int band)
        {
            band = Mathf.Clamp(band, 0, Bands - 1);
            foreach (var piece in pieces ?? Array.Empty<Piece>())
            {
                if (piece.id != id || piece.bands == null || band >= piece.bands.Length)
                    continue;
                var look = piece.bands[band];
                return look != null && look.idle != null && look.idle.Length > 0 ? look : null;
            }
            return null;
        }

        public Water GetWater(int band)
        {
            band = Mathf.Clamp(band, 0, Bands - 1);
            return waters != null && band < waters.Length && waters[band] != null && waters[band].IsValid ? waters[band] : null;
        }
    }
}

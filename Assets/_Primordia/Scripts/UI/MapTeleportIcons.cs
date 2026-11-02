using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Iconos del mapa para teletransportes, regreso y medio corazón sin recoger. Con el arte importado
    /// (<see cref="UiExtrasArt"/>) se usa ese; sin él, dibujos PROVISIONALES por código de 7×7 px.
    /// </summary>
    public static class MapTeleportIcons
    {
        static readonly string[] Ring =
        {
            "..ooo..",
            ".oaaao.",
            "oaa.aao",
            "oa...ao",
            "oaa.aao",
            ".oaaao.",
            "..ooo..",
        };

        static readonly string[] Disc =
        {
            "..ooo..",
            ".oyyyo.",
            "oyyyyyo",
            "oyyoyyo",
            "oyyyyyo",
            ".oyyyo.",
            "..ooo..",
        };

        static readonly string[] HalfHeart =
        {
            ".......",
            ".oo.oo.",
            "ohroeeo",
            "orrree.",
            ".orree.",
            "..ore..",
            "...o...",
        };

        static Sprite teleport;
        static Sprite returnMark;
        static Sprite heart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            teleport = null;
            returnMark = null;
            heart = null;
        }

        /// <summary>Cuadro <paramref name="index"/> del remolino del teletransporte (el arte gira; el provisional es fijo).</summary>
        public static Sprite TeleportFrame(int index, bool hover)
        {
            var art = UiExtrasArt.Current;
            var frames = art != null ? (hover ? art.mapTeleportHover : art.mapTeleport) : null;
            if (frames != null && frames.Length > 0)
                return frames[index % frames.Length];
            return Teleport;
        }

        /// <summary>El icono apagado del teletransporte, si hay arte (el provisional se apaga con la transparencia).</summary>
        public static Sprite TeleportDim => UiExtrasArt.Current != null ? UiExtrasArt.Current.mapTeleportDim : null;

        public static bool HasArt => UiExtrasArt.Current != null && UiExtrasArt.Current.mapTeleport.Length > 0;

        public static Sprite Teleport => teleport != null ? teleport : teleport = Build(Ring, new Color32(120, 245, 225, 255));

        public static Sprite Return => returnMark != null ? returnMark : returnMark = Build(Disc, new Color32(255, 190, 60, 255));

        /// <summary>Marca de regreso: la flecha del arte (resaltada o apagada) o el círculo provisional.</summary>
        public static Sprite ReturnIcon(bool hover, bool dim)
        {
            var art = UiExtrasArt.Current;
            if (art != null && art.mapReturn != null)
                return dim && art.mapReturnDim != null ? art.mapReturnDim : hover && art.mapReturnHover != null ? art.mapReturnHover : art.mapReturn;
            return Return;
        }

        public static Sprite Heart
        {
            get
            {
                var art = UiExtrasArt.Current;
                if (art != null && art.mapHeart != null)
                    return art.mapHeart;
                return heart != null ? heart : heart = Build(HalfHeart, new Color32(240, 112, 110, 255));
            }
        }

        static Sprite Build(string[] rows, Color32 fill)
        {
            var colors = new Dictionary<char, Color32>
            {
                ['o'] = new Color32(24, 20, 37, 255),
                ['a'] = fill,
                ['y'] = fill,
                ['r'] = fill,
                ['h'] = new Color32(255, 208, 192, 255),
                ['e'] = new Color32(74, 34, 52, 255),
            };
            int size = rows.Length;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, colors.TryGetValue(rows[size - 1 - y][x], out var color) ? color : new Color32(0, 0, 0, 0));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}

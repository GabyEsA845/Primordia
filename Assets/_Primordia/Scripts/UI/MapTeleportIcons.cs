using System.Collections.Generic;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Iconos PROVISIONALES del mapa para los teletransportes (anillo aguamarina) y la marca de regreso (círculo
    /// ámbar), dibujados por código, de 7×7 px como las demás marcas, hasta que haya arte.
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

        static Sprite teleport;
        static Sprite returnMark;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            teleport = null;
            returnMark = null;
        }

        public static Sprite Teleport => teleport != null ? teleport : teleport = Build(Ring, new Color32(120, 245, 225, 255));

        public static Sprite Return => returnMark != null ? returnMark : returnMark = Build(Disc, new Color32(255, 190, 60, 255));

        static Sprite Build(string[] rows, Color32 fill)
        {
            var colors = new Dictionary<char, Color32>
            {
                ['o'] = new Color32(10, 40, 50, 255),
                ['a'] = fill,
                ['y'] = fill,
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

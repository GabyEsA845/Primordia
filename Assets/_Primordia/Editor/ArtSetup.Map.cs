using System.IO;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Minimapa y mapa grande (docs/multimedia-temp/map/). Añade a GameUI el minimapa arriba a la derecha (marco,
    /// fondo y una ventana recortada donde va el plano) y el mapa grande que se abre con TAB (velo oscuro, panel
    /// de vidrio, título "MAPA" y reflejo), y le da a <see cref="MapUI"/> la paleta del plano y los iconos.
    /// Lo que crea se rehace entero cada vez.
    /// </summary>
    public static partial class ArtSetup
    {
        const string MapSourceFolder = "docs/multimedia-temp/map";
        const string MapArtFolder = "Assets/_Primordia/Art/UI/Map";

        static readonly Vector2 MinimapSize = new(88f, 68f);
        static readonly Vector2 BigMapSize = new(400f, 220f);
        const float MinimapBorder = 4f;
        const float BigMapBorder = 12f;

        [MenuItem("Primordia/Art/Importar mapa")]
        static void ImportMap()
        {
            var frame = MapSprite("UI_Minimap_Frame", 6);
            var back = MapSprite("UI_Minimap_Back", 2);
            var panel = MapSprite("UI_Map_Panel", 12);
            var glint = MapSprite("UI_Map_Glint", 12);
            var boss = MapSprite("UI_Map_Boss", 0);
            var reward = MapSprite("UI_Map_Reward", 0);
            string playerSheet = $"{MapArtFolder}/UI_Map_Player.png";
            var player = CopyArt($"{MapSourceFolder}/UI_Map_Player.png", playerSheet) ? SliceSheet(playerSheet, 7) : System.Array.Empty<Sprite>();
            var palette = ReadPalette();
            // De los menús (Primordia > Art > Importar menús).
            var title = MenuSprite("Pause", "UI_Pause_Title", new Vector4(8, 0, 8, 0));

            EditPrefab(UISetup.GameUIPath, root =>
            {
                // Minimapa: fondo, ventana del plano y marco encima.
                var minimap = Recreate("Minimap", root.transform);
                minimap.anchorMin = minimap.anchorMax = minimap.pivot = Vector2.one;
                minimap.anchoredPosition = new Vector2(-8f, -8f);
                minimap.sizeDelta = MinimapSize;
                Inset(SlicedImage(Recreate("Back", minimap).gameObject, back).rectTransform, MinimapBorder);
                var minimapViewport = Recreate("Viewport", minimap);
                Inset(minimapViewport, MinimapBorder);
                minimapViewport.gameObject.AddComponent<RectMask2D>();
                Inset(SlicedImage(Recreate("Frame", minimap).gameObject, frame).rectTransform, 0f);

                // Mapa grande: velo, panel, plano recortado al interior, reflejo y título sobre el borde de arriba.
                var bigMap = Recreate("MapPanel", root.transform);
                UISetup.Stretch(bigMap);
                var veil = bigMap.gameObject.AddComponent<Image>();
                veil.color = new Color(0f, 0f, 0f, 0.75f);

                var glass = Recreate("Panel", bigMap);
                glass.anchorMin = glass.anchorMax = glass.pivot = new Vector2(0.5f, 0.5f);
                glass.sizeDelta = BigMapSize;
                glass.anchoredPosition = new Vector2(0f, -6f);
                SlicedImage(glass.gameObject, panel);
                var bigViewport = Recreate("Viewport", glass);
                Inset(bigViewport, BigMapBorder);
                bigViewport.gameObject.AddComponent<RectMask2D>();
                Inset(SlicedImage(Recreate("Glint", glass).gameObject, glint).rectTransform, 0f);

                var banner = Recreate("TitleBanner", glass);
                banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
                banner.pivot = new Vector2(0.5f, 0.5f);
                banner.anchoredPosition = Vector2.zero;
                banner.sizeDelta = new Vector2(96f, 16f);
                SlicedImage(banner.gameObject, title);
                var titleText = UISetup.CreateText(banner, "MAPA", 16, UISetup.TitleColor);
                titleText.raycastTarget = false;
                UISetup.Stretch(titleText.rectTransform);
                bigMap.gameObject.SetActive(false);

                var mapUI = GetOrAdd<MapUI>(root);
                mapUI.Configure(palette, player, boss, reward);
                mapUI.ConfigureViews(minimap.gameObject, minimapViewport, bigMap.gameObject, bigViewport);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: mapa importado.");
        }

        /// <summary>Ocupa todo el padre menos un borde de ese grosor.</summary>
        static void Inset(RectTransform rect, float border)
        {
            UISetup.Stretch(rect);
            rect.offsetMin = new Vector2(border, border);
            rect.offsetMax = new Vector2(-border, -border);
        }

        static Sprite MapSprite(string file, float border) =>
            ImportSprite($"{MapSourceFolder}/{file}.png", $"{MapArtFolder}/{file}.png", new Vector4(border, border, border, border));

        /// <summary>Los 8 colores de la tira UI_Map_Palette (8×1), de izquierda a derecha.</summary>
        static Color[] ReadPalette()
        {
            string target = $"{MapArtFolder}/UI_Map_Palette.png";
            if (!CopyArt($"{MapSourceFolder}/UI_Map_Palette.png", target))
                return System.Array.Empty<Color>();

            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(target));
                var colors = new Color[texture.width];
                for (int x = 0; x < texture.width; x++)
                    colors[x] = texture.GetPixel(x, 0);
                return colors;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}

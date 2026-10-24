using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de la sala de paso: los temas Shore (intermedio) y Cambrian (piso 2) de docs/multimedia-temp/environment,
    /// generados con generar_tema_cambrico.py, y la fosa, su burbuja y el icono del mapa de
    /// docs/multimedia-temp/floor-transition (generar_fosa.py). Lo usa <see cref="FloorTransitionSetup"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        internal const string ShoreThemeName = "Shore";
        internal const string CambrianThemeName = "Cambrian";
        const string FloorTransitionSourceFolder = "docs/multimedia-temp/floor-transition";
        const string FloorTransitionArtFolder = "Assets/_Primordia/Art/FloorTransition";

        // Medidas de las hojas de generar_fosa.py: la fosa es de 7×5 tiles; la burbuja, de 8×8 píxeles.
        const int PitCellWidth = 112;
        const int BubbleCellWidth = 8;
        const int KeyCellWidth = 13;

        /// <summary>Sprites de la fosa, ya importados.</summary>
        internal sealed class TransitionPitArt
        {
            public Sprite[] Pit;
            public Sprite[] Bubble;
            public Sprite[] Key;
        }

        /// <summary>Importa los temas Shore y Cambrian. Cualquiera puede ser null si faltan sus tiles.</summary>
        internal static (RoomTheme mid, RoomTheme next) ImportTransitionThemes()
        {
            var missing = CreateMissingTile();
            bool shore = ImportTheme(ShoreThemeName, missing);
            bool cambrian = ImportTheme(CambrianThemeName, missing);
            AssetDatabase.SaveAssets();
            return (shore ? LoadTheme(ShoreThemeName) : null, cambrian ? LoadTheme(CambrianThemeName) : null);
        }

        static RoomTheme LoadTheme(string themeName) =>
            AssetDatabase.LoadAssetAtPath<RoomTheme>($"{EnvironmentArtFolder}/{themeName}/Theme_{themeName}.asset");

        /// <summary>Importa la fosa, la burbuja y la tecla E (la de la tienda). Null si no está la fosa.</summary>
        internal static TransitionPitArt ImportTransitionPitArt()
        {
            string pitPath = $"{FloorTransitionArtFolder}/Transition_Pit.png";
            if (!CopyArt($"{FloorTransitionSourceFolder}/Transition_Pit.png", pitPath))
                return null;

            string bubblePath = $"{FloorTransitionArtFolder}/Transition_Bubble.png";
            string keyPath = $"{ShopArtFolder}/UI_Key_E.png";
            return new TransitionPitArt
            {
                Pit = SliceSheet(pitPath, PitCellWidth),
                Bubble = CopyArt($"{FloorTransitionSourceFolder}/Transition_Bubble.png", bubblePath)
                    ? SliceSheet(bubblePath, BubbleCellWidth)
                    : System.Array.Empty<Sprite>(),
                Key = CopyArt($"{ShopSourceFolder}/UI_Key_E.png", keyPath)
                    ? SliceSheet(keyPath, KeyCellWidth, new Vector2(0.5f, 0f))
                    : System.Array.Empty<Sprite>(),
            };
        }

        /// <summary>Pone el icono de la sala de paso (UI_Map_Transition) en el minimapa y el mapa grande.</summary>
        internal static void ApplyTransitionMapIcon()
        {
            var icon = ImportSprite($"{FloorTransitionSourceFolder}/UI_Map_Transition.png", $"{MapArtFolder}/UI_Map_Transition.png",
                Vector4.zero);
            if (icon == null)
                return;

            EditPrefab(UISetup.GameUIPath, root =>
            {
                foreach (var map in root.GetComponentsInChildren<MapUI>(true))
                    map.SetTransitionIcon(icon);
            });
        }
    }
}

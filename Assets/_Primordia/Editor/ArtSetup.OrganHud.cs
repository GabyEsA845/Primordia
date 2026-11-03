using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Recuadro del Órgano especial en el HUD, abajo a la izquierda (docs/multimedia-temp/organ-hud): copia el arte, añade el
    /// <see cref="OrganHud"/> al prefab GameUI y quita del jugador la barra de recarga de antes (<see cref="Primordia.UI.CooldownBar"/>).
    /// Se puede repetir sin miedo.
    /// </summary>
    public static partial class ArtSetup
    {
        const string OrganHudSourceFolder = "docs/multimedia-temp/organ-hud/HUD";
        const string OrganHudArtFolder = "Assets/_Primordia/Art/UI/OrganHud";

        [MenuItem("Primordia/Art/Importar recuadro del Órgano (HUD)")]
        internal static void ImportOrganHud()
        {
            Sprite One(string name) => ImportSprite($"{OrganHudSourceFolder}/{name}.png", $"{OrganHudArtFolder}/{name}.png", Vector4.zero);
            Sprite[] Sheet(string name, int cell) => ImportLooseSheet($"{OrganHudSourceFolder}/{name}.png", $"{OrganHudArtFolder}/{name}.png", cell);

            var panel = One("UI_Organ_Panel");
            var panelEmpty = One("UI_Organ_Panel_Empty");
            var emptyIcon = One("UI_Organ_EmptyIcon");
            var sapFill = One("UI_Organ_SapFill");
            var notch = One("UI_Organ_Notch");
            var bubbles = Sheet("UI_Organ_SapBubbles", 4);
            var top = Sheet("UI_Organ_SapTop", 4);
            var ready = Sheet("UI_Organ_Ready", 46);
            var pulse = Sheet("UI_Organ_Pulse", 46);
            if (panel == null || panelEmpty == null || emptyIcon == null || sapFill == null || notch == null
                || bubbles.Length == 0 || top.Length == 0 || ready.Length == 0 || pulse.Length == 0)
            {
                Debug.LogWarning($"ArtSetup: faltan hojas del recuadro del Órgano en {OrganHudSourceFolder}.");
                return;
            }

            EditPrefab(UISetup.GameUIPath, root =>
            {
                var hud = root.GetComponentInChildren<OrganHud>(true);
                if (hud == null)
                {
                    var rect = UISetup.CreateRect("OrganHud", root.transform);
                    hud = rect.gameObject.AddComponent<OrganHud>();
                }
                hud.ConfigureArt(panel, panelEmpty, emptyIcon, sapFill, notch, bubbles, top, ready, pulse);
                EditorUtility.SetDirty(hud);
            });

            // La barra bajo el jugador ya no existe: debajo va la barra de carga del Chorro de vinagre.
            EditPrefab(PlayerSetup.PlayerPath, root =>
            {
                var bar = root.transform.Find("CooldownBar");
                if (bar != null)
                    Object.DestroyImmediate(bar.gameObject);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: recuadro del Órgano puesto en el HUD (abajo a la izquierda); la barra bajo el jugador se ha quitado.");
        }
    }
}

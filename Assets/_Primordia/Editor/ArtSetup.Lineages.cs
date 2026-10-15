using Primordia.Mutations;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de los beneficios de los linajes (docs/multimedia-temp/lineages/): el medio corazón verde de la barrera,
    /// las matas de hongo de Colonizar la sala (normal y grande) y la estela urticante de la Medusa. Al final rehace
    /// los linajes (<see cref="LineageSetup.CreateLineages"/>), que ponen al jugador los prefabs y efectos.
    /// </summary>
    public static partial class ArtSetup
    {
        const string LineagesSourceFolder = "docs/multimedia-temp/lineages";

        static readonly Character LineageEffectsArt = new()
        {
            Name = "",
            SourceFolder = LineagesSourceFolder + "/Effects",
            ArtFolder = "Assets/_Primordia/Art/Effects/Lineages",
            ControllerPerClip = true,
            Timings = new[] { ("FungusMat", 1f / 0.22f, true), ("StingingTrail", 1f / 0.12f, true) },
        };

        [MenuItem("Primordia/Art/Importar beneficios de linajes")]
        static void ImportLineageBenefits()
        {
            var art = Import(LineageEffectsArt);
            if (art != null)
            {
                // En el suelo: las matas bajo el micelio (orden 4) y la estela con él, todo bajo los personajes.
                LineagePatch("Effect_FungusMat", art, "FungusMat", sortingOrder: 3);
                LineagePatch("Effect_FungusMat_Large", art, "FungusMat_Large", sortingOrder: 3);
                LineagePatch("Effect_StingingTrail", art, "StingingTrail", sortingOrder: 4);
                AssetDatabase.SaveAssets();
            }

            var heart = ImportSprite($"{LineagesSourceFolder}/HUD/UI_Heart_Barrier.png", $"{UIArtFolder}/UI_Heart_Barrier.png", Vector4.zero);
            if (heart != null)
            {
                EditPrefab(UISetup.GameUIPath, root =>
                {
                    foreach (var display in root.GetComponentsInChildren<HeartsDisplay>(true))
                        display.SetBarrierSprite(heart);
                });
            }

            LineageSetup.CreateLineages();
            Debug.Log("ArtSetup: arte de los beneficios de linajes importado.");
        }

        static void LineagePatch(string prefabName, ImportedArt art, string clip, int sortingOrder)
        {
            if (CreateEffectPrefab(prefabName, art, clip, sortingOrder) != null)
                EditPrefab($"{EffectPrefabsFolder}/{prefabName}.prefab", root => GetOrAdd<MyceliumPatch>(root));
        }
    }
}

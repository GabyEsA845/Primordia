using System.IO;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Ajustes de las Bocas (docs/multimedia-temp/mouth-tweaks/): Aguijón más pequeño con los proyectiles saliendo de
    /// su punta, y Esporas como gorrito de Amanita en la cabeza (con su espora crema y sus iconos). Al final
    /// rehace los niveles de las mutaciones, que llevan la bocanada que flota de las Esporas y el avance del mordisco.
    /// </summary>
    public static partial class ArtSetup
    {
        const string MouthTweaksSourceFolder = "docs/multimedia-temp/mouth-tweaks";

        /// <summary>Del punto de unión del Aguijón a su punta, en px (lo dice generar_bocas.py).</summary>
        const float StingerTipPixels = 12.8f;

        /// <summary>Cuánto se adelanta el cuerpo al soltar las esporas, en px.</summary>
        const float SporesLeanPixels = 2.5f;

        /// <summary>
        /// Altura de la cabeza de cada Piel sobre la de la célula, en px: ahí va el gorrito. El Caparazón y la Piel
        /// urticante son más altos; el Camuflaje, como la célula.
        /// </summary>
        static readonly (string skin, float pixels)[] SkinHeadHeights =
        {
            ("Carapace", 2f), ("StingingSkin", 2f), ("Camouflage", 0f),
        };

        [MenuItem("Primordia/Art/Importar ajustes de las Bocas")]
        static void ImportMouthTweaks()
        {
            if (!Directory.Exists(MouthTweaksSourceFolder))
            {
                Debug.LogWarning($"ArtSetup: falta {MouthTweaksSourceFolder}; ejecuta antes generar_bocas.py.");
                return;
            }

            ImportSmallStinger();
            ImportSporesCap();
            ImportSporeProjectile();
            AddCapToPlayer();
            AssetDatabase.SaveAssets();

            MutationLevelsSetup.ConfigureLevels();
            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: ajustes de las Bocas importados (Aguijón, gorrito de las Esporas y espora).");
        }

        /// <summary>El Aguijón pequeño está en la carpeta de evoluciones (generar_bocas.py lo copia allí).</summary>
        static void ImportSmallStinger()
        {
            var piece = MutationPiece(MutationSlot.Mouth, "Stinger");
            piece.SourceFolder = $"{EvolutionsSourceFolder}/{MutationSlot.Mouth}";
            var art = Import(piece);
            var mutation = LoadMutation("Stinger");
            if (art == null || mutation == null)
                return;

            mutation.SetLook(art.Controller);
            mutation.SetMouthLook(onHead: false, StingerTipPixels / PixelsPerUnit, newAttackLean: 0f);
            EditorUtility.SetDirty(mutation);
        }

        /// <summary>Gorrito de las Esporas: estados como las Extremidades, para ir al mismo cuadro que el cuerpo.</summary>
        static void ImportSporesCap()
        {
            var art = Import(new Character
            {
                Name = "Cap_Spores",
                SourceFolder = $"{MouthTweaksSourceFolder}/Cap",
                ArtFolder = $"{MutationsArtFolder}/Spores",
                DefaultState = "Idle_down",
                Timings = LimbsTimings,
            });
            var mutation = LoadMutation("Spores");
            if (art == null || mutation == null)
                return;

            mutation.SetLook(art.Controller);
            mutation.SetMouthLook(onHead: true, newMuzzleDistance: 0f, SporesLeanPixels / PixelsPerUnit);

            var small = ImportSprite($"{MouthTweaksSourceFolder}/Icons/Icon_Spores_16.png", $"{UIArtFolder}/Icon_Spores_16.png", Vector4.zero);
            var large = ImportSprite($"{MouthTweaksSourceFolder}/Icons/Icon_Spores_32.png", $"{UIArtFolder}/Icon_Spores_32.png", Vector4.zero);
            if (small != null)
                mutation.SetIcons(small, large);
            EditorUtility.SetDirty(mutation);

            foreach (var (skin, pixels) in SkinHeadHeights)
            {
                var skinMutation = LoadMutation(skin);
                if (skinMutation == null)
                    continue;
                skinMutation.SetHeadHeight(pixels / PixelsPerUnit);
                EditorUtility.SetDirty(skinMutation);
            }
        }

        /// <summary>Espora crema con borde rojizo, en el prefab de siempre.</summary>
        static void ImportSporeProjectile()
        {
            var art = Import(ProjectilesWithQuill($"{MouthTweaksSourceFolder}/Projectiles"));
            if (art != null && art.Controllers.TryGetValue("Spore", out var controller))
                ApplyToProjectilePrefab("Projectile_Spore", controller, art.FirstSprites["Spore"]);
        }

        /// <summary>Pieza del gorrito en el jugador (vacía hasta que se equipan las Esporas), delante de todo.</summary>
        static void AddCapToPlayer()
        {
            EditPrefab(PlayerSetup.PlayerPath, root =>
            {
                if (!root.TryGetComponent<MutationLook>(out var look))
                {
                    Debug.LogWarning("ArtSetup: el jugador no tiene MutationLook; importa antes las piezas de mutación.");
                    return;
                }

                var cap = MutationPart(root.transform, "Cap", sortingOrder: 13, Vector3.zero);
                SetFields(look, ("cap", cap));
            });
        }

        static MutationData LoadMutation(string name)
        {
            var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationDataFolder}/Mutation_{name}.asset");
            if (mutation == null)
                Debug.LogWarning($"ArtSetup: no existe Mutation_{name}; créalo con Primordia > Mutations.");
            return mutation;
        }
    }
}

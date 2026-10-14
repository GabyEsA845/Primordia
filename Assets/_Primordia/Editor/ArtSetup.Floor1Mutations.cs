using System.IO;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de las mutaciones nuevas del piso 1 (docs/multimedia-temp/floor1-mutations/): piezas de Camuflaje (Piel),
    /// Hifas (Extremidades) y Vacuola digestiva (Órgano), sus iconos, la mancha de micelio, el destello al ocultarse
    /// y las marcas de sospecha y susto de los enemigos. Crea antes los datos con
    /// <see cref="Floor1MutationsSetup.CreateMutations"/> si faltan.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor1SourceFolder = "docs/multimedia-temp/floor1-mutations";

        // Piel: colisión 0,45 y Boca a 7 px, como la piel urticante (la célula moteada mide ~16×15).
        static readonly (MutationSlot slot, string name, float bodyRadius, float mouthPixels)[] Floor1Pieces =
        {
            (MutationSlot.Skin, "Camouflage", 0.45f, 7f),
            (MutationSlot.Limbs, "Hyphae", 0f, 0f),
            (MutationSlot.Organ, "DigestiveVacuole", 0f, 0f),
        };

        static readonly Character Floor1EffectsArt = new()
        {
            Name = "",
            SourceFolder = Floor1SourceFolder + "/Effects",
            ArtFolder = "Assets/_Primordia/Art/Effects/Floor1",
            ControllerPerClip = true,
            Timings = new[] { ("Mycelium", 1f / 0.16f, true), ("Camouflage_Hide", 1f / 0.07f, false) },
        };

        static readonly Character Floor1StatusArt = new()
        {
            Name = "",
            SourceFolder = Floor1SourceFolder + "/Status",
            ArtFolder = "Assets/_Primordia/Art/Effects/Status/Floor1",
            ControllerPerClip = true,
            Timings = new[] { ("Suspect", 1f / 0.15f, true), ("Scared", 1f / 0.11f, true) },
        };

        [MenuItem("Primordia/Art/Importar mutaciones nuevas del piso 1")]
        static void ImportFloor1Mutations()
        {
            if (AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationDataFolder}/Mutation_Hyphae.asset") == null)
                Floor1MutationsSetup.CreateMutations();

            foreach (var (slot, name, bodyRadius, mouthPixels) in Floor1Pieces)
            {
                var piece = MutationPiece(slot, name);
                piece.SourceFolder = $"{Floor1SourceFolder}/{slot}";
                var art = Import(piece);

                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationDataFolder}/Mutation_{name}.asset");
                if (mutation == null)
                    continue;

                if (art != null)
                    mutation.SetLook(art.Controller, bodyRadius, mouthPixels / PixelsPerUnit);

                var small = ImportSprite($"{Floor1SourceFolder}/Icons/Icon_{name}_16.png", $"{UIArtFolder}/Icon_{name}_16.png", Vector4.zero);
                var large = ImportSprite($"{Floor1SourceFolder}/Icons/Icon_{name}_32.png", $"{UIArtFolder}/Icon_{name}_32.png", Vector4.zero);
                if (small != null)
                    mutation.SetIcons(small, large);
                EditorUtility.SetDirty(mutation);
            }

            var effects = Import(Floor1EffectsArt);
            var status = Import(Floor1StatusArt);
            Directory.CreateDirectory(EffectPrefabsFolder);

            GameObject Effect(string prefabName) => AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectPrefabsFolder}/{prefabName}.prefab");

            MyceliumPatch patch = null;
            GameObject hide = null;
            if (effects != null)
            {
                // El micelio, en el suelo bajo los personajes (como el pisotón); el destello, sobre el jugador.
                var mycelium = CreateEffectPrefab("Effect_Mycelium", effects, "Mycelium", sortingOrder: 4);
                if (mycelium != null)
                    patch = EditPrefab($"{EffectPrefabsFolder}/Effect_Mycelium.prefab", root => GetOrAdd<MyceliumPatch>(root))
                        .GetComponent<MyceliumPatch>();
                hide = CreateEffectPrefab("Effect_Camouflage_Hide", effects, "Camouflage_Hide", sortingOrder: 13);
            }

            GameObject suspect = null, scared = null;
            if (status != null)
            {
                suspect = CreateEffectPrefab("Effect_Status_Suspect", status, "Suspect", sortingOrder: 22);
                scared = CreateEffectPrefab("Effect_Status_Scared", status, "Scared", sortingOrder: 22);
            }

            var hyphae = AssetDatabase.LoadAssetAtPath<HyphaeMutation>($"{MutationDataFolder}/Mutation_Hyphae.asset");
            if (hyphae != null)
            {
                // La nube de esporas de la red micelial reutiliza el estallido de veneno del Contagio (radio 2).
                hyphae.ConfigureLook(patch, Effect("Effect_PoisonBurst"), Effect("Effect_Poison"));
                EditorUtility.SetDirty(hyphae);
            }

            var camouflage = AssetDatabase.LoadAssetAtPath<CamouflageMutation>($"{MutationDataFolder}/Mutation_Camouflage.asset");
            if (camouflage != null)
            {
                camouflage.ConfigureLook(suspect, scared, hide);
                EditorUtility.SetDirty(camouflage);
            }

            AssetDatabase.SaveAssets();
            ApplyMutationLookToPlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: Hifas, Camuflaje y Vacuola digestiva importadas.");
        }
    }
}

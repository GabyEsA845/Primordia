using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Core;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Linajes del piso 3 (tabla del GDD): crea los 8 <see cref="LineageData"/> con sus mutaciones del piso 3 y su evolución
    /// (docs/multimedia-temp/floor3-lineages), los vincula a las mutaciones y monta el arte del Parásito (marca, salto, gota
    /// de vida y curación, docs/multimedia-temp/floor3-mutations) en los beneficios del jugador. Los toques propios de cada
    /// linaje llegan en el subpaso 4b. Necesita haber creado antes las mutaciones del piso 3 (Bocas y las tres ranuras) y los
    /// linajes del piso 1 (el jugador con <see cref="PlayerLineage"/>). Se puede repetir.
    /// </summary>
    public static class Floor3LineagesSetup
    {
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const string LineagesFolder = MutationsFolder + "/Lineages";
        const string SourceFolder = "docs/multimedia-temp/floor3-mutations";
        const string EffectsArtFolder = "Assets/_Primordia/Art/Effects/Floor3Mutations";

        // Nombre del asset (el de la hoja de evolución), nombre, clase y mutaciones (Boca, Extremidades, Piel, Órgano), sin "Mutation_".
        static readonly (string asset, string name, LineageClass lineageClass, string[] mutations)[] Lineages =
        {
            ("Eryops", "Eryops", LineageClass.Carnivore, new[] { "SpinningTop", "JumpingLegs", "AmphibianSkin", "MethaneSac" }),
            ("Xenacanthus", "Xenacanthus", LineageClass.Carnivore, new[] { "VinegarJet", "JumpingLegs", "CharredScales", "Pheromones" }),
            ("Pulmonoscorpius", "Pulmonoscorpius", LineageClass.Venomous, new[] { "VinegarJet", "SpinnerLegs", "CharredScales", "MethaneSac" }),
            ("Centipede", "Ciempiés", LineageClass.Venomous, new[] { "PoisonSpit", "JumpingLegs", "CharredScales", "Pheromones" }),
            ("TinderFungus", "Yesquero", LineageClass.Fungal, new[] { "PoisonSpit", "JumpingLegs", "CharredScales", "MethaneSac" }),
            ("Cordyceps", "Cordyceps", LineageClass.Fungal, new[] { "VinegarJet", "SpinnerLegs", "AmphibianSkin", "Pheromones" }),
            ("Lamprey", "Lamprea", LineageClass.Parasite, new[] { "SpinningTop", "SpinnerLegs", "AmphibianSkin", "Pheromones" }),
            ("Leech", "Sanguijuela", LineageClass.Parasite, new[] { "PoisonSpit", "SpinnerLegs", "AmphibianSkin", "MethaneSac" }),
        };

        [MenuItem("Primordia/Mutations/Crear linajes del piso 3")]
        static void CreateAll()
        {
            Directory.CreateDirectory(LineagesFolder);
            var evolutions = ArtSetup.ImportFloor3Evolutions(Lineages.Select(l => l.asset));
            var missing = new HashSet<string>();
            int count = 0;

            foreach (var (asset, name, lineageClass, mutationNames) in Lineages)
            {
                var mutations = mutationNames.Select(m =>
                {
                    var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationsFolder}/Mutation_{m}.asset");
                    if (mutation == null)
                        missing.Add(m);
                    return mutation;
                }).ToArray();

                string path = $"{LineagesFolder}/Lineage_{asset}.asset";
                var lineage = AssetDatabase.LoadAssetAtPath<LineageData>(path);
                if (lineage == null)
                {
                    lineage = ScriptableObject.CreateInstance<LineageData>();
                    AssetDatabase.CreateAsset(lineage, path);
                }
                lineage.Configure(name, lineageClass, mutations, "");
                // Sin toque hasta el subpaso 4b. Como en el piso 2, no cambian piezas del cuerpo: solo la animación de la evolución.
                lineage.ConfigureLook(null, 0f, null, evolutions.TryGetValue(asset, out var evolution) ? evolution : null);
                EditorUtility.SetDirty(lineage);
                count++;
            }

            AssetDatabase.SaveAssets();
            LineageSetup.LinkMutations();
            SetUpParasite();

            if (missing.Count > 0)
                Debug.LogWarning($"Floor3LineagesSetup: faltan las mutaciones {string.Join(", ", missing)}; créalas con Primordia > Mutations / Floor 3.");
            Debug.Log($"Floor3LineagesSetup: {count} linajes del piso 3 en {LineagesFolder}, con su evolución y el arte del Parásito.");
        }

        // ---------- Parásito ----------

        static void SetUpParasite()
        {
            var marker = Floor2MouthsSetup.CreateLoopEffect("Effect_Status_Parasite", Sheet("Parasite", "Parasite_Attached", 12), 0.12f, sortingOrder: 22);
            var leap = Floor2MouthsSetup.CreateFrameEffect("Effect_Parasite_Leap", Sheet("Parasite", "Parasite_Leap", 16), 0.07f, sortingOrder: 23);
            var drop = Floor2MouthsSetup.CreateLoopEffect("Effect_Parasite_Drain", Sheet("Effects", "Fx_Parasite_Drain", 8), 0.1f, sortingOrder: 24);
            var heal = Floor2MouthsSetup.CreateFrameEffect("Effect_Parasite_Heal", Sheet("Effects", "Fx_Parasite_Heal", 32), 0.07f, sortingOrder: 21);
            AddArcFlight(leap);
            AddArcFlight(drop);
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerLineage>(out var lineage))
                {
                    Debug.LogWarning("Floor3LineagesSetup: el jugador no tiene PlayerLineage; créalo con Primordia > Mutations > Crear linajes.");
                    return;
                }
                var benefits = lineage.Benefits ?? new LineageBenefitSettings();
                benefits.parasiteMarker = marker;
                benefits.parasiteJumpEffect = leap;
                benefits.parasiteDrainDrop = drop;
                benefits.parasiteHealEffect = heal;
                lineage.ConfigureBenefits(benefits);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Al prefab del efecto le pone el vuelo en arco (el código lo lanza al crearlo).</summary>
        static void AddArcFlight(GameObject prefab)
        {
            if (prefab == null)
                return;
            string path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!contents.TryGetComponent<ArcFlight>(out _))
                    contents.AddComponent<ArcFlight>();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Sprite[] Sheet(string folder, string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{SourceFolder}/{folder}/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);
    }
}

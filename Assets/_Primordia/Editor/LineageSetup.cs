using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Linajes del piso 1 (tabla del GDD): crea un <see cref="LineageData"/> por linaje con sus 4 mutaciones, apunta
    /// en cada mutación los linajes a los que pertenece, pinta cada mutación del color de su ranura, añade
    /// <see cref="PlayerLineage"/> al jugador y el rótulo del linaje al HUD. Se puede volver a ejecutar: rehace los
    /// datos de los linajes a partir de la tabla. Las mutaciones que aún no existen (Hifas, Camuflaje y Vacuola
    /// digestiva) quedan vacías y esos linajes no se pueden completar hasta crearlas.
    /// </summary>
    public static class LineageSetup
    {
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations";
        const string LineagesFolder = MutationsFolder + "/Lineages";
        const string EffectsFolder = "Assets/_Primordia/Prefabs/Effects";

        /// <summary>Colores por ranura, en el orden de <see cref="MutationSlot"/> (los de las burbujas).</summary>
        internal static readonly Color[] SlotColors =
        {
            new(0.95f, 0.5f, 0.35f),  // Boca: naranja
            new(0.66f, 0.35f, 0.82f), // Extremidades: morado
            new(0.55f, 0.7f, 0.9f),   // Piel: azul
            new(0.55f, 0.9f, 0.35f),  // Órgano: lima
        };

        // Nombre del asset, nombre, clase y mutaciones (Boca, Extremidades, Piel, Órgano), sin "Mutation_".
        static readonly (string asset, string name, LineageClass lineageClass, string[] mutations)[] Lineages =
        {
            ("Shark", "Tiburón", LineageClass.Carnivore, new[] { "Mandibles", "Fins", "Carapace", "ShockGland" }),
            ("Squid", "Calamar", LineageClass.Carnivore, new[] { "Mandibles", "Fins", "Camouflage", "InkSac" }),
            ("Turtle", "Tortuga", LineageClass.Herbivore, new[] { "Mandibles", "JointedLegs", "Carapace", "DigestiveVacuole" }),
            ("Jellyfish", "Medusa", LineageClass.Venomous, new[] { "Stinger", "Fins", "StingingSkin", "ShockGland" }),
            ("Scorpion", "Escorpión", LineageClass.Venomous, new[] { "Stinger", "JointedLegs", "Camouflage", "DigestiveVacuole" }),
            ("Mold", "Moho", LineageClass.Fungal, new[] { "Spores", "Hyphae", "StingingSkin", "DigestiveVacuole" }),
            ("Puffball", "Cuesco de lobo", LineageClass.Fungal, new[] { "Spores", "Hyphae", "Camouflage", "InkSac" }),
        };

        [MenuItem("Primordia/Mutations/Crear linajes")]
        static void CreateLineages()
        {
            Directory.CreateDirectory(LineagesFolder);
            var missing = new HashSet<string>();
            var created = new List<LineageData>();

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
                EditorUtility.SetDirty(lineage);
                created.Add(lineage);
            }

            // Cada mutación apunta a sus linajes y toma el color de su ranura.
            foreach (var guid in AssetDatabase.FindAssets("t:MutationData", new[] { MutationsFolder }))
            {
                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid));
                if (mutation == null)
                    continue;

                mutation.SetLineages(created.Where(l => l.MutationFor(mutation.Slot) == mutation).ToArray());
                mutation.SetColor(SlotColors[(int)mutation.Slot]);
                EditorUtility.SetDirty(mutation);
            }

            AssetDatabase.SaveAssets();
            AddLineageToPlayer();
            UISetup.EnsureLineageLabel();
            AssetDatabase.SaveAssets();

            if (missing.Count > 0)
                Debug.LogWarning($"LineageSetup: faltan las mutaciones {string.Join(", ", missing)}; sus linajes no se pueden completar todavía.");
            Debug.Log($"LineageSetup: {created.Count} linajes creados en {LineagesFolder}.");
        }

        static void AddLineageToPlayer()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerMutations>(out _))
                {
                    Debug.LogWarning("LineageSetup: el jugador no tiene PlayerMutations; créalo antes con Primordia > Mutations.");
                    return;
                }

                var lineage = root.TryGetComponent<PlayerLineage>(out var existing) ? existing : root.AddComponent<PlayerLineage>();
                lineage.Configure(AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectsFolder}/Effect_LevelUp_Gold.prefab"));
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

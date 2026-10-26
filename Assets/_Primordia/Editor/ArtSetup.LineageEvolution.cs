using System.Collections.Generic;
using System.Linq;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Evolución de los linajes (docs/multimedia-temp/lineage-evolution/): las piezas con las que cada linaje sustituye
    /// a las de sus mutaciones (Extremidades, Bocas que giran y el gorrito del Cuesco de lobo), la animación de la
    /// evolución de cada uno y el pof al perderlo. Pone en cada <see cref="LineageData"/> su aspecto y al jugador
    /// <see cref="LineageEvolution"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        const string LineageEvolutionSourceFolder = "docs/multimedia-temp/lineage-evolution";
        const string LineageArtFolder = "Assets/_Primordia/Art/Lineages";
        const string LineageDataFolder = MutationDataFolder + "/Lineages";

        /// <summary>De la unión de la cola del Escorpión a su punta, en px (lo dice el generador).</summary>
        const float ScorpionTailTipPixels = 15.6f;

        // Milisegundos de cada cuadro de la evolución (los del generador): unos 2 s, el animal se queda más.
        static readonly float[] EvolutionDurations = { 0.1f, 0.11f, 0.12f, 0.12f, 0.13f, 0.15f, 0.26f, 0.26f, 0.26f, 0.15f, 0.12f, 0.2f };

        // Qué sustituye cada linaje: Extremidades, Boca (giratoria o gorrito) y su punta de disparo.
        static readonly (string lineage, bool limbs, string mouth, float tipPixels)[] LineagePieces =
        {
            ("Shark", true, null, 0f),
            ("Squid", true, null, 0f),
            ("Turtle", true, "Mouth", 0f),
            ("Jellyfish", true, null, 0f),
            ("Scorpion", true, "Mouth", ScorpionTailTipPixels),
            ("Mold", true, null, 0f),
            ("Puffball", false, "Cap", 0f),
        };

        /// <summary>
        /// Evoluciones de los linajes del piso 2 (docs/multimedia-temp/floor2-lineages/Evolution, 64×64, mismas duraciones
        /// que el piso 1): un prefab <c>Effect_Evolution_&lt;Linaje&gt;</c> por hoja. Devuelve los prefabs por nombre del linaje.
        /// </summary>
        internal static Dictionary<string, GameObject> ImportFloor2Evolutions(IEnumerable<string> lineageNames)
        {
            var names = lineageNames.ToArray();
            var art = Import(new Character
            {
                Name = "",
                SourceFolder = "docs/multimedia-temp/floor2-lineages/Evolution",
                ArtFolder = "Assets/_Primordia/Art/Effects/Evolution/Floor2",
                ControllerPerClip = true,
                Timings = new[] { ("Evolution", 10f, false) },
                FrameDurations = names.ToDictionary(n => $"Evolution_{n}", _ => EvolutionDurations),
            });
            var prefabs = new Dictionary<string, GameObject>();
            if (art == null)
                return prefabs;
            foreach (var name in names)
                prefabs[name] = CreateEffectPrefab($"Effect_Evolution_{name}", art, $"Evolution_{name}", sortingOrder: 15);
            return prefabs;
        }

        [MenuItem("Primordia/Art/Importar evolución de linajes")]
        static void ImportLineageEvolution()
        {
            var evolutions = Import(new Character
            {
                Name = "",
                SourceFolder = LineageEvolutionSourceFolder + "/Evolution",
                ArtFolder = "Assets/_Primordia/Art/Effects/Evolution",
                ControllerPerClip = true,
                Timings = new[] { ("Evolution", 10f, false) },
                FrameDurations = LineagePieces.ToDictionary(p => $"Evolution_{p.lineage}", _ => EvolutionDurations),
            });
            var lost = Import(new Character
            {
                Name = "",
                SourceFolder = LineageEvolutionSourceFolder + "/Effects",
                ArtFolder = "Assets/_Primordia/Art/Effects/LineageLost",
                ControllerPerClip = true,
                Timings = new[] { ("LineageLost", 1f / 0.07f, false) },
            });

            foreach (var (name, hasLimbs, mouth, tipPixels) in LineagePieces)
            {
                var lineage = AssetDatabase.LoadAssetAtPath<LineageData>($"{LineageDataFolder}/Lineage_{name}.asset");
                if (lineage == null)
                {
                    Debug.LogWarning($"ArtSetup: no existe Lineage_{name}; créalo con Primordia > Mutations > Crear linajes.");
                    continue;
                }

                var limbs = hasLimbs ? ImportLineagePiece($"Limbs_{name}", "Limbs", name, "Idle_down", LimbsTimings, new Vector2(0.5f, 0.5f)) : null;
                RuntimeAnimatorController mouthLook = null;
                if (mouth == "Mouth")
                    mouthLook = ImportLineagePiece($"Mouth_{name}", "Mouth", name, "Idle", MouthTimings, new Vector2(0f, 0.5f));
                else if (mouth == "Cap")
                    mouthLook = ImportLineagePiece($"Cap_{name}", "Cap", name, "Idle_down", LimbsTimings, new Vector2(0.5f, 0.5f));

                // Por encima de las piezas del jugador (orden 13) y de los efectos sobre él.
                GameObject evolution = null;
                if (evolutions != null)
                    evolution = CreateEffectPrefab($"Effect_Evolution_{name}", evolutions, $"Evolution_{name}", sortingOrder: 15);

                lineage.ConfigureLook(mouthLook, tipPixels / PixelsPerUnit, limbs, evolution);
                EditorUtility.SetDirty(lineage);
            }

            GameObject lostEffect = lost != null ? CreateEffectPrefab("Effect_LineageLost", lost, "LineageLost", sortingOrder: 14) : null;
            AssetDatabase.SaveAssets();

            EditPrefab(PlayerSetup.PlayerPath, root =>
            {
                if (!root.TryGetComponent<PlayerLineage>(out var playerLineage))
                {
                    Debug.LogWarning("ArtSetup: el jugador no tiene PlayerLineage; créalo con Primordia > Mutations > Crear linajes.");
                    return;
                }

                GetOrAdd<LineageEvolution>(root).Configure(lostEffect);
                // La evolución ya celebra el linaje: sin el destello dorado de antes.
                playerLineage.Configure(null);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: evolución de linajes importada.");
        }

        /// <summary>Una pieza de linaje con su controlador, en Art/Lineages/&lt;Linaje&gt;/.</summary>
        static RuntimeAnimatorController ImportLineagePiece(string sheetName, string folder, string lineage, string defaultState,
            (string, float, bool)[] timings, Vector2 pivot)
        {
            var art = Import(new Character
            {
                Name = sheetName,
                SourceFolder = $"{LineageEvolutionSourceFolder}/{folder}",
                ArtFolder = $"{LineageArtFolder}/{lineage}",
                DefaultState = defaultState,
                Timings = timings,
                Pivot = pivot,
            });
            return art != null ? art.Controller : null;
        }
    }
}

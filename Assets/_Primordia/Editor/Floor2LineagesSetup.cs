using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Linajes del piso 2 (tabla del GDD): crea los 8 <see cref="LineageData"/> con sus mutaciones del piso 2, su toque y
    /// su evolución (docs/multimedia-temp/floor2-lineages), los vincula a las mutaciones y monta la Carroña del
    /// Carroñero: restos, restos dobles, efecto al comerlos y marcas de colmillos (docs/multimedia-temp/floor2-mutations),
    /// que pone en los beneficios del jugador. Las clases remodeladas del piso 2 salen del piso de las mutaciones del
    /// linaje (<see cref="LineageData.Floor"/>). Necesita haber creado antes las mutaciones del piso 2 y los linajes del
    /// piso 1 (el jugador con <see cref="PlayerLineage"/>). Se puede repetir.
    /// </summary>
    public static class Floor2LineagesSetup
    {
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor2";
        const string LineagesFolder = MutationsFolder + "/Lineages";
        const string SourceFolder = "docs/multimedia-temp/floor2-mutations/Effects";
        const string EffectsArtFolder = "Assets/_Primordia/Art/Effects/Floor2";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Mutations";

        // Restos sobre el suelo, por debajo de enemigos y jugador.
        const int RemainsOrder = 4;

        // Nombre del asset, nombre, clase y mutaciones (Boca, Extremidades, Piel, Órgano), sin "Mutation_".
        static readonly (string asset, string name, LineageClass lineageClass, string[] mutations)[] Lineages =
        {
            ("Placoderm", "Placodermo", LineageClass.Carnivore, new[] { "JawPlates", "TailPropulsor", "TrilobitePlates", "CompoundEyes" }),
            ("Opabinia", "Opabinia", LineageClass.Carnivore, new[] { "JawPlates", "HallucigeniaSpines", "WiwaxiaSclerites", "CompoundEyes" }),
            ("Burgessomedusa", "Burgessomedusa", LineageClass.Venomous, new[] { "Cnidocytes", "TailPropulsor", "WiwaxiaSclerites", "BroodSac" }),
            ("Anemone", "Anémona", LineageClass.Venomous, new[] { "Cnidocytes", "HallucigeniaSpines", "TrilobitePlates", "BroodSac" }),
            ("Crinoid", "Lirio de mar", LineageClass.Herbivore, new[] { "ArmCrown", "TailPropulsor", "TrilobitePlates", "BroodSac" }),
            ("Wiwaxia", "Wiwaxia", LineageClass.Herbivore, new[] { "JawPlates", "TailPropulsor", "WiwaxiaSclerites", "BroodSac" }),
            ("Trilobite", "Trilobite", LineageClass.Scavenger, new[] { "JawPlates", "HallucigeniaSpines", "TrilobitePlates", "CompoundEyes" }),
            ("Hallucigenia", "Hallucigenia", LineageClass.Scavenger, new[] { "ArmCrown", "HallucigeniaSpines", "WiwaxiaSclerites", "CompoundEyes" }),
        };

        // Toque propio de cada linaje: nombre y lo que hace (corto: se lee en la pausa).
        static readonly Dictionary<string, (LineageTouch touch, string name, string description)> Touches = new()
        {
            ["Placoderm"] = (LineageTouch.Feast, "Festín", "Rematar con un mordisco cura medio corazón (como mucho cada 6 s)."),
            ["Opabinia"] = (LineageTouch.FiveEyes, "Cinco ojos", "Los Ojos compuestos duran 1 s más; cada remate los alarga 0,5 s."),
            ["Burgessomedusa"] = (LineageTouch.StingingPolyps, "Pólipos urticantes", "Las larvas envenenan al morder."),
            ["Anemone"] = (LineageTouch.StingingBed, "Lecho urticante", "Quien pisa tus púas queda envenenado."),
            ["Crinoid"] = (LineageTouch.NutrientCurrent, "Corriente nutritiva", "El remolino atrae también la biomasa del suelo."),
            ["Wiwaxia"] = (LineageTouch.Harvest, "Cosecha", "Una tanda de escleritos que alcanza a 3 enemigos da medio corazón verde."),
            ["Trilobite"] = (LineageTouch.CarrionArmor, "Coraza de carroña", "Tras comer restos, el siguiente proyectil se deshace en tus placas."),
            ["Hallucigenia"] = (LineageTouch.DoubleServing, "Doble ración", "Las púas que matan dejan restos dobles."),
        };

        [MenuItem("Primordia/Mutations/Crear linajes del piso 2")]
        static void CreateAll()
        {
            Directory.CreateDirectory(LineagesFolder);
            var evolutions = ArtSetup.ImportFloor2Evolutions(Lineages.Select(l => l.asset));
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
                var touch = Touches[asset];
                lineage.ConfigureTouch(touch.touch, touch.name, touch.description);
                // Los linajes del piso 2 no cambian piezas del cuerpo: solo la animación de la evolución.
                lineage.ConfigureLook(null, 0f, null, evolutions.TryGetValue(asset, out var evolution) ? evolution : null);
                EditorUtility.SetDirty(lineage);
                count++;
            }

            AssetDatabase.SaveAssets();
            LineageSetup.LinkMutations();
            SetUpCarrion();

            if (missing.Count > 0)
                Debug.LogWarning($"Floor2LineagesSetup: faltan las mutaciones {string.Join(", ", missing)}; créalas con Primordia > Mutations.");
            Debug.Log($"Floor2LineagesSetup: {count} linajes del piso 2 en {LineagesFolder}, con su evolución y la Carroña del Carroñero.");
        }

        // ---------- Carroña ----------

        static void SetUpCarrion()
        {
            var remains = CreateRemains("CarrionRemains", "Fx_Carrion_Remains", 1);
            var doubleRemains = CreateRemains("CarrionRemains_Double", "Fx_Carrion_RemainsDouble", 2);
            var eat = Floor2MouthsSetup.CreateFrameEffect("Effect_Carrion_Eat", Sheet("Fx_Carrion_Eat", 24), 0.06f, sortingOrder: 14);
            var markers = new GameObject[3];
            for (int i = 0; i < markers.Length; i++)
                markers[i] = Floor2MouthsSetup.CreateLoopEffect($"Effect_Status_Carrion_{i + 1}", Sheet($"Status_Carrion_{i + 1}", 12), 0.25f, sortingOrder: 22);
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerLineage>(out var lineage))
                {
                    Debug.LogWarning("Floor2LineagesSetup: el jugador no tiene PlayerLineage; créalo con Primordia > Mutations > Crear linajes.");
                    return;
                }
                var benefits = lineage.Benefits ?? new LineageBenefitSettings();
                benefits.remainsPrefab = remains;
                benefits.doubleRemainsPrefab = doubleRemains;
                benefits.eatEffect = eat;
                benefits.carrionMarkers = markers;
                lineage.ConfigureBenefits(benefits);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static CarrionRemains CreateRemains(string prefabName, string sheet, int value)
        {
            var frames = Sheet(sheet, 16);
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = RemainsOrder;
                if (frames.Length > 1)
                    root.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.15f, 0f);
                var remains = root.AddComponent<CarrionRemains>();
                remains.Configure(value, value > 1 ? 0.65f : 0.55f);
                Directory.CreateDirectory(PrefabsFolder);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{prefabName}.prefab").GetComponent<CarrionRemains>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Sprite[] Sheet(string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{SourceFolder}/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);
    }
}

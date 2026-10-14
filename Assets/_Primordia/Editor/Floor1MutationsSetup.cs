using System.Linq;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Las 3 mutaciones nuevas del piso 1 (GDD): Hifas, Camuflaje y Vacuola digestiva. Crea sus datos con los textos
    /// de cada nivel (sin pisar los que ya existen, salvo los textos), las añade a la reserva de mutaciones y rehace
    /// los linajes para que las usen. El aspecto (piezas, iconos, mancha de micelio, marcas) lo pone
    /// Primordia > Art > Importar mutaciones nuevas del piso 1.
    /// </summary>
    public static class Floor1MutationsSetup
    {
        const string DataFolder = "Assets/_Primordia/Data/Mutations";
        const string PoolPath = DataFolder + "/MutationPool.asset";
        const string EffectsFolder = "Assets/_Primordia/Prefabs/Effects";

        [MenuItem("Primordia/Mutations/Crear mutaciones nuevas del piso 1")]
        internal static void CreateMutations()
        {
            var hyphae = Create<HyphaeMutation>("Mutation_Hyphae", "Hifas",
                "Al moverte dejas un rastro de micelio que dura 3 s: los enemigos encima van un 40 % más lentos y reciben 1 de daño por segundo.",
                MutationSlot.Limbs);
            hyphae.SetLevelDescriptions("El micelio dura 4,5 s y quita 2 de vida por segundo.",
                "Red micelial: los enemigos que mueren sobre el micelio sueltan una nube de esporas que envenena a los cercanos.");

            var camouflage = Create<CamouflageMutation>("Mutation_Camouflage", "Camuflaje",
                "Tras 3 s sin atacar ni recibir golpes te vuelves casi invisible: los enemigos te buscan sin verte y tu siguiente ataque hace el doble de daño. Si te acercas demasiado, acabarán descubriéndote.",
                MutationSlot.Skin);
            camouflage.SetLevelDescriptions("Te ocultas a los 2 s y el ataque desde el camuflaje hace el triple de daño.",
                "Pánico: si matas a un enemigo desde el camuflaje, los demás de la sala se asustan 2 s (no atacan y se alejan despacio).");

            var vacuole = Create<DigestiveVacuoleMutation>("Mutation_DigestiveVacuole", "Vacuola digestiva",
                "Clic derecho: digiere y cura medio corazón. Se recarga matando enemigos: cada 4 muertes.",
                MutationSlot.Organ);
            vacuole.SetLevelDescriptions("Se recarga cada 3 muertes.",
                "Digestión completa: cura un corazón entero (sigue recargándose cada 3 muertes).");
            vacuole.ConfigureAbility(newCooldown: 0.5f, AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectsFolder}/Effect_Heal.prefab"));

            foreach (var mutation in new MutationData[] { hyphae, camouflage, vacuole })
                EditorUtility.SetDirty(mutation);

            var pool = AssetDatabase.LoadAssetAtPath<MutationPool>(PoolPath);
            if (pool != null)
            {
                var all = pool.Mutations.Where(m => m != null).ToList();
                foreach (var mutation in new MutationData[] { hyphae, camouflage, vacuole })
                {
                    if (!all.Contains(mutation))
                        all.Add(mutation);
                }
                pool.Configure(all);
                EditorUtility.SetDirty(pool);
            }
            else
            {
                Debug.LogWarning("Floor1MutationsSetup: no existe la reserva de mutaciones; créala con Primordia > Mutations > Crear mutaciones y escena de prueba.");
            }

            AssetDatabase.SaveAssets();
            LineageSetup.CreateLineages();
            Debug.Log("Floor1MutationsSetup: Hifas, Camuflaje y Vacuola digestiva creadas, en la reserva y en sus linajes.");
        }

        /// <summary>Crea la mutación si no existe; si existe, solo actualiza nombre, descripción, ranura y color.</summary>
        static T Create<T>(string assetName, string displayName, string description, MutationSlot slot) where T : MutationData
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var mutation = AssetDatabase.LoadAssetAtPath<T>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(mutation, path);
            }
            mutation.ConfigureBase(displayName, description, slot, LineageSetup.SlotColors[(int)slot]);
            return mutation;
        }
    }
}

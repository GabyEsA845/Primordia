using System.IO;
using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Las 3 mutaciones especiales de las almejas (GDD, "Mutaciones especiales de las almejas"): Flagelos de Leanchoilia,
    /// Cutícula de Marrella y Yema clonal. Crea sus datos (piso 2, especiales, fuera de la reserva) con los textos de cada
    /// nivel, las pone en las almejas grandes del piso 2, importa su arte (docs/multimedia-temp/clam-mutations: iconos,
    /// insignia dorada y corazones de Inestable) y añade al HUD el cartel para elegir linaje cuando el comodín completa dos.
    /// No pisa las mutaciones que ya existen, salvo sus textos.
    /// </summary>
    public static class SpecialMutationsSetup
    {
        const string DataFolder = "Assets/_Primordia/Data/Mutations/Floor2";
        const string SourceFolder = "docs/multimedia-temp/clam-mutations";
        const string UIArtFolder = "Assets/_Primordia/Art/UI";
        const string IconsArtFolder = "Assets/_Primordia/Art/UI/Special";

        internal static readonly string[] AssetNames = { "Mutation_LeanchoiliaFlagella", "Mutation_MarrellaCuticle", "Mutation_ClonalBud" };

        [MenuItem("Primordia/Mutations/Crear mutaciones especiales (almejas)")]
        internal static void CreateAll()
        {
            var specials = CreateMutations();
            AssetDatabase.SaveAssets();

            ImportIcons(specials);
            var art = ImportUIArt();
            ConfigureGameUI(art);
            PutInClams(specials);

            AssetDatabase.SaveAssets();
            // Para probarlas en Test_Floor2 hace falta el HUD (las escenas viejas no lo traían).
            Floor2Setup.EnsureTestSceneHUD();
            Debug.Log("SpecialMutationsSetup: Flagelos de Leanchoilia, Cutícula de Marrella y Yema clonal creadas y en las almejas grandes del piso 2. "
                + "Sus efectos llegan en los pasos 2 a 4; de momento cuentan como especiales (comodín de linaje y precio Inestable).");
        }

        /// <summary>Las especiales que existen (para las almejas del piso 2), en orden.</summary>
        internal static MutationData[] LoadAll() =>
            AssetNames.Select(n => AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/{n}.asset")).Where(m => m != null).ToArray();

        static MutationData[] CreateMutations()
        {
            Directory.CreateDirectory(DataFolder);

            var flagella = Create<LeanchoiliaFlagellaMutation>(AssetNames[0], "Flagelos de Leanchoilia",
                "Cuando tu ataque alcanza a un enemigo, unos flagelos aparecen junto a él y lo azotan con el daño de 1 s de tu Boca "
                + "y sus efectos (con la Corona de brazos, a todos los del remolino). Uno cada 3 s.",
                MutationSlot.Limbs);
            flagella.SetLevelDescriptions("Uno cada 2,5 s y un 25 % más de daño.",
                "Latigazo en cadena: el latigazo salta a los 2 enemigos más cercanos con el mismo daño.");

            var cuticle = Create<MarrellaCuticleMutation>(AssetNames[1], "Cutícula de Marrella",
                "Esquiva justa: si un ataque te alcanza mientras esquivas, tu piel lo refleja. El proyectil vuelve a quien lo lanzó "
                + "y le hace 6 de daño; un golpe cuerpo a cuerpo aturde 1 s al atacante y le hace 3.",
                MutationSlot.Skin);
            cuticle.SetLevelDescriptions("8 de daño el proyectil y 4 el destello, que aturde 1,5 s.",
                "Prisma: cada proyectil reflejado se divide en 3 en abanico.");

            var bud = Create<ClonalBudMutation>(AssetNames[2], "Yema clonal",
                "Clic derecho: brota una copia translúcida de ti durante 6 s que ataca sola con tu Boca y la mitad de tu daño. "
                + "No recibe daño, pero los enemigos la atacan. La recarga de 15 s empieza al desaparecer.",
                MutationSlot.Organ);
            bud.SetLevelDescriptions("Dura 7 s y hace el 65 % de tu daño.", "Gemación: brotan dos copias.");
            bud.ConfigureAbility(newCooldown: 15f, null);

            var all = new MutationData[] { flagella, cuticle, bud };
            foreach (var mutation in all)
            {
                mutation.SetFloor(2);
                mutation.SetSpecial(true);
                // Fuera de los linajes: completan cualquiera como comodín.
                mutation.SetLineages(null);
                EditorUtility.SetDirty(mutation);
            }
            return all;
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

        static void ImportIcons(MutationData[] specials)
        {
            string[] names = { "LeanchoiliaFlagella", "MarrellaCuticle", "ClonalBud" };
            for (int i = 0; i < specials.Length; i++)
            {
                var small = ArtSetup.ImportLooseSprite($"{SourceFolder}/Icons/Icon_{names[i]}_16.png", $"{IconsArtFolder}/Icon_{names[i]}_16.png");
                var large = ArtSetup.ImportLooseSprite($"{SourceFolder}/Icons/Icon_{names[i]}_32.png", $"{IconsArtFolder}/Icon_{names[i]}_32.png");
                if (small != null)
                {
                    specials[i].SetIcons(small, large);
                    EditorUtility.SetDirty(specials[i]);
                }
            }
        }

        internal struct UIArt
        {
            public Sprite badge;
            public Sprite[] inlays;
            public Sprite[] cracks;
            public Sprite[] breakFrames;
        }

        static UIArt ImportUIArt()
        {
            Sprite[] Three(string name) => Enumerable.Range(0, 3)
                .Select(i => ArtSetup.ImportLooseSprite($"{SourceFolder}/UI/{name}_{i}.png", $"{IconsArtFolder}/{name}_{i}.png"))
                .Where(s => s != null).ToArray();

            return new UIArt
            {
                badge = ArtSetup.ImportLooseSprite($"{SourceFolder}/UI/UI_Special_Badge.png", $"{IconsArtFolder}/UI_Special_Badge.png"),
                inlays = Three("UI_Heart_UnstableInlay"),
                cracks = Three("UI_Heart_UnstableCracks"),
                breakFrames = ArtSetup.ImportLooseSheet($"{SourceFolder}/UI/Fx_Heart_UnstableBreak.png", $"{IconsArtFolder}/Fx_Heart_UnstableBreak.png", 24),
            };
        }

        /// <summary>Corazones de Inestable, insignia en tarjetas, ranuras y pausa, y el cartel para elegir linaje (se rehace).</summary>
        static void ConfigureGameUI(UIArt art)
        {
            var root = PrefabUtility.LoadPrefabContents(UISetup.GameUIPath);
            try
            {
                foreach (var hearts in root.GetComponentsInChildren<HeartsDisplay>(true))
                    hearts.SetUnstableArt(art.inlays, art.cracks, art.breakFrames);
                foreach (var card in root.GetComponentsInChildren<MutationCard>(true))
                    card.SetSpecialBadge(art.badge);
                foreach (var slots in root.GetComponentsInChildren<MutationSlotsDisplay>(true))
                    slots.SetSpecialBadge(art.badge);
                foreach (var callout in root.GetComponentsInChildren<SlotCallout>(true))
                    callout.SetSpecialBadge(art.badge);

                UISetup.AddLineageChoice(root);
                PrefabUtility.SaveAsPrefabAsset(root, UISetup.GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void PutInClams(MutationData[] specials)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("SpecialMutationsSetup: no existe el piso 2; créalo con Primordia > Rooms > Crear piso 2 y escena de prueba (ya las pondrá en las almejas).");
                return;
            }
            var clams = config.Clams;
            if (clams.smallBiomass == Vector2Int.zero)
                clams = FloorConfig.ClamContents.Default;
            clams.bigMutations = specials;
            config.ConfigureClams(clams);
            EditorUtility.SetDirty(config);
        }
    }
}

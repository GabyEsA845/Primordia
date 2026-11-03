using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Las mutaciones del piso 3 que no son Bocas (GDD, "Mutaciones del piso 3"), por subpasos. Subpaso 1: las Extremidades (Patas
    /// de salto y Patas hilanderas), solo con efectos y sin capa sobre el cuerpo (regla de niveles de las mutaciones). Importa su
    /// arte de docs/multimedia-temp/floor3-mutations y rehace los textos y los efectos cada vez. Cada menú deja al día la reserva del
    /// piso 3 (<see cref="EnsurePool"/>); se prueban con los atajos F1-F5.
    /// </summary>
    public static class Floor3MutationsSetup
    {
        const string SourceFolder = "docs/multimedia-temp/floor3-mutations";
        const string JumpSourceFolder = "docs/multimedia-temp/floor3-jump";
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const string EffectsArtFolder = "Assets/_Primordia/Art/Effects/Floor3Mutations";
        const string UIArtFolder = "Assets/_Primordia/Art/UI/Icons";
        const string PoolPath = "Assets/_Primordia/Data/Mutations/MutationPool_Floor3.asset";

        [MenuItem("Primordia/Mutations/Crear mutaciones del piso 3 (Extremidades)")]
        static void CreateLimbs()
        {
            Directory.CreateDirectory(MutationsFolder);
            Directory.CreateDirectory(EffectsArtFolder);
            CreateJumpingLegs();
            CreateSpinnerLegs();
            AssetDatabase.SaveAssets();
            EnsurePool();
            Debug.Log("Floor3MutationsSetup: Patas de salto y Patas hilanderas creadas (Data/Mutations/Floor3).");
        }

        [MenuItem("Primordia/Mutations/Crear mutaciones del piso 3 (Piel)")]
        static void CreateSkins()
        {
            Directory.CreateDirectory(MutationsFolder);
            Directory.CreateDirectory(EffectsArtFolder);
            CreateAmphibianSkin();
            CreateCharredScales();
            AssetDatabase.SaveAssets();
            EnsurePool();
            Debug.Log("Floor3MutationsSetup: Piel anfibia y Escamas carbonizadas creadas (Data/Mutations/Floor3), con sus capas sobre los 3 cuerpos.");
        }

        [MenuItem("Primordia/Mutations/Crear mutaciones del piso 3 (Órgano)")]
        static void CreateOrgans()
        {
            Directory.CreateDirectory(MutationsFolder);
            Directory.CreateDirectory(EffectsArtFolder);
            CreateMethaneSac();
            CreatePheromones();
            AssetDatabase.SaveAssets();
            EnsurePool();
            Debug.Log("Floor3MutationsSetup: Saco de metano y Feromonas creados (Data/Mutations/Floor3).");
        }

        // ---------- Reserva del piso 3 ----------

        /// <summary>
        /// La reserva del piso 3 (elecciones, tienda y sala de cambio): solo mutaciones del piso 3 que no sean especiales, es decir, las
        /// Extremidades, la Piel y los Órganos y las 3 Bocas. Las Bocas solo se ofrecen como mejora de la que se lleva (la Boca nueva llega
        /// por la sala de paso y la metamorfosis). Las mejoras de lo que se trae de pisos anteriores se ofrecen aunque no estén aquí
        /// (<see cref="MutationPool.PickOffer(int, PlayerMutations)"/>). Se crea si no existe y se rehace con lo que haya en
        /// Data/Mutations/Floor3, así que se puede repetir.
        /// </summary>
        [MenuItem("Primordia/Mutations/Crear reserva del piso 3")]
        internal static MutationPool EnsurePool()
        {
            Directory.CreateDirectory(MutationsFolder);
            var pool = AssetDatabase.LoadAssetAtPath<MutationPool>(PoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<MutationPool>();
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            var mutations = AssetDatabase.FindAssets("t:MutationData", new[] { MutationsFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(m => m != null && m.Floor >= 3 && !m.IsSpecial)
                .OrderBy(m => m.Slot)
                .ThenBy(m => m.name)
                .ToList();
            pool.Configure(mutations, newOffersNewMouths: false);
            EditorUtility.SetDirty(pool);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3MutationsSetup: reserva del piso 3 con {mutations.Count} mutaciones (sin especiales; las Bocas solo como mejora).");
            return pool;
        }

        // ---------- Patas de salto ----------

        static void CreateJumpingLegs()
        {
            var legs = Mutation<JumpingLegsMutation>("Mutation_JumpingLegs", "Patas de salto",
                "La esquiva es un salto alto de 4 u: en el aire eres invulnerable y pasas por encima de enemigos, proyectiles, lodo, charcos y obstáculos bajos. Al caer esparces los charcos de veneno.",
                MutationSlot.Limbs,
                "Salto de 5 u; el aterrizaje hace 3 de daño a los de alrededor.",
                "Salto cargado: salto de 6 u; mantén la esquiva al menos 1 s y un marcador muestra dónde caerás (más lejos cuanto más aguantes, hacia donde apuntes). Al soltar saltas ahí con un impacto de 6 de daño que empuja y aturde 1,2 s.");
            SetIcons(legs, "JumpingLegs");
            legs.ConfigureLook(Effect("Fx_JumpingLegs_Takeoff", 32), Effect("Fx_JumpingLegs_Land", 32), Effect("Fx_JumpingLegs_Quake", 48),
                Effect("Fx_Stunned", 16));
            // Marcador del salto cargado (diana de huellas y esquinas del impacto), de la tanda de arte del salto.
            legs.ConfigureMarker(JumpEffect("Fx_JumpMarker_Spot", 24), JumpEffect("Fx_JumpMarker_Full", 24), JumpEffect("Fx_JumpMarker_Corner", 8));
            EditorUtility.SetDirty(legs);
        }

        // ---------- Patas hilanderas ----------

        static void CreateSpinnerLegs()
        {
            var legs = Mutation<SpinnerLegsMutation>("Mutation_SpinnerLegs", "Patas hilanderas",
                "La esquiva (4 u) tiende un hilo de 7 s entre donde empiezas y donde acabas: frena a los enemigos y daña 3 al que lo toca.",
                MutationSlot.Limbs,
                "Esquiva de 5 u, hilos de 9 s con 4 de daño y hasta 2 a la vez.",
                "Red: los hilos ya no desaparecen; con 3 en la sala sus extremos se unen y una telaraña cubre todo lo de dentro: 4 de daño e inmoviliza 3 s.");
            SetIcons(legs, "SpinnerLegs");
            legs.ConfigureLook(Effect("Fx_SilkThread_Start", 16), Effect("Fx_SilkThread", 8), Effect("Fx_SilkThread_Anchor", 8),
                Effect("Fx_SilkThread_Hit", 12), Effect("Fx_SilkWeb_Appear", 32), Effect("Fx_SilkWeb_Idle", 32), Effect("Fx_SilkWeb_Fade", 32));
            EditorUtility.SetDirty(legs);
        }

        // ---------- Saco de metano ----------

        static void CreateMethaneSac()
        {
            var sac = Mutation<MethaneSacMutation>("Mutation_MethaneSac", "Saco de metano",
                "Clic derecho: sueltas una nube de gas que deriva hacia donde apuntas durante 8 s y explota con cualquier ataque tuyo (6 de daño, no te daña a ti). Recarga de 10 s. Con el saco, ninguna explosión de gas te daña.",
                MutationSlot.Organ,
                "Sueltas dos nubes bien separadas.",
                "Incendio: la explosión deja fuego en el suelo durante 3 s (no te quema a ti).");
            SetIcons(sac, "MethaneSac");
            sac.ConfigureAbility(10f, null);
            sac.ConfigureLook(Effect("Fx_MethaneSac_Release", 48), Effect("Fx_MethaneSac_Cloud", 40), Effect("Fx_MethaneSac_CloudFade", 40),
                Effect("Fx_MethaneSac_Explode", 64));
            EditorUtility.SetDirty(sac);
        }

        // ---------- Feromonas ----------

        static void CreatePheromones()
        {
            var pheromones = Mutation<PheromonesMutation>("Mutation_Pheromones", "Feromonas",
                "Clic derecho: lanzas un velo de feromonas hasta 5 u hacia el cursor; los enemigos en radio 2 se atacan entre sí durante 3 s. Los jefes no se confunden. Recarga de 14 s.",
                MutationSlot.Organ,
                "La confusión dura 4 s.",
                "Frenesí: el daño entre los confundidos es doble.");
            SetIcons(pheromones, "Pheromones");
            pheromones.ConfigureAbility(14f, null);
            pheromones.ConfigureLook(Effect("Fx_Pheromones_Release", 72), Effect("Fx_Pheromones_Aura", 32), Effect("Fx_Pheromones_Mark", 14),
                Effect("Fx_Pheromones_Frenzy", 14));
            EditorUtility.SetDirty(pheromones);
        }

        // ---------- Piel anfibia ----------

        static void CreateAmphibianSkin()
        {
            var skin = Mutation<AmphibianSkinMutation>("Mutation_AmphibianSkin", "Piel anfibia",
                "De pie en agua, lodo o charcos se carga un moco (3 s): cargado, eres inmune al fuego, al gas y al veneno de las plantas hasta que un golpe lo rompe. Funciona sin Escupitajo.",
                MutationSlot.Skin,
                "El moco se carga en 2 s y vas un 20 % más rápido.",
                "Bloqueo: el golpe que rompe el moco no te hace daño; hay que volver a cargarlo.");
            SetIcons(skin, "AmphibianSkin");
            SetLayers(skin, "Skin_AmphibianSkin");
            skin.ConfigureLook(Effect("Fx_AmphibianSkin_Charge", 32), Effect("Fx_AmphibianSkin_Shield", 48),
                Effect("Fx_AmphibianSkin_Absorb", 48), Effect("Fx_AmphibianSkin_Reflect", 48));
            EditorUtility.SetDirty(skin);
        }

        // ---------- Escamas carbonizadas ----------

        static void CreateCharredScales()
        {
            var scales = Mutation<CharredScalesMutation>("Mutation_CharredScales", "Escamas carbonizadas",
                "Inmune al fuego y a las explosiones de gas. Tras pisar fuego, tus ataques queman 4 s.",
                MutationSlot.Skin,
                "También eres inmune al veneno de las plantas, y al recibir un golpe estallas en llamas (4 de daño en radio 1,5; recarga de 5 s).",
                "Llamarada: todos tus ataques prenden fuego siempre, a los enemigos y al escenario (helechos, metano, charcos de veneno) donde impactan y por donde pasan.");
            SetIcons(scales, "CharredScales");
            SetLayers(scales, "Skin_CharredScales");
            scales.ConfigureLook(Effect("Fx_CharredScales_Primed", 32), Effect("Fx_CharredScales_Blaze", 48));
            EditorUtility.SetDirty(scales);
        }

        /// <summary>La capa de la Piel sobre cada cuerpo de criatura del piso 3 (el de cada Boca).</summary>
        static void SetLayers(MutationData mutation, string prefix)
        {
            var layers = new List<MutationData.CreatureLayer>();
            foreach (var guid in AssetDatabase.FindAssets("t:MutationData", new[] { MutationsFolder }))
            {
                var mouth = AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid));
                if (mouth == null || mouth.Slot != MutationSlot.Mouth || mouth.CreatureBody == null)
                    continue;
                string body = mouth.CreatureBody.name.Replace("Player_", "");
                var layer = ArtSetup.ImportFloor3SkinLayer(prefix, body);
                if (layer != null)
                    layers.Add(new MutationData.CreatureLayer { body = mouth.CreatureBody, layer = layer });
            }
            mutation.SetCreatureLayers(layers.ToArray());
        }

        // ---------- Ayudas ----------

        /// <summary>Los datos de la mutación: se crean si no existen; los textos, el color y el piso se ponen siempre.</summary>
        static T Mutation<T>(string assetName, string displayName, string description, MutationSlot slot, string level2, string level3)
            where T : MutationData
        {
            string path = $"{MutationsFolder}/{assetName}.asset";
            var mutation = AssetDatabase.LoadAssetAtPath<T>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(mutation, path);
            }
            mutation.ConfigureBase(displayName, description, slot, LineageSetup.SlotColors[(int)slot]);
            mutation.SetLevelDescriptions(level2, level3);
            mutation.SetFloor(3);
            EditorUtility.SetDirty(mutation);
            return mutation;
        }

        static void SetIcons(MutationData mutation, string name)
        {
            var small = ArtSetup.ImportLooseSprite($"{SourceFolder}/Icons/Icon_{name}_16.png", $"{UIArtFolder}/Icon_{name}_16.png");
            var large = ArtSetup.ImportLooseSprite($"{SourceFolder}/Icons/Icon_{name}_32.png", $"{UIArtFolder}/Icon_{name}_32.png");
            if (small != null)
                mutation.SetIcons(small, large);
        }

        static Sprite[] JumpEffect(string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{JumpSourceFolder}/Effects/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);

        static Sprite[] Effect(string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{SourceFolder}/Effects/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);

    }
}

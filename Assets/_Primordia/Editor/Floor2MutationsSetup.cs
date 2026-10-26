using System.Collections.Generic;
using System.IO;
using Primordia.Combat;
using Primordia.Mutations;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Las 6 mutaciones del piso 2 que no son Bocas (GDD, tabla de mutaciones del piso 2): las Extremidades (Cola
    /// propulsora y Espinas de Hallucigenia), la Piel (Placas de trilobite y Escleritos de Wiwaxia) y los Órganos (Saco de
    /// crías y Ojos compuestos, con el marco de facetas de la pantalla en GameUI). Importa su arte (docs/multimedia-temp/floor2-mutations),
    /// crea sus datos con textos y valores, sus capas sobre los cuerpos de las Bocas del piso, sus efectos y prefabs, y las
    /// mete en la reserva del piso 2. Rehace los textos, los efectos y las capas cada vez.
    /// </summary>
    public static class Floor2MutationsSetup
    {
        const string SourceFolder = "docs/multimedia-temp/floor2-mutations";
        const string IconsFolder = "docs/multimedia-temp/floor2-bodies/Icons";
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor2";
        const string Floor2PoolPath = "Assets/_Primordia/Data/Mutations/MutationPool_Floor2.asset";
        const string LayersArtFolder = "Assets/_Primordia/Art/Characters/Player/Floor2";
        const string EffectsArtFolder = "Assets/_Primordia/Art/Effects/Floor2";
        const string UIArtFolder = "Assets/_Primordia/Art/UI/Icons";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Mutations";
        const string EffectsFolder = "Assets/_Primordia/Prefabs/Effects";

        // Burbujas y púas por encima del suelo y por debajo de los enemigos y el jugador.
        const int BubbleOrder = 6;
        const int SpineOrder = -3;

        [MenuItem("Primordia/Mutations/Crear mutaciones del piso 2")]
        static void CreateAll()
        {
            Directory.CreateDirectory(MutationsFolder);
            Directory.CreateDirectory(EffectsArtFolder);
            Directory.CreateDirectory(PrefabsFolder);

            var created = new List<MutationData>
            {
                CreateTailPropulsor(),
                CreateHallucigeniaSpines(),
                CreateTrilobitePlates(),
                CreateWiwaxiaSclerites(),
                CreateBroodSac(),
                CreateCompoundEyes(),
            };
            AddVignette();
            AssetDatabase.SaveAssets();
            AddToPool(created);
            Debug.Log("Floor2MutationsSetup: las 6 mutaciones del piso 2 (Extremidades, Piel y Órganos) creadas, con sus capas y efectos, y en la reserva del piso 2.");
        }

        // ---------- Cola propulsora ----------

        static MutationData CreateTailPropulsor()
        {
            var tail = Mutation<TailPropulsorMutation>("Mutation_TailPropulsor", "Cola propulsora",
                "Dos esquivas seguidas antes de la recarga.", MutationSlot.Limbs,
                "La segunda esquiva deja 3 burbujas que hacen 2 de daño y empujan.",
                "Torbellino: las dos esquivas dejan 5 burbujas de 3 de daño.");
            SetIcons(tail, "TailPropulsor");
            SetLayers(tail, "Limbs", "Limbs_TailPropulsor");

            var pop = Floor2MouthsSetup.CreateFrameEffect("Effect_TailPropulsor_Pop", Effect("Fx_TailPropulsor_Pop", 16), 0.06f);
            var bubble = CreateBubble("TailBubble", Effect("Fx_TailPropulsor_Bubble", 12), 0.3f, pop);
            var big = CreateBubble("TailBubbleBig", Effect("Fx_TailPropulsor_BubbleBig", 16), 0.42f, pop);
            tail.ConfigureBubbles(bubble, big);
            EditorUtility.SetDirty(tail);
            return tail;
        }

        static TailBubble CreateBubble(string prefabName, Sprite[] frames, float radius, GameObject pop)
        {
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = BubbleOrder;
                if (frames.Length > 1)
                    root.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.12f, 0f);
                var bubble = root.AddComponent<TailBubble>();
                bubble.SetPopEffect(pop);
                SetFloat(bubble, "radius", radius);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{prefabName}.prefab").GetComponent<TailBubble>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Espinas de Hallucigenia ----------

        static MutationData CreateHallucigeniaSpines()
        {
            var spines = Mutation<HallucigeniaSpinesMutation>("Mutation_HallucigeniaSpines", "Espinas de Hallucigenia",
                "Al esquivar dejas 2 púas en el suelo durante 4 s; quien las pisa recibe 2 de daño.", MutationSlot.Limbs,
                "3 púas que duran 6 s.",
                "Erizo: al desaparecer, cada púa sale disparada hacia el enemigo más cercano.");
            SetIcons(spines, "HallucigeniaSpines");
            SetLayers(spines, "Limbs", "Limbs_HallucigeniaSpines");

            // La púa del suelo: el apoyo es la grieta, en (6, 10) de su cuadro de 12×12.
            var frames = ArtSetup.ImportLooseSheet($"{SourceFolder}/Effects/Fx_HallucigeniaSpine_Ground.png",
                $"{EffectsArtFolder}/Fx_HallucigeniaSpine_Ground.png", 12, new Vector2(0.5f, 1.5f / 12f));
            var spine = CreateSpine(frames);

            var projectile = Floor2MouthsSetup.CreateProjectile("Projectile_HallucigeniaSpine",
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Effects/Proj_HallucigeniaSpine.png", $"{EffectsArtFolder}/Proj_HallucigeniaSpine.png", 12),
                radius: 0.15f);
            var shot = CombatSetup.LoadAttack("Attack_HallucigeniaSpine");
            if (shot == null)
                shot = CombatSetup.CreateAttack("Attack_HallucigeniaSpine", projectile, 1f, 2, 12f, 0.8f);
            shot.Configure(projectile, fireRate: 1f, damage: 2, speed: 12f, lifetime: 0.8f);
            EditorUtility.SetDirty(shot);

            spines.ConfigureLook(spine, shot);
            EditorUtility.SetDirty(spines);
            return spines;
        }

        static GroundSpine CreateSpine(Sprite[] frames)
        {
            var root = new GameObject("GroundSpine");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = SpineOrder;
                var spine = root.AddComponent<GroundSpine>();
                spine.Configure(renderer, frames, 0.08f);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/GroundSpine.prefab").GetComponent<GroundSpine>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Placas de trilobite ----------

        static MutationData CreateTrilobitePlates()
        {
            var plates = Mutation<TrilobitePlatesMutation>("Mutation_TrilobitePlates", "Placas de trilobite",
                "+1 corazón. 1 de cada 4 proyectiles que te alcanzan se deshace contra tus placas sin hacerte daño.", MutationSlot.Skin,
                "Se deshace 1 de cada 3.",
                "Enrollarse: te enrollas al esquivar y destruyes los proyectiles contra los que esquivas.");
            SetIcons(plates, "TrilobitePlates");
            SetLayers(plates, "Skin", "Skin_TrilobitePlates");
            plates.ConfigureLook(Effect("Fx_TrilobitePlates_Bounce", 16), Effect("Fx_TrilobitePlates_Roll", 32));
            EditorUtility.SetDirty(plates);
            return plates;
        }

        // ---------- Escleritos de Wiwaxia ----------

        static MutationData CreateWiwaxiaSclerites()
        {
            var sclerites = Mutation<WiwaxiaScleritesMutation>("Mutation_WiwaxiaSclerites", "Escleritos de Wiwaxia",
                "Cada 9 s sueltas 3 escamas hacia los enemigos cercanos (2 de daño cada una).", MutationSlot.Skin,
                "Cada 7 s, 5 escamas.",
                "Órbita: antes de salir disparadas, las escamas giran 2 s a tu alrededor y destruyen los proyectiles enemigos que tocan.");
            SetIcons(sclerites, "WiwaxiaSclerites");
            SetLayers(sclerites, "Skin", "Skin_WiwaxiaSclerites");

            var scaleFrames = Effect("Proj_WiwaxiaSclerite", 10);
            var projectile = Floor2MouthsSetup.CreateProjectile("Projectile_WiwaxiaSclerite", scaleFrames, radius: 0.15f);
            var attack = CombatSetup.LoadAttack("Attack_WiwaxiaSclerite");
            if (attack == null)
                attack = CombatSetup.CreateAttack("Attack_WiwaxiaSclerite", projectile, 1f, 2, 11f, 0.9f);
            attack.Configure(projectile, fireRate: 1f, damage: 2, speed: 11f, lifetime: 0.9f);
            EditorUtility.SetDirty(attack);

            sclerites.ConfigureLook(attack, scaleFrames, Effect("Fx_WiwaxiaSclerites_Release", 24), Effect("Fx_WiwaxiaSclerite_Block", 12));
            EditorUtility.SetDirty(sclerites);
            return sclerites;
        }

        // ---------- Saco de crías ----------

        const int LarvaOrder = 7;

        static MutationData CreateBroodSac()
        {
            var sac = Mutation<BroodSacMutation>("Mutation_BroodSac", "Saco de crías",
                "Clic derecho: 2 larvas aliadas que persiguen y muerden durante 8 s. Recarga de 14 s.", MutationSlot.Organ,
                "3 larvas (8 s).",
                "Enjambre: la larva que mata a un enemigo se divide en dos (también 8 s).");
            SetIcons(sac, "BroodSac");
            sac.ConfigureAbility(14f, null);

            var open = Floor2MouthsSetup.CreateFrameEffect("Effect_BroodSac_Open", Effect("Fx_BroodSac_Open", 32), 0.07f);
            var splitEffect = Floor2MouthsSetup.CreateFrameEffect("Effect_BroodSac_Split", Effect("Fx_BroodSac_Split", 16), 0.06f);
            sac.ConfigureLook(CreateLarva(splitEffect), open);
            EditorUtility.SetDirty(sac);
            return sac;
        }

        static Larva CreateLarva(GameObject splitEffect)
        {
            Sprite[] Sheet(string name) =>
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Larva/{name}.png", $"{EffectsArtFolder}/{name}.png", 16);
            var swim = Sheet("Larva_Swim");
            var root = new GameObject("Larva");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = swim.Length > 0 ? swim[0] : null;
                renderer.sortingOrder = LarvaOrder;
                var larva = root.AddComponent<Larva>();
                larva.Configure(renderer, swim, Sheet("Larva_Bite"), Sheet("Larva_Death"), splitEffect);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/Larva.prefab").GetComponent<Larva>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Ojos compuestos ----------

        static MutationData CreateCompoundEyes()
        {
            var eyes = Mutation<CompoundEyesMutation>("Mutation_CompoundEyes", "Ojos compuestos",
                "Clic derecho: durante 3 s todo va al 60 % de velocidad salvo tú. Recarga de 12 s.", MutationSlot.Organ,
                "Dura 4 s.",
                "Visión total: mientras dura, tus ataques hacen un 30 % más de daño.");
            SetIcons(eyes, "CompoundEyes");
            eyes.ConfigureAbility(12f, null);
            eyes.ConfigureLook(Effect("Fx_CompoundEyes_Flash", 32));
            EditorUtility.SetDirty(eyes);
            return eyes;
        }

        /// <summary>El marco de facetas de los Ojos compuestos en GameUI: a pantalla completa, detrás del resto de la interfaz.</summary>
        static void AddVignette()
        {
            var sprite = ArtSetup.ImportLooseSprite($"{SourceFolder}/UI/UI_CompoundEyes_Vignette.png", $"{UIArtFolder}/UI_CompoundEyes_Vignette.png");
            var root = PrefabUtility.LoadPrefabContents(UISetup.GameUIPath);
            try
            {
                var old = root.transform.Find("ScreenVignette");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                var rect = UISetup.CreateRect("ScreenVignette", root.transform);
                UISetup.Stretch(rect);
                rect.SetAsFirstSibling();
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.enabled = false;
                rect.gameObject.AddComponent<ScreenVignette>();
                PrefabUtility.SaveAsPrefabAsset(root, UISetup.GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
            mutation.SetFloor(2);
            EditorUtility.SetDirty(mutation);
            return mutation;
        }

        static void SetIcons(MutationData mutation, string name)
        {
            var small = ArtSetup.ImportLooseSprite($"{IconsFolder}/Icon_{name}_16.png", $"{UIArtFolder}/Icon_{name}_16.png");
            var large = ArtSetup.ImportLooseSprite($"{IconsFolder}/Icon_{name}_32.png", $"{UIArtFolder}/Icon_{name}_32.png");
            if (small != null)
                mutation.SetIcons(small, large);
        }

        /// <summary>
        /// La capa de la mutación sobre cada cuerpo de criatura del piso (el de cada Boca del piso 2): sus hojas son
        /// <c>{prefix}_{cuerpo}_*</c> y el cuerpo sale del nombre de las animaciones de la Boca (Player_{cuerpo}).
        /// </summary>
        static void SetLayers(MutationData mutation, string folder, string prefix)
        {
            var layers = new List<MutationData.CreatureLayer>();
            foreach (var guid in AssetDatabase.FindAssets("t:MutationData", new[] { MutationsFolder }))
            {
                var mouth = AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid));
                if (mouth == null || mouth.Slot != MutationSlot.Mouth || mouth.CreatureBody == null)
                    continue;
                string body = mouth.CreatureBody.name.Replace("Player_", "");
                var layer = ArtSetup.ImportCreatureLayer($"{prefix}_{body}", $"{SourceFolder}/{folder}", $"{LayersArtFolder}/{prefix}/{body}");
                if (layer != null)
                    layers.Add(new MutationData.CreatureLayer { body = mouth.CreatureBody, layer = layer });
            }
            mutation.SetCreatureLayers(layers.ToArray());
        }

        static Sprite[] Effect(string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{SourceFolder}/Effects/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);

        static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Las mete en la reserva del piso 2 (elecciones, tienda y sala de cambio) si no están.</summary>
        static void AddToPool(IEnumerable<MutationData> mutations)
        {
            var pool = AssetDatabase.LoadAssetAtPath<MutationPool>(Floor2PoolPath);
            if (pool == null)
            {
                Debug.LogWarning("Floor2MutationsSetup: no existe MutationPool_Floor2; créala con Primordia > Rooms > Crear piso 2 y escena de prueba.");
                return;
            }
            var list = new List<MutationData>(pool.Mutations);
            foreach (var mutation in mutations)
            {
                if (mutation != null && !list.Contains(mutation))
                    list.Add(mutation);
            }
            pool.Configure(list, pool.OffersNewMouths);
            EditorUtility.SetDirty(pool);
            AssetDatabase.SaveAssets();
        }
    }
}

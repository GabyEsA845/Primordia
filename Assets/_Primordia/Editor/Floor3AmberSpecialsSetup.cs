using System.Collections.Generic;
using System.Linq;
using Primordia.Combat;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Las 3 mutaciones especiales del ámbar del piso 3 (paso 6, bloque 2, subpasos 2b-2d): Cercos de cucaracha, Cápsula de ámbar y Gota
    /// de ámbar. Crea sus datos con los textos de cada nivel, importa sus iconos, sus capas sobre los cuerpos de las criaturas del piso 3
    /// y los efectos (docs/multimedia-temp/floor3-mutations y floor3-specials-look/shell + level3), las mete en el bloque de la sala
    /// del ámbar y monta la escena de prueba Test_Floor3Specials, con atajos para probarlas una por una en sus tres niveles.
    /// </summary>
    public static class Floor3AmberSpecialsSetup
    {
        const string DataFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const string SourceFolder = "docs/multimedia-temp/floor3-mutations";
        const string LookFolder = "docs/multimedia-temp/floor3-specials-look";
        const string UIArtFolder = "Assets/_Primordia/Art/UI/Icons";
        const string EffectsArtFolder = "Assets/_Primordia/Art/Effects/Floor3Amber";
        const string ScenePath = "Assets/_Primordia/Scenes/Test_Floor3Specials.unity";
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_3_Themes.asset";

        static readonly string[] BodyNames = { "Frog", "Diplocaulus", "Whipscorpion" };

        [MenuItem("Primordia/Floor 3/Crear mutaciones especiales del ámbar")]
        internal static void CreateAll()
        {
            Directory.EnsureCreated(DataFolder);
            var cerci = CreateCerci();
            var capsule = CreateCapsule();
            var gland = CreateGland();
            AssetDatabase.SaveAssets();

            // El bloque de la sala del ámbar guarda ya las tres (antes tenía las del piso 2 de prueba).
            EditorApplication.ExecuteMenuItem("Primordia/Floor 3/Crear sala del ámbar");
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3AmberSpecialsSetup: Cercos de cucaracha, Cápsula de ámbar y Gota de ámbar creadas y en el bloque de la sala del ámbar.");
        }

        // ---------- Cercos de cucaracha ----------

        static CerciMutation CreateCerci()
        {
            var cerci = Mutation<CerciMutation>("Mutation_Cerci", "Cercos de cucaracha",
                "Si un proyectil está a punto de alcanzarte, haces un quiebro en el sitio, sin moverte, y te atraviesa sin daño. Recarga de 8 s, con un brillo cuando vuelve a estar listo. Solo proyectiles.",
                MutationSlot.Limbs,
                "Recarga de 6 s.",
                "Doble quiebro: guardas 2 quiebros, cada uno con su recarga, y tras cada uno vas un 30 % más rápido durante 2 s.");
            cerci.ConfigureLevels(new CerciMutation.CerciLevel { cooldown = 8f },
                new CerciMutation.CerciLevel { cooldown = 6f },
                new CerciMutation.CerciLevel { cooldown = 6f, charges = 2, speedBonus = 0.3f, speedSeconds = 2f });
            SetIcons(cerci, "Cerci");
            SetLayers(cerci, "Special", "Special_Cerci");
            cerci.SetArt(Effect(SourceFolder, "Effects", "Fx_Cerci_Flash", 32), Effect(SourceFolder, "Effects", "Fx_Cerci_Ready", 16),
                Effect(LookFolder, "level3", "Fx_Cerci_Burst_III", 56));
            EditorUtility.SetDirty(cerci);
            return cerci;
        }

        // ---------- Cápsula de ámbar ----------

        static AmberCapsuleMutation CreateCapsule()
        {
            var capsule = Mutation<AmberCapsuleMutation>("Mutation_AmberCapsule", "Cápsula de ámbar",
                "Un golpe que te deja a medio corazón o menos te encierra en ámbar 2 s, invulnerable e inmóvil. Al encerrarte te cura medio corazón; al romperse suelta esquirlas que dañan y empujan, y tienes 2 s de invencibilidad al salir. Una vez por sala.",
                MutationSlot.Skin,
                "Te cura 1 corazón entero en total y las esquirlas aturden 1 s (no a jefes).",
                "Doble cáscara: dos activaciones por sala y te cura 1 corazón y medio.");
            capsule.ConfigureLevels(new AmberCapsuleMutation.CapsuleLevel { heal = 1, perRoom = 1 },
                new AmberCapsuleMutation.CapsuleLevel { heal = 2, perRoom = 1, shardStun = 1f },
                new AmberCapsuleMutation.CapsuleLevel { heal = 3, perRoom = 2, shardStun = 1f });
            SetIcons(capsule, "AmberCapsule");
            SetLayers(capsule, "Special", "Special_AmberCapsule");
            capsule.SetArt(Shell(false), Shell(true), Effect(LookFolder, "level3", "Fx_CapsuleHeal", 48));
            EditorUtility.SetDirty(capsule);
            return capsule;
        }

        // ---------- Gota de ámbar ----------

        static AmberGlandMutation CreateGland()
        {
            var gland = Mutation<AmberGlandMutation>("Mutation_AmberGland", "Gota de ámbar",
                "Mantén el clic derecho para cargar y suelta para lanzar una gota que encierra a un enemigo 5 s (1,5 s a un jefe). Dentro del bloque está aturdido, hace de escudo y recibe daño; al romperse suelta esquirlas que dañan alrededor. Recarga de 20 s.",
                MutationSlot.Organ,
                "Encierra 8 s (jefes 2 s) y se recarga en 18 s.",
                "Fosilización: el enemigo muere al instante y su cadáver queda en ámbar como escudo que se puede romper soltando esquirlas (los jefes solo 2 s). Recarga de 15 s.");
            gland.ConfigureAbility(20f, null);
            gland.ConfigureLevels(new AmberGlandMutation.GlandLevel { cooldown = 20f, stunSeconds = 5f, bossSeconds = 1.5f },
                new AmberGlandMutation.GlandLevel { cooldown = 18f, stunSeconds = 8f, bossSeconds = 2f },
                new AmberGlandMutation.GlandLevel { cooldown = 15f, stunSeconds = 8f, bossSeconds = 2f, kills = true });
            SetIcons(gland, "AmberDrop");
            SetLayers(gland, "Special", "Special_AmberGland_Rest");
            gland.SetFullLayers(Layers("Special", "Special_AmberGland_Full"));
            gland.SetArt(Shell(false), new AmberShellArt(),
                Effect(LookFolder, "shell", "Proj_AmberDrop", 16), Effect(LookFolder, "shell", "Fx_AmberDrop_Hit", 24),
                Effect(LookFolder, "level3", "Fx_AmberStun_Stars", 32), Effect(LookFolder, "level3", "Fx_AmberCrystals_Erupt", 48),
                Effect(LookFolder, "level3", "Fx_AmberFossil_Front", 48));
            EditorUtility.SetDirty(gland);
            return gland;
        }

        // ---------- Ayudas ----------

        /// <summary>El bloque de dos capas: el de siempre (shell) o el de doble cáscara del nivel III (level3).</summary>
        static AmberShellArt Shell(bool levelThree)
        {
            string folder = levelThree ? "level3" : "shell";
            string prefix = levelThree ? "Fx_AmberShell3" : "Fx_AmberShell";
            return new AmberShellArt
            {
                backForm = Effect(LookFolder, folder, $"{prefix}_Back_Form", 48),
                backHold = Effect(LookFolder, folder, $"{prefix}_Back_Hold", 48),
                frontForm = Effect(LookFolder, folder, $"{prefix}_Front_Form", 48),
                frontHold = Effect(LookFolder, folder, $"{prefix}_Front_Hold", 48),
                crack = Effect(LookFolder, folder, $"{prefix}_Front_Crack", 48),
                shatter = Effect(LookFolder, "shell", "Fx_AmberShell_Shatter", 80),
            };
        }

        static Sprite[] Effect(string root, string folder, string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{root}/{folder}/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);

        /// <summary>Los datos de la mutación: se crean si no existen; los textos, el color, el piso y «especial» se ponen siempre.</summary>
        static T Mutation<T>(string assetName, string displayName, string description, MutationSlot slot, string level2, string level3)
            where T : MutationData
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var mutation = AssetDatabase.LoadAssetAtPath<T>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(mutation, path);
            }
            mutation.ConfigureBase(displayName, description, slot, LineageSetup.SlotColors[(int)slot]);
            mutation.SetLevelDescriptions(level2, level3);
            mutation.SetFloor(3);
            mutation.SetSpecial(true);
            // Fuera de los linajes: completan cualquiera como comodín.
            mutation.SetLineages(null);
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

        static void SetLayers(MutationData mutation, string folder, string prefix) => mutation.SetCreatureLayers(Layers(folder, prefix));

        /// <summary>La capa sobre cada cuerpo de criatura del piso 3 (el de cada Boca): hojas <c>{prefix}_{cuerpo}_*</c>.</summary>
        static MutationData.CreatureLayer[] Layers(string folder, string prefix)
        {
            var layers = new List<MutationData.CreatureLayer>();
            foreach (var guid in AssetDatabase.FindAssets("t:MutationData", new[] { DataFolder }))
            {
                var mouth = AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid));
                if (mouth == null || mouth.Slot != MutationSlot.Mouth || mouth.CreatureBody == null)
                    continue;
                string body = mouth.CreatureBody.name.Replace("Player_", "");
                var layer = ArtSetup.ImportFloor3SkinLayer(prefix, body, folder);
                if (layer != null)
                    layers.Add(new MutationData.CreatureLayer { body = mouth.CreatureBody, layer = layer });
            }
            return layers.ToArray();
        }

        // ---------- Escena de prueba ----------

        [MenuItem("Primordia/Floor 3/Crear escena de prueba de las especiales del ámbar")]
        static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            var cerci = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_Cerci.asset");
            var capsule = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_AmberCapsule.asset");
            var gland = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_AmberGland.asset");
            if (floor1 == null || config == null || cerci == null || capsule == null || gland == null)
            {
                Debug.LogError("Floor3AmberSpecialsSetup: faltan Floor_1, Floor_3_Themes o las mutaciones del ámbar (Primordia > Floor 3 > Crear mutaciones especiales del ámbar y Crear escena de prueba de las salas con tema).");
                return;
            }

            // La creación de la escena no sobrescribe una existente: se borra para regenerarla.
            AssetDatabase.DeleteAsset(ScenePath);
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            if (!CoreSetup.TryCreateSceneFromTemplate(ScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Al abrir la escena Unity descarga los assets sin usar: se vuelven a cargar (si no, las referencias quedan vacías).
            config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            cerci = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_Cerci.asset");
            capsule = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_AmberCapsule.asset");
            gland = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_AmberGland.asset");
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: config);
            Floor2Setup.AddTestLoadout(scene, floor1);
            Floor2Setup.AddHUD(scene);

            // Una Boca del piso 3 para que se vean las capas del cuerpo (la rana).
            var mouth = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_PoisonSpit.asset");
            var playerObject = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<PlayerMutations>()).FirstOrDefault(p => p != null);
            if (playerObject != null && mouth != null && playerObject.TryGetComponent<TestLoadout>(out var loadout))
            {
                var serialized = new SerializedObject(loadout);
                var list = serialized.FindProperty("mutations");
                list.arraySize = list.arraySize + 1;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = mouth;
                serialized.ApplyModifiedProperties();
            }

            var keys = new GameObject("SpecialsTestKeys").AddComponent<SpecialsTestKeys>();
            var keysData = new SerializedObject(keys);
            var specials = keysData.FindProperty("specials");
            specials.arraySize = 3;
            specials.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_Cerci.asset");
            specials.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_AmberCapsule.asset");
            specials.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<MutationData>($"{DataFolder}/Mutation_AmberGland.asset");
            keysData.FindProperty("shooterAttack").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AttackData>("Assets/_Primordia/Data/Attacks/Attack_CalamitesSpore.asset");
            keysData.FindProperty("dummyEnemy").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Enemy>("Assets/_Primordia/Prefabs/Enemies/Enemy_Medullosa.prefab");
            keysData.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3AmberSpecialsSetup: escena Test_Floor3Specials lista. Teclas 1/2/3 = Cercos/Cápsula/Gota (repetir = subir de nivel), 4 medio corazón, 5 vida llena, 6 tirador, 7 enemigo.");
        }

        static class Directory
        {
            public static void EnsureCreated(string path) => System.IO.Directory.CreateDirectory(path);
        }
    }
}

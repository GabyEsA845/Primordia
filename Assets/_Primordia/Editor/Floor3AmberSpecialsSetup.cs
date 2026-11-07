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
    /// y los efectos (docs/multimedia-temp/floor3-mutations, floor3-capsule y floor3-drop; los Cercos aún usan floor3-specials-look), las mete en el bloque de la sala
    /// del ámbar y monta la escena de prueba Test_Floor3Specials, con atajos para probarlas una por una en sus tres niveles.
    /// </summary>
    public static class Floor3AmberSpecialsSetup
    {
        const string DataFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const string SourceFolder = "docs/multimedia-temp/floor3-mutations";
        const string LookFolder = "docs/multimedia-temp/floor3-specials-look";
        const string CapsuleFolder = "docs/multimedia-temp/floor3-capsule";
        const string DropFolder = "docs/multimedia-temp/floor3-drop";
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
            DeleteOldArt();
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
            // Antes de activarse la lleva un halo con alas (objeto aparte), no una capa sobre el cuerpo.
            capsule.SetCreatureLayers();
            capsule.SetArt(CapsuleShell(false), CapsuleShell(true), new[] { Heal(1), Heal(2), Heal(3) }, Halo());
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
            // El cristal del lomo (vacío, a medias, lleno y cargando) lo pone AmberGlandGlow, no la capa normal del Órgano.
            gland.SetCreatureLayers();
            gland.SetCrystalLayers(Layers("glandula", "Special_AmberGland_Empty", DropFolder), Layers("glandula", "Special_AmberGland_Half", DropFolder),
                Layers("glandula", "Special_AmberGland_Full", DropFolder), Layers("glandula", "Special_AmberGland_Charge", DropFolder));
            gland.SetArt(DropShell("Normal", false), DropShell("Large", false), DropShell("Normal", true), DropShell("Large", true),
                DropSheet("disparo", "Proj_AmberDrop", 24, 12, 17, 6), DropSheet("disparo", "Fx_AmberDrop_Hit", 32, 32, 16, 16));
            EditorUtility.SetDirty(gland);
            return gland;
        }

        // ---------- Ayudas ----------

        // Tiempos de las animaciones (ms de las hojas, README de floor3-capsule y floor3-drop).
        static readonly float[] CapsuleFormMs = { 90, 90, 90, 90, 90, 90, 90, 90, 110, 110, 110, 110 };
        static readonly float[] CapsuleBreakMs = { 110, 110, 110, 110, 90, 70, 70, 70, 70, 70, 70, 70 };
        static readonly float[] DropFormMs = { 70, 70, 70, 70, 70, 90, 90 };
        static readonly float[] DropFormThreeMs = { 70, 70, 70, 70, 70, 90, 90, 110 };
        static readonly float[] DropBreakMs = { 90, 60, 60, 60, 60, 60, 60 };
        // Cuadro en el que cura cada pulso de la curación (nivel I, II y III) y cuánto se tiñe de dorado la criatura (generar_curacion.py).
        static readonly int[][] HealPulses = { new[] { 5 }, new[] { 5, 10 }, new[] { 5, 9, 13 } };
        static readonly float[][] HealTint =
        {
            new[] { 0f, 0f, 0.1f, 0.2f, 0.3f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.16f, 0.32f, 0.45f, 0.32f, 0.16f, 0f },
            new[] { 0f, 0f, 0.1f, 0.2f, 0.3f, 0.2f, 0.1f, 0.1f, 0.2f, 0.3f, 0.2f, 0.1f, 0f, 0f, 0.16f, 0.32f, 0.45f, 0.32f, 0.16f, 0f },
            new[] { 0f, 0f, 0.1f, 0.2f, 0.3f, 0.2f, 0.2f, 0.2f, 0.3f, 0.2f, 0.2f, 0.2f, 0.3f, 0.2f, 0.26f, 0.32f, 0.45f, 0.32f, 0.16f, 0f },
        };

        static float[] Seconds(float[] milliseconds) => milliseconds.Select(ms => ms / 1000f).ToArray();

        /// <summary>
        /// Una hoja del arte nuevo de la Cápsula o de la Gota. <paramref name="pivotX"/> y <paramref name="pivotY"/> son píxeles desde
        /// la esquina de arriba a la izquierda de cada cuadro (pueden salirse): el punto donde se ancla el dibujo. Los dibujos de
        /// un mismo bloque comparten ese punto aunque sus celdas sean distintas.
        /// </summary>
        static Sprite[] Sheet(string root, string folder, string name, int cellWidth, int cellHeight, float pivotX, float pivotY)
        {
            string subfolder = root == CapsuleFolder ? "Capsule" : "Drop";
            return ArtSetup.ImportLooseSheet($"{root}/{folder}/{name}.png", $"{EffectsArtFolder}/{subfolder}/{name}.png", cellWidth,
                new Vector2(pivotX / cellWidth, 1f - pivotY / cellHeight));
        }

        // ---------- Cápsula ----------

        // El cuerpo mide 32×32 y va con su centro en (28, 39) de la celda de 58×60 de la formación y el reposo, y en (47, 39) de la de 96×60
        // de la rotura y la curación; las vetas del suelo (96×16) empiezan 44 px más abajo que la celda de la cápsula.
        static Sprite[] Capsule(string folder, string name, bool wide) =>
            Sheet(CapsuleFolder, folder, name, wide ? 96 : 58, 60, wide ? 47 : 28, 39);

        static Sprite[] Veins(string folder, string name) => Sheet(CapsuleFolder, folder, name, 96, 16, 47, -5);

        static AmberShellArt CapsuleShell(bool levelThree)
        {
            string tag = levelThree ? "Fx_AmberCapsule3" : "Fx_AmberCapsule";
            return new AmberShellArt
            {
                formBack = Capsule("cristalizacion", $"{tag}_Form_Back", false),
                formFront = Capsule("cristalizacion", $"{tag}_Form_Front", false),
                formGlow = Capsule("cristalizacion", "Fx_AmberCapsule_Form_Glow", false),
                veinsForm = levelThree ? Veins("cristalizacion", "Fx_AmberFloorVeins_Form") : new Sprite[0],
                formDurations = Seconds(CapsuleFormMs),
                holdBack = Capsule("reposo", $"{tag}_Idle_Back", false),
                holdFront = Capsule("reposo", $"{tag}_Idle_Front", false),
                veinsHold = levelThree ? Veins("reposo", "Fx_AmberFloorVeins_Idle") : new Sprite[0],
                holdFrame = 0.12f,
                breakBack = Capsule("rotura", $"{tag}_Break_Back", true),
                breakFront = Capsule("rotura", $"{tag}_Break_Front", true),
                breakGlow = Capsule("rotura", "Fx_AmberCapsule_Break_Glow", true),
                veinsBreak = levelThree ? Veins("rotura", "Fx_AmberFloorVeins_Break") : new Sprite[0],
                breakDurations = Seconds(CapsuleBreakMs),
                // El quiebre (5.º cuadro) libera al jugador.
                breakCountFrame = 4,
            };
        }

        static AmberHealArt Heal(int level) => new()
        {
            back = Capsule("curacion", $"Fx_AmberCapsule_Heal{level}_Back", true),
            front = Capsule("curacion", $"Fx_AmberCapsule_Heal{level}_Front", true),
            hearts = Capsule("curacion", $"Fx_AmberCapsule_Heal{level}_Hearts", true),
            glow = Capsule("curacion", $"Fx_AmberCapsule_Heal{level}_Glow", true),
            frameSeconds = 0.1f,
            pulseFrames = HealPulses[level - 1],
            tint = HealTint[level - 1],
        };

        // El halo (44×28) se ancla en el centro del anillo (22, 18); su resplandor (56×36), en (28, 20).
        static AmberHaloArt Halo()
        {
            Sprite[] Ring(string name) => Sheet(CapsuleFolder, "halo", name, 44, 28, 22, 18);
            return new AmberHaloArt
            {
                ready = Ring("Fx_AmberHalo_I"),
                readyFull = Ring("Fx_AmberHalo_III_Full"),
                readyHalf = Ring("Fx_AmberHalo_III_Half"),
                off = Ring("Fx_AmberHalo_Off_I").FirstOrDefault(),
                offThree = Ring("Fx_AmberHalo_Off_III").FirstOrDefault(),
                fade = Ring("Fx_AmberHalo_Fade_I"),
                glow = Sheet(CapsuleFolder, "halo", "Fx_AmberHalo_Glow", 56, 36, 28, 20),
            };
        }

        // ---------- Gota ----------

        // Bloque normal 48×40 con los pies en (24, 34); grande 56×68 con los pies en (28, 62). Las hojas de la rotura (96×96) llevan el
        // centro del bloque en (48, 48), así que los pies caen 14 px (normal) o 28 px (grande) más abajo.
        static Sprite[] DropSheet(string folder, string name, int cellWidth, int cellHeight, float pivotX, float pivotY) =>
            Sheet(DropFolder, folder, name, cellWidth, cellHeight, pivotX, pivotY);

        static AmberShellArt DropShell(string size, bool levelThree)
        {
            bool large = size == "Large";
            int w = large ? 56 : 48, h = large ? 68 : 40;
            float feetX = w / 2f, feetY = large ? 62f : 34f;
            float centerRise = large ? 28f : 14f;
            string block = levelThree ? "Fx_AmberDropBlock3" : "Fx_AmberDropBlock";
            string folderHold = levelThree ? "nivel3" : "bloque";
            string folderForm = levelThree ? "nivel3" : "formacion";
            string folderBreak = levelThree ? "nivel3" : "rotura";

            Sprite[] Block(string folder, string name) => DropSheet(folder, name, w, h, feetX, feetY);
            Sprite[] Broken(string name) => DropSheet(folderBreak, name, 96, 96, 48, 48 + centerRise);

            return new AmberShellArt
            {
                formBack = Block(folderForm, $"{block}_{size}_Form_Back"),
                formFront = Block(folderForm, $"{block}_{size}_Form_Front"),
                formDurations = Seconds(levelThree ? DropFormThreeMs : DropFormMs),
                holdBack = Block(folderHold, $"{block}_{size}_Back"),
                holdFront = Block(folderHold, $"{block}_{size}_Front"),
                crack = Block("grietas", $"Fx_AmberDropBlock_{size}_Crack"),
                breakBack = Broken($"{block}_{size}_Break_Back"),
                breakFront = Broken($"{block}_{size}_Break_Front"),
                breakDurations = Seconds(DropBreakMs),
                // Cuadro 1: el enemigo queda libre y salen las esquirlas.
                breakCountFrame = 1,
                centerRise = centerRise / 16f,
                // Cuerpo sólido del bloque (el dibujo mide 41×28 px la talla normal y 49×59 la grande): frena al jugador y a los proyectiles.
                // El cadáver del nivel III tiene el suyo propio.
                solidSize = levelThree ? Vector2.zero : large ? new Vector2(3f, 3.6f) : new Vector2(2.5f, 1.7f),
                solidOffset = new Vector2(0f, large ? 1.72f : 0.75f),
                // Estrellas (32×16, base en la celda): su borde de arriba queda 12 px sobre la punta del bloque.
                stars = levelThree ? new Sprite[0] : DropSheet("aturdido", "Fx_AmberDropStun_Stars", 32, 16, 16, 16),
                starsRise = ((large ? 62f - 4f : 34f - 7f) - 4f) / 16f,
            };
        }

        /// <summary>Quita el arte que sustituyen las hojas nuevas (capas del lomo, bloque octogonal, gota, cristales y fósil).</summary>
        static void DeleteOldArt()
        {
            string[] oldEffects =
            {
                "Fx_AmberCrystals_Erupt", "Fx_AmberDrop_Hit", "Fx_AmberFossil_Front", "Fx_AmberShell3_Back_Form", "Fx_AmberShell3_Back_Hold",
                "Fx_AmberShell3_Front_Crack", "Fx_AmberShell3_Front_Form", "Fx_AmberShell3_Front_Hold", "Fx_AmberShell_Back_Form",
                "Fx_AmberShell_Back_Hold", "Fx_AmberShell_Front_Crack", "Fx_AmberShell_Front_Form", "Fx_AmberShell_Front_Hold",
                "Fx_AmberShell_Shatter", "Fx_AmberStun_Stars", "Fx_CapsuleHeal", "Proj_AmberDrop",
            };
            foreach (string name in oldEffects)
                AssetDatabase.DeleteAsset($"{EffectsArtFolder}/{name}.png");
            foreach (string layer in new[] { "Special_AmberCapsule", "Special_AmberGland_Rest", "Special_AmberGland_Full" })
                AssetDatabase.DeleteAsset($"Assets/_Primordia/Art/Characters/Player/Floor3Skins/{layer}");
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
        static MutationData.CreatureLayer[] Layers(string folder, string prefix, string root = SourceFolder)
        {
            var layers = new List<MutationData.CreatureLayer>();
            foreach (var guid in AssetDatabase.FindAssets("t:MutationData", new[] { DataFolder }))
            {
                var mouth = AssetDatabase.LoadAssetAtPath<MutationData>(AssetDatabase.GUIDToAssetPath(guid));
                if (mouth == null || mouth.Slot != MutationSlot.Mouth || mouth.CreatureBody == null)
                    continue;
                string body = mouth.CreatureBody.name.Replace("Player_", "");
                var layer = ArtSetup.ImportFloor3SkinLayer(prefix, body, folder, root);
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

using System.IO;
using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Sala de paso del piso 2 al 3: la misma cueva de la sala de paso del piso 1, que pasa del Abismo (la entrada, junto al jefe) a
    /// las Someras y al pantano junto a la charca. Crea el prefab de la charca (con su burbuja que revienta), lo pone en Floor_2 con
    /// las 3 Bocas del piso 3 (rana, Diplocaulus y vinagrillo) y enlaza Floor_2 con Floor_3. También crea Test_Floor2Transition:
    /// un piso 2 corto (inicio, un "jefe" perseguidor y la sala de paso). Se puede repetir sin miedo.
    /// </summary>
    public static partial class FloorTransitionSetup
    {
        internal const string SwampPitPrefabPath = PrefabsFolder + "/TransitionPit_Swamp.prefab";
        const string Floor2TestScenePath = "Assets/_Primordia/Scenes/Test_Floor2Transition.unity";
        const string Floor2TestConfigPath = RoomsFolder + "/Floor_2TransitionTest.asset";
        const string Floor3MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        internal const string Floor3FloorName = "pantano carbonífero";

        [MenuItem("Primordia/Floor transition/Crear sala de paso del piso 2 (charca) y escena de prueba")]
        static void CreateFloor2Transition()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var floor2 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor1 == null || floor2 == null || floor3 == null)
            {
                Debug.LogError("FloorTransitionSetup: faltan Floor_1, Floor_2 o Floor_3; crea antes los pisos "
                    + "(Primordia > Rooms > Crear piso 2 y Primordia > Floor 3 > Crear piso 3).");
                return;
            }

            var swamp = ArtSetup.FindTheme("Swamp");
            var shallows = floor2.BandThemes.Length > 0 ? floor2.BandThemes[0] : null;
            var art = ArtSetup.ImportSwampPitArt();
            var mouths = new[] { "Mutation_PoisonSpit", "Mutation_SpinningTop", "Mutation_VinegarJet" }
                .Select(name => AssetDatabase.LoadAssetAtPath<MutationData>($"{Floor3MutationsFolder}/{name}.asset"))
                .ToArray();
            if (swamp == null || shallows == null || art == null || mouths.Any(mouth => mouth == null))
            {
                Debug.LogError("FloorTransitionSetup: falta el tema Swamp (Primordia > Floor 3 > Crear tilesets), las Someras de Floor_2, el arte "
                    + "de la charca (docs/multimedia-temp/floor3-transition) o las Bocas del piso 3 (Primordia > Floor 3 > Crear Bocas del piso 3).");
                return;
            }

            var square = CoreSetup.CreateSquareSprite();
            var cave = CreateCaveTemplate();
            var rising = ArtSetup.ImportRisingMetamorphosisArt();
            var pit = CreatePitPrefab(square, art, SwampPitPrefabPath, ArtSetup.SwampBubblePopFrames, rising);
            ArtSetup.ApplyFloor3Metamorphosis();

            floor2.ConfigureTransition(cave, pit, shallows, swamp);
            floor2.ConfigureNextFloor(mouths, Floor3FloorName);
            floor2.ConfigureFloor(2, floor3);
            EditorUtility.SetDirty(floor2);
            AssetDatabase.SaveAssets();

            CreateFloor2TestScene(floor1, floor2, cave, pit, shallows, swamp, mouths, square);
            Debug.Log("FloorTransitionSetup: sala de paso del piso 2 lista en Floor_2 (Abismo > Someras > Pantano, charca, Bocas del piso 3).");
        }

        static void CreateFloor2TestScene(FloorConfig floor1, FloorConfig floor2, RoomTemplate cave, TransitionPit pit, RoomTheme shallows,
            RoomTheme swamp, MutationData[] mouths, Sprite square)
        {
            var chaser = EnemySetup.LoadPrefab("Enemy_Chaser");
            if (chaser == null)
            {
                Debug.LogWarning("FloorTransitionSetup: falta Enemy_Chaser; no se crea la escena de prueba del piso 2.");
                return;
            }

            AssetDatabase.DeleteAsset(Floor2TestConfigPath);
            var config = Object.Instantiate(floor2);
            AssetDatabase.CreateAsset(config, Floor2TestConfigPath);
            // Un piso 2 corto: inicio, una sala de combate, un "jefe" que es un perseguidor y la sala de paso. Sin tienda ni recompensas.
            config.ConfigureRooms(floor2.StartRoom, floor2.BossRoom, floor2.CombatRooms, System.Array.Empty<RoomTemplate>(),
                new Vector2Int(1, 1), Vector2Int.zero);
            config.ConfigureShop(null, null);
            config.ConfigureRewards(floor2.RewardPool, System.Array.Empty<int>());
            config.ConfigureBosses(new[] { new FloorConfig.BossEntry(chaser, floor2.BossRoom, floor2.BossTheme, Vector2.zero) });
            config.ConfigureTransition(cave, pit, shallows, swamp);
            config.ConfigureNextFloor(mouths, Floor3FloorName);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(Floor2TestScenePath);
            if (!CoreSetup.TryCreateSceneFromTemplate(Floor2TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se vuelve a cargar: al abrir la escena nueva Unity descarga los assets sin usar.
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2TestConfigPath));
            Floor2Setup.AddTestLoadout(scene, AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath));
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"FloorTransitionSetup: {Floor2TestScenePath} creada: inicio > sala > \"jefe\" perseguidor (F3 vacía la sala) > sala de paso.");
        }

    }
}

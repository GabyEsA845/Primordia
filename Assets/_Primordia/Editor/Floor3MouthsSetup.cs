using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Las Bocas del piso 3 (GDD, "Mutaciones del piso 3"), por subtandas. Subtanda 3a: los 3 cuerpos (rana, Diplocaulus y
    /// vinagrillo, con su esquiva propia) y las 3 Bocas creadas con un ataque provisional (el del Aguijón para el Chorro, el de
    /// las Mandíbulas para la Peonza y el de las Esporas para el Escupitajo); las subtandas siguientes ponen el ataque de
    /// verdad de cada una. Las siluetas de la metamorfosis aún no existen. Las Bocas están en el piso 3 para el atajo F6.
    /// </summary>
    public static class Floor3MouthsSetup
    {
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const float FrogAttackHitTime = 7 * 0.08f;

        [MenuItem("Primordia/Floor 3/Crear Bocas del piso 3 (cuerpos y datos)")]
        internal static void CreateAll()
        {
            Directory.CreateDirectory(MutationsFolder);
            var art = ArtSetup.ImportFloor3MouthArt();
            var color = LineageSetup.SlotColors[(int)MutationSlot.Mouth];

            var poisonSpit = CreateMouth("Mutation_PoisonSpit", "Escupitajo venenoso",
                "Bola de veneno lenta que atraviesa y deja un rastro de charcos. (Ataque provisional.)",
                WeaponFamily.Area, "Attack_Spores", color, art.PoisonSpit);
            // La rana suelta la bola en el cuadro 7 de su ataque (0,56 s).
            poisonSpit.SetAttackHitTime(FrogAttackHitTime);
            // Peso al arrancar y frenar: la rana es ágil, el Diplocaulus resbala.
            poisonSpit.SetMoveInertia(0.8f);

            var spinningTop = CreateMouth("Mutation_SpinningTop", "Peonza",
                "Gira como una peonza con la cabeza como aspa. (Ataque provisional.)",
                WeaponFamily.Melee, "Attack_Mandibles", color, art.SpinningTop);
            spinningTop.SetMoveInertia(1.5f);

            CreateMouth("Mutation_VinegarJet", "Chorro de vinagre",
                "Chorro de vinagre desde la cola alzada. (Ataque provisional.)",
                WeaponFamily.Projectile, "Attack_Stinger", color, art.VinegarJet);

            AssetDatabase.SaveAssets();
            EquipInTestScene();
            Debug.Log("Floor3MouthsSetup: cuerpos de la rana, el Diplocaulus y el vinagrillo y sus 3 Bocas (con ataque provisional).");
        }

        static AttackMutation CreateMouth(string assetName, string displayName, string description, WeaponFamily family,
            string placeholderAttack, Color color, ArtSetup.MouthArt art)
        {
            string path = $"{MutationsFolder}/{assetName}.asset";
            var mutation = AssetDatabase.LoadAssetAtPath<AttackMutation>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<AttackMutation>();
                mutation.ConfigureBase(displayName, description, MutationSlot.Mouth, color);
                mutation.Configure(CombatSetup.LoadAttack(placeholderAttack), newHealEveryKills: 0);
                AssetDatabase.CreateAsset(mutation, path);
            }

            mutation.SetFloor(3, family);
            if (art != null)
            {
                mutation.SetIcons(art.Icon, art.LargeIcon);
                mutation.SetCreatureFrames(art.Idle, art.Attack);
                mutation.SetCreature(art.Body, art.Morph, art.Reveal);
            }
            EditorUtility.SetDirty(mutation);
            return mutation;
        }

        /// <summary>Test_Floor3 empieza con la rana (en vez de una Boca del piso 2), a nivel II como el resto.</summary>
        static void EquipInTestScene()
        {
            const string scenePath = "Assets/_Primordia/Scenes/Test_Floor3.unity";
            var frog = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationsFolder}/Mutation_PoisonSpit.asset");
            if (frog == null || !File.Exists(scenePath) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(scenePath);
            var player = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<PlayerMutations>()).FirstOrDefault(p => p != null);
            var loadout = player != null ? player.GetComponent<TestLoadout>() : null;
            if (loadout == null)
                return;

            var serialized = new SerializedObject(loadout);
            var list = serialized.FindProperty("mutations");
            for (int i = 0; i < list.arraySize; i++)
            {
                var item = list.GetArrayElementAtIndex(i).objectReferenceValue as MutationData;
                if (item != null && item.Slot == MutationSlot.Mouth)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = frog;
            }
            serialized.ApplyModifiedProperties();
            EditorSceneManager.SaveScene(scene);
        }
    }
}

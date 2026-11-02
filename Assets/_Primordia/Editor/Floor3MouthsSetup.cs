using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
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
        const string ReactionKitPath = "Assets/_Primordia/Data/Rooms/ReactionKit_Floor3.asset";
        const string ProjectilesFolder = "Assets/_Primordia/Prefabs/Combat";

        [MenuItem("Primordia/Floor 3/Crear Bocas del piso 3 (cuerpos y datos)")]
        internal static void CreateAll()
        {
            Directory.CreateDirectory(MutationsFolder);
            var art = ArtSetup.ImportFloor3MouthArt();
            var color = LineageSetup.SlotColors[(int)MutationSlot.Mouth];

            var poisonSpit = CreateMouth("Mutation_PoisonSpit", "Escupitajo venenoso",
                "Bola de veneno lenta que atraviesa y deja un rastro de charcos. ",
                WeaponFamily.Area, "Attack_Spores", color, art.PoisonSpit);
            // La rana suelta la bola en el cuadro 7 de su ataque (0,56 s).
            poisonSpit.SetAttackHitTime(FrogAttackHitTime);
            // Peso al arrancar y frenar: la rana es ágil, el Diplocaulus resbala.
            poisonSpit.SetMoveInertia(0.8f);
            ConfigurePoisonSpit(poisonSpit);

            var spinningTop = CreateMouth("Mutation_SpinningTop", "Peonza",
                "Gira como una peonza con la cabeza como aspa.",
                WeaponFamily.Melee, "Attack_Mandibles", color, art.SpinningTop);
            spinningTop.SetMoveInertia(1.5f);
            ConfigureSpinningTop(spinningTop);

            var vinegarJet = CreateMouth("Mutation_VinegarJet", "Chorro de vinagre",
                "Chorro de vinagre desde la cola alzada.",
                WeaponFamily.Projectile, "Attack_Stinger", color, art.VinegarJet);
            ConfigureVinegarJet(vinegarJet);

            AssetDatabase.SaveAssets();
            ArtSetup.ConfigureSpitKit(AssetDatabase.LoadAssetAtPath<ReactionKit>(ReactionKitPath));
            AssetDatabase.SaveAssets();
            EquipInTestScene();
            Debug.Log("Floor3MouthsSetup: cuerpos de la rana, el Diplocaulus y el vinagrillo y sus 3 Bocas (Escupitajo, Peonza y Chorro de vinagre).");
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

        // ---------- Escupitajo venenoso (rana) ----------

        /// <summary>
        /// Nivel I: bola cada 2,8 s a 2 u/s que atraviesa (1 de daño al tocar), vive 4 s, deja un charco de 3 s cada 0,5 s y estalla
        /// con 6 de daño en 1,5 u y un charco grande de 4 s. II: cada 2,4 s, rastro de 4 s y 8 de daño. III, Camada: el estallido suelta
        /// 4 bolitas en cruz que viajan 1 s y estallan con 3 de daño en 0,8 u.
        /// </summary>
        static void ConfigurePoisonSpit(AttackMutation mutation)
        {
            string effects = "Assets/_Primordia/Art/Floor3/floor3-bodies/Effects";
            var ballFrames = ArtSetup.Floor3BodyEffect("Proj_PoisonSpit", 24);
            var miniFrames = ArtSetup.Floor3BodyEffect("Proj_PoisonSpitSmall", 12);
            if (ballFrames.Length == 0 || miniFrames.Length == 0)
            {
                Debug.LogWarning($"Floor3MouthsSetup: faltan las hojas del Escupitajo en {effects}; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            Directory.CreateDirectory(ProjectilesFolder);
            var ball = CreateSpitProjectile("Projectile_PoisonSpit", ballFrames, radius: 0.3f, frameSeconds: 0.07f);
            var mini = CreateSpitProjectile("Projectile_PoisonSpitMini", miniFrames, radius: 0.15f, frameSeconds: 0.08f);

            var template = CombatSetup.LoadAttack("Attack_Stinger");
            var miniAttack = Attack("Attack_PoisonSpitMini", template, a =>
            {
                a.Configure(mini, fireRate: 1f, damage: 0, speed: 2.5f, lifetime: 1f);
                a.SetPierce(99);
                a.ConfigureSpit(0.5f, 3f, burstDamage: 3, burstRadius: 0.8f, burstPuddleSeconds: 3f, mini: true);
                ClearExtras(a);
            });
            var one = Attack("Attack_PoisonSpit", template, a =>
            {
                a.Configure(ball, fireRate: 1f / 2.8f, damage: 1, speed: 2f, lifetime: 4f, automatic: true);
                a.SetPierce(99);
                a.ConfigureSpit(0.5f, 3f, burstDamage: 6, burstRadius: 1.5f, burstPuddleSeconds: 4f);
                ClearExtras(a);
            });
            var two = Attack("Attack_PoisonSpit_2", one, a =>
            {
                a.SetFireRate(1f / 2.4f);
                a.ConfigureSpit(0.5f, 4f, burstDamage: 8, burstRadius: 1.5f, burstPuddleSeconds: 4f);
            });
            var three = Attack("Attack_PoisonSpit_3", two, a =>
                a.ConfigureSpit(0.5f, 4f, burstDamage: 8, burstRadius: 1.5f, burstPuddleSeconds: 4f, splitCount: 4, splitAttack: miniAttack));

            mutation.ConfigureBase(mutation.DisplayName,
                "Bola de veneno lenta (2 u/s) que atraviesa a los enemigos con 1 de daño y deja un charco cada 0,5 s que frena y envenena. Encoge mientras vuela y estalla a los 4 s, al chocar con una pared o al disparar otra: 6 de daño en 1,5 u y un charco grande de 4 s. Una bola cada 2,8 s.",
                MutationSlot.Mouth, mutation.Color);
            mutation.SetLevelDescriptions("Una bola cada 2,4 s, el rastro dura 4 s y el estallido hace 8 de daño.",
                "Camada: al estallar suelta 4 bolitas en cruz que viajan 1 s y estallan con 3 de daño.");
            mutation.Configure(one, newHealEveryKills: 0);
            mutation.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
            EditorUtility.SetDirty(mutation);
        }

        // ---------- Peonza (Diplocaulus) ----------

        /// <summary>
        /// Nivel I: mantener gira (radio 1,3 u, 2 de daño cada 0,25 s, empuja, 35 % más lento), máximo 3 s y mareo de 0,5 s. II: 3 de
        /// daño, radio 1,5 u, 4 s y devuelve proyectiles. III, Bumerán: tras girar al menos 1 s, al soltar sale una peonza 3 u hacia
        /// el cursor (8 de daño y aturde, a la ida y a la vuelta).
        /// </summary>
        static void ConfigureSpinningTop(AttackMutation mutation)
        {
            var whirlFrames = ArtSetup.Floor3BodyEffect("Fx_SpinningTop_Whirl", 48);
            var deflectFrames = ArtSetup.Floor3BodyEffect("Fx_SpinningTop_Deflect", 16);
            var spinFrames = ArtSetup.Floor3BodySheet("Diplocaulus", "Spin");
            if (whirlFrames.Length == 0 || deflectFrames.Length == 0 || spinFrames.Length == 0)
            {
                Debug.LogWarning("Floor3MouthsSetup: faltan las hojas de la Peonza; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            Directory.CreateDirectory(ProjectilesFolder);
            var whirl = Floor2MouthsSetup.CreateLoopEffect("Effect_SpinningTop_Whirl", whirlFrames, 0.05f, 11);
            var deflect = Floor2MouthsSetup.CreateFrameEffect("Effect_SpinningTop_Deflect", deflectFrames, 0.05f);
            var boomerang = CreateBoomerangPrefab("Boomerang_Diplocaulus", spinFrames);

            var one = Attack("Attack_SpinningTop", null, a =>
            {
                a.Configure(null, fireRate: 4f, damage: 2, speed: 1f, lifetime: 1f);
                a.ConfigureSpin(1.3f, 2, 0.25f, 6f, 0.65f, 3f, 1f, 0.5f, false, whirl, deflect);
                a.ConfigureBoomerang(0f, 9f, 8, 0.8f, null);
            });
            var two = Attack("Attack_SpinningTop_2", one, a =>
            {
                a.SetDamage(3);
                a.ConfigureSpin(1.5f, 3, 0.25f, 6f, 0.65f, 4f, 1f, 0.5f, true, whirl, deflect);
            });
            var three = Attack("Attack_SpinningTop_3", two, a => a.ConfigureBoomerang(1f, 9f, 8, 0.8f, boomerang));

            mutation.ConfigureBase(mutation.DisplayName,
                "Mantén el botón para girar como una peonza: golpea a los enemigos cercanos (2 de daño cada 0,25 s en 1,3 u) y los empuja, pero te mueves un 35 % más lento. Aguanta 3 s y después te mareas 1 s, sin poder moverte ni atacar.",
                MutationSlot.Mouth, mutation.Color);
            mutation.SetLevelDescriptions("3 de daño, radio de 1,5 u, aguanta 4 s y devuelve los proyectiles que entran en el radio.",
                "Bumerán: si giras al menos 1 s, al soltar lanza un clon translúcido 9 u hacia el cursor que va y vuelve con 8 de daño y aturde. No puedes atacar hasta que regrese.");
            mutation.Configure(one, newHealEveryKills: 0);
            mutation.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
            EditorUtility.SetDirty(mutation);
        }

        /// <summary>La peonza que sale y vuelve del nivel III, con el dibujo de Spin en bucle. Se rehace.</summary>
        static GameObject CreateBoomerangPrefab(string prefabName, Sprite[] frames)
        {
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = 20;
                // Clon translúcido del Diplocaulus (provisional: los mismos cuadros del giro; el clon dibujado llegará con el arte).
                renderer.color = new Color(0.75f, 0.95f, 1f, 0.6f);
                root.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.04f, 0f);
                root.AddComponent<SpinBoomerang>();
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilesFolder}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Chorro de vinagre (vinagrillo) ----------

        /// <summary>
        /// Nivel I: mantener echa un chorro de 5 u desde la cola que sigue al cursor a 180°/s y hace 1 de daño cada 0,15 s al primer enemigo
        /// o pared que toca; barra de 3 s que se rellena en 2 s al soltar (3 s si se vació, sin poder usarlo hasta llenarse). II: atraviesa a
        /// los enemigos y la barra dura 4 s. III, Salpicadura: 7 u de alcance y 1 de daño en 2 u alrededor del final del chorro cada 0,5 s (el II llega a 6 u).
        /// </summary>
        static void ConfigureVinegarJet(AttackMutation mutation)
        {
            var streamFrames = ArtSetup.Floor3BodyEffect("Fx_VinegarJet_Stream", 16);
            var muzzleFrames = ArtSetup.Floor3BodyEffect("Fx_VinegarJet_Muzzle", 12);
            var hitFrames = ArtSetup.Floor3BodyEffect("Fx_VinegarJet_Hit", 16);
            var splashFrames = ArtSetup.Floor3BodyEffect("Fx_VinegarJet_Splash", 32);
            if (streamFrames.Length == 0 || muzzleFrames.Length == 0 || hitFrames.Length == 0 || splashFrames.Length == 0)
            {
                Debug.LogWarning("Floor3MouthsSetup: faltan las hojas del Chorro de vinagre; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            Directory.CreateDirectory(ProjectilesFolder);
            var segment = CreateJetSegmentPrefab("Effect_VinegarJet_Segment", streamFrames);
            var muzzle = Floor2MouthsSetup.CreateLoopEffect("Effect_VinegarJet_Muzzle", muzzleFrames, 0.05f, 21);
            var hit = Floor2MouthsSetup.CreateLoopEffect("Effect_VinegarJet_Hit", hitFrames, 0.06f, 21);
            var splash = Floor2MouthsSetup.CreateFrameEffect("Effect_VinegarJet_Splash", splashFrames, 0.06f);

            var one = Attack("Attack_VinegarJet", null, a =>
            {
                a.Configure(null, fireRate: 1f / 0.15f, damage: 1, speed: 1f, lifetime: 1f);
                a.ConfigureJet(5f, 0.15f, 180f, 3f, 2f, 3f, false, segment, hit, splash);
                a.ConfigureJetSplash(0f, 0.5f, 1);
                a.SetMuzzleEffect(muzzle);
            });
            var two = Attack("Attack_VinegarJet_2", one, a => a.ConfigureJet(6f, 0.15f, 180f, 4f, 2f, 3f, true, segment, hit, splash));
            var three = Attack("Attack_VinegarJet_3", two, a =>
            {
                a.ConfigureJet(7f, 0.15f, 180f, 4f, 2f, 3f, true, segment, hit, splash);
                a.ConfigureJetSplash(2f, 0.5f, 1);
            });

            mutation.ConfigureBase(mutation.DisplayName,
                "Mantén el botón para echar un chorro de vinagre de 5 u desde la cola, que sigue al cursor (180°/s) y hace 1 de daño cada 0,15 s al primer enemigo que toca. La barra dura 3 s y se rellena en 2 s al soltar; si se vacía, no puedes usarlo hasta que se llene (3 s).",
                MutationSlot.Mouth, mutation.Color);
            mutation.SetLevelDescriptions("Atraviesa a los enemigos, llega a 6 u y la barra dura 4 s.",
                "Salpicadura: llega a 7 u y cada 0,5 s hace 1 de daño a los enemigos a 2 u del final del chorro.");
            mutation.Configure(one, newHealEveryKills: 0);
            mutation.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
            EditorUtility.SetDirty(mutation);
        }

        /// <summary>Un tramo de 1 u del chorro (16×8 px, hacia la derecha) con su animación en bucle. Se rehace.</summary>
        static GameObject CreateJetSegmentPrefab(string prefabName, Sprite[] frames)
        {
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = 19;
                root.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.06f, 0f);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilesFolder}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Un ataque limpio de efectos heredados del Aguijón (veneno, abanico, buscador…).
        static void ClearExtras(AttackData attack)
        {
            attack.SetPoison(0, 0f);
            attack.ConfigureVolley(0, 3, 30f);
            attack.ConfigureHoming(0f, 0f);
            attack.ConfigureContagion(0f, null);
            attack.ConfigureBleed(0, 0f, null);
            attack.SetMuzzleEffect(null);
        }

        /// <summary>El ataque de ese nombre, copiado de <paramref name="from"/> y cambiado con <paramref name="edit"/>. Se rehace cada vez.</summary>
        static AttackData Attack(string assetName, AttackData from, System.Action<AttackData> edit)
        {
            var attack = CombatSetup.LoadAttack(assetName) ?? CombatSetup.CreateAttack(assetName, null, 1f, 1, 1f, 1f);
            if (from != null)
                attack.CopyFrom(from);
            edit(attack);
            EditorUtility.SetDirty(attack);
            return attack;
        }

        /// <summary>Proyectil con la bola animada (los cuadros miran a la derecha) y el componente del Escupitajo. Se rehace.</summary>
        static Projectile CreateSpitProjectile(string prefabName, Sprite[] frames, float radius, float frameSeconds)
        {
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = 20;
                root.AddComponent<SpriteLoop>().Configure(renderer, frames, frameSeconds, 0f);
                var collider = root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = radius;
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;
                root.AddComponent<Projectile>();
                root.AddComponent<PoisonSpitBall>();
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilesFolder}/{prefabName}.prefab").GetComponent<Projectile>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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

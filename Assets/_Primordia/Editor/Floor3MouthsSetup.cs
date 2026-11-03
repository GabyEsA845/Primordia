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
        const string JetArtPath = "Assets/_Primordia/Data/Attacks/JetArt_VinegarJet.asset";

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
            Floor3MutationsSetup.EnsurePool();
            MouthSummaries.Apply();
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
        /// Nivel I: bola cada 2,8 s a 2 u/s que rueda atravesando (1 de daño al tocar), vive 4 s, deja una banda de baba que se seca a los
        /// 3 s (cada sello desde que se pinta) y estalla con 6 de daño en 1,5 u y un charco salpicado de 4 s. II: cada 2,4 s, rastro de 4 s y 8 de daño. III, Camada: el estallido suelta
        /// 4 bolitas en cruz que viajan 1 s y estallan con 3 de daño en 0,8 u.
        /// </summary>
        static void ConfigurePoisonSpit(AttackMutation mutation)
        {
            // El pivote de las bolas es su punto de apoyo en el suelo (README de floor3-spit).
            var ballPivot = new Vector2(0.5f, 0.3125f);
            var miniPivot = new Vector2(0.5f, 0.29f);
            var ballArt = new SpitBallArt(
                ArtSetup.Floor3SpitSheet("Proj_PoisonSpit_Roll_side", 24, ballPivot), ArtSetup.Floor3SpitSheet("Proj_PoisonSpit_Roll_down", 24, ballPivot),
                ArtSetup.Floor3SpitSheet("Proj_PoisonSpit_Roll_up", 24, ballPivot), ArtSetup.Floor3SpitSheet("Proj_PoisonSpit_Swell_side", 24, ballPivot),
                ArtSetup.Floor3SpitSheet("Proj_PoisonSpit_Swell_down", 24, ballPivot), ArtSetup.Floor3SpitSheet("Proj_PoisonSpit_Swell_up", 24, ballPivot));
            var miniArt = new SpitBallArt(
                ArtSetup.Floor3SpitSheet("Proj_PoisonSpitSmall_Roll_side", 12, miniPivot), ArtSetup.Floor3SpitSheet("Proj_PoisonSpitSmall_Roll_down", 12, miniPivot),
                ArtSetup.Floor3SpitSheet("Proj_PoisonSpitSmall_Roll_up", 12, miniPivot), System.Array.Empty<Sprite>(),
                System.Array.Empty<Sprite>(), System.Array.Empty<Sprite>());
            if (!ballArt.Complete || miniArt.Side.Length == 0 || miniArt.Down.Length == 0 || miniArt.Up.Length == 0)
            {
                Debug.LogWarning("Floor3MouthsSetup: faltan las hojas del Escupitajo en Art/Floor3/floor3-spit; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            Directory.CreateDirectory(ProjectilesFolder);
            var ball = CreateSpitProjectile("Projectile_PoisonSpit", ballArt, radius: 0.3f, rollFrameSeconds: 0.135f);
            var mini = CreateSpitProjectile("Projectile_PoisonSpitMini", miniArt, radius: 0.15f, rollFrameSeconds: 0.11f);

            var template = CombatSetup.LoadAttack("Attack_Stinger");
            var miniAttack = Attack("Attack_PoisonSpitMini", template, a =>
            {
                a.Configure(mini, fireRate: 1f, damage: 0, speed: 2.5f, lifetime: 1f);
                a.SetPierce(99);
                a.ConfigureSpit(SlimeTrail.ThinSpacing, 3f, burstDamage: 3, burstRadius: 0.8f, burstPuddleSeconds: 3f, mini: true);
                ClearExtras(a);
            });
            var one = Attack("Attack_PoisonSpit", template, a =>
            {
                a.Configure(ball, fireRate: 1f / 2.8f, damage: 1, speed: 2f, lifetime: 4f, automatic: true);
                a.SetPierce(99);
                a.ConfigureSpit(SlimeTrail.Spacing, 3f, burstDamage: 6, burstRadius: 1.5f, burstPuddleSeconds: 4f);
                ClearExtras(a);
            });
            var two = Attack("Attack_PoisonSpit_2", one, a =>
            {
                a.SetFireRate(1f / 2.4f);
                a.ConfigureSpit(SlimeTrail.Spacing, 4f, burstDamage: 8, burstRadius: 1.5f, burstPuddleSeconds: 4f);
            });
            var three = Attack("Attack_PoisonSpit_3", two, a =>
                a.ConfigureSpit(SlimeTrail.Spacing, 4f, burstDamage: 8, burstRadius: 1.5f, burstPuddleSeconds: 4f, splitCount: 4, splitAttack: miniAttack));

            mutation.ConfigureBase(mutation.DisplayName,
                "Bola de veneno lenta (2 u/s) que atraviesa a los enemigos con 1 de daño y deja una banda de baba que frena y envenena y se seca a los 3 s. Estalla a los 4 s (se hincha y parpadea al final), al chocar con una pared o al disparar otra: 6 de daño en 1,5 u y un charco salpicado de 4 s. Una bola cada 2,8 s.",
                MutationSlot.Mouth, mutation.Color);
            mutation.SetLevelDescriptions("Una bola cada 2,4 s, la baba dura 4 s y el estallido hace 8 de daño.",
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
            var cloneArt = new CloneArt
            {
                Spin = ArtSetup.Floor3CloneSheet("Fx_SpinClone_Spin", 40), Echo = ArtSetup.Floor3CloneSheet("Fx_SpinClone_Echo", 40),
                Whirl = ArtSetup.Floor3CloneSheet("Fx_SpinClone_Whirl", 32), Appear = ArtSetup.Floor3CloneSheet("Fx_SpinClone_Appear", 40),
                Merge = ArtSetup.Floor3CloneSheet("Fx_SpinClone_Merge", 40),
            };
            if (whirlFrames.Length == 0 || deflectFrames.Length == 0 || !cloneArt.Complete)
            {
                Debug.LogWarning("Floor3MouthsSetup: faltan las hojas de la Peonza; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            Directory.CreateDirectory(ProjectilesFolder);
            var whirl = Floor2MouthsSetup.CreateLoopEffect("Effect_SpinningTop_Whirl", whirlFrames, 0.05f, 11);
            var deflect = Floor2MouthsSetup.CreateFrameEffect("Effect_SpinningTop_Deflect", deflectFrames, 0.05f);
            var boomerang = CreateBoomerangPrefab("Boomerang_Diplocaulus", cloneArt);

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

        /// <summary>Las hojas del clon de la Peonza (floor3-top-clone).</summary>
        sealed class CloneArt
        {
            public Sprite[] Spin, Echo, Whirl, Appear, Merge;

            public bool Complete => Spin.Length > 0 && Echo.Length > 0 && Whirl.Length > 0 && Appear.Length > 0 && Merge.Length > 0;
        }

        /// <summary>
        /// El clon que sale y vuelve del nivel III: el clon girando (encima), su remolino, y dos ecos detrás que el código coloca en las
        /// dos posiciones anteriores. Orden de dibujo: ecos debajo, remolino, clon encima. Se rehace.
        /// </summary>
        static GameObject CreateBoomerangPrefab(string prefabName, CloneArt art)
        {
            var root = new GameObject(prefabName);
            try
            {
                SpriteRenderer Layer(string layerName, int order)
                {
                    var child = new GameObject(layerName);
                    child.transform.SetParent(root.transform, false);
                    var layer = child.AddComponent<SpriteRenderer>();
                    layer.sortingOrder = order;
                    layer.enabled = false;
                    return layer;
                }

                var echoes = new[] { Layer("Echo1", 19), Layer("Echo2", 19) };
                var whirl = Layer("Whirl", 20);
                var body = root.AddComponent<SpriteRenderer>();
                body.sprite = art.Appear[0];
                body.sortingOrder = 21;
                root.AddComponent<SpinBoomerang>().ConfigureArt(body, echoes, whirl, art.Spin, art.Echo, art.Whirl, art.Appear, art.Merge);
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
            // Los tramos tienen el pivote en el borde izquierdo, a media altura (README de floor3-jet).
            var segmentPivot = new Vector2(0f, 0.5f);
            var start = ArtSetup.Floor3JetSheet("Fx_VinegarJet_Start", 16, segmentPivot);
            var streams = new System.Collections.Generic.List<Sprite>();
            var tips = new System.Collections.Generic.List<Sprite>();
            foreach (string letter in new[] { "A", "B", "C" })
            {
                streams.AddRange(ArtSetup.Floor3JetSheet($"Fx_VinegarJet_Stream_{letter}", 16, segmentPivot));
                tips.AddRange(ArtSetup.Floor3JetSheet($"Fx_VinegarJet_Tip_{letter}", 16, segmentPivot));
            }
            var muzzleFrames = ArtSetup.Floor3JetSheet("Fx_VinegarJet_Muzzle", 16);
            var hitFrames = ArtSetup.Floor3JetSheet("Fx_VinegarJet_Hit", 24);
            var splashFrames = new Sprite[3][];
            for (int i = 0; i < 3; i++)
                splashFrames[i] = ArtSetup.Floor3JetSheet($"Fx_VinegarJet_Splash_{i}", 80);
            if (start.Length != JetArt.Frames || streams.Count != JetArt.Frames * 3 || tips.Count != JetArt.Frames * 3
                || muzzleFrames.Length == 0 || hitFrames.Length == 0 || splashFrames.Any(frames => frames.Length == 0))
            {
                Debug.LogWarning("Floor3MouthsSetup: faltan las hojas del Chorro de vinagre en Art/Floor3/floor3-jet; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
                return;
            }

            Directory.CreateDirectory(ProjectilesFolder);
            var art = AssetDatabase.LoadAssetAtPath<JetArt>(JetArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<JetArt>();
                AssetDatabase.CreateAsset(art, JetArtPath);
            }
            art.Configure(start, streams.ToArray(), tips.ToArray());
            EditorUtility.SetDirty(art);

            // Salida y salpicaduras a 50 y 65 ms por cuadro, impacto a 60 ms (README).
            var muzzle = Floor2MouthsSetup.CreateLoopEffect("Effect_VinegarJet_Muzzle", muzzleFrames, 0.05f, 21);
            var hit = Floor2MouthsSetup.CreateLoopEffect("Effect_VinegarJet_Hit", hitFrames, 0.06f, 21);
            var splash = new GameObject[3];
            for (int i = 0; i < 3; i++)
                splash[i] = Floor2MouthsSetup.CreateFrameEffect($"Effect_VinegarJet_Splash_{i}", splashFrames[i], 0.065f);

            var one = Attack("Attack_VinegarJet", null, a =>
            {
                a.Configure(null, fireRate: 1f / 0.15f, damage: 1, speed: 1f, lifetime: 1f);
                a.ConfigureJet(5f, 0.15f, 180f, 3f, 2f, 3f, false, art, hit, splash);
                a.ConfigureJetSplash(0f, 0.5f, 1);
                a.SetMuzzleEffect(muzzle);
            });
            var two = Attack("Attack_VinegarJet_2", one, a => a.ConfigureJet(6f, 0.15f, 180f, 4f, 2f, 3f, true, art, hit, splash));
            var three = Attack("Attack_VinegarJet_3", two, a =>
            {
                a.ConfigureJet(7f, 0.15f, 180f, 4f, 2f, 3f, true, art, hit, splash);
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

        /// <summary>Las hojas de una bola del Escupitajo: rodando hacia los lados, abajo y arriba, y el hinchado final (solo la grande).</summary>
        sealed class SpitBallArt
        {
            public readonly Sprite[] Side, Down, Up, SwellSide, SwellDown, SwellUp;

            public SpitBallArt(Sprite[] side, Sprite[] down, Sprite[] up, Sprite[] swellSide, Sprite[] swellDown, Sprite[] swellUp)
            {
                Side = side;
                Down = down;
                Up = up;
                SwellSide = swellSide;
                SwellDown = swellDown;
                SwellUp = swellUp;
            }

            public bool Complete => Side.Length > 0 && Down.Length > 0 && Up.Length > 0 && SwellSide.Length > 0 && SwellDown.Length > 0 && SwellUp.Length > 0;
        }

        /// <summary>
        /// Proyectil de la bola: la raíz gira hacia donde va (y es el punto de apoyo en el suelo) y un hijo lleva el dibujo, que no gira
        /// (<see cref="PoisonSpitBall"/> lo orienta). Se rehace.
        /// </summary>
        static Projectile CreateSpitProjectile(string prefabName, SpitBallArt art, float radius, float rollFrameSeconds)
        {
            var root = new GameObject(prefabName);
            try
            {
                var visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(root.transform, false);
                var renderer = visualObject.AddComponent<SpriteRenderer>();
                renderer.sprite = art.Side[0];
                renderer.sortingOrder = 20;
                var collider = root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = radius;
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;
                root.AddComponent<Projectile>();
                root.AddComponent<PoisonSpitBall>().ConfigureArt(renderer, art.Side, art.Down, art.Up, art.SwellSide, art.SwellDown, art.SwellUp,
                    rollFrameSeconds);
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

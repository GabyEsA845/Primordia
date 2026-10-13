using Primordia.Combat;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Niveles II y III de las 9 mutaciones (la tabla del GDD): valores, evoluciones, textos de cada nivel y los
    /// efectos que usan. Rehace los ataques de cada nivel a partir del de nivel I, así que se puede volver a
    /// ejecutar después de retocar valores. Los efectos los crea Primordia > Art > Importar evoluciones; sin
    /// ellos, todo funciona igual pero sin esas marcas.
    /// </summary>
    public static class MutationLevelsSetup
    {
        const string DataFolder = "Assets/_Primordia/Data/Mutations";
        const string AttacksFolder = "Assets/_Primordia/Data/Attacks";
        const string EffectsFolder = "Assets/_Primordia/Prefabs/Effects";
        const string MutationPrefabsFolder = "Assets/_Primordia/Prefabs/Mutations";
        const string CombatPrefabsFolder = "Assets/_Primordia/Prefabs/Combat";

        [MenuItem("Primordia/Mutations/Configurar niveles y evoluciones")]
        internal static void ConfigureLevels()
        {
            var poison = Effect("Effect_Poison");
            var stun = Effect("Effect_Status_Stun");

            Edit<AttackMutation>("Mandibles", m =>
            {
                var two = LevelAttack(m.Attack, "Attack_Mandibles_2", a =>
                {
                    a.SetDamage(4);
                    a.SetMeleeRange(1.5f * 1.15f);
                });
                var three = LevelAttack(two, "Attack_Mandibles_3", a => a.ConfigureBleed(1, 3f, Effect("Effect_Status_Bleed")));
                m.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two, healEveryKills = 3 },
                    new AttackMutation.AttackLevel { attack = three, healEveryKills = 2 });
                m.SetHealEffect(Effect("Effect_Heal"));
                m.SetLevelDescriptions("Mordisco más fuerte (4 de daño) y un 15 % más de alcance.",
                    "Desgarro: el mordisco hace sangrar (1 de daño por segundo durante 3 s) y curas medio corazón cada 2 enemigos.");
            });

            Edit<AttackMutation>("Stinger", m =>
            {
                var two = LevelAttack(m.Attack, "Attack_Stinger_2", a => a.SetPoison(2, 3f));
                var three = LevelAttack(two, "Attack_Stinger_3", a => a.ConfigureContagion(2f, Effect("Effect_PoisonBurst")));
                m.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
                m.SetLevelDescriptions("El veneno quita 2 de vida por segundo.",
                    "Contagio: un enemigo envenenado que muere estalla y envenena a los que tenga cerca.");
            });

            Edit<AttackMutation>("Spores", m =>
            {
                var two = LevelAttack(m.Attack, "Attack_Spores_2", a => a.SetFan(5, 45f));
                var three = LevelAttack(two, "Attack_Spores_3", a => a.ConfigureHoming(180f, 5f));
                m.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
                m.SetLevelDescriptions("Cinco esporas en un abanico más abierto.",
                    "Esporas buscadoras: se curvan hacia el enemigo más cercano.");
            });

            Edit<StatMutation>("Fins", m =>
            {
                var upgrade = new StatMutation.StatLevel { moveSpeedMultiplier = 1.45f, dodgeDistanceMultiplier = 1.5f, dodgeCooldownMultiplier = 0.75f };
                m.ConfigureUpgrades(upgrade, upgrade);
                m.ConfigureAmbush(0.5f, 1.5f, Effect("Effect_Ambush"));
                m.SetLevelDescriptions("+45 % de velocidad y esquivas más seguidas (−25 % de espera).",
                    "Emboscada: tras esquivar, haces un 50 % más de daño durante 1,5 s.");
            });

            Edit<DodgePushMutation>("JointedLegs", m =>
            {
                var upgrade = new DodgePushMutation.PushLevel { stunDuration = 1f, damage = 2 };
                m.ConfigureUpgrades(upgrade, upgrade);
                m.ConfigureStomp(2f, Effect("Effect_Stomp"), stun);
                m.SetLevelDescriptions("El empujón aturde 1 s y hace 2 de daño.",
                    "Pisotón: al terminar cada esquiva, aturdes a todos los enemigos cercanos.");
            });

            Edit<StatMutation>("Carapace", m =>
            {
                var upgrade = new StatMutation.StatLevel { moveSpeedMultiplier = 0.95f, maxHealthBonus = 4 };
                m.ConfigureUpgrades(upgrade, upgrade);
                m.ConfigurePlates(true, Effect("Effect_Plates"), Effect("Effect_PlatesBreak"));
                m.SetLevelDescriptions("+2 corazones en total y solo −5 % de velocidad.",
                    "Placas: anulan el primer golpe que recibes en cada sala.");
            });

            Edit<ThornsMutation>("StingingSkin", m =>
            {
                var upgrade = new ThornsMutation.ThornsLevel { damage = 3, poisonDamagePerSecond = 1, poisonDuration = 3f };
                m.ConfigureUpgrades(upgrade, upgrade);
                m.ConfigureQuills(QuillAttack(poison), 8, poison);
                m.SetLevelDescriptions("3 de daño al tocarte, y además envenena.",
                    "Erizo: al recibir un golpe, disparas 8 púas venenosas en círculo.");
            });

            Edit<InkCloudMutation>("InkSac", m =>
            {
                var large = AssetDatabase.LoadAssetAtPath<GameObject>($"{MutationPrefabsFolder}/Effect_InkCloud_Large.prefab");
                m.ConfigureUpgrades(
                    new InkCloudMutation.InkLevel { radius = 4f, cooldown = 6f, effect = large, effectRadius = 4f },
                    new InkCloudMutation.InkLevel { radius = 4f, cooldown = 6f, effect = large, effectRadius = 4f, damagePerSecond = 1 });
                m.SetBlindMarker(Effect("Effect_Status_Blind"));
                m.SetLevelDescriptions("Nube más grande (radio 4) y recarga de 6 s.",
                    "Tinta corrosiva: los enemigos dentro de la nube pierden 1 de vida por segundo.");
            });

            Edit<ShockwaveMutation>("ShockGland", m =>
            {
                var upgrade = new ShockwaveMutation.ShockLevel
                {
                    radius = 6f, cooldown = 4.5f, effect = Effect("Effect_Shockwave_Large"), effectRadius = 6f,
                };
                m.ConfigureUpgrades(upgrade, upgrade);
                m.ConfigureParalysis(2f, Effect("Effect_Status_Paralyzed"), Effect("Effect_Status_ParalyzedBody"));
                m.SetLevelDescriptions("Onda mucho más grande (radio 6) y recarga de 4,5 s.",
                    "Paralizante: los enemigos que alcanza la onda quedan paralizados 2 s.");
            });

            AddLevelUpEffectToPlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("MutationLevelsSetup: niveles y evoluciones configurados.");
        }

        static void Edit<T>(string name, System.Action<T> edit) where T : MutationData
        {
            var mutation = AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/Mutation_{name}.asset");
            if (mutation == null)
            {
                Debug.LogWarning($"MutationLevelsSetup: no existe Mutation_{name}; créalo con Primordia > Mutations.");
                return;
            }

            edit(mutation);
            EditorUtility.SetDirty(mutation);
        }

        /// <summary>Ataque de un nivel: copia del anterior con sus cambios. Si ya existe, se rehace igual.</summary>
        static AttackData LevelAttack(AttackData previous, string assetName, System.Action<AttackData> change)
        {
            if (previous == null)
                return null;

            string path = $"{AttacksFolder}/{assetName}.asset";
            var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            bool created = attack == null;
            if (created)
                attack = ScriptableObject.CreateInstance<AttackData>();

            attack.CopyFrom(previous);
            change(attack);

            if (created)
                AssetDatabase.CreateAsset(attack, path);
            else
                EditorUtility.SetDirty(attack);
            return attack;
        }

        /// <summary>Púa del Erizo: 1 de daño y veneno, unas 4 unidades de alcance.</summary>
        static AttackData QuillAttack(GameObject poison)
        {
            var projectile = AssetDatabase.LoadAssetAtPath<Projectile>($"{CombatPrefabsFolder}/Projectile_Quill.prefab");
            if (projectile == null)
            {
                Debug.LogWarning("MutationLevelsSetup: falta Projectile_Quill; créalo con Primordia > Art > Importar evoluciones.");
                return null;
            }

            var attack = CombatSetup.CreateAttack("Attack_Quill", projectile, fireRate: 1f, damage: 1, speed: 9f, lifetime: 0.45f,
                poisonDamagePerSecond: 1, poisonDuration: 3f);
            attack.Configure(projectile, 1f, 1, 9f, 0.45f, poisonDamagePerSecond: 1, poisonDuration: 3f);
            attack.SetPoisonEffect(poison);
            EditorUtility.SetDirty(attack);
            return attack;
        }

        static void AddLevelUpEffectToPlayer()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerMutations>(out _))
                    return;

                var effect = root.TryGetComponent<LevelUpEffect>(out var existing) ? existing : root.AddComponent<LevelUpEffect>();
                effect.Configure(Effect("Effect_LevelUp"), Effect("Effect_LevelUp_Gold"));
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static GameObject Effect(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectsFolder}/{prefabName}.prefab");
    }
}

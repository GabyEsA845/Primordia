using System.IO;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Las Bocas del piso 2 con sus ataques de verdad (GDD, "Mutaciones del piso 2"), por pasos: Cnidocitos (arpón que
    /// atraviesa, veneno y Ramillete); después Placas mandibulares y Corona de brazos. Importa su arte de
    /// docs/multimedia-temp/floor2-bodies/Effects. Rehace los ataques de cada nivel; las mutaciones ya existen (las crea
    /// Primordia > Floor transition) y aquí solo cambian su ataque y sus textos.
    /// </summary>
    public static class Floor2MouthsSetup
    {
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor2";
        const string SourceFolder = "docs/multimedia-temp/floor2-bodies/Effects";
        const string ArtFolder = "Assets/_Primordia/Art/Effects/Floor2";
        const string ProjectilesFolder = "Assets/_Primordia/Prefabs/Combat";
        const string EffectsFolder = "Assets/_Primordia/Prefabs/Effects";

        [MenuItem("Primordia/Mutations/Crear Bocas del piso 2")]
        internal static void CreateAll()
        {
            Directory.CreateDirectory(ArtFolder);
            CreateCnidocytes();
            CreateJawPlates();
            AssetDatabase.SaveAssets();
            Debug.Log("Floor2MouthsSetup: Cnidocitos (arpón, veneno y Ramillete) y Placas mandibulares (mordisco, Quebrar y Guillotina). La Corona de brazos, en el paso 4.");
        }

        // ---------- Cnidocitos ----------

        static void CreateCnidocytes()
        {
            var mutation = AssetDatabase.LoadAssetAtPath<AttackMutation>($"{MutationsFolder}/Mutation_Cnidocytes.asset");
            if (mutation == null)
            {
                Debug.LogWarning("Floor2MouthsSetup: no existe Mutation_Cnidocytes; créala antes con Primordia > Floor transition > Crear sala de paso y escena de prueba.");
                return;
            }

            var harpoon = CreateProjectile("Projectile_Cnidocyte",
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Proj_Cnidocyte.png", $"{ArtFolder}/Proj_Cnidocyte.png", 16), radius: 0.15f);
            var muzzle = CreateFrameEffect("Effect_Cnidocyte_Muzzle",
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Fx_Cnidocyte_Muzzle.png", $"{ArtFolder}/Fx_Cnidocyte_Muzzle.png", 16), 0.04f);
            var stinger = CombatSetup.LoadAttack("Attack_Stinger");

            // Nivel I: 2 de daño, 3 por segundo, atraviesa a 1 y veneno de 1 por segundo durante 3 s.
            var one = Attack("Attack_Cnidocytes", stinger, a =>
            {
                a.Configure(harpoon, fireRate: 3f, damage: 2, speed: 15f, lifetime: 0.8f, automatic: true, poisonDamagePerSecond: 1, poisonDuration: 3f);
                a.SetPierce(1);
                a.ConfigureVolley(0, 3, 30f);
                a.SetMuzzleEffect(muzzle);
                a.ConfigureHoming(0f, 0f);
                a.ConfigureContagion(0f, null);
                a.ConfigureBleed(0, 0f, null);
            });
            // Nivel II: atraviesa a 2 y el veneno sube a 2 por segundo.
            var two = Attack("Attack_Cnidocytes_2", one, a =>
            {
                a.SetPierce(2);
                a.SetPoison(2, 3f);
            });
            // Nivel III, Ramillete: cada cuarto disparo son 3 arpones en abanico.
            var three = Attack("Attack_Cnidocytes_3", two, a => a.ConfigureVolley(4, 3, 30f));

            mutation.ConfigureBase(mutation.DisplayName, "Arpón urticante que atraviesa a 1 enemigo: 2 de daño, 3 por segundo y veneno de 1 por segundo durante 3 s.",
                MutationSlot.Mouth, mutation.Color);
            mutation.SetLevelDescriptions("Atraviesa a 2 enemigos y el veneno sube a 2 por segundo.",
                "Ramillete: cada cuarto disparo son 3 arpones en abanico.");
            mutation.Configure(one, newHealEveryKills: 0);
            mutation.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
            EditorUtility.SetDirty(mutation);
        }

        // ---------- Placas mandibulares ----------

        /// <summary>Alcance del mordisco desde la boca: lo dibujado (30 px de la unión a la punta, a 16 px por unidad).</summary>
        const float JawReach = 30f / 16f;
        /// <summary>Alcance de la segunda dentellada de la Guillotina: 52 px.</summary>
        const float GuillotineReach = 52f / 16f;

        static void CreateJawPlates()
        {
            var mutation = AssetDatabase.LoadAssetAtPath<AttackMutation>($"{MutationsFolder}/Mutation_JawPlates.asset");
            if (mutation == null)
            {
                Debug.LogWarning("Floor2MouthsSetup: no existe Mutation_JawPlates; créala antes con Primordia > Floor transition > Crear sala de paso y escena de prueba.");
                return;
            }

            // Dos placas de hueso dibujadas hacia la derecha con la unión en el borde izquierdo: el pivote ahí, a escala 1.
            var bite = CreateFrameEffect("Effect_JawPlates_Bite",
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Fx_JawPlates_Bite.png", $"{ArtFolder}/Fx_JawPlates_Bite.png", 32, new Vector2(1f / 32f, 0.5f)),
                0.05f, sortingOrder: 12);
            var guillotine = CreateFrameEffect("Effect_JawPlates_Guillotine",
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Fx_JawPlates_Guillotine.png", $"{ArtFolder}/Fx_JawPlates_Guillotine.png", 56, new Vector2(1f / 56f, 0.5f)),
                0.05f, sortingOrder: 12);
            var marker = CreateLoopEffect("Effect_Status_Shatter",
                ArtSetup.ImportLooseSheet($"{SourceFolder}/Status_Shatter.png", $"{ArtFolder}/Status_Shatter.png", 12), 0.15f, sortingOrder: 22);
            var mandibles = CombatSetup.LoadAttack("Attack_Mandibles");

            // Nivel I: arco de 110° tan largo como lo dibujado, 5 de daño, 2 por segundo, empujón y Quebrar (+25 % de daño
            // durante 2 s, con su marca).
            var one = Attack("Attack_JawPlates", mandibles, a =>
            {
                a.Configure(null, fireRate: 2f, damage: 5, speed: 12f, lifetime: 0.25f);
                a.ConfigureMelee(JawReach, 110f, bite);
                a.SetMeleeFromMuzzle(true);
                a.ConfigureLunge(0f, 0.1f);
                a.ConfigureBleed(0, 0f, null);
                a.ConfigureJaws(push: 6f, newShatterDuration: 2f, newShatterMultiplier: 1.25f);
                a.SetShatterMarker(marker);
                a.ConfigureGuillotine(0, GuillotineReach, guillotine);
            });
            // Nivel II: 6 de daño y Quebrar dura 3 s.
            var two = Attack("Attack_JawPlates_2", one, a =>
            {
                a.SetDamage(6);
                a.ConfigureJaws(6f, 3f, 1.25f);
            });
            // Nivel III, Guillotina: cada cuarto mordisco, una segunda dentellada más grande que llega más lejos.
            var three = Attack("Attack_JawPlates_3", two, a => a.ConfigureGuillotine(4, GuillotineReach, guillotine));

            mutation.ConfigureBase(mutation.DisplayName,
                "Mordisco en arco de 110°: 5 de daño, 2 por segundo y un pequeño empujón. Quebrar: el enemigo mordido recibe un 25 % más de daño de todo durante 2 s.",
                MutationSlot.Mouth, mutation.Color);
            mutation.SetLevelDescriptions("6 de daño y Quebrar dura 3 s.",
                "Guillotina: cada cuarto mordisco va seguido de una segunda dentellada más grande, que llega más lejos.");
            mutation.Configure(one, newHealEveryKills: 0);
            mutation.ConfigureUpgrades(new AttackMutation.AttackLevel { attack = two }, new AttackMutation.AttackLevel { attack = three });
            EditorUtility.SetDirty(mutation);
        }

        // ---------- Ayudas ----------

        /// <summary>
        /// El ataque de ese nombre, copiado de <paramref name="from"/> y cambiado con <paramref name="edit"/>. Se rehace cada vez
        /// (conserva el asset para no romper referencias).
        /// </summary>
        static AttackData Attack(string assetName, AttackData from, System.Action<AttackData> edit)
        {
            var attack = CombatSetup.LoadAttack(assetName);
            if (attack == null)
                attack = CombatSetup.CreateAttack(assetName, null, 1f, 1, 1f, 1f);
            if (from != null)
                attack.CopyFrom(from);
            edit(attack);
            EditorUtility.SetDirty(attack);
            return attack;
        }

        /// <summary>Proyectil animado en bucle (los cuadros miran a la derecha), con el impacto lima del jugador. Se rehace.</summary>
        static Projectile CreateProjectile(string prefabName, Sprite[] frames, float radius)
        {
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = 20;
                if (frames.Length > 1)
                    root.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.08f, 0f);

                var collider = root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = radius;
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;

                var projectile = root.AddComponent<Projectile>();
                projectile.SetImpactEffect(AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectsFolder}/Effect_Impact_Player.prefab"));
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilesFolder}/{prefabName}.prefab").GetComponent<Projectile>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Efecto en bucle (marcas de estado: las quita quien las pone). Se rehace.</summary>
        static GameObject CreateLoopEffect(string prefabName, Sprite[] frames, float frameDuration, int sortingOrder)
        {
            if (frames.Length == 0)
                return null;

            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = sortingOrder;
                root.AddComponent<SpriteLoop>().Configure(renderer, frames, frameDuration, 0f);
                Directory.CreateDirectory(EffectsFolder);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{EffectsFolder}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Efecto de cuadros que se reproduce una vez y se destruye (<see cref="FrameEffect"/>). Se rehace.</summary>
        static GameObject CreateFrameEffect(string prefabName, Sprite[] frames, float frameDuration, int sortingOrder = 21, float scale = 1f)
        {
            if (frames.Length == 0)
                return null;

            var root = new GameObject(prefabName);
            root.transform.localScale = Vector3.one * scale;
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = sortingOrder;
                root.AddComponent<FrameEffect>().Configure(frames, frameDuration);
                Directory.CreateDirectory(EffectsFolder);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{EffectsFolder}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
